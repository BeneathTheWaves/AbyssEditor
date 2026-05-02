using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AbyssEditor.Scripts.CursorTools;
using AbyssEditor.Scripts.CursorTools.Brush;
using AbyssEditor.Scripts.Mesh_Gen;
using AbyssEditor.Scripts.SaveSystem;
using AbyssEditor.Scripts.TaskSystem;
using AbyssEditor.Scripts.ThreadingManager;
using AbyssEditor.Scripts.UI;
using AbyssEditor.Scripts.Util;
using AbyssEditor.Scripts.VoxelTech.VoxelMeshing;
using AbyssEditor.Scripts.VoxelTech.VoxelMeshing.VoxelGrids;
using AbyssEditor.Scripts.VoxelTech.VoxelMeshing.VoxelGrids.Brushes;
using Unity.Jobs;
using UnityEngine;

namespace AbyssEditor.Scripts.VoxelTech {
    public class VoxelMetaspace : MonoBehaviour
    {
        public static VoxelMetaspace metaspace;
        
        public readonly Dictionary<Vector3Int,VoxelBatch> batches = new();

        private void Awake() {
            metaspace = this;
            VoxelGrid.PrecomputeNeighborOffsets();
            VoxelGrid.PrecomputePaddingVoxels();
        }

        private void Start()
        {
            new WorkerThreadManager();
            new AsyncMeshBuilder();
        }

        private VoxelBatch EnsureMesh(Vector3Int batchIndex) {
            if(TryGetVoxelBatch(batchIndex, out VoxelBatch voxelMesh))
            {
                return voxelMesh;
            }
            return CreateMesh(batchIndex);
        }
        
        public List<VoxelBatch> EnsureRegion(Vector3Int startBatch, Vector3Int endBatch) {
            List<VoxelBatch> returnMeshes = new List<VoxelBatch>();
            foreach (Vector3Int batchIndex in startBatch.IterateTo(endBatch))
            {
                if(TryGetVoxelBatch(batchIndex, out VoxelBatch voxelMesh))
                {
                    returnMeshes.Add(voxelMesh);
                    continue;
                }
                returnMeshes.Add(CreateMesh(batchIndex));
            }
            return returnMeshes;
        }
        
        private VoxelBatch CreateMesh(Vector3Int batchIndex)
        {
            GameObject obj = new GameObject($"batch-{batchIndex.x}-{batchIndex.y}-{batchIndex.z}");
            VoxelBatch voxelBatch = new VoxelBatch(batchIndex, obj);
            batches.TryAdd(batchIndex, voxelBatch);
            return voxelBatch;
        }

        public IEnumerator RemoveBatch(Vector3Int batchIndex)
        {
            CursorToolManager.main.RegisterInputBlock(this);
            if (TryGetVoxelBatch(batchIndex, out VoxelBatch voxelMesh))
            {
                batches.Remove(batchIndex, out VoxelBatch _);
                voxelMesh.Dispose();
                
                DestroyImmediate(voxelMesh.gameObject);
            }

            yield return RegenerateNeighboringVoxelGridsCache();
            
            yield return RegenerateMeshesAsync(reloadBoundariesOnComplete: true);
            
            CursorToolManager.main.UnregisterInputBlock(this);
            StatsTextUI.main.UpdateStats();
        }

        public VoxelGrid TryGetVoxelGrid(Vector3Int batchIndex, Vector3Int containerIndex)
        {
            if (TryGetVoxelBatch(batchIndex, out VoxelBatch mesh))
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
            
            List<VoxelMesh> modifiedContainers = new List<VoxelMesh>(8);
            List<BrushJob> brushJobs = new List<BrushJob>(8);
            foreach(Vector3Int batchIndex in minCheck.IterateTo(maxCheck))
            {
                if (!TryGetVoxelBatch(batchIndex, out VoxelBatch batch))
                {
                    continue;
                }
                if (Utils.SquaredDistanceToBox(stroke.brushLocation, batch.GetBatchMinBound(), batch.GetBatchMaxBound()) <= stroke.squaredRadius) {
                    batch.ApplyJobBasedDensityFunction(stroke, brushJobs, modifiedContainers);
                }
            }

            //Merge Unity handles into one and them poll check every frame
            JobHandle mainHandle = brushJobs.First().jobHandle;
            for (int i = 1; i < brushJobs.Count; i++)
            {
                mainHandle = JobHandle.CombineDependencies(mainHandle, brushJobs[i].jobHandle);
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
            
            foreach (VoxelMesh pointContainer in modifiedContainers)
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
                if(i % WorkerThreadManager.main.workersCount == 0) await Task.Yield();
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
            
            List<VoxelBatch> voxelBatches = metaspace.EnsureRegion(startBatch, endBatch);
            List<Task> tasks = new();
            voxelBatches.ForEach(batch => tasks.Add(batch.LoadGridsFromBatchesAsync(allowModded, statusHandle)));
            await Task.WhenAll(tasks);
            
            statusHandle.CompletePhase();

            await RegenerateNeighboringVoxelGridsCache(statusHandle);

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
                VoxelBatch batch = EnsureMesh(batchIndex);
                
                tasks.Add(batch.LoadGridsFromPatchAsync(patchBytes, offset, statusHandle));
            }
            await Task.WhenAll(tasks);
            statusHandle.CompletePhase();

            await RegenerateNeighboringVoxelGridsCache(statusHandle);
            
            await RegenerateMeshesAsync(statusHandle, true);
        }

        private async Task RegenerateNeighboringVoxelGridsCache(EditorProcessHandle statusHandle = null)
        {
            if (statusHandle == null) { statusHandle = TaskManager.main.GetEditorProcessHandle(1); }
            
            if (batches.Count == 0) {
                statusHandle.CompletePhase();
                return;
            }
            
            statusHandle.SetTasksToCompleteForPhase(batches.Count);
            statusHandle.SetPhasePrefix("Caching tree neighbors (%completedTasks%/%totalTasks%)");
            
            List<Task> tasks = new List<Task>();
            foreach (VoxelBatch batch in batches.Values)
            {
                tasks.Add(batch.CacheNeighboringGridsAsync(statusHandle));
            }
            await Task.WhenAll(tasks);
            
            statusHandle.CompletePhase();
        }
        
        public async Task RegenerateMeshesAsync(EditorProcessHandle statusHandle = null, bool reloadBoundariesOnComplete = false)
        {
            if (statusHandle == null) { statusHandle = TaskManager.main.GetEditorProcessHandle(1); }

            const int meshesToUpdatePerBatch = VoxelWorld.OCTREES_PER_SIDE * VoxelWorld.OCTREES_PER_SIDE * VoxelWorld.OCTREES_PER_SIDE;
            int totalTasks = batches.Count + (batches.Count * meshesToUpdatePerBatch);
            statusHandle.SetTasksToCompleteForPhase(totalTasks);
            
            if(batches.Values.Count != 0) statusHandle.SetPhasePrefix($"Updating Grid(s) (%completedTasks%/%totalTasks%)");
            List<Task> tasks = new List<Task>();
            foreach (VoxelBatch batch in batches.Values) {
                tasks.Add(batch.UpdateFullGridsAsync(statusHandle));
            }
            await Task.WhenAll(tasks);

            if(batches.Values.Count != 0) statusHandle.SetPhasePrefix($"Regenerating Meshes (%completedTasks%/%totalTasks%)");
            tasks.Clear();
            foreach (VoxelBatch batch in batches.Values) {
                tasks.AddRange(await batch.ScheduleMeshRegenAsync(statusHandle));
            }
            
            await Task.WhenAll(tasks);
            
            statusHandle.CompletePhase();

            if (reloadBoundariesOnComplete)
            {
                ReloadBoundaries();
            }
        }
        
        public byte SampleBlocktype(Vector3 hitPoint, Ray cameraRay, int retryCount= 0) {
            // batch -> octree -> voxel
            if (retryCount == 32) return 0;

            // batch
            Vector3Int batchIndex = VoxelWorld.GetBatchIndexFromPoint(hitPoint);
            if (!metaspace.BatchLoaded(batchIndex)) {
                float newDistance = Vector3.Distance(hitPoint, cameraRay.origin) + .5f;
                Vector3 newPoint = cameraRay.GetPoint(newDistance);
                return SampleBlocktype(newPoint, cameraRay, retryCount + 1);
            }

            if (!metaspace.TryGetVoxelBatch(batchIndex, out VoxelBatch batch))
            {
                return 0;
            }

            Vector3 local = hitPoint - batchIndex * (VoxelWorld.BATCH_WIDTH); 
            int x = (int)local.x / VoxelWorld.OCTREE_WIDTH;
            int y = (int)local.y / VoxelWorld.OCTREE_WIDTH;
            int z = (int)local.z / VoxelWorld.OCTREE_WIDTH;

            byte type = batch.pointContainers[Utils.LinearIndex(x, y, z, VoxelWorld.OCTREES_PER_SIDE)].SampleBlocktype(hitPoint);

            if (type == 0) {
                float newDistance = Vector3.Distance(hitPoint, cameraRay.origin) + .5f;
                Vector3 newPoint = cameraRay.GetPoint(newDistance);
                return SampleBlocktype(newPoint, cameraRay, retryCount + 1);
            }

            return type;
        }
        
        
        public bool BatchLoaded(Vector3Int batchIndex)
        {
            return batches.TryGetValue(batchIndex, out VoxelBatch _);
        }

        public void ReloadBoundaries()
        {
            foreach (VoxelBatch mesh in batches.Values)
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
            foreach (VoxelBatch mesh in batches.Values)
            {
                mesh.Dispose();
            }
            BrushJob.DisposeNativeArrayPool();
            VoxelGrid.neighboursToCheckInSmooth.Dispose();
            WorkerThreadManager.main.Dispose();
            FaceGPUBuilder.builder.DisposeNativeArrays();
        }
        
        //TODO migrate above code to use this, conforms to standards better
        public bool TryGetVoxelBatch(Vector3Int batchIndex, out VoxelBatch batch)
        {
            if (!batches.TryGetValue(batchIndex, out VoxelBatch mesResult))
            {
                batch = null;
                return false;
            }
            batch = mesResult;
            return true;
        }
        
    }
}
