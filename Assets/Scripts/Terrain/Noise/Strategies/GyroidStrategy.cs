using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;

namespace Terrain.Noise.Strategies
{
    public unsafe struct GyroidStrategy : ITerrainStrategy
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Execute(int baseIdx, float3 startPos, int chunkSize, ref NoiseJobData s, NativeArray<sbyte> data)
        {
            // Memory Layout: [ WarpedX | WarpedZ ]
            float* memory = stackalloc float[chunkSize * 2];
            
            float* wx = memory;
            float* wz = memory + chunkSize;

            // Domain Warping
            TerrainNoiseUtils.ComputeDomainWarping(s.Seed, startPos, chunkSize, s.NoiseScale, s.WarpStrength, wx, wz);

            // Calculation & Apply
            float scale = s.NoiseScale * 0.5f;

            for (int z = 0; z < chunkSize; z++)
            {
                float3 pos = new(wx[z], startPos.y, wz[z]);
                float3 p = pos * scale;

                // Gyroid: sin(x)cos(y) + sin(y)cos(z) + sin(z)cos(x)
                var val = math.sin(p.x) * math.cos(p.y) +
                          math.sin(p.y) * math.cos(p.z) +
                          math.sin(p.z) * math.cos(p.x);

                var finalSdf = math.abs(val) - 0.3f;
                
                var floorSdf = pos.y - s.GroundLevel;
                finalSdf = math.max(finalSdf, -floorSdf);

                // Packing
                data[baseIdx + z] = TerrainNoiseUtils.PackSDF(finalSdf);
            }
        }
    }
}
