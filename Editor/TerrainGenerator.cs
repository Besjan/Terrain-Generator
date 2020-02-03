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
        static string DataPath = "Assets/StreamingAssets/Test";
        static string TerrainDataPath = "TerrainData/";

        static Transform Terrain;

        static GameObject TileObjectPrefab;

        static int PatchResolution = 2000; // 2km

        static int TileResolution = 4097;

        static int TilesPerPatch = 10;
        static int TileCoordinateStep;

        static int CenterTileLon = 392000;
        static int CenterTileLat = 5820000;

        struct Tile
        {
            public int Lon;
            public int Lat;
            public Mesh Mesh;
        }

        struct TileData
        {
            public string Id;
            public float[,] Heights;
            public int[] Bounds;
        }
        #endregion

        static string[] Initialize(string terrainName)
        {
            //Terrain = new GameObject(terrainName).transform;

            TileObjectPrefab = Resources.Load<GameObject>("Tile");

            TileCoordinateStep = PatchResolution / TilesPerPatch;

            var dataPaths = Directory.GetFiles(DataPath, "*.txt");

            return dataPaths;
        }

        [MenuItem("Cuku/Generate Terrain From DGM (1m grid)")]
        static void GenerateTerrainFromDGM1()
        {
            var filePaths = Initialize("Terrain DGM (1m grid)");

            for (int filePath = 0; filePath < filePaths.Length; filePath++)
            {
                CreateTilesData(filePaths[filePath]);
            }
        }

        static void CreateTilesData(string filePath)
        {
            // Get patch coordinates
            var coordinates = Path.GetFileNameWithoutExtension(filePath).Split(new char[] { '_' });
            var patchLon = Convert.ToInt32(coordinates[0]) * 1000;
            var patchLat = Convert.ToInt32(coordinates[1]) * 1000;
            Debug.Log(patchLon + "_" + patchLat);

            List<TileData> tilesData = GetRelatedTilesData(patchLon, patchLat);

            Debug.Log(tilesData.Count);
            Debug.Log("-----------------");

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

                    MoveTilePoint(lon, lat, height, tilesData);
                }
            }

            foreach (var tile in tilesData)
            {
                var terrainData = CreateTerrainData(tile.Heights);
                AssetDatabase.CreateAsset(terrainData, string.Format("Assets/Resources/{0}{1}.asset", TerrainDataPath, tile.Id));
            }
        }

        private static List<TileData> GetRelatedTilesData(int patchLon, int patchLat)
        {
            var tilesData = new List<TileData>();

            // Current tile
            var currentTileX = (int)Math.Floor(1f * (patchLon - CenterTileLon) / TileResolution);
            var currentTileZ = (int)Math.Floor(1f * (patchLat - CenterTileLat) / TileResolution);
            var currentTileId = string.Format("{0}_{1}", currentTileX, currentTileZ);
            int[] currentTileBounds = GetTileBounds(currentTileX, currentTileZ);
            AddRelatedTile(tilesData, currentTileId, currentTileBounds);

            // Next up tile
            var nextUpTileX = currentTileX;
            var nextUpTileZ = currentTileZ + 1;
            var nextUpTileId = string.Format("{0}_{1}", nextUpTileX, nextUpTileZ);
            var nextUpTileBounds = GetTileBounds(nextUpTileX, nextUpTileZ);
            AddRelatedTile(tilesData, nextUpTileId, nextUpTileBounds);

            // Next right tile
            var nextRightTileX = currentTileX + 1;
            var nextRightTileZ = currentTileZ;
            var nextRightTileId = string.Format("{0}_{1}", nextRightTileX, nextRightTileZ);
            var nextRightTileBounds = GetTileBounds(nextRightTileX, nextRightTileZ);
            AddRelatedTile(tilesData, nextRightTileId, nextRightTileBounds);

            // Next up right tile
            var nextUpRightTileX = currentTileX;
            var nextUpRightTileZ = currentTileZ + 1;
            var nextUpRightTileId = string.Format("{0}_{1}", nextUpRightTileX, nextUpRightTileZ);
            var nextUpRightTileBounds = GetTileBounds(nextUpRightTileX, nextUpRightTileZ);
            AddRelatedTile(tilesData, nextUpRightTileId, nextUpRightTileBounds);

            // Previous tile bottom
            var previousBottomTileX = currentTileX;
            var previousBottomTileZ = currentTileZ - 1;
            var previousBottomTileId = string.Format("{0}_{1}", previousBottomTileX, previousBottomTileZ);
            var previousBottomTileBounds = GetTileBounds(previousBottomTileX, previousBottomTileZ);
            AddRelatedTile(tilesData, previousBottomTileId, previousBottomTileBounds, false);

            // Previous tile left
            var previousLeftTileX = currentTileX - 1;
            var previousLeftTileZ = currentTileZ;
            var previousLeftTileId = string.Format("{0}_{1}", previousLeftTileX, previousLeftTileZ);
            var previousLeftTileBounds = GetTileBounds(previousLeftTileX, previousLeftTileZ);
            AddRelatedTile(tilesData, previousLeftTileId, previousLeftTileBounds, false);

            // Previous tile bottom left
            var previousBottomLeftTileX = currentTileX - 1;
            var previousBottomLeftTileZ = currentTileZ - 1;
            var previousBottomLeftTileId = string.Format("{0}_{1}", previousBottomLeftTileX, previousBottomLeftTileZ);
            var previousBottomLeftTileBounds = GetTileBounds(previousBottomLeftTileX, previousBottomLeftTileZ);
            AddRelatedTile(tilesData, previousBottomLeftTileId, previousBottomLeftTileBounds, false);

            Debug.Log(currentTileId);

            Debug.Log(nextUpTileId);
            Debug.Log(nextRightTileId);
            Debug.Log(nextUpRightTileId);

            Debug.Log(previousBottomTileId);
            Debug.Log(previousLeftTileId);
            Debug.Log(previousBottomLeftTileId);

            return tilesData;
        }

        private static int[] GetTileBounds(int currentTileX, int currentTileZ)
        {
            var currentTileBounds = new int[4];
            currentTileBounds[0] = CenterTileLon + currentTileX * (TileResolution - 1);
            currentTileBounds[1] = currentTileBounds[0] + (TileResolution - 1);
            currentTileBounds[2] = CenterTileLat + currentTileZ * (TileResolution - 1);
            currentTileBounds[3] = currentTileBounds[2] + (TileResolution - 1);
            return currentTileBounds;
        }

        static void AddRelatedTile(List<TileData> tiles, string tileId, int[] bounds, bool canAddNew = true)
        {
            var terrainData = Resources.Load<TerrainData>(TerrainDataPath + tileId);
            if (terrainData != null)
            {
                var heights = DenormalizeHeights(terrainData.GetHeights(0, 0, TileResolution, TileResolution), terrainData.size.y);
                tiles.Add(new TileData
                {
                    Id = tileId,
                    Heights = heights,
                    Bounds = bounds
                });
                return;
            }

            // In case of previous tiles
            if (!canAddNew) return;

            tiles.Add(new TileData
            {
                Id = tileId,
                Heights = new float[TileResolution, TileResolution],
                Bounds = bounds
            });
            return;
        }

        static void MoveTilePoint(int pointLon, int pointLat, float height, List<TileData> tiles)
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

        static void CreateTiles(string filePath)
        {
            // Get patch coordinates
            var coordinates = Path.GetFileNameWithoutExtension(filePath).Split(new char[] { '_' });
            var patchLon = Convert.ToInt32(coordinates[0]) * 1000;
            var patchLat = Convert.ToInt32(coordinates[1]) * 1000;

            // Create patch game object
            var patch = new GameObject(patchLon + "_" + patchLat).transform;
            patch.SetParent(Terrain);

            // Create tile game objects
            var tiles = new Tile[TilesPerPatch * TilesPerPatch];
            int tileId = 0;

            // Loop vertical tiles
            for (int vTile = 0; vTile < TilesPerPatch; vTile++)
            {
                var tileLat = patchLat + vTile * TileCoordinateStep;

                // Loop horizontal tiles
                for (int hTile = 0; hTile < TilesPerPatch; hTile++)
                {
                    var tileLon = patchLon + hTile * TileCoordinateStep;

                    var tileMesh = CreateTile(patch, tileLon, tileLat);
                    var tile = new Tile()
                    {
                        Lon = tileLon,
                        Lat = tileLat,
                        Mesh = tileMesh
                    };

                    tiles[tileId] = tile;
                    tileId++;
                }
            }

            var tilesWithMissingPoints = GetTilesWithMissingPoints(patchLon, patchLat);

            // Move tile points
            using (FileStream fs = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
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

                    //var relatedTiles = GetPointTiles(lon, lat, new Tile[] { tiles[0], tiles[1] }, tilesWithMissingPoints);
                    var relatedTiles = GetPointTiles(lon, lat, tiles, tilesWithMissingPoints);
                    if (relatedTiles == null) continue;
                    for (int i = 0; i < relatedTiles.Length; i++)
                    {
                        MoveTilePoint(lon, lat, height, relatedTiles[i]);
                    }
                }
            }
        }

        static Mesh CreateTile(Transform patch, int longitude, int latitude)
        {
            // Create mesh
            var mesh = CreateTileMesh(longitude + "_" + latitude);

            // Create object
            var tileObject = GameObject.Instantiate<GameObject>(TileObjectPrefab, patch);
            tileObject.name = longitude + "_" + latitude;
            tileObject.GetComponent<MeshFilter>().sharedMesh = mesh;

            // Position relative to centre tile
            var posX = longitude - CenterTileLon;
            var posZ = latitude - CenterTileLat;
            tileObject.transform.position = new Vector3(Mathf.Round(posX), 0, Mathf.Round(posZ));

            return mesh;
        }

        static Mesh CreateTileMesh(string name)
        {
            var path = "Assets/Resources/" + name + ".asset";

            // Return mesh asset if it already exists
            var existingMesh = AssetDatabase.LoadAssetAtPath(path, typeof(Mesh)) as Mesh;
            if (existingMesh != null)
            {
                return existingMesh;
            }

            // Create new mesh asset
            var newMesh = new Mesh();
            newMesh.name = name;

            var hRes = TileResolution;
            var vRes = TileResolution;

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
                    vertices[id] = new Vector3(hPos, 0, vPos);
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

            AssetDatabase.CreateAsset(newMesh, path);
            AssetDatabase.Refresh();

            return newMesh;
        }

        static Tile[] GetTilesWithMissingPoints(int longitude, int latitude)
        {
            List<Tile> tilesWithMissingPoints = new List<Tile>();

            // Get column of tiles with missing points
            var leftTileLon = longitude - TileCoordinateStep;

            for (int vt = 0; vt < TilesPerPatch; vt++)
            {
                var leftTileLat = latitude + vt * TileCoordinateStep;
                
                var mesh = Resources.Load<Mesh>(leftTileLon + "_" + leftTileLat);
                if (mesh == null) continue;

                var tile = new Tile()
                {
                    Lon = leftTileLon,
                    Lat = leftTileLat,
                    Mesh = mesh
                };
                tilesWithMissingPoints.Add(tile);
            }

            // Get row of tiles with missing points
            var bottomTileLat = latitude - TileCoordinateStep;

            for (int ht = 0; ht < TilesPerPatch; ht++)
            {
                var bottomTileLon = longitude + ht * TileCoordinateStep;

                var mesh = Resources.Load<Mesh>(bottomTileLon + "_" + bottomTileLat);
                if (mesh == null) continue;

                var tile = new Tile()
                {
                    Lon = bottomTileLon,
                    Lat = bottomTileLat,
                    Mesh = mesh
                };
                tilesWithMissingPoints.Add(tile);
            }

            return tilesWithMissingPoints.ToArray();
        }

        static Tile[] GetPointTiles(int pointLon, int pointLat, Tile[] tiles, Tile[] tilesWithMissingPoints)
        {
            var relatedTiles = tiles.Where(tile => tile.Lon <= pointLon && pointLon < tile.Lon + TileResolution &&
            tile.Lat <= pointLat && pointLat < tile.Lat + TileResolution).ToArray();



            return relatedTiles;
        }

        static void MoveTilePoint(int pointLon, int pointLat, float height, Tile tile)
        {
            var row = pointLat - tile.Lat;
            var clm = pointLon - tile.Lon;

            var pointId = row * TileResolution + clm;

            tile.Mesh.MarkDynamic();

            var vertices = tile.Mesh.vertices;
            vertices[pointId].y = height;

            tile.Mesh.vertices = vertices;
        }
    }
}
