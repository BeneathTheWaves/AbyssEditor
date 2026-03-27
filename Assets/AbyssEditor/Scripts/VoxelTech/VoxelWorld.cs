using System.Collections.Generic;
using System.Threading.Tasks;
using AbyssEditor.Scripts.BatchOutline;
using AbyssEditor.Scripts.CursorTools;
using AbyssEditor.Scripts.TaskSystem;
using AbyssEditor.Scripts.UI;
using AbyssEditor.Scripts.VoxelTech.VoxelMeshing;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;


namespace AbyssEditor.Scripts.VoxelTech {
    public class VoxelWorld : MonoBehaviour {
        // constants
        // LOD. 0-5 lod => 32-1 resolution
        //public const int LEVEL_OF_DETAIL = 0;
        public const int MAX_OCTREE_DEPTH = 5;
        // this defines the in-game size of the meshes
        public const int OCTREE_WIDTH = 32;
        // this is the 'resolution' but for batches
        public const int CONTAINERS_PER_SIDE = 5;
        //the length/width/height of a batch in meters
        public const int BATCH_WIDTH = 160;
        // This defines the count of voxels (count = resolution^3) in a grid
        public const int GRID_RESOLUTION = 32;
        
        public static VoxelWorld world;
        
        private void Awake() {
            world = this;
        }
        
        public async Task LoadOctreePatchAsync(byte[] patchBytes, List<Vector3Int> batchesInPatch, List<int> batchOffsets)
        {
            CursorToolManager.main.RegisterInputBlock(this);
            
            await VoxelMetaspace.metaspace.PatchReadAsync(patchBytes, batchesInPatch, batchOffsets);
            
            BatchOutlineManager.main.ResetOutlines();
            
            CursorToolManager.main.UnregisterInputBlock(this);
            StatsTextUI.main.UpdateStats();;
        }
        
        public async Task RegionLoadAsync(Vector3Int startBatch, Vector3Int endBatch, bool allowModded)
        {
            DebugOverlay.LogMessage($"Loading {startBatch} to {endBatch}");
            
            CursorToolManager.main.RegisterInputBlock(this);

            await VoxelMetaspace.metaspace.RegionReadAsync(allowModded, startBatch, endBatch);
            
            BatchOutlineManager.main.ResetOutlines();
            
            CursorToolManager.main.UnregisterInputBlock(this);
            StatsTextUI.main.UpdateStats();
        }
        
        public static async Task ExportRegionAsync(int mode) {
            switch (mode) {
                case 0:
                    //TODO: THIS SHOULD BE IN ITS OWN FUNCTION PROBABLY
                    EditorProcessHandle statusHandle = TaskManager.main.GetEditorProcessHandle(1);
                    int meshCount = VoxelMetaspace.metaspace.meshes.Count;
                    int meshIndex = 0;
                    foreach (VoxelMesh batch in VoxelMetaspace.metaspace.meshes.Values) {
                        statusHandle.SetProgress((float)meshIndex/meshCount);
                        statusHandle.SetStatus($"Writing {batch}");
                        batch.Write();
                        await Task.Yield();
                        meshIndex++;
                    }
                    statusHandle.CompletePhase();
                    break;
                case 1:
                    await BatchReadWriter.WriteOctreePatchCoroutine(VoxelMetaspace.metaspace);
                    break;
                case 2:
                    //StartCoroutine(ExportFBX.ExportMetaspaceAsync(VoxelMetaspace.metaspace, Globals.instance.batchOutputPath));
                    break;
                default:
                    DebugOverlay.LogError("Unexpected export mode!");
                    break;
            }
        }

        public static Vector3Int BatchSpacePosToBatchId(Vector3 spacePos)
        { 
            //must floor to int as c# ints will round towards 0 in the negatives when cast flooring
            return new Vector3Int( Mathf.FloorToInt(spacePos.x / BATCH_WIDTH), Mathf.FloorToInt(spacePos.y / BATCH_WIDTH), Mathf.FloorToInt(spacePos.z / BATCH_WIDTH) );
        }
        
        public static Vector3Int BatchSpacePosToBatchId(Vector3Int spacePos)
        {
            return new Vector3Int( Mathf.FloorToInt((float)spacePos.x / BATCH_WIDTH), Mathf.FloorToInt((float)spacePos.y / BATCH_WIDTH), Mathf.FloorToInt((float)spacePos.z / BATCH_WIDTH) );
        }
        
        public byte SampleBlocktype(Vector3 hitPoint, Ray cameraRay, int retryCount= 0) {
            // batch -> octree -> voxel
            if (retryCount == 32) return 0;

            // batch
            Vector3Int batchIndex = GetBatchIndexFromPoint(hitPoint);
            if (!VoxelMetaspace.metaspace.BatchLoaded(batchIndex)) {
                float newDistance = Vector3.Distance(hitPoint, cameraRay.origin) + .5f;
                Vector3 newPoint = cameraRay.GetPoint(newDistance);
                return SampleBlocktype(newPoint, cameraRay, retryCount + 1);
            }

            if (!VoxelMetaspace.metaspace.TryGetVoxelMesh(batchIndex, out VoxelMesh batch))
            {
                return 0;
            }

            Vector3 _local = hitPoint - batchIndex * (OCTREE_WIDTH * CONTAINERS_PER_SIDE); 
            int x = (int)_local.x / OCTREE_WIDTH;
            int y = (int)_local.y / OCTREE_WIDTH;
            int z = (int)_local.z / OCTREE_WIDTH;

            byte type = batch.pointContainers[Globals.LinearIndex(x, y, z, 5)].SampleBlocktype(hitPoint);

            if (type == 0) {
                float newDistance = Vector3.Distance(hitPoint, cameraRay.origin) + .5f;
                Vector3 newPoint = cameraRay.GetPoint(newDistance);
                return SampleBlocktype(newPoint, cameraRay, retryCount + 1);
            }

            return type;
        }

        public static Vector3Int GetBatchOrigin(Vector3Int batchIndex)
        {
            return batchIndex * (OCTREE_WIDTH * CONTAINERS_PER_SIDE);
        }

        public static Vector3Int GetBatchIndexFromPoint(Vector3 p) {
            const int batchSide = OCTREE_WIDTH * CONTAINERS_PER_SIDE;
            return new Vector3Int(Mathf.FloorToInt(p.x / batchSide), Mathf.FloorToInt(p.y / batchSide), Mathf.FloorToInt(p.z / batchSide));
        }
    }
}