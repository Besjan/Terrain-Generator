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

        static Transform terrain;

        static float centreTileLon = 392000;
        static float centreTileLat = 5820000;
        #endregion

        [MenuItem("Cuku/Generate Terrain From DGM (1m grid)")]
        static void GenerateTerrainFromDGM1System()
        {
            terrain = new GameObject("Terrain DGM (1m grid)").transform;
            var filePaths = Directory.GetFiles(dataPath, "*.txt");
            var pointPrefab = Resources.Load<GameObject>("Point");

            for (int fp = 0; fp < filePaths.Length; fp++)
            {
                GeneratePoints(filePaths[fp], pointPrefab);
            }
        }

        static void GeneratePoints(string filePath, GameObject pointPrefab)
        {
            // Create patch game object
            var patch = new GameObject(Path.GetFileNameWithoutExtension(filePath)).transform;
            patch.SetParent(terrain);

            using (FileStream fs = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (BufferedStream bs = new BufferedStream(fs))
            using (StreamReader sr = new StreamReader(bs))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    var pointString = line.Split(new char[] { ' ' });
                    var point = new Vector3(
                        float.Parse(pointString[0]),
                        float.Parse(pointString[1]),
                        float.Parse(pointString[2]));

                    point.x -= centreTileLon;
                    point.z -= centreTileLat;

                    GameObject.Instantiate<GameObject>(pointPrefab, point, Quaternion.identity, patch);
                }
            }
        }
    }
}
