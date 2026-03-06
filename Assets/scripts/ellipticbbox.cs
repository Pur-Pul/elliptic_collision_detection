using System;
using System.Diagnostics;
using UnityEngine;

public class EllipticBBox : IBoundingVolume
{
    public RuntimeRecord Record { get; set; }
    public BoundingType Btype { get => BoundingType.BC; }
    private float width;
    public float eRad;
    public float Width { 
        get => width;
        set
        {
            width = value;
            eRad = EuclideanToEllipticDistance(value/2f);
        }
    }
    public Vector3 Position { get; set; }

    public bool CheckFastOverlaps(IBoundingVolume other)
    {
        return other switch
        {
            EllipticBBox bbox => Timed(bbox, CheckFastOverlaps),
            _ => false
        };
    }

    public bool CheckFastOverlaps(EllipticBBox bbox)
    {
        if (eRad >= 1) { return true; }
        float centerDist = EllipticDistance(Position, bbox.Position);
        return centerDist < bbox.eRad + eRad;
    }

    bool Timed<T>(
        T volume,
        Func<T, bool> check
    ) where T : IBoundingVolume
    {
        long start = Stopwatch.GetTimestamp();
        bool result = check(volume);
        long end = Stopwatch.GetTimestamp();

        Record.Collision(start, end, (this.GetType(), check.Method));

        return result;
    }

    public bool CheckContains(IBoundingVolume other)
    {
        return other switch
        {
            EllipticBBox bbox => Timed(bbox, CheckContains),
            _ => false
        };
    }

    public bool CheckContains(EllipticBBox bbox)
    {   
        if (eRad >= 1) { return true; }
        float centerDist = EllipticDistance(Position, bbox.Position);
        //($"Dist + rad: {centerDist + bbox.eRad} | own eRad: {eRad}");
        return centerDist + bbox.eRad < eRad;
    }

    public static float EllipticDistance(Vector3 a, Vector3 b)
    {
        float d = Vector3.Dot(a, b);
        return (d - 1)/(-2f);
    }

    public static float EuclideanToEllipticDistance(float dist)
    {
        // law of cosines 
        // assumes a unitsphere.
        // assumes the distance is bewtween two points on the unitsphere.
        // c^2 = a^2 + b^2 − 2ab cos(C) | a = b = 1
        // c^2 = 1 + 1 - 2 cos(C) | -2
        // c^2 - 2 = -2 cos(C) | /(-2)
        // cos(C) = (c^2 - 2) / (-2)
        float d = (dist*dist - 2) / (-2);
        return (d - 1)/(-2f);
    }


    public bool CheckCollision(IBoundingVolume other)
    {
        return other switch
        {
            EllipticBBox bbox => Timed(bbox, CheckCollision),
            _ => false
        };
    }

    public bool CheckCollision(EllipticBBox bbox)
    {
        return CheckFastOverlaps(bbox);
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

    public Vector3[][] Edges
    {
        get {
        int n = (int)Mathf.Round(20*EllipticBBox.EllipticDistance(C1, C2))+1;
       
        Vector3[][] edges = new Vector3[3*n][];
        for (int i = 0; i < n; i++)
        {
            float t = (float)i / n;
            edges[i] = new[]
            { 
                i == 0 ? C1 : edges[i-1][1],
                Vector3.Slerp(C1, C2, t)
            };
            edges[n + i] = new[]
            { 
                i == 0 ? C2 : edges[n+i-1][1],
                Vector3.Slerp(C2, C3, t)
            };
            edges[2*n + i] = new[]
            { 
                i == 0 ? C3 : edges[2*n+i-1][1],
                Vector3.Slerp(C3, C1, t)
            };
        }
        return edges;
        }
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
        float perimeter = Length12 + Length23 + Length31;
        Vector3 centroid = new(
            (C1.x + C2.x + C3.x) / perimeter,
            (C1.y + C2.y + C3.y) / perimeter,
            (C1.z + C2.z + C3.z) / perimeter
        );
        centroid.Normalize();
        return centroid;
    }
    public float ComputeEllipticInRadius()
    {
        // Semiperimeter
        float sp = (Length12 + Length23 + Length31) / 2f;
        // Herons formula for triangle area.
        float inRadius = Mathf.Sqrt((sp - Length12) * (sp - Length23) * (sp - Length23) / sp);
        return EllipticBBox.EuclideanToEllipticDistance(inRadius);
    }
}