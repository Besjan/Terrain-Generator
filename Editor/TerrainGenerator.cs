namespace Cuku
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEditor;
    using System.IO;
    using System;
    using System.Linq;

    public class TerrainGenerator
    {
        #region Properties
        static string dataPath = "Assets/StreamingAssets/Test";

        static Transform tileContainer;

        static Material tileMaterial;

        static Transform[] tiles;

        static int patchSize = 2000; // 2km

        static int tileSize = 200;
        static int tileResolution = 200;

        static int centreTileLon = 392;
        static int centreTileLat = 5820;
        #endregion

        static string[] Initialize(string terrainName)
        {
            tileContainer = new GameObject(terrainName).transform;

            tileMaterial = new Material(Shader.Find("Diffuse"));

            var dataPaths = Directory.GetFiles(dataPath, "*.txt");

            tiles = new Transform[dataPaths.Length];

            return dataPaths;
        }

        [MenuItem("Cuku/Generate Terrain From DGM (1m grid)")]
        static void GenerateTerrainFromDGM1System()
        {
            var dataPaths = Initialize("Terrain DGM (1m grid)");

            for (int dp = 0; dp < dataPaths.Length; dp++)
            {
                var tiles = SplitPatchToTiles(dataPaths[dp]);

                for (int t = 0; t < tiles.Length; t++)
                {
                    CreateTile(tiles[t]);
                }
                //tiles[i] = CreateTile(dataPaths[i]);
            }
        }

        static Tile[] SplitPatchToTiles(string filePath)
        {
            // Get coordinates
            var coordinates = Path.GetFileNameWithoutExtension(filePath).Split(new char[] { '_' });
            var lon = float.Parse(coordinates[0]);
            var lat = float.Parse(coordinates[1]);

            // Get points
            List<Point> points = new List<Point>();
            using (FileStream fs = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (BufferedStream bs = new BufferedStream(fs))
            using (StreamReader sr = new StreamReader(bs))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    var pointString = line.Split(new char[] { ' ' });
                    points.Add(new Point()
                    {
                        X = float.Parse(pointString[0]),
                        Y = float.Parse(pointString[1]),
                        Z = float.Parse(pointString[2]),
                    });
                }
            }

            if (points.Count != patchSize * patchSize)
            {
                Debug.LogWarning(coordinates + " has " + points.Count + " points!");
                return null;
            }

            // Order points
            var rows = points.OrderBy(point => point.X)
                .ThenBy(point => point.Y)
                .Select((x, i) => new { Index = i, Value = x })
                .GroupBy(x => x.Index / patchSize)
                .Select(x => x.Select(v => v.Value).ToArray())
                .ToArray();

            Debug.Log(rows.Length + " rows");

            // Split points in tiles
            var tiles = new List<Tile>();
            var tileCount = patchSize / tileResolution;
            var tileCoordinateStep = patchSize * 1.0f / (1000 * tileCount);

            for (int vt = 0; vt < tileCount; vt++)
            {
                var tileLat = lat + vt * tileCoordinateStep;
                var startRow = vt * tileResolution;

                for (int ht = 0; ht < tileCount; ht++)
                {
                    var tileLon = lon + ht * tileCoordinateStep;
                    var heights = new List<float>();

                    for (int r = startRow; r < startRow + tileResolution; r++)
                    {
                        var startPoint = ht * tileResolution;

                        for (int p = startPoint; p < startPoint + tileResolution; p++)
                        {
                            heights.Add(rows[p][r].Z);
                        }
                    }

                    var tile = new Tile()
                    {
                        Lon = tileLon,
                        Lat = tileLat,
                        Heights = heights.ToArray()
                    };

                    tiles.Add(tile);
                }
            }

            return tiles.ToArray();
        }

        static Transform CreateTile(Tile tile)
        {
            // Create game object
            var tileName = tile.Lon + "_" + tile.Lat;

            var tileObj = new GameObject(tileName, new Type[] { typeof(MeshFilter), typeof(MeshRenderer) }).transform;
            tileObj.SetParent(tileContainer);

            // Apply default material
            tileObj.GetComponent<MeshRenderer>().material = tileMaterial;

            // Create mesh
            CreateMesh(tileObj.GetComponent<MeshFilter>(), tile.Heights);

            // Position relative to centre tile
            tileObj.position = new Vector3(tile.Lon - centreTileLon, 0, tile.Lat - centreTileLat) * 1000;

            return tileObj;
        }

        static void CreateMesh(MeshFilter filter, float[] heights)
        {
            var mesh = new Mesh();
            filter.mesh = mesh;

            #region Vertices		
            Vector3[] vertices = new Vector3[tileResolution * tileResolution];
            for (int z = 0; z < tileResolution; z++)
            {
                float zPos = ((float)z / (tileResolution - 1) - 0.5f) * tileSize;
                for (int x = 0; x < tileResolution; x++)
                {
                    float xPos = ((float)x / (tileResolution - 1) - 0.5f) * tileSize;
                    var id = x + z * tileResolution;
                    vertices[id] = new Vector3(xPos, heights[id], zPos);
                }
            }
            #endregion

            #region Normals
            Vector3[] normales = new Vector3[vertices.Length];
            for (int n = 0; n < normales.Length; n++)
            {
                normales[n] = Vector3.up;
            }
            #endregion

            #region UVs		
            Vector2[] uvs = new Vector2[vertices.Length];
            for (int v = 0; v < tileResolution; v++)
            {
                for (int u = 0; u < tileResolution; u++)
                {
                    uvs[u + v * tileResolution] = new Vector2((float)u / (tileResolution - 1), (float)v / (tileResolution - 1));
                }
            }
            #endregion

            #region Triangles
            int nbFaces = (tileResolution - 1) * (tileResolution - 1);
            int[] triangles = new int[nbFaces * 6];
            int t = 0;
            for (int face = 0; face < nbFaces; face++)
            {
                // Retrieve lower left corner from face ind
                int i = face % (tileResolution - 1) + (face / (tileResolution - 1) * tileResolution);

                triangles[t++] = i + tileResolution;
                triangles[t++] = i + 1;
                triangles[t++] = i;

                triangles[t++] = i + tileResolution;
                triangles[t++] = i + tileResolution + 1;
                triangles[t++] = i + 1;
            }
            #endregion

            mesh.vertices = vertices;
            mesh.normals = normales;
            mesh.uv = uvs;
            mesh.triangles = triangles;

            //mesh.RecalculateBounds();
            //mesh.Optimize();
        }
    }

    struct Tile
    {
        public float Lon;
        public float Lat;
        // Order of points is: Left -> Right and Bottom -> Up
        public float[] Heights;
    }

    struct Point
    {
        public float X;
        public float Y;
        public float Z;
    }
}
