namespace Cuku.Terrain
{
	using System;
	using System.IO;
	using UnityEngine;

	public class TerrainSettings
    {
        public const string ResourcesPath = "Assets/Resources/";

        public const string SourceTerrainPointsPath = "Assets/StreamingAssets/SourceTerrainPoints";
        public const string CompletedTerrainPointsPath = "Assets/StreamingAssets/CompletedTerrainPoints";

        public const string TerrainDataPath = "TerrainData/";

        public static string SourceImagesPath;
        public static string TexturesPath;

        public const int CenterTileLon = 392;
        public const int CenterTileLat = 5820;

        public const int HeightmapResolution = 4097;
        public const string TextureFormat = "tif";
        public static int TextureResolution;

        public const int PatchSize = 2000;
        public const int PatchResolution = 10000;

        public static string Magick;

        public const int maxTextureResolution = 16384;


        public struct Tile
        {
            public string Id;
            public float[,] Heights;
            public int[] Bounds;
        }

        static TerrainSettings()
        {
            var dataPath = Application.dataPath.Replace(@"/", @"\");
            var projectPath = Directory.GetParent(dataPath).ToString();

            SourceImagesPath = Path.Combine(projectPath, "SourceImages");
            TexturesPath = Path.Combine(projectPath, "TerrainTextures");
            if (!Directory.Exists(TexturesPath)) Directory.CreateDirectory(TexturesPath);

            // Fit 2 images of size 2000 + 96 in 4K terrain
            TextureResolution = (16384 - 96) / 2;

            Magick = Path.Combine(new string[] { projectPath, "ImageMagick", "magick.exe" });
        }

        public static int[] GetLonLat(string filePath)
        {
            var coordinates = Path.GetFileNameWithoutExtension(filePath).Split(new char[] { '_' });
            var lon = Convert.ToInt32(coordinates[0]);
            var lat = Convert.ToInt32(coordinates[1]);
            return new int[] { lon, lat };
        }

        public static string GetTerrainDataName(string id)
        {
            return string.Format("{0}{1}{2}.asset", ResourcesPath, TerrainDataPath, id);
        }

        public static string GetTerrainObjectName(int posX, int posZ)
        {
            return string.Format("{0} | {1}", posX, posZ);
        }
    }
}