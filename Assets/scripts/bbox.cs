using UnityEngine;
using System;



public class Face
{
    public Vector3 tl;
    public Vector3 tr;
    public Vector3 br;
    public Vector3 bl;
    
    public Vector3[][] Edges {
        get => new[] {
            new[] { tl, tr },
            new[] { tr, br },
            new[] { br, bl },
            new[] { bl, tl }
        };
    }
    public Face(Vector3 top_left, Vector3 top_right, Vector3 bottom_right, Vector3 bottom_left)
    {
        tl = top_left;
        tr = top_right;
        br = bottom_right;
        bl = bottom_left;
    }
    public Vector3[] GetVertices()
    {
        Vector3[] _verts = { tl, tr, br, bl };
        return _verts;
    }
}

public class BBox : IBoundingVolume
{
    private float width;
    public virtual float Width 
    { 
        get => width;
        set 
        {
            max_x = null;
            max_y = null;
            max_z = null;
            min_x = null;
            min_y = null;
            min_z = null;
            width = value;
        }
    }
    private Vector3 position;
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
    private float? max_x;
    private float? max_y;
    private float? max_z;
    private float? min_x;
    private float? min_y;
    private float? min_z;

    public float MaxX
    {
        get => max_x ??= Position.x + Width/2f;
    }
    public float MaxY
    {
        get => max_y ??= Position.y + Width/2f;
    }
    public float MaxZ
    {
        get => max_z ??= Position.z + Width/2f;
    }
    public float MinX
    {
        get => min_x ??= Position.x - Width/2f;
    }
    public float MinY
    {
        get => min_y ??= Position.y - Width/2f;
    }
    public float MinZ
    {
        get => min_z ??= Position.z - Width/2f;
    }

    public Vector3[][] GetAABBEdges()
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

    public bool CheckContains(IBoundingVolume other)
    {
        return other switch
        {
            BBox bbox => CheckContains(bbox),
            _ => false
        };
    }

    public bool CheckContains(BBox other)
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

    public bool CheckFastOverlaps(IBoundingVolume other)
    {
        return other switch
        {
            BBox bbox => CheckFastOverlaps(bbox),
            _ => false
        };
    }

    public virtual bool CheckFastOverlaps(BBox other)
    {
        return !(
            MaxX <= other.MinX || MinX >= other.MaxX ||
            MaxY <= other.MinY || MinY >= other.MaxY ||
            MaxZ <= other.MinZ || MinZ >= other.MaxZ
		);
    }

    public virtual Vector3 CheckPoint (Vector3 point)
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
	
	public virtual bool CheckSphere (BBoxSphere sphere)
    {
        return false;
    }
    
    public bool CheckCollision(IBoundingVolume other)
    {
        return other switch
        {
            BBoxSphere sphere => CheckSphere(sphere),
            BBox aabb => CheckFastOverlaps(aabb),
            _ => false
        };
    }
}

public class BBoxSphere : BBox
{   
    private float radius;
    private float radius2;
    public float Radius { get => radius; set { radius = value; } }
    public override float Width
    {
        get => base.Width;
        set
        {
            base.Width = value;
            Radius = value/2f;
            radius2 = Radius*Radius;
        }
    }
    public override bool CheckFastOverlaps(BBox aabb)
    // solid Sphere - solid AABB collision check method by Jim Arvo, in "Graphics Gems", Academic Press, 1990.
    // https://web.archive.org/web/20100323053111/http://www.ics.uci.edu/~arvo/code/BoxSphereIntersect.c
    {
        float dmin = 0;
        if (Position.x < aabb.MinX)        { dmin += (float)Math.Pow(Position.x - aabb.MinX, 2); }
        else if (Position.x > aabb.MaxX)   { dmin += (float)Math.Pow(Position.x - aabb.MaxX, 2); }

        if (Position.y < aabb.MinY)        { dmin += (float)Math.Pow(Position.y - aabb.MinY, 2); }
        else if (Position.y > aabb.MaxY)   { dmin += (float)Math.Pow(Position.y - aabb.MaxY, 2); }

        if (Position.z < aabb.MinZ)        { dmin += (float)Math.Pow(Position.z - aabb.MinZ, 2); }
        else if (Position.z > aabb.MaxZ)   { dmin += (float)Math.Pow(Position.z - aabb.MaxZ, 2); }
        return dmin <= radius2;
    }
    public override bool CheckSphere(BBoxSphere sphere)
    {
        float centerDist = Radius + sphere.Radius;
        return (Position - sphere.Position).sqrMagnitude < centerDist * centerDist;
    }
}

public class OBBox : BBox
{
    public Vector3 Right { get; set; }
    public Vector3 Up { get; set; }
    public Vector3 Forward { get; set; }
    float RightWidth { get; set; }
    float UpWidth { get; set; }
    float ForwardWidth { get; set; }

    //https://dev.to/pratyush_mohanty_6b8f2749/the-math-behind-bounding-box-collision-detection-aabb-vs-obbseparate-axis-theorem-1gdn
    bool SATAxis(OBBox bbox, Vector3 axis, float scalar)
    {
        float left = MathF.Abs(Vector3.Dot(bbox.Position - Position, axis));
        
        float right = scalar + 
        MathF.Abs(Vector3.Dot(bbox.RightWidth * bbox.Right, axis)) +
        MathF.Abs(Vector3.Dot(bbox.UpWidth * bbox.Up, axis)) + 
        MathF.Abs(Vector3.Dot(bbox.ForwardWidth * bbox.Forward, axis));

        return left <= right;
    }
    
    bool SAT(OBBox bbox)
    {
        return SATAxis(bbox, Right, RightWidth) || SATAxis(bbox, Up, UpWidth) || SATAxis(bbox, Forward, ForwardWidth);
    }
}