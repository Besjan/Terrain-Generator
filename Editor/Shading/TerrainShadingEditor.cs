namespace Cuku.Terrain
{
    using UnityEngine;
    using UnityEditor;
    using System.IO;
    using System.Linq;
	using Sirenix.OdinInspector.Editor;
	using Sirenix.Utilities.Editor;
	using Sirenix.OdinInspector;
	using Sirenix.Utilities;

	public class TerrainShadingEditor : OdinEditorWindow
	{
		#region Editor
		[MenuItem("Cuku/Terrain/Shading Editor")]
		private static void OpenWindow()
		{
			var window = GetWindow<TerrainShadingEditor>();
			window.position = GUIHelper.GetEditorWindowRect().AlignCenter(700, 700);
		}

		[PropertySpace, InlineEditor]
		public TerrainShadingConfig ShadingConfig;

		[PropertySpace, InlineEditor]
		public TerrainCommonConfig CommonConfig;

		private bool IsConfigValid()
		{
			return ShadingConfig != null && CommonConfig != null;
		}
		#endregion

#if __MICROSPLAT__
		[ShowIf("IsConfigValid"), PropertySpace(20), Button(ButtonSizes.Large)]
		public void ApplyMicroSplatMaterial()
		{
			var material = Resources.Load<Material>(Path.Combine(CommonConfig.TerrainDataFolder(), ShadingConfig.MicroSplatDataPath));
			var terrains = GameObject.FindObjectsOfType<Terrain>();

			terrains[0].transform.parent.gameObject.SetActive(false);

			for (int i = 0; i < terrains.Length; i++)
			{
				var msTerrain = terrains[i].gameObject.AddComponent<MicroSplatTerrain>();
				msTerrain.templateMaterial = material;
			}

			terrains[0].transform.parent.gameObject.SetActive(true);
		}
#endif

		#region Actions
#if __MICROSPLAT_GLOBALTEXTURE__
		[ShowIf("IsConfigValid"), PropertySpace(20), Button(ButtonSizes.Large)]
		public void ApplyTintMap()
		{
			var textures = Resources.LoadAll<Texture2D>(ShadingConfig.TintTexturesPath);
			var msTerrains = GameObject.FindObjectsOfType<MicroSplatTerrain>();

			msTerrains[0].transform.parent.gameObject.SetActive(false);

			for (int i = 0; i < msTerrains.Length; i++)
			{
				msTerrains[i].tintMapOverride = textures.FirstOrDefault(t => t.name == msTerrains[i].name);
			}

			msTerrains[0].transform.parent.gameObject.SetActive(true);
		}
#endif

#if __MICROSPLAT_PROCTEX__
		[ShowIf("IsConfigValid"), PropertySpace(20), Button(ButtonSizes.Large)]
		public void ApplyBiomeMask()
		{
			var textures = Resources.LoadAll<Texture2D>(ShadingConfig.BiomeMapsPath);
			var msTerrains = GameObject.FindObjectsOfType<MicroSplatTerrain>();

			msTerrains[0].transform.parent.gameObject.SetActive(false);

			for (int i = 0; i < msTerrains.Length; i++)
			{
				msTerrains[i].procBiomeMask = textures.FirstOrDefault(t => t.name == msTerrains[i].name);
			}

			msTerrains[0].transform.parent.gameObject.SetActive(true);
		}
#endif 
		#endregion
	}
}
