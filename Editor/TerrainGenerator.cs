namespace Cuku
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEditor;
    using System.IO;
    using System;
    using System.Linq;
    using System.Runtime.Serialization.Formatters.Binary;

    public class TerrainGenerator
    {
        #region Properties
        static string DataPath = "Assets/StreamingAssets/Test";
        static string TilesPath = "Assets/StreamingAssets/Tiles";

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
            public float MaxHeight;
            public float[,] Heights;
        }
        #endregion

        static string[] Initialize(string terrainName)
        {
            Terrain = new GameObject(terrainName).transform;

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

            // Get or create tiles data files
            var firstTileX = (patchLon - CenterTileLon) / TileResolution;
            var firstTileY = (patchLat - CenterTileLat) / TileResolution;
            var firstTileId = string.Format("{0}_{1}", firstTileX, firstTileY);

            var secondTileX = Convert.ToInt32((patchLon - CenterTileLon + Mathf.Sign(firstTileX) * PatchResolution) / TileResolution);
            var secondTileY = Convert.ToInt32((patchLat - CenterTileLat + Mathf.Sign(firstTileX) * PatchResolution) / TileResolution);
            var secondTileId = string.Format("{0}_{1}", secondTileX, secondTileY);

            Debug.Log(firstTileId);
            Debug.Log(secondTileId);

            var tilesData = new List<TileData>();
            GetOrCreateTileData(tilesData, firstTileId);
            if (secondTileId != firstTileId) GetOrCreateTileData(tilesData, secondTileId);

            Debug.Log(tilesData.Count);
            Debug.Log("-----------------");



            SaveTilesData(tilesData);

            return;

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

        static void GetOrCreateTileData(List<TileData> tiles, string tileId)
        {
            var path = Directory.GetFiles(TilesPath, "*.dat").FirstOrDefault(tp => tp.Contains(tileId));
            if (File.Exists(path))
            {
                BinaryFormatter bf = new BinaryFormatter();
                FileStream file = File.Open(path, FileMode.Open);
                var heights = (float[,])bf.Deserialize(file);
                file.Close();

                var maxHeight = float.Parse(Path.GetFileNameWithoutExtension(path).Split('_').Last());
                tiles.Add(new TileData
                {
                    Id = tileId,
                    MaxHeight = maxHeight,
                    Heights = heights
                });
                return;
            }

            tiles.Add(new TileData
            {
                Id = tileId,
                MaxHeight = 0,
                Heights = new float[TileResolution, TileResolution]
            });
            return;
        }

        static void SaveTilesData(List<TileData> tiles)
        {
            foreach (var tile in tiles)
            {
                BinaryFormatter bf = new BinaryFormatter();
                var path = Path.Combine(TilesPath, string.Format("{0}_{1}.dat", tile.Id, tile.MaxHeight));
                FileStream file = File.Create(path);
                bf.Serialize(file, tile.Heights);
                file.Close();
            }
        }

        /// <summary>
        /// Creates terrain data from heights.
        /// </summary>
        /// <param name="heightPercents">Terrain height percentages ranging from 0 to 1.</param>
        /// <param name="maxHeight">The maximum height of the terrain, corresponding to a height percentage of 1.</param>
        /// <param name="heightSampleDistance">The horizontal/vertical distance between height samples.</param>
        /// <returns>A TerrainData instance.</returns>
        static TerrainData CreateTerrainData(float[,] heightPercents, float maxHeight, float heightSampleDistance)
        {
            Debug.Assert((heightPercents.GetLength(0) == heightPercents.GetLength(1)) && (maxHeight >= 0) && (heightSampleDistance >= 0));

            // Create the TerrainData.
            var terrainData = new TerrainData();
            terrainData.heightmapResolution = heightPercents.GetLength(0);

            var terrainWidth = (terrainData.heightmapResolution - 1) * heightSampleDistance;

            // If maxHeight is 0, leave all the heights in terrainData at 0 and make the vertical size of the terrain 1 to ensure valid AABBs.
            if (Mathf.Approximately(maxHeight, 0))
            {
                terrainData.size = new Vector3(terrainWidth, 1, terrainWidth);
            }
            else
            {
                terrainData.size = new Vector3(terrainWidth, maxHeight, terrainWidth);
                terrainData.SetHeights(0, 0, heightPercents);
            }

            return terrainData;
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
