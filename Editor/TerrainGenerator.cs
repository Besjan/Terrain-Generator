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

        static Transform tileContainer;

        static Material tileMaterial;

        static Transform[] tiles;
        static string[] tileNames;

        static float tileSize = 2000;

        static int centreTileLon = 392;
        static int centreTileLat = 5820;
        #endregion

        static string[] Initialize(string terrainName)
        {
            tileContainer = new GameObject(terrainName).transform;

            tileMaterial = new Material(Shader.Find("Diffuse"));

            tileNames = Directory.GetFiles(dataPath, "*.txt");

            tiles = new Transform[tileNames.Length];

            return tileNames;
        }

        [MenuItem("Cuku/Generate Terrain From DGM (1m grid)")]
        static void GenerateTerrainFromDGM1System()
        {
            Initialize("Terrain GDM (1m grid)");

            for (int i = 0; i < tileNames.Length; i++)
            {
                tiles[i] = CreateTile(Path.GetFileNameWithoutExtension(tileNames[i]));
            }
        }

        static Transform CreateTile(string tileName)
        {
            var tile = new GameObject(tileName, new System.Type[] { typeof(MeshFilter), typeof(MeshRenderer) }).transform;
            tile.SetParent(tileContainer);

            tile.GetComponent<MeshRenderer>().material = tileMaterial;

            var mesh = new Mesh();
            tile.GetComponent<MeshFilter>().mesh = mesh;

            var vertices = new Vector3[4]
            {
            new Vector3(0, 0, 0),
            new Vector3(tileSize, 0, 0),
            new Vector3(0, 0, tileSize),
            new Vector3(tileSize, 0, tileSize)
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

            PositionTile(tile, tileName);

            return tile;
        }

        static void PositionTile(Transform tile, string tileName)
        {
            var coordinates = tileName.Split(new char[] { '_' });
            var lon = System.Convert.ToInt32(coordinates[0]);
            var lat = System.Convert.ToInt32(coordinates[1]);

            tile.position = new Vector3(lon - centreTileLon, 0, lat - centreTileLat) * 1000; // 1000m
        }
    }
}