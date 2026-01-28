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
    public virtual float Width { get; set; }
    public Vector3 Position { get; set; }

    public Vector3[][] GetAABBEdges()
    {
        float max_x = Position.x + Width/2f;
        float max_y = Position.y + Width/2f;
        float max_z = Position.z + Width/2f;
        float min_x = Position.x - Width/2f;
        float min_y = Position.y - Width/2f;
        float min_z = Position.z - Width/2f;
        Vector3[] _vertices =
        {
            new (min_x, min_y, min_z), //left    top     front
            new (max_x, min_y, min_z), //right   top     front
            new (min_x, max_y, min_z), //left    bottom  front
            new (max_x, max_y, min_z), //right   bottom  front
            new (min_x, min_y, max_z), //left    top     back
            new (max_x, min_y, max_z), //right   top     back
            new (min_x, max_y, max_z), //left    bottom  back
            new (max_x, max_y, max_z)  //right   bottom  back
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

    public Vector3 MinVec()
    {
        return new Vector3(
            Position.x - Width/2f,
            Position.y - Width/2f,
            Position.z - Width/2f
        );
    }

    public Vector3 MaxVec()
    {
        return new Vector3(
            Position.x + Width/2f,
            Position.y + Width/2f,
            Position.z + Width/2f
        );
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
			Position.x + Width/2f >= other.Position.x + other.Width/2f &&
            Position.y + Width/2f >= other.Position.y + other.Width/2f &&
            Position.z + Width/2f >= other.Position.z + other.Width/2f &&
            Position.x - Width/2f <= other.Position.x - other.Width/2f &&
            Position.y - Width/2f <= other.Position.y - other.Width/2f &&
            Position.z - Width/2f <= other.Position.z - other.Width/2f
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
			Position.x + Width/2f <= other.Position.x - other.Width/2f || Position.x - Width/2f >= other.Position.x + other.Width/2f ||
            Position.y + Width/2f <= other.Position.y - other.Width/2f || Position.y - Width/2f >= other.Position.y + other.Width/2f ||
            Position.z + Width/2f <= other.Position.z - other.Width/2f || Position.z - Width/2f >= other.Position.z + other.Width/2f
		);
    }

    public virtual Vector3 CheckPoint (Vector3 point)
    {
        float max_x = Position.x + Width/2f;
        float max_y = Position.y + Width/2f;
        float max_z = Position.z + Width/2f;
        float min_x = Position.x - Width/2f;
        float min_y = Position.y - Width/2f;
        float min_z = Position.z - Width/2f;

        float overlapx = Math.Min(max_x, point.x) - Math.Max(min_x, point.x);
        float overlapy = Math.Min(max_y, point.y) - Math.Max(min_y, point.y);
        float overlapz = Math.Min(max_z, point.z) - Math.Max(min_z, point.z);

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
        get => radius * 2f;
        set
        {
            Radius = value/2f;
            radius2 = Radius*Radius;
        }
    }
    public override bool CheckFastOverlaps(BBox aabb)
    // solid Sphere - solid AABB collision check method by Jim Arvo, in "Graphics Gems", Academic Press, 1990.
    // https://web.archive.org/web/20100323053111/http://www.ics.uci.edu/~arvo/code/BoxSphereIntersect.c
    {
        Vector3 aabb_min = aabb.MinVec();
        Vector3 aabb_max = aabb.MaxVec();
        float dmin = 0;
        if (Position.x < aabb_min.x)        { dmin += (float)Math.Pow(Position.x - aabb_min.x, 2); }
        else if (Position.x > aabb_max.x)   { dmin += (float)Math.Pow(Position.x - aabb_max.x, 2); }

        if (Position.y < aabb_min.y)        { dmin += (float)Math.Pow(Position.x - aabb_min.y, 2); }
        else if (Position.y > aabb_max.y)   { dmin += (float)Math.Pow(Position.x - aabb_max.y, 2); }

        if (Position.z < aabb_min.z)        { dmin += (float)Math.Pow(Position.x - aabb_min.z, 2); }
        else if (Position.z > aabb_max.z)   { dmin += (float)Math.Pow(Position.x - aabb_max.z, 2); }
        return dmin <= radius2;
    }
    public override bool CheckSphere(BBoxSphere sphere)
    {
        float centerDist = Radius + sphere.Radius;
        return (Position - sphere.Position).sqrMagnitude < centerDist * centerDist;
    }
}