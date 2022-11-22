using System;
using System.Collections.Generic;
using Unity.AI.Navigation;
using Unity.VisualScripting;
using UnityEngine;

public class NavMeshManager : Singleton<NavMeshManager>
{
    public Dictionary<Chunk, NavMeshSurface> navMeshSurfaces;

    public NavMeshSurface surfaceForCloning;

    public void Awake()
    {
        MapGen.instance.OnChunksGenerated += Initialize;
    }

    public void Initialize()
    {
        DateTime exectime = DateTime.Now;
        Debug.Log("NavMesh Initialization");
        InitializeNavMeshSurfaces();
        InitializeNavMeshes();
        Debug.Log("NavMesh generated in: " + (DateTime.Now - exectime).Milliseconds + " ms");
    }

    private void InitializeNavMeshSurfaces()
    {
        navMeshSurfaces = new Dictionary<Chunk, NavMeshSurface>(MapGen.instance.ChunkCells.Count);

        foreach (KeyValuePair<Vector3Int, Chunk> chunk in MapGen.instance.ChunkCells)
        {
            navMeshSurfaces.Add(chunk.Value, chunk.Value.AddComponent<NavMeshSurface>());
            CopyNavMeshComponents(surfaceForCloning, navMeshSurfaces[chunk.Value]);
        }
    }

    private void CopyNavMeshComponents(NavMeshSurface surfaceForCloning, NavMeshSurface navMeshSurface)
    {
        //throw new NotImplementedException();
    }

    private void InitializeNavMeshes()
    {
        foreach (KeyValuePair<Chunk, NavMeshSurface> navMeshSurface in navMeshSurfaces)
        {
            navMeshSurface.Value.BuildNavMesh();
        }
    }
}
