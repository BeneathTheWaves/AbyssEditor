using System.IO;
using AbyssEditor.Scripts.Octrees;
using AbyssEditor.Scripts.VoxelTech;
using UnityEngine;


namespace AbyssEditor.Scripts.BinaryReading
{
    public static partial class ThreadedBinaryReadWriter
    {
        public static Octree[,,] ReadBatchThreadable(Vector3Int batchIndex, bool allowModded, bool generateEmpty)
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
    }
}
