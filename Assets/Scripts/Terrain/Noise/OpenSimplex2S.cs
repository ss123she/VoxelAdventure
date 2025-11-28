using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace Terrain.Noise
{
    public static class OpenSimplex2S
    {
        private const long X_PRIME = 1619;
        private const long Y_PRIME = 31337;
        private const long Z_PRIME = 6971;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Noise3_ImproveXZ(long seed, float3 p)
        {
            return OpenSimplex3D(p, (int)seed);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float OpenSimplex3D(float3 v, int seed)
        {
            const float F3 = 1.0f / 3.0f;
            const float G3 = 1.0f / 6.0f;

            float s = math.csum(v) * F3;
            float3 i = math.floor(v + s);
            float3 x0 = v - (i - math.csum(i) * G3);

            float3 g = math.step(x0.yzx, x0.xyz);
            float3 l = 1.0f - g;
            float3 i1 = math.min(g, l.zxy);
            float3 i2 = math.max(g, l.zxy);

            float3 x1 = x0 - i1 + G3;
            float3 x2 = x0 - i2 + G3 * 2.0f;
            float3 x3 = x0 - 1.0f + G3 * 3.0f;

            float4 n;
            n.x = GetContribution(i, x0, seed);
            n.y = GetContribution(i + i1, x1, seed);
            n.z = GetContribution(i + i2, x2, seed);
            n.w = GetContribution(i + 1.0f, x3, seed);

            return math.csum(n) * 32.0f; 
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float GetContribution(float3 i, float3 x, int seed)
        {
            float t = 0.6f - math.dot(x, x);
            
            t = math.max(0.0f, t); 
    
            t *= t;
            t *= t;

            int3 ii = (int3)i;
            int hash = (int)(ii.x * X_PRIME ^ ii.y * Y_PRIME ^ ii.z * Z_PRIME ^ seed);
            
            hash ^= hash >> 15; hash *= 0x735a2d97; hash ^= hash >> 15;

            int h = hash & 0x0F;
            
            float u = h < 8 ? x.x : x.y;
            float v = h < 4 ? x.y : (h == 12 || h == 14 ? x.x : x.z);
            float grad = ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);

            return t * grad;
        }
    }
}