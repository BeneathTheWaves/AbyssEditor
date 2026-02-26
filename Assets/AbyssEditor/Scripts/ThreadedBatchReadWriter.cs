using System;
using AbyssEditor.Scripts.Octrees;
using AbyssEditor.Scripts.VoxelTech;
using System.IO;
using System.Threading.Tasks;
using AbyssEditor.Scripts.TaskSystem;
using AbyssEditor.Scripts.ThreadingManager;
using AbyssEditor.Scripts.UI;
using UnityEngine;

namespace AbyssEditor.Scripts
{
    /// <summary>
    /// NOTE. None of this is async due to the INSANE GC allocations since the Tree is made of Objects :skull:
    /// </summary>
    public static partial class ThreadedBatchReadWriter
    {
        public const int MAX_BATCH_UPDATES_PER_FRAME = 5;
        public static async Task ReadBatchParallelable(BatchReadWriter.ReadFinishedCall readFinishedCall, Vector3Int batchIndex, bool generateEmpty = true, bool allowModded = false, EditorProcessHandle statusHandle = null)
        {
            TaskCompletionSource<ReadBatchResult> readBatchTcs = new();
            AsyncThreadScheduler.main.Enqueue(() => ReadBatchThreaded(readBatchTcs, batchIndex, generateEmpty, allowModded));
            
            ReadBatchResult data = await readBatchTcs.Task;
            
            statusHandle?.IncrementTasksComplete();
            
            TaskCompletionSource<Boolean> applyBatch = new();
            AsyncThreadScheduler.main.Enqueue(() => Test(applyBatch, readFinishedCall, data.octrees));

            await applyBatch.Task;
                        
            statusHandle?.IncrementTasksComplete();
        }

        private static void Test(TaskCompletionSource<Boolean> tcs, BatchReadWriter.ReadFinishedCall readFinishedCall, Octree[,,] octrees)
        {
            readFinishedCall(octrees);
            tcs.SetResult(true);
        }
        
        private static void ReadBatchThreaded(TaskCompletionSource<ReadBatchResult> tcs, Vector3Int batchIndex, bool allowModded, bool generateEmpty)
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
                tcs.SetResult(new ReadBatchResult(octrees));
            }

            // if no batch file
            else
            {
                /*EditorUI.DisplayErrorMessage($"No file for batch {batchIndex.x}-{batchIndex.y}-{batchIndex.z}\n" +
                                             $"Created an empty batch", EditorUI.NotificationType.Warning);*/

                if (generateEmpty && batchIndex != new Vector3Int(0, 13, 17))
                {
                    //This is honestly so stupid but ig it works
                    ReadBatchThreaded(tcs, new Vector3Int(0, 13, 17), false, false);
                }
                else
                {
                    tcs.SetResult(null);
                }
            }
        }

        public class ReadBatchResult
        {
            public Octree[,,] octrees;
            public ReadBatchResult(Octree[,,] octrees)
            {
                this.octrees = octrees;
            }
        }
    }
}
