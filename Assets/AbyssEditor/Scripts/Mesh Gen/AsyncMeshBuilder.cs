using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AbyssEditor.Scripts.Mesh_Gen.Datas;
using AbyssEditor.Scripts.VoxelTech;
using UnityEngine;
using UnityEngine.Rendering;

namespace AbyssEditor.Scripts.Mesh_Gen
{
    public class AsyncMeshBuilder
    {
        public static AsyncMeshBuilder builder;
        
        private const int WORKER_COUNT = 16;
        
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
        
        public async Task<MeshResult> RequestMesh(QuadFace[] faces, Vector3Int resolution, Vector3 offset)
        {
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
            data.builder.Locked = false;
            return new MeshResult(mesh, data.blockTypes);
        }

        
        private void WorkerLoop()
        {
            var builder = new MeshBuilder();//each thread gets its own builder

            while (running)
            {
                try
                {
                    MeshRequest request = queue.Take(); // may throw if CompleteAdding called
                    MeshData data = builder.MakeMeshData(request.faces, request.resolution, request.offset);
                    request.tcs.TrySetResult(data);

                    while (builder.Locked)
                    {
                        Thread.Sleep(1);
                    }
                }
                catch (InvalidOperationException)
                {
                    // queue is empty & CompleteAdding was called
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
