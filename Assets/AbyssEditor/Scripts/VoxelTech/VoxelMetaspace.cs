using System.Collections;
using System.Collections.Generic;
using AbyssEditor.Scripts.TerrainMaterials;
using AbyssEditor.Scripts.VoxelTech.VoxelGrids;
using AbyssEditor.Scripts.VoxelTech.VoxelGrids.Brushes;
using AbyssEditor.TerrainMaterials;
using UnityEngine;

namespace AbyssEditor.VoxelTech {
    public class VoxelMetaspace : MonoBehaviour
    {
        public static VoxelMetaspace metaspace;
        public VoxelMesh[] meshes;
        public VoxelMesh this[Vector3Int index] {
            get {
                return meshes[GetLabel(index)];
            }
        }

        public bool allowModded;

        // loading fields
        public bool loadInProgress = false;
        public float loadingProgress;
        public string loadingState;

        void Awake() {
            metaspace = this;
            VoxelGrid.PrecomputeNeighborOffsets();
        }

        public void Create(int numBatches) {
            meshes = new VoxelMesh[numBatches];

            if (!SnMaterialLoader.instance.contentLoaded) {
                SnMaterialLoader.instance.updateMeshesOnLoad = true;
            }

            for (int y = VoxelWorld.startBatch.y; y <= VoxelWorld.endBatch.y; y++) {
                for (int z = VoxelWorld.startBatch.z; z <= VoxelWorld.endBatch.z; z++) {
                    for (int x = VoxelWorld.startBatch.x; x <= VoxelWorld.endBatch.x; x++) {
                        
                        Vector3Int coords = new Vector3Int(x, y, z);
                        VoxelMesh batchComponent = new GameObject($"batch-{x}-{y}-{z}").AddComponent<VoxelMesh>();
                        batchComponent.Create(coords);
                        int label = GetLabel(coords);
                        meshes[label] = batchComponent;
                    }
                }   
            }
        }
        public void Clear() {
            for (int y = VoxelWorld.startBatch.y; y <= VoxelWorld.endBatch.y; y++) {
                for (int z = VoxelWorld.startBatch.z; z <= VoxelWorld.endBatch.z; z++) {
                    for (int x = VoxelWorld.startBatch.x; x <= VoxelWorld.endBatch.x; x++) {
                        Destroy(meshes[GetLabel(x, y, z)].gameObject);
                    }
                }
            }
        }

        public VoxelGrid GetVoxelGrid(Vector3Int globalBatchIndex, Vector3Int containerIndex) => meshes[GetLabel(globalBatchIndex)].GetVoxelGrid(containerIndex);
        public byte[] GetVoxel(Vector3Int voxel, Vector3Int octree, Vector3Int batch) => GetVoxelGrid(batch, octree).GetVoxel(voxel.x, voxel.y, voxel.z);

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

            foreach (BrushJob brushJob in brushJobs)
            {
                brushJob.EnsureComplete();
                brushJob.OnJobCompleteCleanup();
            }
            
            sw.Stop();
            DebugOverlay.LogMessage($"Brush Operation in {sw.ElapsedMilliseconds}ms");
            
            foreach(VoxelMesh mesh in modifiedMeshes) {
                mesh.UpdateMeshesAfterBrush(stroke);
            }
        }
        
        public void ApplyDensityAction(Brush.BrushStroke stroke) {

            if (Brush.activeMode == BrushMode.Smooth)
            {
                ApplyJobBasedDensityAction(stroke);
                return;
            }
            
            List<VoxelMesh> modifiedMeshes = new List<VoxelMesh>();
            foreach(VoxelMesh mesh in meshes) {
                if (OctreeRaycasting.DistanceToBox(stroke.brushLocation, mesh.GetBatchMinBound(), mesh.GetBatchMaxBound()) <= stroke.brushRadius) {
                    mesh.ApplyDensityAction(stroke);
                    modifiedMeshes.Add(mesh);
                }
            }
            foreach(VoxelMesh mesh in modifiedMeshes) {
                mesh.UpdateMeshesAfterBrush(stroke);
            }
        }

        public IEnumerator RegionReadCoroutine(bool allowModded) {
            // sets + rasterizes all octrees
            loadInProgress = true;
            float endLabel = GetLabel(VoxelWorld.endBatch) + 1;
            foreach (VoxelMesh mesh in meshes) {
                loadingProgress = GetLabel(mesh.batchIndex) / (endLabel * 3);
                BatchReadWriter.GetPath(mesh.batchIndex, allowModded, out bool isModded);
                loadingState = $"Reading {(isModded ? "modded" : "")} batch {mesh.batchIndex}";
                yield return BatchReadWriter.readWriter.ReadBatchCoroutine(mesh.OctreesReadCallback, mesh.batchIndex, allowModded, true);
            }

            yield return RegenerateMeshesCoroutine(1, 3);
        }

        public IEnumerator RegenerateMeshesCoroutine(int tasksDone, int totalTasks) {
            // redistribute full grids
            loadInProgress = true;
            float endLabel = GetLabel(VoxelWorld.endBatch) + 1;
            foreach (VoxelMesh mesh in meshes) {
                loadingProgress = (GetLabel(mesh.batchIndex) / (endLabel * totalTasks)) + ((float)tasksDone) / totalTasks;
                loadingState = $"Joining batch {mesh.batchIndex}";
                mesh.UpdateFullGrids();
                yield return null;
            }
            // generate meshes
            foreach (VoxelMesh mesh in meshes) {
                loadingProgress = (GetLabel(mesh.batchIndex) / (endLabel * totalTasks)) + ((float)tasksDone + 1) / totalTasks;
                loadingState = $"Creating mesh for {mesh.batchIndex}";
                mesh.Regenerate();
                yield return null;
            }
            loadInProgress = false;
        }

        public static bool VoxelExists(int x, int y, int z) {
            return x >= 1 && x < VoxelWorld.RESOLUTION + 1 && y >= 1 && y < VoxelWorld.RESOLUTION + 1 && z >= 1 && z < VoxelWorld.RESOLUTION + 1;
        }
        public static bool OctreeExists(Vector3Int treeIndex, Vector3Int batchIndex) {
            if (!BatchExists(batchIndex)) return false;
            Vector3Int dimensions = metaspace[batchIndex].octreeCounts;
            return (treeIndex.x >= 0 && treeIndex.x < dimensions.x && treeIndex.y >= 0 && treeIndex.y < dimensions.y && treeIndex.z >= 0 && treeIndex.z < dimensions.z);
        }
        public static bool BatchExists(Vector3Int batchIndex) {
            if (batchIndex.x >= VoxelWorld.startBatch.x && batchIndex.x <= VoxelWorld.endBatch.x
                                                   && batchIndex.y >= VoxelWorld.startBatch.y &&
                                                   batchIndex.y <= VoxelWorld.endBatch.y
                                                   && batchIndex.z >= VoxelWorld.startBatch.z &&
                                                   batchIndex.z <= VoxelWorld.endBatch.z)
            {
                return metaspace[batchIndex].nodes != null;
            }

            return false;
        }


        private int GetLabel(Vector3Int globalBatchIndex) {
            return GetLabel(globalBatchIndex.x, globalBatchIndex.y, globalBatchIndex.z);
        }
        private int GetLabel(int x, int y, int z) {
            int localX = x - VoxelWorld.startBatch.x;
            int localY = y - VoxelWorld.startBatch.y;
            int localZ = z - VoxelWorld.startBatch.z;
            Vector3Int regionSize = VoxelWorld.endBatch - VoxelWorld.startBatch + Vector3Int.one;

            return localY * regionSize.x * regionSize.z + localZ * regionSize.x + localX;
        }
    }
}
