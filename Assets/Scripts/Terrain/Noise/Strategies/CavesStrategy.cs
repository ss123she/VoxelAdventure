using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;

namespace Terrain.Noise.Strategies
{
    public struct CavesStrategy : ITerrainStrategy
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Execute(int baseIdx, float3 startPos, int chunkSize, ref NoiseJobData s, NativeArray<sbyte> data)
        {
            if (startPos.y < -500) {}

            for (int z = 0; z < chunkSize; z++)
            {
                float3 pos = new(startPos.x, startPos.y, startPos.z + z);
                float3 seedPos = pos + s.Seed;

                float2 pos2D = new(seedPos.x, seedPos.z);
                float hNoise = NoiseUtils.GetFractalNoise2D(pos2D, s.NoiseScale, 3, s.Lacunarity, s.Persistence);
                float surfaceSdf = pos.y - s.GroundLevel - hNoise * s.TerrainHeight;

                if (surfaceSdf > 10.0f)
                {
                    data[baseIdx + z] = -127;
                    continue;
                }

                float warp = NoiseUtils.GetGradientNoise(seedPos * 0.02f) * 4.0f;
                float3 cavePos = seedPos + warp;
                
                float caveNoise = NoiseUtils.GetFractalNoise(cavePos, s.NoiseScale * 2.0f, 2, 2.0f, 0.5f);
                float caveSdf = caveNoise - s.CaveDensity;

                float finalSdf = math.max(surfaceSdf, -caveSdf);
                
                if (finalSdf < -1.0f) finalSdf = -1.0f;
                else if (finalSdf > 1.0f) finalSdf = 1.0f;

                data[baseIdx + z] = (sbyte)(finalSdf * -127.0f);
            }
        }
    }
}