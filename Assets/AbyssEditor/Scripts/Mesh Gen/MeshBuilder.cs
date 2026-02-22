using System;
using System.Collections.Generic;
using System.Linq;
using AbyssEditor.Scripts.UI;
using AbyssEditor.Scripts.VoxelTech;
using AbyssEditor.Scripts.VoxelTech.VoxelGrids;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Assertions.Must;
using UnityEngine.Rendering;
namespace AbyssEditor.Scripts.Mesh_Gen {
    public class MeshBuilder : MonoBehaviour {
        public static MeshBuilder builder;

        private const int MAX_ADJACENT_FACES = 24;
        
        private const int SUBMESH_BLOCK_TYPES_CAPACITY = 20;
        private const int SUBMESH_QUAD_FACE_CAPACITY = 10000;
        
        // Compute stuff
        [SerializeField] private ComputeShader shader;
        private ComputeBuffer voxelBuffer;
        private ComputeBuffer faceBuffer; 
        private ComputeBuffer triCountBuffer;
        
        //Pooling/reusing arrays
        private uint[] packed;//Used for passing to the shader with greater efficiency
        private VoxelVertex[] verticesOfNodes;
        private readonly List<Vector3> vertices = new(10000);
        private Dictionary<int, QuadFaceGroup> submeshFaces;
        private readonly Stack<QuadFaceGroup> faceGroupPool = new();
        private readonly List<int> submeshVerts = new(10000);
        
        public void Awake() {
            builder = this;
            
            //Initialize vertices arrays, so we don't allocate in mesh building
            verticesOfNodes = new VoxelVertex[VoxelGrid.GRID_FULL_SIDE * VoxelGrid.GRID_FULL_SIDE * VoxelGrid.GRID_FULL_SIDE];
            for (int i = 0; i < verticesOfNodes.Length; i++)
            {
                verticesOfNodes[i] = new VoxelVertex(MAX_ADJACENT_FACES);
            }
            
            //Initialize SubMesh arrays, so we don't allocate in mesh building
            submeshFaces = new Dictionary<int, QuadFaceGroup>(SUBMESH_BLOCK_TYPES_CAPACITY);
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

            // Always create buffers in editor (since buffers are released immediately to MeshStreamingprevent memory leak)
            // Otherwise, only create if null or if size has changed
            bool bufferSizeChanged = false;
            if (voxelBuffer != null) bufferSizeChanged = numPoints != voxelBuffer.count;

            if (Application.isPlaying == false || (voxelBuffer == null || bufferSizeChanged)) {

                ReleaseBuffers();
                voxelBuffer = new ComputeBuffer(numPoints, sizeof(uint));
                faceBuffer = new ComputeBuffer (maxFaceCount, QuadFace.GetStride(), ComputeBufferType.Append);
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
            shader.SetBuffer(kernel, voxels, voxelBuffer);
            shader.SetBuffer (kernel, faces1, faceBuffer);

            shader.SetInt (numPointsX, resolution.x);
            shader.SetInt (numPointsY, resolution.y);
            shader.SetInt (numPointsZ, resolution.z);
            shader.SetVector(meshOffset, offset);
            
            shader.Dispatch (kernel, numThreads, numThreads, numThreads);

            // Retrieving data from shader
            ComputeBuffer.CopyCount (faceBuffer, triCountBuffer, 0);
            int[] triCountArray = new int[1];
            triCountBuffer.GetData (triCountArray);
            int numFaces = triCountArray[0];

            QuadFace[] faces = new QuadFace[numFaces];

            faceBuffer.GetData (faces, 0, 0, numFaces);
            

            return MakeMeshes(faces, resolution, offset, out blocktypes);
        } 

        Mesh MakeMeshes(QuadFace[] faces, Vector3Int resolution, Vector3 offset, out int[] blocktypes) {
            // Reset arrays without new allocations
            for (int i = 0; i < verticesOfNodes.Length; i++)
            {
                verticesOfNodes[i].ResetDataNoAlloc();
            }
            vertices.Clear();
            //NOTE: the submeshfaces dictionary is reset as we go later
            
            //Get the types within the mesh
            for(int i = 0; i < faces.Length; i++) {
                if (!submeshFaces.TryGetValue(faces[i].type, out QuadFaceGroup value)) {
                    QuadFaceGroup faceGroup = GetPooledQuadFaceGroup();
                    
                    faceGroup.ResetDataNoAlloc();
                    submeshFaces.Add(faces[i].type, faceGroup);
                    faceGroup.AddFace(ref faces[i]);
                    continue;
                }
                value!.AddFace(ref faces[i]);
            }
            blocktypes = submeshFaces.Keys.ToArray();
            
            //Get mesh Vertices
            //Note, this stores the vertices in "vertices", so we don't do a copy out
            GetMeshVertices(blocktypes, resolution, ref offset);

            int submeshCount = blocktypes.Length;
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
            
            int nextStart = 0;
            for (int k = 0; k < blocktypes.Length; k++) {
                submeshVerts.Clear();
                ref int blocktype = ref blocktypes[k];
                QuadFaceGroup faceGroup = submeshFaces[blocktype];
                int countIndexes = 0;
                
                for (int i = 0; i < faceGroup.faceCount; i++) {
                    ref QuadFace quadFaceNow = ref faceGroup.faces[i];
                    if (!quadFaceNow.IsPartOfMesh()) continue;
                    // A, B, C, D
                    submeshVerts.Add(verticesOfNodes[Globals.LinearIndex((int)quadFaceNow[0].x, (int)quadFaceNow[0].y, (int)quadFaceNow[0].z, resolution)].vertIndex);
                    submeshVerts.Add(verticesOfNodes[Globals.LinearIndex((int)quadFaceNow[1].x, (int)quadFaceNow[1].y, (int)quadFaceNow[1].z, resolution)].vertIndex);
                    submeshVerts.Add(verticesOfNodes[Globals.LinearIndex((int)quadFaceNow[2].x, (int)quadFaceNow[2].y, (int)quadFaceNow[2].z, resolution)].vertIndex);
                    submeshVerts.Add(verticesOfNodes[Globals.LinearIndex((int)quadFaceNow[3].x, (int)quadFaceNow[3].y, (int)quadFaceNow[3].z, resolution)].vertIndex);
                    countIndexes += 4;
                }
                
                mesh.SetIndices(submeshVerts.ToArray(), MeshTopology.Quads, k, false);
                mesh.SetSubMesh(k, new SubMeshDescriptor(nextStart, countIndexes, MeshTopology.Quads));
                nextStart += countIndexes;
                
                submeshFaces.Remove(blocktype, out QuadFaceGroup group);
                ReturnQuadFaceGroupToPool(group);
            }

            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            
            return mesh;
        }
        private void GetMeshVertices(int[] blocktypes, Vector3Int resolution, ref Vector3 offset)
        {
            Vector3 vertexOffsetSum = Vector3.one * -0.5f + offset;
            float scaleFactor = Mathf.Pow(2, VoxelWorld.LEVEL_OF_DETAIL);
            
            for (int blockTypeIndex = 0; blockTypeIndex < blocktypes.Length; blockTypeIndex++) {
                ref int blockType = ref blocktypes[blockTypeIndex];
                QuadFaceGroup meshFaceGroup = submeshFaces[blockType];
                
                for(int i = 0; i < meshFaceGroup.faceCount; i++)
                {
                    ref QuadFace meshFace = ref meshFaceGroup.faces[i];

                    checkVert(ref meshFace, ref meshFace.a);
                    checkVert(ref meshFace, ref meshFace.b);
                    checkVert(ref meshFace, ref meshFace.c);
                    checkVert(ref meshFace, ref meshFace.d);
                    
                    void checkVert(ref QuadFace meshFace, ref Vector3 dcCube)
                    {
                        if (dcCube.x >= 0 && dcCube.x < resolution.x && dcCube.y >= 0 && dcCube.y < resolution.y && dcCube.z >= 0 && dcCube.z < resolution.z) {
                            int voxelIndex = Globals.LinearIndex((int)dcCube.x, (int)dcCube.y, (int)dcCube.z, resolution);
                            ref VoxelVertex voxelVert = ref verticesOfNodes[voxelIndex];
                            voxelVert.AddNeighborFace(ref meshFace);
                            voxelVert.isSet = true;
                            voxelVert.addedToVertexArray = false;
                        }
                    }
                }

                for (int i = 0; i < verticesOfNodes.Length; i++) { 
                    ref VoxelVertex voxelVertex = ref verticesOfNodes[i];
                    
                    if (!voxelVertex.isSet || voxelVertex.addedToVertexArray) continue;
                    
                    voxelVertex.addedToVertexArray = true;
                    voxelVertex.vertIndex = vertices.Count;
                    vertices.Add((voxelVertex.ComputePos() + vertexOffsetSum) * scaleFactor);
                }
            }
        }

        private QuadFaceGroup GetPooledQuadFaceGroup()
        {
            if (faceGroupPool.Count == 0)
            {
                return new QuadFaceGroup(SUBMESH_QUAD_FACE_CAPACITY);
            }
            return faceGroupPool.Pop();
        }

        private void ReturnQuadFaceGroupToPool(QuadFaceGroup faceGroup)
        {
            faceGroupPool.Push(faceGroup);
        }
        
        
        private class QuadFaceGroup
        {
            public readonly QuadFace[] faces;
            public int faceCount;

            public QuadFaceGroup(int faceCountCapacity)
            {
                faces = new QuadFace[faceCountCapacity];
                faceCount = 0;
            }

            public void AddFace(ref QuadFace face)
            {
                faces[faceCount] = face;
                faceCount++;
            }
            

            public void ResetDataNoAlloc()
            {
                faceCount = 0;
            }
        }

        private  struct QuadFace : IComparable {
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

        private struct VoxelVertex {
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
        
        //Down here cause I don't like to see them :/
        private static readonly int meshOffset = Shader.PropertyToID("meshOffset");
        private static readonly int numPointsZ = Shader.PropertyToID("numPointsZ");
        private static readonly int numPointsY = Shader.PropertyToID("numPointsY");
        private static readonly int numPointsX = Shader.PropertyToID("numPointsX");
        private static readonly int faces1 = Shader.PropertyToID("faces");
        private static readonly int voxels = Shader.PropertyToID("voxels");
    }
}