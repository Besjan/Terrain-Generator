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
        const float boundaryResolution = 1.5f;

        const int curveOffset = 2;

        const float smoothDistance = 10.0f;
        const float smoothAmount = 5.0f;


        [MenuItem("Cuku/Terrain/Create Boundary")]
        static void CreateBoundary()
        {
            var boundaryPoints = GetBoundaryPoints();

            boundaryPoints.CreateWall("Boundary");
        }

        [MenuItem("Cuku/Terrain/Lower Outer Terrains")]
        static void LowerOuterTerrain()
        {
            var boundaryPoints = GetBoundaryPoints();
            var boundaryPoints2D = boundaryPoints.ProjectToXZPlane();

            var hitTerrains = boundaryPoints.GetHitTerrainsAndBoundaryPoints();

            foreach (var keyPair in hitTerrains)
            {
                var terrain = keyPair.Key;
                var boundaryCurve = keyPair.Value.GetCurve();

                keyPair.Value.CreateWall(terrain.name);
                return;

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

                return;
            }
        }

        static Dictionary<Terrain, Vector3[]> GetHitTerrainsAndBoundaryPoints(this Vector3[] boundaryPoints)
        {
            Dictionary<Terrain, int[]> terrainLimitsIds = new Dictionary<Terrain, int[]>();
            for (int i = 0; i < boundaryPoints.Length - 1; i++)
            {
                var terrain = boundaryPoints[i].GetHitTerrain();
                if (!terrainLimitsIds.ContainsKey(terrain))
                {
                    terrainLimitsIds.Add(terrain, new int[] { i, i });
                    continue;
                };

                var limits = terrainLimitsIds[terrain];
                limits[0] = Mathf.Min(limits[0], i);
                limits[1] = Mathf.Max(limits[1], i);

                terrainLimitsIds[terrain] = limits;
            }

            Dictionary<Terrain, Vector3[]> terrains = new Dictionary<Terrain, Vector3[]>();
            foreach (var terrain in terrainLimitsIds)
            {
                var limits = terrain.Value;
                var startId = limits[0] - curveOffset;
                var endId = limits[1] + curveOffset;

                var points = new List<Vector3>();
                if (limits[0] == 0) points.AddRange(boundaryPoints.Skip(boundaryPoints.Count() - curveOffset));
                points.AddRange(boundaryPoints.Skip(startId).Take(endId - startId));

                terrains.Add(terrain.Key, points.ToArray().IncreaseResolution());
            }

            return terrains;
        }

        private static void CreateWall(this Vector3[] boundaryPoints, string name)
        {
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

            wall.gameObject.name = wall.name = name;
            wall.transform.SetParent(null, true);
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

            boundaryPoints.Reverse(); // Normals face outside

            return boundaryPoints.AddTileIntersectionPoints();
        }

        static Spline GetCurve(this Vector3[] points)
        {
            SplinePoint[] splinePoints = new SplinePoint[points.Length];
            for (int i = 0; i < splinePoints.Length; i++)
            {
                splinePoints[i] = new SplinePoint(points[i]);
            }

            Spline curve = new Spline(Spline.Type.Linear);
            curve.points = splinePoints;
            curve.Close();

            return curve;
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

        static Vector3[] IncreaseResolution(this Vector3[] points)
        {
            var highRestPoints = new List<Vector3>();

            for (int i = 0; i < points.Length - 1; i++)
            {
                var point1 = new Vector2(points[i].x, points[i].z);
                var point2 = new Vector2(points[i + 1].x, points[i + 1].z);
                var distance = Vector2.Distance(point1, point2);

                highRestPoints.Add(points[i]);

                if (distance < boundaryResolution) continue;

                var step = boundaryResolution / distance;
                var t = step;
                while (t < 1)
                {
                    var point = Vector2.Lerp(point1, point2, t).GetHitTerrainPosition();
                    highRestPoints.Add(point);
                    t += step;
                }
            }
            highRestPoints.Add(points.Last());

            return highRestPoints.ToArray();
        }

        static Vector3[] AddTileIntersectionPoints(this List<Vector3> points)
        {
            var allPoints = new List<Vector3>();
            var intersectionPointIds = new List<int>();
            for (int i = 1; i < points.Count; i++)
            {
                var point1 = points[i - 1];
                var point2 = points[i];

                if (point1.GetHitTerrainName() != point2.GetHitTerrainName())
                {
                    var intersectionPoint = GetTileIntersectionPoint(point1, point2);
                    allPoints.Add(intersectionPoint);
                    intersectionPointIds.Add(allPoints.Count - 1);
                }

                allPoints.Add(point2);
            }

            // Shift points to match the start of a tile
            var shiftedPoints = new Vector3[allPoints.Count + 1];
            for (int i = 0; i < intersectionPointIds.Count; i++)
            {
                if (allPoints[intersectionPointIds[i]].GetHitTerrainName() == allPoints[intersectionPointIds[i + 1]].GetHitTerrainName()) continue;

                var startId = intersectionPointIds[i + 1];

                for (int j = startId; j < allPoints.Count; j++)
                {
                    shiftedPoints[j - startId] = allPoints[j];
                }
                for (int j = 0; j < startId; j++)
                {
                    shiftedPoints[(allPoints.Count - startId) + j] = allPoints[j];
                }
                shiftedPoints[shiftedPoints.Length - 1] = shiftedPoints[0];

                break;
            }
    
            return shiftedPoints;
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
