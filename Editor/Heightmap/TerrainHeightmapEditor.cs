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
	using Sirenix.OdinInspector.Editor;
	using Sirenix.Utilities.Editor;
	using Sirenix.OdinInspector;
	using Sirenix.Utilities;

	public class TerrainHeightmapEditor : OdinEditorWindow
    {
		#region Editor
		[MenuItem("Cuku/Terrain/Heightmap Editor")]
		private static void OpenWindow()
		{
			var window = GetWindow<TerrainHeightmapEditor>();
			window.position = GUIHelper.GetEditorWindowRect().AlignCenter(700, 700);
		}

		[PropertySpace, InlineEditor]
		public TerrainHeightmapConfig HeightmapConfig;

		[PropertySpace, InlineEditor]
		public TerrainCommonConfig CommonConfig;

		private bool IsConfigValid()
		{
			return HeightmapConfig != null && CommonConfig != null;
		} 
		#endregion

		#region Actions
		[ShowIf("IsConfigValid"), PropertySpace(20), Button(ButtonSizes.Large)]
		static void CreateTerrainDataFromPoints()
		{
			var filePath = Directory.GetFiles(TerrainUtilities.TerrainPointsPath, "*.txt")[0];

			if (string.IsNullOrEmpty(filePath))
			{
				Debug.Log("Finished creating terrain data!");
			}

			CreateTilesData(filePath);
		}

		[ShowIf("IsConfigValid"), PropertySpace(20), Button(ButtonSizes.Large)]
		static void CreateTerrainDataFromHeightmap()
		{
			var files = Directory.GetFiles(TerrainUtilities.HeightmapsPath, "*.hm");

			foreach (var file in files)
			{
				var bytes = File.ReadAllBytes(file);
				var heightmap = MessagePackSerializer.Deserialize<TerrainUtilities.Heightmap>(bytes);

				var terrainData = CreateTerrainData(heightmap.Heights, normalize: false);
				terrainData.size = new Vector3(terrainData.size.x, heightmap.TerrainHeight, terrainData.size.z);

				var path = Path.GetFileNameWithoutExtension(file).GetTerrainDataPath();
				AssetDatabase.CreateAsset(terrainData, path);
				AssetDatabase.Refresh();
			}
		}

		[ShowIf("IsConfigValid"), PropertySpace(20), Button(ButtonSizes.Large)]
		static void CreateHeightmapData()
		{
			var terrainsData = Resources.LoadAll<TerrainData>(TerrainUtilities.TerrainDataPath);

			for (int i = 0; i < terrainsData.Length; i++)
			{
				var hmResolution = terrainsData[i].heightmapResolution;
				var heights = terrainsData[i].GetHeights(0, 0, hmResolution, hmResolution);

				var path = Path.Combine(TerrainUtilities.HeightmapsPath,
					terrainsData[i].name + ".hm");

				var heightmap = new TerrainUtilities.Heightmap()
				{
					TerrainHeight = terrainsData[i].size.y,
					Heights = heights
				};
				var bytes = MessagePackSerializer.Serialize(heightmap);
				File.WriteAllBytes(path, bytes);
			}
		}

		[ShowIf("IsConfigValid"), PropertySpace(20), Button(ButtonSizes.Large)]
		static void CreateTiles()
		{
			var terrain = new GameObject("Terrain", new Type[] { typeof(TerrainGroup) });
			var tilePrefab = Resources.Load<GameObject>("TerrainTile");

			var terrainsData = Resources.LoadAll<TerrainData>(TerrainUtilities.TerrainDataPath);
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

		[ShowIf("IsConfigValid"), PropertySpace(20), Button(ButtonSizes.Large)]
		static void StitchTiles()
		{
			NormalizeTilesHeights();

			RoundBorderHeights();

			EvenBorderHeights();
		}
		#endregion

		#region Create Data
		static void CreateTilesData(string filePath)
        {
            Vector2Int patchLonLat = filePath.GetLonLat();
            List<TerrainUtilities.Tile> tiles = GetRelatedTilesData(patchLonLat);

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

            var completedPath = Path.Combine(TerrainUtilities.CombinedTerrainPointsPath, Path.GetFileName(filePath));
            File.Move(filePath, completedPath);

            CreateTerrainDataFromPoints();
        }

        static List<TerrainUtilities.Tile> GetRelatedTilesData(Vector2Int patchLonLat)
        {
            var tiles = new List<TerrainUtilities.Tile>();
            Vector2Int tileXZ = patchLonLat.GetTileXZIdFromLonLat();

            for (int x = tileXZ[0] - 1; x < tileXZ[0] + 2; x++)
            {
                for (int z = tileXZ[1] - 1; z < tileXZ[1] + 2; z++)
                {
                    Vector2Int xz = new Vector2Int();
                    var tileId = xz.GetTileIdFromXZ();

                    var bounds = new int[4];
                    bounds[0] = TerrainUtilities.CenterLonLat[0] + x * (TerrainUtilities.HeightmapResolution - 1);
                    bounds[1] = bounds[0] + (TerrainUtilities.HeightmapResolution - 1);
                    bounds[2] = TerrainUtilities.CenterLonLat[1] + z * (TerrainUtilities.HeightmapResolution - 1);
                    bounds[3] = bounds[2] + (TerrainUtilities.HeightmapResolution - 1);

                    // Add new tile if is current or next tile
                    var isNewTile = x - tileXZ[0] >= 0 && z - tileXZ[1] >= 0;
                    AddRelatedTile(tiles, tileId, bounds, isNewTile);
                }
            }

            return tiles;
        }

        static void AddRelatedTile(List<TerrainUtilities.Tile> tiles, string tileId, int[] bounds, bool canAddNew)
        {
            var terrainData = Resources.Load<TerrainData>(TerrainUtilities.TerrainDataPath + tileId);
            if (terrainData != null)
            {
                var heights = DenormalizeHeights(terrainData.GetHeights(0, 0, TerrainUtilities.HeightmapResolution, TerrainUtilities.HeightmapResolution), terrainData.size.y);
                tiles.Add(new TerrainUtilities.Tile
                {
                    Id = tileId,
                    Heights = heights,
                    Bounds = bounds
                });
                return;
            }

            // Cannot add new tile in case is previous tile
            if (!canAddNew) return;

            tiles.Add(new TerrainUtilities.Tile
            {
                Id = tileId,
                Heights = new float[TerrainUtilities.HeightmapResolution, TerrainUtilities.HeightmapResolution],
                Bounds = bounds
            });
            return;
        }

        static void MoveTilePoint(int pointLon, int pointLat, float height, List<TerrainUtilities.Tile> tiles)
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

        static TerrainData CreateTerrainData(float[,] heights, float heightSampleDistance = 1, bool normalize = true)
        {
            var maxHeight = heights.Cast<float>().Max();

            if (normalize)
            {
                heights = NormalizeHeights(heights, maxHeight);
            }

            Debug.Assert((heights.GetLength(0) == heights.GetLength(1)) && (maxHeight >= 0) && (heightSampleDistance >= 0));

            var terrainData = new TerrainData();
            terrainData.heightmapResolution = heights.GetLength(0);

            var terrainWidth = (terrainData.heightmapResolution - 1) * heightSampleDistance;

            // If maxHeight is 0, leave all the heights in terrainData at 0 and make the vertical size of the terrain 1 to ensure valid AABBs
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
            float maxHeight = TerrainUtilities.MaxTerrainHeight;

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

        static void RoundBorderHeights()
        {
            var terrains = GameObject.FindObjectsOfType<Terrain>();

            var rounding = TerrainUtilities.TileStitchPrecision;

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
