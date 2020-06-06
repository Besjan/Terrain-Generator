namespace Cuku.Terrain
{
	using UnityEngine;
	using UnityEditor;
	using System.IO;
	using Cuku.Utilities;
	using System.Collections.Generic;
	using System.Linq;
	using System;
	using Sirenix.OdinInspector.Editor;
	using Sirenix.Utilities.Editor;
	using Sirenix.OdinInspector;
	using Sirenix.Utilities;

	public class TerrainTextureEditor : OdinEditorWindow
	{
		#region Editor
		[MenuItem("Cuku/Terrain/Texture Editor")]
		private static void OpenWindow()
		{
			var window = GetWindow<TerrainTextureEditor>();
			window.position = GUIHelper.GetEditorWindowRect().AlignCenter(700, 700);
		}

		[PropertySpace, InlineEditor]
		public TerrainTextureConfig TextureConfig;

		[PropertySpace, InlineEditor]
		public TerrainCommonConfig CommonConfig;

		private bool IsConfigValid()
		{
			return TextureConfig != null && CommonConfig != null;
		}
		#endregion

		#region Actions
		[ShowIf("IsConfigValid"), PropertySpace(20), Button(ButtonSizes.Large)]
		static void ConvertSatelliteImages()
		{
			// Convert original images
			var images = Directory.GetFiles(TerrainUtilities.SourcePath, "*" + TerrainUtilities.SourceFormat);
			foreach (var image in images)
			{
				var texturePath = Path.Combine(TerrainUtilities.ConvertedPath, Path.GetFileNameWithoutExtension(image) + TerrainUtilities.ImageFormat);
				var commandConvert = string.Format(@"{0} {1} {2}", TerrainUtilities.ConversionCommand, image, texturePath);

				commandConvert.ExecutePowerShellCommand(true);
			}

			// Cleanup texture names
			var textures = Directory.GetFiles(TerrainUtilities.ConvertedPath, "*" + TerrainUtilities.ImageFormat);
			foreach (var texture in textures)
			{
				var newName = texture;
				foreach (var filter in TerrainUtilities.NameFilters)
				{
					newName = newName.Replace(filter, string.Empty);
				}
				File.Move(texture, newName);
			}
		}

		[ShowIf("IsConfigValid"), PropertySpace(20), Button(ButtonSizes.Large)]
		static void CombineImages()
		{
			// Group textures in tiles as terrain data
			var tilePositions = new List<Vector2Int>();
			var terrainsData = Resources.LoadAll<TerrainData>(TerrainUtilities.TerrainDataPath);

			var terrainSize = TerrainUtilities.HeightmapResolution - 1;

			foreach (TerrainData terrainData in terrainsData)
			{
				Vector2Int posXZ = terrainData.name.GetTilePosition();
				tilePositions.Add(posXZ);
			}

			var patchRatio = TerrainUtilities.PatchResolution * 1.0f / TerrainUtilities.PatchSize;

			var tiles = new Dictionary<string, Dictionary<string, Vector2Int>>();
			var images = Directory.GetFiles(TerrainUtilities.ConvertedPath, "*" + TerrainUtilities.ImageFormat);

			foreach (var image in images)
			{
				Vector2Int lonLat = image.GetLonLat();
				var minImageX = lonLat[0] - TerrainUtilities.CenterLonLat[0];
				var maxImageX = minImageX + TerrainUtilities.PatchSize - 1;
				var minImageZ = lonLat[1] - TerrainUtilities.CenterLonLat[1];
				var maxImageZ = minImageZ + TerrainUtilities.PatchSize - 1;

				var points = new Vector2[] {
					new Vector2(minImageX, minImageZ),
					new Vector2(maxImageX, minImageZ),
					new Vector2(maxImageX, maxImageZ),
					new Vector2(minImageX, maxImageZ),
				};

				for (int i = 0; i < tilePositions.Count; i++)
				{
					Vector2Int tilePosition = tilePositions[i];
					var minTileX = tilePosition[0];
					var maxTileX = minTileX + terrainSize;
					var minTileZ = tilePosition[1];
					var maxTileZ = minTileZ + terrainSize;

					var tilePoints = new Vector2[] {
						new Vector2(minTileX,minTileZ),
						new Vector2(maxTileX,minTileZ),
						new Vector2(maxTileX,maxTileZ),
						new Vector2(minTileX,maxTileZ),
					};

					if (!points.Any(point => point.IsInside(tilePoints))) continue;

					var imagePosition = new Vector2Int();
					imagePosition[0] = Convert.ToInt32((minImageX - minTileX) * patchRatio);
					imagePosition[1] = Convert.ToInt32(TerrainUtilities.TileResolution - (maxImageZ + 1 - minTileZ) * patchRatio);

					var tileId = tilePosition.GetTileIdFromPosition();

					if (!tiles.ContainsKey(tileId))
					{
						var imageAndPoints = new Dictionary<string, Vector2Int>();
						imageAndPoints.Add(image, imagePosition);
						tiles.Add(tileId, imageAndPoints);
						continue;
					}

					if (!tiles[tileId].ContainsKey(image))
					{
						tiles[tileId].Add(image, imagePosition);
					}
				}
			}

			// Combine images to tiles
			foreach (var tile in tiles)
			{
				var commandComposite = string.Format("convert -size {0}x{0} canvas:white ", TerrainUtilities.TileResolution);

				foreach (var image in tile.Value)
				{
					commandComposite += string.Format("{0} -geometry {1}{2}{3}{4} -composite ",
						image.Key, image.Value[0] >= 0 ? "+" : "", image.Value[0], image.Value[1] >= 0 ? "+" : "", image.Value[1]);
				}

				var tileName = Path.Combine(TerrainUtilities.CombinedPath, tile.Key + TerrainUtilities.ImageFormat);
				commandComposite += tileName;

				commandComposite.DoMagick(true);
			}
		}

		[ShowIf("IsConfigValid"), PropertySpace(20), Button(ButtonSizes.Large)]
		private static void ResizeImages()
		{
			var commandResize = string.Format("mogrify -resize {0}x{0} {1}\\*{2}",
				TerrainUtilities.TextureResolution, TerrainUtilities.CombinedPath, TerrainUtilities.ImageFormat);

			commandResize.DoMagick();
		}

		/// <summary>
		/// Saturate biome mask images to better emphasize colors
		/// </summary>
		[ShowIf("IsConfigValid"), PropertySpace(20), Button(ButtonSizes.Large)]
		static void SaturateImages()
		{
			var images = Directory.GetFiles(TerrainUtilities.CombinedPath, "*" + TerrainUtilities.ImageFormat);

			foreach (var image in images)
			{
				var modulatedImagePath = Path.Combine(TerrainUtilities.SaturatedPath, Path.GetFileName(image));

				var commandModulate = string.Format("convert {0} -modulate 100,200 {1}", image, modulatedImagePath);

				commandModulate.DoMagick(true);
			}
		}

		#endregion
	}
}
