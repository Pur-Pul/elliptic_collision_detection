using UnityEngine;
using System;


public class Face
{
    public Vector3 tl;
    public Vector3 tr;
    public Vector3 br;
    public Vector3 bl;
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

public class BBox
{
    public float width;
    public Vector3 position;
    public BBox(Vector3 pos, float w)
    {
        position = pos;
        width = w;
    }

    public Face[] GetAABBFaces()
    {
        float max_x = position.x + width/2f;
        float max_y = position.y + width/2f;
        float max_z = position.z + width/2f;
        float min_x = position.x - width/2f;
        float min_y = position.y - width/2f;
        float min_z = position.z - width/2f;
        Vector3[] _vertices =
        {
            new Vector3(min_x, min_y, min_z), //left    top     front
            new Vector3(max_x, min_y, min_z), //right   top     front
            new Vector3(min_x, max_y, min_z), //left    bottom  front
            new Vector3(max_x, max_y, min_z), //right   bottom  front
            new Vector3(min_x, min_y, max_z), //left    top     back
            new Vector3(max_x, min_y, max_z), //right   top     back
            new Vector3(min_x, max_y, max_z), //left    bottom  back
            new Vector3(max_x, max_y, max_z)  //right   bottom  back
        };

        Face[] _faces =
        {
            new Face(_vertices[0], _vertices[1], _vertices[3], _vertices[2]),   //front
            new Face(_vertices[4], _vertices[0], _vertices[2], _vertices[6]),   //left
            new Face(_vertices[1], _vertices[5], _vertices[7], _vertices[3]),  //right
            new Face(_vertices[4], _vertices[5], _vertices[1], _vertices[0]),   //top
            new Face(_vertices[2], _vertices[3], _vertices[7], _vertices[6]),  //bottom
            new Face(_vertices[5], _vertices[4], _vertices[6], _vertices[7])  //back
        };

        return _faces;
    }

    public Vector3 MinVec()
    {
        return new Vector3(
            position.x - width/2f,
            position.y - width/2f,
            position.z - width/2f
        );
    }

    public Vector3 MaxVec()
    {
        return new Vector3(
            position.x + width/2f,
            position.y + width/2f,
            position.z + width/2f
        );
    }

    public bool CheckContains(BBox other)
    {
        return (
			position.x + width/2f >= other.position.x + other.width/2f &&
            position.y + width/2f >= other.position.y + other.width/2f &&
            position.z + width/2f >= other.position.z + other.width/2f &&
            position.x - width/2f <= other.position.x - other.width/2f &&
            position.y - width/2f <= other.position.y - other.width/2f &&
            position.z - width/2f <= other.position.z - other.width/2f
		);
    }

    public virtual bool CheckAABB(BBox other)
    {
        return !(
			position.x + width/2f <= other.position.x - other.width/2f || position.x - width/2f >= other.position.x + other.width/2f ||
            position.y + width/2f <= other.position.y - other.width/2f || position.y - width/2f >= other.position.y + other.width/2f ||
            position.z + width/2f <= other.position.z - other.width/2f || position.z - width/2f >= other.position.z + other.width/2f
		);
    }

    public virtual Vector3 CheckPoint (Vector3 point)
    {
        float max_x = position.x + width/2f;
        float max_y = position.y + width/2f;
        float max_z = position.z + width/2f;
        float min_x = position.x - width/2f;
        float min_y = position.y - width/2f;
        float min_z = position.z - width/2f;

        float overlapx = Math.Min(max_x, point.x) - Math.Max(min_x, point.x);
        float overlapy = Math.Min(max_y, point.y) - Math.Max(min_y, point.y);
        float overlapz = Math.Min(max_z, point.z) - Math.Max(min_z, point.z);

        if (overlapx <= overlapy && overlapx <= overlapz)
        {
            return new Vector3(position.x < point.x ? -overlapx : overlapx, 0, 0);
        }
        else if (overlapy <= overlapx && overlapy <= overlapz)
        {
            return new Vector3(0, position.y < point.y ? -overlapy : overlapy, 0);   
        }
        else
        {
            return new Vector3(0, 0, position.z < point.z ? -overlapz : overlapz);   
        }
    }
	
	public virtual bool CheckSphere (BBoxSphere sphere)
    {
        return false;
    }
	
	public virtual bool CheckQuadrilateral (BBoxQuadrilateral quad)
    {
        return false;
    }

    public bool CheckCollision(BBox bbox)
    {
        return bbox switch
        {
            BBoxSphere sphere => CheckSphere(sphere),
            BBoxQuadrilateral quad => CheckQuadrilateral(quad),
            BBox aabb => CheckAABB(aabb),
            _ => false,
        };
    }
}

public class BBoxSphere : BBox
{
    public float radius;
    public float radius2;
    public BBoxSphere(Vector3 pos, float r) : base(pos, r*2f)
    {
        radius = r;
        radius2 = (float)Math.Pow(r, 2);
    }
    public override bool CheckAABB(BBox aabb)
    // solid Sphere - solid AABB collision check method by Jim Arvo, in "Graphics Gems", Academic Press, 1990.
    // https://web.archive.org/web/20100323053111/http://www.ics.uci.edu/~arvo/code/BoxSphereIntersect.c
    {
        Vector3 aabb_min = aabb.MinVec();
        Vector3 aabb_max = aabb.MaxVec();
        float dmin = 0;
        if (position.x < aabb_min.x)        { dmin += (float)Math.Pow(position.x - aabb_min.x, 2); }
        else if (position.x > aabb_max.x)   { dmin += (float)Math.Pow(position.x - aabb_max.x, 2); }

        if (position.y < aabb_min.y)        { dmin += (float)Math.Pow(position.x - aabb_min.y, 2); }
        else if (position.y > aabb_max.y)   { dmin += (float)Math.Pow(position.x - aabb_max.y, 2); }

        if (position.z < aabb_min.z)        { dmin += (float)Math.Pow(position.x - aabb_min.z, 2); }
        else if (position.z > aabb_max.z)   { dmin += (float)Math.Pow(position.x - aabb_max.z, 2); }
        return dmin <= radius2;
    }
    public override bool CheckSphere(BBoxSphere sphere)
    {
        float centerDist = radius + sphere.radius;
        return (position - sphere.position).sqrMagnitude < centerDist * centerDist;
    }
}

public class BBoxQuadrilateral : BBox
{
    public float radius;
    public BBoxQuadrilateral(Vector3 pos, float width) : base(pos, width)
    {
        
    }
}