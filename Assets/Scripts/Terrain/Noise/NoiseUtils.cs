using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace Terrain.Noise
{
    public static class NoiseUtils
    {
        private const uint Kx = 0x1a872343; 
        private const uint Ky = 0x5c42f837; 
        private const uint Kz = 0x3ac5d6ab; 

        private static readonly float3x3 RotationMatrix = new float3x3(
            0.00f,  0.80f,  0.60f,
            -0.80f,  0.36f, -0.48f,
            -0.60f, -0.48f,  0.64f
        );

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float3 HashGradient(int3 p)
        {
            var h = (uint)p.x * Kx + (uint)p.y * Ky + (uint)p.z * Kz;
            h ^= h >> 15; h *= 0x735a2d97; h ^= h >> 15;
            var x = (h & 0x000F) / 7.5f - 1.0f; 
            var y = ((h >> 4) & 0x000F) / 7.5f - 1.0f;
            var z = ((h >> 8) & 0x000F) / 7.5f - 1.0f;
            return math.normalize(new float3(x, y, z));
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float2 HashGradient2D(int2 p)
        {
            var h = (uint)p.x * Kx + (uint)p.y * Kz;
            h ^= h >> 15; h *= 0x735a2d97; h ^= h >> 15;
            var x = (h & 0x000F) / 7.5f - 1.0f; 
            var y = ((h >> 4) & 0x000F) / 7.5f - 1.0f;
            return math.normalize(new float2(x, y));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetGradientNoise2D(float2 p)
        {
            var i = math.floor(p);
            var f = p - i;
            var u = f * f * f * (f * (f * 6.0f - 15.0f) + 10.0f);
            var i2 = (int2)i;

            var n00 = math.dot(HashGradient2D(i2 + new int2(0, 0)), f - new float2(0, 0));
            var n10 = math.dot(HashGradient2D(i2 + new int2(1, 0)), f - new float2(1, 0));
            var n01 = math.dot(HashGradient2D(i2 + new int2(0, 1)), f - new float2(0, 1));
            var n11 = math.dot(HashGradient2D(i2 + new int2(1, 1)), f - new float2(1, 1));

            return math.lerp(math.lerp(n00, n10, u.x), math.lerp(n01, n11, u.x), u.y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetGradientNoise(float3 p)
        {
            var i = math.floor(p);
            var f = p - i;
            var u = f * f * f * (f * (f * 6.0f - 15.0f) + 10.0f);
            var i3 = (int3)i;

            var n000 = math.dot(HashGradient(i3 + new int3(0, 0, 0)), f - new float3(0, 0, 0));
            var n100 = math.dot(HashGradient(i3 + new int3(1, 0, 0)), f - new float3(1, 0, 0));
            var n010 = math.dot(HashGradient(i3 + new int3(0, 1, 0)), f - new float3(0, 1, 0));
            var n110 = math.dot(HashGradient(i3 + new int3(1, 1, 0)), f - new float3(1, 1, 0));
            var n001 = math.dot(HashGradient(i3 + new int3(0, 0, 1)), f - new float3(0, 0, 1));
            var n101 = math.dot(HashGradient(i3 + new int3(1, 0, 1)), f - new float3(1, 0, 1));
            var n011 = math.dot(HashGradient(i3 + new int3(0, 1, 1)), f - new float3(0, 1, 1));
            var n111 = math.dot(HashGradient(i3 + new int3(1, 1, 1)), f - new float3(1, 1, 1));

            return math.lerp(
                math.lerp(math.lerp(n000, n100, u.x), math.lerp(n010, n110, u.x), u.y),
                math.lerp(math.lerp(n001, n101, u.x), math.lerp(n011, n111, u.x), u.y),
                u.z);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetFractalNoise(float3 pos, float frequency, int octaves, float lacunarity, float persistence)
        {
            var sum = 0.0f;
            var amp = 1.0f;
            var p = pos * frequency;
            var shift = new float3(100, 100, 100); 

            for (var i = 0; i < octaves; i++)
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
            var sum = 0.0f;
            var amp = 1.0f;
            var p = pos * frequency;
            
            for (int i = 0; i < octaves; i++)
            {
                sum += GetGradientNoise2D(p) * amp;
                var old = p;
                p.x = old.x * 0.6f - old.y * 0.8f;
                p.y = old.x * 0.8f + old.y * 0.6f;
                p *= lacunarity;
                amp *= persistence;
            }
            return sum;
        }
    }
}