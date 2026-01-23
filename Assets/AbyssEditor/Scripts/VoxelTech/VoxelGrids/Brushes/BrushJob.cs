using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
namespace AbyssEditor.Scripts.VoxelTech.VoxelGrids.Brushes
{
    public abstract class BrushJob
    {
        public VoxelGrid grid;
        
        public Vector3 brushLocation;
        //TODO: should we not just take the stroke and get the vars off that?
        public float brushRadius;
        public float brushIntensity;
        public byte brushSelectedType;
        public Vector3 gridOrigin;
        
        public JobHandle jobHandle;

        public BrushJob(VoxelGrid grid, Vector3 brushLocation, float brushRadius, float brushIntensity, byte brushSelectedType, Vector3 gridOrigin)
        {
            this.grid = grid;
            this.brushLocation = brushLocation;
            this.brushRadius = brushRadius;
            this.brushIntensity = brushIntensity;
            this.brushSelectedType = brushSelectedType;
            this.gridOrigin = gridOrigin;
        }
        
        public abstract void StartJob(NativeArray<int3> voxelsToUpdate);
        
        public abstract void OnJobCompleteCleanup();

        public void EnsureComplete()
        {
            jobHandle.Complete();
        }
    }
}
