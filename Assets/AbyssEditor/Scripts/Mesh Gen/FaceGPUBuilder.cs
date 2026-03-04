using System.Collections.Generic;
using System.Linq;
using AbyssEditor.Scripts.Mesh_Gen.Datas;
using Unity.Collections;
using UnityEngine;

namespace AbyssEditor.Scripts.Mesh_Gen
{
    public class FaceGPUBuilder : MonoBehaviour
    {
        public static FaceGPUBuilder builder;
        
        [SerializeField] private ComputeShader shader;
        private ComputeBuffer voxelBuffer;
        private ComputeBuffer faceBuffer; 
        private ComputeBuffer triCountBuffer;
        
        //Used for passing to the shader without new allocations
        private uint[] packed;
        
        Dictionary<int, LODGridGroup> lodCacheGrids = new(); 

        public void Awake()
        {
            builder = this;
            for (int i = 1; i <= 5; i++)
            {

                LODGridGroup gridGroup = new();
                gridGroup.blockWidth = 1 << i;
                gridGroup.gridWidth = (int) Mathf.Pow(2, 5 - i);
                gridGroup.resolution = new Vector3Int(gridGroup.gridWidth, gridGroup.gridWidth, gridGroup.gridWidth);
                
                int gridLinearSize = gridGroup.gridWidth * gridGroup.gridWidth * gridGroup.gridWidth;
                
                gridGroup.densityGrid = new NativeArray<byte>(gridLinearSize, Allocator.Persistent);
                gridGroup.typeGrid = new NativeArray<byte>(gridLinearSize, Allocator.Persistent);

                
                lodCacheGrids.Add(i, gridGroup);
            }
        }

        public QuadFace[] GenerateFaces(NativeArray<byte> densityGrid, NativeArray<byte> typeGrid, Vector3Int resolution, Vector3 offset, int lodLevel) {
            // Setting data inside shader
            const int kernel = 0;
            
            //this is FUCKING awefull for now.... NEED to cleanup majorly
            if (lodLevel > 0)
            {
                LODGridGroup lodGrids = GenerateLodGrids(densityGrid, typeGrid, resolution, offset, lodLevel);
                offset /= lodGrids.blockWidth;
            
                CreateBuffers(lodGrids.resolution);

                int numThreads = Mathf.CeilToInt ((lodGrids.resolution.x) / (float) Globals.THREAD_GROUP_SIZE);
            
                int numPoints = lodGrids.densityGrid.Length;
            
                for (int i = 0; i < numPoints; i++) {
                    packed[i] = (uint)(lodGrids.densityGrid[i] | (lodGrids.typeGrid[i] << 8));
                }
            
                voxelBuffer.SetData(packed);
                faceBuffer.SetCounterValue(0);

                shader.SetBuffer(kernel, voxels, voxelBuffer);
                shader.SetBuffer (kernel, faces1, faceBuffer);

                shader.SetInt (numPointsX, lodGrids.resolution.x);
                shader.SetInt (numPointsY, lodGrids.resolution.y);
                shader.SetInt (numPointsZ, lodGrids.resolution.z);
                shader.SetVector(meshOffset, offset);
            
                shader.Dispatch (kernel, numThreads, numThreads, numThreads);

                // Retrieving data from shader
                ComputeBuffer.CopyCount (faceBuffer, triCountBuffer, 0);
                int[] triCountArray = new int[1];
                triCountBuffer.GetData (triCountArray);
                int numFaces = triCountArray[0];

                QuadFace[] faces = new QuadFace[numFaces];

                faceBuffer.GetData (faces, 0, 0, numFaces);
                return faces;
            }
            
            CreateBuffers(resolution);

            int numFullThreads = Mathf.CeilToInt ((resolution.x) / (float) Globals.THREAD_GROUP_SIZE);
            
            int numFullPoints = densityGrid.Length;
            
            for (int i = 0; i < numFullPoints; i++) {
                packed[i] = (uint)(densityGrid[i] | (typeGrid[i] << 8));
            }
            
            voxelBuffer.SetData(packed);
            faceBuffer.SetCounterValue(0);
            
            shader.SetBuffer(kernel, voxels, voxelBuffer);
            shader.SetBuffer (kernel, faces1, faceBuffer);

            shader.SetInt (numPointsX, resolution.x);
            shader.SetInt (numPointsY, resolution.y);
            shader.SetInt (numPointsZ, resolution.z);
            shader.SetVector(meshOffset, offset);
            
            shader.Dispatch (kernel, numFullThreads, numFullThreads, numFullThreads);

            // Retrieving data from shader
            ComputeBuffer.CopyCount (faceBuffer, triCountBuffer, 0);
            int[] triCountFullArray = new int[1];
            triCountBuffer.GetData (triCountFullArray);
            int numFullFaces = triCountFullArray[0];

            QuadFace[] facesFull = new QuadFace[numFullFaces];

            faceBuffer.GetData (facesFull, 0, 0, numFullFaces);
            return facesFull;
        }
        
        
        
        private void CreateBuffers (Vector3Int pointCounts) {
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
                faceBuffer = new ComputeBuffer(maxFaceCount, QuadFace.GetStride(), ComputeBufferType.Append);
                triCountBuffer = new ComputeBuffer(1, sizeof (int), ComputeBufferType.Raw);
                packed = new uint[numPoints];
            }
        }

        private LODGridGroup GenerateLodGrids(NativeArray<byte> originalDensityGrid, NativeArray<byte> originalTypeGrid, Vector3Int originalResolution, Vector3 originalOffset, int lodLevel)
        {

            if (!lodCacheGrids.TryGetValue(lodLevel, out LODGridGroup lodGridGroup))
            {
                Debug.LogError($"LODLevel {lodLevel} does not exist");
            }

            NativeArray<byte> lodDensityGrid = lodGridGroup.densityGrid;
            NativeArray<byte> lodTypeGrid = lodGridGroup.typeGrid;
                
            Debug.Log(originalResolution);
            
            float scale = (originalResolution.x - 1) / (float)(lodGridGroup.resolution.x - 1);
            
            for (int x = 0; x < lodGridGroup.resolution.x; x++) {
                for (int y = 0; y < lodGridGroup.resolution.y; y++) {
                    for (int z = 0; z < lodGridGroup.resolution.z; z++)
                    {
                        DownSampleVoxel(originalDensityGrid, originalTypeGrid, originalResolution, scale, x, y, z, out byte sampledDensity, out byte sampledType);
                        
                        lodTypeGrid[Globals.LinearIndex(x, y, z, lodGridGroup.resolution)] = sampledType;
                        lodDensityGrid[Globals.LinearIndex(x, y, z, lodGridGroup.resolution)] = sampledDensity;
                    }
                }
            }
            
            return lodGridGroup;
        }
        
        private void OnDestroy() {
            if (Application.isPlaying) {
                ReleaseBuffers();
            }
        }

        private void ReleaseBuffers() {
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

        public void DisposeNativeArrays()
        {
            foreach (KeyValuePair<int, LODGridGroup> gridGoup in lodCacheGrids)
            {
                gridGoup.Value.densityGrid.Dispose();
                gridGoup.Value.typeGrid.Dispose();
            }
        }

        private struct LODGridGroup
        {
            public NativeArray<byte> densityGrid;
            public NativeArray<byte> typeGrid;
            public int gridWidth;
            public int blockWidth;
            public Vector3Int resolution;
        }
        
        private static void DownSampleVoxel(NativeArray<byte> densityGrid, NativeArray<byte> typeGrid, Vector3Int res, float scale, int lx, int ly, int lz, out byte outDensity, out byte outType)
        {
            float startX = lx * scale;
            float startY = ly * scale;
            float startZ = lz * scale;

            float endX = (lx + 1) * scale;
            float endY = (ly + 1) * scale;
            float endZ = (lz + 1) * scale;

            int x0 = Mathf.FloorToInt(startX);
            int y0 = Mathf.FloorToInt(startY);
            int z0 = Mathf.FloorToInt(startZ);

            int x1 = Mathf.CeilToInt(endX);
            int y1 = Mathf.CeilToInt(endY);
            int z1 = Mathf.CeilToInt(endZ);

            int densitySum = 0;
            int count = 0;

            byte nearestValidType = 0;
            
            for (int x = x0; x < x1; x++)
            for (int y = y0; y < y1; y++)
            for (int z = z0; z < z1; z++)
            {
                int cx = Mathf.Clamp(x, 0, res.x - 1);
                int cy = Mathf.Clamp(y, 0, res.y - 1);
                int cz = Mathf.Clamp(z, 0, res.z - 1);

                int index = Globals.LinearIndex(cx, cy, cz, res);

                byte density = densityGrid[index];
                byte type = typeGrid[index];
                
                int effectiveDensity = density;
                if (density == 0 && type != 0)
                    effectiveDensity = 252;

                densitySum += effectiveDensity;
                count++;

                if (nearestValidType == 0 && type != 0)
                {
                    nearestValidType = type;
                }
            }

            outDensity = (byte)(densitySum / count);

            outType = nearestValidType;
        }
                
        //Down here cause I don't like to see them :/
        private static readonly int meshOffset = Shader.PropertyToID("meshOffset");
        private static readonly int numPointsZ = Shader.PropertyToID("numPointsZ");
        private static readonly int numPointsY = Shader.PropertyToID("numPointsY");
        private static readonly int numPointsX = Shader.PropertyToID("numPointsX");
        private static readonly int lodLevelShaderProp = Shader.PropertyToID("lodLevel");
        private static readonly int faces1 = Shader.PropertyToID("faces");
        private static readonly int voxels = Shader.PropertyToID("voxels");
    }
}
