namespace Cuku.Terrain
{
    using UnityEngine;
    using UnityEditor;
    using System.IO;
    using Cuku.Utilities;
	using System.Collections.Generic;
	using System;

	public class CreateMaps
    {
        static string magick;

        static int centerTileLon;
        static int centerTileLat;

        static void Initialize()
        {
            magick = TerrainSettings.Magick;

            centerTileLon = TerrainSettings.CenterTileLon;
            centerTileLat = TerrainSettings.CenterTileLat;
        }

        [MenuItem("Cuku/Terrain/Resize Source Images")]
        static void ResizeSourceImages()
        {
            Initialize();

            var sourcePath = TerrainSettings.SourceImagesPath;
            var texturesPath = TerrainSettings.TexturesPath;

            var command = string.Format("mogrify -resize {0}x{0} -quality 100 -path {1}  {2}\\*.{3}", 
                TerrainSettings.PatchResolution, texturesPath, sourcePath, TerrainSettings.TextureFormat);

            var arguments = string.Format(@"{0} {1}", magick, command);

            Debug.Log(arguments);

            arguments.ExecutePowerShellCommand();
        }

        [MenuItem("Cuku/Terrain/Combine Satellite Images")]
        static void CombineSatelliteImages()
        {
            Initialize();

            var texturesPath = TerrainSettings.TexturesPath;

            var images = Directory.GetFiles(texturesPath, "*." + TerrainSettings.TextureFormat);

            var tiles = new Dictionary<string, List<string>>();

            foreach (var image in images)
            {
                // Get tile coordinates
                int lon = 0;
                int lat = 0;
                TerrainSettings.GetLonLat(image, ref lon, ref lat);

                var id = TerrainSettings.GetTileIdFromLonLat(lon, lat);

                if (!tiles.ContainsKey(id))
                {
                    tiles.Add(id, new List<string>());
                    continue;
                }

                // todo: include image in all intersecting terrains, using size 

                tiles[id].Add(image);
            }

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
                Debug.Log(tile.Key);
                foreach (var value in tile.Value)
                {
                    Debug.Log(value);
                }
                Debug.Log("=============");
            }
        }
    }
}
