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

        public const int CenterTileLon = 392000;
        public const int CenterTileLat = 5820000;

        public const int HeightmapResolution = 4097;

        public static string SourceImagesPath;
        public static string TexturesPath;

        public const int TextureResolution = 16384;
        public static int PatchResolution;
        public static int PatchSize = 2000;

        public const string TextureFormat = "tif";
        public const char IdSeparator = '_';

        public static string Magick;


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
            PatchResolution = (TextureResolution - (HeightmapResolution - 1) % PatchSize) / ((HeightmapResolution - 1) / PatchSize);

            Magick = Path.Combine(new string[] { projectPath, "ImageMagick", "magick.exe" });
        }

        public static void GetLonLat(string filePath, ref int lon, ref int lat)
        {
            var coordinates = Path.GetFileNameWithoutExtension(filePath).Split(new char[] { '_' });
            lon = Convert.ToInt32(coordinates[0]) * 1000;
            lat = Convert.ToInt32(coordinates[1]) * 1000;
        }

        public static void GetTileXZId(int lon, int lat, ref int tileXId, ref int tileZId)
        {
            tileXId = (int)Math.Floor(1f * (lon - CenterTileLon) / HeightmapResolution);
            tileZId = (int)Math.Floor(1f * (lat - CenterTileLat) / HeightmapResolution);
        }

        public static string GetTileIdFromLonLat(int lon, int lat)
        {
            int x = 0;
            int z = 0;
            GetTileXZId(lon, lat, ref x, ref z);
            return GetTileIdFromXZ(x, z);
        }

        public static string GetTileIdFromXZ(int x, int z)
        {
            return string.Format("{0}{2}{1}", x, z, IdSeparator);
        }

        public static void GetTilePosition(string id, ref int x, ref int z)
        {
            var xz = id.Split(IdSeparator);
            x = Convert.ToInt32(xz[0]) * (HeightmapResolution - 1);
            z = Convert.ToInt32(xz[1]) * (HeightmapResolution - 1);
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