using AbyssEditor.Scripts.CursorTools.Brush;
using AbyssEditor.Scripts.Mesh_Gen;
using AbyssEditor.Scripts.TaskSystem;
using AbyssEditor.Scripts.TerrainMaterials;
using AbyssEditor.Scripts.VoxelTech.VoxelMeshing.VoxelGrids;
using AbyssEditor.Scripts.VoxelTech.VoxelMeshing.VoxelGrids.Brushes;
using Unity.Collections;
using UnityEngine;
using Task = System.Threading.Tasks.Task;

namespace AbyssEditor.Scripts.VoxelTech.VoxelMeshing
{
    public class VoxelMesh
    {
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

        public VoxelMesh(Transform batchTransform, Vector3Int octreeIndex, Vector3Int batchIndex)
        {
            this.octreeIndex = octreeIndex;
            this.batchIndex = batchIndex;
            int fullGridSide = VoxelWorld.GRID_RESOLUTION + 2;
            // assume bounds has a center relative to game object origin
            
            bounds = new Bounds(
                VoxelWorld.GetBatchOrigin(batchIndex) + this.octreeIndex * VoxelWorld.GRID_RESOLUTION + Vector3.one * fullGridSide / 2,
                Vector3.one * fullGridSide);

            CreateMeshObject(batchTransform);
        }

        public void DisposeMesh()
        {
            Object.Destroy(mesh);
        } 

        void CreateMeshObject(Transform batchTransform)
        {
            meshObj = new GameObject($"OctreeMesh-");
            meshFilter = meshObj.AddComponent<MeshFilter>();
            meshRenderer = meshObj.AddComponent<MeshRenderer>();
            meshCollider = meshObj.AddComponent<MeshCollider>();
            meshObj.transform.SetParent(batchTransform);
            meshObj.transform.localPosition = Vector3.zero;
            mesh = new Mesh();
            mesh.MarkDynamic();
            meshFilter.sharedMesh = mesh;
            meshCollider.sharedMesh = mesh;
        }

        public void CreateVoxelGrid(NativeArray<byte> densityGrid, NativeArray<byte> typeGrid)
        {
            grid = new VoxelGrid(densityGrid, typeGrid, octreeIndex, batchIndex);
        }
        
        public BrushJob ApplyJobBasedDensityAction(BrushStroke stroke)
        {
            if (grid != null)
                return grid.ApplyJobBasedDensityFunction(stroke, octreeIndex * VoxelWorld.OCTREE_WIDTH + meshObj.transform.position);
            return null;
        }
        
        public async Task UpdateMeshAsync(EditorProcessHandle statusHandle = null)//Handle can be null, increments on complete
        {
            grid.GetFullGrids(out NativeArray<byte> gridDensity, out NativeArray<byte> gridType);
            
            Vector3 offset = octreeIndex * VoxelWorld.GRID_RESOLUTION;
            
            //Note, this will overwrite the old mesh so "mesh" becomes the new one inherently
            AsyncMeshBuilder.MeshResult meshRequest = await AsyncMeshBuilder.main.RequestMesh(grid, offset, mesh);
            
            int[] blocktypes = meshRequest.blockTypes;
            
            // update materials
            if (mesh.vertexCount > 2 && mesh.triangles.Length > 0)
            {
                //This is the only way to force the collider to update. We just re-set the reference to update itself
                meshCollider.enabled = true;
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
                meshCollider.enabled = false;
            }

            statusHandle?.IncrementTasksComplete();
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
            Vector3 localPoint = worldPoint - octreeIndex * VoxelWorld.GRID_RESOLUTION - meshObj.transform.parent.position;
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