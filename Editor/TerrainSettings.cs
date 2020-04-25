namespace Cuku.Terrain
{
	using System;
	using System.IO;
	using UnityEngine;
    using Cuku.Utilities;

    public static class TerrainSettings
    {
		#region Properties
		public const string ResourcesPath = "Assets/Resources/";

        public const string SourceTerrainPointsPath = "Assets/StreamingAssets/SourceTerrainPoints";
        public const string CompletedTerrainPointsPath = "Assets/StreamingAssets/CompletedTerrainPoints";

        public const string TerrainDataPath = "TerrainData/";

        public const char IdSeparator = '_';

        public static Vector2Int CenterLonLat = new Vector2Int(392000, 5820000);

        public const int HeightmapResolution = 4097;

        public static int PatchResolution;
        public static int PatchSize = 2000;

        public static string SourceImagesPath;
        public const string ImageFormat = ".ecw";
        public const string ImageConversionCommand = "gdal_translate -of JPEG -a_srs EPSG:4258";

        public static string TexturesPath;
        //public const int TextureResolution = 2048;
        public const int TextureResolution = 16384;
        public const string TextureFormat = ".jpg";
        public static string[] TextureNameDirt = new string[] { "dop20rgb_", "_2_be_2019" };

        public static string Magick;

        public struct Tile
        {
            public string Id;
            public float[,] Heights;
            public int[] Bounds;
        }
		#endregion

		static TerrainSettings()
        {
            var dataPath = Application.dataPath.Replace(@"/", @"\");
            var projectPath = Directory.GetParent(dataPath).ToString();

            SourceImagesPath = Path.Combine(projectPath, "SourceImages");
            TexturesPath = Path.Combine(projectPath, "TerrainTextures");
            if (!Directory.Exists(TexturesPath)) Directory.CreateDirectory(TexturesPath);

            // Fit 2 images of size 2000 + 96 in 4K terrain
            PatchResolution = (TextureResolution - (HeightmapResolution - 1) % PatchSize) / ((HeightmapResolution - 1) / PatchSize);

            Magick = Path.Combine(new string[] { projectPath, "ImageMagick", "magick.exe" });
        }

        public static Vector2Int GetLonLat(this string filePath)
        {
            var coordinates = Path.GetFileNameWithoutExtension(filePath).Split(new char[] { '_' });
            var lonLat = new Vector2Int(Convert.ToInt32(coordinates[0]),
                                        Convert.ToInt32(coordinates[1]));
            return lonLat * 1000;
        }

        public static Vector2Int GetTileXZIdFromLonLat(this Vector2Int lonLat)
        {
            var xzId = new Vector2Int(Convert.ToInt32(Math.Floor(1f * (lonLat[0] - CenterLonLat[0]))),
                                      Convert.ToInt32(Math.Floor(1f * (lonLat[1] - CenterLonLat[1]))));
            return xzId / HeightmapResolution;
        }

        public static Vector2Int GetTileXZIdFromName(this string name)
        {
            var xzId = name.Split(new char[] { '_' });
            return new Vector2Int(Convert.ToInt32(xzId[0]), Convert.ToInt32(xzId[1]));
        }

        public static string GetTileIdFromLonLat(this Vector2Int lonLat)
        {
            Vector2Int xz = lonLat.GetTileXZIdFromLonLat();
            return GetTileIdFromXZ(xz);
        }

        public static string GetTileIdFromPosition(this Vector2Int position)
        {
            Vector2Int xz = new Vector2Int(position[0], position[1]) / (HeightmapResolution - 1);
            return GetTileIdFromXZ(xz);
        }

        public static string GetTileIdFromXZ(this Vector2Int xz)
        {
            return string.Format("{0}{2}{1}", xz[0], xz[1], IdSeparator);
        }

        public static Vector2Int GetTilePosition(this string id)
        {
            var idXZ = id.Split(IdSeparator);
            return new Vector2Int(Convert.ToInt32(idXZ[0]), Convert.ToInt32(idXZ[1])) * (HeightmapResolution - 1);
        }

        public static string GetTerrainDataName(this string id)
        {
            return string.Format("{0}{1}{2}.asset", ResourcesPath, TerrainDataPath, id);
        }

        public static string GetTerrainObjectName(this Vector2Int xz)
        {
            return string.Format("{0} | {1}", xz[0], xz[1]);
        }

        public static void DoMagick(this string command, bool wait = false)
        {
            var arguments = string.Format(@"{0} {1}", Magick, command);

            arguments.ExecutePowerShellCommand(wait);
        }
    }
}