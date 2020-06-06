namespace Cuku.Terrain
{
    using Sirenix.OdinInspector;

    public class TerrainShadingConfig : SerializedScriptableObject
    {
        [PropertySpace, Title("Paths"), FolderPath(AbsolutePath = true, RequireExistingPath = true)]
        public string TintTexturesPath;

        [PropertySpace, FolderPath(AbsolutePath = true, RequireExistingPath = true)]
        public string BiomeMapsPath;

        [PropertySpace, FolderPath(AbsolutePath = true, RequireExistingPath = true)]
        public string MicroSplatDataPath;
    }
}
