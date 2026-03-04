using System.Collections.Generic;
using AbyssEditor.Scripts.CursorTools;
using AbyssEditor.Scripts.CursorTools.Brush;
using AbyssEditor.Scripts.VoxelTech.VoxelGrids.Brushes;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

namespace AbyssEditor.Scripts.VoxelTech.VoxelGrids {
    public class VoxelGrid
    {
        public const int GRID_PADDING = 1;
        public static int GRID_FULL_SIDE = VoxelWorld.GRID_RESOLUTION + 2;
        
        public NativeArray<byte> densityGrid;
        public NativeArray<byte> typeGrid;
        public Vector3Int fullGridDim;
        private readonly Vector3Int octreeIndex;
        private readonly Vector3Int batchIndex;
        private VoxelGrid[] neighboringGrids = new VoxelGrid[27];//the center is a reference to self, references can be null
        
        public static NativeArray<int3> neighboursToCheckInSmooth;
        private static Vector3Int[] paddingVoxels;

        public VoxelGrid(NativeArray<byte> _coreDensity, NativeArray<byte> _coreTypes, Vector3Int _octreeIndex, Vector3Int _batchIndex)
        {
            int gridSize = GetFullGridSize();
            densityGrid = new NativeArray<byte>(gridSize, Allocator.Persistent);
            typeGrid = new NativeArray<byte>(gridSize, Allocator.Persistent);
            
            for (int z = 0; z < VoxelWorld.GRID_RESOLUTION; z++) {
                for (int y = 0; y < VoxelWorld.GRID_RESOLUTION; y++) {
                    for (int x = 0; x < VoxelWorld.GRID_RESOLUTION; x++) {
                        SetVoxel(densityGrid, x + GRID_PADDING, y + GRID_PADDING, z + GRID_PADDING, GetVoxel(_coreDensity, x, y, z, 0));
                        SetVoxel(typeGrid, x + GRID_PADDING, y + GRID_PADDING, z + GRID_PADDING, GetVoxel(_coreTypes, x, y, z, 0));
                    }
                }
            }

            fullGridDim = Vector3Int.one * GRID_FULL_SIDE;

            octreeIndex = _octreeIndex;
            batchIndex = _batchIndex;
            
            _coreDensity.Dispose();
            _coreTypes.Dispose();
        }

        public static byte GetVoxel(NativeArray<byte> array, int x, int y, int z, int padding) {
            return array[Globals.LinearIndex(x, y, z, VoxelWorld.GRID_RESOLUTION + padding*2)];
        }
        public static byte GetVoxel(NativeArray<byte> array, Vector3Int voxel) {
            return array[Globals.LinearIndex(voxel.x, voxel.y, voxel.z, VoxelWorld.GRID_RESOLUTION + GRID_PADDING * 2)];
        }
        
        public static void SetVoxel(NativeArray<byte> array, int x, int y, int z, byte val) {
            array[Globals.LinearIndex(x, y, z, VoxelWorld.GRID_RESOLUTION + GRID_PADDING*2)] = val;
        }
        
        public static void SetVoxel(NativeArray<byte> array, ref Vector3Int voxel, byte val) {
            array[Globals.LinearIndex(voxel.x, voxel.y, voxel.z, VoxelWorld.GRID_RESOLUTION + GRID_PADDING*2)] = val;
        }

        public void CacheNeighboringVoxelGrids()
        {
            for (int x = -1; x <= 1; x++) {
                for (int y = -1; y <= 1; y++) {
                    for (int z = -1; z <= 1; z++)
                    {
                        Vector3Int sampleVoxel = new Vector3Int(x, y, z);
                        ParseSampleVoxel(ref sampleVoxel);
                    }
                }
            }
            void ParseSampleVoxel(ref Vector3Int sampleVoxel)
            {
                int neighborGridCacheIndex = (sampleVoxel.x + 1) + (sampleVoxel.y + 1) * 3 + (sampleVoxel.z + 1) * 9;
                        
                
                Vector3Int neighborContainerIndex = octreeIndex + sampleVoxel;
                Vector3Int neighborBatchIndex = batchIndex;
            
                if (neighborContainerIndex.x < 0 || neighborContainerIndex.y < 0 || neighborContainerIndex.z < 0 ||
                    neighborContainerIndex.x >= VoxelWorld.CONTAINERS_PER_SIDE || neighborContainerIndex.y >= VoxelWorld.CONTAINERS_PER_SIDE || neighborContainerIndex.z >= VoxelWorld.CONTAINERS_PER_SIDE) 
                {
                    //outside the current batch
                    neighborBatchIndex = NeighbourBatchFromSampleVoxel(ref sampleVoxel);
                            
                    if (!VoxelMetaspace.metaspace.BatchLoaded(neighborBatchIndex))
                    {
                        neighboringGrids[neighborGridCacheIndex] = null;
                        return;
                    }
                
                    neighborContainerIndex = IndexMod(ref neighborContainerIndex, 5);
                }
                        
                neighboringGrids[neighborGridCacheIndex] = VoxelMetaspace.metaspace.TryGetVoxelGrid(neighborBatchIndex, neighborContainerIndex);
            }
        }
        
        public void NeighborDataUpdate() {
            for (int i = 0; i < paddingVoxels.Length; i++)
            {
                UpdateNeighborVoxel(ref paddingVoxels[i]);
            }
        }

        private void UpdateNeighborVoxel(ref Vector3Int voxel)
        {
            Vector3Int neighbourGridOffset = NeighbourGridOffsetFromPaddedVoxel(ref voxel);

            //Cache is offset by 1 bc indexes cant be negative :/
            VoxelGrid neighborGrid = neighboringGrids[(neighbourGridOffset.x + 1) + (neighbourGridOffset.y + 1) * 3 + (neighbourGridOffset.z + 1) * 9];
            if (neighborGrid == null)
            {
                SetVoxel(densityGrid, ref voxel, 0);
                SetVoxel(typeGrid, ref voxel, 0);
                return;
            }
            
            Vector3Int sampleVoxel = new Vector3Int(voxel.x, voxel.y, voxel.z);
            if (voxel.x == 0) sampleVoxel.x = VoxelWorld.GRID_RESOLUTION;
            else if (voxel.x == VoxelWorld.GRID_RESOLUTION + 1) sampleVoxel.x = 1;

            if (voxel.y == 0) sampleVoxel.y = VoxelWorld.GRID_RESOLUTION;
            else if (voxel.y == VoxelWorld.GRID_RESOLUTION + 1) sampleVoxel.y = 1;

            if (voxel.z == 0) sampleVoxel.z = VoxelWorld.GRID_RESOLUTION;
            else if (voxel.z == VoxelWorld.GRID_RESOLUTION + 1) sampleVoxel.z = 1;

            SetVoxel(densityGrid, ref voxel, GetVoxel(neighborGrid.densityGrid, sampleVoxel));
            SetVoxel(typeGrid, ref voxel, GetVoxel(neighborGrid.typeGrid, sampleVoxel));
        }
        
        private static Vector3Int NeighbourGridOffsetFromPaddedVoxel(ref Vector3Int voxel) {
            Vector3Int offset = Vector3Int.zero;
            if (voxel.x <= 0) offset.x = -1;                                                                                                
            else if (voxel.x >= VoxelWorld.GRID_RESOLUTION + GRID_PADDING) offset.x = 1;

            if (voxel.y <= 0) offset.y = -1;                                                                                                              
            else if (voxel.y >= VoxelWorld.GRID_RESOLUTION + GRID_PADDING) offset.y = 1;

            if (voxel.z <= 0) offset.z = -1;
            else if (voxel.z >= VoxelWorld.GRID_RESOLUTION + GRID_PADDING) offset.z = 1;

            return offset;
        }
        
        /// <summary>
        /// Gets a batch from a sample voxel offset (between -1 and 1).
        /// Uses absolute positioning for calculations
        /// </summary>
        private Vector3Int NeighbourBatchFromSampleVoxel(ref Vector3Int sampleVoxel)
        {
            Vector3Int batchPos = batchIndex * VoxelWorld.BATCH_WIDTH;
            Vector3Int octreePos = batchPos + (octreeIndex * VoxelWorld.OCTREE_WIDTH);
            int voxelWidth = 32 / VoxelWorld.GRID_RESOLUTION;
            Vector3Int voxelPos = octreePos + new Vector3Int((sampleVoxel.x) * voxelWidth * VoxelWorld.GRID_RESOLUTION, (sampleVoxel.y) * voxelWidth * VoxelWorld.GRID_RESOLUTION, (sampleVoxel.z) * voxelWidth * VoxelWorld.GRID_RESOLUTION);
            return new Vector3Int(
                Mathf.FloorToInt((float)voxelPos.x / VoxelWorld.BATCH_WIDTH),
                Mathf.FloorToInt((float)voxelPos.y / VoxelWorld.BATCH_WIDTH),
                Mathf.FloorToInt((float)voxelPos.z / VoxelWorld.BATCH_WIDTH)
            );
        }
        
        private static Vector3Int IndexMod(ref Vector3Int octreeIndex, int mod) => new Vector3Int((octreeIndex.x + mod) % mod, (octreeIndex.y + mod) % mod, (octreeIndex.z + mod) % mod);
        
        public void GetFullGrids(out NativeArray<byte> _fullDensityGrid, out NativeArray<byte> _fullTypeGrid) {
            _fullDensityGrid =   densityGrid;
            _fullTypeGrid =      typeGrid;
        }

        public BrushJob ApplyJobBasedDensityFunction(BrushStroke stroke, Vector3 gridOrigin)
        {
            BrushJob brushJob = null;
            
            if (stroke.brushMode == BrushMode.Smooth)
            {
                brushJob = new SmoothJob(this, stroke.brushLocation, stroke.brushRadius, stroke.strength, gridOrigin);
            }
            
            else if (stroke.brushMode == BrushMode.Add || stroke.brushMode == BrushMode.Remove )
            {
                bool shouldRemoveDensity = stroke.brushMode == BrushMode.Remove;
                brushJob = new AddSubJob(this, stroke.brushLocation, stroke.brushRadius, stroke.strength, CursorToolManager.main.brushTool.currentSelectedType, gridOrigin, shouldRemoveDensity);
            }

            else if (stroke.brushMode == BrushMode.Paint)
            {
                brushJob = new PaintJob(this, stroke.brushLocation, stroke.brushRadius, CursorToolManager.main.brushTool.currentSelectedType, gridOrigin);
            }

            brushJob?.StartJob();

            return brushJob;
        }
        
        /*
        public static float SampleDensity_Plane(Vector3 sample, Vector3 origin, Vector3 normal) {
            float d = -(origin.x * normal.x + origin.y * normal.y + origin.z * normal.z);
            return -(sample.x * normal.x + sample.y * normal.y + sample.z * normal.z + d);
        }
        
        void DensityAction_Flatten(int x, int y, int z, Vector3 gridOrigin, Brush.BrushStroke stroke) {

            // Make voxels solid below surface & inside brush
            // Make voxels empty above surface & inside brush

            // offset sample position because full grid
            float planeDensity = SampleDensity_Plane(new Vector3(x - 1, y - 1, z - 1) + gridOrigin, stroke.firstStrokePoint, stroke.firstStrokeNormal);
            float sphereDensity = SampleDensity_Sphere_Squared(new Vector3(x - 1, y - 1, z - 1) + gridOrigin, stroke.brushLocation, stroke.brushRadius);
            float clampedDensity = Mathf.Clamp(planeDensity, -1, 1);
            byte encodedDensity = OctNodeData.EncodeDensity(clampedDensity);

            // only affect voxels inside spherical brush
            if (sphereDensity > 0) {
                if (planeDensity != clampedDensity) {
                    SetVoxel(densityGrid, x, y, z, 0); // far node
                } else {
                    SetVoxel(densityGrid, x, y, z, encodedDensity);
                }

                if (encodedDensity >= 126) {
                    SetVoxel(typeGrid, x, y, z, Brush.selectedType);
                } else {
                    SetVoxel(typeGrid, x, y, z, 0); // solid node
                }
            }
        }
        */
        
        /// <summary>
        /// This should be called when closing the player to free the memory
        /// </summary>
        internal void DisposeGrids()
        {
            densityGrid.Dispose();
            typeGrid.Dispose();
        }

        /// <summary>
        /// Precompute the neighbors to check when smoothing to reduce needed calls
        /// </summary>
        internal static void PrecomputeNeighborOffsets()
        {
            int blurRadius = 1;
            List<int3> offsets = new List<int3>();

            for (int dz = -blurRadius; dz <= blurRadius; dz++) {
                for (int dy = -blurRadius; dy <= blurRadius; dy++) {
                    for (int dx = -blurRadius; dx <= blurRadius; dx++) {
                        // Skip the center voxel
                        if (dx == 0 && dy == 0 && dz == 0)
                            continue;

                        offsets.Add(new int3(dx, dy, dz));
                    }
                }
            }
            neighboursToCheckInSmooth = new NativeArray<int3>(offsets.ToArray(), Allocator.Persistent);
        }

        /// <summary>
        /// Precompute the neighbors to check when updating the internal neighboring voxel data
        /// </summary>
        internal static void PrecomputePaddingVoxels()
        {
            List<Vector3Int> offsets = new List<Vector3Int>();
            for (int z = 0; z < GRID_FULL_SIDE; z++) {
                for (int y = 0; y < GRID_FULL_SIDE; y++) {
                    for (int x = 0; x < GRID_FULL_SIDE; x++)
                    {
                        Vector3Int voxel = new Vector3Int(x, y, z);
                        if (NeighbourGridOffsetFromPaddedVoxel(ref voxel) == Vector3Int.zero) {
                            continue;
                        }
                        offsets.Add(new Vector3Int(x, y, z));
                    }
                }
            }
            paddingVoxels = offsets.ToArray();
        }

        /// <summary>
        /// Get the number of voxels within the grid.
        /// </summary>
        /// <returns>the number of voxels excluding the padding of 1 voxel that is present in each direction</returns>
        private static int GetFullGridSize()
        {
            return GRID_FULL_SIDE * GRID_FULL_SIDE * GRID_FULL_SIDE;
        }
        
        /// <summary>
        /// Get the number of voxels excluding the 1 voxel padding on each side
        /// </summary>
        /// <returns>the number of voxels excluding the padding of 1 voxel that is present in each direction</returns>
        internal static int GetGridInnerSize()
        {
            int innerSide = VoxelWorld.GRID_RESOLUTION;
            return innerSide * innerSide * innerSide;
        }
    }
}