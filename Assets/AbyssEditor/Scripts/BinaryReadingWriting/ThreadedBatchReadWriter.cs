using System;
using System.Collections.Generic;
using System.IO;
using AbyssEditor.Scripts.Util;
using AbyssEditor.Scripts.VoxelTech;
using Unity.Collections;
using UnityEngine;

namespace AbyssEditor.Scripts.BinaryReadingWriting
{
    public static partial class ThreadedBinaryReadWriter
    {
        public static void ReadBatchThreadable(Vector3Int batchIndex, out NativeArray<byte>[] densityGrids, out NativeArray<byte>[] typeGrids, bool generateEmpty = true, bool usePaddedSize = false)
        {
            int gridSize = usePaddedSize ? VoxelWorld.VOXELS_FLAT_PADDED_GRID : VoxelWorld.VOXELS_FLAT_UNPADDED_GRID;

            int gridPadding = usePaddedSize ? 1 : 0;
            
            string filePath = ThreadedBinaryReadWriter.GetPath(batchIndex, false, out _);
            
            if (!File.Exists(filePath))
            {
                if (generateEmpty) GenerateEmptyGrids(gridSize, out densityGrids, out typeGrids);
                else
                {
                    densityGrids = null;
                    typeGrids = null;
                }
                return;
            }

            BinaryReader reader = new(File.Open(filePath, FileMode.Open));
            reader.ReadInt32(); // skip version field
            
            densityGrids = new NativeArray<byte>[VoxelWorld.OCTREES_PER_BATCH];
            typeGrids = new NativeArray<byte>[VoxelWorld.OCTREES_PER_BATCH];
            
            for (int i = 0; i < VoxelWorld.OCTREES_PER_BATCH; i++)
            {
                int treeNodeCount = reader.ReadUInt16();
                int treeByteSize = treeNodeCount * OCTREE_NODE_BYTE_SIZE;
                byte[] octree = reader.ReadBytes(treeByteSize);
                
                NativeArray<byte> densityGrid = new(gridSize, Allocator.Persistent);
                NativeArray<byte> typeGrid = new(gridSize, Allocator.Persistent);

                ConvertOctreeToGrid(octree, densityGrid, typeGrid, gridPadding: gridPadding);
                densityGrids[i] = densityGrid;
                typeGrids[i] = typeGrid;
            }
        }

        /// <summary>
        /// Recursively converts a singular octree from an optoctree file into a dense 3d grid
        /// The densityGrid and typeGrid you supply will be filled out and be of the correct size
        /// </summary>
        private static void ConvertOctreeToGrid(
            byte[] data,
            NativeArray<byte> densityGrid,
            NativeArray<byte> typeGrid,
            int currentNodeIndex = 0, 
            int currentNodeRegionWidth = 32,
            int x = 0, int y = 0, int z = 0,
            int gridPadding = 1)
        {
            int nodeByteOffset = currentNodeIndex * 4;
            
            byte type = data[nodeByteOffset];
            byte density = data[nodeByteOffset + 1];
            ushort startingChildIndex = BitConverter.ToUInt16(data, nodeByteOffset + 2);
            
            // Leaf Node, safe to fill in the grid from here
            if (startingChildIndex == 0)
            {
                for (int ix = x; ix < x + currentNodeRegionWidth; ix++)
                for (int iy = y; iy < y + currentNodeRegionWidth; iy++)
                for (int iz = z; iz < z + currentNodeRegionWidth; iz++)
                {
                    int pos = Util.Utils.LinearIndex(gridPadding + ix, gridPadding + iy, gridPadding + iz, VoxelWorld.GRID_RESOLUTION + (2 * gridPadding));
                    densityGrid[pos] = density;
                    typeGrid[pos] = type;
                }
                return;
            }
            
            // Region size per node is halved, the area one covers decreases by a factor of 8 but the perimeter only by 2
            // In other words the side length of the region this node represents is half when subdividing
            int half = currentNodeRegionWidth / 2; 
            
            /*
               0	000
               1	001
               2	010
               3	011
               4	100
               5	101
               6	110
               7	111
             */
            for (byte i = 0; i < 8; i++)
            {
                int childIndex = startingChildIndex + i;
                
                //bitwise-and operations. Explanations simplify to 3 bits instead of the entire int but the idea is there.
                int ox = (i & 4) != 0 ? half : 0;//looks for 3rd bit, determines left or right
                //for example if i = 1, in binary it's represented as 001. 4 is 100. So the resulting bitwise-and of the two is 000 which = 0 and thus the offset is set to 0
                //Alternatively if i = 5, in binary it's 101, so when bitwise-and with 100 or 4, the result is 100. This does not equal 0 so the iteration is shifted over half.
                int oy = (i & 2) != 0 ? half : 0;//looks for 2nd bit, determines bottom or top
                int oz = (i & 1) != 0 ? half : 0;//looks for first bit, determines front or back

                ConvertOctreeToGrid(data, densityGrid, typeGrid, childIndex, half, x + ox, y + oy, z + oz, gridPadding);
            }
        }

        
        /// <summary>
        /// Generates empty grids for a batch, with every byte set to 0 for density and 0 for typy, making everything air.
        /// </summary>
        private static void GenerateEmptyGrids(int gridSize, out NativeArray<byte>[] densityGrids, out NativeArray<byte>[] typeGrids)
        {
            densityGrids = new NativeArray<byte>[VoxelWorld.OCTREES_PER_BATCH];
            typeGrids = new NativeArray<byte>[VoxelWorld.OCTREES_PER_BATCH];
            for (int i = 0; i < VoxelWorld.OCTREES_PER_BATCH; i++)
            {
                densityGrids[i] = new NativeArray<byte>(gridSize, Allocator.Persistent);
                typeGrids[i] = new NativeArray<byte>(gridSize, Allocator.Persistent);
            }
        }
        
        public static void WriteBatchThreadable(
            BinaryWriter writer,
            NativeArray<byte> densityGrid,
            NativeArray<byte> typeGrid)
        {
            byte[] octreeBytes = ConvertGridToOctree(densityGrid, typeGrid);
                
            // Node count as ushort
            int nodeCount = octreeBytes.Length / OCTREE_NODE_BYTE_SIZE;
            writer.Write((ushort)nodeCount);
            writer.Write(octreeBytes);
        }
        
        private static byte[] ConvertGridToOctree(
            NativeArray<byte> densityGrid,
            NativeArray<byte> typeGrid,
            int gridPadding = 1)//
        {
            // Each node is 4 bytes: [type, density, childLo, childHi]
            // We use a list so we can patch child indices after the fact.
            List<byte[]> nodes = new();

            // BFS queue: (nodeListIndex, regionX, regionY, regionZ, regionWidth)
            Queue<(int nodeIdx, int x, int y, int z, int width)> queue = new();

            // --- Root node (placeholder, will be patched) ---
            nodes.Add(new byte[4]);
            queue.Enqueue((0, 0, 0, 0, VoxelWorld.GRID_RESOLUTION)); // 32

            while (queue.Count > 0)
            {
                var (nodeIdx, x, y, z, width) = queue.Dequeue();

                // Sample the region to determine dominant type, average density, uniformity
                SampleRegion(densityGrid, typeGrid,
                    x, y, z, width, gridPadding,
                    out byte dominantType,
                    out byte avgDensity,
                    out bool isUniform);

                if (isUniform || width == 1)
                {
                    // Leaf node — child index stays 0
                    nodes[nodeIdx][0] = dominantType;
                    nodes[nodeIdx][1] = avgDensity;
                    nodes[nodeIdx][2] = 0;
                    nodes[nodeIdx][3] = 0;
                }
                else
                {
                    // Internal node — reserve 8 consecutive child slots
                    int firstChildIdx = nodes.Count;
                    for (int c = 0; c < 8; c++)
                        nodes.Add(new byte[4]);

                    // Patch current node
                    nodes[nodeIdx][0] = dominantType;
                    nodes[nodeIdx][1] = avgDensity;
                    nodes[nodeIdx][2] = (byte)(firstChildIdx & 0xFF);
                    nodes[nodeIdx][3] = (byte)((firstChildIdx >> 8) & 0xFF);

                    // Enqueue 8 children using the same xyz bit-mapping as the reader
                    int half = width / 2;
                    for (int i = 0; i < 8; i++)
                    {
                        Vector3Int offset = cornerOffsets[i] * half;
                        queue.Enqueue((firstChildIdx + i, x + offset.x, y + offset.y, z + offset.z, half));
                    }
                }
            }

            // Flatten node list into a byte array
            byte[] result = new byte[nodes.Count * OCTREE_NODE_BYTE_SIZE];
            for (int i = 0; i < nodes.Count; i++)
                Buffer.BlockCopy(nodes[i], 0, result, i * OCTREE_NODE_BYTE_SIZE, OCTREE_NODE_BYTE_SIZE);

            return result;
        }
        
        private static readonly Vector3Int[] cornerOffsets = {
            new (0, 0, 0),
            new (0, 0, 1),
            new (0, 1, 0),
            new (0, 1, 1),
            new (1, 0, 0),
            new (1, 0, 1),
            new (1, 1, 0),
            new (1, 1, 1)
        };
        
        private static void SampleRegion(
            NativeArray<byte> densityGrid,
            NativeArray<byte> typeGrid,
            int x, int y, int z, int width,
            int gridPadding,
            out byte dominantType,
            out byte avgDensity,
            out bool isUniform)
        {
            long densitySum = 0;
            int sampleCount = 0;

            // Tally type frequencies
            // Small fixed array is cheaper than a Dictionary for 254 materials
            Span<int> typeCounts = stackalloc int[256];

            byte firstType = 0;
            byte firstDensity = 0;
            bool first = true;
            bool uniform = true;

            for (int ix = x; ix < x + width; ix++)
            for (int iy = y; iy < y + width; iy++)
            for (int iz = z; iz < z + width; iz++)
            {
                int pos = Utils.LinearIndex(
                    gridPadding + ix,
                    gridPadding + iy,
                    gridPadding + iz,
                    VoxelWorld.PADDED_GRID_RESOLUTION);

                byte d = densityGrid[pos];
                byte t = typeGrid[pos];

                typeCounts[t]++;
                densitySum += d;
                sampleCount++;

                if (first)
                {
                    firstType = t;
                    firstDensity = d;
                    first = false;
                }
                else if (uniform && (t != firstType || d != firstDensity))
                {
                    uniform = false;
                }
            }

            isUniform = uniform;
            avgDensity = sampleCount > 0 ? (byte)(densitySum / sampleCount) : (byte)0;

            // Find most frequent type
            dominantType = 0;
            int maxCount = 0;
            for (int t = 0; t < 256; t++)
            {
                if (typeCounts[t] > maxCount)
                {
                    maxCount = typeCounts[t];
                    dominantType = (byte)t;
                }
            }
        }
    }
}
