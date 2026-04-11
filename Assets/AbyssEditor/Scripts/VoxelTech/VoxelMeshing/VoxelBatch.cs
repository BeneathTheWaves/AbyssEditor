using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AbyssEditor.Scripts.BinaryReadingWriting;
using AbyssEditor.Scripts.CursorTools.Brush;
using AbyssEditor.Scripts.TaskSystem;
using AbyssEditor.Scripts.ThreadingManager;
using AbyssEditor.Scripts.Util;
using AbyssEditor.Scripts.VoxelTech.VoxelMeshing.VoxelGrids;
using AbyssEditor.Scripts.VoxelTech.VoxelMeshing.VoxelGrids.Brushes;
using Unity.Collections;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

namespace AbyssEditor.Scripts.VoxelTech.VoxelMeshing
{
    public class VoxelBatch : MonoBehaviour
    {
        internal VoxelMesh[] pointContainers;
        public Vector3Int batchIndex;
        public Vector3Int octreeCounts;

        private readonly int queuesPerFrame = WorkerThreadManager.main.workersCount;
        
        GameObject[] boundaryPlanes;
        
        public void Create(Vector3Int _batchIndex)
        {
            batchIndex = _batchIndex;
            SetupGameObject();

            octreeCounts = Vector3Int.one * VoxelWorld.CONTAINERS_PER_SIDE;

            pointContainers = new VoxelMesh[VoxelWorld.CONTAINERS_PER_SIDE * VoxelWorld.CONTAINERS_PER_SIDE * VoxelWorld.CONTAINERS_PER_SIDE];

            for (int z = 0; z < octreeCounts.z; z++)
            {
                for (int y = 0; y < octreeCounts.y; y++)
                {
                    for (int x = 0; x < octreeCounts.x; x++)
                    {
                        pointContainers[Utils.LinearIndex(x, y, z, octreeCounts)] = new VoxelMesh(transform, new Vector3Int(x, y, z), batchIndex);
                    }
                }
            }
        }

        private void SetupGameObject()
        {
            const int octreeSide = VoxelWorld.OCTREE_WIDTH;

            transform.position = batchIndex * (Vector3Int.one * VoxelWorld.BATCH_WIDTH);

            BoxCollider coll = gameObject.AddComponent<BoxCollider>();
            gameObject.layer = 1;

            coll.center = (Vector3)octreeCounts * octreeSide / 2f;
            coll.size = octreeCounts * octreeSide;
            coll.isTrigger = true;
        }

        public async Task<List<Task>> ScheduleMeshRegenAsync(EditorProcessHandle statusHandle)
        {
            var tasks = new List<Task>();
            for (int i = 0; i < pointContainers.Length; i++)
            {
                tasks.Add(pointContainers[i].UpdateMeshAsync(statusHandle));
                if (i % queuesPerFrame == 0)
                {
                    await Task.Yield();
                }
            }
            return tasks;
        }
        
        public async Task LoadGridsFromBatchesAsync(bool allowModded, EditorProcessHandle statusHandle)
        {
            await WorkerThreadManager.main.ScheduleParallel(() =>
            {
                ThreadedBinaryReadWriter.ReadBatchThreadable(batchIndex, out NativeArray<byte>[] densityGrids, out NativeArray<byte>[] typeGrids, usePaddedSize: true);
                
                for (int z = 0; z < octreeCounts.z; z++)
                for (int y = 0; y < octreeCounts.y; y++)
                for (int x = 0; x < octreeCounts.x; x++)
                {
                    int linearIndexContainer = Utils.LinearIndex(x, y, z, octreeCounts);
                    int linearIndexOctree = Utils.LinearIndex(z, y, x, octreeCounts);//they operate in z y x for some reason, couldn't tell you why, but it works :)
                    pointContainers[linearIndexContainer].CreateVoxelGrid(densityGrids[linearIndexOctree], typeGrids[linearIndexOctree]);
                }
                
            });
            statusHandle.IncrementTasksComplete();
        }
        public async Task LoadGridsFromPatchAsync(byte[] patchByteArray, int batchIndexOffset, EditorProcessHandle statusHandle)
        {
            await WorkerThreadManager.main.ScheduleParallel(() =>
            {
                ThreadedBinaryReadWriter.GetPatchOctreesThreadable(patchByteArray, batchIndexOffset, out NativeArray<byte>[] densityGrids, out NativeArray<byte>[] typeGrids);

                for (int z = 0; z < octreeCounts.z; z++)
                for (int y = 0; y < octreeCounts.y; y++)
                for (int x = 0; x < octreeCounts.x; x++)
                {
                    int linearIndexContainer = Utils.LinearIndex(x, y, z, octreeCounts);
                    int linearIndexOctree = Utils.LinearIndex(z, y, x, octreeCounts);//they operate in z y x for some reason, couldn't tell you why, but it works :)
                    pointContainers[linearIndexContainer].CreateVoxelGrid(densityGrids[linearIndexOctree], typeGrids[linearIndexOctree]);
                }
            });
            statusHandle.IncrementTasksComplete();
        }

        public VoxelGrid GetVoxelGrid(Vector3Int containerIndex)
        {
            return pointContainers[Utils.LinearIndex(containerIndex.x, containerIndex.y, containerIndex.z, octreeCounts)].grid;
        }

        public void UpdateFullGrids()
        {
            foreach (VoxelMesh container in pointContainers)
            {
                container.UpdateNeighborData();
            }
        }

        public void CacheNeighboringVoxelGrids()
        {
            foreach (VoxelMesh container in pointContainers)
            {
                container.CacheNeighborGrids();
            }
        }

        public void Write(string exportFileLocation) {} // BatchReadWriter.WriteOptoctrees(batchIndex, ConvertGridsToOctree(), exportFileLocation);//TODO: Check if this works, not tested atm :/
        
        public List<Vector3Int> GetDifferentGrids(NativeArray<byte>[] originalDensityGrids, NativeArray<byte>[] originalTypeGrids)
        {
            List<Vector3Int> changedGridIndexes = new();
            //Return all 
            if (originalDensityGrids == null || originalTypeGrids == null)
            {
                for (int x = 0; x < VoxelWorld.CONTAINERS_PER_SIDE; x++)
                for (int y = 0; y < VoxelWorld.CONTAINERS_PER_SIDE; y++)
                for (int z = 0; z < VoxelWorld.CONTAINERS_PER_SIDE; z++)
                {
                    changedGridIndexes.Add(new Vector3Int(x, y, z));
                }
                return changedGridIndexes;
            }
            
            for (int x = 0; x < VoxelWorld.CONTAINERS_PER_SIDE; x++)
            for (int y = 0; y < VoxelWorld.CONTAINERS_PER_SIDE; y++)
            for (int z = 0; z < VoxelWorld.CONTAINERS_PER_SIDE; z++)
            {
                int index = Utils.LinearIndex(x, y, z, VoxelWorld.CONTAINERS_PER_SIDE);
                if (originalDensityGrids[index].IsIdenticalTo(pointContainers[index].grid.densityGrid) && originalTypeGrids[index].IsIdenticalTo(pointContainers[index].grid.typeGrid))
                {
                    continue;   
                }
                
                changedGridIndexes.Add(new Vector3Int(x, y, z));
            }

            return changedGridIndexes;
        }
        
        public void ApplyJobBasedDensityFunction(BrushStroke stroke, List<BrushJob> brushActions, List<VoxelMesh> modifiedContainers)
        {
            foreach (VoxelMesh container in pointContainers)
            {
                Bounds bounds = container.bounds;
                if (Utils.SquaredDistanceToBox(stroke.brushLocation, bounds.min, bounds.max) <= stroke.squaredRadius )
                {
                    brushActions.Add(container.ApplyJobBasedDensityAction(stroke));
                    modifiedContainers.Add(container);
                }
            }
        }

        /// <summary>
        /// This should be called when closing the player to free the memory
        /// </summary>
        public void Dispose()
        {
            foreach (VoxelMesh pointContainer in pointContainers)
            {
                pointContainer.grid.DisposeGrids();
                pointContainer.DisposeMesh();
            }
        }

        public Vector3Int GetBatchMinBound()
        {
            Vector3Int min = VoxelWorld.GetBatchOrigin(batchIndex);
            return min;
        }

        public Vector3Int GetBatchMaxBound()
        {
            Vector3Int max = VoxelWorld.GetBatchOrigin(batchIndex) + (Vector3Int.one * VoxelWorld.BATCH_WIDTH);
            return max;
        }
        
        
        public void RedrawBoundaryPlanes()
        {
            if (boundaryPlanes == null) {
                boundaryPlanes = new GameObject[6];
                for(int c = 0; c < 6; c++) {
                    boundaryPlanes[c] = GameObject.CreatePrimitive(PrimitiveType.Plane);
                    boundaryPlanes[c].transform.SetParent(transform);
                    boundaryPlanes[c].GetComponent<MeshRenderer>().material = VoxelWorld.world.boundaryGizmoMat;
                }
                
                // bottom
                boundaryPlanes[0].transform.eulerAngles = Vector3.zero;
                // top
                boundaryPlanes[1].transform.eulerAngles = Vector3.right * 180;
                // left
                boundaryPlanes[2].transform.eulerAngles = Vector3.forward * -90;
                // right
                boundaryPlanes[3].transform.eulerAngles = Vector3.forward * 90;
                // back
                boundaryPlanes[4].transform.eulerAngles = Vector3.right * 90;
                // forward
                boundaryPlanes[5].transform.eulerAngles = Vector3.right * -90;
            }
            bool[] neighbors = GetActiveNeighboringMeshes();
            for (int i = 0; i < neighbors.Length; i++)
            {
                boundaryPlanes[i].SetActive(!neighbors[i]);
            }
            
            float halfPos = VoxelWorld.BATCH_WIDTH / 2f;
            
            boundaryPlanes[0].transform.localPosition = new Vector3(halfPos, 0, halfPos);
            boundaryPlanes[1].transform.localPosition = new Vector3(halfPos, VoxelWorld.BATCH_WIDTH, halfPos);
            boundaryPlanes[2].transform.localPosition = new Vector3(0, halfPos, halfPos);
            boundaryPlanes[3].transform.localPosition = new Vector3(VoxelWorld.BATCH_WIDTH, halfPos, halfPos);
            boundaryPlanes[4].transform.localPosition = new Vector3(halfPos, halfPos, 0);
            boundaryPlanes[5].transform.localPosition = new Vector3(halfPos, halfPos, VoxelWorld.BATCH_WIDTH);
            
            foreach (GameObject plane in boundaryPlanes)
            {
                //planes have a width of 10 just from their mesh
                plane.transform.localScale = new Vector3(VoxelWorld.BATCH_WIDTH * 0.1f, 1, VoxelWorld.BATCH_WIDTH * 0.1f);
            }
        }


        private bool[] GetActiveNeighboringMeshes()
        {
            bool[] neighboringMeshes = new bool[6];
            
            neighboringMeshes[0] = VoxelMetaspace.metaspace.BatchLoaded(batchIndex + Vector3Int.down);
            neighboringMeshes[1] = VoxelMetaspace.metaspace.BatchLoaded(batchIndex + Vector3Int.up);
            neighboringMeshes[2] = VoxelMetaspace.metaspace.BatchLoaded(batchIndex + Vector3Int.left);
            neighboringMeshes[3] = VoxelMetaspace.metaspace.BatchLoaded(batchIndex + Vector3Int.right);
            neighboringMeshes[4] = VoxelMetaspace.metaspace.BatchLoaded(batchIndex + Vector3Int.back);
            neighboringMeshes[5] = VoxelMetaspace.metaspace.BatchLoaded(batchIndex + Vector3Int.forward);
            
            return neighboringMeshes;
        }
    }
}