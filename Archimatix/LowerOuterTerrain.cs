namespace Cuku.Terrain
{
    using AX;
    using AXGeometry;
	using Sirenix.OdinInspector;
	using System.Collections;
	using System.Linq;
    using UnityEngine;

    [RequireComponent(typeof(AXModel))]
    public class LowerOuterTerrain : MonoBehaviour
    {
        public void Apply(Vector3[] boundaryPoints, Terrain[] terrains)
        {
            StartCoroutine(LowerOuterTerrains(boundaryPoints, terrains));
        }

        [Button]
        public void ResetAXModel()
        {
            var axModel = GetComponent<AXModel>();

            var boundary = axModel.parametricObjects.FirstOrDefault(p => p.Name == "Boundary");
            boundary.curve.Clear();

            var lowerOuterTerrain = axModel.parametricObjects.FirstOrDefault(p => p.Name == "LowerOuterTerrain");
            lowerOuterTerrain.terrain = null;

            axModel.build();
        }

        IEnumerator LowerOuterTerrains(Vector3[] boundaryPoints, Terrain[] terrains)
        {
            foreach (var terrain in terrains)
            {
                yield return StartCoroutine(Apply(boundaryPoints, terrain));
            }

            // Reset Archimatix
            ResetAXModel();
        }

        IEnumerator Apply(Vector3[] boundaryPoints, Terrain terrain)
        {
             var axModel = GetComponent<AXModel>();

            // Terrain Tile

            var tile = axModel.parametricObjects.FirstOrDefault(p => p.Name == "Tile");
            var tilePosition = terrain.GetPosition() - new Vector3(-1, 0, -1);
            var tileSize = terrain.terrainData.heightmapResolution + 1;

            tile.setParameterValueByName("Trans_X", tilePosition.x);
            tile.setParameterValueByName("Trans_Y", tilePosition.z);
            tile.setParameterValueByName("size", tileSize);

            // Boundary

            var boundary = axModel.parametricObjects.FirstOrDefault(p => p.Name == "Boundary");
            boundary.curve.Clear();
            foreach (var point in boundaryPoints)
            {
                boundary.curve.Add(new CurveControlPoint2D(point.x, point.z));
            }

            // Lower Terrain

            var lowerOuterTerrain = axModel.parametricObjects.FirstOrDefault(p => p.Name == "LowerOuterTerrain");
            lowerOuterTerrain.terrain = terrain;

            yield return new WaitForSeconds(1);

            axModel.build();

            yield return new WaitForSeconds(1);
        }
    }
}