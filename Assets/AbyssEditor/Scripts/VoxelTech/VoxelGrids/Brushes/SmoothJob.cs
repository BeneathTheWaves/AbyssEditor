using AbyssEditor.VoxelTech;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace AbyssEditor.Scripts.VoxelTech.VoxelGrids.Brushes
{
    public class SmoothJob : BrushJob
    {
        public DensitySmoothJob job;
        
        public SmoothJob(VoxelGrid grid, Vector3 brushLocation, float brushRadius, float brushIntensity, Vector3 gridOrigin) 
            : base(grid, brushLocation, brushRadius, brushIntensity, 0/*not needed specially*/, gridOrigin) { }
        
        public override void StartJob(NativeArray<int3> voxelsToUpdate)
        {
            job = new DensitySmoothJob
            {
                voxelsToUpdate = voxelsToUpdate,
                resultingDensities = new NativeArray<byte>(voxelsToUpdate.Length, Allocator.TempJob),
                resultingTypes = new NativeArray<byte>(voxelsToUpdate.Length, Allocator.TempJob),
                densityGrid = grid.densityGrid,
                typeGrid = grid.typeGrid,
                octreeIndex = grid.octreeIndex,
                batchIndex = grid.batchIndex,
                gridOrigin = gridOrigin,
                brushLocation = brushLocation,
                brushIntensity = brushIntensity,
                brushRadius = brushRadius,
            };
            jobHandle = job.ScheduleParallel(voxelsToUpdate.Length, voxelsToUpdate.Length/64, new JobHandle());
        }
        
        public override void OnJobCompleteCleanup()
        {
            for (int i = 0; i < job.voxelsToUpdate.Length; i++)
            {
                int3 voxel = job.voxelsToUpdate[i];
                VoxelGrid.SetVoxel(grid.densityGrid, voxel.x, voxel.y, voxel.z, job.resultingDensities[i]);
                VoxelGrid.SetVoxel(grid.typeGrid, voxel.x, voxel.y, voxel.z, job.resultingTypes[i]);
            }
                
            job.voxelsToUpdate.Dispose();
            job.resultingDensities.Dispose();
            job.resultingTypes.Dispose();
        }
        
        public struct DensitySmoothJob : IJobFor
        {
            [NativeDisableContainerSafetyRestriction] public NativeArray<byte> densityGrid;
            [NativeDisableContainerSafetyRestriction] public NativeArray<byte> typeGrid;

            [ReadOnly] public Vector3Int octreeIndex;
            [ReadOnly] public Vector3Int batchIndex;

            [ReadOnly] public Vector3 gridOrigin;
            [ReadOnly] public Vector3 brushLocation;
            [ReadOnly] public float brushRadius;
            [ReadOnly] public float brushIntensity;
            
            //in
            [ReadOnly] public NativeArray<int3> voxelsToUpdate;
            
            //out
            [WriteOnly] public NativeArray<byte> resultingDensities;
            [WriteOnly] public NativeArray<byte> resultingTypes;
            
            public void Execute(int index)
            {
                int3 voxelUpdate = voxelsToUpdate[index];
                DensityAction_Smooth(voxelUpdate.x, voxelUpdate.y, voxelUpdate.z, index);
            }

            // basically Gaussian blur
            private void DensityAction_Smooth(int x, int y, int z, int index)
            {
                //we always need these values so might as well get them once
                byte originalType = VoxelGrid.GetVoxel(typeGrid, x, y, z);
                byte originalDensity = VoxelGrid.GetVoxel(densityGrid, x, y, z);
                
                
                // If voxel is outside the brush, skip it
                // offset sample position because full grid
                if (VoxelGrid.SampleDensity_Sphere_Squared(new Vector3(x - 1, y - 1, z - 1) + gridOrigin, brushLocation, brushRadius) < 0)
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
                    Vector3Int voxel = new Vector3Int(x + neighborOffset.x, y + neighborOffset.y, z + neighborOffset.z);
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
