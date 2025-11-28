<<<<<<< HEAD
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
            
            for (int z = 0; z < chunkSize; z++)
            {
                float3 pos = new float3(startPos.x, startPos.y, startPos.z + z);
                var p = (pos + s.Seed) * scale;
                
                float3 warpVec;
                warpVec.x = NoiseUtils.GetGradientNoise(p * 0.4f);
                warpVec.y = NoiseUtils.GetGradientNoise((p + 100) * 0.4f);
                warpVec.z = NoiseUtils.GetGradientNoise((p + 200) * 0.4f);
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
=======
namespace Terrain.Noise.Strategies
{
    public class GyroidStrategy
    {
        
>>>>>>> fd4fb025f38ce06c097181230c65bf81b8998614
    }
}