using System.Collections.Generic;
using UnityEngine;

namespace Terrain
{
    public class WorldManager : MonoBehaviour
    {
        [Header("World Settings")] [SerializeField]
        private Transform player;

        [SerializeField] private GameObject chunkPrefab;
        [SerializeField] private TerrainSettings terrainSettings;
        
        [SerializeField] private int viewDistanceHorizontal = 4;
        [SerializeField] private int viewDistanceVertical = 2;

        private const int ChunkSize = NaiveSurfaceNets.Chunk.ChunkSizeMinusTwo;
        
        private readonly Dictionary<Vector3Int, Chunk> _activeChunks = new();
        private Vector3Int _lastPlayerChunkCoord;
        
        private readonly Queue<Chunk> _chunkPool = new();

        private void Start()
        {
            var poolSizeX = viewDistanceHorizontal * 2 + 1;
            var poolSizeY = viewDistanceVertical * 2 + 1;
            var initialPoolSize = poolSizeX * poolSizeY * poolSizeX;
            
            for (var i = 0; i < initialPoolSize; i++)
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

        private Vector3Int GetChunkCoordinate(Vector3 position)
        {
            return new Vector3Int(
                Mathf.FloorToInt(position.x / ChunkSize),
                Mathf.FloorToInt(position.y / ChunkSize),
                Mathf.FloorToInt(position.z / ChunkSize)
            );
        }

        private void UpdateChunks()
        {
            var playerChunkCoord = GetChunkCoordinate(player.position);

            for (var x = -viewDistanceHorizontal; x <= viewDistanceHorizontal; x++)
            for (var y = -viewDistanceVertical; y <= viewDistanceVertical; y++)
            for (var z = -viewDistanceHorizontal; z <= viewDistanceHorizontal; z++)
            {
                var coord = new Vector3Int(
                    playerChunkCoord.x + x, 
                    playerChunkCoord.y + y, 
                    playerChunkCoord.z + z
                );

                if (!_activeChunks.ContainsKey(coord))
                    GetAndSetupChunk(coord);
            }

            var chunksToReturn = new List<Vector3Int>();
            foreach (var chunk in _activeChunks)
            {
                var coord = chunk.Key;
                
                var horzDistance = Vector2.Distance(new Vector2(coord.x, coord.z), new Vector2(playerChunkCoord.x, playerChunkCoord.z));
                var vertDistance = Mathf.Abs(coord.y - playerChunkCoord.y);

                if (horzDistance > viewDistanceHorizontal + 1 || vertDistance > viewDistanceVertical + 1)
                {
                    chunksToReturn.Add(coord);
                }
            }

            foreach (var coord in chunksToReturn)
                ReturnChunkToPool(coord);
        }
        private void GetAndSetupChunk(Vector3Int coord)
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

            _activeChunks.Add(coord, chunkScript);
        }



        private void ReturnChunkToPool(Vector3Int coord)
        {
            if (!_activeChunks.TryGetValue(coord, out var chunk)) return;

            chunk.CompleteJobs();
            chunk.gameObject.SetActive(false);
            _chunkPool.Enqueue(chunk);
            
            _activeChunks.Remove(coord);
        }
    }
}