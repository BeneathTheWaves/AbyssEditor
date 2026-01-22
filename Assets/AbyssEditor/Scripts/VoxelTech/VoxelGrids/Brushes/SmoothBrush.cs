using AbyssEditor.VoxelTech;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace AbyssEditor.Scripts.VoxelTech.VoxelGrids.Brushes
{
    public class SmoothBrush : BrushJob
    {
        public DensitySmoothJob job;
        
        public SmoothBrush(VoxelGrid grid, Vector3 brushLocation, float brushRadius, Vector3 gridOrigin) 
            : base(grid, brushLocation, brushRadius, gridOrigin) { }
        
        public override void StartJob(NativeArray<Vector3Int> voxelsToUpdate)
        {
            job = new DensitySmoothJob
            {
                voxelsToUpdate = voxelsToUpdate,
                resultingDensities = new NativeArray<byte>(voxelsToUpdate.Length, Allocator.TempJob),
                resultingTypes = new NativeArray<byte>(voxelsToUpdate.Length, Allocator.TempJob),
                densityGrid = new NativeArray<byte>(grid.densityGrid, Allocator.TempJob),
                typeGrid = new NativeArray<byte>(grid.typeGrid, Allocator.TempJob),
                octreeIndex = grid.octreeIndex,
                batchIndex = grid.batchIndex,
                gridOrigin = gridOrigin,
                brushLocation = brushLocation,
                brushRadius = brushRadius,
            };
            jobHandle = job.ScheduleParallel(voxelsToUpdate.Length, voxelsToUpdate.Length/Globals.threadGroupSize, new JobHandle());
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
            job.densityGrid.Dispose();
            job.typeGrid.Dispose();
        }

        public struct DensitySmoothJob : IJobFor
        {
            [ReadOnly] public NativeArray<byte> densityGrid;
            [ReadOnly] public NativeArray<byte> typeGrid;

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

            private void DensityAction_Smooth(int x, int y, int z, int index)
            {
                // basically Gaussian blur
                // If voxel is outside the brush, skip it
                // offset sample position because full grid
                if (VoxelGrid.SampleDensity_Sphere_Squared(new Vector3(x - 1, y - 1, z - 1) + gridOrigin, brushLocation, brushRadius) < 0) {
                    resultingDensities[index] = GetVoxel(densityGrid, x, y, z);
                    resultingTypes[index] = GetVoxel(typeGrid, x, y, z);
                    return;
                }
                
                int sum = 0;
                int count = 0;

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

                    byte[] voxelData = VoxelMetaspace.metaspace.GetVoxel(voxel, voxelOctree, voxelBatch);
                    if (voxelData[0] == 0 && voxelData[1] != 0) {
                        sum += 252;
                    } else {
                        sum += voxelData[0];
                    }

                    count++;
                }

                sum /= count;
                resultingDensities[index] = (byte) sum;

                // update type as well
                bool solidBefore = GetVoxel(densityGrid, x, y, z) >= 126;
                
                bool solidNow = ((byte) sum) >= 126;
                if (solidNow != solidBefore) {
                    if (solidNow)
                    {
                        resultingTypes[index] = Brush.selectedType;
                    } else {
                        resultingTypes[index] = 0;
                    }
                }
                else
                {
                    resultingTypes[index] = GetVoxel(typeGrid, x, y, z);
                }
            }
            
            public static byte GetVoxel(NativeArray<byte> array, int x, int y, int z) {
                return array[Globals.LinearIndex(x, y, z, VoxelWorld.RESOLUTION + 2)];
            }
        }
    }
}
