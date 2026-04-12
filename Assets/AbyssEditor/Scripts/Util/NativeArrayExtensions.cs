using AbyssEditor.Scripts.VoxelTech;
using AbyssEditor.Scripts.VoxelTech.VoxelMeshing.VoxelGrids;
using Unity.Collections;
namespace AbyssEditor.Scripts.Util
{
    public static class NativeArrayExtensions
    {
        public static bool IsIdenticalTo(this NativeArray<byte> array1, NativeArray<byte> array2)
        {
            if (array1.Length != array2.Length) return false;

            for(int x = 0; x < VoxelWorld.GRID_RESOLUTION; x++)
            for(int y = 0; y < VoxelWorld.GRID_RESOLUTION; y++)
            for(int z = 0; z < VoxelWorld.GRID_RESOLUTION; z++)
            {
                int index = Utils.LinearIndex(x + VoxelGrid.GRID_PADDING, y + VoxelGrid.GRID_PADDING, z + VoxelGrid.GRID_PADDING, VoxelWorld.PADDED_GRID_RESOLUTION);
                if (array1[index] != array2[index])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
