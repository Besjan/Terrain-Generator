namespace Cuku.Terrain
{
	using Cuku.ScriptableObject;
	using Sirenix.OdinInspector;
	using System.IO;

	public class TerrainCommonConfig : SerializedScriptableObject
    {
        [PropertySpace(20), Title("Id"), InfoBox("Used to separate tile names.", InfoMessageType.None)]
        public string IdSeparator = "_";

        [PropertySpace(20), Title("City"), Required, InlineEditor]
        public StringSO CityName;

        [PropertySpace, InlineEditor, Required]
        [InfoBox("City Center in Universal Transverse Mercator coordinates.", InfoMessageType.None)]
        public Vector2IntSO CenterUtm;

        [PropertySpace, InlineEditor, Required]
        [InfoBox("Minimum and maximum terrain heights.", InfoMessageType.None)]
        public Vector2SO TerrainHeightRange;


        [PropertySpace(20), Title("Heightmap"), FolderPath(RequireExistingPath = true)]
        [InfoBox("Folder path where terrain data is stored, must be inside Resources folder.", InfoMessageType.None)]
        public string TerrainDataPath;

        [PropertySpace, ValueDropdown("heighmapResolutions")]
        public int HeightmapResolution = 4097;


        private int[] heighmapResolutions = { 33, 65, 129, 257, 513, 1025, 2049, 4097 };


        public string TerrainDataFolder()
		{
            return new DirectoryInfo(TerrainDataPath).Name;
        }
    }
}
