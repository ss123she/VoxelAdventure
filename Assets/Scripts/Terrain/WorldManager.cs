using System;
using System.Collections.Generic;
using UnityEngine;

namespace Terrain
{
    public class WorldManager : MonoBehaviour
    {
        private static short ChunkPoolSize = 256;
        
        [SerializeField] private Transform player;
        [SerializeField] private GameObject chunkPrefab;
        [SerializeField] private TerrainSettings terrainSettings;
        [SerializeField] private WorldManagerSettings worldManagerSettings;
        
        private const int ChunkSize = NaiveSurfaceNets.Chunk.ChunkSizeMinusTwo;
        private readonly Dictionary<Vector3Int, Chunk> _activeChunks = new();
        private readonly List<Chunk> _processingChunks = new();
        private readonly Queue<Chunk> _chunkPool = new();
        private Vector3Int _lastPlayerChunk;
        private Vector3Int[] _chunkOffsets;
        private bool _lastDebugState;

        private void Start()
        {
            if (terrainSettings.RandomizeSeedOnPlay)
                terrainSettings.WorldSeed = new Unity.Mathematics.Random((uint)DateTime.Now.Ticks).NextInt();

            CreateSortedOffsets();
            
            for (int i = 0; i < ChunkPoolSize; i++)
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
            var moved = playerChunk != _lastPlayerChunk;
            _lastPlayerChunk = playerChunk;

            if (moved)
            {
                UnloadChunks(playerChunk);
                UpdateChunkDebugInfo(playerChunk);
            }
            LoadChunks(playerChunk);

            if (_lastDebugState != worldManagerSettings.showDebugInfo)
            {
                _lastDebugState = worldManagerSettings.showDebugInfo;
                RefreshDebugInfoVisibility();
            }

            ProcessChunksLifecycle();
        }

        private void UnloadChunks(Vector3Int center)
        {
            var viewDistanceHorizontal = worldManagerSettings.viewDistanceHorizontal;
            var viewDistanceVertical = worldManagerSettings.viewDistanceVertical;
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
                if (loadedCount >= worldManagerSettings.chunksPerFrame) break;

                var coord = center + offset;
                if (_activeChunks.ContainsKey(coord)) continue;

                if (Mathf.Abs(coord.y - center.y) > worldManagerSettings.viewDistanceVertical) continue;

                Chunk chunk = _chunkPool.Count > 0 ? _chunkPool.Dequeue() : Instantiate(chunkPrefab, transform).GetComponent<Chunk>();
                
                chunk.transform.SetPositionAndRotation((Vector3)coord * ChunkSize, Quaternion.identity);
                chunk.ChunkCoordinate = coord;
                chunk.gameObject.SetActive(true);
                chunk.StartGeneration(terrainSettings, worldManagerSettings.showDebugInfo);

                _activeChunks.Add(coord, chunk);
                _processingChunks.Add(chunk);
                UpdateChunkDebugInfo(_lastPlayerChunk);
                loadedCount++;
            }
        }

        private void ProcessChunksLifecycle()
        {
            for (int i = _processingChunks.Count - 1; i >= 0; i--)
            {
                var chunk = _processingChunks[i];

                if (chunk.IsDataGenerationCompleted())
                    chunk.StartMeshGeneration();
                else if (chunk.IsMeshGenerationCompleted())
                {
                    chunk.ApplyMesh();
                    _processingChunks.RemoveAt(i);
                }
            }
        }

        private void CreateSortedOffsets()
        {
            var viewDistanceHorizontal = worldManagerSettings.viewDistanceHorizontal;
            var viewDistanceVertical = worldManagerSettings.viewDistanceVertical;

            var list = new List<Vector3Int>();
            for (int x = -viewDistanceHorizontal; x <= viewDistanceHorizontal; x++)
            for (int y = -viewDistanceVertical; y <= viewDistanceVertical; y++)
            for (int z = -viewDistanceHorizontal; z <= viewDistanceHorizontal; z++)
                list.Add(new Vector3Int(x, y, z));

            list.Sort((a, b) => a.sqrMagnitude.CompareTo(b.sqrMagnitude));
            _chunkOffsets = list.ToArray();
        }

        private void UpdateChunkDebugInfo(Vector3Int playerChunkCoord)
        {
            foreach (var kvp in _activeChunks)
            {
                Chunk chunk = kvp.Value;
                Vector3Int chunkCoord = kvp.Key;

                var dx = Mathf.Abs(chunkCoord.x - playerChunkCoord.x);
                var dy = Mathf.Abs(chunkCoord.y - playerChunkCoord.y);
                var dz = Mathf.Abs(chunkCoord.z - playerChunkCoord.z);

                int distance = Mathf.Max(dx, Mathf.Max(dy, dz));

                chunk.DebugDistanceFromPlayer = distance;
            }
        }

        private void RefreshDebugInfoVisibility()
        {
            bool show = worldManagerSettings.showDebugInfo;
            foreach (var chunk in _activeChunks.Values)
                chunk.SetDebugVisibility(show);
        }
    }
}