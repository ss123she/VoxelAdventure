using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;

namespace Terrain.Noise.Strategies
{
    public struct GyroidStrategy : ITerrainStrategy
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Execute(int baseIdx, float3 startPos, int chunkSize, ref NoiseJobData s, NativeArray<sbyte> data)
        {
            float scale = s.NoiseScale * 0.5f;
            long seed = (long)math.hash(s.Seed);
            
            for (int z = 0; z < chunkSize; z++)
            {
                float3 pos = new float3(startPos.x, startPos.y, startPos.z + z);
                float3 p = pos * scale;
                
                float3 warpVec;
                float3 warpInput = p * 0.4f;
                
                warpVec.x = OpenSimplex2S.Noise3_ImproveXZ(seed, warpInput);
                warpVec.y = OpenSimplex2S.Noise3_ImproveXZ(seed + 100, warpInput);
                warpVec.z = OpenSimplex2S.Noise3_ImproveXZ(seed + 200, warpInput);
                
                p += warpVec * 2.5f; 

                var val = math.sin(p.x) * math.cos(p.y) +
                          math.sin(p.y) * math.cos(p.z) +
                          math.sin(p.z) * math.cos(p.x);

                var finalSdf = math.abs(val) - 0.3f;
                var floorSdf = pos.y - s.GroundLevel;
                
                finalSdf = math.max(finalSdf, -floorSdf);

                if (finalSdf < -1.0f) finalSdf = -1.0f;
                else if (finalSdf > 1.0f) finalSdf = 1.0f;
                
                data[baseIdx + z] = (sbyte)(finalSdf * -127.0f);
            }
        }
    }
}