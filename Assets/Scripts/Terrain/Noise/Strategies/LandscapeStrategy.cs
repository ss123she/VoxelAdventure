using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;

namespace Terrain.Noise.Strategies
{
    public unsafe struct LandscapeStrategy : ITerrainStrategy
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Execute(int baseIdx, float3 startPos, int chunkSize, ref NoiseJobData s, NativeArray<sbyte> data)
        {
            if (TerrainNoiseUtils.TryFastVerticalFill(baseIdx, startPos.y, s.GroundLevel, s.TerrainHeight, chunkSize, data))
                return;

            // [ WarpedX | WarpedZ | Noise ]
            float* memory = stackalloc float[chunkSize * 3];
            
            float* wx = memory;
            float* wz = memory + chunkSize;
            float* noise = memory + (chunkSize * 2);

            // Domain Warping
            TerrainNoiseUtils.ComputeDomainWarping(s.Seed, startPos, chunkSize, s.NoiseScale, s.WarpStrength, wx, wz);

            // FBM
            TerrainNoiseUtils.ComputeNoiseFBM(s.Seed, chunkSize, wx, wz, noise, s);

            // Apply
            for (int z = 0; z < chunkSize; z++)
            {
                float surfaceHeight = s.GroundLevel + noise[z] * s.TerrainHeight;
                float dist = startPos.y - surfaceHeight;
                
                data[baseIdx + z] = TerrainNoiseUtils.PackSDF(dist);
            }
        }
    }
}