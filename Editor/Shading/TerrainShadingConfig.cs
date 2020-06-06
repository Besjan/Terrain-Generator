namespace Cuku.Terrain
{
    using Sirenix.OdinInspector;

    public class TerrainShadingConfig : SerializedScriptableObject
    {
        [PropertySpace, Title("MicroSplat"), AssetsOnly]
        public UnityEngine.Material MicroSplatMaterial;

        [PropertySpace(20), Title("Paths"), FolderPath(AbsolutePath = true, RequireExistingPath = true)]
        [InfoBox("Must be in a Resources folder.")]
        public string TintTexturesPath;

        [PropertySpace, FolderPath(AbsolutePath = true, RequireExistingPath = true)]
        [InfoBox("Must be in a Resources folder.")]
        public string BiomeMapsPath;
    }
}
