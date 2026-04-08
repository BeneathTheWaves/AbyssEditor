using System;
using System.IO;
using AbyssEditor.Scripts.Octrees;
using AbyssEditor.Scripts.VoxelTech;
using AbyssEditor.Scripts.VoxelTech.VoxelMeshing.VoxelGrids;
using Unity.Collections;
using UnityEngine;


namespace AbyssEditor.Scripts.BinaryReading
{
    public static partial class ThreadedBinaryReadWriter
    {
        private static Octree[,,] ReadBatchThreadable(Vector3Int batchIndex, bool allowModded, bool generateEmpty)
        {
            Vector3Int octreeDimensions = Vector3Int.one * VoxelWorld.CONTAINERS_PER_SIDE;
            if (batchIndex.x == 25) octreeDimensions.x = 3;
            if (batchIndex.z == 25) octreeDimensions.z = 3;
            Octree[,,] octrees = new Octree[octreeDimensions.z, octreeDimensions.y, octreeDimensions.x];
            int countOctrees = 0;
            int expectedOctrees = octreeDimensions.x * octreeDimensions.y * octreeDimensions.z;

            var filePath = BatchReadWriter.GetPath(batchIndex, allowModded, out _);
            if (File.Exists(filePath))
            {
                BinaryReader reader = new BinaryReader(File.Open(filePath, FileMode.Open));
                reader.ReadInt32();

                // assemble a data array
                int curr_pos = 0;
                long dataLength = reader.BaseStream.Length - 4;
                byte[] data = reader.ReadBytes((int)dataLength);

                reader.Close();

                curr_pos = 0;

                while (curr_pos < data.Length && countOctrees < expectedOctrees)
                {
                    int x = countOctrees / (octreeDimensions.z * octreeDimensions.y);
                    int y = countOctrees % (octreeDimensions.z * octreeDimensions.y) / octreeDimensions.z;
                    int z = countOctrees % octreeDimensions.z;

                    int nodeCount = data[curr_pos + 1] * 256 + data[curr_pos];
                    // record all nodes of this octree in an array
                    OctNodeData[] nodesOfThisOctree = new OctNodeData[nodeCount];
                    Vector3 batchOrigin = (batchIndex) * (VoxelWorld.BATCH_WIDTH);

                    for (int i = 0; i < nodeCount; ++i)
                    {
                        byte type = data[curr_pos + 2 + i * 4];
                        byte signedDist = data[curr_pos + 3 + i * 4];
                        ushort childIndex = (ushort)(data[curr_pos + 5 + i * 4] * 256 + data[curr_pos + 4 + i * 4]);

                        nodesOfThisOctree[i] = (new OctNodeData(type, signedDist, childIndex));
                    }

                    Octree octree = new Octree(x, y, z, VoxelWorld.OCTREE_WIDTH, batchOrigin);
                    octree.Write(nodesOfThisOctree);
                    octrees[z, y, x] = octree;

                    curr_pos += (nodeCount * 4) + 2;
                    countOctrees++;
                }

                return octrees;
            }

            if (generateEmpty)
            {
                //This is honestly so stupid but ig it works
                return GenerateEmptyTreesForBatch(batchIndex);
            }

            return null;
        }

        public static void NewReadBatchThreadable(Vector3Int batchIndex, out byte[][] densityGrids, out byte[][] typeGrids, bool generateEmpty = true, bool usePaddedSize = false)
        {
            const int OCTREE_NODE_BYTE_SIZE = 4;
            
            int gridSize = usePaddedSize ? VoxelWorld.VOXELS_FLAT_PADDED_GRID : VoxelWorld.VOXELS_FLAT_UNPADDED_GRID;
            
            string filePath = BatchReadWriter.GetPath(batchIndex, false, out _);
            
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
            
            densityGrids = new byte[VoxelWorld.OCTREES_PER_BATCH][];
            typeGrids = new byte[VoxelWorld.OCTREES_PER_BATCH][];
            
            for (int i = 0; i < VoxelWorld.OCTREES_PER_BATCH; i++)
            {
                int treeNodeCount = reader.ReadUInt16();
                int treeByteSize = treeNodeCount * OCTREE_NODE_BYTE_SIZE;
                byte[] octree = reader.ReadBytes(treeByteSize);
                
                byte[] density = new byte[gridSize];
                byte[] type = new byte[gridSize];

                ConvertOctreeToGrid(octree, density, type);
                densityGrids[i] = density;
                typeGrids[i] = type;
            }
        }

        private static void ConvertOctreeToGrid(
            byte[] data,
            byte[] densityGrid,
            byte[] typeGrid,
            int currentNodeIndex = 0, 
            int currentNodeRegionWidth = 32, 
            int x = 0, int y = 0, int z = 0)
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
                    int pos = Globals.LinearIndex(ix, iy, iz, VoxelWorld.GRID_RESOLUTION);
                    densityGrid[pos] = density;
                    typeGrid[pos] = type;
                }
                return;
            }
            
            // 8 distinct Children Below, must parse them individually
            
            // Region size per node is halved, the area one covers decreases by a factor of 8 but the perimeter only by 2
            // In other words the side length of the region this node represents is half when subdividing
            int half = currentNodeRegionWidth / 2; 

            for (int i = 0; i < 8; i++)
            {
                int childIndex = startingChildIndex + i;

                int ox = (i & 4) != 0 ? half : 0;
                int oy = (i & 2) != 0 ? half : 0;
                int oz = (i & 1) != 0 ? half : 0;

                ConvertOctreeToGrid(data, densityGrid, typeGrid, childIndex, half, x + ox, y + oy, z + oz);
            }
        }

        private static void GenerateEmptyGrids(int gridSize, out byte[][] densityGrids, out byte[][] typeGrids)
        {
            densityGrids = new byte[VoxelWorld.OCTREES_PER_BATCH][];
            typeGrids = new byte[VoxelWorld.OCTREES_PER_BATCH][];
            for (int i = 0; i < VoxelWorld.OCTREES_PER_BATCH; i++)
            {
                densityGrids[i] = new byte[gridSize];
                typeGrids[i] = new byte[gridSize];
            }
        }
    }
}
