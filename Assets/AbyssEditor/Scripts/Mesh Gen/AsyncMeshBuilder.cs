using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using AbyssEditor.Scripts.Mesh_Gen.Datas;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

namespace AbyssEditor.Scripts.Mesh_Gen
{
    public class AsyncMeshBuilder
    {
        public static AsyncMeshBuilder builder;

        private readonly BlockingCollection<MeshRequest> queue = new();
        private readonly Thread[] workers;
        private bool running = true;
        
        public AsyncMeshBuilder()
        {
            builder = this;

            int worker_count = SystemInfo.processorCount - 1;
            workers = new Thread[worker_count];
            
            for (int i = 0; i < worker_count; i++)
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
                mesh.SetIndices(data.subMeshVertIndexesGroups[materialIndex].ToArray(), MeshTopology.Quads, materialIndex, false);
                mesh.SetSubMesh(materialIndex, new SubMeshDescriptor(nextStart, data.subMeshVertIndexesGroups[materialIndex].Count, MeshTopology.Quads));
                nextStart += data.subMeshVertIndexesGroups[materialIndex].Count;
            }
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            data.builder.SetLocked(false);//release the builder back to the thread
            return new MeshResult(mesh, data.blockTypes);
        }

        //This is the seperate thread,
        private void WorkerLoop()
        {
            Profiler.BeginThreadProfiling("AsyncMeshBuilders", "Worker");
            
            var meshBuilder = new MeshBuilder();//each thread gets its own builder

            while (running)
            {
                try
                {
                    MeshRequest request = queue.Take(); //NOTE: this does not make it busy wait, it will be awoken when queue has something for it
                    MeshData data = meshBuilder.MakeMeshData(request.faces, request.resolution, request.offset);
                    request.tcs.TrySetResult(data);

                    lock (meshBuilder)//wait for the main thread to release the builder back
                    {
                        while (meshBuilder.Locked)
                        {
                            Monitor.Wait(meshBuilder);
                        }
                    }
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
