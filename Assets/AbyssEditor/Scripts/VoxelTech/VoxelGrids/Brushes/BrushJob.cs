using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
namespace AbyssEditor.Scripts.VoxelTech.VoxelGrids.Brushes
{
    public abstract class BrushJob
    {
        public VoxelGrid grid;
        
        public Vector3 brushLocation;
        public float brushRadius;
        public float brushIntensity;
        public Vector3 gridOrigin;
        
        public JobHandle jobHandle;

        public BrushJob(VoxelGrid grid, Vector3 brushLocation, float brushRadius, float brushIntensity, Vector3 gridOrigin)
        {
            this.grid = grid;
            this.brushLocation = brushLocation;
            this.brushRadius = brushRadius;
            this.brushIntensity = brushIntensity;
            this.gridOrigin = gridOrigin;
        }
        
        public abstract void StartJob(NativeArray<Vector3Int> voxelsToUpdate);
        
        public abstract void OnJobCompleteCleanup();

        public void EnsureComplete()
        {
            jobHandle.Complete();
        }
    }
}
