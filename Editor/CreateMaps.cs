namespace Cuku.Terrain
{
    using UnityEngine;
    using UnityEditor;
    using System.IO;
    using Cuku.Utilities;
    using System.Collections.Generic;
    using System.Linq;

    public static class CreateMaps
    {
        [MenuItem("Cuku/Terrain/Resize Source Images")]
        static void ResizeSourceImages()
        {
            var sourcePath = TerrainSettings.SourceImagesPath;
            var texturesPath = TerrainSettings.TexturesPath;

            var command = string.Format("mogrify -resize {0}x{0} -quality 100 -path {1}  {2}\\*.{3}",
                TerrainSettings.PatchResolution, texturesPath, sourcePath, TerrainSettings.TextureFormat);

            var arguments = string.Format(@"{0} {1}", TerrainSettings.Magick, command);

            Debug.Log(arguments);

            arguments.ExecutePowerShellCommand();
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

            var tiles = new Dictionary<string, Dictionary<string, Vector2[]>>();
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

                    //bool isInsideTile = points.Any(point => point.IsInside(tilePoints));
                    //foreach (var point in points)
                    //{
                    //    if (point.IsInside(tilePoints))
                    //    {
                    //        isInsideTile = true;
                    //        continue;
                    //    }
                    //}

                    //Debug.Log(minTileX + " | " + minImageX + " | " + maxImageX + " | " + maxTileX);
                    //Debug.Log(minTileZ + " | " + minImageZ + " | " + maxImageZ + " | " + maxTileZ);

                    var tileId = tilePosition.GetTileIdFromPosition();

                    if (!tiles.ContainsKey(tileId))
                    {
                        var imageAndPoints = new Dictionary<string, Vector2[]>();
                        imageAndPoints.Add(image, points);
                        tiles.Add(tileId, imageAndPoints);
                        continue;
                    }

                    if (!tiles[tileId].ContainsKey(image) && !tiles[tileId].ContainsValue(points))
                    {
                        tiles[tileId].Add(image, points);
                    }
                }
            }

            //return;

            //convert - size 100x100 xc:skyblue \
            //  -draw "image over  5,10 0,0 'balloon.gif'" \
            //  -draw "image over 35,30 0,0 'medical.gif'" \
            //  -draw "image over 62,50 0,0 'present.gif'" \
            //  -draw "image over 10,55 0,0 'shading.gif'" \
            //  drawn.gif

            //var command = string.Format("convert -size {0}x{0} xc:skyblue ", TerrainSettings.TextureResolution);
            //var croppedName = string.Format("{0}_{1}.tif", x, y);

            //var croppedImage = Path.Combine(texturesPath, croppedName);

            ////var command = string.Format("convert {0} -crop 1000x1000+0+0 +repage {1}", sourceImage, croppedImage);

            //Debug.Log(command);

            //var arguments = string.Format(@"{0} {1}", magick, command);

            //arguments.ExecutePowerShellCommand();

            Debug.Log(tiles.Count);
            foreach (var tile in tiles)
            {
                Debug.Log(tile.Key + " | " + tile.Value.Count);

                //continue;
                foreach (var texture in tile.Value)
                {
                    Debug.Log(texture.Key);
                }
                Debug.Log("=============");
            }
        }
    }
}
