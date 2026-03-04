using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using AbyssEditor.Scripts.Mesh_Gen.Datas;
using AbyssEditor.Scripts.VoxelTech;
using UnityEngine;

namespace AbyssEditor.Scripts.Mesh_Gen {
    public class MeshBuilder
    {
        public bool threadLocked { get; private set; }
        
        private const int MAX_ADJACENT_FACES = 24;
        private const int SUBMESH_BLOCK_TYPES_CAPACITY = 20;
        private const int SUBMESH_QUAD_FACE_CAPACITY = 10000;
        
        //Pooling/reusing arrays
        private uint[] packed;//Used for passing to the shader with greater efficiency
        private VoxelVertex[] verticesOfNodes;
        private readonly List<Vector3> vertices = new(10000);
        private readonly Dictionary<int, QuadFaceGroup> submeshFaces;
        private readonly Stack<QuadFaceGroup> faceGroupPool = new();
        private readonly Stack<List<int>> subMeshVertPool = new();
        private readonly List<List<int>> submeshVerticesIndexes = new();
        
        public MeshBuilder() {
            //Initialize vertices arrays, so we don't allocate in mesh building
            verticesOfNodes = new VoxelVertex[34 * 34 * 34];
            for (int i = 0; i < verticesOfNodes.Length; i++)
            {
                verticesOfNodes[i] = new VoxelVertex(MAX_ADJACENT_FACES);
            }
            
            //Initialize SubMesh arrays, so we don't allocate in mesh building
            submeshFaces = new Dictionary<int, QuadFaceGroup>(SUBMESH_BLOCK_TYPES_CAPACITY);
        }

        public MeshData MakeMeshData(QuadFace[] faces, Vector3Int resolution, Vector3 offset)
        {
            threadLocked = true;
            // Reset arrays without new allocations
            //TODO: move reset stuff to its own function
            for (int i = 0; i < verticesOfNodes.Length; i++)
            {
                verticesOfNodes[i].ResetDataNoAlloc();
            }
            vertices.Clear();
            for (int i = 0; i < submeshVerticesIndexes.Count; i++)
            {
                submeshVerticesIndexes[i].Clear();
                ReturnSubMeshVertsGroupToPool(submeshVerticesIndexes[i]);
            }
            submeshVerticesIndexes.Clear();
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
            int[] blocktypes = submeshFaces.Keys.ToArray();
            
            //Get mesh Vertices
            //Note, this stores the vertices in "vertices", so we don't do a copy out
            GetMeshVertices(blocktypes, resolution, ref offset);
            
            for (int k = 0; k < blocktypes.Length; k++)
            {
                List<int> submeshVertsArray = GetPooledSubMeshVertsGroup();
                ref int blocktype = ref blocktypes[k];
                QuadFaceGroup faceGroup = submeshFaces[blocktype];
                
                for (int i = 0; i < faceGroup.faceCount; i++) {
                    ref QuadFace quadFaceNow = ref faceGroup.faces[i];
                    if (!quadFaceNow.IsPartOfMesh()) continue;
                    // A, B, C, D
                    submeshVertsArray.Add(verticesOfNodes[Globals.LinearIndex((int)quadFaceNow[0].x, (int)quadFaceNow[0].y, (int)quadFaceNow[0].z, resolution)].vertIndex);
                    submeshVertsArray.Add(verticesOfNodes[Globals.LinearIndex((int)quadFaceNow[1].x, (int)quadFaceNow[1].y, (int)quadFaceNow[1].z, resolution)].vertIndex);
                    submeshVertsArray.Add(verticesOfNodes[Globals.LinearIndex((int)quadFaceNow[2].x, (int)quadFaceNow[2].y, (int)quadFaceNow[2].z, resolution)].vertIndex);
                    submeshVertsArray.Add(verticesOfNodes[Globals.LinearIndex((int)quadFaceNow[3].x, (int)quadFaceNow[3].y, (int)quadFaceNow[3].z, resolution)].vertIndex);
                }
                
                submeshFaces.Remove(blocktype, out QuadFaceGroup group);
                ReturnQuadFaceGroupToPool(group);
                
                submeshVerticesIndexes.Add(submeshVertsArray);
            }
            
            MeshData meshData = new();
            meshData.blockTypes = blocktypes;
            meshData.vertices = vertices;
            meshData.subMeshVertIndexesGroups = submeshVerticesIndexes;
            meshData.builder = this;
            return meshData;
        }
        private void GetMeshVertices(int[] blocktypes, Vector3Int resolution, ref Vector3 offset)
        {
            Vector3 vertexOffsetSum = Vector3.one * -0.5f + offset;
            float scaleFactor = 1;//scale pos based on LOD
            
            for (int blockTypeIndex = 0; blockTypeIndex < blocktypes.Length; blockTypeIndex++) {
                ref int blockType = ref blocktypes[blockTypeIndex];
                QuadFaceGroup meshFaceGroup = submeshFaces[blockType];
                
                for(int i = 0; i < meshFaceGroup.faceCount; i++)
                {
                    ref QuadFace meshFace = ref meshFaceGroup.faces[i];

                    CheckVert(ref meshFace, ref meshFace.a);
                    CheckVert(ref meshFace, ref meshFace.b);
                    CheckVert(ref meshFace, ref meshFace.c);
                    CheckVert(ref meshFace, ref meshFace.d);
                    [MethodImpl(MethodImplOptions.AggressiveInlining)]
                    void CheckVert(ref QuadFace meshFace, ref Vector3 dcCube)
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
        
        private List<int> GetPooledSubMeshVertsGroup()
        {
            if (subMeshVertPool.Count == 0)
            {
                return new List<int>(40000);
            }
            return subMeshVertPool.Pop();
        }

        private void ReturnSubMeshVertsGroupToPool(List<int> faceGroup)
        {
            subMeshVertPool.Push(faceGroup);
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
        
        public void SetLocked(bool value)
        {
            lock (this)
            {
                threadLocked = value;
                if (!threadLocked)
                {
                    Monitor.PulseAll(this); // wake any waiting threads
                }
            }
        }
    }
    
    public class MeshData
    {
        public int[] blockTypes;
        public List<Vector3> vertices;
        public List<List<int>> subMeshVertIndexesGroups;
        public MeshBuilder builder;
    }
}