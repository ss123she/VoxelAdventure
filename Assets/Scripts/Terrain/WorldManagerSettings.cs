using UnityEngine;

namespace Terrain
{
    [CreateAssetMenu(fileName = "New World Manager Settings", menuName = "Terrain/World Manager Settings")]
    public class WorldManagerSettings : ScriptableObject
    {
        [Range(0, 50)]
        public int viewDistanceHorizontal;
        [Range(0, 10)]
        public int viewDistanceVertical;
        [Range(0, 50)]
        public int chunksPerFrame;

        public bool showDebugInfo;
    }
}