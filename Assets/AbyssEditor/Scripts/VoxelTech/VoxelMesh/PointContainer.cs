using System.Threading;
using AbyssEditor.Octrees;
using AbyssEditor.Scripts.TerrainMaterials;
using AbyssEditor.Scripts.VoxelTech.VoxelGrids;
using AbyssEditor.Scripts.VoxelTech.VoxelGrids.Brushes;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace AbyssEditor.VoxelTech
{
    public class PointContainer
    {
        Vector3Int batchIndex;
        Vector3Int octreeIndex;

        // density data
        public VoxelGrid grid;

        // other objects
        public Bounds bounds;
        public GameObject meshObj;

        private Mesh mesh;
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private MeshCollider meshCollider;

        public PointContainer(Transform _voxelandTf, Vector3Int _octreeIndex, Vector3Int _batchIndex)
        {
            octreeIndex = _octreeIndex;
            batchIndex = _batchIndex;
            int fullGridSide = VoxelWorld.RESOLUTION + 2;
            // assume bounds has a center relative to game object origin
            
            bounds = new Bounds(
                VoxelWorld.GetBatchOrigin(_batchIndex) + octreeIndex * VoxelWorld.RESOLUTION + Vector3.one * fullGridSide / 2,
                Vector3.one * fullGridSide);

            CreateMeshObject(_voxelandTf);
        }

        void CreateMeshObject(Transform _voxelandTf)
        {
            meshObj = new GameObject($"OctreeMesh-");
            meshFilter = meshObj.AddComponent<MeshFilter>();
            meshRenderer = meshObj.AddComponent<MeshRenderer>();
            meshCollider = meshObj.AddComponent<MeshCollider>();
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
        
        public void UpdateMesh()
        {
            grid.GetFullGrids(out NativeArray<byte> _tempDensities, out NativeArray<byte> _tempTypes);

            int[] blocktypes;
            Vector3 offset = octreeIndex * VoxelWorld.RESOLUTION;
            mesh = MeshBuilder.builder.GenerateMesh(_tempDensities, _tempTypes, grid.fullGridDim, offset, out blocktypes);

            // update data
            if (mesh.triangles.Length > 0)
            {
                meshFilter.sharedMesh = mesh;

                meshCollider.sharedMesh = mesh;
                
                Material[] materials = new Material[blocktypes.Length];
                for (int b = 0; b < blocktypes.Length; b++)
                {
                    materials[b] = SnMaterialLoader.GetMaterialForType(blocktypes[b]);
                }

                meshRenderer.materials = materials;
            }
        }

        public void UpdateNeighborData()
        {
            grid.NeighborDataUpdate();
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