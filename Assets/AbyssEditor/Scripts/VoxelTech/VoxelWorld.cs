using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using AbyssEditor.Scripts;
using AbyssEditor.Scripts.BatchOutline;
using AbyssEditor.Scripts.TaskSystem;
using AbyssEditor.UI;
using AbyssEditor.VoxelTech;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

namespace AbyssEditor {
    public class VoxelWorld : MonoBehaviour {
        // constants
        // LOD. 0-5 lod => 32-1 resolution
        public static int LEVEL_OF_DETAIL = 0;
        // this defines the in-game size of the meshes
        public const int OCTREE_WIDTH = 32;
        // this is the 'resolution' but for batches
        public const int CONTAINERS_PER_SIDE = 5;

        public static int BATCH_WIDTH => OCTREE_WIDTH * CONTAINERS_PER_SIDE;
        
        // This defines the count of voxels (count = resolution^3)
        public static int RESOLUTION => (int)Mathf.Pow(2, 5 - LEVEL_OF_DETAIL);
        
        public static VoxelWorld world;
        public static event Action OnRegionExported;

        // Mono methods
        void Awake() {
            world = this;
        }

        public void LoadRegion(Vector3Int _start, Vector3Int _end, bool allowModded)
        {
            DebugOverlay.LogMessage($"Reading {_start} to {_end}");
            
            Vector3Int startBatch = new Vector3Int(Math.Min(_start.x, _end.x), Math.Min(_start.y, _end.y), Math.Min(_start.z, _end.z));
            Vector3Int endBatch = new Vector3Int(Math.Max(_start.x, _end.x), Math.Max(_start.y, _end.y), Math.Max(_start.z, _end.z));
            
            StartCoroutine(RegionLoadCoroutine(allowModded, startBatch, endBatch));
        }

        public void LoadOctreePatch(string filePath)
        {
            StartCoroutine(OctreePatchCoroutine(filePath));
        }
        
        IEnumerator OctreePatchCoroutine(string filePath)
        {

            yield return StartCoroutine(VoxelMetaspace.metaspace.OctreePatchReadCoroutine(filePath));
        }
        
        IEnumerator RegionLoadCoroutine(bool allowModded, Vector3Int startBatch, Vector3Int endBatch)
        {
            VoxelMetaspace.metaspace.AddRegion(startBatch, endBatch);
            
            yield return StartCoroutine(VoxelMetaspace.metaspace.RegionReadCoroutine(allowModded, startBatch, endBatch));
            
            VoxelMetaspace.metaspace.ReloadBoundaries();
            
            BatchOutlineManager.main.ResetLoadOutlines();
        }

        public static void ExportRegion(int mode) {
            world.StartCoroutine(world.ExportRegionCoroutine(mode));
        }
        IEnumerator ExportRegionCoroutine(int mode) {
            switch (mode) {
                case 0:
                    //TODO: THIS SHOULD BE IN ITS OWN FUNCTION PROBABLY
                    EditorProcessHandle statusHandle = TaskManager.main.GetEditorProcessHandle(1);
                    int meshCount = VoxelMetaspace.metaspace.meshes.Count;
                    int meshIndex = 0;
                    foreach (VoxelMesh batch in VoxelMetaspace.metaspace.meshes) {
                        statusHandle.SetProgress((float)meshIndex/meshCount);
                        statusHandle.SetStatus($"Writing {batch}");
                        batch.Write();
                        yield return null;
                        meshIndex++;
                    }
                    statusHandle.CompletePhase();
                    break;
                case 1:
                    yield return StartCoroutine(BatchReadWriter.readWriter.WriteOctreePatchCoroutine(VoxelMetaspace.metaspace));
                    break;
                case 2:
                    yield return StartCoroutine(ExportFBX.ExportMetaspaceAsync(VoxelMetaspace.metaspace, Globals.instance.batchOutputPath));
                    break;
                default:
                    DebugOverlay.LogError("Unexpected export mode!");
                    break;
            }

            OnRegionExported?.Invoke();
        }
        
        //TODO:THIS PROBABLY DOESNT FUCKING WORK NOW
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

            VoxelMesh batch = VoxelMetaspace.metaspace.TryGetVoxelMesh(batchIndex);

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