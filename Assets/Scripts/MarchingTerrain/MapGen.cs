using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

public class MapGen : Singleton<MapGen>
{
    public UnityAction OnChunksGenerated;

    public void ChunksGenerated()
    {
        Debug.Log("Chunks ready!");
    }

    public static Vector3Int mapSize = new Vector3Int(256, 5, 256);
    public static Vector3Int chunkSize = new Vector3Int(16, 5, 16);
    private const float chunkMeshResolution = 2.0f;

    public Material TerrainMat;

    public Dictionary<Vector3Int, Chunk> ChunkCells = new Dictionary<Vector3Int, Chunk>();

    private MarchingCubes marchCubes;

    //0 water -> black, 1 land -> red, 2 elevation -> green, 3 hill -> blue 
    private int[,] heightMap;
    //3D density map
    private Point[,,] Points;

    public bool visualizeHeightMap = false;
    [Range(0, 4)]
    public int pointLayer = 0;
    public bool visualizePoints = false;

    private Bounds worldBounds;
    private Vector2 noiseOffset;
    private float noiseScale = 16.0f;

    //This is an offset for each poly to make some roughness on terrain surface
    private static float iceRoughness = 0.02f;
    private static float terrainRoughness = 0.08f;

    void Start()
    {
        noiseOffset = new Vector2(Random.Range(0, 999999), Random.Range(0, 999999));

        Points = new Point[mapSize.x, mapSize.y, mapSize.z];
        heightMap = new int[mapSize.x, mapSize.z];

        Application.targetFrameRate = 60;

        worldBounds = new Bounds(new Vector3(mapSize.x * 0.5f, mapSize.y * 0.5f, mapSize.z * 0.5f), mapSize);

        //Fill up height and density map
        DateTime exectime = DateTime.Now;

        float xCoord;
        float zCoord;
        float density;
        Color color;

        for (int y = 0; y < mapSize.y; y++)
        {
            for (int z = 0; z < mapSize.z; z++)
            {
                for (int x = 0; x < mapSize.x; x++)
                {
                    if (y == 0)
                    {
                        xCoord = (float)x / (float)mapSize.x * noiseScale + noiseOffset.x;
                        zCoord = (float)z / (float)mapSize.z * noiseScale + noiseOffset.y;
                        heightMap[x, z] = Mathf.RoundToInt(Mathf.PerlinNoise(xCoord, zCoord) * (float)(mapSize.y - 2));

                        Points[x, y, z] = new Point(new Vector3(x, y, z),
                                                    Random.Range(1.0f - iceRoughness, 1.0f),
                                                    Color.black);
                    }
                    else
                    {
                        density = (heightMap[x, z] >= y) ? Random.Range(1.0f - terrainRoughness, 1.0f) : 0.0f;

                        color = Color.black;

                        if (heightMap[x, z] > 1 && heightMap[x, z] < 2) color = Color.red;
                        if (heightMap[x, z] > 2 && heightMap[x, z] < 3) color = Color.green;
                        if (heightMap[x, z] > 3 && heightMap[x, z] < 4) color = Color.blue;

                        Points[x, y, z] = new Point(new Vector3(x, y, z), density, color);
                    }
                }
            }
        }

        Debug.Log("Height and density map generated in: " + (DateTime.Now - exectime).Milliseconds + " ms");

        //Chunk Generation
        exectime = DateTime.Now;

        for (int z = 0; z < mapSize.z - chunkSize.z; z += chunkSize.z)
        {
            for (int x = 0; x < mapSize.x - chunkSize.x; x += chunkSize.x)
            {
                Vector3Int worldPosition = new Vector3Int(x, 0, z);

                ChunkCells.Add(worldPosition, CreateChunk(worldPosition));
            }
        }

        Debug.Log("Chunks generated in: " + (DateTime.Now - exectime).Milliseconds + " ms");

        OnChunksGenerated += ChunksGenerated;
        OnChunksGenerated.Invoke();
    }

    public Chunk CreateChunk(Vector3Int worldPosition)
    {
        GameObject chunkObj = new GameObject();
        MeshFilter meshFilter = chunkObj.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = chunkObj.AddComponent<MeshRenderer>();
        MeshCollider meshCollider = chunkObj.AddComponent<MeshCollider>();

        chunkObj.transform.parent = transform;
        chunkObj.transform.position = worldPosition;
        chunkObj.transform.localPosition = Vector3.zero;

        chunkObj.name = "Chunk " + worldPosition;

        Chunk currentChunk = chunkObj.AddComponent<Chunk>();
        currentChunk.chunkWorldPos = worldPosition;

        //Fill Chunk Points
        for (int z = worldPosition.z; z < worldPosition.z + chunkSize.z; z++)
        {
            for (int y = worldPosition.y; y < worldPosition.y + chunkSize.y; y++)
            {
                for (int x = worldPosition.x; x < worldPosition.x + chunkSize.x; x++)
                {
                    Vector3Int localCoords = new Vector3Int(x - worldPosition.x, y - worldPosition.y, z - worldPosition.z);

                    if (x < 0 || z < 0 || x > mapSize.x || z > mapSize.z)
                    {
                        continue;
                    }

                    //Debug.Log("Point world coords: x " + x + " y " + y + " z " + z + " local" + localCoords);
                    currentChunk.Points[localCoords.x, localCoords.y, localCoords.z] = Points[x, y, z];
                }
            }
        }

        meshRenderer.material = TerrainMat;
        meshFilter.sharedMesh = GenerateMesh(worldPosition);
        meshCollider.sharedMesh = meshFilter.sharedMesh; //maybe this should be a terrain collider?

        currentChunk.InitMesh();

        return currentChunk;
    }

    /// scale : The scale of the "perlin noise" view
    /// heightMultiplier : The maximum height of the terrain
    /// octaves : Number of iterations (the more there is, the more detailed the terrain will be)
    /// persistance : The higher it is, the rougher the terrain will be (this value should be between 0 and 1 excluded)
    /// lacunarity : The higher it is, the more "feature" the terrain will have (should be strictly positive)
    public static float GetNoiseAt(float x, float z, float scale, float heightMultiplier, int octaves, float persistance, float lacunarity)
    {
        float PerlinValue = 0f;
        float amplitude = 1f;
        float frequency = 1f;

        for (int i = 0; i < octaves; i++)
        {
            PerlinValue += Mathf.PerlinNoise(x * frequency, z * frequency) * amplitude;
            amplitude *= persistance;
            frequency *= lacunarity;
        }

        return PerlinValue * heightMultiplier;
    }

    public static Vector3 GetChunkLocalCoord(float x, float y, float z) => new Vector3(x % chunkSize.x, y, z % chunkSize.z);

    private Mesh GenerateMesh(Vector3 worldPosition)
    {
        Mesh mesh = new Mesh();

        //Vertices and colors
        Vector3[] vertices = new Vector3[(chunkSize.x + 1) * (chunkSize.z + 1)];
        Color[] colors = new Color[(chunkSize.x + 1) * (chunkSize.z + 1)];

        int i = 0;
        for (int d = 0; d <= chunkSize.z; d++)
        {
            for (int w = 0; w <= chunkSize.x; w++)
            {
                int height = heightMap[(int)worldPosition.x + w, (int)worldPosition.z + d];
                float yOffset = Points[(int)worldPosition.x + w, height, (int)worldPosition.z + d].density;
                vertices[i] = new Vector3(worldPosition.x + w, height + (1 - yOffset), worldPosition.z + d);
                colors[i] = new Color(0.3f, 0.3f, 0.3f);
                i++;
            }
        }

        //Triangles
        int[] triangles = new int[chunkSize.x * chunkSize.z * 6];

        for (int d = 0; d < chunkSize.z; d++)
        {
            for (int w = 0; w < chunkSize.x; w++)
            {
                int ti = (d * (chunkSize.x) + w) * 6;

                triangles[ti] = (d * (chunkSize.x + 1)) + w;
                triangles[ti + 1] = ((d + 1) * (chunkSize.x + 1)) + w;
                triangles[ti + 2] = ((d + 1) * (chunkSize.x + 1)) + w + 1;

                triangles[ti + 3] = (d * (chunkSize.x + 1)) + w;
                triangles[ti + 4] = ((d + 1) * (chunkSize.x + 1)) + w + 1;
                triangles[ti + 5] = (d * (chunkSize.x + 1)) + w + 1;
            }
        }

        //UV
        Vector2[] uv = new Vector2[(chunkSize.x + 1) * (chunkSize.z + 1)];

        i = 0;
        for (int d = 0; d <= chunkSize.z; d++)
        {
            for (int w = 0; w <= chunkSize.x; w++)
            {
                uv[i] = new Vector2(w / (float)chunkSize.x, d / (float)chunkSize.z);
                i++;
            }
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.colors = colors;
        mesh.uv = uv;
        mesh.RecalculateTangents();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mesh.Optimize();

        return mesh;
    }

    private void OnDrawGizmos()
    {
        foreach (KeyValuePair<Vector3Int, Chunk> chunk in ChunkCells)
        {
            if (chunk.Value.isActiveAndEnabled)
            {
                GizmoExtension.GizmosExtend.DrawBox(chunk.Value.chunkWorldPos, chunkSize, Quaternion.identity, Color.green);
            }
            else
            {
                GizmoExtension.GizmosExtend.DrawBox(chunk.Value.chunkWorldPos, chunkSize, Quaternion.identity, Color.red);
            }
        }

        GizmoExtension.GizmosExtend.DrawBounds(worldBounds, Color.blue);

        if (visualizeHeightMap && heightMap != null)
        {
            for (int z = 0; z < mapSize.z; z++)
            {
                for (int x = 0; x < mapSize.x; x++)
                {
                    GizmoExtension.GizmosExtend.DrawBox(new Vector3(x, heightMap[x, z], z), Vector3.one * 0.5f, Quaternion.identity, Color.cyan);
                }
            }
        }

        if (visualizePoints && Points != null)
        {
            for (int z = 0; z < mapSize.z; z++)
            {
                for (int x = 0; x < mapSize.x; x++)
                {
                    GizmoExtension.GizmosExtend.DrawBox(new Vector3(x, pointLayer, z), Vector3.one * 0.5f, Quaternion.identity, new Color(Points[x, pointLayer, z].density, Points[x, pointLayer, z].density, Points[x, pointLayer, z].density, 1.0f));
                }
            }
        }
    }
}