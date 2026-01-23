using System.Numerics;
using AbyssEditor.VoxelTech;
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
                octreeIndex = grid.octreeIndex,
                batchIndex = grid.batchIndex,
                gridOrigin = gridOrigin,
                brushLocation = brushLocation,
                brushIntensity = brushIntensity,
                brushRadius = brushRadius,
            };
            
            int voxelsToUpdateCount = VoxelGrid.GetGridInnerSize();
            jobHandle = job.ScheduleParallel(voxelsToUpdateCount, voxelsToUpdateCount/64, new JobHandle());
        }
        
        public override void OnJobCompleteCleanup()
        {
            int innerSize = VoxelGrid.GetGridInnerSize();
            for (int i = 0; i < innerSize; i++)
            {
                Vector3Int voxel = BrushUtils.Managed.GetVoxelFromIndex(i);
                VoxelGrid.SetVoxel(grid.densityGrid, voxel.x, voxel.y, voxel.z, job.resultingDensities[i]);
                VoxelGrid.SetVoxel(grid.typeGrid, voxel.x, voxel.y, voxel.z, job.resultingTypes[i]);
            }

            ReturnPooledNativeArray(job.resultingDensities);
            ReturnPooledNativeArray(job.resultingTypes);
        }

        private struct DensitySmoothJob : IJobFor
        {
            [NativeDisableContainerSafetyRestriction] public NativeArray<byte> densityGrid;
            [NativeDisableContainerSafetyRestriction] public NativeArray<byte> typeGrid;

            [ReadOnly] public Vector3Int octreeIndex;
            [ReadOnly] public Vector3Int batchIndex;

            [ReadOnly] public Vector3 gridOrigin;
            [ReadOnly] public Vector3 brushLocation;
            [ReadOnly] public float brushRadius;
            [ReadOnly] public float brushIntensity;
            
            //out
            [WriteOnly] public NativeArray<byte> resultingDensities;
            [WriteOnly] public NativeArray<byte> resultingTypes;
            
            public void Execute(int index)
            {
                Vector3Int voxelToUpdate = BrushUtils.Managed.GetVoxelFromIndex(index);
                DensityAction_Smooth(voxelToUpdate, index);
            }

            // basically Gaussian blur
            private void DensityAction_Smooth(Vector3Int voxelToUpdate, int index)
            {
                //we always need these values so might as well get them once
                byte originalType = VoxelGrid.GetVoxel(typeGrid, voxelToUpdate.x, voxelToUpdate.y, voxelToUpdate.z);
                byte originalDensity = VoxelGrid.GetVoxel(densityGrid, voxelToUpdate.x, voxelToUpdate.y, voxelToUpdate.z);
                
                
                // If voxel is outside the brush, skip it
                // offset sample position because full grid
                if (BrushUtils.Managed.SampleDensity_Sphere_Squared(voxelToUpdate - Vector3Int.one+ gridOrigin, brushLocation, brushRadius) < 0)
                {
                    resultingDensities[index] = originalDensity;
                    resultingTypes[index] = originalType;
                    return;
                }
                
                int sum = 0;
                int count = 0;
                
                byte nearestValidType = 0;

                foreach (Vector3Int neighborOffset in VoxelGrid.neighboursToCheckInSmooth)
                {
                    //Vector3Int voxel = new Vector3Int(x + neighborOffset.x, y + neighborOffset.y, z + neighborOffset.z);
                    Vector3Int voxel = voxelToUpdate - (Vector3Int.one * neighborOffset);
                    Vector3Int voxelOctree = octreeIndex;
                    Vector3Int voxelBatch = batchIndex;
                    if (!VoxelMetaspace.VoxelExists(voxel.x, voxel.y, voxel.z)) {
                        // its in another octree
                        Vector3Int neigOffset = VoxelGrid.NeighbourOffsetFromVoxel(voxel.x, voxel.y, voxel.z);
                        voxel = VoxelGrid.IndexMod(voxel + neigOffset * 2, VoxelWorld.RESOLUTION + 2);
                        voxelOctree += neigOffset;
                        if (!VoxelMetaspace.OctreeExists(voxelOctree, batchIndex)) {
                            // its in another batch
                            voxelOctree = VoxelGrid.IndexMod(voxelOctree, VoxelWorld.CONTAINERS_PER_SIDE);
                            voxelBatch += neigOffset;
                            if (!VoxelMetaspace.BatchExists(voxelBatch)) {
                                //doesnt exist
                                continue;
                            }
                        }
                    }
                    
                    VoxelMetaspace.metaspace.GetVoxel(voxel, voxelOctree, voxelBatch, out byte density, out byte type);
                    
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

                //state changes
                if (solidNow != solidBefore)
                {
                    if (solidNow)
                    {
                        resultingTypes[index] = nearestValidType;
                    }
                    else//Air now, density less than 126
                    {
                        // voxel became air
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
