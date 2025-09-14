using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace Terrain
{
    [BurstCompile]
    public struct MarchingCubesJob : IJob
    {
        // --- Input data ---
        [ReadOnly] public NativeArray<float> VoxelData;
        [ReadOnly] public NativeArray<int> TriangleTable;
        [ReadOnly] public NativeArray<int3> CornerTable;
        [ReadOnly] public NativeArray<int2> EdgeConnectionTable;

        public int GridSize;
        public float SurfaceLevel;

        // --- Output data ---
        public NativeList<float3> Vertices;
        public NativeList<int> Triangles;

        public void Execute()
        {
            // Walk through every voxel
            for (var x = 0; x < GridSize - 1; x++)
            for (var y = 0; y < GridSize - 1; y++)
            for (var z = 0; z < GridSize - 1; z++)
            {
                var cubePos = new int3(x, y, z);

                var cubeCornerValues = new NativeArray<float>(8, Allocator.Temp);
                for (var i = 0; i < 8; i++)
                {
                    var cornerPos = cubePos + CornerTable[i];
                    // 3D to 1D
                    var cornerIndex1D = cornerPos.x + cornerPos.y * GridSize + cornerPos.z * GridSize * GridSize;
                    cubeCornerValues[i] = VoxelData[cornerIndex1D];
                }

                // Count mask
                var cubeIndex = 0;
                if (cubeCornerValues[0] < SurfaceLevel) cubeIndex |= 1;
                if (cubeCornerValues[1] < SurfaceLevel) cubeIndex |= 2;
                if (cubeCornerValues[2] < SurfaceLevel) cubeIndex |= 4;
                if (cubeCornerValues[3] < SurfaceLevel) cubeIndex |= 8;
                if (cubeCornerValues[4] < SurfaceLevel) cubeIndex |= 16;
                if (cubeCornerValues[5] < SurfaceLevel) cubeIndex |= 32;
                if (cubeCornerValues[6] < SurfaceLevel) cubeIndex |= 64;
                if (cubeCornerValues[7] < SurfaceLevel) cubeIndex |= 128;

                // Use mask
                for (var i = 0; TriangleTable[cubeIndex * 16 + i] != -1; i += 3)
                {
                    var edge1 = TriangleTable[cubeIndex * 16 + i];
                    var edge2 = TriangleTable[cubeIndex * 16 + i + 1];
                    var edge3 = TriangleTable[cubeIndex * 16 + i + 2];

                    // Create vertices
                    var vert1 = InterpolateVertex(cubePos, edge1, cubeCornerValues, SurfaceLevel, CornerTable,
                        EdgeConnectionTable);
                    var vert2 = InterpolateVertex(cubePos, edge2, cubeCornerValues, SurfaceLevel, CornerTable,
                        EdgeConnectionTable);
                    var vert3 = InterpolateVertex(cubePos, edge3, cubeCornerValues, SurfaceLevel, CornerTable,
                        EdgeConnectionTable);

                    Triangles.Add(Vertices.Length);
                    Vertices.Add(vert1);
                    Triangles.Add(Vertices.Length);
                    Vertices.Add(vert2);
                    Triangles.Add(Vertices.Length);
                    Vertices.Add(vert3);
                }

                cubeCornerValues.Dispose();
            }
        }

        private static float3 InterpolateVertex(int3 cubePos, int edgeIndex, NativeArray<float> cornerValues,
            float surfaceLevel, [ReadOnly] NativeArray<int3> cornerTable,
            [ReadOnly] NativeArray<int2> edgeConnectionTable)
        {
            var indexA = edgeConnectionTable[edgeIndex].x;
            var indexB = edgeConnectionTable[edgeIndex].y;

            var cornerA = cornerTable[indexA];
            var cornerB = cornerTable[indexB];

            var valueA = cornerValues[indexA];
            var valueB = cornerValues[indexB];

            var t = math.unlerp(valueA, valueB, surfaceLevel);

            var positionOnEdge = math.lerp(cornerA, cornerB, t);

            return cubePos + positionOnEdge;
        }
    }
}