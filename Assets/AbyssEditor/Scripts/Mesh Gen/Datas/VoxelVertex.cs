using UnityEngine;

namespace AbyssEditor.Scripts.Mesh_Gen.Datas
{
    public struct VoxelVertex {
        private readonly QuadFace[] adjFaces;
        private int adjCount;
            
        public int vertIndex;
        public bool isSet;
        public bool addedToVertexArray;

        public VoxelVertex(int adjFacesCapacity)
        {
            adjFaces = new QuadFace[adjFacesCapacity];
            adjCount = 0;
            vertIndex = 0;
            isSet = false;
            addedToVertexArray = false;
        }
            
        public Vector3 ComputePos() {

            Vector3 res = Vector3.zero;
            int count = 0;

            for (int i = 0; i < adjCount; ++i) {
                res += adjFaces[i].surfaceIntersection;
                count++;
            }

            return res / count;
        }

        public void AddNeighborFace(ref QuadFace _quadFace) {
            adjFaces[adjCount] = _quadFace;
            adjCount++;
        }

        public void ResetDataNoAlloc()
        {
            addedToVertexArray = false;
            isSet = false;
            vertIndex = 0;
            adjCount = 0;
        }
    }
}
