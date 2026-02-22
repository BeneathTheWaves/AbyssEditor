using System;
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

        public void Awake()
        {
            builder = this;
        }

        public QuadFace[] GenerateFaces(NativeArray<byte> densityGrid, NativeArray<byte> typeGrid, Vector3Int resolution, Vector3 offset) {
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
            return faces;
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
                faceBuffer = new ComputeBuffer(maxFaceCount, QuadFace.GetStride(), ComputeBufferType.Append);
                triCountBuffer = new ComputeBuffer(1, sizeof (int), ComputeBufferType.Raw);
                packed = new uint[numPoints];
            }
        }
        
        void OnDestroy() {
            if (Application.isPlaying) {
                ReleaseBuffers();
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
        
                
        //Down here cause I don't like to see them :/
        private static readonly int meshOffset = Shader.PropertyToID("meshOffset");
        private static readonly int numPointsZ = Shader.PropertyToID("numPointsZ");
        private static readonly int numPointsY = Shader.PropertyToID("numPointsY");
        private static readonly int numPointsX = Shader.PropertyToID("numPointsX");
        private static readonly int faces1 = Shader.PropertyToID("faces");
        private static readonly int voxels = Shader.PropertyToID("voxels");
    }
}
