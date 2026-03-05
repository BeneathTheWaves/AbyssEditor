using System.Collections.Generic;
using System.Linq;
using AbyssEditor.Scripts.Mesh_Gen.Datas;
using AbyssEditor.Scripts.Mesh_Gen.VoxelDownsampling;
using AbyssEditor.Scripts.VoxelTech.VoxelGrids;
using Unity.Collections;
using UnityEngine;

namespace AbyssEditor.Scripts.Mesh_Gen
{
    public class FaceGPUBuilder : MonoBehaviour
    {
        public static FaceGPUBuilder builder;
        
        private VoxelDownsampler voxelDownsampler;
        
        [SerializeField] private ComputeShader shader;
        private ComputeBuffer voxelBuffer;
        private ComputeBuffer faceBuffer; 
        private ComputeBuffer triCountBuffer;
        
        //Used for passing to the shader without new allocations
        private uint[] packed;

        public void Awake()
        {
            builder = this;
            voxelDownsampler = new VoxelDownsampler();
        }

        public QuadFace[] GenerateFaces(NativeArray<byte> densityGrid, NativeArray<byte> typeGrid, Vector3Int resolution, int lodLevel) {
            // Setting data inside shader
            const int kernel = 0;
            
            //this is FUCKING awefull for now.... NEED to cleanup majorly
            if (lodLevel > 0)
            {
                LODGridGroup lodGrids = GenerateLodGrids(densityGrid, typeGrid, resolution, lodLevel);
            
                CreateBuffers(lodGrids.resolution);

                int numThreads = Mathf.CeilToInt ((lodGrids.resolution.x) / (float) Globals.THREAD_GROUP_SIZE);
            
                int numPoints = lodGrids.densityGrid.Length;
            
                for (int i = 0; i < numPoints; i++) {
                    packed[i] = (uint)(lodGrids.densityGrid[i] | (lodGrids.typeGrid[i] << 8));
                }
            
                voxelBuffer.SetData(packed);
                faceBuffer.SetCounterValue(0);

                shader.SetBuffer(kernel, voxels, voxelBuffer);
                shader.SetBuffer(kernel, faces1, faceBuffer);

                shader.SetInt (numPointsX, lodGrids.resolution.x);
                shader.SetInt (numPointsY, lodGrids.resolution.y);
                shader.SetInt (numPointsZ, lodGrids.resolution.z);
            
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

        private LODGridGroup GenerateLodGrids(NativeArray<byte> originalDensityGrid, NativeArray<byte> originalTypeGrid, Vector3Int originalResolution, int lodLevel)
        {

            if (!voxelDownsampler.lodCacheGrids.TryGetValue(lodLevel, out LODGridGroup lodGridGroup))
            {
                Debug.LogError($"LODLevel {lodLevel} does not exist");
            }

            NativeArray<byte> lodDensityGrid = lodGridGroup.densityGrid;
            NativeArray<byte> lodTypeGrid = lodGridGroup.typeGrid;
            
            Debug.Log(lodGridGroup.resolution);
            
            //this iteration is fucked but idk why it *generally works*, there are still holes sometimes but it's not too bad
            //its sampling data from inside the padded voxels on 3 axises, but this somehow fixes the meshes generally ig idk.
            //its cursed but it works. if you want to fix it we will likely need fixes for the dual contour compute shader
            for (int x = VoxelGrid.GRID_PADDING; x < lodGridGroup.resolution.x - VoxelGrid.GRID_PADDING; x++) 
            for (int y = VoxelGrid.GRID_PADDING; y < lodGridGroup.resolution.y - VoxelGrid.GRID_PADDING; y++) 
            for (int z = VoxelGrid.GRID_PADDING; z < lodGridGroup.resolution.z - VoxelGrid.GRID_PADDING; z++)
            {
                voxelDownsampler.DownSampleInnerVoxel(originalDensityGrid, originalTypeGrid, originalResolution, lodGridGroup.blockWidth, x, y, z, out byte sampledDensity, out byte sampledType);
                        
                lodTypeGrid[Globals.LinearIndex(x, y, z, lodGridGroup.resolution)] = sampledType;
                lodDensityGrid[Globals.LinearIndex(x, y, z, lodGridGroup.resolution)] = sampledDensity;
            }
            
            foreach (Vector3Int paddingVoxel in lodGridGroup.paddingVoxels)
            {
                voxelDownsampler.DownSamplePaddedVoxel(originalDensityGrid, originalTypeGrid, originalResolution, lodGridGroup.blockWidth, paddingVoxel.x, paddingVoxel.y, paddingVoxel.z, out byte sampledDensity, out byte sampledType);
                
                lodTypeGrid[Globals.LinearIndex(paddingVoxel.x, paddingVoxel.y, paddingVoxel.z, lodGridGroup.resolution)] = sampledType;
                lodDensityGrid[Globals.LinearIndex(paddingVoxel.x, paddingVoxel.y, paddingVoxel.z, lodGridGroup.resolution)] = sampledDensity;
            }
            return lodGridGroup;
        }
        
        private void OnDestroy() {
            if (Application.isPlaying) {
                ReleaseBuffers();
            }
        }

        public void DisposeNativeArrays()
        {
            voxelDownsampler.DisposeNativeArrays();
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
                
        //Down here cause I don't like to see them :/
        private static readonly int numPointsZ = Shader.PropertyToID("numPointsZ");
        private static readonly int numPointsY = Shader.PropertyToID("numPointsY");
        private static readonly int numPointsX = Shader.PropertyToID("numPointsX");
        private static readonly int faces1 = Shader.PropertyToID("faces");
        private static readonly int voxels = Shader.PropertyToID("voxels");
    }
}
