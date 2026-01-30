using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace AbyssEditor.Scripts.VoxelTech.VoxelGrids.Brushes
{
    public abstract class BrushJob
    {
        private static Stack<NativeArray<byte>> pooledResultArrays = new();

        protected VoxelGrid grid;

        protected Vector3 brushLocation;

        //TODO: should we not just take the stroke and get the vars off that?
        protected readonly float brushRadius;
        protected readonly float brushIntensity;
        protected readonly byte brushSelectedType;
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

        /// <summary>
        /// 
        /// </summary>
        public static void DisposeNativeArrayPool()
        {
            foreach (NativeArray<byte> jobArray in pooledResultArrays)
            {
                jobArray.Dispose();
            }
            pooledResultArrays.Clear();
        }


        protected void ProcessCopyJob(NativeArray<byte> densityGrid, NativeArray<byte> typeGrid, NativeArray<byte> resultingDensities, NativeArray<byte> resultingTypes)
        {
            ArrayCopyJob job = new ArrayCopyJob()
            {
                densityGrid = densityGrid,
                typeGrid = typeGrid,
                resultingDensities = resultingDensities,
                resultingTypes = resultingTypes,
                dim = VoxelWorld.RESOLUTION + 2,
            };
            JobHandle handle = job.ScheduleParallel(VoxelGrid.GetGridInnerSize(), VoxelGrid.GetGridInnerSize() / 128, new JobHandle());
            handle.Complete();
        }

        /// <summary>
        /// yes, somehow its like 5x faster to copy an array with a job lmao
        /// </summary>
        [BurstCompile]
        protected struct ArrayCopyJob : IJobFor
        {
            //Copy into
            [NativeDisableParallelForRestriction] public NativeArray<byte> densityGrid;
            [NativeDisableParallelForRestriction] public NativeArray<byte> typeGrid;
                
            [ReadOnly] public int dim;
            //read from
            [ReadOnly] public NativeArray<byte> resultingDensities;
            [ReadOnly] public NativeArray<byte> resultingTypes;
            public void Execute(int index)
            {
                int3 voxel = BrushUtils.Bursted.GetVoxelFromIndex(index, dim);
                    
                BrushUtils.Bursted.SetVoxelData(densityGrid, voxel, dim, resultingDensities[index]);
                BrushUtils.Bursted.SetVoxelData(typeGrid, voxel, dim, resultingTypes[index]);
            }
        }
    }
}
