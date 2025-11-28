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
            float limitTop = s.GroundLevel + s.TerrainHeight + 15.0f; 
            float limitBot = s.GroundLevel - s.TerrainHeight - 15.0f;

            if (startPos.y > limitTop)
            {
                for (int z = 0; z < chunkSize; z++) data[baseIdx + z] = -127; 
                return;
            }
            if (startPos.y < limitBot)
            {
                for (int z = 0; z < chunkSize; z++) data[baseIdx + z] = 127;
                return;
            }

            for (int z = 0; z < chunkSize; z++)
            {
                float3 currentPos = new(startPos.x, startPos.y, startPos.z + z);
                
                float2 pos2D = new(currentPos.x + s.Seed.x, currentPos.z + s.Seed.z);
                
                float warp2D = NoiseUtils.GetGradientNoise2D(pos2D * s.NoiseScale * 0.5f);
                float2 warpedPos2D = pos2D + warp2D * 5.0f;

                float noiseHeight = NoiseUtils.GetFractalNoise2D(warpedPos2D, s.NoiseScale, 3, s.Lacunarity, s.Persistence); 
                
                float surfaceHeight = s.GroundLevel + noiseHeight * s.TerrainHeight;
                float baseSdf = currentPos.y - surfaceHeight;

                if (baseSdf > -15.0f && baseSdf < 15.0f)
                {
                    float3 pos3D = currentPos + s.Seed;
                    pos3D.x += warp2D * 4.0f; 
                    
                    float noise3D = NoiseUtils.GetFractalNoise(pos3D, s.NoiseScale * 1.5f, 2, s.Lacunarity, s.Persistence);
                    
                    float archSdf = 0.2f - noise3D; 
                    
                    baseSdf = math.min(baseSdf, archSdf);
                }

                if (baseSdf < -1.0f) baseSdf = -1.0f;
                else if (baseSdf > 1.0f) baseSdf = 1.0f;
                
                data[baseIdx + z] = (sbyte)(baseSdf * -127.0f);
            }
        }
    }
}