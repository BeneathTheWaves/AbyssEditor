using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AbyssEditor.Scripts.CursorTools;
using AbyssEditor.Scripts.CursorTools.Brush;
using AbyssEditor.Scripts.Octrees;
using AbyssEditor.Scripts.VoxelTech.VoxelGrids;
using AbyssEditor.Scripts.VoxelTech.VoxelGrids.Brushes;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

namespace AbyssEditor.Scripts.VoxelTech.VoxelMesh
{
    public class VoxelMesh : MonoBehaviour
    {
        internal PointContainer[] pointContainers;
        public Vector3Int batchIndex;
        public Vector3Int octreeCounts;

        GameObject[] boundaryPlanes;
        
        public void Create(Vector3Int _batchIndex)
        {
            batchIndex = _batchIndex;
            SetupGameObject();

            octreeCounts = Vector3Int.one * VoxelWorld.CONTAINERS_PER_SIDE;

            pointContainers = new PointContainer[octreeCounts.x * octreeCounts.y * octreeCounts.z];

            for (int z = 0; z < octreeCounts.z; z++)
            {
                for (int y = 0; y < octreeCounts.y; y++)
                {
                    for (int x = 0; x < octreeCounts.x; x++)
                    {
                        pointContainers[Globals.LinearIndex(x, y, z, octreeCounts)] =
                            new PointContainer(transform, new Vector3Int(x, y, z), batchIndex);
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

        public bool OctreesReadCallback(Octree[,,] _nodes)
        {
            for (int z = 0; z < octreeCounts.z; z++)
            {
                for (int y = 0; y < octreeCounts.y; y++)
                {
                    for (int x = 0; x < octreeCounts.x; x++)
                    {
                        pointContainers[Globals.LinearIndex(x, y, z, octreeCounts)].SetOctree(_nodes[z, y, x]);
                    }
                }
            }
            return true;
        }

        public void Regenerate()
        {
            foreach (var container in pointContainers)
            {
                container.UpdateMeshAsync();
            }
        }

        public VoxelGrid GetVoxelGrid(Vector3Int containerIndex)
        {
            return pointContainers[
                Globals.LinearIndex(containerIndex.x, containerIndex.y, containerIndex.z, octreeCounts)].grid;
        }

        public void UpdateFullGrids()
        {
            foreach (PointContainer container in pointContainers)
            {
                container.UpdateNeighborData();
            }
        }

        public void CacheNeighboringVoxelGrids()
        {
            foreach (PointContainer container in pointContainers)
            {
                container.CacheNeighborGrids();
            }
        }

        public void Write() => BatchReadWriter.WriteOptoctrees(batchIndex, ConvertGridsToOctree());//TODO: Check if this works, not tested atm :/

        public void ApplyJobBasedDensityFunction(BrushStroke stroke, List<BrushJob> brushActions, List<PointContainer> modifiedContainers)
        {
            foreach (PointContainer container in pointContainers)
            {
                Bounds bounds = container.bounds;
                if (OctreeRaycasting.SquaredDistanceToBox(stroke.brushLocation, bounds.min, bounds.max) <= stroke.squaredRadius )
                {
                    brushActions.Add(container.ApplyJobBasedDensityAction(stroke));
                    modifiedContainers.Add(container);
                }
            }
        }

        public Octree[,,] ConvertGridsToOctree()
        {
            Octree[,,] nodes = new Octree[VoxelWorld.CONTAINERS_PER_SIDE, VoxelWorld.CONTAINERS_PER_SIDE, VoxelWorld.CONTAINERS_PER_SIDE];
            
            for (int z = 0; z < 5; z++)
            {
                for (int y = 0; y < 5; y++)
                {
                    for (int x = 0; x < 5; x++)
                    {
                        ref Octree tree = ref nodes[z, y, x];
                        tree = new Octree(x, y, z, VoxelWorld.OCTREE_WIDTH, batchIndex * VoxelWorld.BATCH_WIDTH);
                        
                        OctNodeData[] nodeData = new OctNodeData[1];
                        nodeData[0] = new OctNodeData();
                        
                        tree.Write(nodeData);
                        
                        PointContainer _container = pointContainers[Globals.LinearIndex(x, y, z, octreeCounts)];
                        tree.DeRasterizeGrid(_container.grid.densityGrid, _container.grid.typeGrid, VoxelGrid.GRID_PADDING, 5 - VoxelWorld.LEVEL_OF_DETAIL);
                    }
                }
            }
            return nodes;
        }

        /// <summary>
        /// This should be called when closing the player to free the memory
        /// </summary>
        public void DisposeGrids()
        {
            foreach (PointContainer pointContainer in pointContainers)
            {
                pointContainer.grid.DisposeGrids();
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
                    boundaryPlanes[c].GetComponent<MeshRenderer>().material = Globals.instance.boundaryGizmoMat;
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

        
        public bool[] GetActiveNeighboringMeshes()
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