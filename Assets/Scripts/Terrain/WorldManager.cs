using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;

namespace Terrain
{
    public class WorldManager : MonoBehaviour
    {
        [Header("World Settings")]
        [SerializeField] private Transform player;
        [SerializeField] private GameObject chunkPrefab;
        [SerializeField] private TerrainSettings terrainSettings;

        [Header("Generation Settings")]
        [SerializeField] private int viewDistanceHorizontal = 8;
        [SerializeField] private int viewDistanceVertical = 3;

        [Header("Performance Settings")]
        [SerializeField] private int maxChunkCreationsPerFrame = 8;
        [SerializeField] private int maxMeshAppliesPerFrame = 8;

        private const int ChunkSize = NaiveSurfaceNets.Chunk.ChunkSizeMinusTwo;

        private readonly Dictionary<Vector3Int, Chunk> _activeChunks = new();

        private readonly List<Vector3Int> _chunksToLoadList = new(); 
        
        private readonly List<Vector3Int> _chunksToUnload = new();
        
        private readonly List<Chunk> _chunksGeneratingData = new();
        private readonly List<Chunk> _chunksGeneratingMesh = new();
        private readonly Queue<Chunk> _chunksReadyToApplyMesh = new();
        private readonly Queue<Chunk> _chunkPool = new();
        
        private Vector3Int _lastPlayerChunkCoord;
        private bool _forceUpdate = true; 

        private void Start()
        {
            var poolVolume = (viewDistanceHorizontal * 2 + 1) * (viewDistanceHorizontal * 2 + 1) * (viewDistanceVertical * 2 + 1);
            for (var i = 0; i < poolVolume; i++)
            {
                var chunk = CreateChunkInstance();
                chunk.gameObject.SetActive(false);
                _chunkPool.Enqueue(chunk);
            }
        }
        
        private Chunk CreateChunkInstance()
        {
            var go = Instantiate(chunkPrefab, transform);
            var chunk = go.GetComponent<Chunk>();
            return chunk;
        }

        private void Update()
        {
            var playerChunkCoord = GetChunkCoordinate(player.position);
            
            if (_forceUpdate || playerChunkCoord != _lastPlayerChunkCoord)
            {
                _lastPlayerChunkCoord = playerChunkCoord;
                _forceUpdate = false;
                
                Profiler.BeginSample("WorldManager.RefreshLists");
                RefreshChunkLists();
                Profiler.EndSample();
            }
            
            Profiler.BeginSample("WorldManager.Unloading");
            ProcessUnloading();
            Profiler.EndSample();

            Profiler.BeginSample("WorldManager.Loading");
            ProcessLoading();
            Profiler.EndSample();

            Profiler.BeginSample("WorldManager.Lifecycle");
            ProcessGenerationLifecycle();
            Profiler.EndSample();
        }

        private void RefreshChunkLists()
        {
            _chunksToLoadList.Clear();
            _chunksToUnload.Clear();
            
            for (var x = -viewDistanceHorizontal; x <= viewDistanceHorizontal; x++)
            for (var y = -viewDistanceVertical; y <= viewDistanceVertical; y++)
            for (var z = -viewDistanceHorizontal; z <= viewDistanceHorizontal; z++)
            {
                var coord = _lastPlayerChunkCoord + new Vector3Int(x, y, z);

                if (_activeChunks.ContainsKey(coord)) continue;
                _chunksToLoadList.Add(coord);
            }

            _chunksToLoadList.Sort((a, b) => 
            {
                var distA = (a - _lastPlayerChunkCoord).sqrMagnitude;
                var distB = (b - _lastPlayerChunkCoord).sqrMagnitude;
                return distA.CompareTo(distB);
            });

            foreach (var coord in from coord in _activeChunks.Keys let outOfBoundsX = Mathf.Abs(coord.x - _lastPlayerChunkCoord.x) > viewDistanceHorizontal let outOfBoundsZ = Mathf.Abs(coord.z - _lastPlayerChunkCoord.z) > viewDistanceHorizontal let outOfBoundsY = Mathf.Abs(coord.y - _lastPlayerChunkCoord.y) > viewDistanceVertical where outOfBoundsX || outOfBoundsZ || outOfBoundsY select coord)
                _chunksToUnload.Add(coord);
        }
        
        private void ProcessUnloading()
        {
            if (_chunksToUnload.Count == 0) return;

            foreach (var coord in _chunksToUnload)
            {
                if (!_activeChunks.TryGetValue(coord, out var chunk)) continue;
                
                _chunksGeneratingData.Remove(chunk);
                _chunksGeneratingMesh.Remove(chunk);
                
                chunk.CancelAndClear();
                
                chunk.gameObject.SetActive(false);
                chunk.name = "Chunk (Pooled)";
                
                _chunkPool.Enqueue(chunk);
                _activeChunks.Remove(coord);
            }
            _chunksToUnload.Clear();
        }

        private void ProcessLoading()
        {
            if (_chunksToLoadList.Count == 0) return;

            var loadedCount = 0;

            foreach (var i in _chunksToLoadList)
            {
                if (loadedCount >= maxChunkCreationsPerFrame) break;

                if (_activeChunks.ContainsKey(i)) continue;

                var position = (Vector3)i * ChunkSize;

                var chunk = _chunkPool.Count > 0 ? _chunkPool.Dequeue() : CreateChunkInstance();
                
                
                chunk.transform.position = position;
                chunk.name = $"Chunk {i}";
                chunk.gameObject.SetActive(true);
                chunk.ChunkCoordinate = i;
                chunk.StartGeneration(terrainSettings);

                _activeChunks.Add(i, chunk);
                _chunksGeneratingData.Add(chunk);
                
                loadedCount++;
            }

            if (loadedCount > 0)
                _chunksToLoadList.RemoveRange(0, Mathf.Min(loadedCount, _chunksToLoadList.Count));
        }

        private void ProcessGenerationLifecycle()
        {
            _chunksGeneratingData.RemoveAll(chunk => 
            {
                if (!chunk.gameObject.activeSelf) return true; 

                if (!chunk.IsDataGenerationCompleted()) return false;
                
                chunk.StartMeshGeneration();
                _chunksGeneratingMesh.Add(chunk);
                return true;
            });

            _chunksGeneratingMesh.RemoveAll(chunk => 
            {
                if (!chunk.gameObject.activeSelf) return true;

                if (!chunk.IsMeshGenerationCompleted()) return false;
                _chunksReadyToApplyMesh.Enqueue(chunk);
                return true;
            });

            var appliedCount = 0;
            while (_chunksReadyToApplyMesh.Count > 0 && appliedCount < maxMeshAppliesPerFrame)
            {
                var chunk = _chunksReadyToApplyMesh.Dequeue();
                
                if (!chunk.gameObject.activeSelf) continue;

                Profiler.BeginSample("ApplyMesh");
                chunk.ApplyMesh();
                Profiler.EndSample();
                appliedCount++;
            }
        }

        private static Vector3Int GetChunkCoordinate(Vector3 position) => Vector3Int.FloorToInt(position / ChunkSize);
    }
}