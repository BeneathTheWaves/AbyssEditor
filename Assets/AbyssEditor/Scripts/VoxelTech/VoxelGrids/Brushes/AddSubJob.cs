using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace AbyssEditor.Scripts.VoxelTech.VoxelGrids.Brushes
{
    public class AddSubJob : BrushJob
    {
        private readonly bool shouldRemoveDensity;

        public AddSubJob(VoxelGrid grid, Vector3 brushLocation, float brushRadius, float brushIntensity, byte brushSelectedType, Vector3 gridOrigin, bool shouldRemoveDensity)
            : base(grid, brushLocation, brushRadius, brushIntensity, brushSelectedType, gridOrigin)
        {
            this.shouldRemoveDensity = shouldRemoveDensity;
        }
        
        public override void StartJob()
        {
            DensityAddSubJob job = new DensityAddSubJob
            {
                densityGrid = grid.densityGrid,
                typeGrid = grid.typeGrid,
                gridOrigin = gridOrigin,
                brushLocation = brushLocation,
                brushIntensity = brushIntensity,
                brushSelectedType = brushSelectedType,
                shouldRemoveDensity = shouldRemoveDensity,
                brushRadius = brushRadius,
                dim = VoxelWorld.RESOLUTION + 2,
            };

            int voxelsToUpdateCount = VoxelGrid.GetGridInnerSize();
            jobHandle = job.ScheduleParallel(voxelsToUpdateCount, voxelsToUpdateCount/64, new JobHandle());
        }
        
        [BurstCompile]
        private struct DensityAddSubJob : IJobFor
        {
            //its safe here to operate directly on the array in parallel as we know we will modify only 1 value per given index.
            [NativeDisableContainerSafetyRestriction] public NativeArray<byte> densityGrid;
            [NativeDisableContainerSafetyRestriction] public NativeArray<byte> typeGrid;

            [ReadOnly] public float3 gridOrigin;
            [ReadOnly] public float3 brushLocation;
            [ReadOnly] public float brushIntensity;
            [ReadOnly] public float brushRadius;
            [ReadOnly] public byte brushSelectedType;

            [ReadOnly] public bool shouldRemoveDensity;

            [ReadOnly] public int dim;
            
            public void Execute(int index)
            {
                DensityAction_AddSmooth(BrushUtils.Bursted.GetVoxelFromIndex(index, dim));
            }
        
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void DensityAction_AddSmooth(int3 voxel) {
                float functionDensity = BrushUtils.Bursted.SampleDensity_Sphere_Squared(new float3(voxel.x - 1, voxel.y - 1, voxel.z - 1) + gridOrigin, brushLocation, brushRadius);
                float clampedFunctionDensity = math.clamp(functionDensity, -1, 1);

                if (clampedFunctionDensity > 0) {

                    float add = math.sqrt(clampedFunctionDensity) * GetWeight(voxel+ gridOrigin);
                    if (shouldRemoveDensity) add *= -1;
                    VoxelAdd(voxel.x, voxel.y, voxel.z, (int) add, brushSelectedType);
                }
            }
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static float SmoothStep(float t, float min, float max)
            {
                // avoid divide-by-zero
                float invRange = 1f / (max - min);
                t = math.saturate((t - min) * invRange);
                return t * t * (3f - 2f * t);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            float GetWeight(float3 voxelPos) {
                float dist = math.length(brushLocation - voxelPos);

                float falloff = 1f - SmoothStep(dist, brushRadius * 0.7f, brushRadius);

                float scaledRadius = math.pow(brushRadius, 0.8f) * (100f * brushIntensity);
                return falloff * scaledRadius;
            }
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void VoxelAdd(int x, int y, int z, int increment, byte newSolidVoxelType)
            {
                int idx = x + y * dim + z * dim * dim; // compute linear index once

                int distanceValue = densityGrid[idx];
                byte typeValue = typeGrid[idx];

                if (distanceValue == 0)
                    distanceValue = typeValue == 0 ? 0 : 252;

                bool solidBefore = distanceValue >= 126;
                distanceValue += increment;
                bool solidAfter = distanceValue >= 126;

                // clamp distanceValue to [0,252] and store
                densityGrid[idx] = (byte)math.clamp(distanceValue, 0, 252);

                if (solidBefore != solidAfter)
                    typeGrid[idx] = solidAfter ? newSolidVoxelType : (byte)0;
            }
        }
    }
}
