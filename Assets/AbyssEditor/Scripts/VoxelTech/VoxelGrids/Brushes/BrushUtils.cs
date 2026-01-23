using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;
namespace AbyssEditor.Scripts.VoxelTech.VoxelGrids.Brushes
{
    
    /// <summary>
    /// This is a collection of util functions.
    /// There is Burst code that can be used within Burst Compiled Jobs and managed code for non burst compiled
    /// </summary>
    public static class BrushUtils
    {
        
        public static class Bursted
        {
            [BurstCompile]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static float SampleDensity_Sphere_Squared(float3 sample, float3 brushLocation, float brushRadius)
            {
                float3 d = sample - brushLocation;
                return brushRadius * brushRadius - math.lengthsq(d);
            }

            /// <summary>
            /// Gets the voxel within the non-padded region of the grid.
            /// 0,0,0 would actually be 1,1,1 of the padded grid
            /// </summary>
            /// <returns></returns>
            [BurstCompile]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static int3 GetVoxelFromIndex(int index, int voxelGridResolution)
            {
                // Map linear index to 3D inner coordinates
                int innerSide = voxelGridResolution - 2;
                int z = index / (innerSide * innerSide);
                int y = (index / innerSide) % innerSide;
                int x = index % innerSide;

                x += 1 ;
                y += 1;
                z += 1;
                
                return new int3(x, y, z);
            }
        }
        
        public static class Managed
        {
            //TODO: MIGRATE THIS TO BRUSH UTILS
            public static float SampleDensity_Sphere_Squared(Vector3 sample, Vector3 origin, float radius)
            {
                return radius * radius - (sample - origin).sqrMagnitude;
            }
            
            /// <summary>
            /// Gets the voxel within the non-padded region of the grid based on the index from a IJobFor.
            /// 0,0,0 would actually be 1,1,1 of the padded grid
            /// </summary>
            /// <returns></returns>
            [BurstCompile]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static Vector3Int GetVoxelFromIndex(int index)
            {
                // Map linear index to 3D inner coordinates
                int innerSide = VoxelWorld.RESOLUTION;
                int z = index / (innerSide * innerSide);
                int y = (index / innerSide) % innerSide;
                int x = index % innerSide;

                x += 1 ;
                y += 1;
                z += 1;
                
                return new Vector3Int(x, y, z);
            }
        }
    }
}
