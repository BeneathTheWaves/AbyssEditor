using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using AbyssEditor.Scripts.BinaryReading;
using AbyssEditor.Scripts.Octrees;
using AbyssEditor.Scripts.TaskSystem;
using AbyssEditor.Scripts.UI;
using AbyssEditor.Scripts.VoxelTech;
using AbyssEditor.Scripts.VoxelTech.VoxelMeshing;
using Unity.Collections;
using UnityEngine;

namespace AbyssEditor.Scripts {
    public static class BatchReadWriter
    {
        public delegate bool ReadFinishedCall(Octree[,,] nodes);
        
        public static async Task ReadBatchCoroutine(ReadFinishedCall readFinishedCall, Vector3Int batchIndex, bool allowModded, bool generateEmpty)
        {
            Vector3Int octreeDimensions = Vector3Int.one * VoxelWorld.CONTAINERS_PER_SIDE;
            if (batchIndex.x == 25) octreeDimensions.x = 3;
            if (batchIndex.z == 25) octreeDimensions.z = 3;
            Octree[,,] octrees = new Octree[octreeDimensions.z, octreeDimensions.y, octreeDimensions.x];
            int countOctrees = 0;
            int expectedOctrees = octreeDimensions.x * octreeDimensions.y * octreeDimensions.z;

            var filePath = GetPath(batchIndex, allowModded, out _);
            if (File.Exists(filePath))
            {
                BinaryReader reader = new BinaryReader(File.Open(filePath, FileMode.Open));
                reader.ReadInt32();

                // assemble a data array
                int curr_pos = 0;
                byte[] data = new byte[reader.BaseStream.Length - 4];

                long streamLength = reader.BaseStream.Length - 4;
                while (curr_pos < streamLength)
                {

                    data[curr_pos] = reader.ReadByte();
                    curr_pos++;
                }

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
                readFinishedCall(octrees);
                reader.Close();
            }

            // if no batch file
            else
            {
                EditorUI.DisplayErrorMessage($"No file for batch {batchIndex.x}-{batchIndex.y}-{batchIndex.z}\n" +
                                             $"Created an empty batch", EditorUI.NotificationType.Warning);

                if (generateEmpty && batchIndex != new Vector3Int(0, 13, 17))
                {
                    await ReadBatchCoroutine(readFinishedCall, new Vector3Int(0, 13, 17), false, false);
                }
                else
                {
                    readFinishedCall(null);
                }
            }
        }

        public static string GetPath(Vector3Int batchIndex, bool allowModded, out bool isModded)
        {
            var fileName = string.Format("compiled-batch-{0}-{1}-{2}.optoctrees", batchIndex.x, batchIndex.y, batchIndex.z);

            var vanillaFile = Path.Combine(Globals.instance.batchSourcePath, fileName);
            isModded = false;
            if (!allowModded)
            {
                if (File.Exists(vanillaFile))
                    return vanillaFile;
                return fileName;
            }

            if (Directory.Exists(Path.Combine(Globals.instance.batchSourcePath, "patches")) &&
                File.Exists(Path.Combine(Globals.instance.batchSourcePath, "patches", fileName)))
            {
                isModded = true;
                return Path.Combine(Globals.instance.batchSourcePath, "patches", fileName);
            }
            if (File.Exists(vanillaFile))
                return vanillaFile;

            return fileName;
        }

        //TODO: ensure this works still :/. After updating octrees being generated from scratch
        public static bool WriteOptoctrees(Vector3 batchIndex, Octree[,,] octrees)
        {
            string batchname = string.Format(Path.DirectorySeparatorChar + "compiled-batch-{0}-{1}-{2}.optoctrees", batchIndex.x, batchIndex.y, batchIndex.z);

            DebugOverlay.LogMessage($"Writing {batchname} to {Globals.instance.batchOutputPath}");

            BinaryWriter writer = new BinaryWriter(File.Open(Globals.instance.batchOutputPath + batchname, FileMode.OpenOrCreate));
            writer.Write(4);

            for (int z = 0; z < 5; z++)
            {
                for (int y = 0; y < 5; y++)
                {
                    for (int x = 0; x < 5; x++)
                    {
                        WriteOctree(writer, octrees[x, y, z]);
                    }
                }
            }

            writer.Close();
            return true;
        }

        public static async Task WriteOctreePatchCoroutine(VoxelMetaspace metaspace, EditorProcessHandle statusHandle = null)
        {
            if (statusHandle == null) statusHandle = TaskManager.main.GetEditorProcessHandle(1);
            
            DebugOverlay.LogMessage($"Writing {metaspace.meshes.Count} batch patches as {Globals.instance.batchOutputPath}");

            BinaryWriter writer = new BinaryWriter(File.Open(Globals.instance.batchOutputPath, FileMode.Create));
            // write version
            writer.Write(0u);

            int meshCount = metaspace.meshes.Count;
            int meshIndex = 0;

            //we will reuse this array for each grid over and over since they are the same size.
            const int res = VoxelWorld.GRID_RESOLUTION;
            NativeArray<byte> tempTypes = new NativeArray<byte>(res * res * res, Allocator.Persistent);
            NativeArray<byte> tempDensities = new NativeArray<byte>(res * res * res, Allocator.Persistent);

            meshIndex = 0;
            foreach (VoxelMesh batch in metaspace.meshes.Values)
            {
                statusHandle.SetProgress((float)meshIndex / meshCount);
                statusHandle.SetStatus($"Generating octrees for {batch.batchIndex}");
                Octree[,,] nodes = batch.ConvertGridsToOctree();
                await Task.Yield();
                
                // load original nodes from file
                NodeContainer container = new NodeContainer();
                statusHandle.SetStatus($"Loading old octrees for {batch.batchIndex}");
                await ReadBatchCoroutine(container.Callback, batch.batchIndex, false, false);
                await Task.Yield();
                Octree[,,] originalNodes = container.nodes;
                ThreadedBinaryReadWriter.RasterDeRasterizeBatch(tempDensities, tempTypes, originalNodes);

                // Diff trees
                List<Octree> batchChanges = GetChangedOctrees(nodes, originalNodes);
                
                DebugOverlay.LogMessage($"Patch contains {batchChanges.Count} changed octrees.");
                if (batchChanges.Count != 0)
                {
                    // start of batch write
                    writer.Write((short)batch.batchIndex.x);
                    writer.Write((short)batch.batchIndex.y);
                    writer.Write((short)batch.batchIndex.z);

                    // num octrees to replace
                    writer.Write((byte)batchChanges.Count);

                    foreach (Octree change in batchChanges)
                    {
                        if (change.Index > 125) DebugOverlay.LogMessage("found an octree index > 125");
                        writer.Write(change.Index);
                        WriteOctree(writer, change);
                    }
                }
                meshIndex++;
            }
            statusHandle.CompletePhase();

            tempTypes.Dispose();
            tempDensities.Dispose();

            writer.Close();
        }

        private static void WriteOctree(BinaryWriter writer, Octree octree)
        {
            //assemble the octnode array
            OctNodeData[] nodes = octree.Read();

            // write number of nodes in this octree
            ushort numNodes = (ushort)nodes.Length;
            writer.Write(numNodes);

            // write type, signedDist, childIndex of each octree
            for (int i = 0; i < nodes.Length; i++)
            {
                if (nodes[i].IsBelowSurface() && nodes[i].type == 0)
                {
                    Debug.Log($"Found odd node: {nodes}");
                }
                writer.Write((byte)(nodes[i].IsBelowSurface() && nodes[i].type == 0 ? 1 : nodes[i].type));
                writer.Write(nodes[i].signedDist);
                writer.Write(nodes[i].childPosition);
            }
        }

        /// <summary>
        /// Compare two octrees and return the octrees that are different.
        /// Essentially a diff algorithm.
        /// </summary>
        /// <param name="newNodes">The new nodes of a batch that were generated from a grid</param>
        /// <param name="originalNodes">The original nodes of a batch</param>
        /// <returns></returns>
        private static List<Octree> GetChangedOctrees(Octree[,,] newNodes, Octree[,,] originalNodes)
        {
            List<Octree> batchChanges = new List<Octree>();
            for (int z = 0; z < 5; z++)
            {
                for (int y = 0; y < 5; y++)
                {
                    for (int x = 0; x < 5; x++)
                    {
                        if (originalNodes == null)// the original batch didn't exist so add all octrees
                        {
                            batchChanges.Add(newNodes[x, y, z]);
                        }
                        else if (!newNodes[x, y, z].IdenticalTo(originalNodes[x, y, z]))
                        {
                            batchChanges.Add(newNodes[x, y, z]);
                        }
                    }
                }
            }
            return batchChanges;
        }
    }

    [System.Serializable]
    public class NodeContainer {
        public Octree[,,] nodes;

        public bool Callback(Octree[,,] originalNodes) {
            if (originalNodes is null) return false;
            nodes = originalNodes;
            return true;
        }
    }
}
