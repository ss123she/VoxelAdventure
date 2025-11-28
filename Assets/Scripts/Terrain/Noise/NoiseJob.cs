<<<<<<< HEAD
using Terrain.Noise.Strategies;
=======
using System.Runtime.CompilerServices;
>>>>>>> fd4fb025f38ce06c097181230c65bf81b8998614
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
<<<<<<< HEAD

namespace Terrain.Noise
{
=======
using UnityEngine;

namespace Terrain
{
    // Интерфейс изменен для обработки целого ряда вокселей (оптимизация)
    public interface ITerrainStrategy
    {
        void Execute(int baseIdx, float3 startPos, int chunkSize, ref NoiseJobData s, NativeArray<sbyte> data);
    }

>>>>>>> fd4fb025f38ce06c097181230c65bf81b8998614
    public struct NoiseJobData
    {
        public float3 Seed;
        public float NoiseScale;
        public float TerrainHeight;
        public float GroundLevel;
        public int Octaves;
        public float Lacunarity;
        public float Persistence;
        public float CaveDensity;
    }
<<<<<<< HEAD
=======

    public static class FastNoise
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
        
        // --- 2D ШУМ (Добавлен для скорости) ---
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float2 HashGradient2D(int2 p)
        {
            uint h = (uint)p.x * kX + (uint)p.y * kZ; // используем kZ для Y в 2D
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

        // 2D Fractal для быстрой генерации поверхности
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetFractalNoise2D(float2 pos, float frequency, int octaves, float lacunarity, float persistence)
        {
            float sum = 0.0f;
            float amp = 1.0f;
            float2 p = pos * frequency;
            
            for (int i = 0; i < octaves; i++)
            {
                sum += GetGradientNoise2D(p) * amp;
                // Простой поворот 2D
                float2 old = p;
                p.x = old.x * 0.6f - old.y * 0.8f;
                p.y = old.x * 0.8f + old.y * 0.6f;
                p *= lacunarity;
                amp *= persistence;
            }
            return sum;
        }
    }

    // --- СТРАТЕГИИ ---

    public struct LandscapeStrategy : ITerrainStrategy
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Execute(int baseIdx, float3 startPos, int chunkSize, ref NoiseJobData s, NativeArray<sbyte> data)
        {
            // 1. ОПТИМИЗАЦИЯ ПО ВЫСОТЕ (Y)
            // startPos.y постоянен для всей строки.
            // Если мы слишком высоко или слишком глубоко - не считаем шум вообще.
            
            // Максимально возможная высота рельефа + запас для арок
            float limitTop = s.GroundLevel + s.TerrainHeight + 15.0f; 
            float limitBot = s.GroundLevel - s.TerrainHeight - 15.0f;

            // Если текущий слой (Y) выше гор -> ВОЗДУХ
            if (startPos.y > limitTop)
            {
                for (int z = 0; z < chunkSize; z++) data[baseIdx + z] = -127; 
                return;
            }
            // Если текущий слой (Y) ниже дна -> КАМЕНЬ
            if (startPos.y < limitBot)
            {
                for (int z = 0; z < chunkSize; z++) data[baseIdx + z] = 127;
                return;
            }

            // 2. ГЕНЕРАЦИЯ СТРОКИ
            for (int z = 0; z < chunkSize; z++)
            {
                float3 currentPos = new float3(startPos.x, startPos.y, startPos.z + z);
                
                // --- ШАГ 1: Базовая форма земли (2D Шум) ---
                // Используем 2D шум для основы, это в 2 раза быстрее 3D.
                // Добавляем Seed
                float2 pos2D = new float2(currentPos.x + s.Seed.x, currentPos.z + s.Seed.z);
                
                // Легкий warping для 2D, чтобы убрать сетку
                float warp2D = FastNoise.GetGradientNoise2D(pos2D * s.NoiseScale * 0.5f);
                float2 warpedPos2D = pos2D + warp2D * 5.0f;

                // Получаем высоту. Диапазон примерно -0.8 .. 0.8
                float noiseHeight = FastNoise.GetFractalNoise2D(warpedPos2D, s.NoiseScale, 3, s.Lacunarity, s.Persistence); 
                
                // Базовый SDF: Pos.Y - (Ground + Noise * Height)
                float surfaceHeight = s.GroundLevel + noiseHeight * s.TerrainHeight;
                float baseSdf = currentPos.y - surfaceHeight;

                // --- ШАГ 2: 3D Детали (Арки и Нависания) ---
                // Считаем тяжелый 3D шум ТОЛЬКО если мы рядом с поверхностью
                // Если baseSdf близко к 0, значит мы на границе земли и воздуха.
                if (baseSdf > -15.0f && baseSdf < 15.0f)
                {
                    float3 pos3D = currentPos + s.Seed;
                    // Warping для 3D
                    pos3D.x += warp2D * 4.0f; 
                    
                    // 3D Шум для арок (можно меньше октав)
                    float noise3D = FastNoise.GetFractalNoise(pos3D, s.NoiseScale * 1.5f, 2, s.Lacunarity, s.Persistence);
                    
                    // Создаем арки: SDF < 0 = твердое
                    // 0.4f - порог плотности
                    float archSdf = 0.2f - noise3D; 
                    
                    // Объединяем землю и арки (min = union)
                    baseSdf = math.min(baseSdf, archSdf);
                }

                // Кламп и запись
                if (baseSdf < -1.0f) baseSdf = -1.0f;
                else if (baseSdf > 1.0f) baseSdf = 1.0f;
                
                data[baseIdx + z] = (sbyte)(baseSdf * -127.0f);
            }
        }
    }

    public struct CavesStrategy : ITerrainStrategy
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Execute(int baseIdx, float3 startPos, int chunkSize, ref NoiseJobData s, NativeArray<sbyte> data)
        {
            // Для пещер нет "потолка", но есть дно мира.
            if (startPos.y < -500) { /* оптимизация для дна */ }

            for (int z = 0; z < chunkSize; z++)
            {
                float3 pos = new float3(startPos.x, startPos.y, startPos.z + z);
                float3 seedPos = pos + s.Seed;

                // 2D поверхность (быстро) для определения "земли"
                float2 pos2D = new float2(seedPos.x, seedPos.z);
                float hNoise = FastNoise.GetFractalNoise2D(pos2D, s.NoiseScale, 3, s.Lacunarity, s.Persistence);
                float surfaceSdf = pos.y - s.GroundLevel - hNoise * s.TerrainHeight;

                // Если мы высоко над землей, пещеры считать нет смысла
                if (surfaceSdf > 10.0f)
                {
                    data[baseIdx + z] = -127; // Воздух
                    continue;
                }

                // 3D Пещеры
                // Warping
                float warp = FastNoise.GetGradientNoise(seedPos * 0.02f) * 4.0f;
                float3 cavePos = seedPos + warp;
                
                float caveNoise = FastNoise.GetFractalNoise(cavePos, s.NoiseScale * 2.0f, 2, 2.0f, 0.5f);
                float caveSdf = caveNoise - s.CaveDensity;

                float finalSdf = math.max(surfaceSdf, -caveSdf);
                
                if (finalSdf < -1.0f) finalSdf = -1.0f;
                else if (finalSdf > 1.0f) finalSdf = 1.0f;

                data[baseIdx + z] = (sbyte)(finalSdf * -127.0f);
            }
        }
    }

    public struct GyroidStrategy : ITerrainStrategy
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Execute(int baseIdx, float3 startPos, int chunkSize, ref NoiseJobData s, NativeArray<sbyte> data)
        {
            // Гироид бесконечен, оптимизации по Y сложнее, считаем честно
            float scale = s.NoiseScale * 0.5f;
            
            for (int z = 0; z < chunkSize; z++)
            {
                float3 pos = new float3(startPos.x, startPos.y, startPos.z + z);
                var p = (pos + s.Seed) * scale;
                
                // Warp
                float3 warpVec;
                warpVec.x = FastNoise.GetGradientNoise(p * 0.4f);
                warpVec.y = FastNoise.GetGradientNoise((p + 100) * 0.4f);
                warpVec.z = FastNoise.GetGradientNoise((p + 200) * 0.4f);
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
>>>>>>> fd4fb025f38ce06c097181230c65bf81b8998614
    
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

<<<<<<< HEAD
=======
            // Мы передаем начало строки. Z будет перебираться внутри стратегии.
            // Y здесь константа, что позволяет стратегии делать быстрые проверки высоты.
>>>>>>> fd4fb025f38ce06c097181230c65bf81b8998614
            var startPos = new float3(posX, posY, posZBase);
            var settingsCopy = Settings;

            Strategy.Execute(baseArrayIdx, startPos, ChunkSize, ref settingsCopy, DataNative);
        }
    }
}