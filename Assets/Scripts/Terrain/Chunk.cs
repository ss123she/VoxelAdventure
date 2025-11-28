using NaiveSurfaceNets;
using Terrain.Noise;
using Terrain.Noise.Strategies;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Terrain
{
    [RequireComponent(typeof(MeshFilter))]
    public class Chunk : MonoBehaviour
    {
        public const int ChunkSize = 32;
        public const int ChunkSizeMinusOne = ChunkSize - 1;
        public const int ChunkSizeMinusTwo = ChunkSize - 2;

        private enum ChunkState
        {
            Idle,
            GeneratingData,
            GeneratingMesh,
            Ready
        }

        public Vector3Int ChunkCoordinate { get; set; }

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
            
            const int size = NaiveSurfaceNets.Chunk.ChunkSize; 
            
            const int jobLength = size * size; 
        
            var offset = new float3(ChunkCoordinate.x * ChunkSizeMinusTwo, ChunkCoordinate.y * ChunkSizeMinusTwo, ChunkCoordinate.z * ChunkSizeMinusTwo);
        
            var jobData = new NoiseJobData
            {
                Seed = new float3(1,1,1),
                CaveDensity = settings.caveDensity,
                NoiseScale = settings.noiseScale,
                TerrainHeight = settings.terrainHeight,
                GroundLevel = settings.groundLevel,
                Lacunarity = settings.lacunarity,
                Octaves = settings.octaves,
                Persistence = settings.persistence
            };
        
            switch (settings.generationMode)
            {
                case GenerationMode.Landscape:
                    var jobL = new NoiseJob<LandscapeStrategy>
                    {
                        DataNative = _data.data,
                        Offset = offset,
                        Settings = jobData,
                        ChunkSize = size,
                        Strategy = new LandscapeStrategy()
                    };
                    _dataGenerationHandle = jobL.Schedule(jobLength, 16);
                    break;
        
                case GenerationMode.Caves:
                    var jobC = new NoiseJob<CavesStrategy>
                    {
                        DataNative = _data.data,
                        Offset = offset,
                        Settings = jobData,
                        ChunkSize = size,
                        Strategy = new CavesStrategy()
                    };
                    _dataGenerationHandle = jobC.Schedule(jobLength, 16);
                    break;
        
                case GenerationMode.Gyroid:
                    var jobG = new NoiseJob<GyroidStrategy>
                    {
                        DataNative = _data.data,
                        Offset = offset,
                        Settings = jobData,
                        ChunkSize = size,
                        Strategy = new GyroidStrategy()
                    };
                    _dataGenerationHandle = jobG.Schedule(jobLength, 16);
                    break;
            }
            
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

            if (IsUniform(_data.data))
            {
                _currentState = ChunkState.Ready;
                return;
            }
            
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
                var mesh = _meshFilter.sharedMesh;
                if (!mesh) 
                {
                    mesh = new Mesh
                    {
                        name = $"ChunkMesh_{ChunkCoordinate}"
                    };
                    _meshFilter.sharedMesh = mesh;
                }
        
                mesh.SetMesh(_mesher);
        
                mesh.RecalculateBounds(); 
                mesh.RecalculateNormals(); 
            }
    
            _currentState = ChunkState.Ready;
        }
        
        public void CancelAndClear()
        {
            if (_currentState is ChunkState.GeneratingData or ChunkState.GeneratingMesh)
            {
                _dataGenerationHandle.Complete();
                _meshGenerationHandle.Complete();
            }
            
            if (_meshFilter.mesh)
                _meshFilter.mesh.Clear();
            
            _currentState = ChunkState.Idle;
        }

        private void OnDestroy()
        {
            CancelAndClear();
            _data?.Dispose();
            _mesher?.Dispose();
        }
        
        private static bool IsUniform(NativeArray<sbyte> data)
        {
            if (data.Length == 0) return true;
    
            var firstSign = data[0] > 0;
    
            for (var i = 1; i < data.Length; i++)
            {
                var currentSign = data[i] > 0;
                if (currentSign != firstSign) return false;
            }
    
            return true;
        }
    }
}