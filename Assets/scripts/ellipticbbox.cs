using UnityEngine;

public class EllipticBBox
{
    float eRad;
    Vector3 pos;
    public EllipticBBox(Vector3 position, float ellipticRadius)
    {
        eRad = ellipticRadius;
        pos = position;
    }

    public bool CheckOverlaps(EllipticBBox bbox)
    {
        float centerDist = EllipticDistance(pos, bbox.pos);
        return centerDist < bbox.eRad + eRad;
    }

    public bool CheckContains(EllipticBBox bbox)
    {
        float centerDist = EllipticDistance(pos, bbox.pos);
        return centerDist + bbox.eRad < eRad;
    }

    public static float EllipticDistance(Vector3 a, Vector3 b)
    {
        float d = Vector3.Dot(a, b);
        return (d - 1)/(-2f);
    }
    public bool CheckCollision(EllipticBBox bbox)
    {
        return CheckOverlaps(bbox);
        //return bbox switch
        //{
        //    BBoxSphere sphere => CheckSphere(sphere),
        //    BBoxQuadrilateral quad => CheckQuadrilateral(quad),
        //    BBox aabb => CheckAABB(aabb),
        //    _ => false,
        //};
    }
}

public class EllipticTriangle
{
    private Vector3? inCenter;
    private float? inRadius;
    private float? length12;
    private float? length23;
    private float? length31;
    private Vector3 c1;
    private Vector3 c2;
    private Vector3 c3;

    public Vector3 C1
    {
        get => c1; set
        {
            if (c1 != value)
            {
                c1 = value;
                inCenter = null;
                inRadius = null;
                length12 = null;
                length31 = null;
            }
        }
    }
    public Vector3 C2
    {
        get => c2; set
        {
            if (c2 != value)
            {
                c2 = value;
                inCenter = null;
                inRadius = null;
                length12 = null;
                length23 = null;
            }
        }
    }
    public Vector3 C3
    {
        get => c3; set
        {
            if (c3 != value)
            {
                c3 = value;
                inCenter = null;
                inRadius = null;
                length12 = null;
                length23 = null;
            }
        }
    }
    
    public float Length12 
    { 
        get => length12 ??= (C1 - C2).magnitude;
    }
    public float Length23
    { 
        get => length23 ??= (C2 - C3).magnitude;
    }
    public float Length31
    { 
        get => length31 ??= (C3 - C1).magnitude;
    }

    public Vector3[] Edges
    {
        get => new[] { C1, C2, C3, C1 };
    }

    public Vector3 InCenter => inCenter ??= ComputeEllipticInCenter();
    public float InRadius => inRadius ??= ComputeEllipticInRadius();

    public EllipticTriangle[] Subdivide()
    {
        Vector3 c1half = Vector3.Slerp(C1, C2, 0.5f);
        Vector3 c2half = Vector3.Slerp(C2, C3, 0.5f);
        Vector3 c3half = Vector3.Slerp(C3, C1, 0.5f);
        EllipticTriangle[] triangles =
        {
            new () { C1 = C1,       C2 = c1half, C3 = c3half },
            new () { C1 = C2,       C2 = c2half, C3 = c1half },
            new () { C1 = C3,       C2 = c3half, C3 = c2half },
            new () { C1 = c1half,   C2 = c2half, C3 = c3half },
        };
        return triangles;
    }
    public Vector3 ComputeEllipticInCenter()
    {
        float semiperimeter = Length12 + Length23 + Length31;
        Vector3 centroid = new(
            (C1.x + C2.x + C3.x) / semiperimeter,
            (C1.y + C2.y + C3.y) / semiperimeter,
            (C1.z + C2.z + C3.z) / semiperimeter
        );
        centroid.Normalize();
        return centroid;
    }
    public float ComputeEllipticInRadius()
    {
        float semiperimeter = Length12 + Length23 + Length31;
        // Herons formula for triangle area.
        float area = 0.25f * Mathf.Sqrt(
            (Length12 + Length23 + Length31)    *
            (-Length12 + Length23 + Length31)   *
            (Length12 - Length23 + Length31)    *
            (Length12 + Length23 - Length31)
        );
        float inRadius = area / semiperimeter;
        // law of cosines 
        // assumes the radius of the sphere is 1.
        // c^2 = a^2 + b^2 − 2ab cos(C) | a = b = 1
        // c^2 = 1 + 1 - 2 cos(C) | -2, /(-2)
        // cos(C) = (c^2 - 2) / (-2)
        return (inRadius*inRadius - 2) / (-2);
    }
}