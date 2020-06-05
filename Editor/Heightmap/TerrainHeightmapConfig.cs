namespace Cuku.Terrain
{
	using Sirenix.OdinInspector;

    public class TerrainHeightmapConfig : SerializedScriptableObject
    {
        [PropertySpace, Title("Point Data"), VerticalGroup("Point Data"), FolderPath(AbsolutePath = true)]
        [InfoBox("Folder path where original LiDAR points are stored in .txt format.", InfoMessageType.None)]
        public string TerrainPointsPath;

        [PropertySpace, VerticalGroup("Point Data")]
        [InfoBox("Point data patch size in meters.", InfoMessageType.None)]
        public int PatchSize = 2000;


		[PropertySpace(20), Title("Terrain Data"), VerticalGroup("Terrain Data"), FolderPath]
		[InfoBox("Folder path where terrain data is stored, must be inside Resources folder.", InfoMessageType.None)]
        public string TerrainDataPath;

        [PropertySpace, VerticalGroup("Terrain Data"), ValueDropdown("heighmapResolutions")]
        public int HeightmapResolution = 4097;

        [PropertySpace, VerticalGroup("Terrain Data"), FolderPath(AbsolutePath = true)]
        [InfoBox("Folder path where heightmaps are stored and retrieved from.", InfoMessageType.None)]
        public string HeightmapsPath;

        // TODO: use it
        [PropertySpace(20), Title("Terrain Tiles"), AssetsOnly, VerticalGroup("Terrain Tiles")]
        public UnityEngine.GameObject TilePrefab;

        [PropertySpace, VerticalGroup("Terrain Tiles")]
        [InfoBox("Doesn't have to be precise.", InfoMessageType.None)]
        public const float MaxTerrainHeight = 123f;

        [PropertySpace, VerticalGroup("Terrain Tiles")]
        [InfoBox("Values higher than 1000 don't make much difference.", InfoMessageType.None)]
        public const float TileStitchPresision = 1000f;


        private int[] heighmapResolutions = { 33, 65, 129, 257, 513, 1025, 2049, 4097 };
    }
}
