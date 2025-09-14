using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Terrain
{
    [BurstCompile]
    public struct NoiseJob : IJobParallelFor
    {
        [WriteOnly] public NativeArray<float> DataNative;

        public int ChunkSize;
        public float NoiseScale;
        public float3 Offset;

        public float TerrainHeight;
        public float GroundLevel;

        public void Execute(int index)
        {
            var gridSize = ChunkSize + 1;
            var x = index % gridSize;
            var y = index / gridSize % gridSize;
            var z = index / (gridSize * gridSize);

            var worldPosition = new float3(x, y, z) + Offset;

            var noiseValue = noise.snoise(worldPosition * NoiseScale);

            var normalizedY = (float)y / ChunkSize;
            var density = noiseValue - (normalizedY * TerrainHeight - GroundLevel);

            DataNative[index] = density;
        }
    }
}