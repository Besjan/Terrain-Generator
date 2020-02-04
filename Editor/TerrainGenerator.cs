namespace Cuku
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEditor;
    using System.IO;
    using System;
    using System.Linq;

    public class TerrainGenerator
    {
        #region Properties
        static string SourceDataPath = "Assets/StreamingAssets/Data";
        static string CompletedDataPath = "Assets/StreamingAssets/Completed";
        static string TerrainDataPath = "TerrainData/";

        static int TileResolution = 4097;

        static int CenterTileLon = 392000;
        static int CenterTileLat = 5820000;

        static Transform Terrain;
        static GameObject TileObjectPrefab;

        struct Tile
        {
            public string Id;
            public float[,] Heights;
            public int[] Bounds;
        }
        #endregion

        [MenuItem("Cuku/Generate Terrain Data")]
        static void GenerateTerrainData()
        {
            var filePath = Directory.GetFiles(SourceDataPath, "*.txt")[0];

            if (string.IsNullOrEmpty(filePath))
            {
                Debug.Log("Finished!");
            }

            CreateTilesData(filePath);
        }

        [MenuItem("Cuku/Generate Terrain Tiles")]
        static void GenerateTerrainTiles()
        {
            Terrain = new GameObject("Berlin Terrain").transform;
            TileObjectPrefab = Resources.Load<GameObject>("Tile");
        }

        static void CreateTilesData(string filePath)
        {
            // Get patch coordinates
            var coordinates = Path.GetFileNameWithoutExtension(filePath).Split(new char[] { '_' });
            var patchLon = Convert.ToInt32(coordinates[0]) * 1000;
            var patchLat = Convert.ToInt32(coordinates[1]) * 1000;
            Debug.Log(patchLon + "_" + patchLat);

            List<Tile> tiles = GetRelatedTilesData(patchLon, patchLat);

            // Put all patch points in related tiles
            using (FileStream fs = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (BufferedStream bs = new BufferedStream(fs))
            using (StreamReader sr = new StreamReader(bs))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    var pointString = line.Split(new char[] { ' ' });
                    var lon = Convert.ToInt32(pointString[0]);
                    var lat = Convert.ToInt32(pointString[1]);
                    var height = float.Parse(pointString[2]);

                    MoveTilePoint(lon, lat, height, tiles);
                }

                fs.Close();
                fs.Dispose();
            }

            for (int i = 0; i < tiles.Count; i++)
            {
                Tile tile = tiles[i];
                var terrainData = CreateTerrainData(tile.Heights);
                AssetDatabase.CreateAsset(terrainData, string.Format("Assets/Resources/{0}{1}.asset", TerrainDataPath, tile.Id));
                AssetDatabase.Refresh();

                tile.Heights = null;
                terrainData = null;
            }

            tiles = null;

            var completedPath = Path.Combine(CompletedDataPath, Path.GetFileName(filePath));
            File.Move(filePath, completedPath);

            GenerateTerrainData();
        }

        private static List<Tile> GetRelatedTilesData(int patchLon, int patchLat)
        {
            var tiles = new List<Tile>();

            var tileX = (int)Math.Floor(1f * (patchLon - CenterTileLon) / TileResolution);
            var tileZ = (int)Math.Floor(1f * (patchLat - CenterTileLat) / TileResolution);

            for (int x = tileX - 1; x < tileX + 2; x++)
            {
                for (int z = tileZ - 1; z < tileZ + 2; z++)
                {
                    var tileId = string.Format("{0}_{1}", x, z);

                    var bounds = new int[4];
                    bounds[0] = CenterTileLon + x * (TileResolution - 1);
                    bounds[1] = bounds[0] + (TileResolution - 1);
                    bounds[2] = CenterTileLat + z * (TileResolution - 1);
                    bounds[3] = bounds[2] + (TileResolution - 1);

                    // Add new tile if is current or next tile
                    var isNewTile = x - tileX >= 0 && z - tileZ >= 0;
                    AddRelatedTile(tiles, tileId, bounds, isNewTile);
                }
            }

            return tiles;
        }

        static void AddRelatedTile(List<Tile> tiles, string tileId, int[] bounds, bool canAddNew)
        {
            var terrainData = Resources.Load<TerrainData>(TerrainDataPath + tileId);
            if (terrainData != null)
            {
                var heights = DenormalizeHeights(terrainData.GetHeights(0, 0, TileResolution, TileResolution), terrainData.size.y);
                tiles.Add(new Tile
                {
                    Id = tileId,
                    Heights = heights,
                    Bounds = bounds
                });
                return;
            }

            // Cannot add new tile in case is previous tile
            if (!canAddNew) return;

            tiles.Add(new Tile
            {
                Id = tileId,
                Heights = new float[TileResolution, TileResolution],
                Bounds = bounds
            });
            return;
        }

        static void MoveTilePoint(int pointLon, int pointLat, float height, List<Tile> tiles)
        {
            foreach (var tile in tiles)
            {
                if (pointLon < tile.Bounds[0] || pointLon > tile.Bounds[1] ||
                    pointLat < tile.Bounds[2] || pointLat > tile.Bounds[3])
                    continue;

                var clm = pointLon - tile.Bounds[0];
                var row = pointLat - tile.Bounds[2];

                tile.Heights[row, clm] = height;
            }
        }

        /// <summary>
        /// Creates terrain data from heights.
        /// </summary>
        /// <param name="heights">Terrain height percentages ranging from 0 to 1.</param>
        /// <param name="heightSampleDistance">The horizontal/vertical distance between height samples.</param>
        /// <returns>A TerrainData instance.</returns>
        static TerrainData CreateTerrainData(float[,] heights, float heightSampleDistance = 1)
        {
            var maxHeight = heights.Cast<float>().Max();
            heights = NormalizeHeights(heights, maxHeight);

            Debug.Assert((heights.GetLength(0) == heights.GetLength(1)) && (maxHeight >= 0) && (heightSampleDistance >= 0));

            // Create the TerrainData.
            var terrainData = new TerrainData();
            terrainData.heightmapResolution = heights.GetLength(0);

            var terrainWidth = (terrainData.heightmapResolution - 1) * heightSampleDistance;

            // If maxHeight is 0, leave all the heights in terrainData at 0 and make the vertical size of the terrain 1 to ensure valid AABBs.
            if (Mathf.Approximately(maxHeight, 0))
            {
                terrainData.size = new Vector3(terrainWidth, 1, terrainWidth);
            }
            else
            {
                terrainData.size = new Vector3(terrainWidth, maxHeight, terrainWidth);
                terrainData.SetHeights(0, 0, heights);
            }

            return terrainData;
        }

        static float[,] NormalizeHeights(float[,] heights, float maxHeight)
        {
            int bound0 = heights.GetUpperBound(0);
            int bound1 = heights.GetUpperBound(1);
            for (int clm = 0; clm <= bound0; clm++)
            {
                for (int row = 0; row <= bound1; row++)
                {
                    heights[row, clm] /= maxHeight;
                }
            }

            return heights;
        }

        static float[,] DenormalizeHeights(float[,] heights, float maxHeight)
        {
            int bound0 = heights.GetUpperBound(0);
            int bound1 = heights.GetUpperBound(1);
            for (int clm = 0; clm <= bound0; clm++)
            {
                for (int row = 0; row <= bound1; row++)
                {
                    heights[row, clm] *= maxHeight;
                }
            }

            return heights;
        }

        static Mesh CreateTileMesh(string name, float[,] heights)
        {
            // Create new mesh asset
            var newMesh = new Mesh();
            newMesh.name = name;
            newMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            var hRes = heights.GetUpperBound(0) - 1;
            var vRes = heights.GetUpperBound(1) - 1;

            var length = vRes - 1;
            var width = hRes - 1;

            #region Vertices		
            Vector3[] vertices = new Vector3[hRes * vRes];
            // Loop columns
            for (int clm = 0; clm < hRes; clm++)
            {
                float vPos = ((float)clm / (hRes - 1)) * length;
                // Loop rows
                for (int row = 0; row < vRes; row++)
                {
                    float hPos = ((float)row / (vRes - 1)) * width;
                    var id = row + clm * vRes;
                    vertices[id] = new Vector3(hPos, heights[row, clm], vPos);
                }
            }
            #endregion

            #region Normals
            Vector3[] normals = new Vector3[vertices.Length];
            for (int n = 0; n < normals.Length; n++)
            {
                normals[n] = Vector3.up;
            }
            #endregion

            #region UVs		
            Vector2[] uvs = new Vector2[vertices.Length];
            for (int v = 0; v < hRes; v++)
            {
                for (int u = 0; u < vRes; u++)
                {
                    uvs[u + v * vRes] = new Vector2((float)u / (vRes - 1), (float)v / (hRes - 1));
                }
            }
            #endregion

            #region Triangles
            int nbFaces = (vRes - 1) * (hRes - 1);
            int[] triangles = new int[nbFaces * 6];
            int t = 0;
            for (int face = 0; face < nbFaces; face++)
            {
                // Retrieve lower left corner from face ind
                int i = face % (vRes - 1) + (face / (hRes - 1) * vRes);

                triangles[t++] = i + vRes;
                triangles[t++] = i + 1;
                triangles[t++] = i;

                triangles[t++] = i + vRes;
                triangles[t++] = i + vRes + 1;
                triangles[t++] = i + 1;
            }
            #endregion

            newMesh.MarkDynamic();
            newMesh.vertices = vertices;
            newMesh.normals = normals;
            newMesh.uv = uvs;
            newMesh.triangles = triangles;

            var meshObj = new GameObject(name);
            var mf = meshObj.AddComponent<MeshFilter>();
            mf.mesh = newMesh;
            meshObj.AddComponent<MeshRenderer>();

            return newMesh;
        }
    }
}
