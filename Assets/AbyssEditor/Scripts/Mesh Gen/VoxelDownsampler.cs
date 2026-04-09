using System.Collections.Generic;
using AbyssEditor.Scripts.Util;
using AbyssEditor.Scripts.VoxelTech.VoxelMeshing.VoxelGrids;
using Unity.Collections;
using UnityEngine;

namespace AbyssEditor.Scripts.Mesh_Gen
{
    public class VoxelDownsampler
    {
        public readonly Dictionary<int, LODGridGroup> lodCacheGrids = new(); 
        
        public VoxelDownsampler()
        {
            for (int i = 1; i <= 5; i++)
            {
                LODGridGroup gridGroup = new();
                gridGroup.blockWidth = 1 << i;
                gridGroup.gridInnerWidth = 1 << (5 - i);
                int gridFullWidth = gridGroup.gridInnerWidth + VoxelGrid.GRID_PADDING*2;
                gridGroup.lodFullResolution = new Vector3Int(gridFullWidth, gridFullWidth, gridFullWidth);
                
                int gridLinearSize = gridFullWidth * gridFullWidth * gridFullWidth;
                
                gridGroup.densityGrid = new NativeArray<byte>(gridLinearSize, Allocator.Persistent);
                gridGroup.typeGrid = new NativeArray<byte>(gridLinearSize, Allocator.Persistent);
                gridGroup.paddingVoxels = PrecomputePaddingVoxels(gridGroup.lodFullResolution);
                lodCacheGrids.Add(i, gridGroup);
            }
        }
        
        public void DownSampleInnerVoxel(NativeArray<byte> originalDensityGrid, NativeArray<byte> originalTypeGrid, Vector3Int originalRes, int sampleBlockWidth, int lodX, int lodY, int lodZ, out byte outDensity, out byte outType)
        {
            int startX = VoxelGrid.GRID_PADDING + lodX * sampleBlockWidth;
            int startY = VoxelGrid.GRID_PADDING + lodY * sampleBlockWidth;
            int startZ = VoxelGrid.GRID_PADDING + lodZ * sampleBlockWidth;

            // Shift the block if it goes beyond the grid
            // moving the sample area back into the padding grid with valid positions to get a general density
            /*if (startX + sampleBlockWidth > originalRes.x) startX = originalRes.x - sampleBlockWidth;
            if (startY + sampleBlockWidth > originalRes.y) startY = originalRes.y - sampleBlockWidth;
            if (startZ + sampleBlockWidth > originalRes.z) startZ = originalRes.z - sampleBlockWidth;*/
            
            int densitySum = 0;
            int count = 0;

            byte nearestValidType = 0;

            for (int x = startX; x < startX + sampleBlockWidth; x++)
            for (int y = startY; y < startY + sampleBlockWidth; y++)
            for (int z = startZ; z < startZ + sampleBlockWidth; z++)
            {
                int index = Utils.LinearIndex(x, y, z, originalRes);

                byte density = originalDensityGrid[index];
                byte type = originalTypeGrid[index];
    
                int effectiveDensity = density;
                if (density == 0 && type != 0)
                    effectiveDensity = 252;

                densitySum += effectiveDensity;
                count++;

                if (nearestValidType == 0 && type != 0)
                    nearestValidType = type;
            }

            outDensity = (byte)(densitySum / count);

            outType = nearestValidType;
        }

        /// <summary>
        /// Only sample voxels for padding with other padding voxels
        /// </summary>
        private static bool IsInnerVoxel(ref Vector3Int res, ref Vector3Int voxel)
        {
            return (voxel.x >= VoxelGrid.GRID_PADDING && voxel.x < res.x - VoxelGrid.GRID_PADDING) &&
                   (voxel.y >= VoxelGrid.GRID_PADDING && voxel.y < res.y - VoxelGrid.GRID_PADDING) &&
                   (voxel.z >= VoxelGrid.GRID_PADDING && voxel.z < res.z - VoxelGrid.GRID_PADDING);
        }
        
        /// <summary>
        /// Precompute the neighbors to check when updating the internal neighboring voxel data
        /// </summary>
        private static NativeArray<Vector3Int> PrecomputePaddingVoxels(Vector3Int res)
        {
            List<Vector3Int> offsets = new List<Vector3Int>();
            
            for (int x = 0; x < res.x; x++)
            for (int y = 0; y < res.y; y++)
            for (int z = 0; z < res.z; z++)
            {
                Vector3Int voxel = new Vector3Int(x, y, z);
                
                if (IsInnerVoxel(ref res, ref voxel)) {
                    continue;
                }
                
                offsets.Add(new Vector3Int(x, y, z));
            }
            
            return new NativeArray<Vector3Int>(offsets.ToArray(), Allocator.Persistent);
        }
        
        public void DisposeNativeArrays()
        {
            foreach (KeyValuePair<int, LODGridGroup> gridGoup in lodCacheGrids)
            {
                gridGoup.Value.densityGrid.Dispose();
                gridGoup.Value.typeGrid.Dispose();
                gridGoup.Value.paddingVoxels.Dispose();
            }
        }
    }
    
    public struct LODGridGroup
    {
        public NativeArray<byte> densityGrid;
        public NativeArray<byte> typeGrid;
        public NativeArray<Vector3Int> paddingVoxels;
        public int gridInnerWidth;
        public int blockWidth;
        public Vector3Int lodFullResolution;
    }
}
