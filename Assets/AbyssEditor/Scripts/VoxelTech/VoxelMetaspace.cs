using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AbyssEditor.Scripts.CursorTools;
using AbyssEditor.Scripts.Octrees;
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

        public VoxelGrid TryGetVoxelGrid(Vector3Int batchIndex, Vector3Int containerIndex)
        {
            VoxelMesh.VoxelMesh mesh = TryGetVoxelMesh(batchIndex);
            if (mesh != null)
            {
                return mesh.GetVoxelGrid(containerIndex);
            }
            return null;
        }
        
        public void ApplyJobBasedDensityAction(BrushTool.BrushStroke stroke)
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            
            List<PointContainer> modifiedContainers = new List<PointContainer>(8);
            List<BrushJob> brushJobs = new List<BrushJob>(8);
            foreach(VoxelMesh.VoxelMesh mesh in meshes) {
                if (OctreeRaycasting.DistanceToBox(stroke.brushLocation, mesh.GetBatchMinBound(), mesh.GetBatchMaxBound()) <= stroke.brushRadius) {
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
            
            sw.Stop();
            double elapsedMs = (double)sw.ElapsedTicks / System.Diagnostics.Stopwatch.Frequency * 1000.0;
            DebugOverlay.LogMessage($"Completed Brush Job in {elapsedMs:F4}ms with {brushJobs.Count} Scheduled");
            
            
            sw.Restart();
            foreach (PointContainer pointContainer in modifiedContainers)
            {
                pointContainer.UpdateNeighborData();
            }
            
            foreach(PointContainer pointContainer in modifiedContainers) {
                pointContainer.UpdateMesh();
            }
            sw.Stop();
            double elapsedMs3 = (double)sw.ElapsedTicks / System.Diagnostics.Stopwatch.Frequency * 1000.0;
            DebugOverlay.LogMessage($"Neighbor Copy/Mesh Rebuild took {elapsedMs3:F4}ms");
        }

        public IEnumerator RegionReadCoroutine(bool allowModded, Vector3Int startBatch, Vector3Int endBatch, EditorProcessHandle statusHandle = null) {
            if(statusHandle == null) { statusHandle = TaskManager.main.GetEditorProcessHandle(2); }

            int batchCount = startBatch.GetNumberOfPointsInRegion(endBatch);
            int readCount = 0;
            
            foreach (Vector3Int batchIndex in startBatch.IterateTo(endBatch))
            {
                statusHandle.SetProgress((float)readCount/batchCount);
                statusHandle.SetStatus($"Reading {batchIndex}");
                readCount++;
                
                VoxelMesh.VoxelMesh mesh = TryGetVoxelMesh(batchIndex);
                
                BatchReadWriter.GetPath(mesh.batchIndex, allowModded, out bool isModded);
                
                yield return BatchReadWriter.ReadBatchCoroutine(mesh.OctreesReadCallback, mesh.batchIndex, allowModded, true);
            }
            statusHandle.CompletePhase();

            yield return RegenerateMeshesCoroutine(statusHandle);
        }
        
        public IEnumerator OctreePatchReadCoroutine(byte[] patchBytes, List<Vector3Int> batchesInPatch, EditorProcessHandle statusHandle = null) {
            if(statusHandle == null) { statusHandle = TaskManager.main.GetEditorProcessHandle(3); }    
            
            PatchContainer patchContainer = new PatchContainer();
            
            yield return BatchReadWriter.ReadOctreePatchCoroutine(patchContainer.Callback, patchBytes, batchesInPatch, statusHandle);

            Vector3Int startBatch = new Vector3Int(-1000, -1000, -1000);
            Vector3Int endBatch = Vector3Int.zero;

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
                if (startBatch == new Vector3Int(-1000, -1000, -1000))
                {
                    startBatch = modifiedBatch;
                }
                
                //TODO: THIS IS SCUFFED ASF RN SMH TS PMO ONG ONG FRFR NO CAPA LAPA HI KOOKOO
                AddRegion(modifiedBatch, modifiedBatch);
                
                VoxelMesh.VoxelMesh mesh = TryGetVoxelMesh(modifiedBatch);
                
                mesh.OctreesReadCallback(nodes);
                endBatch = modifiedBatch;
                yield return null;
            }
            statusHandle.CompletePhase();

            yield return RegenerateMeshesCoroutine(statusHandle);
            
            ReloadBoundaries();
            
            CameraControls.main.OnRegionLoad(startBatch, endBatch);
        }

        public IEnumerator RegenerateMeshesCoroutine(EditorProcessHandle statusHandle = null)
        {
            if (statusHandle == null) { statusHandle = TaskManager.main.GetEditorProcessHandle(1); }
            
            int totalTasks = meshes.Count * 2;
            int completedTasks = 0;
            
            foreach (VoxelMesh.VoxelMesh mesh in meshes) {
                mesh.UpdateFullGrids();
                
                statusHandle.SetStatus($"Updating Grid(s) for {mesh.batchIndex}");
                completedTasks++;
                statusHandle.SetProgress((float) completedTasks / totalTasks);
                
                yield return null;
            }

            foreach (VoxelMesh.VoxelMesh mesh in meshes) {
                mesh.Regenerate();
                
                statusHandle.SetStatus($"Regenerating mesh(es) for {mesh.batchIndex}");
                completedTasks++;
                statusHandle.SetProgress((float) completedTasks / totalTasks);
                
                yield return null;
            }
            statusHandle.CompletePhase();
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
                mesh.DisposeGrids();
            }
            BrushJob.DisposeNativeArrayPool();
            VoxelGrid.neighboursToCheckInSmooth.Dispose();
        }

        public VoxelMesh.VoxelMesh TryGetVoxelMesh(Vector3Int batchIndex)
        {
            return meshes.FirstOrDefault(mesh => mesh.batchIndex == batchIndex);
        }
        
        
    }
}
