using System;
using System.Collections.Generic;
using UnityEngine;

public interface ISimpleBoundingVolume
{
    public Vector3 Position { get; set; }
    public RuntimeRecord Record { get; set; }
    public Vector3 Size { get; set; }
    public bool SimpleContains(ISimpleBoundingVolume other);
    public bool SimpleIntersects(ISimpleBoundingVolume other);
    public Vector3[][] GetEdges();
}

public interface IBoundingVolume
{
    public Vector3 Position { get; set; }
    public RuntimeRecord Record { get; set; }
    public Vector3 Size { get; set; }
    public ISimpleBoundingVolume Simple { get; set; }
    public bool CheckCollision(IBoundingVolume other);
    public bool CheckFastOverlaps(IBoundingVolume other);
}
public interface IItem
{
    IBoundingVolume BBox { get; }
    public int Id { get; }
}

public interface ICollisionTree
{
    public Vector3 Position { get; set; }
    public RuntimeRecord Record { get; set; }
    public Vector3 Size { get; set; }
    public void Clear();
    public bool Add(IItem item);
    public bool Remove(IItem item);
    public int MaxItems { get; set; }
    public int MaxDepth { get; set; }
    public List<IItem> CheckCollisions(IItem item);
    public Vector3[][] GetTreeEdges();
}