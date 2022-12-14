using UnityEngine;

public class MarchingCubes
{
    private Vector3[] _vertices;
    private int[] _triangles;
    private Color[] _colors;
    //private Vector2[] _uvs;
    private float _isolevel;

    private int _vertexIndex;

    private Vector3[] _vertexList;
    private Color[] _vertexColors;
    //private Vector2[] _vertexUVs;
    private Point[] _initPoints;
    private Mesh _mesh;
    private int[,,] _cubeIndexes;

    public MarchingCubes(Point[,,] points, float isolevel)
    {
        _isolevel = isolevel;

        _mesh = new Mesh();

        _vertexIndex = 0;

        _vertexList = new Vector3[12];
        _vertexColors = new Color[12];
        //_vertexUVs = new Vector2[12];
        _initPoints = new Point[8];
        _cubeIndexes = new int[points.GetLength(0) - 1, points.GetLength(1) - 1, points.GetLength(2) - 1];
    }

    private Vector3 VertexInterpolate(Vector3 p1, Vector3 p2, float v1, float v2)
    {
        if (Mathf.Abs(_isolevel - v1) < 0.000001f)
        {
            return p1;
        }
        if (Mathf.Abs(_isolevel - v2) < 0.000001f)
        {
            return p2;
        }
        if (Mathf.Abs(v1 - v2) < 0.000001f)
        {
            return p1;
        }

        float mu = (_isolevel - v1) / (v2 - v1);

        Vector3 p = p1 + mu * (p2 - p1);

        return p;
    }

    private void March(Point[] points, int cubeIndex)
    {
        int edgeIndex = LookupTables.EdgeTable[cubeIndex];

        _vertexList = GenerateVertexList(points, edgeIndex);

        int[] row = LookupTables.TriangleTable[cubeIndex];

        for (int i = 0; i < row.Length; i += 3)
        {
            _vertices[_vertexIndex] = _vertexList[row[i + 0]];
            //_uvs[_vertexIndex]= _vertexUVs[row[i + 0]];
            _triangles[_vertexIndex] = _vertexIndex;
            _colors[_vertexIndex] = _vertexColors[row[i + 0]];
            _vertexIndex++;

            _vertices[_vertexIndex] = _vertexList[row[i + 1]];
            //_uvs[_vertexIndex] = _vertexUVs[row[i + 1]];
            _triangles[_vertexIndex] = _vertexIndex;
            _colors[_vertexIndex] = _vertexColors[row[i + 1]];
            _vertexIndex++;

            _vertices[_vertexIndex] = _vertexList[row[i + 2]];
            //_uvs[_vertexIndex] = _vertexUVs[row[i + 2]];
            _triangles[_vertexIndex] = _vertexIndex;
            _colors[_vertexIndex] = _vertexColors[row[i + 2]];
            _vertexIndex++;
        }
    }

    private Vector3[] GenerateVertexList(Point[] points, int edgeIndex)
    {
        for (int i = 0; i < 12; i++)
        {
            if ((edgeIndex & (1 << i)) != 0)
            {
                int[] edgePair = LookupTables.EdgeIndexTable[i];
                int edge1 = edgePair[0];
                int edge2 = edgePair[1];

                Point point1 = points[edge1];
                Point point2 = points[edge2];

                _vertexList[i] = VertexInterpolate(point1.worldPosition, point2.worldPosition, point1.density, point2.density);
                /*if (point1.worldPosition == _vertexList[i])
                {
                    _vertexColors[i] = point1.color;
                }
                else
                {
                    _vertexColors[i] = point2.color;
                }*/
                _vertexColors[i] = (point1.color + point2.color) * 0.5f;
                //_vertexUVs[i] = new Vector2(VertexInterpolate(point1.worldPosition, point2.worldPosition, point1.density, point2.density).x, VertexInterpolate(point1.worldPosition, point2.worldPosition, point1.density, point2.density).z);
            }
        }

        return _vertexList;
    }

    private int CalculateCubeIndex(Point[] points, float iso)
    {
        int cubeIndex = 0;

        for (int i = 0; i < 8; i++)
            if (points[i].density > iso)
                cubeIndex |= 1 << i;

        return cubeIndex;
    }

    public Mesh CreateMeshData(Point[,,] points)
    {
        _cubeIndexes = GenerateCubeIndexes(points);
        int vertexCount = GenerateVertexCount(_cubeIndexes);

        if (vertexCount <= 0)
        {
            return new Mesh();
        }

        _vertices = new Vector3[vertexCount];
        _triangles = new int[vertexCount];
        _colors = new Color[vertexCount];
        //_uvs = new Vector2[vertexCount];

        for (int x = 0; x < points.GetLength(0) - 1; x++)
        {
            for (int y = 0; y < points.GetLength(1) - 1; y++)
            {
                for (int z = 0; z < points.GetLength(2) - 1; z++)
                {
                    int cubeIndex = _cubeIndexes[x, y, z];
                    if (cubeIndex == 0 || cubeIndex == 255) continue;

                    March(GetPoints(x, y, z, points), cubeIndex);
                }
            }
        }

        _vertexIndex = 0;

        _mesh.Clear();
        //_mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        _mesh.vertices = _vertices;
        _mesh.SetTriangles(_triangles, 0);
        _mesh.colors = _colors;
        //_mesh.SetUVs(0, _uvs);
        _mesh.RecalculateNormals();
        _mesh.RecalculateBounds();
        _mesh.RecalculateTangents();
        _mesh.Optimize();

        return _mesh;
    }

    private Point[] GetPoints(int x, int y, int z, Point[,,] points)
    {
        for (int i = 0; i < 8; i++)
        {
            Point p = points[x + CubePointsX[i], y + CubePointsY[i], z + CubePointsZ[i]];
            _initPoints[i] = p;
        }

        return _initPoints;
    }

    private int[,,] GenerateCubeIndexes(Point[,,] points)
    {
        for (int x = 0; x < points.GetLength(0) - 1; x++)
        {
            for (int y = 0; y < points.GetLength(1) - 1; y++)
            {
                for (int z = 0; z < points.GetLength(2) - 1; z++)
                {
                    _initPoints = GetPoints(x, y, z, points);

                    _cubeIndexes[x, y, z] = CalculateCubeIndex(_initPoints, _isolevel);
                }
            }
        }

        return _cubeIndexes;
    }

    private int GenerateVertexCount(int[,,] cubeIndexes)
    {
        int vertexCount = 0;

        for (int x = 0; x < cubeIndexes.GetLength(0); x++)
        {
            for (int y = 0; y < cubeIndexes.GetLength(1); y++)
            {
                for (int z = 0; z < cubeIndexes.GetLength(2); z++)
                {
                    int cubeIndex = cubeIndexes[x, y, z];
                    int[] row = LookupTables.TriangleTable[cubeIndex];
                    vertexCount += row.Length;
                }
            }
        }

        return vertexCount;
    }

    public static readonly Vector3Int[] CubePoints =
    {
        new Vector3Int(0, 0, 0),
        new Vector3Int(1, 0, 0),
        new Vector3Int(1, 0, 1),
        new Vector3Int(0, 0, 1),
        new Vector3Int(0, 1, 0),
        new Vector3Int(1, 1, 0),
        new Vector3Int(1, 1, 1),
        new Vector3Int(0, 1, 1)
    };

    public static readonly int[] CubePointsX =
    {
        0,
        1,
        1,
        0,
        0,
        1,
        1,
        0
    };

    public static readonly int[] CubePointsY =
    {
        0,
        0,
        0,
        0,
        1,
        1,
        1,
        1
    };

    public static readonly int[] CubePointsZ =
    {
        0,
        0,
        1,
        1,
        0,
        0,
        1,
        1
    };
}