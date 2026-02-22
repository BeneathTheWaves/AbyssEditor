using AbyssEditor.Scripts.CursorTools.Brush;
using AbyssEditor.Scripts.Mesh_Gen;
using AbyssEditor.Scripts.Octrees;
using AbyssEditor.Scripts.TerrainMaterials;
using AbyssEditor.Scripts.VoxelTech.VoxelGrids;
using AbyssEditor.Scripts.VoxelTech.VoxelGrids.Brushes;
using Unity.Collections;
using UnityEngine;
using Task = System.Threading.Tasks.Task;

namespace AbyssEditor.Scripts.VoxelTech.VoxelMesh
{
    public class PointContainer
    {
        public static bool isAnyUpdatingMeshes => meshesUpdatingCounter > 0;
        private static int meshesUpdatingCounter = 0;

        private readonly Vector3Int batchIndex;
        private readonly Vector3Int octreeIndex;

        // density data
        public VoxelGrid grid;

        // other objects
        public Bounds bounds;
        private GameObject meshObj;

        private Mesh mesh;
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private MeshCollider meshCollider;

        public PointContainer(Transform batchTransform, Vector3Int octreeIndex, Vector3Int batchIndex)
        {
            this.octreeIndex = octreeIndex;
            this.batchIndex = batchIndex;
            int fullGridSide = VoxelWorld.RESOLUTION + 2;
            // assume bounds has a center relative to game object origin
            
            bounds = new Bounds(
                VoxelWorld.GetBatchOrigin(batchIndex) + this.octreeIndex * VoxelWorld.RESOLUTION + Vector3.one * fullGridSide / 2,
                Vector3.one * fullGridSide);

            CreateMeshObject(batchTransform);
        }

        void CreateMeshObject(Transform batchTransform)
        {
            meshObj = new GameObject($"OctreeMesh-");
            meshFilter = meshObj.AddComponent<MeshFilter>();
            meshRenderer = meshObj.AddComponent<MeshRenderer>();
            meshCollider = meshObj.AddComponent<MeshCollider>();
            meshObj.transform.SetParent(batchTransform);
            meshObj.transform.localPosition = Vector3.zero;
        }

        public void SetOctree(Octree octree)
        {
            meshObj.name = $"OctreeMesh-{octree.index}";
            RasterizeOctree(octree);
        }

        public void RasterizeOctree(Octree octree)
        {
            int _res = VoxelWorld.RESOLUTION;
            NativeArray<byte> tempTypes = new NativeArray<byte>(_res * _res * _res, Allocator.Temp);
            NativeArray<byte> tempDensities = new NativeArray<byte>(_res * _res * _res, Allocator.Temp);

            octree.Rasterize(tempDensities, tempTypes, _res, 5 - VoxelWorld.LEVEL_OF_DETAIL);

            //if this is an overwrite, we need to free the old native arrays before overwriting.
            if (grid != null)
            {
                grid.DisposeGrids();
                grid = null;
            }
            
            grid = new VoxelGrid(tempDensities, tempTypes, octreeIndex, batchIndex);
            //Note: we free the temporary arrays within the voxel grid once we initialize the grid to its padding
            //WE MAY want to change this so we don't have to do that tho :)
        }
        
        public BrushJob ApplyJobBasedDensityAction(BrushStroke stroke)
        {
            if (grid != null)
                return grid.ApplyJobBasedDensityFunction(stroke, octreeIndex * VoxelWorld.OCTREE_WIDTH + meshObj.transform.position);
            return null;
        }
        
        public async Task UpdateMeshAsync()
        {
            meshesUpdatingCounter++;
            
            grid.GetFullGrids(out NativeArray<byte> gridDensity, out NativeArray<byte> gridType);
            
            Vector3 offset = octreeIndex * VoxelWorld.RESOLUTION;
            
            AsyncMeshBuilder.MeshResult meshRequest = await AsyncMeshBuilder.builder.RequestMesh(gridDensity, gridType, grid.fullGridDim, offset);
            
            Mesh mesh = meshRequest.mesh;
            int[] blocktypes = meshRequest.blockTypes;

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
            else
            {
                meshFilter.mesh = null;
                meshCollider.sharedMesh = null;
            }
            meshesUpdatingCounter--;
        }

        public void UpdateNeighborData()
        {
            grid.NeighborDataUpdate();
        }
        
        public void CacheNeighborGrids()
        {
            grid.CacheNeighboringVoxelGrids();
        }

        public byte SampleBlocktype(Vector3 worldPoint)
        {
            Vector3 localPoint = worldPoint - octreeIndex * VoxelWorld.RESOLUTION - meshObj.transform.parent.position;
            int x = (int)localPoint.x;
            int y = (int)localPoint.y;
            int z = (int)localPoint.z;

            return VoxelGrid.GetVoxel(grid.typeGrid, x + 1, y + 1, z + 1, VoxelGrid.GRID_PADDING);
        }
    }
    public class MeshUpdateHandle
    {
        public bool isComplete;
    }
}