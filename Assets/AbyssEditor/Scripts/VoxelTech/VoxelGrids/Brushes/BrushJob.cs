using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
namespace AbyssEditor.Scripts.VoxelTech.VoxelGrids.Brushes
{
    public abstract class BrushJob
    {
        private static Stack<NativeArray<byte>> pooledResultArrays = new ();
        
        protected VoxelGrid grid;
        
        protected Vector3 brushLocation;
        //TODO: should we not just take the stroke and get the vars off that?
        protected float brushRadius;
        protected float brushIntensity;
        protected byte brushSelectedType;
        protected Vector3 gridOrigin;
        
        public JobHandle jobHandle;

        protected BrushJob(VoxelGrid grid, Vector3 brushLocation, float brushRadius, float brushIntensity, byte brushSelectedType, Vector3 gridOrigin)
        {
            this.grid = grid;
            this.brushLocation = brushLocation;
            this.brushRadius = brushRadius;
            this.brushIntensity = brushIntensity;
            this.brushSelectedType = brushSelectedType;
            this.gridOrigin = gridOrigin;
        }
        
        public abstract void StartJob();

        public virtual void OnJobCompleteCleanup()
        {
            Debug.Log($"{pooledResultArrays.Count} pool size");
        }
        
        /// <summary>
        /// Get an array that is either already allocated or newly allocated if none exist
        /// </summary>
        /// <returns>an allocated array, assume data within is random</returns>
        public NativeArray<byte> GetPooledNativeArray()
        {
            if (pooledResultArrays.Count <= 0)
            {
                return new NativeArray<byte>(VoxelGrid.GetGridInnerSize(), Allocator.Persistent);
            }
            return pooledResultArrays.Pop();
        } 
        
        public void ReturnPooledNativeArray(NativeArray<byte> arr)
        {
            pooledResultArrays.Push(arr);
        }
    }
}
