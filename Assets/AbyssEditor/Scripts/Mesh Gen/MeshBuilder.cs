using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using AbyssEditor.Scripts.Mesh_Gen.Datas;
using AbyssEditor.Scripts.VoxelTech.VoxelGrids;
using UnityEngine;


namespace AbyssEditor.Scripts.Mesh_Gen {
    public class MeshBuilder
    {
        public bool threadLocked { get; private set; }

        //Yes this is kind of hacky to fix *most* seams between lods, seems to work good enough tho
        private static readonly Dictionary<int, float> lodScales = new Dictionary<int, float>
        {
            { 0, 1f },
            { 1, 1f/*1.15f*/ },
            { 2, 1f },
            { 3, 1f },
        };
        
        private const int MAX_ADJACENT_FACES = 24;
        private const int SUBMESH_BLOCK_TYPES_CAPACITY = 20;
        private const int SUBMESH_QUAD_FACE_CAPACITY = 10000;
        
        //Pooling/reusing arrays
        private uint[] packed;//Used for passing to the shader with greater efficiency
        private readonly VoxelVertexGroup vertexGroup = new(VoxelGrid.GRID_FULL_SIDE * VoxelGrid.GRID_FULL_SIDE * VoxelGrid.GRID_FULL_SIDE);
        private readonly List<Vector3> vertices = new(10000);
        private readonly Dictionary<int, QuadFaceGroup> submeshFaces = new(SUBMESH_BLOCK_TYPES_CAPACITY);
        private readonly Stack<QuadFaceGroup> faceGroupPool = new();
        private readonly Stack<List<int>> subMeshVertPool = new();
        private readonly List<List<int>> submeshVerticesIndexes = new();

        //Initialize vertices arrays, so we don't allocate in mesh building
        //Initialize SubMesh arrays, so we don't allocate in mesh building

        public MeshData MakeMeshData(QuadFace[] faces, Vector3Int resolution, Vector3 offset, int lodLevel)
        {
            threadLocked = true;
            // Reset arrays without new allocations
            //TODO: move reset stuff to its own function
            vertexGroup.InitializeNewSize(resolution.x * resolution.y * resolution.z);
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
            
            if(!lodScales.TryGetValue(lodLevel, out var lodVoxelScale))
            {
                Debug.LogError($"No scale for {lodLevel} found!");
                return null;
            }
            //Get mesh Vertices
            //Note, this stores the vertices in "vertices", so we don't do a copy out
            GetMeshVertices(blocktypes, resolution, ref offset, ref lodLevel, ref lodVoxelScale);
            
            for (int k = 0; k < blocktypes.Length; k++)
            {
                List<int> submeshVertsArray = GetPooledSubMeshVertsGroup();
                ref int blockType = ref blocktypes[k];
                QuadFaceGroup faceGroup = submeshFaces[blockType];
                
                for (int i = 0; i < faceGroup.faceCount; i++) {
                    ref QuadFace quadFaceNow = ref faceGroup.faces[i];
                    if (!quadFaceNow.IsPartOfMesh()) continue;
                    // A, B, C, D
                    submeshVertsArray.Add(vertexGroup.LinearIndex((int)quadFaceNow[0].x, (int)quadFaceNow[0].y, (int)quadFaceNow[0].z, resolution).vertIndex);
                    submeshVertsArray.Add(vertexGroup.LinearIndex((int)quadFaceNow[1].x, (int)quadFaceNow[1].y, (int)quadFaceNow[1].z, resolution).vertIndex);
                    submeshVertsArray.Add(vertexGroup.LinearIndex((int)quadFaceNow[2].x, (int)quadFaceNow[2].y, (int)quadFaceNow[2].z, resolution).vertIndex);
                    submeshVertsArray.Add(vertexGroup.LinearIndex((int)quadFaceNow[3].x, (int)quadFaceNow[3].y, (int)quadFaceNow[3].z, resolution).vertIndex);
                }
                
                submeshFaces.Remove(blockType, out QuadFaceGroup group);
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
        private void GetMeshVertices(int[] blocktypes, Vector3Int resolution, ref Vector3 meshOffsetWithinBatch, ref int lodLevel, ref float lodVoxelScale)
        {
            int voxelSize = 1 << lodLevel; // 2 ^ lodLevel
            Vector3 vertexOffset = (Vector3.one * -0.5f + meshOffsetWithinBatch) * voxelSize;
            
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
                            ref VoxelVertex voxelVert = ref vertexGroup.verticesOfNodes[voxelIndex];
                            voxelVert.AddNeighborFace(ref meshFace);
                            voxelVert.isSet = true;
                            voxelVert.addedToVertexArray = false;
                        }
                    }
                }

                for (int i = 0; i < vertexGroup.size; i++) { 
                    ref VoxelVertex voxelVertex = ref vertexGroup.verticesOfNodes[i];
                    
                    if (!voxelVertex.isSet || voxelVertex.addedToVertexArray) continue;
                    
                    voxelVertex.addedToVertexArray = true;
                    voxelVertex.vertIndex = vertices.Count;
                    
                    vertices.Add(voxelVertex.ComputePos() * (voxelSize * lodVoxelScale) + vertexOffset);
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

        private class VoxelVertexGroup
        {
            public readonly VoxelVertex[] verticesOfNodes;
            public int size;
            public VoxelVertexGroup(int internalMaxCapacity)
            {
                verticesOfNodes = new VoxelVertex[internalMaxCapacity];
                size = 0;
                
                //initialize vertexes to memory
                for (int i = 0; i < verticesOfNodes.Length; i++)
                {
                    verticesOfNodes[i] = new VoxelVertex(MAX_ADJACENT_FACES);
                }
            }

            public void InitializeNewSize(int newSize)
            {
                ResetDataNoAlloc();
                size = newSize;
            }
            
            public VoxelVertex LinearIndex(int x, int y, int z, Vector3Int resolution)
            {
                return verticesOfNodes[x + y * resolution.x + z * resolution.y * resolution.z];
            }
            
            private void ResetDataNoAlloc()
            {
                for (int i = 0; i < size; i++)
                {
                    verticesOfNodes[i].ResetDataNoAlloc();
                }
                size = 0;
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