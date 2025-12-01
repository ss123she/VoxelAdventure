using System.Runtime.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Terrain.Noise
{
    public static unsafe class TerrainNoiseUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryFastVerticalFill(
            int baseIdx, 
            float currentY, 
            float groundLevel, 
            float terrainHeight, 
            int chunkSize, 
            Unity.Collections.NativeArray<sbyte> data)
        {
            const float margin = 15.0f; 
            float limitTop = groundLevel + terrainHeight + margin;
            float limitBot = groundLevel - terrainHeight - margin;

            if (currentY > limitTop)
            {
                UnsafeUtility.MemSet(data.GetSubArray(baseIdx, chunkSize).GetUnsafePtr(), unchecked((byte)-127), chunkSize);
                return true;
            }
            if (currentY < limitBot)
            {
                UnsafeUtility.MemSet(data.GetSubArray(baseIdx, chunkSize).GetUnsafePtr(), (byte)127, chunkSize);
                return true;
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ComputeNoiseFBM(
            long seed,
            int chunkSize,
            float* xCoords,
            float* zCoords,
            float* outNoiseBuffer,
            NoiseJobData s)
        {
            UnsafeUtility.MemClear(outNoiseBuffer, chunkSize * sizeof(float));

            float amp = 1.0f;
            float freq = s.NoiseScale;

            for (int i = 0; i < s.Octaves; i++)
            {
                long octaveSeed = seed + (i * 59384);

                for (int z = 0; z < chunkSize; z++)
                {
                    float3 samplePos = new float3(xCoords[z], 0, zCoords[z]) * freq;
                    outNoiseBuffer[z] += OpenSimplex2S.Noise3_ImproveXZ(octaveSeed, samplePos) * amp;
                }

                freq *= s.Lacunarity;
                amp *= s.Persistence;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ComputeDomainWarping(
            long seed, 
            float3 startPos, 
            int chunkSize, 
            float scale, 
            float strength, 
            float* outX,
            float* outZ)
        {

            if (strength <= 0.001f)
            {
                for (int z = 0; z < chunkSize; z++)
                {
                    outX[z] = startPos.x;
                    outZ[z] = startPos.z + z;
                }
                return;
            }

            for (int z = 0; z < chunkSize; z++)
            {
                float3 currentPos = new(startPos.x, startPos.y, startPos.z + z);
                
                float3 warpSamplePos = 0.5f * scale * new float3(currentPos.x, 0, currentPos.z);
                
                float warpVal = OpenSimplex2S.Noise3_ImproveXZ(seed, warpSamplePos);
                
                outX[z] = currentPos.x + warpVal * strength; 
                outZ[z] = currentPos.z + warpVal * strength;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static sbyte PackSDF(float sdf)
        {
            if (sdf < -1.0f) sdf = -1.0f;
            else if (sdf > 1.0f) sdf = 1.0f;
            
            return (sbyte)(sdf * -127.0f);
        }
    }
}