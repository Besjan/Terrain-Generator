namespace Cuku.Terrain
{
    using UnityEngine;
    using UnityEditor;
    using System.IO;
    using Cuku.Utilities;

    public class CreateMaps
    {
        static string magick;

        static void Initialize()
        {
            magick = TerrainSettings.Magick;
        }

        [MenuItem("Cuku/Terrain/Combine Satellite Images")]
        static void CombineSatelliteImages()
        {
            Initialize();

            var projectPath = Directory.GetParent(Application.dataPath.Replace(@"/", @"\")).ToString();
            var path = Path.Combine(projectPath, "Images");
            var sourceImage = Path.Combine(path, "1.jpg");
            var convertedImage = Path.Combine(path, "1.png");

            var command = sourceImage + " " + convertedImage;

            var arguments = string.Format(@"{0} {1}", magick, command);

            arguments.ExecutePowerShellCommand();
        }
    }
}
