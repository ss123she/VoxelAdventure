using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

namespace Terrain
{
    public class WorldManager : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private GameObject chunkPrefab;
        [SerializeField] private TerrainSettings terrainSettings;

        [Header("Settings")]
        [SerializeField] private int viewDistanceHorizontal = 8;
        [SerializeField] private int viewDistanceVertical = 4; 
        [SerializeField] private int chunksPerFrame = 5;

        private const int ChunkSize = NaiveSurfaceNets.Chunk.ChunkSizeMinusTwo;
        private readonly Dictionary<Vector3Int, Chunk> _activeChunks = new();
        private readonly List<Chunk> _processingChunks = new();
        private readonly Queue<Chunk> _chunkPool = new();
        private Vector3Int _lastPlayerChunk;
        private Vector3Int[] _chunkOffsets;

        private void Start()
        {
            if (terrainSettings.RandomizeSeedOnPlay)
                terrainSettings.WorldSeed = new Unity.Mathematics.Random((uint)System.DateTime.Now.Ticks).NextInt();

            CreateSortedOffsets();
            
            for (int i = 0; i < 200; i++)
            {
                var go = Instantiate(chunkPrefab, transform);
                go.SetActive(false);
                if (go.TryGetComponent(out Chunk c)) _chunkPool.Enqueue(c);
                else Destroy(go);
            }
        }

        private void Update()
        {
            if (!player) return;

            var playerChunk = Vector3Int.FloorToInt(player.position / ChunkSize);
            bool moved = playerChunk != _lastPlayerChunk;
            _lastPlayerChunk = playerChunk;

            if (moved) UnloadChunks(playerChunk);
            LoadChunks(playerChunk);
            ProcessChunksLifecycle();
        }

        private void UnloadChunks(Vector3Int center)
        {
            var toRemove = new List<Vector3Int>();
            foreach (var kvp in _activeChunks)
            {
                var pos = kvp.Key;
                if (Mathf.Abs(pos.x - center.x) > viewDistanceHorizontal ||
                    Mathf.Abs(pos.y - center.y) > viewDistanceVertical ||
                    Mathf.Abs(pos.z - center.z) > viewDistanceHorizontal)
                {
                    toRemove.Add(pos);
                }
            }

            foreach (var pos in toRemove)
            {
                var chunk = _activeChunks[pos];
                chunk.CancelAndClear();
                chunk.gameObject.SetActive(false);
                _processingChunks.Remove(chunk);
                _activeChunks.Remove(pos);
                _chunkPool.Enqueue(chunk);
            }
        }

        private void LoadChunks(Vector3Int center)
        {
            int loadedCount = 0;
            foreach (var offset in _chunkOffsets)
            {
                if (loadedCount >= chunksPerFrame) break;

                var coord = center + offset;
                if (_activeChunks.ContainsKey(coord)) continue;

                if (Mathf.Abs(coord.y - center.y) > viewDistanceVertical) continue;

                Chunk chunk = _chunkPool.Count > 0 ? _chunkPool.Dequeue() : Instantiate(chunkPrefab, transform).GetComponent<Chunk>();
                
                chunk.transform.SetPositionAndRotation((Vector3)coord * ChunkSize, Quaternion.identity);
                chunk.ChunkCoordinate = coord;
                chunk.gameObject.SetActive(true);
                chunk.StartGeneration(terrainSettings);

                _activeChunks.Add(coord, chunk);
                _processingChunks.Add(chunk);
                loadedCount++;
            }
        }

        private void ProcessChunksLifecycle()
        {
            for (int i = _processingChunks.Count - 1; i >= 0; i--)
            {
                var chunk = _processingChunks[i];

                if (chunk.IsDataGenerationCompleted())
                {
                    chunk.StartMeshGeneration();
                }
                else if (chunk.IsMeshGenerationCompleted())
                {
                    chunk.ApplyMesh();
                    _processingChunks.RemoveAt(i);
                }
            }
        }

        private void CreateSortedOffsets()
        {
            var list = new List<Vector3Int>();
            for (int x = -viewDistanceHorizontal; x <= viewDistanceHorizontal; x++)
            for (int y = -viewDistanceVertical; y <= viewDistanceVertical; y++)
            for (int z = -viewDistanceHorizontal; z <= viewDistanceHorizontal; z++)
                list.Add(new Vector3Int(x, y, z));

            list.Sort((a, b) => a.sqrMagnitude.CompareTo(b.sqrMagnitude));
            _chunkOffsets = list.ToArray();
        }
    }
}