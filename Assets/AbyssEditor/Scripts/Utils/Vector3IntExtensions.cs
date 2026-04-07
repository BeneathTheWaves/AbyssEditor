using System.Collections.Generic;
using UnityEngine;
namespace AbyssEditor.Scripts
{
    public static class Vector3IntExtensions
    {
        /// <summary>
        /// Iterates through all integer coordinates in the box defined by start and end (inclusive).
        /// Order is not guaranteed, just iterates through all coordinates.
        /// </summary>
        public static IEnumerable<Vector3Int> IterateTo(this Vector3Int start, Vector3Int end)
        {
            int minX = Mathf.Min(start.x, end.x);
            int maxX = Mathf.Max(start.x, end.x);
            int minY = Mathf.Min(start.y, end.y);
            int maxY = Mathf.Max(start.y, end.y);
            int minZ = Mathf.Min(start.z, end.z);
            int maxZ = Mathf.Max(start.z, end.z);

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    for (int z = minZ; z <= maxZ; z++)
                    {
                        yield return new Vector3Int(x, y, z);
                    }
                }
            }
        }
        
        /// <summary>
        /// Iterates through all integer coordinates in the box defined by start and end (inclusive).
        /// Order is not guaranteed, just iterates through all coordinates.
        /// </summary>
        public static List<Vector3Int> ListTo(this Vector3Int start, Vector3Int end)
        {
            int minX = Mathf.Min(start.x, end.x);
            int maxX = Mathf.Max(start.x, end.x);
            int minY = Mathf.Min(start.y, end.y);
            int maxY = Mathf.Max(start.y, end.y);
            int minZ = Mathf.Min(start.z, end.z);
            int maxZ = Mathf.Max(start.z, end.z);

            List<Vector3Int> list = new ();
            
            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    for (int z = minZ; z <= maxZ; z++)
                    {
                        list.Add(new Vector3Int(x, y, z));
                    }
                }
            }
            
            return list;
        }
        
        /// <summary>
        /// Returns the number of integer grid points inside the axis-aligned box
        /// defined by the start and end positions, inclusive.
        /// </summary>
        public static int GetNumberOfPointsInRegion(this Vector3Int start, Vector3Int end)
        {
            int xCount = Mathf.Abs(end.x - start.x) + 1;
            int yCount = Mathf.Abs(end.y - start.y) + 1;
            int zCount = Mathf.Abs(end.z - start.z) + 1;

            return xCount * yCount * zCount;
        }



    }
}
