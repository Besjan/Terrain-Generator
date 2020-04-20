namespace Cuku.Terrain
{
    using UnityEngine;
    using UnityEditor;
    using System.IO;
    using Cuku.Utilities;

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
                TerrainSettings.TextureResolution, texturesPath, sourcePath, TerrainSettings.TextureFormat);

            var arguments = string.Format(@"{0} {1}", magick, command);

            Debug.Log(arguments);

            arguments.ExecutePowerShellCommand();
        }

        [MenuItem("Cuku/Terrain/Combine Satellite Images")]
        static void CombineSatelliteImages()
        {
            Initialize();

            var source = TerrainSettings.SourceImagesPath;
            var cropped = TerrainSettings.TexturesPath;

            var images = Directory.GetFiles(source, "*.tif");

            // Resize source images to Unity's max texture resolution

            foreach (var image in images)
            {
                var sourceImage = image;

                // Get tile coordinates
                var coordinates = TerrainSettings.GetLonLat(sourceImage);
                var lon = coordinates[0];
                var lat = coordinates[1];

                // group images by 9 per tile
                // use convert -draw to combine them

                var x = (lon - centerTileLon) / 2;
                var y = (lat - centerTileLat) / 2;

                var croppedName = string.Format("{0}_{1}.tif", x, y);

                var croppedImage = Path.Combine(cropped, croppedName);

                var command = string.Format("convert {0} -crop 1000x1000+0+0 +repage {1}", sourceImage, croppedImage);

                Debug.Log(command);

                var arguments = string.Format(@"{0} {1}", magick, command);

                arguments.ExecutePowerShellCommand();
            }
        }
    }
}
