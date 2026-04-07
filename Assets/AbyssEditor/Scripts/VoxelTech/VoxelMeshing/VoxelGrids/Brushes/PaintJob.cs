using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace AbyssEditor.Scripts.VoxelTech.VoxelMeshing.VoxelGrids.Brushes
{
    public class PaintJob : BrushJob
    {
        public PaintJob(VoxelGrid grid, Vector3 brushLocation, float brushRadius, byte brushSelectedType, Vector3 gridOrigin)
            : base(grid, brushLocation, brushRadius, 0, brushSelectedType, gridOrigin) { }
        
        public override void StartJob()
        {
            DensityPaintJob job = new DensityPaintJob
            {
                densityGrid = grid.densityGrid,
                typeGrid = grid.typeGrid,
                gridOrigin = gridOrigin,
                brushLocation = brushLocation,
                brushSelectedType = brushSelectedType,
                brushRadius = brushRadius,
                dim = VoxelWorld.GRID_RESOLUTION + 2,
            };

            int voxelsToUpdateCount = VoxelGrid.GetGridInnerSize();
            jobHandle = job.ScheduleParallel(voxelsToUpdateCount, voxelsToUpdateCount/64, new JobHandle());
        }

        [BurstCompile]
        private struct DensityPaintJob : IJobFor
        {
            [NativeDisableContainerSafetyRestriction] public NativeArray<byte> densityGrid;
            [NativeDisableContainerSafetyRestriction] public NativeArray<byte> typeGrid;

            [ReadOnly] public float3 gridOrigin;
            [ReadOnly] public float3 brushLocation;
            [ReadOnly] public byte brushSelectedType;
            [ReadOnly] public float brushRadius;

            [ReadOnly] public int dim;
            
            public void Execute(int index)
            {
                DensityAction_Paint(BrushUtils.Bursted.GetVoxelFromIndex(index, dim));
            }
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            void DensityAction_Paint(int3 voxel) {
                if (BrushUtils.Bursted.SampleDensity_Sphere_Squared(new float3(voxel.x - 1, voxel.y - 1, voxel.z - 1) + gridOrigin, brushLocation, brushRadius) < 0)
                {
                    //Voxel outside brush
                    return;
                }

                byte voxelDensity = BrushUtils.Bursted.GetVoxelData(densityGrid, voxel, dim);
                byte originalVoxelType = BrushUtils.Bursted.GetVoxelData(typeGrid, voxel, dim);

                if (voxelDensity == 0 && originalVoxelType != 0)
                {
                    voxelDensity = 252;
                }
                
                if (voxelDensity >= 126) {
                    BrushUtils.Bursted.SetVoxelData(typeGrid, voxel, dim, brushSelectedType);
                }
            }
        }
    }
}
