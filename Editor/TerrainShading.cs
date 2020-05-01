namespace Cuku.Terrain
{
    using UnityEngine;
    using UnityEditor;
    using System.IO;
    using System.Linq;

    public static class TerrainShading
    {
        [MenuItem("Cuku/Terrain/Shading/Convert To MicroSplat Terrain")]
        static void ApplyMicroSplatMaterial()
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

        [MenuItem("Cuku/Terrain/Shading/Override Tint Texture")]
        static void OverrideTintTexture()
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
    }
}
