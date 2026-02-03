using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using AbyssEditor.Octrees;
using AbyssEditor.Scripts;
using AbyssEditor.Scripts.TerrainMaterials;
using AbyssEditor.Scripts.VoxelTech.VoxelGrids;
using AbyssEditor.Scripts.VoxelTech.VoxelGrids.Brushes;
using AbyssEditor.TerrainMaterials;
using Unity.Jobs;
using UnityEngine;

namespace AbyssEditor.VoxelTech {
    public class VoxelMetaspace : MonoBehaviour
    {
        public static VoxelMetaspace metaspace;
        
        public List<VoxelMesh> meshes = new();

        void Awake() {
            metaspace = this;
            VoxelGrid.PrecomputeNeighborOffsets();
            VoxelGrid.PrecomputePaddingVoxels();
        }

        public void AddRegion(Vector3Int startBatch, Vector3Int endBatch) {
            if (!SnMaterialLoader.instance.contentLoaded) {
                SnMaterialLoader.instance.updateMeshesOnLoad = true;
            }

            foreach (Vector3Int batchIndex in startBatch.IterateTo(endBatch))
            {
                VoxelMesh voxelMesh = TryGetVoxelMesh(batchIndex);
                
                if(!voxelMesh)
                {
                    voxelMesh = new GameObject($"batch-{batchIndex.x}-{batchIndex.y}-{batchIndex.z}").AddComponent<VoxelMesh>();
                    voxelMesh.Create(batchIndex);
                    meshes.Add(voxelMesh);
                }
            }
        }

        public VoxelGrid TryGetVoxelGrid(Vector3Int batchIndex, Vector3Int containerIndex)
        {
            VoxelMesh mesh = TryGetVoxelMesh(batchIndex);
            if (mesh != null)
            {
                return mesh.GetVoxelGrid(containerIndex);
            }
            return null;
        }
        
        public void ApplyJobBasedDensityAction(Brush.BrushStroke stroke)
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            
            List<PointContainer> modifiedContainers = new List<PointContainer>(8);
            List<BrushJob> brushJobs = new List<BrushJob>(8);
            foreach(VoxelMesh mesh in meshes) {
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

        public IEnumerator RegionReadCoroutine(bool allowModded, Vector3Int startBatch, Vector3Int endBatch) {
            foreach (Vector3Int batchIndex in startBatch.IterateTo(endBatch))
            {
                VoxelMesh mesh = TryGetVoxelMesh(batchIndex);
                
                BatchReadWriter.GetPath(mesh.batchIndex, allowModded, out bool isModded);
                
                yield return BatchReadWriter.readWriter.ReadBatchCoroutine(mesh.OctreesReadCallback, mesh.batchIndex, allowModded, true);
            }

            yield return RegenerateMeshesCoroutine();
        }
        
        public IEnumerator OctreePatchReadCoroutine(string filepath) {
            
            PatchContainer patchContainer = new PatchContainer();
            yield return BatchReadWriter.readWriter.ReadOctreePatchCoroutine(patchContainer.Callback, filepath);

            Vector3Int startBatch = new Vector3Int(-100, -100, -100);
            Vector3Int endBatch = Vector3Int.zero; 
            foreach (Vector3Int modifiedBatch in patchContainer.modifiedBatches.Keys)
            {
                if (!patchContainer.modifiedBatches.TryGetValue(modifiedBatch, out Octree[,,] nodes))
                {
                    continue;
                }
                if (startBatch == new Vector3Int(-100, -100, -100))
                {
                    startBatch = modifiedBatch;
                }
                
                //TODO: THIS IS SCUFFED ASF RN SMH TS PMO ONG ONG FRFR NO CAPA LAPA HI KOOKOO
                AddRegion(modifiedBatch, modifiedBatch);
                
                VoxelMesh mesh = TryGetVoxelMesh(modifiedBatch);
                
                mesh.OctreesReadCallback(nodes);
                endBatch = modifiedBatch;
            }

            yield return RegenerateMeshesCoroutine();
            
            ReloadBoundaries();
            
            CameraControls.main.OnRegionLoad(startBatch, endBatch);
        }

        public IEnumerator RegenerateMeshesCoroutine() {
            foreach (VoxelMesh mesh in meshes) {
                mesh.UpdateFullGrids();
                yield return null;
            }

            foreach (VoxelMesh mesh in meshes) {
                mesh.Regenerate();
                yield return null;
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
            foreach (VoxelMesh mesh in meshes)
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
            foreach (VoxelMesh mesh in meshes)
            {
                mesh.DisposeGrids();
            }
            BrushJob.DisposeNativeArrayPool();
            VoxelGrid.neighboursToCheckInSmooth.Dispose();
        }

        public VoxelMesh TryGetVoxelMesh(Vector3Int batchIndex)
        {
            return meshes.FirstOrDefault(mesh => mesh.batchIndex == batchIndex);
        }
        
        
    }
}
