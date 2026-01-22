using System.Collections.Generic;
using System.Numerics;
using AbyssEditor.Octrees;
using AbyssEditor.Scripts.TerrainMaterials;
using AbyssEditor.Scripts.VoxelTech.VoxelGrids;
using AbyssEditor.Scripts.VoxelTech.VoxelGrids.Brushes;
using AbyssEditor.TerrainMaterials;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

namespace AbyssEditor.VoxelTech
{
    public class VoxelMesh : MonoBehaviour
    {
        internal PointContainer[] octreeContainers;
        public Octree[,,] nodes;
        public Vector3Int batchIndex;
        public Vector3Int octreeCounts;

        public void Create(Vector3Int _batchIndex)
        {
            batchIndex = _batchIndex;
            SetupGameObject();

            octreeCounts = Vector3Int.one * VoxelWorld.CONTAINERS_PER_SIDE;
            if (_batchIndex.x == 25) octreeCounts.x = 3;
            if (_batchIndex.z == 25) octreeCounts.z = 3;

            octreeContainers = new PointContainer[octreeCounts.x * octreeCounts.y * octreeCounts.z];

            for (int z = 0; z < octreeCounts.z; z++)
            {
                for (int y = 0; y < octreeCounts.y; y++)
                {
                    for (int x = 0; x < octreeCounts.x; x++)
                    {
                        octreeContainers[Globals.LinearIndex(x, y, z, octreeCounts)] =
                            new PointContainer(transform, new Vector3Int(x, y, z), batchIndex);
                    }
                }
            }
        }

        private void SetupGameObject()
        {
            const int octreeSide = VoxelWorld.OCTREE_WIDTH;
            transform.position = (batchIndex - VoxelWorld.startBatch) * octreeSide * 5;

            BoxCollider coll = gameObject.AddComponent<BoxCollider>();
            gameObject.layer = 1;

            coll.center = (Vector3)octreeCounts * octreeSide / 2f;
            coll.size = octreeCounts * octreeSide;
            coll.isTrigger = true;
        }

        public bool OctreesReadCallback(Octree[,,] _nodes)
        {
            nodes = _nodes;
            for (int z = 0; z < octreeCounts.z; z++)
            {
                for (int y = 0; y < octreeCounts.y; y++)
                {
                    for (int x = 0; x < octreeCounts.x; x++)
                    {
                        octreeContainers[Globals.LinearIndex(x, y, z, octreeCounts)].SetOctree(nodes[z, y, x]);
                    }
                }
            }

            return true;
        }

        public void Regenerate()
        {
            for (int i = 0; i < octreeContainers.Length; i++)
            {
                octreeContainers[i].UpdateMesh();
            }
        }

        public VoxelGrid GetVoxelGrid(Vector3Int containerIndex)
        {
            return octreeContainers[
                Globals.LinearIndex(containerIndex.x, containerIndex.y, containerIndex.z, octreeCounts)].grid;
        }

        public GameObject GetContainerObject(Vector3Int containerIndex)
        {
            return octreeContainers[
                Globals.LinearIndex(containerIndex.x, containerIndex.y, containerIndex.z, octreeCounts)].meshObj;
        }

        public void UpdateFullGrids()
        {
            foreach (PointContainer container in octreeContainers)
            {
                container.UpdateFullGrid();
            }
        }

        public void Write() => BatchReadWriter.readWriter.WriteOptoctrees(batchIndex, nodes);

        public void ApplyJobBasedDensityFunction(Brush.BrushStroke stroke, List<BrushJob> brushActions)
        {
            foreach (PointContainer container in octreeContainers)
            {
                Bounds bounds = container.bounds;
                if (OctreeRaycasting.DistanceToBox(stroke.brushLocation, bounds.min, bounds.max) <= stroke.brushRadius)
                {
                    brushActions.Add(container.ApplyJobBasedDensityAction(stroke));
                    
                }
            }
            
            
        }
        
        public void ApplyDensityAction(Brush.BrushStroke stroke)
        {
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            foreach (PointContainer container in octreeContainers)
            {
                Bounds bounds = container.bounds;
                if (OctreeRaycasting.DistanceToBox(stroke.brushLocation, bounds.min, bounds.max) <= stroke.brushRadius)
                {
                    container.ApplyDensityAction(stroke);
                }
            }
            sw.Stop();
            DebugOverlay.LogMessage($"Brush Operation in {sw.ElapsedMilliseconds}ms");
        }

        public void UpdateMeshesAfterBrush(Brush.BrushStroke stroke)
        {
            foreach (PointContainer container in octreeContainers)
            {
                Bounds bounds = container.bounds;
                if (OctreeRaycasting.DistanceToBox(stroke.brushLocation, bounds.min, bounds.max) <= stroke.brushRadius)
                {
                    container.UpdateFullGrid();
                    container.UpdateMesh();
                }
            }
        }

        public void UpdateOctreeDensity()
        {
            for (int z = 0; z < 5; z++)
            {
                for (int y = 0; y < 5; y++)
                {
                    for (int x = 0; x < 5; x++)
                    {
                        PointContainer _container = octreeContainers[Globals.LinearIndex(x, y, z, octreeCounts)];
                        nodes[z, y, x].DeRasterizeGrid(_container.grid.densityGrid, _container.grid.typeGrid,
                            VoxelWorld.RESOLUTION + 2, 5 - VoxelWorld.LEVEL_OF_DETAIL);
                    }
                }
            }
        }

        public Vector3Int GetBatchMinBound()
        {
            Vector3Int min = (batchIndex - VoxelWorld.startBatch) *
                             (VoxelWorld.OCTREE_WIDTH * VoxelWorld.CONTAINERS_PER_SIDE);
            return min;
        }

        public Vector3Int GetBatchMaxBound()
        {
            Vector3Int max = (batchIndex - VoxelWorld.startBatch + Vector3Int.one) *
                             (VoxelWorld.OCTREE_WIDTH * VoxelWorld.CONTAINERS_PER_SIDE);
            return max;
        }
    }
}