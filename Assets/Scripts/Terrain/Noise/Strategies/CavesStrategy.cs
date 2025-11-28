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

            long seed = (long)math.hash(s.Seed);

            for (int z = 0; z < chunkSize; z++)
            {
                float3 pos = new(startPos.x, startPos.y, startPos.z + z);

                float hNoise = 0.0f;
                float amp = 1.0f;
                float freq = s.NoiseScale;
                float3 pos2D = new float3(pos.x, 0, pos.z);

                for (int i = 0; i < 3; i++)
                {
                    hNoise += OpenSimplex2S.Noise3_ImproveXZ(seed + i * 133, pos2D * freq) * amp;
                    freq *= s.Lacunarity;
                    amp *= s.Persistence;
                }

                float surfaceSdf = pos.y - s.GroundLevel - hNoise * s.TerrainHeight;

                if (surfaceSdf > 10.0f)
                {
                    data[baseIdx + z] = -127;
                    continue;
                }

                float warp = OpenSimplex2S.Noise3_ImproveXZ(seed, pos * 0.02f) * 4.0f;
                float3 cavePos = pos;
                cavePos.x += warp;
                cavePos.z += warp;
                
                float caveNoise = 0.0f;
                amp = 1.0f;
                freq = s.NoiseScale * 2.0f;

                for (int i = 0; i < 2; i++)
                {
                    caveNoise += OpenSimplex2S.Noise3_ImproveXZ(seed + 500 + i * 133, cavePos * freq) * amp;
                    freq *= 2.0f;
                    amp *= 0.5f;
                }

                float caveSdf = caveNoise - s.CaveDensity;
                float finalSdf = math.max(surfaceSdf, -caveSdf);
                
                if (finalSdf < -1.0f) finalSdf = -1.0f;
                else if (finalSdf > 1.0f) finalSdf = 1.0f;

                data[baseIdx + z] = (sbyte)(finalSdf * -127.0f);
            }
        }
    }
}