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
using AbyssEditor.Scripts.UI;
using AbyssEditor.Scripts.VoxelTech.VoxelGrids;
using AbyssEditor.Scripts.VoxelTech.VoxelGrids.Brushes;
using AbyssEditor.Scripts.VoxelTech.VoxelMesh;
using UnityEngine;

namespace AbyssEditor.Scripts.VoxelTech {
    public class VoxelMetaspace : MonoBehaviour
    {
        public static VoxelMetaspace metaspace;
        
        public List<VoxelMesh.VoxelMesh> meshes = new();

        void Awake() {
            metaspace = this;
            VoxelGrid.PrecomputeNeighborOffsets();
            VoxelGrid.PrecomputePaddingVoxels();

            new AsyncMeshBuilder();//This is kinda scuffed, change it
        }

        public void AddRegion(Vector3Int startBatch, Vector3Int endBatch) {
            foreach (Vector3Int batchIndex in startBatch.IterateTo(endBatch))
            {
                VoxelMesh.VoxelMesh voxelMesh = TryGetVoxelMesh(batchIndex);
                
                if(!voxelMesh)
                {
                    voxelMesh = new GameObject($"batch-{batchIndex.x}-{batchIndex.y}-{batchIndex.z}").AddComponent<VoxelMesh.VoxelMesh>();
                    voxelMesh.Create(batchIndex);
                    meshes.Add(voxelMesh);
                }
            }
        }

        public IEnumerator RemoveBatch(Vector3Int batchIndex)
        {
            CursorToolManager.main.RegisterInputBlock(this);
            if (TryGetVoxelMesh(batchIndex, out VoxelMesh.VoxelMesh voxelMesh))
            {
                meshes.Remove(voxelMesh);
                voxelMesh.Dispose();

                Debug.Log("REMOVED: " + voxelMesh.name);
                
                DestroyImmediate(voxelMesh.gameObject);
            }

            RegenerateNeighboringVoxelGridsCache();
            
            yield return RegenerateMeshesAsync(reloadBoundariesOnComplete: true);
            
            CursorToolManager.main.UnregisterInputBlock(this);
        }

        public VoxelGrid TryGetVoxelGrid(Vector3Int batchIndex, Vector3Int containerIndex)
        {
            VoxelMesh.VoxelMesh mesh = TryGetVoxelMesh(batchIndex);
            if (mesh != null)
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
            
            List<PointContainer> modifiedContainers = new List<PointContainer>(8);
            List<BrushJob> brushJobs = new List<BrushJob>(8);
            foreach(VoxelMesh.VoxelMesh mesh in meshes) {
                if (OctreeRaycasting.SquaredDistanceToBox(stroke.brushLocation, mesh.GetBatchMinBound(), mesh.GetBatchMaxBound()) <= stroke.squaredRadius) {
                    mesh.ApplyJobBasedDensityFunction(stroke, brushJobs, modifiedContainers);
                }
            }

            //ensure they are ALL complete
            foreach (BrushJob brushJob in brushJobs)
            {
                brushJob.jobHandle.Complete();
            }
            
            //cleanup job arrays (if any)
            foreach (BrushJob brushJob in brushJobs)
            {
                brushJob.OnJobCompleteCleanup();
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

            var tasks = new List<Task>();
            foreach (PointContainer container in modifiedContainers)
            {
                tasks.Add(container.UpdateMeshAsync());
            }
            await Task.WhenAll(tasks);

            CursorToolManager.main.UnregisterInputBlock(this);
            
            if (sw == null) return;
            
            sw.Stop();
            double elapsedMs3 = (double)sw.ElapsedTicks / System.Diagnostics.Stopwatch.Frequency * 1000.0;
            DebugOverlay.LogMessage($"Scheduled Rebuilds in {elapsedMs3:F4}ms");
        }

        public async Task RegionReadCoroutine(bool allowModded, Vector3Int startBatch, Vector3Int endBatch, EditorProcessHandle statusHandle = null) {
            if(statusHandle == null) { statusHandle = TaskManager.main.GetEditorProcessHandle(3); }

            int batchCount = startBatch.GetNumberOfPointsInRegion(endBatch);
            int readCount = 0;
            
            foreach (Vector3Int batchIndex in startBatch.IterateTo(endBatch))
            {
                statusHandle.SetProgress((float)readCount/batchCount);
                statusHandle.SetStatus($"Reading {batchIndex}");
                readCount++;
                
                VoxelMesh.VoxelMesh mesh = TryGetVoxelMesh(batchIndex);
                
                BatchReadWriter.GetPath(mesh.batchIndex, allowModded, out bool isModded);
                
                await BatchReadWriter.ReadBatchCoroutine(mesh.OctreesReadCallback, mesh.batchIndex, allowModded, true);
                await Task.Yield();
            }
            statusHandle.CompletePhase();

            RegenerateNeighboringVoxelGridsCache(statusHandle);

            await RegenerateMeshesAsync(statusHandle, true);
        }
        
        public async Task OctreePatchReadAsync(byte[] patchBytes, List<Vector3Int> batchesInPatch, EditorProcessHandle statusHandle = null) {
            if(statusHandle == null) { statusHandle = TaskManager.main.GetEditorProcessHandle(4); }    
            
            PatchContainer patchContainer = new PatchContainer();
            
            await BatchReadWriter.ReadOctreePatchCoroutine(patchContainer.Callback, patchBytes, batchesInPatch, statusHandle);
            

            int batchCount = patchContainer.modifiedBatches.Keys.Count;
            int readIndex = 0;
            
            foreach (Vector3Int modifiedBatch in patchContainer.modifiedBatches.Keys)
            {
                statusHandle.SetStatus($"Applying {modifiedBatch}");
                statusHandle.SetProgress((float)readIndex/batchCount);
                readIndex++;
                
                if (!patchContainer.modifiedBatches.TryGetValue(modifiedBatch, out Octree[,,] nodes))
                {
                    continue;
                }
                
                //TODO: THIS IS SCUFFED ASF RN SMH TS PMO ONG ONG FRFR NO CAPA LAPA HI KOOKOO
                AddRegion(modifiedBatch, modifiedBatch);
                
                VoxelMesh.VoxelMesh mesh = TryGetVoxelMesh(modifiedBatch);
                
                mesh.OctreesReadCallback(nodes);
            }
            statusHandle.CompletePhase();

            RegenerateNeighboringVoxelGridsCache(statusHandle);
            
            await RegenerateMeshesAsync(statusHandle, true);
        }

        private void RegenerateNeighboringVoxelGridsCache(EditorProcessHandle statusHandle = null)
        {
            if (statusHandle == null) { statusHandle = TaskManager.main.GetEditorProcessHandle(1); }
            
            int totalTasks = meshes.Count * VoxelWorld.CONTAINERS_PER_SIDE;
            int completedTasks = 0;
            
            foreach (VoxelMesh.VoxelMesh mesh in meshes)
            { 
                mesh.CacheNeighboringVoxelGrids();
                
                statusHandle.SetStatus($"Caching Neighbors for {mesh.batchIndex}");
                completedTasks++;
                statusHandle.SetProgress((float) completedTasks / totalTasks);
            }
            
            statusHandle.CompletePhase();
        }
        
        public async Task RegenerateMeshesAsync(EditorProcessHandle statusHandle = null, bool reloadBoundariesOnComplete = false)
        {
            if (statusHandle == null) { statusHandle = TaskManager.main.GetEditorProcessHandle(1); }
            
            int totalTasks = meshes.Count * 2;
            int completedTasks = 0;
            
            foreach (VoxelMesh.VoxelMesh mesh in meshes) {
                mesh.UpdateFullGrids();
                
                statusHandle.SetStatus($"Updating Grid(s) for {mesh.batchIndex}");
                completedTasks++;
                statusHandle.SetProgress((float) completedTasks / totalTasks);

                await Task.Yield();
            }

            List<Task> tasks = new List<Task>();
            foreach (VoxelMesh.VoxelMesh mesh in meshes) {
                tasks.AddRange(mesh.ScheduleMeshRegenAsync());
                
                statusHandle.SetStatus($"Scheduled mesh regen for {mesh.batchIndex}");
                completedTasks++;
                statusHandle.SetProgress((float) completedTasks / totalTasks);
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
            if(meshes.FirstOrDefault(mesh => mesh.batchIndex == batchIndex))
            {
                return true;
            }
            return false;
        }

        public void ReloadBoundaries()
        {
            foreach (VoxelMesh.VoxelMesh mesh in meshes)
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
            foreach (VoxelMesh.VoxelMesh mesh in meshes)
            {
                mesh.Dispose();
            }
            BrushJob.DisposeNativeArrayPool();
            VoxelGrid.neighboursToCheckInSmooth.Dispose();
            AsyncMeshBuilder.builder.Dispose();
        }
        
        public VoxelMesh.VoxelMesh TryGetVoxelMesh(Vector3Int batchIndex)
        {
            return meshes.FirstOrDefault(mesh => mesh.batchIndex == batchIndex);
        }
        
        //TODO migrate above code to use this, conforms to standards better
        public bool TryGetVoxelMesh(Vector3Int batchIndex, out VoxelMesh.VoxelMesh mesh)
        {
            VoxelMesh.VoxelMesh voxelMesh = TryGetVoxelMesh(batchIndex);
            if (voxelMesh == null)
            {
                mesh = null;
                return false;
            }
            mesh = voxelMesh;
            return true;
        }
        
    }
}
