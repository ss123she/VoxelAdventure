<<<<<<< HEAD
using Unity.Collections;
using Unity.Mathematics;

namespace Terrain.Noise.Strategies
{
    public interface ITerrainStrategy
    {
        void Execute(int baseIdx, float3 startPos, int chunkSize, ref NoiseJobData s, NativeArray<sbyte> data);
=======
namespace Terrain.Strategies
{
    public interface ITerrainStrategy
    {
        
>>>>>>> fd4fb025f38ce06c097181230c65bf81b8998614
    }
}