using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AbyssEditor.Scripts;
using AbyssEditor.Scripts.TerrainMaterials;
using AbyssEditor.Scripts.VoxelTech.VoxelGrids;
using AbyssEditor.Scripts.VoxelTech.VoxelGrids.Brushes;
using AbyssEditor.TerrainMaterials;
using UnityEngine;

namespace AbyssEditor.VoxelTech {
    public class VoxelMetaspace : MonoBehaviour
    {
        public static VoxelMetaspace metaspace;
        
        public List<VoxelMesh> meshes = new();

        void Awake() {
            metaspace = this;
            VoxelGrid.PrecomputeNeighborOffsets();
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
            
            List<VoxelMesh> modifiedMeshes = new List<VoxelMesh>();
            List<BrushJob> brushJobs = new List<BrushJob>();
            foreach(VoxelMesh mesh in meshes) {
                if (OctreeRaycasting.DistanceToBox(stroke.brushLocation, mesh.GetBatchMinBound(), mesh.GetBatchMaxBound()) <= stroke.brushRadius) {
                    mesh.ApplyJobBasedDensityFunction(stroke, brushJobs);
                    modifiedMeshes.Add(mesh);
                }
            }

            //ensure they are ALL complete
            foreach (BrushJob brushJob in brushJobs)
            {
                brushJob.jobHandle.Complete();
            }
            
            //Then update grid
            foreach (BrushJob brushJob in brushJobs)
            {
                brushJob.OnJobCompleteCleanup();
            }
            
            sw.Stop();
            double elapsedMs = (double)sw.ElapsedTicks / System.Diagnostics.Stopwatch.Frequency * 1000.0;
            DebugOverlay.LogMessage($"Completed Brush Job in {(elapsedMs):F4}ms with {brushJobs.Count} Scheduled");
            
            foreach(VoxelMesh mesh in modifiedMeshes) {
                mesh.UpdateMeshesAfterBrush(stroke);
            }
        }
        
        public void ApplyDensityAction(Brush.BrushStroke stroke) {

            if (Brush.activeMode == BrushMode.Smooth || Brush.activeMode == BrushMode.Add || Brush.activeMode == BrushMode.Remove || Brush.activeMode == BrushMode.Paint)
            {
                ApplyJobBasedDensityAction(stroke);
                return;
            }
            
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            
            List<VoxelMesh> modifiedMeshes = new List<VoxelMesh>();
            foreach(VoxelMesh mesh in meshes) {
                if (OctreeRaycasting.DistanceToBox(stroke.brushLocation, mesh.GetBatchMinBound(), mesh.GetBatchMaxBound()) <= stroke.brushRadius) {
                    mesh.ApplyDensityAction(stroke);
                    modifiedMeshes.Add(mesh);
                }
            }
            
            sw.Stop();
            double elapsedMs = (double)sw.ElapsedTicks / System.Diagnostics.Stopwatch.Frequency * 1000.0;
            DebugOverlay.LogMessage($"Completed Brush Job in {(elapsedMs):F4}ms");
            
            foreach(VoxelMesh mesh in modifiedMeshes) {
                mesh.UpdateMeshesAfterBrush(stroke);
            }
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
        
        public bool OctreeExists(Vector3Int treeIndex, Vector3Int batchIndex)
        {
            VoxelMesh mesh = TryGetVoxelMesh(batchIndex);
            if (!mesh) return false;
            
            Vector3Int dimensions = mesh.octreeCounts;
            return (treeIndex.x >= 0 && treeIndex.x < dimensions.x && treeIndex.y >= 0 && treeIndex.y < dimensions.y && treeIndex.z >= 0 && treeIndex.z < dimensions.z);
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
