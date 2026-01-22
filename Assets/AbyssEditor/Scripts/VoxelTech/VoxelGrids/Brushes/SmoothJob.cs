using AbyssEditor.VoxelTech;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;

namespace AbyssEditor.Scripts.VoxelTech.VoxelGrids.Brushes
{
    public class SmoothJob : BrushJob
    {
        public DensitySmoothJob job;
        
        public SmoothJob(VoxelGrid grid, Vector3 brushLocation, float brushRadius, Vector3 gridOrigin) 
            : base(grid, brushLocation, brushRadius, gridOrigin) { }
        
        public override void StartJob(NativeArray<Vector3Int> voxelsToUpdate)
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
                brushRadius = brushRadius,
            };
            jobHandle = job.ScheduleParallel(voxelsToUpdate.Length, voxelsToUpdate.Length/64, new JobHandle());
        }
        
        public override void OnJobCompleteCleanup()
        {
            for (int i = 0; i < job.voxelsToUpdate.Length; i++)
            {
                Vector3Int voxel = job.voxelsToUpdate[i];
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
            
            //in
            [ReadOnly] public NativeArray<Vector3Int> voxelsToUpdate;
            
            //out
            [WriteOnly] public NativeArray<byte> resultingDensities;
            [WriteOnly] public NativeArray<byte> resultingTypes;
            
            public void Execute(int index)
            {
                Vector3Int voxelUpdate = voxelsToUpdate[index];
                DensityAction_Smooth(voxelUpdate.x, voxelUpdate.y, voxelUpdate.z, index);
            }

            // basically Gaussian blur
            private void DensityAction_Smooth(int x, int y, int z, int index)
            {
                // If voxel is outside the brush, skip it
                // offset sample position because full grid
                if (VoxelGrid.SampleDensity_Sphere_Squared(new Vector3(x - 1, y - 1, z - 1) + gridOrigin, brushLocation, brushRadius) < 0) {
                    resultingDensities[index] = VoxelGrid.GetVoxel(densityGrid, x, y, z);
                    resultingTypes[index] = VoxelGrid.GetVoxel(typeGrid, x, y, z);
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

                bool solidBefore = VoxelGrid.GetVoxel(densityGrid, x, y, z) >= 126;
                bool solidNow = (byte) sum >= 126;

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
                    resultingTypes[index] = VoxelGrid.GetVoxel(typeGrid, x, y, z);
                }
                
                
                resultingDensities[index] = (byte) sum;
            }
        }
    }
}
