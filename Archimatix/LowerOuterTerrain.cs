namespace Cuku.Terrain
{
    using AX;
    using AXGeometry;
	using System.Collections;
	using System.Linq;
    using UnityEngine;

    [RequireComponent(typeof(AXModel))]
    public class LowerOuterTerrain : MonoBehaviour
    {
        public void Apply(Vector3 tilePosition, int tileSize, Vector3[] boundaryPoints, Terrain[] terrains)
        {
            StartCoroutine(LowerOuterTerrains(tilePosition, tileSize, boundaryPoints, terrains));
        }

        IEnumerator LowerOuterTerrains(Vector3 tilePosition, int tileSize, Vector3[] boundaryPoints, Terrain[] terrains)
        {
            foreach (var terrain in terrains)
            {
                yield return StartCoroutine(Apply(tilePosition, tileSize, boundaryPoints, terrain));
            }
        }

        IEnumerator Apply(Vector3 tilePosition, int tileSize, Vector3[] boundaryPoints, Terrain terrain)
        {
             var axModel = GetComponent<AXModel>();

            // Terrain Tile

            var tile = axModel.parametricObjects.FirstOrDefault(p => p.Name == "Tile");
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