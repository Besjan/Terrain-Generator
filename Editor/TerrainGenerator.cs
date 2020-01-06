namespace Cuku
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEditor;
    using System.IO;

    public class TerrainGenerator
    {
        #region Properties
        static string dataPath = "Assets/StreamingAssets/Data";

        static Material material;
        static Transform terrain;

        static float tileWidth = 2000;
        static float tileHeight = 2000;
        #endregion

        static string[] Initialize(string terrainName)
        {
            material = new Material(Shader.Find("Diffuse"));

            terrain = new GameObject(terrainName).transform;

            return Directory.GetFiles(dataPath, "*.txt");
        }

        [MenuItem("Cuku/Generate Terrain From DGM (1m grid)")]
        static void GenerateTerrainFromDGM1System()
        {
            var tiles = Initialize("Terrain GDM (1m grid)");

            for (int i = 0; i < tiles.Length; i++)
            {
                CreateTile(terrain, Path.GetFileNameWithoutExtension(tiles[i]));
            }
        }

        static void CreateTile(Transform parent, string name)
        {
            var tile = new GameObject(name, new System.Type[] { typeof(MeshFilter), typeof(MeshRenderer) }).transform;
            tile.SetParent(parent);

            tile.GetComponent<MeshRenderer>().material = material;

            var mesh = new Mesh();
            tile.GetComponent<MeshFilter>().mesh = mesh;

            var vertices = new Vector3[4]
            {
            new Vector3(0, 0, 0),
            new Vector3(tileWidth, 0, 0),
            new Vector3(0, 0, tileHeight),
            new Vector3(tileWidth, 0, tileHeight)
            };
            mesh.vertices = vertices;

            var tris = new int[6]
            {
            // lower left triangle
            0, 2, 1,
            // upper right triangle
            2, 3, 1
            };
            mesh.triangles = tris;

            var normals = new Vector3[4]
            {
            -Vector3.forward,
            -Vector3.forward,
            -Vector3.forward,
            -Vector3.forward
            };
            mesh.normals = normals;

            var uv = new Vector2[4]
            {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1)
            };
            mesh.uv = uv;
        }
    }
}