namespace Cuku.Terrain
{
	using Sirenix.OdinInspector;

    public class TerrainHeightmapConfig : SerializedScriptableObject
    {
        [PropertySpace, Title("Point Data"), FolderPath(AbsolutePath = true, RequireExistingPath = true)]
        [InfoBox("Folder path where original LiDAR points are stored in .txt format.", InfoMessageType.None)]
        public string TerrainPointsPath;


        [PropertySpace(20), Title("Terrain Data"), FolderPath(AbsolutePath = true, RequireExistingPath = true)]
        [InfoBox("Folder path where heightmaps are stored and retrieved from.", InfoMessageType.None)]
        public string HeightmapsPath;

        [PropertySpace, InfoBox("Format used to save and parse heightmap data.")]
        public string HeightmapFormat;


        [PropertySpace(20), Title("Terrain Tiles"), Required]
        public UnityEngine.GameObject TilePrefab;

        [PropertySpace, InfoBox("Values higher than 1000 don't make much difference.", InfoMessageType.None)]
        public float TileStitchPrecision = 1000f;
    }
}
