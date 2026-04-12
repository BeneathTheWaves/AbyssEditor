using System.Collections.Generic;
using System.IO;
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
                        int octIndex = Utils.LinearIndex(gridFlatIndex.z, gridFlatIndex.y, gridFlatIndex.x, VoxelWorld.OCTREES_PER_SIDE);
                        writer.Write((byte)octIndex);
                        int pointContainerIndex = Utils.LinearIndex(gridFlatIndex.x, gridFlatIndex.y, gridFlatIndex.z, VoxelWorld.OCTREES_PER_SIDE);
                        ThreadedBinaryReadWriter.WriteBatch(writer, batch.pointContainers[pointContainerIndex].grid.densityGrid, batch.pointContainers[pointContainerIndex].grid.typeGrid);
                    }
                }

                if (originalDensityGrids == null || originalTypeGrids == null) continue;
                
                for (int i = 0; i < VoxelWorld.OCTREES_PER_SIDE; i++)
                {
                    originalDensityGrids[i].Dispose();
                    originalTypeGrids[i].Dispose();
                }

            }
            statusHandle.CompletePhase();

            writer.Close();
        }
    }
}
