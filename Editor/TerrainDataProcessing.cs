namespace Cuku.Terrain
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEditor;
    using System.IO;
    using System;
    using System.Linq;
	using MessagePack;
    using Cuku.Utilities;

	public static class TerrainDataProcessing
    {
        [MenuItem("Cuku/Terrain/Data/Create Terrain Data From Points")]
        static void CreateTerrainDataFromPoints()
        {
            var filePath = Directory.GetFiles(TerrainSettings.SourceTerrainPointsPath, "*.txt")[0];

            if (string.IsNullOrEmpty(filePath))
            {
                Debug.Log("Finished creating terrain data!");
            }

            CreateTilesData(filePath);
        }

        [MenuItem("Cuku/Terrain/Data/Create Terrain Data From Heightmap")]
        static void CreateTerrainDataFromHeightmap()
        {
            var files = Directory.GetFiles(TerrainSettings.HeightmapsPath, "*" + TerrainSettings.HeightmapFormat);

            foreach (var file in files)
            {
                var bytes = File.ReadAllBytes(file);
                var heights = MessagePackSerializer.Deserialize<float[,]>(bytes);

                var terrainData = CreateTerrainData(heights);
                var path = Path.GetFileNameWithoutExtension(file).GetTerrainDataPath();
                AssetDatabase.CreateAsset(terrainData, path);
                AssetDatabase.Refresh();
            }
        }

        [MenuItem("Cuku/Terrain/Data/Create Heightmap Data")]
        static void CreateHeightmapData()
        {
            var terrainsData = Resources.LoadAll<TerrainData>(TerrainSettings.TerrainDataPath);
            terrainsData = terrainsData.OrderBy(td => td.name.GetTileXZIdFromName().x)
                .ThenBy(td => td.name.GetTileXZIdFromName().y).ToArray();

            for (int i = 0; i < terrainsData.Length; i++)
            {
                var hmResolution = terrainsData[i].heightmapResolution;
                var heights = terrainsData[i].GetHeights(0, 0, hmResolution, hmResolution);

                var path = Path.Combine(TerrainSettings.HeightmapsPath, 
                    terrainsData[i].name + TerrainSettings.HeightmapFormat);

                var bytes = MessagePackSerializer.Serialize(heights);
                File.WriteAllBytes(path, bytes);

                return;
            }
        }

        [MenuItem("Cuku/Terrain/Data/Create Tiles")]
        static void CreateTiles()
        {
            var terrain = new GameObject("Terrain", new Type[] { typeof(TerrainGroup) });
            var tilePrefab = Resources.Load<GameObject>("TerrainTile");

            var terrainsData = Resources.LoadAll<TerrainData>(TerrainSettings.TerrainDataPath);
            terrainsData = terrainsData.OrderBy(td => td.name.GetTileXZIdFromName().x)
                .ThenBy(td => td.name.GetTileXZIdFromName().y).ToArray();

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

        [MenuItem("Cuku/Terrain/Data/Stitch Tiles")]
        static void StitchTiles()
        {
            NormalizeTilesHeights();

            RoundBorderHeightsPrecision();

            EvenBorderHeights();
        }

        #region Create Data
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
                AssetDatabase.CreateAsset(terrainData, tile.Id.GetTerrainDataPath());
                AssetDatabase.Refresh();

                tile.Heights = null;
                terrainData = null;
            }

            tiles = null;

            var completedPath = Path.Combine(TerrainSettings.CompletedTerrainPointsPath, Path.GetFileName(filePath));
            File.Move(filePath, completedPath);

            CreateTerrainDataFromPoints();
        }

        static List<TerrainSettings.Tile> GetRelatedTilesData(this Vector2Int patchLonLat)
        {
            var tiles = new List<TerrainSettings.Tile>();
            Vector2Int tileXZ = patchLonLat.GetTileXZIdFromLonLat();

            for (int x = tileXZ[0] - 1; x < tileXZ[0] + 2; x++)
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
        #endregion

        #region Stitch Tiles
        static void NormalizeTilesHeights()
        {
            float maxHeight = TerrainSettings.MaxTerrainHeight;

            var terrains = GameObject.FindObjectsOfType<Terrain>();

            for (int t = 0; t < terrains.Length; t++)
            {
                var terrain = terrains[t];

                var hmResolution = terrain.terrainData.heightmapResolution;
                var heights = terrain.terrainData.GetHeights(0, 0, hmResolution, hmResolution);

                var scale = terrain.terrainData.size.y / maxHeight;
                terrain.terrainData.size = new Vector3(terrain.terrainData.size.x, maxHeight, terrain.terrainData.size.z);

                for (int i = 0; i < hmResolution; i++)
                {
                    for (int j = 0; j < hmResolution; j++)
                    {
                        heights[j, i] *= scale;
                    }
                }

                terrain.terrainData.SetHeights(0, 0, heights);
            }
        }

        static void RoundBorderHeightsPrecision()
        {
            var terrains = GameObject.FindObjectsOfType<Terrain>();

            var rounding = TerrainSettings.BorderRounding;

            for (int t = 0; t < terrains.Length; t++)
            {
                var terrain = terrains[t];

                var hmResolution = terrain.terrainData.heightmapResolution;
                var heights = terrain.terrainData.GetHeights(0, 0, hmResolution, hmResolution);

                for (int i = 0; i < hmResolution; i++)
                {
                    var j = 0;
                    heights[j, i] = Mathf.Round(heights[j, i] * rounding) / rounding;

                    j = hmResolution - 1;
                    heights[j, i] = Mathf.Round(heights[j, i] * rounding) / rounding;
                }
                for (int j = 0; j < hmResolution; j++)
                {
                    var i = 0;
                    heights[j, i] = Mathf.Round(heights[j, i] * rounding) / rounding;

                    i = hmResolution - 1;
                    heights[j, i] = Mathf.Round(heights[j, i] * rounding) / rounding;
                }

                terrain.terrainData.SetHeights(0, 0, heights);
            }
        }

        static void EvenBorderHeights()
        {
            var terrains = GameObject.FindObjectsOfType<Terrain>();

            for (int t = 0; t < terrains.Length; t++)
            {
                var terrain = terrains[t];

                var position = terrain.GetPosition();
                var size = terrain.terrainData.size;
                var hmResolution = terrain.terrainData.heightmapResolution;
                var heights = terrain.terrainData.GetHeights(0, 0, hmResolution, hmResolution);

                for (int i = 0; i < hmResolution; i++)
                {
                    var j = 0;
                    heights[j, i] = GetHeight(position, size, hmResolution, i, j);

                    j = hmResolution - 1;
                    heights[j, i] = GetHeight(position, size, hmResolution, i, j);
                }
                for (int j = 0; j < hmResolution; j++)
                {
                    var i = 0;
                    heights[j, i] = GetHeight(position, size, hmResolution, i, j);

                    i = hmResolution - 1;
                    heights[j, i] = GetHeight(position, size, hmResolution, i, j);
                }

                terrain.terrainData.SetHeights(0, 0, heights);
            }
        }

        static float GetHeight(Vector3 position, Vector3 size, int hmResolution, int i, int j)
        {
            var posX = size.x * i / (hmResolution - 1) + position.x;
            var posZ = size.z * j / (hmResolution - 1) + position.z;
            var point = new Vector3(posX, 0, posZ);
            var height = point.GetHitTerrainHeight();
            return height / size.y;
        }
        #endregion
    }
}
