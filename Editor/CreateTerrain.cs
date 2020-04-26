namespace Cuku.Terrain
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEditor;
    using System.IO;
    using System;
    using System.Linq;

    public static class CreateTerrain
    {
        [MenuItem("Cuku/Terrain/Create Terrain Data")]
        static void CreateTerrainData()
        {
            var filePath = Directory.GetFiles(TerrainSettings.SourceTerrainPointsPath, "*.txt")[0];

            if (string.IsNullOrEmpty(filePath))
            {
                Debug.Log("Finished creating terrain data!");
            }

            CreateTilesData(filePath);
        }

        [MenuItem("Cuku/Terrain/Create Terrain Tiles")]
        static void CreateTerrainTiles()
        {
            var terrain = new GameObject("Terrain", new Type[] { typeof(TerrainGroup) });
            var tilePrefab = Resources.Load<GameObject>("TerrainTile");

            var terrainsData = Resources.LoadAll<TerrainData>(TerrainSettings.TerrainDataPath);

            var terrainGroup = terrain.GetComponent<TerrainGroup>();
            terrainGroup.GroupID = 0;

            for (int i = 0; i < terrainsData.Length; i++)
            {
                Vector2Int posXZ = terrainsData[i].name.GetTilePosition();

                var tile = GameObject.Instantiate(tilePrefab, terrain.transform);
                tile.name = terrainsData[i].name;
                tile.transform.position = new Vector3(posXZ[0], 0, posXZ[1]);

                var tileTerrain = tile.GetComponent<Terrain>();
                tileTerrain.groupingID = terrainGroup.GroupID;
                tileTerrain.terrainData = terrainsData[i];
                tile.GetComponent<TerrainCollider>().terrainData = terrainsData[i];
            }
        }

        static void CreateTilesData(string filePath)
        {
            Vector2Int patchLonLat = filePath.GetLonLat();
            List<TerrainSettings.Tile> tiles = patchLonLat.GetRelatedTilesData();

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
                var tile = tiles[i];
                var terrainData = CreateTerrainData(tile.Heights);
                AssetDatabase.CreateAsset(terrainData, tile.Id.GetTerrainDataName());
                AssetDatabase.Refresh();

                tile.Heights = null;
                terrainData = null;
            }

            tiles = null;

            var completedPath = Path.Combine(TerrainSettings.CompletedTerrainPointsPath, Path.GetFileName(filePath));
            File.Move(filePath, completedPath);

            CreateTerrainData();
        }

        static List<TerrainSettings.Tile> GetRelatedTilesData(this Vector2Int patchLonLat)
        {
            var tiles = new List<TerrainSettings.Tile>();
            Vector2Int tileXZ = patchLonLat.GetTileXZIdFromLonLat();

            for (int x = tileXZ[0] - 1; x < tileXZ[0]+ 2; x++)
            {
                for (int z = tileXZ[1] - 1; z < tileXZ[1] + 2; z++)
                {
                    Vector2Int xz = new Vector2Int();
                    var tileId = xz.GetTileIdFromXZ();

                    var bounds = new int[4];
                    bounds[0] = TerrainSettings.CenterLonLat[0] + x * (TerrainSettings.HeightmapResolution - 1);
                    bounds[1] = bounds[0] + (TerrainSettings.HeightmapResolution - 1);
                    bounds[2] = TerrainSettings.CenterLonLat[1] + z * (TerrainSettings.HeightmapResolution - 1);
                    bounds[3] = bounds[2] + (TerrainSettings.HeightmapResolution - 1);

                    // Add new tile if is current or next tile
                    var isNewTile = x - tileXZ[0] >= 0 && z - tileXZ[1] >= 0;
                    AddRelatedTile(tiles, tileId, bounds, isNewTile);
                }
            }

            return tiles;
        }

        static void AddRelatedTile(List<TerrainSettings.Tile> tiles, string tileId, int[] bounds, bool canAddNew)
        {
            var terrainData = Resources.Load<TerrainData>(TerrainSettings.TerrainDataPath + tileId);
            if (terrainData != null)
            {
                var heights = DenormalizeHeights(terrainData.GetHeights(0, 0, TerrainSettings.HeightmapResolution, TerrainSettings.HeightmapResolution), terrainData.size.y);
                tiles.Add(new TerrainSettings.Tile
                {
                    Id = tileId,
                    Heights = heights,
                    Bounds = bounds
                });
                return;
            }

            // Cannot add new tile in case is previous tile
            if (!canAddNew) return;

            tiles.Add(new TerrainSettings.Tile
            {
                Id = tileId,
                Heights = new float[TerrainSettings.HeightmapResolution, TerrainSettings.HeightmapResolution],
                Bounds = bounds
            });
            return;
        }

        static void MoveTilePoint(int pointLon, int pointLat, float height, List<TerrainSettings.Tile> tiles)
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
