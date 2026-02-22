using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using AbyssEditor.Scripts.Mesh_Gen.Datas;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

namespace AbyssEditor.Scripts.Mesh_Gen
{
    public class AsyncMeshBuilder
    {
        public static AsyncMeshBuilder builder;
        
        private const int WORKER_COUNT = 16;
        private int activeWorkerCount;
        
        private readonly BlockingCollection<MeshRequest> queue = new();
        private readonly Thread[] workers = new Thread[WORKER_COUNT];
        private bool running = true;
        
        public AsyncMeshBuilder()
        {
            builder = this;
            for (int i = 0; i < WORKER_COUNT; i++)
            {
                workers[i] = new Thread(WorkerLoop);
                workers[i].Start();
            }
        }
        
        public async Task<MeshResult> RequestMesh(NativeArray<byte> densityGrid, NativeArray<byte> typeGrid, Vector3Int resolution, Vector3 offset)
        {
            
            //get faces from GPU
            QuadFace[] faces = FaceGPUBuilder.builder.GenerateFaces(densityGrid, typeGrid, resolution, offset);
            
            //Build mesh from faces
            TaskCompletionSource<MeshData> meshBuildTcs = new();
            queue.Add(new MeshRequest
            {
                faces = faces,
                resolution = resolution,
                offset = offset,
                tcs = meshBuildTcs,
            });

            MeshData data = await meshBuildTcs.Task;
            
            Mesh mesh = new();
            mesh.subMeshCount = data.blockTypes.Length;
            mesh.vertices = data.vertices.ToArray();
            //mesh.RecalculateBounds();

            int nextStart = 0;
            for (int materialIndex = 0; materialIndex < data.blockTypes.Length; materialIndex++)
            {
                int[] arr = new int[data.subMeshVertIndexesGroups[materialIndex].count];
                System.Array.Copy(data.subMeshVertIndexesGroups[materialIndex].submeshVertsIndexes, arr, arr.Length);
                mesh.SetIndices(arr, MeshTopology.Quads, materialIndex, false);
                mesh.SetSubMesh(materialIndex, new SubMeshDescriptor(nextStart, data.subMeshVertIndexesGroups[materialIndex].count, MeshTopology.Quads));
                nextStart += data.subMeshVertIndexesGroups[materialIndex].count;
            }
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            data.builder.SetLocked(false);//release the builder back to the thread
            return new MeshResult(mesh, data.blockTypes);
        }

        
        private void WorkerLoop()
        {
            var meshBuilder = new MeshBuilder();//each thread gets its own builder

            while (running)
            {
                try
                {
                    MeshRequest request = queue.Take(); // may throw if CompleteAdding called
                    Interlocked.Increment(ref activeWorkerCount);
                    MeshData data = meshBuilder.MakeMeshData(request.faces, request.resolution, request.offset);
                    request.tcs.TrySetResult(data);

                    lock (meshBuilder)//wait for the main thread to release the builder back
                    {
                        while (meshBuilder.Locked)
                        {
                            Monitor.Wait(meshBuilder);
                        }
                    }
                    
                    Interlocked.Decrement(ref activeWorkerCount);
                }
                catch (InvalidOperationException)
                {
                    // happens if the thread is started with no value for Take()
                    // when we are trying to dispose the thread
                    break;
                }
            }
        }

        public void Dispose()
        {
            running = false;
            queue.CompleteAdding();
            
            foreach (var worker in workers)
            {
                if (worker != null && worker.IsAlive)
                {
                    worker.Join();
                }
            }
        }
        
        
        public class MeshResult
        {
            public Mesh mesh;
            public int[] blockTypes;
            public MeshResult(Mesh mesh, int[] blockTypes)
            {
                this.mesh = mesh;
                this.blockTypes = blockTypes;
            }
        }
        
        private struct MeshRequest
        {
            public QuadFace[] faces;
            public Vector3Int resolution;
            public Vector3 offset;
            public TaskCompletionSource<MeshData> tcs; 
        }
    }
}
