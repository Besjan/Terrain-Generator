namespace Cuku.Terrain
{
    using Sirenix.OdinInspector;

    public class TerrainTextureConfig : SerializedScriptableObject
    {
        [PropertySpace, Title("Conversion"), VerticalGroup("Conversion")]
        [InfoBox("Satellite images format.", InfoMessageType.None)]
        public string SourceFormat = ".ecw";

        [PropertySpace, VerticalGroup("Conversion")]
        [InfoBox("Image format used for all processes.", InfoMessageType.None)]
        public string ImageFormat = ".tif";

        [PropertySpace, VerticalGroup("Conversion")]
        [InfoBox(@"GDAL is required. ""-of"" format should match image format above. ""-a_srs"" Target Coordinate System should match that of the source images (https://epsg.io/).", InfoMessageType.None)]
        public string ConversionCommand = "gdal_translate -of GTiff -co TARGET=0 -a_srs EPSG:25833";

        [PropertySpace, VerticalGroup("Conversion")]
        [InfoBox("Text to remove from converted image names.", InfoMessageType.None)]
        public string[] NameFilters = new string[] { "dop20_", "dop20rgb_", "_2_be_2019" };


        [PropertySpace(20), Title("Paths"), VerticalGroup("Paths"), FilePath(AbsolutePath = true, Extensions = ".exe", RequireExistingPath = true)]
        [InfoBox("Portable ImageMagic executable path.", InfoMessageType.None)]
        public string Magick;

        [PropertySpace, VerticalGroup("Paths"), FolderPath(AbsolutePath = true, RequireExistingPath = true)]
        public string SourcePath;

        [PropertySpace, VerticalGroup("Paths"), FolderPath(AbsolutePath = true, RequireExistingPath = true)]
        public string ConvertedPath;

        [PropertySpace, VerticalGroup("Paths"), FolderPath(AbsolutePath = true, RequireExistingPath = true)]
        public string CombinedPath;

        [PropertySpace, VerticalGroup("Paths"), FolderPath(AbsolutePath = true, RequireExistingPath = true)]
        public string SaturatedPath;


        [PropertySpace(20), Title("Resolution"), VerticalGroup("Resolution")]
        [InfoBox("Texture resolution. Must be Power of Two. Can be same as highest texture resolution that Unity supports.", InfoMessageType.None)]
        public int TextureResolution = 16384;

        [PropertySpace, VerticalGroup("Resolution")]
        [InfoBox("Resolution of source images.", InfoMessageType.None)]
        public int PatchResolution = 10000;
    }
}
