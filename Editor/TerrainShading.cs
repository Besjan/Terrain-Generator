namespace Cuku.Terrain
{
    using UnityEngine;
    using UnityEditor;
    using System.IO;
    using System.Linq;

    public static class TerrainShading
    {
#if __MICROSPLAT__
		[MenuItem("Cuku/Terrain/Shading/Convert To MicroSplat Terrain")]
		static void ConvertToMicroSplatTerrain()
		{
			var material = Resources.Load<Material>(Path.Combine(TerrainSettings.TerrainDataPath, TerrainSettings.MicroSplatMaterialPath));
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

#if __MICROSPLAT_GLOBALTEXTURE__
		[MenuItem("Cuku/Terrain/Shading/Apply Tint Map")]
		static void ApplyTintMap()
		{
			var textures = Resources.LoadAll<Texture2D>(TerrainSettings.TerrainTexturesPath);
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
		[MenuItem("Cuku/Terrain/Shading/Apply Biome Mask")]
		static void ApplyBiomeMask()
		{
			var textures = Resources.LoadAll<Texture2D>(TerrainSettings.TerrainBiomeMasksPath);
			var msTerrains = GameObject.FindObjectsOfType<MicroSplatTerrain>();

			msTerrains[0].transform.parent.gameObject.SetActive(false);

			for (int i = 0; i < msTerrains.Length; i++)
			{
				msTerrains[i].procBiomeMask = textures.FirstOrDefault(t => t.name == msTerrains[i].name);
			}

			msTerrains[0].transform.parent.gameObject.SetActive(true);
		}
#endif
	}
}
