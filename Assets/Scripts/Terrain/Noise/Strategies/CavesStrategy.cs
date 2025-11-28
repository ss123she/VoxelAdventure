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
            if (startPos.y < -500) { }

            for (var z = 0; z < chunkSize; z++)
            {
                var pos = new float3(startPos.x, startPos.y, startPos.z + z);
                var seedPos = pos + s.Seed;

                var pos2D = new float2(seedPos.x, seedPos.z);
                var hNoise = NoiseUtils.GetFractalNoise2D(pos2D, s.NoiseScale, 3, s.Lacunarity, s.Persistence);
                var surfaceSdf = pos.y - s.GroundLevel - hNoise * s.TerrainHeight;

                if (surfaceSdf > 10.0f)
                {
                    data[baseIdx + z] = -127;
                    continue;
                }

                var warp = NoiseUtils.GetGradientNoise(seedPos * 0.02f) * 4.0f;
                var cavePos = seedPos + warp;
                
                var caveNoise = NoiseUtils.GetFractalNoise(cavePos, s.NoiseScale * 2.0f, 2, 2.0f, 0.5f);
                var caveSdf = caveNoise - s.CaveDensity;

                var finalSdf = math.max(surfaceSdf, -caveSdf);
                
                if (finalSdf < -1.0f) finalSdf = -1.0f;
                else if (finalSdf > 1.0f) finalSdf = 1.0f;

                data[baseIdx + z] = (sbyte)(finalSdf * -127.0f);
            }
        }
    }
}