using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

namespace Terrain.Noise.Strategies
{
public unsafe struct LandscapeStrategy : ITerrainStrategy // Добавили unsafe для stackalloc
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Execute(int baseIdx, float3 startPos, int chunkSize, ref NoiseJobData s, NativeArray<sbyte> data)
    {            
        float limitTop = s.GroundLevel + s.TerrainHeight + 15.0f; 
        float limitBot = s.GroundLevel - s.TerrainHeight - 15.0f;

        if (startPos.y > limitTop)
        {
            UnsafeUtility.MemSet(data.GetSubArray(baseIdx, chunkSize).GetUnsafePtr(), unchecked((byte)-127), chunkSize);
            return;
        }
        if (startPos.y < limitBot)
        {
            UnsafeUtility.MemSet(data.GetSubArray(baseIdx, chunkSize).GetUnsafePtr(), (byte)127, chunkSize);
            return;
        }

        long seed = math.hash(s.Seed);

        float* noiseBuffer = stackalloc float[chunkSize];
        float* warpedX = stackalloc float[chunkSize];
        float* warpedZ = stackalloc float[chunkSize];

        for (int z = 0; z < chunkSize; z++)
        {
            float3 currentPos = new(startPos.x, startPos.y, startPos.z + z);
            float3 warpCoords = 0.5f * s.NoiseScale * new float3(currentPos.x, 0, currentPos.z);
            
            float warp2D = OpenSimplex2S.Noise3_ImproveXZ(seed, warpCoords);
            
            warpedX[z] = currentPos.x + warp2D * 5.0f;
            warpedZ[z] = currentPos.z + warp2D * 5.0f;
            noiseBuffer[z] = 0.0f;
        }

        float amp = 1.0f;
        float freq = s.NoiseScale;

        for (int i = 0; i < s.Octaves; i++)
        {
            long octaveSeed = seed + i * 100;

            for (int z = 0; z < chunkSize; z++)
            {
                float3 samplePos = new float3(warpedX[z], 0, warpedZ[z]) * freq;
                noiseBuffer[z] += OpenSimplex2S.Noise3_ImproveXZ(octaveSeed, samplePos) * amp;
            }

            freq *= s.Lacunarity;
            amp *= s.Persistence;
        }

        for (int z = 0; z < chunkSize; z++)
        {
            float surfaceHeight = s.GroundLevel + noiseBuffer[z] * s.TerrainHeight;
            float baseSdf = startPos.y - surfaceHeight;

            if (baseSdf < -1.0f) baseSdf = -1.0f;
            else if (baseSdf > 1.0f) baseSdf = 1.0f;
            
            data[baseIdx + z] = (sbyte)(baseSdf * -127.0f);
        }
    }
}}