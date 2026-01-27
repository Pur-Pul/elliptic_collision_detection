using UnityEngine;

public interface IBoundingVolume
{
    public Vector3 Position { get; set; }
    public float Width { get; set; }
    public bool CheckCollision(IBoundingVolume other);
    public bool CheckContains(IBoundingVolume other);
    public bool CheckFastOverlaps(IBoundingVolume other);
}

public interface IItem
{
    IBoundingVolume BBox { get; }
}

public interface ICollisionTree : IBoundingVolume
{
    public bool Add(IItem item);
    public bool Remove(IItem item);
    public bool CheckCollisions(IItem item);
    public Vector3[][] GetTreeEdges();
    public void Draw(Mesh mesh);
}