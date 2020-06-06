namespace Cuku.Terrain
{
	using System;
	using System.IO;
	using UnityEngine;
    using Cuku.Utilities;
	using MessagePack;

	public static class TerrainUtilities
    {
		#region Properties
        public struct Tile
        {
            public string Id;
            public float[,] Heights;
            public int[] Bounds;
        }

        [MessagePackObject]
        public struct Heightmap
        {
            [Key(0)]
            public float TerrainHeight;
            [Key(1)]
            public float[,] Heights;
        }

        // Common / City

        // TODO: use it in MaxTerrainHeight and microsplat world height range
        public const float MaxTerrainHeight = 123f;

        public const int HeightmapResolution = 4097;
        #endregion

        public static Vector2Int GetLonLat(this string filePath)
        {
            var coordinates = Path.GetFileNameWithoutExtension(filePath).Split(new char[] { '_' });
            var lonLat = new Vector2Int(Convert.ToInt32(coordinates[0]),
                                        Convert.ToInt32(coordinates[1]));
            return lonLat * 1000;
        }

        public static Vector2Int GetTileXZIdFromUtm(this Vector2Int utm, Vector2Int centerUtm)
        {
            var xzId = new Vector2Int(Convert.ToInt32(Math.Floor(1f * (utm[0] - centerUtm[0]))),
                                      Convert.ToInt32(Math.Floor(1f * (utm[1] - centerUtm[1]))));
            return xzId / HeightmapResolution;
        }

        public static Vector2Int GetTileXZIdFromName(this string name)
        {
            var xzId = name.Split(new char[] { '_' });
            return new Vector2Int(Convert.ToInt32(xzId[0]), Convert.ToInt32(xzId[1]));
        }

        public static string GetTileIdFromUtm(this Vector2Int utm, Vector2Int centerUtm, string separator)
        {
            Vector2Int xz = utm.GetTileXZIdFromUtm(centerUtm);
            return GetTileIdFromXZ(xz, separator);
        }

        public static string GetTileIdFromPosition(this Vector2Int position, string separator)
        {
            Vector2Int xz = new Vector2Int(position[0], position[1]) / (HeightmapResolution - 1);
            return GetTileIdFromXZ(xz, separator);
        }

        public static string GetTileIdFromXZ(this Vector2Int xz, string separator)
        {
            return string.Format("{0}{2}{1}", xz[0], xz[1], separator);
        }

        public static Vector2Int GetTilePosition(this string id, string separator)
        {
            var idXZ = id.Split(separator.ToCharArray());
            return new Vector2Int(Convert.ToInt32(idXZ[0]), Convert.ToInt32(idXZ[1])) * (HeightmapResolution - 1);
        }

        public static void DoMagick(this string command, bool wait = false)
        {
            //var arguments = string.Format(@"{0} {1}", Magick, command);

            //arguments.ExecutePowerShellCommand(wait);
        }
    }
}