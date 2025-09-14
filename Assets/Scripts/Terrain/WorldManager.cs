using System.Collections.Generic;
using UnityEngine;

namespace Terrain
{
    public class WorldManager : MonoBehaviour
    {
        [Header("World Settings")] [SerializeField]
        private Transform player;

        [SerializeField] private GameObject chunkPrefab;
        [SerializeField] private int viewDistance = 4;

        [Header("Chunk Generation Settings")] [SerializeField]
        private int chunkSize = 20;

        [SerializeField] private float terrainSurface = 0.5f;
        [SerializeField] private float terrainHeight = 4.0f;
        [SerializeField] private float groundLevel = 2.0f;
        [SerializeField] private float noiseScale = 0.04f;

        private readonly Dictionary<Vector2Int, Chunk> _activeChunks = new();
        private Vector2Int _lastPlayerChunkCoord;
        
        private readonly Queue<Chunk> _chunkPool = new();

        private void Start()
        {
            int initialPoolSize = (viewDistance * 2 + 1) * 2;
            for (int i = 0; i < initialPoolSize; i++)
            {
                var chunkObject = Instantiate(chunkPrefab, Vector3.zero, Quaternion.identity, transform);
                chunkObject.SetActive(false);
                _chunkPool.Enqueue(chunkObject.GetComponent<Chunk>());
            }
            
            UpdateChunks();
        }

        private void Update()
        {
            var playerChunkCoord = GetChunkCoordinate(player.position);
            if (playerChunkCoord == _lastPlayerChunkCoord) return;

            _lastPlayerChunkCoord = playerChunkCoord;
            UpdateChunks();
        }

        private Vector2Int GetChunkCoordinate(Vector3 position)
        {
            return new Vector2Int(
                Mathf.FloorToInt(position.x / chunkSize),
                Mathf.FloorToInt(position.z / chunkSize)
            );
        }

        private void UpdateChunks()
        {
            var playerChunkCoord = GetChunkCoordinate(player.position);

            for (var x = -viewDistance; x <= viewDistance; x++)
            for (var z = -viewDistance; z <= viewDistance; z++)
            {
                var coord = new Vector2Int(playerChunkCoord.x + x, playerChunkCoord.y + z);

                if (!_activeChunks.ContainsKey(coord))
                    GetAndSetupChunk(new Vector3Int(coord.x, 0, coord.y));
            }

            var chunksToReturn = new List<Vector2Int>();
            foreach (var chunk in _activeChunks)
            {
                var coord = chunk.Key;
                var distance = Vector2.Distance(coord, playerChunkCoord);

                if (distance > viewDistance + 1) chunksToReturn.Add(coord);
            }

            foreach (var coord in chunksToReturn)
                ReturnChunkToPool(new Vector3Int(coord.x, 0, coord.y));
        }

        private void GetAndSetupChunk(Vector3Int coord)
        {
            var position = new Vector3(coord.x * chunkSize, 0, coord.z * chunkSize);
            
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
            
            chunkScript.name = $"Chunk {coord.x}, {coord.z}";
            chunkScript.chunkCoordinate = coord;
            chunkScript.StartGeneration(chunkSize, terrainSurface, terrainHeight, groundLevel, noiseScale);

            _activeChunks.Add(new Vector2Int(coord.x, coord.z), chunkScript);
        }

        private void ReturnChunkToPool(Vector3Int coord)
        {
            var coord2D = new Vector2Int(coord.x, coord.z);
            if (!_activeChunks.TryGetValue(coord2D, out var chunk)) return;

            chunk.gameObject.SetActive(false);
            _chunkPool.Enqueue(chunk);
            
            _activeChunks.Remove(coord2D);
        }
    }
}