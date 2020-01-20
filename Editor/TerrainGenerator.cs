namespace Cuku
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEditor;
    using System.IO;
    using System;
    using System.Linq;

    public class TerrainGenerator
    {
        #region Properties
        static int TileResolution = 201;

        static string dataPath = "Assets/StreamingAssets/Test";

        static Transform terrain;
        static Material tileMaterial;

        static Dictionary<Coordinates, Point[]> rowPatchMissingPoints;
        static Dictionary<Coordinates, Point[]> columnPatchMissingPoints;
        static Dictionary<Coordinates, Point> anglePatchMissingPoints;

        static int patchSize = 2000; // 2km
        static int patchStep;

        static int centreTileLon = 392;
        static int centreTileLat = 5820;
        #endregion

        static string[] Initialize(string terrainName)
        {
            terrain = new GameObject(terrainName).transform;

            tileMaterial = new Material(Shader.Find("Diffuse"));

            rowPatchMissingPoints = new Dictionary<Coordinates, Point[]>();
            columnPatchMissingPoints = new Dictionary<Coordinates, Point[]>();
            anglePatchMissingPoints = new Dictionary<Coordinates, Point>();

            patchStep = patchSize / 1000;

            var dataPaths = Directory.GetFiles(dataPath, "*.txt");

            return dataPaths;
        }

        [MenuItem("Cuku/Create Tile Mesh")]
        static void CreateTileMesh()
        {
            var tileGO = new GameObject("Tile", new Type[] { typeof(MeshFilter), typeof(MeshRenderer) });

            // Apply default material
            tileGO.GetComponent<MeshRenderer>().material = tileMaterial;

            var mesh = new Mesh();
            tileGO.GetComponent<MeshFilter>().mesh = mesh;

            var hRes = TileResolution;
            var vRes = TileResolution;

            Debug.Log(hRes + " " + vRes);

            var length = vRes - 1;
            var width = hRes - 1;

            #region Vertices		
            Vector3[] vertices = new Vector3[hRes * vRes];
            Debug.Log(vertices.Length);
            // Loop columns
            for (int clm = 0; clm < hRes; clm++)
            {
                float vPos = ((float)clm / (hRes - 1)) * length;
                // Loop rows
                for (int row = 0; row < vRes; row++)
                {
                    float hPos = ((float)row / (vRes - 1)) * width;
                    var id = row + clm * vRes;
                    vertices[id] = new Vector3(hPos, 0, vPos);
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
            for (int v = 0; v < hRes; v++)
            {
                for (int u = 0; u < vRes; u++)
                {
                    uvs[u + v * vRes] = new Vector2((float)u / (vRes - 1), (float)v / (hRes - 1));
                }
            }
            #endregion

            #region Triangles
            int nbFaces = (vRes - 1) * (hRes - 1);
            int[] triangles = new int[nbFaces * 6];
            int t = 0;
            for (int face = 0; face < nbFaces; face++)
            {
                // Retrieve lower left corner from face ind
                int i = face % (vRes - 1) + (face / (hRes - 1) * vRes);

                triangles[t++] = i + vRes;
                triangles[t++] = i + 1;
                triangles[t++] = i;

                triangles[t++] = i + vRes;
                triangles[t++] = i + vRes + 1;
                triangles[t++] = i + 1;
            }
            #endregion

            mesh.vertices = vertices;
            mesh.normals = normales;
            mesh.uv = uvs;
            mesh.triangles = triangles;

            //mesh.RecalculateBounds();
            //mesh.Optimize();

            AssetDatabase.CreateAsset(mesh, "Assets/Resources/Tile.asset");
            PrefabUtility.SaveAsPrefabAsset(tileGO, "Assets/Resources/Tile.prefab");
        }

        [MenuItem("Cuku/Generate Terrain From DGM (1m grid)")]
        static void GenerateTerrainFromDGM1System()
        {
            var filePaths = Initialize("Terrain DGM (1m grid)");

            // Extract missing points from patches
            for (int fp = 0; fp < filePaths.Length; fp++)
            {
                ExtractPatchMissingPoints(filePaths[fp]);
            }

            // Split patches to tiles
            for (int fp = 0; fp < filePaths.Length; fp++)
            {
                SplitPatchToTiles(filePaths[fp]);
            }
        }

        static Coordinates GetCoordinates(string filePath)
        {
            var coordinates = Path.GetFileNameWithoutExtension(filePath).Split(new char[] { '_' });

            return new Coordinates()
            {
                Lon = float.Parse(coordinates[0]),
                Lat = float.Parse(coordinates[1])
            };
        }

        #region Tile
        static Transform CreateTile(Transform patchGO, Tile tile)
        {
            // Create game object
            var tileName = tile.Coordinates.Lon + "_" + tile.Coordinates.Lat;

            var tileGO = new GameObject(tileName, new Type[] { typeof(MeshFilter), typeof(MeshRenderer) }).transform;
            tileGO.SetParent(patchGO);

            // Apply default material
            tileGO.GetComponent<MeshRenderer>().material = tileMaterial;

            // Create mesh
            CreateTileMesh(tileGO.GetComponent<MeshFilter>(), tile);

            // Position relative to centre tile
            //var shift = patchSize / 2 - tileSize / 2;
            //var posX = (tile.Lon - centreTileLon) * 1000 - shift;
            //var posZ = (tile.Lat - centreTileLat) * 1000 - shift;
            var posX = (tile.Coordinates.Lon - centreTileLon) * 1000;
            var posZ = (tile.Coordinates.Lat - centreTileLat) * 1000;
            tileGO.position = new Vector3(Mathf.Round(posX), 0, Mathf.Round(posZ));

            return tileGO;
        }

        static void SplitPatchToTiles(string filePath)
        {
            var coordinates = GetCoordinates(filePath);

            var points = GetAllPoints(filePath, coordinates);

            var hasMissingRow = points.Item1[0];
            var hasMissingColumn = points.Item1[1];

            // Order points in patch rows
            var patchRowLength = patchSize + Convert.ToInt32(hasMissingColumn);

            var patchRows = points.Item2.OrderBy(point => point.X)
                .ThenBy(point => point.Y)
                .Select((x, i) => new { Index = i, Value = x })
                .GroupBy(x => x.Index / patchRowLength)
                .Select(x => x.Select(v => v.Value).ToArray())
                .ToArray();

            Debug.Log(patchRows.Length + " | " + patchRows[0].Length);

            Debug.Log(string.Format("{0} 0 > {1} | 1 > {2} | 2 > {3} | 3 > {4}", coordinates,
                patchRows.First().First().X + ", " + patchRows.First().First().Y + ", " + patchRows.First().First().Z,
                patchRows.First().Last().X + ", " + patchRows.First().Last().Y + ", " + patchRows.First().Last().Z,
                patchRows.Last().First().X + ", " + patchRows.Last().First().Y + ", " + patchRows.Last().First().Z,
                patchRows.Last().Last().X + ", " + patchRows.Last().Last().Y + ", " + patchRows.Last().Last().Z));

            // Split points in tiles
            var tiles = new List<Tile>();
            var tileCount = patchSize / TileResolution;
            var tileCoordinateStep = patchStep * 1.0f / tileCount;

            // Loop vertical tiles
            for (int vt = 0; vt < tileCount; vt++)
            {
                var tileLat = coordinates.Lat + vt * tileCoordinateStep;
                var startCLm = vt * TileResolution;

                // Loop horizontal tiles
                for (int ht = 0; ht < tileCount; ht++)
                {
                    var tileLon = coordinates.Lon + ht * tileCoordinateStep;

                    var hRes = TileResolution + 1;
                    // Tile horizontal resolution is not whole if it is last horizontal tile and patch does not have missing column.
                    if (ht == tileCount - 1 && !hasMissingColumn) hRes--;

                    // Tile vertical resolution is not whole if it is last vertical tile and patch does not have missing row.
                    var vRes = TileResolution + 1;
                    if (vt == tileCount - 1 && !hasMissingRow) vRes--;

                    // Create tile
                    var tile = new Tile()
                    {
                        Coordinates = new Coordinates()
                        {
                            Lon = tileLon,
                            Lat = tileLat
                        },
                        HorizontalResolution = hRes,
                        VerticalResolution = vRes
                    };

                    // Add heights
                    var heights = new List<float>();
                    // Loop tile columns
                    for (int clm = startCLm; clm < startCLm + vRes; clm++)
                    {
                        var startRow = ht * TileResolution;

                        // Loop tile rows
                        for (int row = startRow; row < startRow + hRes; row++)
                        {
                            try
                            {
                                heights.Add(patchRows[row][clm].Z);
                            }
                            catch (Exception)
                            {
                                Debug.Log(row + ", " + clm + " - " + ht + ", " + vt + " - " + hRes + ", " + vRes + " - " + hasMissingRow + ", " + hasMissingColumn);
                                continue;
                            }
                        }
                    }

                    tile.Heights = heights.ToArray();
                    tiles.Add(tile);
                }
            }

            // Create patch gameo object
            var patch = new GameObject(Path.GetFileNameWithoutExtension(filePath)).transform;
            patch.SetParent(terrain);

            // Create tiles
            for (int t = 0; t < tiles.Count; t++)
            {
                CreateTile(patch, tiles[t]);
            }
        }

        static void CreateTileMesh(MeshFilter filter, Tile tile)
        {
            var mesh = new Mesh();
            filter.mesh = mesh;

            var hRes = tile.HorizontalResolution;
            var vRes = tile.VerticalResolution;

            Debug.Log(hRes + " " + vRes);

            var length = vRes - 1;
            var width = hRes - 1;

            #region Vertices		
            Vector3[] vertices = new Vector3[hRes * vRes];
            Debug.Log(vertices.Length);
            // Loop columns
            for (int clm = 0; clm < hRes; clm++)
            {
                float vPos = ((float)clm / (hRes - 1)) * length;
                // Loop rows
                for (int row = 0; row < vRes; row++)
                {
                    float hPos = ((float)row / (vRes - 1)) * width;
                    var id = row + clm * vRes;
                    vertices[id] = new Vector3(hPos, tile.Heights[id], vPos);
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
            for (int v = 0; v < hRes; v++)
            {
                for (int u = 0; u < vRes; u++)
                {
                    uvs[u + v * vRes] = new Vector2((float)u / (vRes - 1), (float)v / (hRes - 1));
                }
            }
            #endregion

            #region Triangles
            int nbFaces = (vRes - 1) * (hRes - 1);
            int[] triangles = new int[nbFaces * 6];
            int t = 0;
            for (int face = 0; face < nbFaces; face++)
            {
                // Retrieve lower left corner from face ind
                int i = face % (vRes - 1) + (face / (hRes - 1) * vRes);

                triangles[t++] = i + vRes;
                triangles[t++] = i + 1;
                triangles[t++] = i;

                triangles[t++] = i + vRes;
                triangles[t++] = i + vRes + 1;
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
        #endregion

        #region Points
        /// <summary>
        /// Missing points for each tile are:
        /// first row of top tile,
        /// first column of right tile,
        /// first point of top right tile.
        /// </summary>
        /// <param name="filePath"></param>
        static void ExtractPatchMissingPoints(string filePath)
        {
            var coordinates = GetCoordinates(filePath);
            var points = GetPatchPoints(filePath);

            rowPatchMissingPoints.Add(coordinates, points.Where(p => p.Y == coordinates.Lat * 1000).ToArray());
            columnPatchMissingPoints.Add(coordinates, points.Where(p => p.X == coordinates.Lon * 1000).ToArray());
            anglePatchMissingPoints.Add(coordinates, points.FirstOrDefault(p => p.X == coordinates.Lon * 1000 && p.Y == coordinates.Lat * 1000));
        }

        static Tuple<bool[], Point[]> GetAllPoints(string filePath, Coordinates coordinates)
        {
            var allPoints = new List<Point>();

            // Get file points
            allPoints.AddRange(GetPatchPoints(filePath));

            // Get missing points from other patches
            var missingPoints = GetMissingPoints(coordinates);
            allPoints.AddRange(missingPoints.Item2);

            return new Tuple<bool[], Point[]>(missingPoints.Item1, allPoints.ToArray());
        }

        static Point[] GetPatchPoints(string filePath)
        {
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

            return points.ToArray();
        }

        static Tuple<bool[], Point[]> GetMissingPoints(Coordinates coordinates)
        {
            List<Point> points = new List<Point>();
            var missingPoints = new bool[] { false, false };

            var rowMissingPoints = rowPatchMissingPoints
                .FirstOrDefault(mp => mp.Key.Lon == coordinates.Lon && mp.Key.Lat == coordinates.Lat + patchStep);
            if (!rowMissingPoints.Equals(default(KeyValuePair<Coordinates, Point[]>)))
            {
                var rowPoints = rowMissingPoints.Value.Where(p => rowMissingPoints.Key.Lat * 1000 == p.Y);
                points.AddRange(rowPoints);
                rowPatchMissingPoints.Remove(rowMissingPoints.Key);
                missingPoints[0] = true;
                Debug.Log("Top: " + rowMissingPoints.Key.Lon + "_" + rowMissingPoints.Key.Lat + " " + points.Count());
            }

            var columnMissingPoints = columnPatchMissingPoints
                .FirstOrDefault(mp => mp.Key.Lon == coordinates.Lon + patchStep && mp.Key.Lat == coordinates.Lat);
            if (!columnMissingPoints.Equals(default(KeyValuePair<Coordinates, Point[]>)))
            {
                var columnPoints = columnMissingPoints.Value.Where(p => columnMissingPoints.Key.Lon * 1000 == p.X);
                points.AddRange(columnPoints);
                columnPatchMissingPoints.Remove(columnMissingPoints.Key);
                missingPoints[1] = true;
                Debug.Log("Right: " + columnMissingPoints.Key.Lon + "_" + columnMissingPoints.Key.Lat + " " + points.Count());
            }

            if (missingPoints.All(mp => mp == true))
            {
                var angleMissingPoint = anglePatchMissingPoints
                    .FirstOrDefault(mp => mp.Key.Lon == coordinates.Lon + patchStep && mp.Key.Lat == coordinates.Lat + patchStep);
                if (!angleMissingPoint.Equals(default(KeyValuePair<Coordinates, Point[]>)))
                {
                    var anglePoint = angleMissingPoint.Value;
                    points.Add(anglePoint);
                    anglePatchMissingPoints.Remove(angleMissingPoint.Key);
                }
                Debug.Log("TopRight: " + angleMissingPoint.Key.Lon + "_" + angleMissingPoint.Key.Lat);
            }

            Debug.Log(points.Count);
            return new Tuple<bool[], Point[]>(missingPoints, points.ToArray());
        }
        #endregion
    }

    struct Tile
    {
        public Coordinates Coordinates;
        public int HorizontalResolution;
        public int VerticalResolution;
        // Order of points is: Left -> Right and Bottom -> Up
        public float[] Heights;
    }

    struct Point
    {
        public float X;
        public float Y;
        public float Z;
    }

    struct Coordinates
    {
        public float Lon;
        public float Lat;
    }
}
