namespace Cuku.Terrain
{
	using Sirenix.OdinInspector.Editor;
	using Sirenix.Utilities.Editor;
	using UnityEditor;
	using Sirenix.Utilities;

	public class MainEditor : OdinMenuEditorWindow
    {
        [MenuItem("Cuku/Terrain/Main Editor", priority = -10)]
        private static void OpenWindow()
        {
            var window = GetWindow<MainEditor>();
            window.position = GUIHelper.GetEditorWindowRect().AlignCenter(700, 700);
        }

        protected override OdinMenuTree BuildMenuTree()
        {
            OdinMenuTree tree = new OdinMenuTree();

			tree.Add("Heightmap Editor", new TerrainHeightmapEditor());
			tree.Add("Texture Editor", new TerrainTextureEditor());
			tree.Add("Shading Editor", new TerrainShadingEditor());

            return tree;
        }
    }
}