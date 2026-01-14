using System.Numerics;

class BBox
{
    public float width;
    public Vector3 position;
    public string group;
    public BBox(Vector3 pos, float w)
    {
        position = pos;
        width = w;
        position = Vector3.Zero;
    }
    public bool CheckContains(BBox aabb)
    {
        return (
			position.X + width/2f <= aabb.position.X + width/2f &&
            position.Y + width/2f <= aabb.position.Y + width/2f &&
            position.Z + width/2f <= aabb.position.Z + width/2f &&
            position.X - width/2f >= aabb.position.X - width/2f &&
            position.Y - width/2f >= aabb.position.Y - width/2f &&
            position.Z - width/2f >= aabb.position.Z - width/2f
		);
    }

    Vector3 CheckAABB (BBox aabb) { return Vector3.Zero; }
	
	Vector3 CheckSphere (BBoxSphere sphere) { return Vector3.Zero; }
	
	Vector3 CheckQuadrilateral (BBoxQuadrilateral quad) { return Vector3.Zero; }

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
					return Vector3.Zero;
			}
		} else {
			return Vector3.Zero;
		}
	}
}

class BBoxSphere : BBox
{
    public float radius;
    BBoxSphere(Vector3 pos, float r) : base(pos, r*2f)
    {
        radius = r;
    }
}

class BBoxQuadrilateral : BBox
{
    public float radius;
    BBoxQuadrilateral(Vector3 pos, float width) : base(pos, width)
    {
        
    }
}