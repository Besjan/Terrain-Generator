namespace Cuku.Terrain
{
    using AX;
    using AXGeometry;
    using System.Linq;
    using UnityEngine;

    [RequireComponent(typeof(AXModel))]
    public class LowerOuterTerrain : MonoBehaviour
    {
        public void SetTilePosition(Vector3 position)
        {
            var axModel = GetComponent<AXModel>();
            var tile = axModel.parametricObjects.FirstOrDefault(p => p.Name == "Tile");
            tile.setParameterValueByName("Trans_X", position.x);
            tile.setParameterValueByName("Trans_Y", position.z);

            axModel.autobuild();
        }

        public void SetTileSize(int resolution)
        {
            var axModel = GetComponent<AXModel>();
            var tile = axModel.parametricObjects.FirstOrDefault(p => p.Name == "Tile");
            tile.setParameterValueByName("size", resolution);

            axModel.autobuild();
        }

        public void SetBoundaryPoints(Vector3[] points)
        {
            var axModel = GetComponent<AXModel>();
            var boundary = axModel.parametricObjects.FirstOrDefault(p => p.Name == "Boundary");

            boundary.curve.Clear();

            foreach (var point in points)
            {
                boundary.curve.Add(new CurveControlPoint2D(point.x, point.z));
            }

            axModel.autobuild();
        }

        public void SetTerrain(Terrain terrain)
        {
            var axModel = GetComponent<AXModel>();
            var lowerOuterTerrain = axModel.parametricObjects.FirstOrDefault(p => p.Name == "LowerOuterTerrain");

            lowerOuterTerrain.terrain = terrain;

            axModel.autobuild();
        }
    }
}