using System.Collections.Generic;
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
        [SerializeField] private int maxChunkUnloadsPerFrame = 8; 
        
        private const int ChunkSize = NaiveSurfaceNets.Chunk.ChunkSizeMinusTwo;

        private readonly Dictionary<Vector3Int, Chunk> _activeChunks = new();
        
        private readonly List<Vector3Int> _chunksToLoadList = new(); 
        private readonly List<Vector3Int> _chunksToUnload = new();
        
        private readonly List<Chunk> _chunksGeneratingData = new();
        private readonly List<Chunk> _chunksGeneratingMesh = new();
        
        private readonly Queue<Chunk> _chunksReadyToApplyMesh = new();
        private readonly Queue<Chunk> _chunkPool = new();

        private Vector3Int[] _sortedChunkOffsets;
        
        private Vector3Int _lastPlayerChunkCoord;
        private bool _forceUpdate = true;
        
        private int _sqrViewDistHorizontal;

        private void Start()
        {
            if (!chunkPrefab)
            {
                Debug.LogError("WorldManager: Chunk Prefab is not assigned!");
                enabled = false;
                return;
            }

            GenerateSortedOffsets();

            int maxChunks = _sortedChunkOffsets.Length + 100;
            _chunksToLoadList.Capacity = maxChunks;
            _chunksToUnload.Capacity = maxChunks;
            _chunksGeneratingData.Capacity = maxChunks;
            _chunksGeneratingMesh.Capacity = maxChunks;

            int poolSize = maxChunks; 
            for (var i = 0; i < poolSize; i++)
            {
                var chunk = CreateChunkInstance();
                if (chunk != null) 
                {
                    chunk.gameObject.SetActive(false);
                    _chunkPool.Enqueue(chunk);
                }
            }
        }

        private void GenerateSortedOffsets()
        {
            var offsets = new List<Vector3Int>();
            
            for (var x = -viewDistanceHorizontal; x <= viewDistanceHorizontal; x++)
            for (var y = -viewDistanceVertical; y <= viewDistanceVertical; y++)
            for (var z = -viewDistanceHorizontal; z <= viewDistanceHorizontal; z++)
                offsets.Add(new Vector3Int(x, y, z));
            
            offsets.Sort((a, b) => a.sqrMagnitude.CompareTo(b.sqrMagnitude));
            
            _sortedChunkOffsets = offsets.ToArray();
        }
        
        private Chunk CreateChunkInstance()
        {
            var go = Instantiate(chunkPrefab, transform);
            if (!go.TryGetComponent<Chunk>(out var chunk))
            {
                Destroy(go);
                return null;
            }
            return chunk;
        }

        private void Update()
        {
            if (player == null) return;

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

            for (int i = 0; i < _sortedChunkOffsets.Length; i++)
            {
                var coord = _lastPlayerChunkCoord + _sortedChunkOffsets[i];
                if (!_activeChunks.ContainsKey(coord))
                {
                    _chunksToLoadList.Add(coord);
                }
            }
            
            _chunksToLoadList.Reverse();

            int minX = _lastPlayerChunkCoord.x - viewDistanceHorizontal;
            int maxX = _lastPlayerChunkCoord.x + viewDistanceHorizontal;
            int minZ = _lastPlayerChunkCoord.z - viewDistanceHorizontal;
            int maxZ = _lastPlayerChunkCoord.z + viewDistanceHorizontal;
            int minY = _lastPlayerChunkCoord.y - viewDistanceVertical;
            int maxY = _lastPlayerChunkCoord.y + viewDistanceVertical;

            foreach (var kvp in _activeChunks)
            {
                var coord = kvp.Key;
                
                bool outOfBounds = coord.x < minX || coord.x > maxX ||
                                   coord.z < minZ || coord.z > maxZ ||
                                   coord.y < minY || coord.y > maxY;

                if (outOfBounds)
                {
                    _chunksToUnload.Add(coord);
                }
            }
        }
        
        private void ProcessUnloading()
        {
            int count = _chunksToUnload.Count;
            if (count == 0) return;

            int unloadedCount = 0;
            for (int i = count - 1; i >= 0; i--)
            {
                if (unloadedCount >= maxChunkUnloadsPerFrame) break;

                var coord = _chunksToUnload[i];
                _chunksToUnload.RemoveAt(i);
                
                if (!_activeChunks.TryGetValue(coord, out var chunk)) continue;
                if (chunk == null) continue;

                _chunksGeneratingData.Remove(chunk);
                _chunksGeneratingMesh.Remove(chunk);
                
                chunk.CancelAndClear();
                chunk.gameObject.SetActive(false);
                
                _chunkPool.Enqueue(chunk);
                _activeChunks.Remove(coord);
                
                unloadedCount++;
            }
        }

        private void ProcessLoading()
        {
            int countToLoad = Mathf.Min(_chunksToLoadList.Count, maxChunkCreationsPerFrame);
            if (countToLoad == 0) return;

            for (int i = 0; i < countToLoad; i++)
            {
                int lastIndex = _chunksToLoadList.Count - 1;
                var coord = _chunksToLoadList[lastIndex];
                _chunksToLoadList.RemoveAt(lastIndex);

                if (_activeChunks.ContainsKey(coord)) continue;

                Chunk chunk;
                if (_chunkPool.Count > 0)
                {
                    chunk = _chunkPool.Dequeue();
                }
                else
                {
                    chunk = CreateChunkInstance();
                    if (chunk == null) continue;
                }
                
                var position = (Vector3)coord * ChunkSize;
                chunk.transform.position = position;
                
#if UNITY_EDITOR
                chunk.name = $"Chunk {coord}";
#endif
                chunk.gameObject.SetActive(true);
                chunk.ChunkCoordinate = coord;
                chunk.StartGeneration(terrainSettings);

                _activeChunks.Add(coord, chunk);
                _chunksGeneratingData.Add(chunk);
            }
        }

        private void ProcessGenerationLifecycle()
        {
            for (int i = _chunksGeneratingData.Count - 1; i >= 0; i--)
            {
                var chunk = _chunksGeneratingData[i];
                
                if (chunk == null || !chunk.gameObject.activeSelf) 
                {
                    SwapRemove(_chunksGeneratingData, i);
                    continue;
                }
                
                if (chunk.IsDataGenerationCompleted())
                {
                    chunk.StartMeshGeneration();
                    _chunksGeneratingMesh.Add(chunk);
                    SwapRemove(_chunksGeneratingData, i);
                }
            }

            for (int i = _chunksGeneratingMesh.Count - 1; i >= 0; i--)
            {
                var chunk = _chunksGeneratingMesh[i];

                if (chunk == null || !chunk.gameObject.activeSelf)
                {
                    SwapRemove(_chunksGeneratingMesh, i);
                    continue;
                }

                if (chunk.IsMeshGenerationCompleted())
                {
                    _chunksReadyToApplyMesh.Enqueue(chunk);
                    SwapRemove(_chunksGeneratingMesh, i);
                }
            }

            while (_chunksReadyToApplyMesh.Count > 0)
            {
                var chunk = _chunksReadyToApplyMesh.Dequeue();
                if (chunk == null || !chunk.gameObject.activeSelf) continue;

                Profiler.BeginSample("ApplyMesh");
                chunk.ApplyMesh();
                Profiler.EndSample();
            }
        }

        private void SwapRemove<T>(List<T> list, int index)
        {
            int lastIndex = list.Count - 1;
            if (index != lastIndex)
            {
                list[index] = list[lastIndex];
            }
            list.RemoveAt(lastIndex);
        }

        private static Vector3Int GetChunkCoordinate(Vector3 position) => Vector3Int.FloorToInt(position / ChunkSize);
    }
}
