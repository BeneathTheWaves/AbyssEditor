using System.Numerics;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

namespace AbyssEditor.Scripts.VoxelTech.VoxelGrids.Brushes
{
    public class SmoothJob : BrushJob
    {
        private DensitySmoothJob job;
        
        public SmoothJob(VoxelGrid grid, Vector3 brushLocation, float brushRadius, float brushIntensity, Vector3 gridOrigin) 
            : base(grid, brushLocation, brushRadius, brushIntensity, 0/*not needed specially*/, gridOrigin) { }
        
        public override void StartJob()
        {
            job = new DensitySmoothJob
            {
                resultingDensities = GetPooledNativeArray(),
                resultingTypes = GetPooledNativeArray(),
                densityGrid = grid.densityGrid,
                typeGrid = grid.typeGrid,
                gridOrigin = gridOrigin,
                brushLocation = brushLocation,
                brushIntensity = brushIntensity,
                brushRadius = brushRadius,
                neighboursToCheckInSmooth = VoxelGrid.neighboursToCheckInSmooth,
                dim = VoxelWorld.GRID_RESOLUTION + 2,
            };
            
            int voxelsToUpdateCount = VoxelGrid.GetGridInnerSize();
            jobHandle = job.ScheduleParallel(voxelsToUpdateCount, voxelsToUpdateCount/64, new JobHandle());
        }
        
        public override void Cleanup()
        {
            ProcessCopyJob(grid.densityGrid, grid.typeGrid, job.resultingDensities, job.resultingTypes);

            ReturnPooledNativeArray(job.resultingDensities);
            ReturnPooledNativeArray(job.resultingTypes);
        }
        
        [BurstCompile]
        private struct DensitySmoothJob : IJobFor
        {
            [NativeDisableContainerSafetyRestriction] public NativeArray<byte> densityGrid;
            [NativeDisableContainerSafetyRestriction] public NativeArray<byte> typeGrid;
            
            [ReadOnly] public float3 gridOrigin;
            [ReadOnly] public float3 brushLocation;
            [ReadOnly] public float brushRadius;
            [ReadOnly] public float brushIntensity;
            
            [ReadOnly] public int dim;

            [ReadOnly] public NativeArray<int3> neighboursToCheckInSmooth;
            //out
            [WriteOnly] public NativeArray<byte> resultingDensities;
            [WriteOnly] public NativeArray<byte> resultingTypes;
            
            public void Execute(int index)
            {
                int3 voxelToUpdate = BrushUtils.Bursted.GetVoxelFromIndex(index, dim);
                DensityAction_Smooth(voxelToUpdate, index);
            }

            // basically Gaussian blur
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void DensityAction_Smooth(int3 voxel, int index)
            {
                //we always need these values so might as well get them once
                byte originalType = BrushUtils.Bursted.GetVoxelData(typeGrid, voxel, dim);
                byte originalDensity = BrushUtils.Bursted.GetVoxelData(densityGrid, voxel, dim);
                
                
                // If voxel is outside the brush, skip it
                // offset sample position because full grid
                if (BrushUtils.Bursted.SampleDensity_Sphere_Squared(new float3(voxel.x - 1, voxel.y - 1, voxel.z - 1)  + gridOrigin, brushLocation, brushRadius) < 0)
                {
                    resultingDensities[index] = originalDensity;
                    resultingTypes[index] = originalType;
                    return;
                }
                
                int sum = 0;
                int count = 0;
                
                byte nearestValidType = 0;

                foreach (int3 neighborOffset in neighboursToCheckInSmooth)
                {
                    int3 neighborVoxel = voxel - neighborOffset;
                    
                    byte density = BrushUtils.Bursted.GetVoxelData(densityGrid, neighborVoxel, dim);
                    byte type = BrushUtils.Bursted.GetVoxelData(typeGrid, neighborVoxel, dim);
                        
                    if (density == 0 && type != 0) {
                        sum += 252;
                    } else {
                        sum += density;
                    }

                    count++;
                    
                    //Store a solid neighbor type to use later
                    if (nearestValidType == 0 && type != 0)
                    {
                        nearestValidType = type;
                    }
                }
                
                sum /= count;
                
                //It's possible for tiles to have their density set incorrectly despite being solid, so treat them as max density
                if (originalDensity == 0 && originalType != 0)
                {
                    originalDensity = 252;
                }
                
                byte newDensity = (byte) Mathf.Lerp(originalDensity, sum, brushIntensity);
                
                bool solidBefore = originalDensity >= 126;
                bool solidNow = newDensity >= 126;

                //state change
                if (solidNow != solidBefore)
                {
                    if (solidNow)
                    {
                        resultingTypes[index] = nearestValidType;
                    }
                    else
                    {
                        resultingTypes[index] = 0;
                    }
                }
                else
                {
                    resultingTypes[index] = originalType;
                }
                
                
                resultingDensities[index] = newDensity;
            }
        }
    }
}
