namespace Cuku.Terrain
{
    public class TerrainSettings
    {
        public const string ResourcesPath = "Assets/Resources/";

        public const string SourceTerrainPointsPath = "Assets/StreamingAssets/SourceTerrainPoints";
        public const string CompletedTerrainPointsPath = "Assets/StreamingAssets/CompletedTerrainPoints";

        public const string TerrainDataPath = "TerrainData/";

        public const int TileResolution = 4097;

        public const int CenterTileLon = 392000;
        public const int CenterTileLat = 5820000;

        public static string Magick;

        public struct Tile
        {
            public string Id;
            public float[,] Heights;
            public int[] Bounds;
        }

        static TerrainSettings()
        {
            var dataPath = UnityEngine.Application.dataPath.Replace(@"/", @"\");
            Magick = System.IO.Path.Combine(new string[] { dataPath, "Plugins", "ImageMagick", "magick.exe" });
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