using UnityEngine;
using System;
using System.Diagnostics;

public class AABB : ISimpleBoundingVolume
{
    private Vector3 size;
    
    public RuntimeRecord Record { get; set; }
    public virtual Vector3 Right { get => Vector3.right; }
    public virtual Vector3 Up { get => Vector3.up; }
    public virtual Vector3 Forward { get => Vector3.forward; }
    private string typeName = "";
    private int?[] methodIds;
    public AABB() {
        typeName = GetType().Name;
        methodIds = new int?[2];
    }

    public Vector3 Position { get; set; }
    public Vector3 HalfSize;
    public Vector3 Size {
        get => size;
        set
        {
            size = value;
            HalfSize = value*0.5f;
        }
    }

    public Vector3[][] GetEdges()
    {
        Vector3 Max = Position + HalfSize;
        Vector3 Min = Position - HalfSize;
        Vector3[] _vertices =
        {
            new (Min.x, Min.y, Min.z), //left    top     front
            new (Max.x, Min.y, Min.z), //right   top     front
            new (Min.x, Max.y, Min.z), //left    bottom  front
            new (Max.x, Max.y, Min.z), //right   bottom  front
            new (Min.x, Min.y, Max.z), //left    top     back
            new (Max.x, Min.y, Max.z), //right   top     back
            new (Min.x, Max.y, Max.z), //left    bottom  back
            new (Max.x, Max.y, Max.z)  //right   bottom  back
        };

        Face[] _faces =
        {
            new (_vertices[0], _vertices[1], _vertices[3], _vertices[2]),   //front
            new (_vertices[4], _vertices[0], _vertices[2], _vertices[6]),   //left
            new (_vertices[1], _vertices[5], _vertices[7], _vertices[3]),   //right
            new (_vertices[4], _vertices[5], _vertices[1], _vertices[0]),   //top
            new (_vertices[2], _vertices[3], _vertices[7], _vertices[6]),   //bottom
            new (_vertices[5], _vertices[4], _vertices[6], _vertices[7])    //back
        };
        Vector3[][] _edges = new Vector3[24][];
        for (int i = 0; i < _faces.Length; i++)
        {
            Vector3[][] _face_edges = _faces[i].Edges;
            _edges[i*4] = _face_edges[0];
            _edges[i*4+1] = _face_edges[1];
            _edges[i*4+2] = _face_edges[2];
            _edges[i*4+3] = _face_edges[3];
        }
        return _edges;
    }

    T Timed<T> (Func<T> func, int id)
    {
        long start = Stopwatch.GetTimestamp();
        T result = func();
        long end = Stopwatch.GetTimestamp();

        Record.Write(start, end, id);

        return result;
    }

    public bool SimpleContains(ISimpleBoundingVolume other)
    {
        return other switch
        {
            AABB aabb => Timed(() => Contains(aabb), methodIds[0] ??= Record.GetId(typeName, nameof(Contains))),
            _ => false
        };
    }

    public bool Contains (AABB other)
    {
        return !(
            Mathf.Abs(Position.x - other.Position.x) >= HalfSize.x - other.HalfSize.x ||
            Mathf.Abs(Position.y - other.Position.y) >= HalfSize.y - other.HalfSize.y ||
            Mathf.Abs(Position.z - other.Position.z) >= HalfSize.z - other.HalfSize.z
        ); 
    }

    public bool SimpleIntersects(ISimpleBoundingVolume other)
    {
        return other switch
        {
            AABB aabb => Timed(() => Intersects(aabb), methodIds[1] ??= Record.GetId(typeName, nameof(Intersects))),
            _ => false
        };
    }

    public virtual bool Intersects(AABB other)
    {
        return !(
            Mathf.Abs(Position.x - other.Position.x) > HalfSize.x + other.HalfSize.x ||
            Mathf.Abs(Position.y - other.Position.y) > HalfSize.y + other.HalfSize.y ||
            Mathf.Abs(Position.z - other.Position.z) > HalfSize.z + other.HalfSize.z
        ); 
    }
}