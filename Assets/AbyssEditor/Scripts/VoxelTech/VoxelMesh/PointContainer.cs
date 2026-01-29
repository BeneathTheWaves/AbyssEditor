using AbyssEditor.Octrees;
using AbyssEditor.Scripts.TerrainMaterials;
using AbyssEditor.Scripts.VoxelTech.VoxelGrids;
using AbyssEditor.Scripts.VoxelTech.VoxelGrids.Brushes;
using Unity.Collections;
using UnityEngine;

namespace AbyssEditor.VoxelTech
{
    internal class PointContainer
    {
        Vector3Int batchIndex;
        Vector3Int octreeIndex;

        // density data
        public VoxelGrid grid;

        // other objects
        public Bounds bounds;
        public GameObject meshObj;

        public Mesh mesh => meshObj.GetComponent<MeshFilter>().mesh;

        public PointContainer(Transform _voxelandTf, Vector3Int _octreeIndex, Vector3Int _batchIndex)
        {
            octreeIndex = _octreeIndex;
            batchIndex = _batchIndex;
            int fullGridSide = VoxelWorld.RESOLUTION + 2;
            // assume bounds has a center relative to game object origin
            Vector3Int batchOriginOffset = (batchIndex - VoxelWorld.startBatch) *
                                           (VoxelWorld.OCTREE_WIDTH * VoxelWorld.CONTAINERS_PER_SIDE);
            bounds = new Bounds(
                batchOriginOffset + octreeIndex * VoxelWorld.RESOLUTION + Vector3.one * fullGridSide / 2,
                Vector3.one * fullGridSide);

            CreateMeshObject(_voxelandTf);
        }

        void CreateMeshObject(Transform _voxelandTf)
        {
            meshObj = new GameObject($"OctreeMesh-");
            meshObj.AddComponent<MeshFilter>();
            meshObj.AddComponent<MeshRenderer>();
            meshObj.transform.SetParent(_voxelandTf);
            meshObj.transform.localPosition = Vector3.zero;
        }

        public void SetOctree(Octree octree)
        {
            meshObj.name = $"OctreeMesh-{octree.Index}";
            RasterizeOctree(octree);
        }

        public void RasterizeOctree(Octree octree)
        {
            int _res = VoxelWorld.RESOLUTION;
            NativeArray<byte> tempTypes = new NativeArray<byte>(_res * _res * _res, Allocator.Temp);
            NativeArray<byte> tempDensities = new NativeArray<byte>(_res * _res * _res, Allocator.Temp);

            octree.Rasterize(tempDensities, tempTypes, _res, 5 - VoxelWorld.LEVEL_OF_DETAIL);

            grid = new VoxelGrid(tempDensities, tempTypes, octreeIndex, batchIndex);
            //Note: we free the temporary arrays within the voxel grid once we initialize the grid to its padding
            //WE MAY want to change this so we don't have to do that tho :)
        }
        
        public BrushJob ApplyJobBasedDensityAction(Brush.BrushStroke stroke)
        {
            if (grid != null)
                return grid.ApplyJobBasedDensityFunction(stroke, octreeIndex * VoxelWorld.OCTREE_WIDTH + meshObj.transform.position);
            return null;
        }

        public void ApplyDensityAction(Brush.BrushStroke stroke)
        {
            if (grid != null)
                grid.ApplyDensityFunction(stroke, octreeIndex * VoxelWorld.OCTREE_WIDTH + meshObj.transform.position);
        }

        public void UpdateMesh()
        {
            NativeArray<byte> _tempDensities;
            NativeArray<byte> _tempTypes;
            if (grid == null) return;
            grid.GetFullGrids(out _tempDensities, out _tempTypes);

            int[] blocktypes;
            Vector3 offset = octreeIndex * VoxelWorld.RESOLUTION;
            Mesh containerMesh =
                MeshBuilder.builder.GenerateMesh(_tempDensities, _tempTypes, grid.fullGridDim, offset, out blocktypes);

            // update data
            if (containerMesh.triangles.Length > 0)
            {
                meshObj.GetComponent<MeshFilter>().sharedMesh = containerMesh;

                MeshCollider coll = meshObj.GetComponent<MeshCollider>();
                if (!coll)
                {
                    meshObj.AddComponent<MeshCollider>();
                }
                else
                {
                    coll.sharedMesh = containerMesh;
                }

                MeshRenderer renderer = meshObj.GetComponent<MeshRenderer>();
                Material[] materials = new Material[blocktypes.Length];
                for (int b = 0; b < blocktypes.Length; b++)
                {
                    materials[b] = SnMaterialLoader.GetMaterialForType(blocktypes[b]);
                }

                renderer.materials = materials;
            }
        }

        public void UpdateFullGrid()
        {
            if (grid != null)
                grid.UpdateFullGrid();
        }

        public byte SampleBlocktype(Vector3 worldPoint)
        {
            Vector3 localPoint = worldPoint - octreeIndex * VoxelWorld.RESOLUTION - meshObj.transform.parent.position;
            int x = (int)localPoint.x;
            int y = (int)localPoint.y;
            int z = (int)localPoint.z;

            return VoxelGrid.GetVoxel(grid.typeGrid, x + 1, y + 1, z + 1);
        }
    }
}