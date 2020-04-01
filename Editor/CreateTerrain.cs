namespace Cuku.Terrain
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEditor;
    using System.IO;
    using System;
    using System.Linq;

    public class CreateTerrain
    {
        #region Properties
        static string SourceDataPath = "Assets/StreamingAssets/Data";
        static string CompletedDataPath = "Assets/StreamingAssets/Completed";
        static string TerrainDataPath = "TerrainData/";

        static int TileResolution = 4097;

        static int CenterTileLon = 392000;
        static int CenterTileLat = 5820000;

        struct Tile
        {
            public string Id;
            public float[,] Heights;
            public int[] Bounds;
        }
        #endregion

        [MenuItem("Cuku/Terrain/Generate Terrain Data")]
        static void CreateTerrainData()
        {
            var filePath = Directory.GetFiles(SourceDataPath, "*.txt")[0];

            if (string.IsNullOrEmpty(filePath))
            {
                Debug.Log("Finished!");
            }

            CreateTilesData(filePath);
        }

        [MenuItem("Cuku/Terrain/Generate Terrain Tiles")]
        static void CreateTerrainTiles()
        {
            var terrain = new GameObject("Berlin Terrain", new Type[] { typeof(TerrainGroup) });
            var tilePrefab = Resources.Load<GameObject>("TerrainTile");

            var terrainsData = Resources.LoadAll<TerrainData>(TerrainDataPath);

            var terrainGroup = terrain.GetComponent<TerrainGroup>();
            terrainGroup.GroupID = 0;

            for (int i = 0; i < terrainsData.Length; i++)
            {
                var id = terrainsData[i].name.Split('_');

                var posX = Convert.ToInt32(id[0]) * (TileResolution - 1);
                var posZ = Convert.ToInt32(id[1]) * (TileResolution - 1);

                var tile = GameObject.Instantiate(tilePrefab, terrain.transform);
                tile.name = string.Format("Terrain_({0}, 0.0, {1})", posX, posZ);
                tile.transform.position = new Vector3(posX, 0, posZ);

                var tileTerrain = tile.GetComponent<Terrain>();
                tileTerrain.groupingID = terrainGroup.GroupID;
                tileTerrain.terrainData = terrainsData[i];
                tile.GetComponent<TerrainCollider>().terrainData = terrainsData[i];
            }
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

            CreateTerrainData();
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
                    //var tileId = string.Format("Terrain_({0}.0, 0.0, {1}.0)_", x * TileResolution, z * TileResolution);

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
    }
}
