using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;

namespace Terrain.Noise.Strategies
{
    public struct LandscapeStrategy : ITerrainStrategy
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Execute(int baseIdx, float3 startPos, int chunkSize, ref NoiseJobData s, NativeArray<sbyte> data)
        {
            var limitTop = s.GroundLevel + s.TerrainHeight + 15.0f; 
            var limitBot = s.GroundLevel - s.TerrainHeight - 15.0f;

            if (startPos.y > limitTop)
            {
                for (var z = 0; z < chunkSize; z++) data[baseIdx + z] = -127; 
                return;
            }
            
            if (startPos.y < limitBot)
            {
                for (var z = 0; z < chunkSize; z++) data[baseIdx + z] = 127;
                return;
            }

            for (var z = 0; z < chunkSize; z++)
            {
                var currentPos = new float3(startPos.x, startPos.y, startPos.z + z);
                
                var pos2D = new float2(currentPos.x + s.Seed.x, currentPos.z + s.Seed.z);
                
                var warp2D = NoiseUtils.GetGradientNoise2D(pos2D * s.NoiseScale * 0.5f);
                var warpedPos2D = pos2D + warp2D * 5.0f;

                var noiseHeight = NoiseUtils.GetFractalNoise2D(warpedPos2D, s.NoiseScale, 3, s.Lacunarity, s.Persistence); 
                
                var surfaceHeight = s.GroundLevel + noiseHeight * s.TerrainHeight;
                var baseSdf = currentPos.y - surfaceHeight;

                if (baseSdf is > -15.0f and < 15.0f)
                {
                    var pos3D = currentPos + s.Seed;
                    pos3D.x += warp2D * 4.0f; 
                    
                    var noise3D = NoiseUtils.GetFractalNoise(pos3D, s.NoiseScale * 1.5f, 2, s.Lacunarity, s.Persistence);
                    
                    var archSdf = 0.2f - noise3D; 
                    
                    baseSdf = math.min(baseSdf, archSdf);
                }

                if (baseSdf < -1.0f) baseSdf = -1.0f;
                else if (baseSdf > 1.0f) baseSdf = 1.0f;
                
                data[baseIdx + z] = (sbyte)(baseSdf * -127.0f);
            }
        }
    }
}