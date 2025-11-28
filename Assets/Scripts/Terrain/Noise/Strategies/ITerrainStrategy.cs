using Unity.Collections;
using Unity.Mathematics;

namespace Terrain.Noise.Strategies
{
    public interface ITerrainStrategy
    {
        void Execute(int baseIdx, float3 startPos, int chunkSize, ref NoiseJobData s, NativeArray<sbyte> data);
    }
}