using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using AbyssEditor.Scripts.BinaryReadingWriting;
using AbyssEditor.Scripts.TaskSystem;
using AbyssEditor.Scripts.UI;
using AbyssEditor.Scripts.Util;
using AbyssEditor.Scripts.VoxelTech;
using AbyssEditor.Scripts.VoxelTech.VoxelMeshing;
using Unity.Collections;
using UnityEngine;

namespace AbyssEditor.Scripts {
    public static class BatchReadWriter
    {
        /*
        public static bool WriteOptoctrees(Vector3 batchIndex, Octree[,,] octrees, string exportFileLocation)
        {
            string batchname = string.Format(Path.DirectorySeparatorChar + "compiled-batch-{0}-{1}-{2}.optoctrees", batchIndex.x, batchIndex.y, batchIndex.z);

            DebugOverlay.LogMessage($"Writing {batchname} to {exportFileLocation}");

            BinaryWriter writer = new BinaryWriter(File.Open(exportFileLocation + batchname, FileMode.OpenOrCreate));
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
        */
        
        public static async Task WriteOctreePatchCoroutine(VoxelMetaspace metaspace, string exportFileLocation, EditorProcessHandle statusHandle = null)
        {
            if (statusHandle == null) statusHandle = TaskManager.main.GetEditorProcessHandle(1);
            
            DebugOverlay.LogMessage($"Writing {metaspace.batches.Count} batches for {exportFileLocation}");

            BinaryWriter writer = new BinaryWriter(File.Open(exportFileLocation, FileMode.Create));
            writer.Write(0u);// write version
            
            foreach (VoxelBatch batch in metaspace.batches.Values)
            {
                // load original nodes from file
                statusHandle.SetStatus($"Loading base octrees {batch.batchIndex}");
                ThreadedBinaryReadWriter.ReadBatchThreadable(batch.batchIndex, out NativeArray<byte>[] originalDensityGrids, out NativeArray<byte>[] originalTypeGrids, generateEmpty: false, usePaddedSize: true);
                await Task.Yield();
                
                // Diff trees
                statusHandle.SetStatus($"Generating octrees diffs {batch.batchIndex}");
                await Task.Yield();
                List<Vector3Int> changedGridFlatIndexs = batch.GetDifferentGrids(originalDensityGrids, originalTypeGrids);
                
                DebugOverlay.LogMessage($"Patch contains {changedGridFlatIndexs.Count} changed octrees.");
                
                statusHandle.SetStatus($"Writing octrees {batch.batchIndex}");
                await Task.Yield();
                if (changedGridFlatIndexs.Count != 0)
                {
                    // start of batch write
                    writer.Write((short)batch.batchIndex.x);
                    writer.Write((short)batch.batchIndex.y);
                    writer.Write((short)batch.batchIndex.z);

                    // num octrees to replace
                    writer.Write((byte)changedGridFlatIndexs.Count);

                    foreach (Vector3Int gridFlatIndex in changedGridFlatIndexs)
                    {
                        //is this cursed as FUCK, yes. but does it work? also yes. so is it bad? yes. it's stupid.
                        int octIndex = Utils.LinearIndex(gridFlatIndex.z, gridFlatIndex.y, gridFlatIndex.x, VoxelWorld.CONTAINERS_PER_SIDE);
                        writer.Write((byte)octIndex);
                        int pointContainerIndex = Utils.LinearIndex(gridFlatIndex.x, gridFlatIndex.y, gridFlatIndex.z, VoxelWorld.CONTAINERS_PER_SIDE);
                        ThreadedBinaryReadWriter.WriteBatchThreadable(writer, batch.pointContainers[pointContainerIndex].grid.densityGrid, batch.pointContainers[pointContainerIndex].grid.typeGrid);
                    }
                }

                if (originalDensityGrids == null || originalTypeGrids == null) continue;
                
                for (int i = 0; i < VoxelWorld.CONTAINERS_PER_SIDE; i++)
                {
                    originalDensityGrids[i].Dispose();
                    originalTypeGrids[i].Dispose();
                }

            }
            statusHandle.CompletePhase();

            writer.Close();
        }
        
        /*
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
        */
    }
}
