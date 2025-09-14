using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Rendering;

namespace Terrain
{
    [RequireComponent(typeof(MeshFilter))]
    public class Chunk : MonoBehaviour
    {
        public Vector3Int chunkCoordinate;
        private int _gridSize;

        private NativeArray<float> _noiseDataNative;
        private NativeList<float3> _vertices;
        private NativeList<int> _triangles;
        
        private JobHandle _generationHandle;
        private bool _isGenerating;
        
        private MeshFilter _meshFilter;

        private void Awake()
        {
            _meshFilter = GetComponent<MeshFilter>();
        }

        public void StartGeneration(int gridSize, float terrainSurface, float terrainHeight, float groundLevel, float noiseScale)
        {
            _gridSize = gridSize;
        
            var size = _gridSize + 1;
            var totalSize = size * size * size;
            var offset = new Vector3(chunkCoordinate.x * _gridSize, 0, chunkCoordinate.z * _gridSize);

            _noiseDataNative = new NativeArray<float>(totalSize, Allocator.Persistent);
            _vertices = new NativeList<float3>(Allocator.Persistent);
            _triangles = new NativeList<int>(Allocator.Persistent);
        
            var noiseJob = new NoiseJob
            {
                DataNative = _noiseDataNative,
                ChunkSize = _gridSize,
                NoiseScale = noiseScale,
                Offset = offset,
                TerrainHeight = terrainHeight,
                GroundLevel = groundLevel
            };
            var nsJobHandle = noiseJob.Schedule(totalSize, 64);

            var mcJob = new MarchingCubesJob
            {
                VoxelData = _noiseDataNative,
                TriangleTable = MarchingTable.TrianglesNative,
                CornerTable = MarchingTable.CornersNative,
                EdgeConnectionTable = MarchingTable.EdgeConnectionsNative,
                GridSize = size,
                SurfaceLevel = terrainSurface,
                Vertices = _vertices,
                Triangles = _triangles
            };
            
            _generationHandle = mcJob.Schedule(nsJobHandle);
            _isGenerating = true;
        }
        
        private void LateUpdate()
        {
            if (!_isGenerating)
                return;
            
            if (!_generationHandle.IsCompleted)
                return;
            
            _generationHandle.Complete();
            ApplyMesh();
            _isGenerating = false;
        }
    
        private void ApplyMesh()
        {
            if (_vertices.Length == 0)
            {
                DisposeNativeData();
                return;
            }
            
            var mesh = new Mesh();
            mesh.indexFormat = _vertices.Length > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16;
        
            mesh.SetVertexBufferParams(_vertices.Length, new VertexAttributeDescriptor(VertexAttribute.Position));
            mesh.SetVertexBufferData(_vertices.AsArray(), 0, 0, _vertices.Length);
            mesh.SetIndexBufferParams(_triangles.Length, IndexFormat.UInt32);
            mesh.SetIndexBufferData(_triangles.AsArray(), 0, 0, _triangles.Length);
        
            mesh.subMeshCount = 1;
            mesh.SetSubMesh(0, new SubMeshDescriptor(0, _triangles.Length));
            
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
        
            _meshFilter.mesh = mesh;
        
            DisposeNativeData();
        }

        private void DisposeNativeData()
        {
            if (_noiseDataNative.IsCreated) _noiseDataNative.Dispose();
            if (_vertices.IsCreated) _vertices.Dispose();
            if (_triangles.IsCreated) _triangles.Dispose();
        }

        private void OnDestroy()
        { 
            if (_isGenerating)
                _generationHandle.Complete();
            
            DisposeNativeData();
        }
        
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(transform.position + new Vector3(_gridSize / 2f, _gridSize / 2f, _gridSize / 2f), 
                new Vector3(_gridSize, _gridSize, _gridSize));
        }
    }
}