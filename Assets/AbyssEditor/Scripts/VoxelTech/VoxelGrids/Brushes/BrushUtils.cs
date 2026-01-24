using System;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
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
            
            /// <summary>
            /// Get the voxel byte data within the padded region of the grid passed
            /// </summary>
            /// <returns>the byte form the position of the voxel in the array</returns>
            [BurstCompile]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static byte GetVoxelData(NativeArray<byte> arr, int3 voxel, int voxelGridResolution)
            {
                return arr[voxel.x + voxel.y * voxelGridResolution + voxel.z * voxelGridResolution * voxelGridResolution];
            }
            
            /// <summary>
            /// Set the voxel byte data within the padded region of the grid passed
            /// </summary>
            /// <returns>the byte form the position of the voxel in the array</returns>
            [BurstCompile]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void SetVoxelData(NativeArray<byte> arr, int3 voxel, int voxelGridResolution, byte val)
            {
                arr[voxel.x + voxel.y * voxelGridResolution + voxel.z * voxelGridResolution * voxelGridResolution] = val;
            }
        }
        
        
        public static class Managed
        {
            public static float SampleDensity_Sphere_Squared(Vector3 sample, Vector3 origin, float radius)
            {
                return radius * radius - (sample - origin).sqrMagnitude;
            }
            
            /// <summary>
            /// Gets the voxel within the non-padded region of the grid based on the index from a IJobFor.
            /// 0,0,0 would actually be 1,1,1 of the padded grid
            /// </summary>
            /// <returns></returns>
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
