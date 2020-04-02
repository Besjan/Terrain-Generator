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
            ResetAXModel();

            StartCoroutine(LowerOuterTerrains(boundaryPoints, terrains));
        }

        IEnumerator LowerOuterTerrains(Vector3[] boundaryPoints, Terrain[] terrains)
        {
            foreach (var terrain in terrains)
            {
                yield return StartCoroutine(Apply(boundaryPoints, terrain));
            }
        }

        IEnumerator Apply(Vector3[] boundaryPoints, Terrain terrain)
        {
            var axModel = GetComponent<AXModel>();

            // Terrain Tile

            var tile = axModel.parametricObjects.FirstOrDefault(p => p.Name == "Tile");
            var tilePosition = terrain.GetPosition() - new Vector3(5, 0, 5);
            var tileSize = terrain.terrainData.heightmapResolution + 10;

            tile.setParameterValueByName("Trans_X", tilePosition.x);
            tile.setParameterValueByName("Trans_Y", tilePosition.z);
            tile.setParameterValueByName("size", tileSize);

            axModel.autobuild();

            yield return new WaitForSeconds(1);

            // Boundary

            var boundary = axModel.parametricObjects.FirstOrDefault(p => p.Name == "Boundary");
            boundary.curve.Clear();
            foreach (var point in boundaryPoints)
            {
                boundary.curve.Add(new CurveControlPoint2D(point.x, point.z));
            }

            axModel.autobuild();

            yield return new WaitForSeconds(1);

            // Lower Terrain

            var lowerOuterTerrain = axModel.parametricObjects.FirstOrDefault(p => p.Name == "LowerOuterTerrain");
            lowerOuterTerrain.terrain = terrain;

            axModel.autobuild();

            yield return new WaitForSeconds(1);

            ResetAXModel();

            yield return new WaitForSeconds(1);
        }

        [Button]
        public void ResetAXModel()
        {
            var axModel = GetComponent<AXModel>();

            var tile = axModel.parametricObjects.FirstOrDefault(p => p.Name == "Tile");
            tile.setParameterValueByName("Trans_X", 0);
            tile.setParameterValueByName("Trans_Y", 0);
            tile.setParameterValueByName("size", 1);

            var boundary = axModel.parametricObjects.FirstOrDefault(p => p.Name == "Boundary");
            boundary.curve.Clear();

            var lowerOuterTerrain = axModel.parametricObjects.FirstOrDefault(p => p.Name == "LowerOuterTerrain");
            lowerOuterTerrain.terrain = null;
            lowerOuterTerrain.heightsOrig = null;

            axModel.autobuild();
        }
    }
}