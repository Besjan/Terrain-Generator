namespace Cuku.Terrain
{
    using Sirenix.OdinInspector;

    public class TerrainShadingConfig : SerializedScriptableObject
    {
        [PropertySpace, VerticalGroup("Paths"), FolderPath(AbsolutePath = true, RequireExistingPath = true)]
        public string TintTexturesPath;

        [PropertySpace, VerticalGroup("Paths"), FolderPath(AbsolutePath = true, RequireExistingPath = true)]
        public string BiomeMapsPath;

        [PropertySpace, VerticalGroup("Paths"), FolderPath(AbsolutePath = true, RequireExistingPath = true)]
        public string MicroSplatDataPath;
    }
}
