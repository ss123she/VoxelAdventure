using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace Terrain.Noise
{
    public static class NoiseUtils
    {
        private const uint kX = 0x1a872343; 
        private const uint kY = 0x5c42f837; 
        private const uint kZ = 0x3ac5d6ab; 

        private static readonly float3x3 RotationMatrix = new float3x3(
            0.00f,  0.80f,  0.60f,
            -0.80f,  0.36f, -0.48f,
            -0.60f, -0.48f,  0.64f
        );

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float3 HashGradient(int3 p)
        {
            uint h = (uint)p.x * kX + (uint)p.y * kY + (uint)p.z * kZ;
            h ^= h >> 15; h *= 0x735a2d97; h ^= h >> 15;
            float x = (h & 0x000F) / 7.5f - 1.0f; 
            float y = ((h >> 4) & 0x000F) / 7.5f - 1.0f;
            float z = ((h >> 8) & 0x000F) / 7.5f - 1.0f;
            return math.normalize(new float3(x, y, z));
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float2 HashGradient2D(int2 p)
        {
            uint h = (uint)p.x * kX + (uint)p.y * kZ;
            h ^= h >> 15; h *= 0x735a2d97; h ^= h >> 15;
            float x = (h & 0x000F) / 7.5f - 1.0f; 
            float y = ((h >> 4) & 0x000F) / 7.5f - 1.0f;
            return math.normalize(new float2(x, y));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetGradientNoise2D(float2 p)
        {
            float2 i = math.floor(p);
            float2 f = p - i;
            float2 u = f * f * f * (f * (f * 6.0f - 15.0f) + 10.0f);
            int2 i2 = (int2)i;

            float n00 = math.dot(HashGradient2D(i2 + new int2(0, 0)), f - new float2(0, 0));
            float n10 = math.dot(HashGradient2D(i2 + new int2(1, 0)), f - new float2(1, 0));
            float n01 = math.dot(HashGradient2D(i2 + new int2(0, 1)), f - new float2(0, 1));
            float n11 = math.dot(HashGradient2D(i2 + new int2(1, 1)), f - new float2(1, 1));

            return math.lerp(math.lerp(n00, n10, u.x), math.lerp(n01, n11, u.x), u.y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetGradientNoise(float3 p)
        {
            float3 i = math.floor(p);
            float3 f = p - i;
            float3 u = f * f * f * (f * (f * 6.0f - 15.0f) + 10.0f);
            int3 i3 = (int3)i;

            float n000 = math.dot(HashGradient(i3 + new int3(0, 0, 0)), f - new float3(0, 0, 0));
            float n100 = math.dot(HashGradient(i3 + new int3(1, 0, 0)), f - new float3(1, 0, 0));
            float n010 = math.dot(HashGradient(i3 + new int3(0, 1, 0)), f - new float3(0, 1, 0));
            float n110 = math.dot(HashGradient(i3 + new int3(1, 1, 0)), f - new float3(1, 1, 0));
            float n001 = math.dot(HashGradient(i3 + new int3(0, 0, 1)), f - new float3(0, 0, 1));
            float n101 = math.dot(HashGradient(i3 + new int3(1, 0, 1)), f - new float3(1, 0, 1));
            float n011 = math.dot(HashGradient(i3 + new int3(0, 1, 1)), f - new float3(0, 1, 1));
            float n111 = math.dot(HashGradient(i3 + new int3(1, 1, 1)), f - new float3(1, 1, 1));

            return math.lerp(
                math.lerp(math.lerp(n000, n100, u.x), math.lerp(n010, n110, u.x), u.y),
                math.lerp(math.lerp(n001, n101, u.x), math.lerp(n011, n111, u.x), u.y),
                u.z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetFractalNoise(float3 pos, float frequency, int octaves, float lacunarity, float persistence)
        {
            float sum = 0.0f;
            float amp = 1.0f;
            float3 p = pos * frequency;
            float3 shift = new float3(100, 100, 100); 

            for (int i = 0; i < octaves; i++)
            {
                sum += GetGradientNoise(p) * amp;
                p = math.mul(RotationMatrix, p + shift); 
                p *= lacunarity;
                amp *= persistence;
            }
            return sum;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetFractalNoise2D(float2 pos, float frequency, int octaves, float lacunarity, float persistence)
        {
            float sum = 0.0f;
            float amp = 1.0f;
            float2 p = pos * frequency;
            
            for (int i = 0; i < octaves; i++)
            {
                sum += GetGradientNoise2D(p) * amp;
                float2 old = p;
                p.x = old.x * 0.6f - old.y * 0.8f;
                p.y = old.x * 0.8f + old.y * 0.6f;
                p *= lacunarity;
                amp *= persistence;
            }
            return sum;
        }
    }
}