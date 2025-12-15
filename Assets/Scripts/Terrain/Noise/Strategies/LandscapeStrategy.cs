using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;

namespace Terrain.Noise.Strategies
{
    public unsafe struct LandscapeStrategy : ITerrainStrategy
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly void Execute(int baseIdx, float3 startPos, int chunkSize, ref NoiseJobData s, NativeArray<sbyte> data)
        {
            if (TerrainNoiseUtils.TryFastVerticalFill(baseIdx, startPos.y, s.GroundLevel, s.TerrainHeight, chunkSize, data))
                return;

            // Stack struct:
            // WarpedX
            // WarpedZ
            // Surface Noise
            float* memory = stackalloc float[chunkSize * 4];
            
            float* wx = memory;
            float* wz = memory + chunkSize;
            float* surfaceNoise = memory + (chunkSize * 2);

            // Domain Warping
            TerrainNoiseUtils.ComputeDomainWarping(s.Seed, startPos, chunkSize, s.NoiseScale, s.WarpStrength, wx, wz);

            // FBM Surface
            TerrainNoiseUtils.ComputeNoiseFBM(s.Seed, chunkSize, wx, wz, surfaceNoise, s);

            // Apply
            for (int z = 0; z < chunkSize; z++)
            {
                // Surface
                float surfaceHeight = s.GroundLevel + surfaceNoise[z] * s.TerrainHeight;
                
                // dist < 0 = Earth, dist > 0 = Air
                float dist = startPos.y - surfaceHeight;
                
                // Pack SDF
                data[baseIdx + z] = TerrainNoiseUtils.PackSDF(dist);
            }
        }
    }
}