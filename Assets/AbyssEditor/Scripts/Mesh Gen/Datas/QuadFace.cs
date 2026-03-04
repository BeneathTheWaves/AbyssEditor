using System;
using AbyssEditor.Scripts.VoxelTech.VoxelGrids;
using UnityEngine;

namespace AbyssEditor.Scripts.Mesh_Gen.Datas
{
    public struct QuadFace : IComparable {
        public Vector3 a, b, c, d;
        public Vector3 surfaceIntersection;//Assigned in compute shader, DON'T BELIEVE RIDERS LIES
        public int type;
        public Vector3 this [int i] {
            get {
                switch (i) {
                    case 0:
                        return a;
                    case 1:
                        return b;
                    case 2:
                        return c;
                    default:
                        return d;
                }
            }
        }

        ///<summary>
        /// Returns stride of one face for Compute shaders
        ///</summary>
        public static int GetStride() {
            return sizeof(float) * 3 * 5 + sizeof(int);
        }

        public bool IsPartOfMesh() {
            return IsVertexPartOfMesh(0) && IsVertexPartOfMesh(1) && IsVertexPartOfMesh(2) && IsVertexPartOfMesh(3);
        }
        bool IsVertexPartOfMesh(int i) {
            return this[i].x > 0 && this[i].y > 0 && this[i].z > 0 && this[i].x < VoxelGrid.GRID_FULL_SIDE && this[i].y < VoxelGrid.GRID_FULL_SIDE && this[i].z < VoxelGrid.GRID_FULL_SIDE;
        }

        public int CompareTo(object obj)
        {
            if (obj is QuadFace) {
                if (type == ((QuadFace)obj).type) return 0;
                return type > ((QuadFace)obj).type ? -1 : 1;
            }
            return -1;
        }
    }
}
