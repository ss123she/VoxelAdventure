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
        [SerializeField] private int maxChunkCreationsPerFrame = 16;
        [SerializeField] private int maxMeshAppliesPerFrame = 16;

        private const int ChunkSize = NaiveSurfaceNets.Chunk.ChunkSizeMinusTwo;

        private readonly Dictionary<Vector3Int, Chunk> _activeChunks = new();
        private readonly HashSet<Vector3Int> _chunksToLoad = new();
        private readonly List<Vector3Int> _chunksToUnload = new();
        
        private readonly List<Chunk> _chunksGeneratingData = new();
        private readonly List<Chunk> _chunksGeneratingMesh = new();
        private readonly Queue<Chunk> _chunksReadyToApplyMesh = new();
        private readonly Queue<Chunk> _chunkPool = new();
        
        private Vector3Int _lastPlayerChunkCoord;

        private void Start()
        {
            var poolVolume = (viewDistanceHorizontal * 2 + 2) * (viewDistanceHorizontal * 2 + 2) * (viewDistanceVertical * 2 + 2);
            for (var i = 0; i < poolVolume; i++)
            {
                var chunk = Instantiate(chunkPrefab, transform).GetComponent<Chunk>();
                chunk.gameObject.SetActive(false);
                _chunkPool.Enqueue(chunk);
            }
            
            _lastPlayerChunkCoord = GetChunkCoordinate(player.position) + Vector3Int.one;
        }
        
        private void Update()
        {
            Profiler.BeginSample("WorldManager.RefreshLists");
            var playerChunkCoord = GetChunkCoordinate(player.position);
            if (playerChunkCoord != _lastPlayerChunkCoord)
            {
                _lastPlayerChunkCoord = playerChunkCoord;
                RefreshChunkLists();
            }
            Profiler.EndSample();
            
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
            _chunksToLoad.RemoveWhere(c => _activeChunks.ContainsKey(c));

            for (var x = -viewDistanceHorizontal; x <= viewDistanceHorizontal; x++)
            for (var y = -viewDistanceVertical; y <= viewDistanceVertical; y++)
            for (var z = -viewDistanceHorizontal; z <= viewDistanceHorizontal; z++)
            {
                var coord = _lastPlayerChunkCoord + new Vector3Int(x, y, z);
                if (!_activeChunks.ContainsKey(coord)) _chunksToLoad.Add(coord);
            }

            var center = new Vector2(_lastPlayerChunkCoord.x, _lastPlayerChunkCoord.z);
            foreach (var coord in _activeChunks.Keys.Where(coord => Vector2.Distance(new Vector2(coord.x, coord.z), center) > viewDistanceHorizontal || 
                                                                    Mathf.Abs(coord.y - _lastPlayerChunkCoord.y) > viewDistanceVertical))
                _chunksToUnload.Add(coord);
        }
        
        private void ProcessUnloading()
        {
            foreach (var coord in _chunksToUnload)
            {
                if (!_activeChunks.TryGetValue(coord, out var chunk)) continue;
                
                _chunksGeneratingData.Remove(chunk);
                _chunksGeneratingMesh.Remove(chunk);
                
                chunk.CancelAndClear();
                chunk.gameObject.SetActive(false);
                _chunkPool.Enqueue(chunk);
                _activeChunks.Remove(coord);
            }
            _chunksToUnload.Clear();
        }

        private void ProcessLoading()
        {
            var loadedCount = 0;
            using var enumerator = _chunksToLoad.GetEnumerator();
            var loadedCoords = new List<Vector3Int>();

            while (loadedCount < maxChunkCreationsPerFrame && enumerator.MoveNext())
            {
                var coord = enumerator.Current;
                loadedCoords.Add(coord);
                
                var position = (Vector3)coord * ChunkSize;
                var chunk = _chunkPool.Count > 0 ? _chunkPool.Dequeue() : Instantiate(chunkPrefab, transform).GetComponent<Chunk>();
                
                chunk.transform.position = position;
                chunk.name = $"Chunk {coord}";
                chunk.gameObject.SetActive(true);
                chunk.chunkCoordinate = coord;
                chunk.StartGeneration(terrainSettings);

                _activeChunks.Add(coord, chunk);
                _chunksGeneratingData.Add(chunk);
                loadedCount++;
            }

            foreach (var c in loadedCoords) _chunksToLoad.Remove(c);
        }

        private void ProcessGenerationLifecycle()
        {
            _chunksGeneratingData.RemoveAll(chunk => 
            {
                if (!chunk.IsDataGenerationCompleted()) return false;
                chunk.StartMeshGeneration();
                _chunksGeneratingMesh.Add(chunk);
                return true;
            });

            _chunksGeneratingMesh.RemoveAll(chunk => 
            {
                if (!chunk.IsMeshGenerationCompleted()) return false;
                _chunksReadyToApplyMesh.Enqueue(chunk);
                return true;
            });

            for (var i = 0; i < maxMeshAppliesPerFrame && _chunksReadyToApplyMesh.TryDequeue(out var chunk); i++)
            {
                Profiler.BeginSample("ApplyMesh");
                chunk.ApplyMesh();
                Profiler.EndSample();
            }
        }

        private static Vector3Int GetChunkCoordinate(Vector3 position) => Vector3Int.FloorToInt(position / ChunkSize);
    }
}