namespace Cuku.Terrain
{
	using Cuku.ScriptableObject;
	using Sirenix.OdinInspector;

	public class TerrainCommonConfig : SerializedScriptableObject
    {
        [PropertySpace(20), VerticalGroup("Common"), InlineEditor]
        [InfoBox("City Center in Universal Transverse Mercator coordinates.", InfoMessageType.None)]
        public Vector2IntSO CenterUTM;

        // TODO: use it in MaxTerrainHeight and microsplat world height range
        [PropertySpace, VerticalGroup("Terrain Tiles"), InlineEditor]
        public Vector2IntSO TerrainHeightRange;

        [PropertySpace(), Title("Common"), VerticalGroup("Common"), ValueDropdown("heighmapResolutions")]
        public int HeightmapResolution = 4097;


        private int[] heighmapResolutions = { 33, 65, 129, 257, 513, 1025, 2049, 4097 };
    }
}
