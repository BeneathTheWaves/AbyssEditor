using System.Collections.Generic;
using AbyssEditor.Scripts.VoxelTech.VoxelGrids.Brushes;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

namespace AbyssEditor.Scripts.VoxelTech.VoxelGrids {
    public class VoxelGrid
    {
        public const int GRID_PADDING = 1;
        public static int GRID_FULL_SIDE = VoxelWorld.RESOLUTION + 2;
        
        public NativeArray<byte> densityGrid;
        public NativeArray<byte> typeGrid;
        public Vector3Int fullGridDim;
        public readonly Vector3Int octreeIndex;
        public readonly Vector3Int batchIndex;
        bool[] neighbourMap;
        
        public static NativeArray<int3> neighboursToCheckInSmooth;
        public static Vector3Int[] paddingVoxels;

        public VoxelGrid(NativeArray<byte> _coreDensity, NativeArray<byte> _coreTypes, Vector3Int _octreeIndex, Vector3Int _batchIndex)
        {
            int gridSize = GetFullGridSize();
            densityGrid = new NativeArray<byte>(gridSize, Allocator.Persistent);
            typeGrid = new NativeArray<byte>(gridSize, Allocator.Persistent);
            
            for (int z = 0; z < VoxelWorld.RESOLUTION; z++) {
                for (int y = 0; y < VoxelWorld.RESOLUTION; y++) {
                    for (int x = 0; x < VoxelWorld.RESOLUTION; x++) {
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
            return array[Globals.LinearIndex(x, y, z, VoxelWorld.RESOLUTION + padding*2)];
        }
        public static byte GetVoxel(NativeArray<byte> array, Vector3Int voxel) {
            return array[Globals.LinearIndex(voxel.x, voxel.y, voxel.z, VoxelWorld.RESOLUTION + GRID_PADDING * 2)];
        }
        
        public static void SetVoxel(NativeArray<byte> array, int x, int y, int z, byte val) {
            array[Globals.LinearIndex(x, y, z, VoxelWorld.RESOLUTION + GRID_PADDING*2)] = val;
        }
        
        public static void SetVoxel(NativeArray<byte> array, Vector3Int voxel, byte val) {
            array[Globals.LinearIndex(voxel.x, voxel.y, voxel.z, VoxelWorld.RESOLUTION + GRID_PADDING*2)] = val;
        }

        public void NeighborDataUpdate() {
            //neighbourMap = new bool[27];
            //neighbourMap[13] = true;
            foreach (Vector3Int voxel in paddingVoxels)
            { 
                UpdateNeighborVoxel(voxel);
            }
        }

        private void UpdateNeighborVoxel(Vector3Int voxel)
        {
            VoxelGrid neighborGrid;
            Vector3Int neighbourGridOffset = NeighbourGridOffsetFromPaddedVoxel(voxel);

            Vector3Int neighborContainerIndex = octreeIndex + neighbourGridOffset;
            Vector3Int neighborBatchIndex = batchIndex;
            
            if (neighborContainerIndex.x < 0 || neighborContainerIndex.y < 0 || neighborContainerIndex.z < 0 ||
                neighborContainerIndex.x >= VoxelWorld.CONTAINERS_PER_SIDE || neighborContainerIndex.y >= VoxelWorld.CONTAINERS_PER_SIDE || neighborContainerIndex.z >= VoxelWorld.CONTAINERS_PER_SIDE) 
            {
                //outside the current batch
                neighborBatchIndex = NeighbourBatchFromPaddedVoxel(voxel.x, voxel.y, voxel.z);
                if (!VoxelMetaspace.metaspace.BatchLoaded(neighborBatchIndex))
                {
                    return;
                }
                
                neighborContainerIndex = IndexMod(neighborContainerIndex, 5);
            }
            
            neighborGrid = VoxelMetaspace.metaspace.TryGetVoxelGrid(neighborBatchIndex, neighborContainerIndex);

            Vector3Int sample = new Vector3Int(voxel.x, voxel.y, voxel.z);
            if (voxel.x == 0) sample.x = VoxelWorld.RESOLUTION;
            else if (voxel.x == VoxelWorld.RESOLUTION + 1) sample.x = 1;

            if (voxel.y == 0) sample.y = VoxelWorld.RESOLUTION;
            else if (voxel.y == VoxelWorld.RESOLUTION + 1) sample.y = 1;

            if (voxel.z == 0) sample.z = VoxelWorld.RESOLUTION;
            else if (voxel.z == VoxelWorld.RESOLUTION + 1) sample.z = 1;

            SetVoxel(densityGrid, voxel, GetVoxel(neighborGrid.densityGrid, sample));
            SetVoxel(typeGrid, voxel, GetVoxel(neighborGrid.typeGrid, sample));
        }
        
        internal static Vector3Int NeighbourGridOffsetFromPaddedVoxel(Vector3Int voxel) {
            Vector3Int offset = Vector3Int.zero;
            if (voxel.x <= 0) offset.x = -1;                                                                                                
            else if (voxel.x >= VoxelWorld.RESOLUTION + GRID_PADDING) offset.x = 1;

            if (voxel.y <= 0) offset.y = -1;                                                                                                              
            else if (voxel.y >= VoxelWorld.RESOLUTION + GRID_PADDING) offset.y = 1;

            if (voxel.z <= 0) offset.z = -1;
            else if (voxel.z >= VoxelWorld.RESOLUTION + GRID_PADDING) offset.z = 1;

            return offset;
        }
        
        /// <summary>
        /// Gets a batch from a voxel.
        /// Uses absolute positioning for calculations
        /// </summary>
        internal Vector3Int NeighbourBatchFromPaddedVoxel(int x, int y, int z)
        {
            Vector3Int batchPos = batchIndex * VoxelWorld.BATCH_WIDTH;
            Vector3Int octreePos = batchPos + (octreeIndex * VoxelWorld.OCTREE_WIDTH);
            int voxelWidth = 32 / VoxelWorld.RESOLUTION;
            Vector3Int voxelPos = octreePos + new Vector3Int((x - 1) * voxelWidth, (y - 1) * voxelWidth, (z - 1) * voxelWidth);
            
            return new Vector3Int(
                Mathf.FloorToInt((float)voxelPos.x / VoxelWorld.BATCH_WIDTH),
                Mathf.FloorToInt((float)voxelPos.y / VoxelWorld.BATCH_WIDTH),
                Mathf.FloorToInt((float)voxelPos.z / VoxelWorld.BATCH_WIDTH)
            );
        }
        
        internal static Vector3Int IndexMod(Vector3Int octreeIndex, int mod) => new Vector3Int((octreeIndex.x + mod) % mod, (octreeIndex.y + mod) % mod, (octreeIndex.z + mod) % mod);
        
        public void GetFullGrids(out NativeArray<byte> _fullDensityGrid, out NativeArray<byte> _fullTypeGrid) {
            _fullDensityGrid =   densityGrid;
            _fullTypeGrid =      typeGrid;
        }

        public BrushJob ApplyJobBasedDensityFunction(Brush.BrushStroke stroke, Vector3 gridOrigin)
        {
            BrushJob brushJob = null;
            
            if (stroke.brushMode == BrushMode.Smooth)
            {
                brushJob = new SmoothJob(this, stroke.brushLocation, stroke.brushRadius, stroke.strength, gridOrigin);
            }
            
            else if (stroke.brushMode == BrushMode.Add || stroke.brushMode == BrushMode.Remove )
            {
                bool shouldRemoveDensity = stroke.brushMode == BrushMode.Remove;
                brushJob = new AddSubJob(this, stroke.brushLocation, stroke.brushRadius, stroke.strength, Brush.selectedType, gridOrigin, shouldRemoveDensity);
            }

            else if (stroke.brushMode == BrushMode.Paint)
            {
                brushJob = new PaintJob(this, stroke.brushLocation, stroke.brushRadius, Brush.selectedType, gridOrigin);
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
            int _fullSide = VoxelWorld.RESOLUTION + 2;
            
            List<Vector3Int> offsets = new List<Vector3Int>();
            for (int z = 0; z < _fullSide; z++) {
                for (int y = 0; y < _fullSide; y++) {
                    for (int x = 0; x < _fullSide; x++) {
                        if (NeighbourGridOffsetFromPaddedVoxel(new Vector3Int(x, y, z)) == Vector3Int.zero) {
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
            int innerSide = VoxelWorld.RESOLUTION;
            return innerSide * innerSide * innerSide;
        }
    }
}