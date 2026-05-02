using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using AbyssEditor.Scripts.UI;
using AbyssEditor.Scripts.Util;
using AbyssEditor.Scripts.VoxelTech;
using AbyssEditor.Scripts.VoxelTech.VoxelMeshing;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace AbyssEditor.Scripts.BinaryReadingWriting
{
    public static partial class ThreadedBinaryReadWriter
    {
        public static void ReadBatchThreadable(Vector3Int batchIndex, out NativeArray<byte>[] densityGrids, out NativeArray<byte>[] typeGrids, bool generateEmpty = true, bool usePaddedSize = false)
        {
            int gridSize = usePaddedSize ? VoxelWorld.VOXELS_FLAT_PADDED_GRID : VoxelWorld.VOXELS_FLAT_UNPADDED_GRID;

            int gridPadding = usePaddedSize ? 1 : 0;
            
            string filePath = GetPath(batchIndex, false, out _);
            
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

            using BinaryReader reader = new(File.Open(filePath, FileMode.Open));
            reader.ReadInt32(); // skip version field
            
            densityGrids = new NativeArray<byte>[VoxelWorld.OCTREES_PER_BATCH];
            typeGrids = new NativeArray<byte>[VoxelWorld.OCTREES_PER_BATCH];
            for (int i = 0; i < VoxelWorld.OCTREES_PER_BATCH; i++)
            {
                int treeNodeCount = reader.ReadUInt16();
                int treeByteSize = treeNodeCount * OCTREE_NODE_BYTE_SIZE;
                byte[] octree = reader.ReadBytes(treeByteSize);
                
                int iz = i % VoxelWorld.OCTREES_PER_SIDE;
                int iy = (i / VoxelWorld.OCTREES_PER_SIDE) % VoxelWorld.OCTREES_PER_SIDE;
                int ix = i / (VoxelWorld.OCTREES_PER_SIDE * VoxelWorld.OCTREES_PER_SIDE);
                int containerIndex = Utils.LinearIndex(ix, iy, iz, VoxelWorld.OCTREES_PER_SIDE);

                NativeArray<byte> densityGrid = new(gridSize, Allocator.Persistent);
                NativeArray<byte> typeGrid = new(gridSize, Allocator.Persistent);
                ConvertOctreeToGrid(octree, densityGrid, typeGrid, gridPadding: gridPadding);
                densityGrids[containerIndex] = densityGrid;
                typeGrids[containerIndex] = typeGrid;
            }
            reader.Close();
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
                    int pos = Utils.LinearIndex(gridPadding + ix, gridPadding + iy, gridPadding + iz, VoxelWorld.GRID_RESOLUTION + (2 * gridPadding));
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

        /// <summary>
        /// Writes a "Base Game" compiled .optoctree file.
        /// </summary>
        public static void WriteOptoctreeThreadable(VoxelBatch batch, string exportLocation)
        {
            Vector3Int batchIndex = batch.batchIndex;
            
            string batchname = $"compiled-batch-{batchIndex.x}-{batchIndex.y}-{batchIndex.z}.optoctrees";

            DebugOverlay.LogMessage($"Writing {batchname}");

            BinaryWriter writer = new BinaryWriter(File.Open(Path.Combine(exportLocation, batchname), FileMode.OpenOrCreate));
            writer.Write(4);//Version number
            
            for (int i = 0; i < VoxelWorld.OCTREES_PER_BATCH; i++)
            {
                int containerIndex = Utils.OctreeIndexToContainerIndex(i);

                WriteBatch(writer, batch.pointContainers[containerIndex].grid.densityGrid, batch.pointContainers[containerIndex].grid.typeGrid);
            }

            writer.Close();
        }
        
        public static void WriteBatch(BinaryWriter writer, NativeArray<byte> densityGrid, NativeArray<byte> typeGrid)
        {
            NativeList<OctNode> octreeNodes = ConvertGridToOctree(densityGrid, typeGrid);
            
            NativeArray<byte> octreeBytes = octreeNodes.AsArray().Reinterpret<byte>(UnsafeUtility.SizeOf<OctNode>());
            
            // Node count as ushort
            writer.Write((ushort)(octreeNodes.Length));
            writer.Write(octreeBytes.AsReadOnlySpan());
            
            octreeNodes.Dispose();
            octreeBytes.Dispose();
        }
        
        /// <summary>
        /// Converts a full 34x34x34 grid into an octree byte sequence
        /// </summary>
        private static NativeList<OctNode> ConvertGridToOctree(
            NativeArray<byte> densityGrid,
            NativeArray<byte> typeGrid,
            int gridPadding = 1)
        {
            // Each node is 4 bytes as a struct.
            // We can modify the nodes afterward.
            NativeList<OctNode> nodes = new NativeList<OctNode>(Allocator.Persistent);

            // Queue allow checking parent nodes before adding child nodes
            // Breath First in a sense as nodes must be listed top down from the tree
            Queue<(int nodeIdx, int x, int y, int z, int width)> queue = new();

            //Add root node
            nodes.Add(new OctNode());
            //Enqueue it to start off the "recursive" sequence
            queue.Enqueue((0, 0, 0, 0, VoxelWorld.GRID_RESOLUTION));//start at 32 res

            while (queue.Count > 0)
            {
                (int nodeIdx, int x, int y, int z, int width) = queue.Dequeue();

                // Sample the region to determine dominant type, average density, uniformity
                SampleRegion(densityGrid, typeGrid,
                    x, y, z, width, gridPadding,
                    out byte dominantType,
                    out byte avgDensity,
                    out bool isUniform);

                
                if (isUniform || width == 1)
                {
                    ref OctNode octNode = ref nodes.ElementAt(nodeIdx);
                    // leaf Node, child index must be set to 0
                    octNode.type = dominantType;
                    octNode.density = avgDensity;
                    octNode.childIndex = 0;
                }
                else
                {
                    // Internal node — reserve 8 consecutive child slots
                    ushort firstChildIdx = (ushort)nodes.Length;
                    for (int c = 0; c < 8; c++)
                        nodes.Add(new OctNode());

                    //We can only get a reference to the parent node AFTER we add the children.
                    //When nativeList reallocates its memory to expand, the ref to the struct can become invalid and cause undefined behavior
                    ref OctNode octNode = ref nodes.ElementAt(nodeIdx);
                    
                    // Modify parent node to point to children.
                    octNode.type = dominantType;
                    octNode.density = avgDensity;
                    octNode.childIndex = firstChildIdx;

                    // Enqueue 8 children 
                    int half = width / 2;
                    for (int i = 0; i < 8; i++)
                    {
                        Vector3Int offset = cornerOffsets[i] * half;
                        queue.Enqueue((firstChildIdx + i, x + offset.x, y + offset.y, z + offset.z, half));
                    }
                }
            }
            return nodes;
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
            int x, int y, int z, int regionWidth,
            int gridPadding,
            out byte dominantType,
            out byte avgDensity,
            out bool isUniform)
        {
            long densitySum = 0;
            int sampleCount = 0;
            
            // Allocate it to the stack for efficiently reasons. A dictionary is too slow but 256 bytes fits easily on the stack
            Span<int> typeDictionary = stackalloc int[254];//255 is reserved and won't ever show up so we can ignore it

            byte firstType = 0;
            byte firstDensity = 0;
            bool first = true;
            isUniform = true;

            for (int ix = x; ix < x + regionWidth; ix++)
            for (int iy = y; iy < y + regionWidth; iy++)
            for (int iz = z; iz < z + regionWidth; iz++)
            {
                int pos = Utils.LinearIndex(
                    gridPadding + ix,
                    gridPadding + iy,
                    gridPadding + iz,
                    VoxelWorld.PADDED_GRID_RESOLUTION);

                byte density = densityGrid[pos];
                byte type = typeGrid[pos];
                
                if (density == 0 && type != 0)
                    density = 252;//special case, when the type not 0 but the density is 0 treat it as 252
                
                typeDictionary[type]++;
                densitySum += density;
                sampleCount++;

                if (first)
                {
                    firstType = type;
                    firstDensity = density;
                    first = false;
                }
                else if (isUniform && (type != firstType || density != firstDensity))
                {
                    isUniform = false;
                }
            }
            
            avgDensity = sampleCount > 0 ? (byte)(densitySum / sampleCount) : (byte)0;
            dominantType = 0;
            
            if (avgDensity < 126) return;
            //determine the best possible material that is non air
            int maxCount = 0;
            //start iterating at 1 as the terrain is solid
            for (int t = 1; t < 254; t++)
            {
                if (typeDictionary[t] <= maxCount) continue;
                
                maxCount = typeDictionary[t];
                dominantType = (byte)t;

            }
        }
        
        //ensure this struct is stored in memory like an array of bytes
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct OctNode
        {
            public byte type;
            public byte density;
            public ushort childIndex;
        }
    }
}
