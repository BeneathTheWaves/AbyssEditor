using System;
using Unity.Collections;
using UnityEngine;

namespace AbyssEditor.Scripts.BinaryReadingWriting
{
    public partial class ThreadedBinaryReadWriter
    {
        /// <summary>
        /// The offset should point to the very start of the batch index, so that if bytearray[batchIndexOffset] will return the first byte of the 6 for a index.
        /// </summary>
        public static void GetPatchOctreesThreadable(byte[] patchByteArray, int batchIndexOffset, out NativeArray<byte>[] densityGrids, out NativeArray<byte>[] typeGrids)
        {
            int currPos = batchIndexOffset;
            
            short bx = BitConverter.ToInt16(patchByteArray, currPos);
            short by = BitConverter.ToInt16(patchByteArray, currPos + 2);
            short bz = BitConverter.ToInt16(patchByteArray, currPos + 4);
            currPos += 6;
            Vector3Int batchIndex = new(bx, by, bz);
            
            //Obtain Original Batch octrees
            ReadBatchThreadable(batchIndex, out densityGrids, out typeGrids, usePaddedSize: true);
            
            //Read patch bytes, apply the octrees on top of it.
            //TODO: Only load the original octrees that weren't overriden. More complicated to implement, but we are doing extra work than is needed and could marginally speed things up
            
            byte octreeCount = patchByteArray[currPos++];
            for (int i = 0; i < octreeCount; i++)
            {
                byte octreeIndex = patchByteArray[currPos++];
                Debug.Log(octreeIndex);
                ushort nodeCount = BitConverter.ToUInt16(patchByteArray, currPos);
                currPos += 2;
                
                byte[] octree = new byte[nodeCount * OCTREE_NODE_BYTE_SIZE];
                Array.Copy(patchByteArray, currPos, octree, 0, octree.Length);
                currPos+=octree.Length;
                ConvertOctreeToGrid(octree, densityGrids[octreeIndex], typeGrids[octreeIndex]);
            }
        }
    }
}
