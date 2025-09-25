using NaiveSurfaceNets;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Terrain
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public class Chunk : MonoBehaviour
    {
        public Vector3Int chunkCoordinate;
        private int _gridSize;
        
        private NaiveSurfaceNets.Chunk _data;
        private NaiveSurfaceNets.Mesher _mesher;

        private JobHandle _generationHandle;
        private bool _isGeneratingData;
        
        private MeshFilter _meshFilter;
        private MeshCollider _meshCollider;
        
        private void Awake()
        {
            _meshFilter = GetComponent<MeshFilter>();
            _meshCollider = GetComponent<MeshCollider>();
        }

        public void StartGeneration(TerrainSettings settings)
        {
            _gridSize = NaiveSurfaceNets.Chunk.ChunkSizeMinusTwo;
        
            _data ??= new NaiveSurfaceNets.Chunk(); 
            _mesher ??= new NaiveSurfaceNets.Mesher();

            var offset = new float3(
                chunkCoordinate.x * _gridSize,
                chunkCoordinate.y * _gridSize,
                chunkCoordinate.z * _gridSize);

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

            _generationHandle = noiseJob.Schedule(_data.data.Length, 64);
            _isGeneratingData = true;

        }
        
        private void LateUpdate()
        {
            if (!_isGeneratingData)
                return;
        
            if (!_generationHandle.IsCompleted)
                return;
        
            _generationHandle.Complete();
            _isGeneratingData = false;
        
            _mesher.StartMeshJob(_data, Mesher.NormalCalculationMode.FromSDF);
            _mesher.WaitForMeshJob();

            if (_mesher.Vertices.Length == 0)
            {
                if (_meshFilter.mesh) _meshFilter.mesh.Clear();
                return;
            }

            if (!_meshFilter.mesh)
                _meshFilter.mesh = new Mesh();
        
            _meshFilter.mesh.SetMesh(_mesher);
            _meshCollider.sharedMesh = _meshFilter.mesh;
        }
        
        public void CompleteJobs()
        {
            if (!_isGeneratingData) return;
            
            _generationHandle.Complete();
            _isGeneratingData = false;
        }

        void OnDestroy()
        {
            if (_isGeneratingData)
                _generationHandle.Complete();
            
            _data?.Dispose();
            _mesher?.Dispose();
        }
        
        private void OnDrawGizmosSelected()
        {
            if (_data == null) return;
            
            const int gridSize = NaiveSurfaceNets.Chunk.ChunkSize;
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.position + new Vector3(gridSize / 2f - 0.5f, gridSize / 2f - 0.5f, gridSize / 2f - 0.5f),
                new Vector3(gridSize, gridSize, gridSize));
        }
    }
}