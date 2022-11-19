using UnityEngine;

public struct Point
{
    public Vector3 worldPosition;
    public float density;
    public Color color;

    public Point(Vector3 worldPosition, float density, Color color)
    {
        this.worldPosition = worldPosition;
        this.density = density;
        this.color = color;
    }
}