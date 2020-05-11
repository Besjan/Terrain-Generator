namespace Cuku.Terrain
{
    using UnityEngine;
    using UnityEditor;
    using System.IO;
    using Cuku.Utilities;
    using System.Collections.Generic;
    using System.Linq;
    using System;
    using Sirenix.Utilities;

    public static class TerrainTextureProcessing
    {
        [MenuItem("Cuku/Terrain/Texture/Convert Satellite Images")]
        static void ConvertSatelliteImages()
        {
            // Convert original images
            var images = Directory.GetFiles(TerrainSettings.SourcePath, "*" + TerrainSettings.SourceFormat);
            foreach (var image in images)
            {
                var texturePath = Path.Combine(TerrainSettings.ConvertedPath, Path.GetFileNameWithoutExtension(image) + TerrainSettings.ConvertedFormat);
                var convert = string.Format(@"{0} {1} {2}", TerrainSettings.ConversionCommand, image, texturePath);
                Debug.Log(convert);
                return;
                convert.ExecutePowerShellCommand(true);
            }

            // Delete source images
            Directory.Delete(TerrainSettings.SourcePath, true);

            // Cleanup texture names
            var textures = Directory.GetFiles(TerrainSettings.ConvertedPath, "*" + TerrainSettings.ConvertedFormat);
            foreach (var texture in textures)
            {
                var newName = texture;
                foreach (var filter in TerrainSettings.NameFilters)
                {
                    newName = newName.Replace(filter, string.Empty);
                }
                File.Move(texture, newName);
            }

            // Cleanup other files
            var metaFiles = Directory.GetFiles(TerrainSettings.ConvertedPath)
                .Where(file => !file.EndsWith(TerrainSettings.ConvertedFormat))
                .ForEach(file => File.Delete(file));
        }

        [MenuItem("Cuku/Terrain/Texture/Combine Textures")]
        static void CombineTextures()
        {
            // Group textures in tiles as terrain data
            var tilePositions = new List<Vector2Int>();
            var terrainsData = Resources.LoadAll<TerrainData>(TerrainSettings.TerrainDataPath);

            var terrainSize = TerrainSettings.HeightmapResolution - 1;

            foreach (TerrainData terrainData in terrainsData)
            {
                Vector2Int posXZ = terrainData.name.GetTilePosition();
                tilePositions.Add(posXZ);
            }

            var patchRatio = TerrainSettings.PatchResolution * 1.0f / TerrainSettings.PatchSize;

            var tiles = new Dictionary<string, Dictionary<string, Vector2Int>>();
            var texturesPath = TerrainSettings.TexturesPath;
            var images = Directory.GetFiles(texturesPath, "*" + TerrainSettings.TextureFormat);

            foreach (var image in images)
            {
                Vector2Int lonLat = image.GetLonLat();
                var minImageX = lonLat[0] - TerrainSettings.CenterLonLat[0];
                var maxImageX = minImageX + TerrainSettings.PatchSize - 1;
                var minImageZ = lonLat[1] - TerrainSettings.CenterLonLat[1];
                var maxImageZ = minImageZ + TerrainSettings.PatchSize - 1;

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
                    imagePosition[1] = Convert.ToInt32(TerrainSettings.TileResolution - (maxImageZ + 1 - minTileZ) * patchRatio);

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

            // Combine textures to tiles
            foreach (var tile in tiles)
            {
                var commandComposite = string.Format("convert -size {0}x{0} canvas:white ", TerrainSettings.TileResolution);

                foreach (var image in tile.Value)
                {
                    commandComposite += string.Format("{0} -geometry {1}{2}{3}{4} -composite ",
                        image.Key, image.Value[0] >= 0 ? "+" : "", image.Value[0], image.Value[1] >= 0 ? "+" : "", image.Value[1]);
                }

                var tileName = Path.Combine(TerrainSettings.TexturesPath, tile.Key + TerrainSettings.TextureFormat);
                commandComposite += tileName;

                commandComposite.DoMagick(true);
            }

            // Cleanup single texture files
            foreach (var image in images)
            {
                File.Delete(image);
            }

            // Resize tile textures
            var commandResize = string.Format("mogrify -resize {0}x{0} {1}\\*{2}",
                TerrainSettings.TextureResolution, TerrainSettings.TexturesPath, TerrainSettings.TextureFormat);

            commandResize.DoMagick();
        }
    }
}
