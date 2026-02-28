using System.Collections.Generic;
using System.IO;
using AbyssEditor.Scripts.Octrees;
using AbyssEditor.Scripts.VoxelTech;
using Unity.Collections;
using UnityEngine;


namespace AbyssEditor.Scripts.BinaryReading
{
    public static partial class ThreadedBinaryReadWriter
    {
        /// <summary>
        /// The octrees loaded from a game batch contain a different shape octree than the one that DeRasterizeGrid generates when converting from the density grid back to octrees.
        /// This function is so that when comparing the octree form from the original batches to the one generated from the grid, they both start from the same base.
        /// We are trying to make the data we are comparing match essentially.
        /// </summary>
        /// <param name="tempDensities">Temporary Densities to use, mainly so that new arrays don't need to be allocated for this operation</param>
        /// <param name="tempTypes">Temporary Types to use, mainly so that new arrays don't need to be allocated for this operation</param>
        /// <param name="originalNodes">Original nodes that are directly from a loaded batch, otherwise this isn't going to do much</param>
        /*PUBLIC FOR NOW*/public static void RasterDeRasterizeBatch(NativeArray<byte> tempDensities, NativeArray<byte> tempTypes, Octree[,,] originalNodes)
        {
            //if this becomes a performance issue there is nothing stopping it from becoming a unity job operation, each octree only looks at itself and its grid
            if (originalNodes != null)
            {
                foreach (Octree node in originalNodes)
                {
                    node.Rasterize(tempDensities, tempTypes, VoxelWorld.RESOLUTION, 5 - VoxelWorld.LEVEL_OF_DETAIL);
                    node.DeRasterizeGrid(tempDensities, tempTypes, 0, 5 - VoxelWorld.LEVEL_OF_DETAIL);
                }
            }
        }

        public static byte[] GetPatchBytes(string patchFilePath)
        {
            if (!File.Exists(patchFilePath))
            {
                Debug.LogError($"Patch file not found: {patchFilePath}");
                return null;
            }
            
            BinaryReader reader = new BinaryReader(File.Open(patchFilePath, FileMode.Open));

            uint version = reader.ReadUInt32();
            
            /*if (version == uint.MaxValue)
            {
                Debug.LogError("Invalid patch file");
                reader.Close();
                yield break;
            }*/
            int curr_pos = 0;
            long payloadLength = reader.BaseStream.Length - 4; // exclude version
            byte[] patchByteArray = new byte[payloadLength];

            while (curr_pos < payloadLength)
            {
                patchByteArray[curr_pos++] = reader.ReadByte();
            }
            
            reader.Close();
            
            return patchByteArray;
        }
        
        public static void GetBatchIndexesAndOffsetsFromPatch(byte[] data, out List<Vector3Int> batchesInPatch, out List<int> batchOffsetsInPatch)
        {
            int pos = 0;
            List<Vector3Int> batchInPatch = new List<Vector3Int>();
            List<int> offsetsInPatch = new List<int>();

            while (pos < data.Length)
            {
                //batch index
                offsetsInPatch.Add(pos);
                Vector3Int index = new Vector3Int(
                    (short)(data[pos] | (data[pos + 1] << 8)),
                    (short)(data[pos + 2] | (data[pos + 3] << 8)),
                    (short)(data[pos + 4] | (data[pos + 5] << 8)));
                
                batchInPatch.Add(index);
                
                pos += 6;

                byte octreeCount = data[pos++];

                for (int i = 0; i < octreeCount; i++)
                {
                    //octreeIndex
                    pos += 1;
                    // read nodeCount
                    ushort nodeCount = (ushort)(data[pos] | (data[pos + 1] << 8));
                    pos += 2;
                    //nodes
                    pos += nodeCount * 4;
                }
            }

            batchesInPatch = batchInPatch;
            batchOffsetsInPatch = offsetsInPatch;
        }
    }
}
