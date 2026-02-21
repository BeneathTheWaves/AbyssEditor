using System;
using System.Collections.Generic;
using System.Linq;
using AbyssEditor.Scripts.VoxelTech;
using AbyssEditor.Scripts.VoxelTech.VoxelGrids;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
namespace AbyssEditor.Scripts.Mesh_Gen {
    public class MeshBuilder : MonoBehaviour {
        public static MeshBuilder builder;
        
        // Compute stuff
        [SerializeField] private ComputeShader shader;
        private ComputeBuffer voxelBuffer;
        private ComputeBuffer faceBuffer; 
        private ComputeBuffer triCountBuffer;
        
        //TODO: Setup pools so multiple mesh gens can run at once
        private uint[] packed;//Used for passing to the shader with greater efficiency
        private VoxelVertex[] verticesOfNodes;
        private readonly List<Vector3> vertices = new(10000);
        
        public void Awake() {
            builder = this;
            verticesOfNodes = new VoxelVertex[VoxelGrid.GRID_FULL_SIDE * VoxelGrid.GRID_FULL_SIDE * VoxelGrid.GRID_FULL_SIDE];
            for (int i = 0; i < verticesOfNodes.Length; i++)
            {
                verticesOfNodes[i].adjFaces = new Face[VoxelVertex.MAX_ADJACENT_FACES];
            }
        }

        void OnDestroy() {
            if (Application.isPlaying) {
                ReleaseBuffers();
            }
        }

        void CreateBuffers (Vector3Int pointCounts) {
            int numPoints = pointCounts.x * pointCounts.y * pointCounts.z;

            int numVoxels = (pointCounts.x - 1) * (pointCounts.y - 1) * (pointCounts.z - 1);
            int maxFaceCount = numVoxels * 6;

            // Always create buffers in editor (since buffers are released immediately to prevent memory leak)
            // Otherwise, only create if null or if size has changed
            bool bufferSizeChanged = false;
            if (voxelBuffer != null) bufferSizeChanged = numPoints != voxelBuffer.count;

            if (Application.isPlaying == false || (voxelBuffer == null || bufferSizeChanged)) {

                ReleaseBuffers();
                voxelBuffer = new ComputeBuffer(numPoints, sizeof(uint));
                faceBuffer = new ComputeBuffer (maxFaceCount, Face.GetStride(), ComputeBufferType.Append);
                triCountBuffer = new ComputeBuffer (1, sizeof (int), ComputeBufferType.Raw);
                packed = new uint[numPoints];
            }
        }

        void ReleaseBuffers() {
            if (faceBuffer != null) {
                faceBuffer.Release();
                faceBuffer = null;
            }

            if (voxelBuffer != null)
            {
                voxelBuffer.Release();
                voxelBuffer = null;
            }

            if (triCountBuffer != null) {
                triCountBuffer.Release();
                triCountBuffer = null;
            }
        }

        public Mesh GenerateMesh(NativeArray<byte> densityGrid, NativeArray<byte> typeGrid, Vector3Int resolution, Vector3 offset, out int[] blocktypes) {
            // Setting data inside shader
            CreateBuffers(resolution);

            int numThreads = Mathf.CeilToInt ((resolution.x) / (float) Globals.THREAD_GROUP_SIZE);
            
            int numPoints = densityGrid.Length;
            
            for (int i = 0; i < numPoints; i++) {
                packed[i] = (uint)(densityGrid[i] | (typeGrid[i] << 8));
            }
            
            voxelBuffer.SetData(packed);
            faceBuffer.SetCounterValue(0);

            int kernel = 0;
            shader.SetBuffer(kernel, "voxels", voxelBuffer);
            shader.SetBuffer (kernel, "faces", faceBuffer);

            shader.SetInt ("numPointsX", resolution.x);
            shader.SetInt ("numPointsY", resolution.y);
            shader.SetInt ("numPointsZ", resolution.z);
            shader.SetVector("meshOffset", offset);
            
            shader.Dispatch (kernel, numThreads, numThreads, numThreads);

            // Retrieving data from shader

            ComputeBuffer.CopyCount (faceBuffer, triCountBuffer, 0);
            int[] triCountArray = new int[1];
            triCountBuffer.GetData (triCountArray);
            int numFaces = triCountArray[0];

            Face[] faces = new Face[numFaces];

            faceBuffer.GetData (faces, 0, 0, numFaces);

            return MakeMeshes(faces, resolution, offset, out blocktypes);
        } 

        Mesh MakeMeshes(Face[] faces, Vector3Int resolution, Vector3 offset, out int[] blocktypes) {
            
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            // calculate vertex positions
            for (int i = 0; i < verticesOfNodes.Length; i++)
            {
                verticesOfNodes[i].ResetVertexDataNoAlloc();
            }
            vertices.Clear();

            //Get the types within the mesh
            Dictionary<int, List<Face>> submeshFaces = new Dictionary<int, List<Face>>();
            foreach (Face face in faces) {
                if (!submeshFaces.TryGetValue(face.type, out var value)) {
                    value = new List<Face>();
                    submeshFaces.Add(face.type, value);
                }
                value.Add(face);
            }

            blocktypes = submeshFaces.Keys.ToArray();

            Vector3 vertexOffsetSum = Vector3.one * -0.5f + offset;
            float scaleFactor = Mathf.Pow(2, VoxelWorld.LEVEL_OF_DETAIL);
            foreach (int blocktype in blocktypes) {
                foreach (Face meshFace in submeshFaces[blocktype]) {
                    for (int v = 0; v < 4; v++) {
                        Vector3 dcCube = meshFace[v];
                        if (dcCube.x >= 0 && dcCube.x < resolution.x && dcCube.y >= 0 && dcCube.y < resolution.y && dcCube.z >= 0 && dcCube.z < resolution.z) {
                            int i = Globals.LinearIndex((int)dcCube.x, (int)dcCube.y, (int)dcCube.z, resolution);
                            verticesOfNodes[i].AddNeighborFace(meshFace);
                            verticesOfNodes[i].isSet = true;
                            verticesOfNodes[i].addedToVertexArray = false;
                        }
                    }
                }

                for (int i = 0; i < verticesOfNodes.Length; i++) { 
                    if (verticesOfNodes[i].isSet && !verticesOfNodes[i].addedToVertexArray) {
                        verticesOfNodes[i].addedToVertexArray = true;
                        verticesOfNodes[i].vertIndex = vertices.Count;
                        vertices.Add((verticesOfNodes[i].ComputePos() + vertexOffsetSum) * scaleFactor);
                    }
                }
            }

            int submeshCount = blocktypes.Length;
            int nextStart = 0;

            Mesh mesh = new Mesh();
            mesh.subMeshCount = submeshCount;
            
            //Check if the vertices array can be reused, don't allocate a new one if so
            if (vertices.Count <= mesh.vertices.Length)
            {
                for(int i = 0; i < mesh.vertices.Length; i++)
                {
                    mesh.vertices[i] = vertices[i];
                }
                mesh.RecalculateBounds();
            }
            else
            {
                mesh.vertices = vertices.ToArray();
            }
            
            for (int k = 0; k < blocktypes.Length; k++) {
                
                int blocktype = blocktypes[k];
                int submeshStart = nextStart;
                int countIndexes = 0;
                List<int> submeshIndexes = new List<int>();
                int basevertex = 0;

                for (int i = 0; i < submeshFaces[blocktype].Count; i++) {
                    Face faceNow = submeshFaces[blocktype][i];
                    if (!faceNow.IsPartOfMesh(resolution.x)) continue;

                    int[] vertIndices = {
                        Globals.LinearIndex((int)faceNow[0].x, (int)faceNow[0].y, (int)faceNow[0].z, resolution),
                        Globals.LinearIndex((int)faceNow[1].x, (int)faceNow[1].y, (int)faceNow[1].z, resolution),
                        Globals.LinearIndex((int)faceNow[2].x, (int)faceNow[2].y, (int)faceNow[2].z, resolution),
                        Globals.LinearIndex((int)faceNow[3].x, (int)faceNow[3].y, (int)faceNow[3].z, resolution)
                    };
                    
                    // A, B, C, D
                    submeshIndexes.Add(verticesOfNodes[vertIndices[0]].vertIndex);
                    submeshIndexes.Add(verticesOfNodes[vertIndices[1]].vertIndex);
                    submeshIndexes.Add(verticesOfNodes[vertIndices[2]].vertIndex);
                    submeshIndexes.Add(verticesOfNodes[vertIndices[3]].vertIndex);
                    countIndexes += 4;

                    basevertex = Math.Min(verticesOfNodes[vertIndices[0]].vertIndex, basevertex);
                    basevertex = Math.Min(verticesOfNodes[vertIndices[1]].vertIndex, basevertex);
                    basevertex = Math.Min(verticesOfNodes[vertIndices[2]].vertIndex, basevertex);
                    basevertex = Math.Min(verticesOfNodes[vertIndices[3]].vertIndex, basevertex);
                }
                
                mesh.SetIndices(submeshIndexes.ToArray(), MeshTopology.Quads, k, false);
                mesh.SetSubMesh(k, new SubMeshDescriptor(submeshStart, countIndexes, MeshTopology.Quads));
                nextStart += countIndexes;
            }

            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            
            return mesh;
        }

        struct Face : IComparable {
            public Vector3 a, b, c, d;
            public Vector3 surfaceIntersection;
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

            public bool IsPartOfMesh(int side) {
                return IsVertexPartOfMesh(0, side) && IsVertexPartOfMesh(1, side) && IsVertexPartOfMesh(2, side) && IsVertexPartOfMesh(3, side);
            }
            bool IsVertexPartOfMesh(int i, int side) {
                return this[i].x > 0 && this[i].y > 0 && this[i].z > 0 && this[i].x < side && this[i].y < side && this[i].z < side;
            }

            public int CompareTo(object obj)
            {
                if (obj is Face) {
                    if (type == ((Face)obj).type) return 0;
                    return type > ((Face)obj).type ? -1 : 1;
                }
                return -1;
            }
        }

        struct VoxelVertex {
            public const int MAX_ADJACENT_FACES = 24;
            
            public Face[] adjFaces;
            public int adjCount;
            
            public int vertIndex;
            public bool isSet;
            public bool addedToVertexArray;
            
            public Vector3 ComputePos() {

                Vector3 res = Vector3.zero;
                int count = 0;

                for (int i = 0; i < adjCount; ++i) {
                    res += adjFaces[i].surfaceIntersection;
                    count++;
                }

                return res / count;
            }

            public void AddNeighborFace(Face _face) {
                adjFaces[adjCount] = _face;
                adjCount++;
                
            }

            public void ResetVertexDataNoAlloc()
            {
                addedToVertexArray = false;
                isSet = false;
                vertIndex = 0;
                adjCount = 0;
            }
        }
    }
}