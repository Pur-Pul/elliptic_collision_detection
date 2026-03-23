using UnityEngine;
using System;

public class AABB : ISimpleBoundingVolume
{
    private Vector3 position;
    private Vector3 size;

    private float? max_x;
    private float? max_y;
    private float? max_z;
    private float? min_x;
    private float? min_y;
    private float? min_z;
    public RuntimeRecord Record { get; set; }
    public virtual Vector3 Right { get => Vector3.right; }
    public virtual Vector3 Up { get => Vector3.up; }
    public virtual Vector3 Forward { get => Vector3.forward; }

    public Vector3 Position 
    {
        get => position;
        set
        {
            max_x = null;
            max_y = null;
            max_z = null;
            min_x = null;
            min_y = null;
            min_z = null;
            position = value;
        }
    }
    public Vector3 Size {
        get => size;
        set
        {
            if (size.x != value.x)
            {
                max_x = null;
                min_x = null;
            }
            if (size.y != value.y)
            {
                max_y = null;
                min_y = null;
            }
            if (size.z != value.z)
            {
                max_z = null;
                min_z = null;
            }
            size = value;
        }
    }

    public float MaxX
    {
        get => max_x ??= Position.x + Size.x/2f;
    }
    public float MaxY
    {
        get => max_y ??= Position.y + Size.y/2f;
    }
    public float MaxZ
    {
        get => max_z ??= Position.z + Size.z/2f;
    }
    public float MinX
    {
        get => min_x ??= Position.x - Size.x/2f;
    }
    public float MinY
    {
        get => min_y ??= Position.y - Size.y/2f;
    }
    public float MinZ
    {
        get => min_z ??= Position.z - Size.z/2f;
    }

    public Vector3[][] GetEdges()
    {
        Vector3[] _vertices =
        {
            new (MinX, MinY, MinZ), //left    top     front
            new (MaxX, MinY, MinZ), //right   top     front
            new (MinX, MaxY, MinZ), //left    bottom  front
            new (MaxX, MaxY, MinZ), //right   bottom  front
            new (MinX, MinY, MaxZ), //left    top     back
            new (MaxX, MinY, MaxZ), //right   top     back
            new (MinX, MaxY, MaxZ), //left    bottom  back
            new (MaxX, MaxY, MaxZ)  //right   bottom  back
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

    public bool SimpleContains(ISimpleBoundingVolume other)
    {
        return other switch
        {
            AABB aabb => Contains(aabb),
            _ => false
        };
    }

    public bool Contains (AABB other)
    {
        return (
            MaxX >= other.MaxX &&
            MaxY >= other.MaxY &&
            MaxZ >= other.MaxZ &&
            MinX <= other.MinX &&
            MinY <= other.MinY &&
            MinZ <= other.MinZ
		);
    }

    public bool SimpleIntersects(ISimpleBoundingVolume other)
    {
        return other switch
        {
            AABB aabb => Intersects(aabb),
            _ => false
        };
    }

    public virtual bool Intersects(AABB other)
    {
        return !(
            MaxX < other.MinX || MinX > other.MaxX ||
            MaxY < other.MinY || MinY > other.MaxY ||
            MaxZ < other.MinZ || MinZ > other.MaxZ
		);
    }

    public virtual Vector3 ContainsPoint (Vector3 point)
    {
        float overlapx = Math.Min(MaxX, point.x) - Math.Max(MinX, point.x);
        float overlapy = Math.Min(MaxY, point.y) - Math.Max(MinY, point.y);
        float overlapz = Math.Min(MaxZ, point.z) - Math.Max(MinZ, point.z);

        if (overlapx <= overlapy && overlapx <= overlapz)
        {
            return new Vector3(Position.x < point.x ? -overlapx : overlapx, 0, 0);
        }
        else if (overlapy <= overlapx && overlapy <= overlapz)
        {
            return new Vector3(0, Position.y < point.y ? -overlapy : overlapy, 0);   
        }
        else
        {
            return new Vector3(0, 0, Position.z < point.z ? -overlapz : overlapz);   
        }
    }
}