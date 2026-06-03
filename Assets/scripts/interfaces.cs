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

public interface ISimpleSphericalBoundingVolume
{
    public Vector2 Position { get; set; }
    public RuntimeRecord Record { get; set; }
    public Vector2 Size { get; set; }
    public bool SimpleContains(ISimpleSphericalBoundingVolume other);
    public bool SimpleIntersects(ISimpleSphericalBoundingVolume other);
}

public interface IBoundingVolume
{
    public int Id { get; }
    public Vector3 Position { get; set; }
    public RuntimeRecord Record { get; set; }
    public Vector3 Size { get; set; }
    public Vector3 Right { get; set; }
    public Vector3 Up { get; set; }
    public Vector3 Forward { get; set; }
    public ISimpleBoundingVolume Simple { get; set; }
    public void Update(Vector3 _size, Quaternion _orientation, bool timed);
    public void UpdateSimpleSize();
    public bool CheckCollision(IBoundingVolume other);
    public bool CheckFastOverlaps(IBoundingVolume other);
}
public interface IItem
{
    IBoundingVolume BBox { get; }
    public string BodyType { get; }
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