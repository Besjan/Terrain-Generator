namespace Cuku.Terrain
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEditor;
    using System.IO;
    using System;
    using System.Linq;
    using MessagePack;
    using UnityEngine.ProBuilder;
    using UnityEditor.ProBuilder;
    using Geo;
	using Dreamteck.Splines;

	public static class EditTerrain
    {
        const string boundaryDataPath = "Assets/StreamingAssets/Data/boundary.cuk";
        const float boundaryHeight = 100.0f;

        const float smoothDistance = 10.0f;
        const float smoothAmount = 5.0f;
        const int neighbourStep = 1;

        [MenuItem("Cuku/Terrain/Create Boundary")]
        static void CreateBoundary()
        {
            var boundaryPoints = GetBoundaryPoints();

            // Create vertices
            var wallVertices = new List<Vector3>();

            for (int p = 0; p < boundaryPoints.Length - 1; p++)
            {
                var point0 = boundaryPoints[p];
                var point1 = boundaryPoints[p + 1];

                wallVertices.Add(point0);
                wallVertices.Add(point1);
                wallVertices.Add(new Vector3(point0.x, point0.y + boundaryHeight, point0.z));
                wallVertices.Add(new Vector3(point1.x, point1.y + boundaryHeight, point1.z));
            }

            var sharedVertices = new List<SharedVertex>();

            // Create faces
            var faces = new List<Face>();
            for (int f = 0; f < wallVertices.Count - 3; f += 4)
            {
                var faceVertices = new int[] { f, f + 1, f + 2, f + 1, f + 3, f + 2 };
                faces.Add(new Face(faceVertices));
            }

            var wall = ProBuilderMesh.Create(wallVertices, faces);

            Normals.CalculateNormals(wall);
            Normals.CalculateTangents(wall);
            Smoothing.ApplySmoothingGroups(wall, faces, 30);
            wall.ToMesh();
            wall.Refresh();
            EditorMeshUtility.Optimize(wall);

            wall.SetMaterial(faces, Resources.Load<Material>("2Sided"));

            wall.gameObject.name = wall.name = "Boundary";
            wall.transform.SetParent(null, true);
        }

        [MenuItem("Cuku/Terrain/Lower Outer Terrains")]
        static void LowerOuterTerrain()
        {
            var boundaryPoints = GetBoundaryPoints();
            var boundaryPoints2D = boundaryPoints.ProjectToXZPlane();
            var boundaryCurve = boundaryPoints.GetBoundaryCurve();

            var terrains = boundaryPoints.GetHitTerrains();
            var ts = new Terrain[] { terrains[1] };

            foreach (var terrain in ts)
            {
                var terrainSize = terrain.terrainData.size;
                var heightmapResolution = terrain.terrainData.heightmapResolution;
                var terrainPosition = terrain.GetPosition();
                var heights = terrain.terrainData.GetHeights(0, 0, heightmapResolution, heightmapResolution);
                int rows = heights.GetUpperBound(0);
                int columns = heights.GetUpperBound(1);

                for (int i = 0; i < rows; i++)
                {
                    for (int j = 0; j < columns; j++)
                    {
                        var height = heights[i, j];

                        float posX = terrainSize.x * i / heightmapResolution + terrainPosition.x;
                        float posY = terrainSize.y * height;
                        float posZ = terrainSize.z * j / heightmapResolution + terrainPosition.z;

                        var pointPosition2D = new Vector2(posX, posZ);

                        if (pointPosition2D.IsInside(boundaryPoints2D)) continue;

                        var pointPosition3D = new Vector3(posX, posY, posZ);
                        var positionOnCurve3D = boundaryCurve.EvaluatePosition(boundaryCurve.Project(pointPosition3D));
                        var positionOnCurve2D = new Vector2(positionOnCurve3D.x, positionOnCurve3D.z);

                        var distanceFromCurve = Vector2.Distance(pointPosition2D, positionOnCurve2D);

                        if (distanceFromCurve > smoothDistance)
                        {
                            heights[j, i] = 0;
                            continue;
                        }

                        var smoothAmountMeter = distanceFromCurve * smoothAmount / smoothDistance;
                        smoothAmountMeter = Mathf.Clamp(smoothAmountMeter, 0, smoothAmount);

                        var smoothedHeight = (positionOnCurve3D.y - smoothAmountMeter) / terrainSize.y;
                        smoothedHeight = Mathf.Clamp(smoothedHeight, 0, 1);

                        heights[j, i] = smoothedHeight;
                    }
                }

                terrain.terrainData.SetHeights(0, 0, heights);
            }
        }

        //[MenuItem("Cuku/Terrain/Lower Outer Terrains")]
        static void LowerOuterTerrain1()
        {
            var boundaryPoints = GetBoundaryPoints();
            var terrains = boundaryPoints.GetHitTerrains();

            var lowerOuterTerrain = GameObject.FindObjectOfType<LowerOuterTerrain>();
            lowerOuterTerrain.Apply(boundaryPoints, terrains.ToArray());
        }

        [MenuItem("Cuku/Terrain/Smooth Outer Terrains")]
        static void SmoothOuterTerrain()
        {
            var boundaryPoints = GetBoundaryPoints();
            var terrains = boundaryPoints.GetHitTerrains();
            var ts = new Terrain[] { terrains[0] };

            foreach (var terrain in ts)
            {
                var terrainSize = terrain.terrainData.size;

                var heights = terrain.terrainData.GetHeights(0, 0, (int)terrainSize.x + 1, (int)terrainSize.z + 1);
                int rows = heights.GetUpperBound(0);
                int columns = heights.GetUpperBound(1);

                for (int iterations = 0; iterations < smoothDistance; iterations++)
                {
                    var smoothAmountMeter = smoothAmount * ((iterations + 1) * 3 / smoothDistance);

                    int[,] alreadySmoothed = new int[rows, columns];

                    for (int row = 0; row < rows; row++)
                    {
                        for (int column = 0; column < columns; column++)
                        {
                            var heightPercent = heights[row, column];
                            if (heightPercent != 0) continue;

                            // Calculate average height

                            var minRow = UnityEngine.ProBuilder.Math.Clamp(row - neighbourStep, 0, rows - 1);
                            var maxRow = UnityEngine.ProBuilder.Math.Clamp(row + neighbourStep, 0, rows - 1);
                            var minColumn = UnityEngine.ProBuilder.Math.Clamp(column - neighbourStep, 0, columns - 1);
                            var maxColumn = UnityEngine.ProBuilder.Math.Clamp(column + neighbourStep, 0, columns - 1);

                            var neighbourHeights = new List<float>();

                            for (int r = minRow; r <= maxRow; r++)
                            {
                                for (int c = minColumn; c <= maxColumn; c++)
                                {
                                    if (alreadySmoothed[r, c] == 1) continue;

                                    var height = heights[r, c];
                                    if (height != 0)
                                    {
                                        neighbourHeights.Add(height);
                                    }
                                }
                            }

                            if (neighbourHeights.Count() == 0) continue;

                            float heightSum = 0;
                            foreach (var height in neighbourHeights)
                            {
                                heightSum += height;
                            }

                            var averageHeight = heightSum / neighbourHeights.Count();
                            var averageHeightMeter = terrainSize.y * averageHeight;

                            if (averageHeightMeter <= 0) continue;

                            var smoothedHeight = (averageHeightMeter - smoothAmountMeter) / terrainSize.y;

                            heights[row, column] = smoothedHeight;

                            alreadySmoothed[row, column] = 1;
                        }
                    }
                }

                terrain.terrainData.SetHeights(0, 0, heights);

                return;
            }
        }

        #region Points
        static Vector3[] GetBoundaryPoints()
        {
            var bytes = File.ReadAllBytes(boundaryDataPath);
            var boundaryData = MessagePackSerializer.Deserialize<Feature>(bytes);

            var members = boundaryData.Relations[0].Members;

            var boundaryPoints = new List<Vector3>();
            for (int m = 0; m < members.Length; m++)
            {
                var line = boundaryData.Lines.FirstOrDefault(l => l.Id == members[m].Id);
                var points = line.Points.GetPointsWorldPositions();

                // Reverse line points to match previous line's direction
                if (boundaryPoints.Count != 0 && boundaryPoints.Last() != points[0])
                {
                    points = points.Reverse().ToArray();
                }

                boundaryPoints.AddRange(points);
            }

            boundaryPoints.Add(boundaryPoints[0]);
            boundaryPoints.Reverse(); // Normals face outside
            boundaryPoints = boundaryPoints.AddTileIntersectionPoints();

            return boundaryPoints.ToArray();
        }

        static Spline GetBoundaryCurve(this Vector3[] boundaryPoints)
        {
            SplinePoint[] points = new SplinePoint[boundaryPoints.Length];
            for (int i = 0; i < points.Length; i++)
            {
                points[i] = new SplinePoint(boundaryPoints[i]);
            }

            Spline boundaryCurve = new Spline(Spline.Type.Linear);
            boundaryCurve.points = points;
            boundaryCurve.Close();

            return boundaryCurve;
        }

        static Vector3[] GetPointsWorldPositions(this Point[] points)
        {
            var positions = new Vector3[points.Length];
            for (int p = 0; p < points.Length; p++)
            {
                positions[p] = new Vector3((float)points[p].X, 0, (float)points[p].Y);
                positions[p].y = positions[p].GetHitTerrainHeight();
            }
            return positions;
        }

        static List<Vector3> AddTileIntersectionPoints(this List<Vector3> points)
        {
            var allPoints = new List<Vector3>();
            allPoints.Add(points[0]);

            for (int i = 1; i < points.Count; i++)
            {
                var point1 = points[i - 1];
                var point2 = points[i];

                if (point1.GetHitTerrainName() != point2.GetHitTerrainName())
                {
                    allPoints.Add(GetTileIntersectionPoint(point1, point2));
                }

                allPoints.Add(point2);
            }

            return allPoints;
        }

        static Vector3 GetTileIntersectionPoint(Vector3 point1, Vector3 point2)
        {
            var terrain = point1.GetTerrainRaycastHit().transform.GetComponent<Terrain>();
            var terrainAnglePoints = terrain.GetTerrainAnglePoints();

            var A1 = new Vector2(point1.x, point1.z);
            var A2 = new Vector2(point2.x, point2.z);

            bool found;
            Vector2[] intersections = new Vector2[4];
            intersections[0] = GetLinesIntersectionPoint(A1, A2, terrainAnglePoints[0], terrainAnglePoints[1], out found);
            intersections[1] = GetLinesIntersectionPoint(A1, A2, terrainAnglePoints[0], terrainAnglePoints[2], out found);
            intersections[2] = GetLinesIntersectionPoint(A1, A2, terrainAnglePoints[1], terrainAnglePoints[3], out found);
            intersections[3] = GetLinesIntersectionPoint(A1, A2, terrainAnglePoints[2], terrainAnglePoints[3], out found);

            var closest = 0;
            for (int i = 1; i < intersections.Length; i++)
            {
                if (Vector3.Distance(A1, intersections[i]) < Vector3.Distance(A1, intersections[closest]))
                {
                    closest = i;
                }
            }

            return intersections[closest].GetHitTerrainPosition();
        }

        /// <summary>
        /// Gets the coordinates of the intersection point of two lines.
        /// </summary>
        /// <param name="A1">A point on the first line.</param>
        /// <param name="A2">Another point on the first line.</param>
        /// <param name="B1">A point on the second line.</param>
        /// <param name="B2">Another point on the second line.</param>
        /// <param name="found">Is set to false of there are no solution. true otherwise.</param>
        /// <returns>The intersection point coordinates. Returns Vector2.zero if there is no solution.</returns>
        static Vector2 GetLinesIntersectionPoint(Vector2 A1, Vector2 A2, Vector2 B1, Vector2 B2, out bool found)
        {
            float tmp = (B2.x - B1.x) * (A2.y - A1.y) - (B2.y - B1.y) * (A2.x - A1.x);

            if (tmp == 0)
            {
                // No solution!
                found = false;
                return Vector2.zero;
            }

            float mu = ((A1.x - B1.x) * (A2.y - A1.y) - (A1.y - B1.y) * (A2.x - A1.x)) / tmp;

            found = true;

            var intersectionPoint = new Vector2(
                B1.x + (B2.x - B1.x) * mu,
                B1.y + (B2.y - B1.y) * mu
            );

            return intersectionPoint;
        }

        static Vector2[] GetTerrainAnglePoints(this Terrain terrain)
        {
            var tileResolution = terrain.terrainData.heightmapResolution;

            var tp1 = new Vector2(terrain.GetPosition().x, terrain.GetPosition().z);
            var tp2 = new Vector2(tp1.x + tileResolution, tp1.y);
            var tp3 = new Vector2(tp1.x, tp1.y + tileResolution);
            var tp4 = new Vector2(tp1.x + tileResolution, tp1.y + tileResolution);

            return new Vector2[] { tp1, tp2, tp3, tp4 };
        }

        static bool IsInside(this Vector2 point, Vector2[] points)
        {
            var j = points.Length - 1;
            var inside = false;
            for (int i = 0; i < points.Length; j = i++)
            {
                var pi = points[i];
                var pj = points[j];
                if (((pi.y <= point.y && point.y < pj.y) || (pj.y <= point.y && point.y < pi.y)) &&
                    (point.x < (pj.x - pi.x) * (point.y - pi.y) / (pj.y - pi.y) + pi.x))
                    inside = !inside;
            }
            return inside;
        }

        static Vector2[] ProjectToXZPlane(this Vector3[] points3D)
        {
            var points = new Vector2[points3D.Length];
            for (int i = 0; i < points.Length; i++)
            {
                points[i] = new Vector2(points3D[i].x, points3D[i].z);
            }
            return points;
        }
        #endregion

        #region Terrain
        static Terrain[] GetHitTerrains(this Vector3[] boundaryPoints)
        {
            var terrains = new List<Terrain>();
            for (int i = 0; i < boundaryPoints.Count(); i++)
            {
                var terrain = boundaryPoints[i].GetHitTerrain();
                if (terrains.Contains(terrain)) continue;
                terrains.Add(terrain);
            }

            return terrains.ToArray();
        }

        static Terrain GetHitTerrain(this Vector3 position)
        {
            return position.GetTerrainRaycastHit().transform.GetComponent<Terrain>();
        }

        static string GetHitTerrainName(this Vector3 position)
        {
            return position.GetTerrainRaycastHit().transform.name;
        }

        static Vector3 GetHitTerrainPosition(this Vector2 position)
        {
            Vector3 hitPosition = new Vector3(position.x, 0, position.y);
            hitPosition.y = hitPosition.GetTerrainRaycastHit().point.y;

            //var c = GameObject.CreatePrimitive(PrimitiveType.Cube);            
            //c.transform.position = hitPosition;

            return hitPosition;
        }

        static float GetHitTerrainHeight(this Vector3 position)
        {
            return position.GetTerrainRaycastHit().point.y;
        }

        static RaycastHit GetTerrainRaycastHit(this Vector3 origin)
        {
            origin.y = 10000;

            Ray ray = new Ray(origin, Vector3.down);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 100000))
            {
                if (hit.transform.GetComponent<Terrain>())
                {
                    return hit;
                }
            }

            Debug.LogError("No Terrain was Hit!" + origin);

            return hit;
        } 
        #endregion
    }
}
