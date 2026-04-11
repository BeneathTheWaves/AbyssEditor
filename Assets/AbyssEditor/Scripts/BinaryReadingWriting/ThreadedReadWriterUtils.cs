using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace AbyssEditor.Scripts.BinaryReadingWriting
{
    public static partial class ThreadedBinaryReadWriter
    {
        private const int OCTREE_NODE_BYTE_SIZE = 4;

        public static string GetPath(Vector3Int batchIndex, bool allowModded, out bool isModded)
        {
            string fileName = $"compiled-batch-{batchIndex.x}-{batchIndex.y}-{batchIndex.z}.optoctrees";

            string vanillaFile = Path.Combine(SnPaths.instance.BatchSourcePath(), fileName);
            isModded = false;
            if (!allowModded)
            {
                return File.Exists(vanillaFile) ? vanillaFile : fileName;
            }

            if (Directory.Exists(SnPaths.instance.CompiledPatchesFolder()) && File.Exists(Path.Combine(SnPaths.instance.CompiledPatchesFolder(), fileName)))
            {
                isModded = true;
                return Path.Combine(SnPaths.instance.CompiledPatchesFolder(), fileName);
            }
            
            return File.Exists(vanillaFile) ? vanillaFile : fileName;
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
