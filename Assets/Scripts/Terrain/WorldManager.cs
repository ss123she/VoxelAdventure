using System.Collections.Generic;
using UnityEngine;

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
        private readonly List<Vector3Int> _chunksToLoad = new();
        private readonly List<Vector3Int> _chunksToUnload = new();
        
        private readonly List<Chunk> _chunksGeneratingData = new();
        private readonly List<Chunk> _chunksGeneratingMesh = new();
        private readonly Queue<Chunk> _chunksReadyToApplyMesh = new();

        private readonly Queue<Chunk> _chunkPool = new();
        private Vector3Int _lastPlayerChunkCoord;

        private void Start()
        {
            var poolSizeX = viewDistanceHorizontal * 2 + 2;
            var poolSizeY = viewDistanceVertical * 2 + 2;
            var initialPoolSize = poolSizeX * poolSizeY * poolSizeX;

            for (var i = 0; i < initialPoolSize; i++)
            {
                var chunkObject = Instantiate(chunkPrefab, Vector3.zero, Quaternion.identity, transform);
                chunkObject.SetActive(false);
                _chunkPool.Enqueue(chunkObject.GetComponent<Chunk>());
            }
            
            _lastPlayerChunkCoord = GetChunkCoordinate(player.position) + Vector3Int.one;
        }
        
        private void Update()
        {
            var playerChunkCoord = GetChunkCoordinate(player.position);
            if (playerChunkCoord != _lastPlayerChunkCoord)
            {
                _lastPlayerChunkCoord = playerChunkCoord;
                UpdateChunkActivationLists();
            }
            
            ProcessUnloadList();
            ProcessLoadList();
            ProcessDataGenerationList();
            ProcessMeshGenerationList();
            ProcessReadyMeshQueue();
        }

        private void UpdateChunkActivationLists()
        {
            for (var x = -viewDistanceHorizontal; x <= viewDistanceHorizontal; x++)
            for (var y = -viewDistanceVertical; y <= viewDistanceVertical; y++)
            for (var z = -viewDistanceHorizontal; z <= viewDistanceHorizontal; z++)
            {
                var coord = new Vector3Int(_lastPlayerChunkCoord.x + x, _lastPlayerChunkCoord.y + y, _lastPlayerChunkCoord.z + z);
                if (!_activeChunks.ContainsKey(coord))
                {
                    if (!_chunksToLoad.Contains(coord))
                    {
                        _chunksToLoad.Add(coord);
                    }
                }
            }

            foreach (var chunk in _activeChunks)
            {
                var coord = chunk.Key;
                var horzDistance = Vector2.Distance(new Vector2(coord.x, coord.z), new Vector2(_lastPlayerChunkCoord.x, _lastPlayerChunkCoord.z));
                var vertDistance = Mathf.Abs(coord.y - _lastPlayerChunkCoord.y);

                if (horzDistance > viewDistanceHorizontal || vertDistance > viewDistanceVertical)
                {
                    if (!_chunksToUnload.Contains(coord))
                    {
                        _chunksToUnload.Add(coord);
                    }
                }
            }
        }
        
        private void ProcessUnloadList()
        {
            for (int i = _chunksToUnload.Count - 1; i >= 0; i--)
            {
                var coord = _chunksToUnload[i];
                if (_activeChunks.TryGetValue(coord, out var chunk))
                {
                    ReturnChunkToPool(coord);
                }
                _chunksToUnload.RemoveAt(i);
            }
        }

        private void ProcessLoadList()
        {
            for (int i = 0; i < maxChunkCreationsPerFrame && _chunksToLoad.Count > 0; i++)
            {
                var coord = _chunksToLoad[0];
                _chunksToLoad.RemoveAt(0);

                var newChunk = GetAndSetupChunk(coord);
                _chunksGeneratingData.Add(newChunk);
                _activeChunks.Add(coord, newChunk);
            }
        }

        private void ProcessDataGenerationList()
        {
            for (int i = _chunksGeneratingData.Count - 1; i >= 0; i--)
            {
                var chunk = _chunksGeneratingData[i];
                if (chunk.IsDataGenerationCompleted())
                {
                    _chunksGeneratingData.RemoveAt(i);
                    _chunksGeneratingMesh.Add(chunk);
                    chunk.StartMeshGeneration();
                }
            }
        }
        
        private void ProcessMeshGenerationList()
        {
            for (int i = _chunksGeneratingMesh.Count - 1; i >= 0; i--)
            {
                var chunk = _chunksGeneratingMesh[i];
                if (chunk.IsMeshGenerationCompleted())
                {
                    _chunksGeneratingMesh.RemoveAt(i);
                    _chunksReadyToApplyMesh.Enqueue(chunk);
                }
            }
        }

        private void ProcessReadyMeshQueue()
        {
            for (int i = 0; i < maxMeshAppliesPerFrame && _chunksReadyToApplyMesh.Count > 0; i++)
            {
                var chunk = _chunksReadyToApplyMesh.Dequeue();
                chunk.ApplyMesh();
            }
        }
        
        private Chunk GetAndSetupChunk(Vector3Int coord)
        {
            var position = new Vector3(coord.x * ChunkSize, coord.y * ChunkSize, coord.z * ChunkSize);
            
            Chunk chunkScript;
            if (_chunkPool.Count > 0)
            {
                chunkScript = _chunkPool.Dequeue();
                chunkScript.transform.position = position;
                chunkScript.gameObject.SetActive(true);
            }
            else
            {
                var chunkObject = Instantiate(chunkPrefab, position, Quaternion.identity, transform);
                chunkScript = chunkObject.GetComponent<Chunk>();
            }
            
            chunkScript.name = $"Chunk {coord.x}, {coord.y}, {coord.z}";
            chunkScript.chunkCoordinate = coord;
            chunkScript.StartGeneration(terrainSettings);
            
            return chunkScript;
        }

        private void ReturnChunkToPool(Vector3Int coord)
        {
            if (!_activeChunks.TryGetValue(coord, out var chunk)) return;

            _chunksGeneratingData.Remove(chunk);
            _chunksGeneratingMesh.Remove(chunk);

            chunk.CancelAndClear();
            chunk.gameObject.SetActive(false);
            _chunkPool.Enqueue(chunk);
            
            _activeChunks.Remove(coord);
        }

        private Vector3Int GetChunkCoordinate(Vector3 position)
        {
            return new Vector3Int(
                Mathf.FloorToInt(position.x / ChunkSize),
                Mathf.FloorToInt(position.y / ChunkSize),
                Mathf.FloorToInt(position.z / ChunkSize)
            );
        }
    }
}