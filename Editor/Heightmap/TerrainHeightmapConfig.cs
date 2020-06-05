namespace Cuku.Terrain
{
	using Cuku.ScriptableObject;
	using Sirenix.OdinInspector;

    public class TerrainHeightmapConfig : SerializedScriptableObject
    {
        [PropertySpace, Title("Point Data"), VerticalGroup("Point Data"), FolderPath(AbsolutePath = true, RequireExistingPath = true)]
        [InfoBox("Folder path where original LiDAR points are stored in .txt format.", InfoMessageType.None)]
        public string TerrainPointsPath;

        [PropertySpace, VerticalGroup("Point Data")]
        [InfoBox("Point data patch size in meters.", InfoMessageType.None)]
        public int PatchSize = 2000;


		[PropertySpace(20), Title("Terrain Data"), VerticalGroup("Terrain Data"), FolderPath]
		[InfoBox("Folder path where terrain data is stored, must be inside Resources folder.", InfoMessageType.None)]
        public string TerrainDataPath;

        [PropertySpace, VerticalGroup("Terrain Data"), FolderPath(AbsolutePath = true, RequireExistingPath = true)]
        [InfoBox("Folder path where heightmaps are stored and retrieved from.", InfoMessageType.None)]
        public string HeightmapsPath;

        // TODO: use it
        [PropertySpace(20), Title("Terrain Tiles"), AssetsOnly, VerticalGroup("Terrain Tiles")]
        public UnityEngine.GameObject TilePrefab;

        [PropertySpace, VerticalGroup("Terrain Tiles")]
        [InfoBox("Values higher than 1000 don't make much difference.", InfoMessageType.None)]
        public float TileStitchPrecision = 1000f;
    }
}
