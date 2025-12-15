using UnityEngine;

namespace Terrain
{
    public class ChunkShowcase : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TerrainSettings settings;
        [SerializeField] private GameObject chunkPrefab;

        [Header("Showcase Controls")]
        [SerializeField] private bool autoRotate = true;
        [SerializeField] private float rotationSpeed = 10f;
        
        private Chunk _currentChunk;
        private bool _isGenerating;

        private void Start()
        {
            var go = Instantiate(chunkPrefab, transform);
            
            float offset = -NaiveSurfaceNets.Chunk.ChunkSizeMinusTwo / 2f;
            go.transform.localPosition = new Vector3(offset, offset, offset);
            
            _currentChunk = go.GetComponent<Chunk>();

            Regenerate();
        }

        private void Update()
        {
            if (autoRotate)
            {
                transform.Rotate(rotationSpeed * Time.deltaTime * Vector3.up);
            }

            if (_isGenerating && _currentChunk != null)
            {
                if (_currentChunk.IsDataGenerationCompleted())
                    _currentChunk.StartMeshGeneration();
                else if (_currentChunk.IsMeshGenerationCompleted())
                {
                    _currentChunk.ApplyMesh();
                    _isGenerating = false;
                }
            }
        }

        public void Regenerate()
        {
            if (_currentChunk == null) return;

            _currentChunk.CancelAndClear();
            _currentChunk.gameObject.SetActive(true);
            
            _currentChunk.ChunkCoordinate = Vector3Int.zero;
            
            _currentChunk.StartGeneration(settings);
            _isGenerating = true;
        }
    }
}