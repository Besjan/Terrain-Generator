namespace Cuku.Terrain
{
    using Sirenix.OdinInspector;

    public class TerrainTextureConfig : SerializedScriptableObject
    {
        [PropertySpace, Title("Conversion"), InfoBox("Satellite images format.", InfoMessageType.None)]
        public string SourceFormat = ".ecw";

        [PropertySpace, InfoBox("Image format used for all processes.", InfoMessageType.None)]
        public string ImageFormat = ".tif";

        [PropertySpace, InfoBox(@"GDAL is required. ""-of"" format should match image format above. ""-a_srs"" Target Coordinate System should match that of the source images (https://epsg.io/).", InfoMessageType.None)]
        public string ConversionCommand = "gdal_translate -of GTiff -co TARGET=0 -a_srs EPSG:25833";

        [PropertySpace, InfoBox("Text to remove from converted image names.", InfoMessageType.None)]
        public string[] NameFilters = new string[] { "dop20_", "dop20rgb_", "_2_be_2019" };


        [PropertySpace(20), Title("Paths"), FilePath(AbsolutePath = true, Extensions = ".exe", RequireExistingPath = true)]
        [InfoBox("Portable ImageMagic executable path.", InfoMessageType.None)]
        public string Magick;

        [PropertySpace, FolderPath(AbsolutePath = true, RequireExistingPath = true)]
        public string SourcePath;

        [PropertySpace, FolderPath(AbsolutePath = true, RequireExistingPath = true)]
        public string ConvertedPath;

        [PropertySpace, FolderPath(AbsolutePath = true, RequireExistingPath = true)]
        public string CombinedPath;

        [PropertySpace, FolderPath(AbsolutePath = true, RequireExistingPath = true)]
        public string SaturatedPath;


        [PropertySpace(20), Title("Resolution")]
        [InfoBox("Texture resolution. Must be Power of Two. Can be same as highest texture resolution that Unity supports.", InfoMessageType.None)]
        public int TextureResolution = 16384;

        [PropertySpace, InfoBox("Resolution of source images.", InfoMessageType.None)]
        public int PatchResolution = 10000;

        [PropertySpace, InfoBox("Point data patch size in meters.", InfoMessageType.None)]
        public int PatchSize = 2000;
    }
}
