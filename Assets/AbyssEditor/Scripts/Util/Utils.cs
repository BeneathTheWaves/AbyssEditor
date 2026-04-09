using UnityEngine;

namespace AbyssEditor.Scripts.Util
{
    public static class Utils
    {
        public static float SquaredDistanceToBox (Vector3 p, Vector3 boxMin, Vector3 boxMax) {
            float dx = Mathf.Max(boxMin.x - p.x, 0, p.x - boxMax.x);
            float dy = Mathf.Max(boxMin.y - p.y, 0, p.y - boxMax.y);
            float dz = Mathf.Max(boxMin.z - p.z, 0, p.z - boxMax.z);
            return dx*dx + dy*dy + dz*dz;
        }
        
        public static int LinearIndex(int x, int y, int z, int dim) {
            return x + y * dim + z * dim * dim;
        }
        public static int LinearIndex(int x, int y, int z, Vector3Int dim) {
            return x + y * dim.x + z * dim.x * dim.y;
        }

    }
}