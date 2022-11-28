using System;
using System.Collections.Generic;
using Unity.AI.Navigation;
using Unity.VisualScripting;
//using UnityEditor;
using UnityEngine;

public class NavMeshManager : Singleton<NavMeshManager>
{
    public Dictionary<Chunk, NavMeshSurface> navMeshSurfaces;

    public bool generateNavMeshSurfaces = true;
    public NavMeshSurface surfaceForCloning;

    public override void Awake()
    {
        if (generateNavMeshSurfaces) MapGen.instance.OnChunksGenerated += Init;
    }

    public void Init()
    {
        DateTime exectime = DateTime.Now;
        Debug.Log("NavMesh Initialization");

        navMeshSurfaces = new Dictionary<Chunk, NavMeshSurface>(MapGen.instance.ChunkCells.Count);

        foreach (KeyValuePair<Vector3Int, Chunk> chunk in MapGen.instance.ChunkCells)
        {
            navMeshSurfaces.Add(chunk.Value, chunk.Value.AddComponent<NavMeshSurface>());
            //This code below works only in Editor! I keep it for later use.
            //EditorUtility.CopySerializedManagedFieldsOnly(surfaceForCloning, navMeshSurfaces[chunk.Value]);
            StaticUtils.GetCopyOf(navMeshSurfaces[chunk.Value], surfaceForCloning);
            navMeshSurfaces[chunk.Value].BuildNavMesh();
        }

        Debug.Log("NavMesh generated in: " + (DateTime.Now - exectime).Milliseconds + " ms");
    }
    public NavMeshSurface GetNavMeshSurfaceOfChunk(Chunk chunk) => navMeshSurfaces[chunk];
}
