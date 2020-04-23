namespace Cuku.Terrain
{
    using UnityEngine;
    using UnityEditor;
    using System.IO;
    using Cuku.Utilities;
    using System.Collections.Generic;
    using System.Linq;

    public static class CreateTextures
    {
        [MenuItem("Cuku/Terrain/Resize Source Images")]
        static void ResizeSourceImages()
        {
            var sourcePath = TerrainSettings.SourceImagesPath;
            var texturesPath = TerrainSettings.TexturesPath;

            var command = string.Format("mogrify -resize {0}x{0} -quality 100 -path {1}  {2}\\*.{3}",
                TerrainSettings.PatchResolution, texturesPath, sourcePath, TerrainSettings.TextureFormat);

            command.DoMagick();
        }

        [MenuItem("Cuku/Terrain/Combine Satellite Images")]
        static void CombineSatelliteImages()
        {
            var tilePositions = new List<int[]>();
            var terrainsData = Resources.LoadAll<TerrainData>(TerrainSettings.TerrainDataPath);
            foreach (TerrainData terrainData in terrainsData)
            {
                int posX = 0;
                int posZ = 0;
                terrainData.name.GetTilePosition(ref posX, ref posZ);
                tilePositions.Add(new int[] { posX, posZ });
            }

            var tiles = new Dictionary<string, Dictionary<string, int[]>>();
            var texturesPath = TerrainSettings.TexturesPath;
            var images = Directory.GetFiles(texturesPath, "*." + TerrainSettings.TextureFormat);
            var terrainSize = TerrainSettings.HeightmapResolution - 1;
            foreach (var image in images)
            {
                int lon = 0;
                int lat = 0;
                image.GetLonLat(ref lon, ref lat);

                var minImageX = lon - TerrainSettings.CenterTileLon;
                var maxImageX = minImageX + TerrainSettings.PatchSize - 1;
                var minImageZ = lat - TerrainSettings.CenterTileLat;
                var maxImageZ = minImageZ + TerrainSettings.PatchSize - 1;

                var points = new Vector2[] {
                    new Vector2(minImageX, minImageZ),
                    new Vector2(maxImageX, minImageZ),
                    new Vector2(maxImageX, maxImageZ),
                    new Vector2(minImageX, maxImageZ),
                };

                foreach (var tilePosition in tilePositions)
                {
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

                    var tileId = tilePosition.GetTileIdFromPosition();

                    var imagePosition = new int[2];
                    imagePosition[0] = (minImageX - minTileX) * TerrainSettings.PatchResolution / TerrainSettings.PatchSize;
                    imagePosition[1] = TerrainSettings.TextureResolution - (maxImageZ + 1 - minTileZ) * TerrainSettings.PatchResolution / TerrainSettings.PatchSize;

                    if (!tiles.ContainsKey(tileId))
                    {
                        var imageAndPoints = new Dictionary<string, int[]>();
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

            foreach (var tile in tiles)
            {
                var command = string.Format("convert -size {0}x{0} xc:skyblue ", TerrainSettings.TextureResolution);

                foreach (var image in tile.Value)
                {
                    command += string.Format("-draw \"image over {0},{1} 0,0 '{2}'\" ", image.Value[0], image.Value[1], image.Key);
                }

                var tileName = Path.Combine(TerrainSettings.TexturesPath, tile.Key + "." + TerrainSettings.TextureFormat);
                command += tileName;

                if (tile.Key != "0_0") continue;

                command.DoMagick();
            }
        }
    }
}
