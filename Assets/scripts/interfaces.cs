using System.Collections.Generic;
using UnityEngine;

public enum BoundingType
{
    OBB,
    BC,
    AABB
}


public interface IBoundingVolume
{
    public Vector3 Position { get; set; }
    public float Width { get; set; }
    public SpeedRecord Record { get; set; }
    public BoundingType Btype { get; }
    public bool CheckCollision(IBoundingVolume other);
    public bool CheckContains(IBoundingVolume other);
    public bool CheckFastOverlaps(IBoundingVolume other);
}

public interface IItem
{
    IBoundingVolume BBox { get; }
    public int Id { get; }
}

public interface ICollisionTree : IBoundingVolume
{
    public bool Add(IItem item);
    public bool Remove(IItem item);
    public List<IItem> CheckCollisions(IItem item);
    public Vector3[][] GetTreeEdges();
}