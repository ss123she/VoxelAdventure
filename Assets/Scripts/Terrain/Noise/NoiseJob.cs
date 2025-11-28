using Terrain.Noise.Strategies;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Terrain.Noise
{
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

            var startPos = new float3(posX, posY, posZBase);
            var settingsCopy = Settings;

            Strategy.Execute(baseArrayIdx, startPos, ChunkSize, ref settingsCopy, DataNative);
        }
    }
}