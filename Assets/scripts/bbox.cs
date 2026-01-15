using UnityEngine;
using System;

public class BBox
{
    public float width;
    public Vector3 position;
    public string group;
    public BBox(Vector3 pos, float w)
    {
        position = pos;
        width = w;
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

    public bool CheckOverlaps(BBox other)
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

    public virtual Vector3 CheckAABB (BBox other) {
        if (CheckOverlaps(other) != true)
        {
            return Vector3.zero;
        }
        float max_x = position.x + width/2f;
        float max_y = position.y + width/2f;
        float max_z = position.z + width/2f;
        
        float other_max_x = other.position.x + other.width/2f;
        float other_max_y = other.position.y + other.width/2f;
        float other_max_z = other.position.z + other.width/2f;
        
        float min_x = position.x - width/2f;
        float min_y = position.y - width/2f;
        float min_z = position.z - width/2f;
        
        float other_min_x = other.position.x - other.width/2f;
        float other_min_y = other.position.y - other.width/2f;
        float other_min_z = other.position.z - other.width/2f;

        float overlapx = Math.Min(max_x, other_max_x) - Math.Max(min_x, other_min_x);
        float overlapy = Math.Min(max_y, other_max_y) - Math.Max(min_y, other_min_y);
        float overlapz = Math.Min(max_z, other_max_z) - Math.Max(min_z, other_min_z);

        if (overlapx <= overlapy && overlapx <= overlapz)
        {
            return new Vector3(position.x < other.position.x ? -overlapx : overlapx, 0, 0);
        }
        else if (overlapy <= overlapx && overlapy <= overlapz)
        {
            return new Vector3(0, position.y < other.position.y ? -overlapy : overlapy, 0);   
        }
        else
        {
            return new Vector3(0, 0, position.z < other.position.z ? -overlapz : overlapz);   
        }
    }
	
	public virtual Vector3 CheckSphere (BBoxSphere sphere) { return Vector3.zero; }
	
	public virtual Vector3 CheckQuadrilateral (BBoxQuadrilateral quad) { return Vector3.zero; }

    public Vector3 CheckCollision(BBox bbox)
    {
		if (group == "all" || bbox.group == "all" || group != bbox.group)
        {
			switch (bbox) {
				case BBoxSphere sphere:
					return CheckSphere(sphere);
				case BBoxQuadrilateral quad:
					return CheckQuadrilateral(quad);
                case BBox aabb:
					return CheckAABB(aabb);
				default:
					return Vector3.zero;
			}
		} else {
			return Vector3.zero;
		}
	}
}

public class BBoxSphere : BBox
{
    public float radius;
    public BBoxSphere(Vector3 pos, float r) : base(pos, r*2f)
    {
        radius = r;
    }
    /*
    public override Vector3 CheckAABB(BBox aabb)
    {
        aabb.CheckPoint(position);
        CheckPoint(aabb.position);

        Vector3 dir = aabb.position - position;
        if (dir.magnitude < radius)
        {
            
        }
        dir.Normalize();
        dir *= radius;
        CheckPoint(position+dir);
    }
    */
}

public class BBoxQuadrilateral : BBox
{
    public float radius;
    public BBoxQuadrilateral(Vector3 pos, float width) : base(pos, width)
    {
        
    }
}