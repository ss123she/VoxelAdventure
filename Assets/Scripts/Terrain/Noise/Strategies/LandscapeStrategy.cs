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

            // [X coords | Z coords | Surface Heights]
            float* wx = stackalloc float[chunkSize * 3];
            float* wz = wx + chunkSize;
            float* surfaceNoise = wz + chunkSize;

            TerrainNoiseUtils.ComputeDomainWarping(s.Seed, startPos, chunkSize, s.NoiseScale, s.WarpStrength, wx, wz);
            TerrainNoiseUtils.ComputeNoiseFBM(s.Seed, chunkSize, wx, wz, surfaceNoise, s);

            const float ReefHeight = 8.0f;
            const float ReefCap = 7.5f;
            const float BiomeThr = 0.5f;

            for (int z = 0; z < chunkSize; z++)
            {
                float dist = startPos.y - (s.GroundLevel + surfaceNoise[z] * s.TerrainHeight);
                float finalDist = dist;

                if (dist > 0 && dist < ReefHeight)
                {
                    float biomeVal = OpenSimplex2S.Noise3_ImproveXZ(s.Seed + 333, new float3(wx[z], 0, wz[z]) * 0.005f);

                    if (biomeVal > BiomeThr)
                    {
                        float thickness = math.lerp(0.20f, 0.45f, math.pow(1.0f - (dist / ReefHeight), 0.25f));
                        thickness *= math.smoothstep(BiomeThr, BiomeThr + 0.1f, biomeVal);

                        float3 reefPos = new float3(wx[z], startPos.y * 0.4f, wz[z]) * (s.NoiseScale * 3.0f);
                        float reefStruct = math.abs(OpenSimplex2S.Noise3_ImproveXZ(s.Seed + 777, reefPos));

                        finalDist = math.min(finalDist, math.max(reefStruct - thickness, dist - ReefCap));
                    }
                }

                data[baseIdx + z] = TerrainNoiseUtils.PackSDF(finalDist);
            }
        }
    }
}