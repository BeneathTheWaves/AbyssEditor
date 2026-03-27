using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AbyssEditor.Scripts.CursorTools;
using AbyssEditor.Scripts.CursorTools.Brush;
using AbyssEditor.Scripts.Mesh_Gen;
using AbyssEditor.Scripts.Octrees;
using AbyssEditor.Scripts.SaveSystem;
using AbyssEditor.Scripts.TaskSystem;
using AbyssEditor.Scripts.ThreadingManager;
using AbyssEditor.Scripts.UI;
using AbyssEditor.Scripts.VoxelTech.VoxelGrids;
using AbyssEditor.Scripts.VoxelTech.VoxelGrids.Brushes;
using AbyssEditor.Scripts.VoxelTech.VoxelMeshing;
using Unity.Jobs;
using UnityEngine;

namespace AbyssEditor.Scripts.VoxelTech {
    public class VoxelMetaspace : MonoBehaviour
    {
        public static VoxelMetaspace metaspace;
        
        public readonly Dictionary<Vector3Int,VoxelMesh> meshes = new();

        void Awake() {
            metaspace = this;
            VoxelGrid.PrecomputeNeighborOffsets();
            VoxelGrid.PrecomputePaddingVoxels();

            new WorkerThreadScheduler();//This is scuffed, change it
            new AsyncMeshBuilder();
        }

        private VoxelMesh EnsureMesh(Vector3Int batchIndex) {
            if(TryGetVoxelMesh(batchIndex, out VoxelMesh voxelMesh))
            {
                return voxelMesh;
            }
            return CreateMesh(batchIndex);
        }
        
        public List<VoxelMesh> EnsureRegion(Vector3Int startBatch, Vector3Int endBatch) {
            List<VoxelMesh> returnMeshes = new List<VoxelMesh>();
            foreach (Vector3Int batchIndex in startBatch.IterateTo(endBatch))
            {
                if(TryGetVoxelMesh(batchIndex, out VoxelMesh voxelMesh))
                {
                    returnMeshes.Add(voxelMesh);
                    continue;
                }
                returnMeshes.Add(CreateMesh(batchIndex));
            }
            return returnMeshes;
        }
        
        private VoxelMesh CreateMesh(Vector3Int batchIndex)
        {
            VoxelMesh voxelMesh = new GameObject($"batch-{batchIndex.x}-{batchIndex.y}-{batchIndex.z}").AddComponent<VoxelMesh>();
            voxelMesh.Create(batchIndex);
            meshes.Add(batchIndex, voxelMesh);
            return voxelMesh;
        }

        public IEnumerator RemoveBatch(Vector3Int batchIndex)
        {
            CursorToolManager.main.RegisterInputBlock(this);
            if (TryGetVoxelMesh(batchIndex, out VoxelMesh voxelMesh))
            {
                meshes.Remove(batchIndex);
                voxelMesh.Dispose();

                Debug.Log("REMOVED: " + voxelMesh.name);
                
                DestroyImmediate(voxelMesh.gameObject);
            }

            RegenerateNeighboringVoxelGridsCache();
            
            yield return RegenerateMeshesAsync(reloadBoundariesOnComplete: true);
            
            CursorToolManager.main.UnregisterInputBlock(this);
            StatsTextUI.main.UpdateStats();
        }

        public VoxelGrid TryGetVoxelGrid(Vector3Int batchIndex, Vector3Int containerIndex)
        {
            if (TryGetVoxelMesh(batchIndex, out VoxelMesh mesh))
            {
                return mesh.GetVoxelGrid(containerIndex);
            }
            return null;
        }
        
        public async Task ApplyJobBasedDensityActionAsync(BrushStroke stroke)
        {
            CursorToolManager.main.RegisterInputBlock(this);
            
            System.Diagnostics.Stopwatch sw = null;
            if (Preferences.data.enableBrushLogs)
            {
                sw = new System.Diagnostics.Stopwatch();
                sw.Start();
            }

            Vector3Int brushBatch = VoxelWorld.BatchSpacePosToBatchId(stroke.brushLocation);
            int brushBatchRadius = 1;
            brushBatchRadius += Mathf.CeilToInt(stroke.brushRadius) / VoxelWorld.BATCH_WIDTH;
            
            Vector3Int minCheck = brushBatch - (Vector3Int.one * brushBatchRadius);
            Vector3Int maxCheck = brushBatch + (Vector3Int.one * brushBatchRadius);

            Debug.Log(minCheck  + " " + maxCheck);
            
            List<PointContainer> modifiedContainers = new List<PointContainer>(8);
            List<BrushJob> brushJobs = new List<BrushJob>(8);
            foreach(Vector3Int batchIndex in minCheck.IterateTo(maxCheck))
            {
                if (!TryGetVoxelMesh(batchIndex, out VoxelMesh mesh))
                {
                    continue;
                }
                if (OctreeRaycasting.SquaredDistanceToBox(stroke.brushLocation, mesh.GetBatchMinBound(), mesh.GetBatchMaxBound()) <= stroke.squaredRadius) {
                    mesh.ApplyJobBasedDensityFunction(stroke, brushJobs, modifiedContainers);
                }
            }

            //Merge Unity handles into one and them poll check every frame
            JobHandle mainHandle = brushJobs.First().jobHandle;
            for (int i = 1; i < brushJobs.Count; i++)
            {
                JobHandle.CombineDependencies(mainHandle, brushJobs[i].jobHandle);
            }
            await AsyncUtils.WaitForJob(mainHandle);
            
            //Some brushes need to clean up disposable arrays after they are done
            foreach (BrushJob brushJob in brushJobs)
            {
                brushJob.Cleanup();
            }

            if (sw != null)
            {
                sw.Stop();
                double elapsedMs = (double)sw.ElapsedTicks / System.Diagnostics.Stopwatch.Frequency * 1000.0;
                DebugOverlay.LogMessage($"Completed Brush Job in {elapsedMs:F4}ms with {brushJobs.Count} Scheduled");
                sw.Restart();
            }
            
            foreach (PointContainer pointContainer in modifiedContainers)
            {
                pointContainer.UpdateNeighborData();
            }
            
            if (sw != null)
            {
                double elapsedMs2 = (double)sw.ElapsedTicks / System.Diagnostics.Stopwatch.Frequency * 1000.0;
                DebugOverlay.LogMessage($"Neighbor Copy took {elapsedMs2:F4}ms");
                sw.Restart();
            }

            List<Task> tasks = new List<Task>();
            for(int i = 0; i < brushJobs.Count; i++)
            {
                tasks.Add(modifiedContainers[i].UpdateMeshAsync());
                //delay returns slightly to reduce stutters when Unity has to parse the data on main thread
                if(i % WorkerThreadScheduler.main.workersCount == 0) await Task.Yield();
            }
            await Task.WhenAll(tasks);

            CursorToolManager.main.UnregisterInputBlock(this);
            
            if (sw == null) return;
            
            sw.Stop();
            double elapsedMs3 = (double)sw.ElapsedTicks / System.Diagnostics.Stopwatch.Frequency * 1000.0;
            DebugOverlay.LogMessage($"Scheduled Rebuilds in {elapsedMs3:F4}ms");
        }

        public async Task RegionReadAsync(bool allowModded, Vector3Int startBatch, Vector3Int endBatch, EditorProcessHandle statusHandle = null) {
            if(statusHandle == null) { statusHandle = TaskManager.main.GetEditorProcessHandle(3); }

            int batchCount = startBatch.GetNumberOfPointsInRegion(endBatch);
            
            statusHandle.SetTasksToCompleteForPhase(batchCount);
            statusHandle.SetPhasePrefix("Batch Load Tasks (%completedTasks%/%totalTasks%)");
            
            List<VoxelMesh> meshes = metaspace.EnsureRegion(startBatch, endBatch);
            List<Task> tasks = new();
            meshes.ForEach(mesh => tasks.Add(mesh.LoadGridsFromBatchesAsync(allowModded, statusHandle)));
            await Task.WhenAll(tasks);
            
            statusHandle.CompletePhase();

            RegenerateNeighboringVoxelGridsCache(statusHandle);

            await RegenerateMeshesAsync(statusHandle, true);
        }
        
        public async Task PatchReadAsync(byte[] patchBytes, List<Vector3Int> batchesInPatch, List<int> offsetsIntPatch, EditorProcessHandle statusHandle = null)
        {
            if (statusHandle == null) { statusHandle = TaskManager.main.GetEditorProcessHandle(3); }    
            
            statusHandle.SetTasksToCompleteForPhase(batchesInPatch.Count);
            statusHandle.SetPhasePrefix("Patch Load Tasks (%completedTasks%/%totalTasks%)");
            List<Task> tasks = new List<Task>();
            for(int i = 0; i < batchesInPatch.Count; i++)
            {
                Vector3Int batchIndex = batchesInPatch[i];
                int offset = offsetsIntPatch[i];
                VoxelMesh mesh = EnsureMesh(batchIndex);
                
                tasks.Add(mesh.LoadGridsFromPatchAsync(patchBytes, offset, statusHandle));
            }
            await Task.WhenAll(tasks);
            statusHandle.CompletePhase();

            RegenerateNeighboringVoxelGridsCache(statusHandle);
            
            await RegenerateMeshesAsync(statusHandle, true);
        }

        private void RegenerateNeighboringVoxelGridsCache(EditorProcessHandle statusHandle = null)
        {
            if (statusHandle == null) { statusHandle = TaskManager.main.GetEditorProcessHandle(1); }
            
            statusHandle.SetTasksToCompleteForPhase(meshes.Count);
            statusHandle.SetPhasePrefix("Setting tree neighbor caches (%completedTasks%/%totalTasks%)");
            
            foreach (VoxelMesh mesh in meshes.Values)
            { 
                mesh.CacheNeighboringVoxelGrids();
                
                statusHandle.IncrementTasksComplete();
            }
            
            statusHandle.CompletePhase();
        }
        
        public async Task RegenerateMeshesAsync(EditorProcessHandle statusHandle = null, bool reloadBoundariesOnComplete = false)
        {
            if (statusHandle == null) { statusHandle = TaskManager.main.GetEditorProcessHandle(1); }

            const int meshesToUpdatePerBatch = VoxelWorld.CONTAINERS_PER_SIDE * VoxelWorld.CONTAINERS_PER_SIDE * VoxelWorld.CONTAINERS_PER_SIDE;
            int totalTasks = meshes.Count + (meshes.Count * meshesToUpdatePerBatch);
            statusHandle.SetTasksToCompleteForPhase(totalTasks);
            
            statusHandle.SetPhasePrefix($"Updating Grid(s) (%completedTasks%/%totalTasks%)");
            foreach (VoxelMesh mesh in meshes.Values) {
                mesh.UpdateFullGrids();
                statusHandle.IncrementTasksComplete();
                await Task.Yield();
            }

            statusHandle.SetPhasePrefix($"Regenerating Meshes (%completedTasks%/%totalTasks%)");
            List<Task> tasks = new List<Task>();
            foreach (VoxelMesh mesh in meshes.Values) {
                tasks.AddRange(await mesh.ScheduleMeshRegenAsync(statusHandle));
                await Task.Yield();
            }
            
            await Task.WhenAll(tasks);
            
            statusHandle.CompletePhase();

            if (reloadBoundariesOnComplete)
            {
                ReloadBoundaries();
            }
        }
        
        public bool BatchLoaded(Vector3Int batchIndex) {
            if(meshes.TryGetValue(batchIndex, out VoxelMesh _))
            {
                return true;
            }
            return false;
        }

        private void ReloadBoundaries()
        {
            foreach (VoxelMesh mesh in meshes.Values)
            {
                mesh.RedrawBoundaryPlanes();
            }
        }
        
        /// <summary>
        /// We HAVE TO free the native arrays before the game closes (especially in the editor) as it will cause a memory leak if we don't.
        /// </summary>
        void OnApplicationQuit()
        {
            Debug.Log("Disposing Native Arrays");
            foreach (VoxelMesh mesh in meshes.Values)
            {
                mesh.Dispose();
            }
            BrushJob.DisposeNativeArrayPool();
            VoxelGrid.neighboursToCheckInSmooth.Dispose();
            WorkerThreadScheduler.main.Dispose();
            FaceGPUBuilder.builder.DisposeNativeArrays();
        }
        
        //TODO migrate above code to use this, conforms to standards better
        public bool TryGetVoxelMesh(Vector3Int batchIndex, out VoxelMesh mesh)
        {
            if (!meshes.TryGetValue(batchIndex, out VoxelMesh mesResult))
            {
                mesh = null;
                return false;
            }
            mesh = mesResult;
            return true;
        }
        
    }
}
