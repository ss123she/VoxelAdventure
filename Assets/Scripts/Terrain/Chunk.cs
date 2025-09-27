using NaiveSurfaceNets;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Terrain
{
    [RequireComponent(typeof(MeshFilter))]
    public class Chunk : MonoBehaviour
    {
        private enum ChunkState
        {
            Idle,
            GeneratingData,
            GeneratingMesh,
            Ready
        }
        
        public Vector3Int chunkCoordinate;

        private NaiveSurfaceNets.Chunk _data;
        private NaiveSurfaceNets.Mesher _mesher;
        private MeshFilter _meshFilter;
        
        private JobHandle _dataGenerationHandle;
        private JobHandle _meshGenerationHandle;
        private ChunkState _currentState = ChunkState.Idle;

        private void Awake()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _data ??= new NaiveSurfaceNets.Chunk();
            _mesher ??= new NaiveSurfaceNets.Mesher();
        }

        public void StartGeneration(TerrainSettings settings)
        {
            if (_currentState != ChunkState.Idle) return;
            
            var gridSize = NaiveSurfaceNets.Chunk.ChunkSizeMinusTwo;
            var offset = new float3(chunkCoordinate.x * gridSize, chunkCoordinate.y * gridSize, chunkCoordinate.z * gridSize);

            var noiseJob = new NoiseJob
            {
                DataNative = _data.data,
                CaveDensity = settings.caveDensity,
                Offset = offset,
                NoiseScale = settings.noiseScale,
                TerrainHeight = settings.terrainHeight,
                GroundLevel = settings.groundLevel,
                Lacunarity = settings.lacunarity,
                Octaves = settings.octaves,
                Persistence = settings.persistence,
                GenerationMode = settings.generationMode
            };

            _dataGenerationHandle = noiseJob.Schedule(_data.data.Length, 64);
            _currentState = ChunkState.GeneratingData;
        }
        
        public bool IsDataGenerationCompleted()
        {
            return _currentState == ChunkState.GeneratingData && _dataGenerationHandle.IsCompleted;
        }

        public void StartMeshGeneration()
        {
            if (_currentState != ChunkState.GeneratingData) return;

            _dataGenerationHandle.Complete();
            _meshGenerationHandle = _mesher.StartMeshJob(_data, Mesher.NormalCalculationMode.FromSDF);
            _currentState = ChunkState.GeneratingMesh;
        }
        
        public bool IsMeshGenerationCompleted()
        {
            return _currentState == ChunkState.GeneratingMesh && _meshGenerationHandle.IsCompleted;
        }

        public void ApplyMesh()
        {
            if (_currentState != ChunkState.GeneratingMesh) return;
            
            _meshGenerationHandle.Complete();
            
            if (_mesher.Vertices.Length == 0)
            {
                if (_meshFilter.mesh) _meshFilter.mesh.Clear();
            }
            else
            {
                if (!_meshFilter.mesh)
                {
                    _meshFilter.mesh = new Mesh();
                }
                _meshFilter.mesh.SetMesh(_mesher);
            }
            
            _currentState = ChunkState.Ready;
        }
        
        public void CancelAndClear()
        {
            if (_currentState == ChunkState.GeneratingData || _currentState == ChunkState.GeneratingMesh)
            {
                _dataGenerationHandle.Complete();
                _meshGenerationHandle.Complete();
            }
            
            if (_meshFilter.mesh)
            {
                _meshFilter.mesh.Clear();
            }
            
            _currentState = ChunkState.Idle;
        }

        private void OnDestroy()
        {
            CancelAndClear();
            _data?.Dispose();
            _mesher?.Dispose();
        }
    }
}