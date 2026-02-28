using AbyssEditor.Scripts.Octrees;
using AbyssEditor.Scripts.VoxelTech;
using Unity.Collections;
using UnityEngine;


namespace AbyssEditor.Scripts.BinaryReading
{
    public partial class ThreadedBinaryReadWriter
    {
        /// <summary>
        /// The offset should point to the very start of the batch index, so that if bytearray[batchIndexOffset] will return the first byte of the 6 for a index.
        /// </summary>
        public static Octree[,,] GetPatchOctreesThreadable(byte[] patchByteArray, int batchIndexOffset)
        {
            int curr_pos = batchIndexOffset;
            
            short bx = (short)(patchByteArray[curr_pos] | (patchByteArray[curr_pos + 1] << 8));
            short by = (short)(patchByteArray[curr_pos + 2] | (patchByteArray[curr_pos + 3] << 8));
            short bz = (short)(patchByteArray[curr_pos + 4] | (patchByteArray[curr_pos + 5] << 8));
            curr_pos += 6;
            Vector3Int batchIndex = new(bx, by, bz);
            
            //Obtain Original Batch octrees            
            Octree[,,] batchOctrees = ReadBatchThreadable(batchIndex, allowModded: true, generateEmpty: true);
            
            //Read patch bytes to get changed octrees
            byte octreeCount = patchByteArray[curr_pos++];
            for (int i = 0; i < octreeCount; i++)
            {
                byte octreeIndex = patchByteArray[curr_pos++];
                ushort nodeCount = (ushort)(patchByteArray[curr_pos] | (patchByteArray[curr_pos + 1] << 8));
                curr_pos += 2;

                OctNodeData[] nodes = new OctNodeData[nodeCount];

                for (int n = 0; n < nodeCount; n++)
                {
                    byte type = patchByteArray[curr_pos];
                    byte dist = patchByteArray[curr_pos + 1];
                    ushort child = (ushort)(patchByteArray[curr_pos + 2] | (patchByteArray[curr_pos + 3] << 8));

                    nodes[n] = new OctNodeData(type, dist, child); 
                    curr_pos += 4;
                }

                // octreeIndex z,y,x
                int z = octreeIndex % 5;
                int y = (octreeIndex / 5) % 5;
                int x = octreeIndex / 25;

                Octree octree = new Octree(x, y, z, VoxelWorld.OCTREE_WIDTH, batchIndex * VoxelWorld.BATCH_WIDTH);

                octree.Write(nodes);
                batchOctrees[z, y, x] = octree;
            }
            
            return batchOctrees;
        }
    }
}
