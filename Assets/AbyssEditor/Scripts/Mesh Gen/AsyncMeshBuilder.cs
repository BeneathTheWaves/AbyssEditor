using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using AbyssEditor.Scripts.Mesh_Gen.Datas;
using AbyssEditor.Scripts.ThreadingManager;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;


namespace AbyssEditor.Scripts.Mesh_Gen
{
    public class AsyncMeshBuilder
    {
        private readonly ConcurrentStack<MeshBuilder> meshBuilders;
        
        public static AsyncMeshBuilder main;
        
        public AsyncMeshBuilder()
        {
            main = this;
            
            meshBuilders = new ConcurrentStack<MeshBuilder>();
            for (int i = 0; i < WorkerThreadScheduler.main.workersCount; i++)
            {
                meshBuilders.Push(new MeshBuilder());
            }
        }
        //TODO: lod level is a enum?
        public async Task<MeshResult> RequestMesh(NativeArray<byte> densityGrid, NativeArray<byte> typeGrid, Vector3Int resolution, Vector3 offset, Mesh meshObjToReuse = null, int lodLevel = 1)
        {
            //get faces from GPU
            //This is sync btw, accessing gpu is blocking in unity (ALTHOUGH VERY fast)
            QuadFace[] faces = FaceGPUBuilder.builder.GenerateFaces(densityGrid, typeGrid, resolution, offset, lodLevel);
            
            //Build mesh from faces
            TaskCompletionSource<MeshData> meshBuildTcs = new();
            
            WorkerThreadScheduler.main.ScheduleParallelManualLocking(() => 
                MeshBuildThreaded(new MeshRequest {
                faces = faces,
                resolution = resolution,
                offset = offset,
                lodLevel = lodLevel,
                tcs = meshBuildTcs
            }));

            MeshData data = await meshBuildTcs.Task;

            Mesh mesh = meshObjToReuse;
            if (!meshObjToReuse)
            {
                mesh = new Mesh();
            }
            else
            {
                mesh.Clear();
            }

            mesh.subMeshCount = data.blockTypes.Length;
            mesh.vertices = data.vertices.ToArray();

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
            return new MeshResult(data.blockTypes);
        }

        private void MeshBuildThreaded(MeshRequest meshRequest)
        {
            if (!meshBuilders.TryPop(out MeshBuilder meshBuilder))
            {
                Debug.LogError($"No mesh builders available for mesh build!!! there needs to be at least as many as the worker count");
            }
            
            MeshData data = meshBuilder.MakeMeshData(meshRequest.faces, meshRequest.resolution, meshRequest.offset, meshRequest.lodLevel);
            meshRequest.tcs.TrySetResult(data);

            lock (meshBuilder)//wait for the main thread to release the builder back
            {
                while (meshBuilder.threadLocked)
                {
                    Monitor.Wait(meshBuilder);
                }
            }
            
            meshBuilders.Push(meshBuilder);//return the builder :)
        }
        
        public class MeshResult
        {
            public readonly int[] blockTypes;
            public MeshResult(int[] blockTypes)
            {
                this.blockTypes = blockTypes;
            }
        }
        
        private struct MeshRequest
        {
            public QuadFace[] faces;
            public Vector3Int resolution;
            public Vector3 offset;
            public int lodLevel;
            public TaskCompletionSource<MeshData> tcs;
        }
    }
}
