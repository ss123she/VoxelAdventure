using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Terrain
{
    public interface ITerrainStrategy
    {
        float Execute(float3 pos, ref NoiseJobData settings);
    }

    public struct NoiseJobData
    {
        public float NoiseScale;
        public float TerrainHeight;
        public float GroundLevel;
        public int Octaves;
        public float Lacunarity;
        public float Persistence;
        public float CaveDensity;
    }

    public struct LandscapeStrategy : ITerrainStrategy
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Execute(float3 pos, ref NoiseJobData s)
        {
            var warp = pos;
            warp.x += NoiseUtils.GetSineNoise(pos, 0.04f, 2, 2.0f, 0.5f) * 4.0f;
            warp.z += NoiseUtils.GetSineNoise(pos + 15.5f, 0.04f, 2, 2.0f, 0.5f) * 4.0f;

            var noiseVal = NoiseUtils.GetSineNoise(warp, s.NoiseScale, s.Octaves, s.Lacunarity, s.Persistence);
            
            return pos.y - s.GroundLevel - noiseVal * s.TerrainHeight;
        }
    }

    public struct CavesStrategy : ITerrainStrategy
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Execute(float3 pos, ref NoiseJobData s)
        {
            var surfNoise = NoiseUtils.GetFractalNoise(pos, s.NoiseScale, s.Octaves, s.Lacunarity, s.Persistence);
            var surfaceSdf = pos.y - s.GroundLevel - surfNoise * s.TerrainHeight;

            var caveNoise = NoiseUtils.GetFractalNoise(pos, s.NoiseScale * 2.0f, s.Octaves, s.Lacunarity, s.Persistence);
            var caveSdf = caveNoise - s.CaveDensity;

            return math.max(surfaceSdf, -caveSdf);
        }
    }

    public struct GyroidStrategy : ITerrainStrategy
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float Execute(float3 pos, ref NoiseJobData s)
        {
            var scale = s.NoiseScale * 0.5f;
            var p = pos * scale;
            
            var val = math.sin(p.x) * math.cos(p.y) +
                      math.sin(p.y) * math.cos(p.z) +
                      math.sin(p.z) * math.cos(p.x);

            var finalSdf = math.abs(val) - 0.3f;
            var floorSdf = pos.y - s.GroundLevel;
            return math.max(finalSdf, -floorSdf);
        }
    }

    public static class NoiseUtils
    {
        private static readonly float3x3 RotationMatrix = new float3x3(
            0.00f,  0.80f,  0.60f,
            -0.80f,  0.36f, -0.48f,
            -0.60f, -0.48f,  0.64f
        );
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetFractalNoise(float3 pos, float frequency, int octaves, float lacunarity, float persistence)
        {
            var noiseValue = 0.0f;
            var amplitude = 1.0f;
            
            for (var i = 0; i < octaves; i++)
            {
                noiseValue += noise.snoise(pos * frequency) * amplitude;
                frequency *= lacunarity;
                amplitude *= persistence;
            }
            return noiseValue;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetSineNoise(float3 pos, float frequency, int octaves, float lacunarity, float persistence)
        {
            var p = pos * frequency;
            var sum = 0.0f;
            var amp = 1.0f;
            
            var pVec = new float4(p, 0);

            for (var i = 0; i < octaves; i++)
            {
                math.sincos(pVec, out var s, out var c);

                var n = math.dot(c.xyz, s.yzx); 

                sum += n * amp;

                amp *= persistence;
                
                var rotated = math.mul(RotationMatrix, pVec.xyz);
                pVec.xyz = rotated * lacunarity;
                
                pVec.xyz += 1.23f; 
            }
            return sum;
        }
    }

    [BurstCompile(FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Low, DisableSafetyChecks = true)] 
    public struct NoiseJob<TStrategy> : IJobParallelFor where TStrategy : struct, ITerrainStrategy
    {
        [WriteOnly] public NativeArray<sbyte> DataNative;
        
        [ReadOnly] public float3 Offset;
        [ReadOnly] public NoiseJobData Settings;
        [ReadOnly] public int ChunkSize;
        [ReadOnly] public TStrategy Strategy;

        [SkipLocalsInit]
        public void Execute(int index)
        {
            var iterX = index / ChunkSize;
            var iterY = index % ChunkSize;
            
            var baseArrayIdx = iterX * ChunkSize * ChunkSize + iterY * ChunkSize;
            
            var posX = iterX + Offset.x;
            var posY = iterY + Offset.y;
            var posZBase = Offset.z;

            for (var z = 0; z < ChunkSize; z++)
            {
                var worldPos = new float3(posX, posY, posZBase + z);
                
                var finalSdf = Strategy.Execute(worldPos, ref Settings);
                
                finalSdf = math.clamp(finalSdf, -1.0f, 1.0f);
                DataNative[baseArrayIdx + z] = (sbyte)(finalSdf * -127.0f);
            }
        }
    }
}