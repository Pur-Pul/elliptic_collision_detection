using UnityEngine;
using System;
using System.Diagnostics;

public class SAABB: ISimpleBoundingVolume
{
    private Vector3? sphericalPos;
    private Vector3? position;
    private Vector3 size;
    private bool sphericalDataDirty;
    private string typeName = "";
    private int?[] methodIds;
    
    public RuntimeRecord Record { get; set; }

    public float AzimuthExtent;
    public float PolarExtent;
    public float CosAzimuthExtent;
    public float SinAzimuthExtent;
    public float CosPolarExtent;
    public float SinPolarExtent;
    public float SinPolar;
    public float CosPolar;
    public float Radius;
    public float CosRadius;
    public float SinRadius;

    public SAABB() {
        typeName = GetType().Name;    
        methodIds = new int?[2];
    }
    
    public Vector3 Position {
        get 
        {
            if (sphericalPos == null && position == null) {
                UnityEngine.Debug.Log("Error: Both cartesian and spherical coordinates are undefined.");
                return new(0,0,0);
            }
            return position ??= SphericalUtils.SphericalToCartesian(SphericalPos);
        }
        set
        {
            position = value;
            sphericalPos = null;
            sphericalDataDirty = true;
        }
    }

    public Vector3 SphericalPos
    {
        get
        {
            if (sphericalPos == null && position == null) {
                UnityEngine.Debug.Log("Error: Both cartesian and spherical coordinates are undefined.");
                return new(0,0,0);
            }
            return sphericalPos ??= SphericalUtils.CartesianToSpherical(Position);
        }
        set
        {
            position = null;
            sphericalPos = value;
            sphericalDataDirty = true;
        }
    }

    public Vector3 Size {
        get => size;
        set
        {
            if (value.z > 0)
            {
                if (size.z != value.z)
                {
                    Radius = value.z;
                    AzimuthExtent = Radius;
                    PolarExtent = Radius;
                    CosPolarExtent = Mathf.Cos(PolarExtent);
                    SinPolarExtent = Mathf.Sin(PolarExtent);
                    CosRadius = Mathf.Cos(value.z);
                    SinRadius = Mathf.Sin(value.z);
                    sphericalDataDirty = true;
                }
            } else
            {
                if (size.x != value.x)
                {
                    AzimuthExtent = value.x * 0.5f;
                    CosAzimuthExtent = Mathf.Cos(AzimuthExtent);
                    SinAzimuthExtent = Mathf.Sin(AzimuthExtent);
                }
                if (size.y != value.y)
                {
                    PolarExtent = value.y * 0.5f;
                    CosPolarExtent = Mathf.Cos(PolarExtent);
                    SinPolarExtent = Mathf.Sin(PolarExtent);
                }   
            }
            size = value;
        }
    }

    public void CalculateSphericalData ()
    {
        if (!sphericalDataDirty) { return; }
        CosPolar = Position.z;
        SinPolar = Mathf.Sqrt(1f - CosPolar * CosPolar);
        if (Radius > 0)
        {
            SinAzimuthExtent = SphericalUtils.SinLongitudeExtent(SinPolar, SinRadius);
            CosAzimuthExtent = Mathf.Sqrt(1f - SinAzimuthExtent * SinAzimuthExtent);
        }
        sphericalDataDirty = false;
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
            SAABB saabb => Timed(() => Contains(saabb), methodIds[0] ??= Record.GetId(typeName, nameof(Contains))), 
            _ => false
        };
    }

    public bool Contains (SAABB other)
    {
        /*
                p
                /\
               /︶\
            a / Δθ \ b 
             /      \
            /        \
           u----------v
                c
        */
        // Note: u and v are not actually on the same latitude.
        // Δθ represents the azimuthal difference between u and v
        // a and b are the polar angles of the SAABB and u and v are their centers.
        // cos(a) = v.z and sin(a) = sqrt(1 - v.z^2) and the same for w.
        // cos(c) = dot(u, v)
        // cos(c) = cos(a)cos(b) + sin(a)sin(b)cos(Δθ)
        // => cos(Δθ)sin(a)sin(b) = cos(c) - cos(a)cos(b)

        
        /*        v
                 / |
                /  |
             c /   | Δφ
              /    | 
             /    ┏|
            u------q
        */
        // Δφ represents the difference between the polar angles of u and v.
        // Cosine subtraction formula: cos(Δφ) = cos(a-b) = cos(a)cos(b) + sin(a)sin(b)
        if (Size.x == SphericalUtils.TwoPI && Size.y == Mathf.PI) { return true; }
        CalculateSphericalData();
        other.CalculateSphericalData();
        if (CosAzimuthExtent > other.CosAzimuthExtent || CosPolarExtent > other.CosPolarExtent) { return false; }
        float cosC = Vector3.Dot(Position, other.Position);
        float cosProd = CosPolar * other.CosPolar;
        float sinProd = SinPolar * other.SinPolar;

        float cosAzimuthExtDiff = CosAzimuthExtent * other.CosAzimuthExtent + SinAzimuthExtent * other.SinAzimuthExtent;
        float cosPolarExtDiff = CosPolarExtent * other.CosPolarExtent + SinPolarExtent * other.SinPolarExtent;

        return !(
            cosProd + sinProd < cosPolarExtDiff ||
            (cosC - cosProd) < cosAzimuthExtDiff * sinProd
        );
    }

    public bool SimpleIntersects(ISimpleBoundingVolume other)
    {
        return other switch
        {
            SAABB saabb => Timed(() => Intersects(saabb), methodIds[1] ??= Record.GetId(typeName, nameof(Intersects))),
            _ => false
        };
    }

    public virtual bool Intersects(SAABB other)
    {
        if (Size.x == SphericalUtils.TwoPI || other.Size.x == SphericalUtils.TwoPI) { return true; }
        // Intersection detection between SBCs is faster than between SAABBs.
        // Since the SAABBs of the spherical shapes are all fitted to SBCs (the SOBRs are first fitten to SBCs), it is possible to completely replace the SAABB intersection function with the SBC intersection function.
        // SBC intersection detection between the inscribed SBCs of the SAABBs is also more accurate.
        float cosC = Vector3.Dot(Position, other.Position);
        float cosRadiusSum = CosRadius * other.CosRadius - SinRadius * other.SinRadius;
        return cosRadiusSum < cosC;
    }

    public Vector3[][] GetEdges() { return null; }
}