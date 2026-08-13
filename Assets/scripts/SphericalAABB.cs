using UnityEngine;
using System;
using System.Diagnostics;

public class SAABB: ISimpleBoundingVolume
{
    private Vector3? sphericalPos;
    private Vector3? position;
    private Vector3 size;
    private Vector2 radii;
    private Vector2 sinRadii;
    public RuntimeRecord Record { get; set; }

    private float? azimuthExtent;
    private float? polarExtent;
    private string typeName = "";
    private int?[] methodIds;
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
            if (position == null)
            {
                position = SphericalUtils.SphericalToCartesian(SphericalPos);
            }
            return position.Value;
        }
        set
        {
            if (position == null || value.z != position.Value.z)
            {
                azimuthExtent = null;
            }

            position = value;
            sphericalPos = null;
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
            if (sphericalPos == null || sphericalPos.Value.y != value.y)
            {
                azimuthExtent = null;
            }

            position = null;
            sphericalPos = value;
        }
    }

    public Vector3 Size {
        get => size;
        set
        {
            if (size.x != value.x)
            {
                azimuthExtent = null;
                radii.x = value.x * 0.5f;
                sinRadii.x = Mathf.Sin(radii.x);
            }
            if (size.y != value.y)
            {
                polarExtent = null;
                radii.y = value.y * 0.5f;
                sinRadii.y = Mathf.Sin(radii.y);
            }
            size = value;
        }
    }

    public float AzimuthalExtent
    // Since the vertical edges of the SAABBs converge at the poles, the SAABBs need to be widened as the shapes they contain move closer to the poles.
    // To widen the SAABBs the z component of the Size property can be set to larger than 0.
    // The SAABBs of the s-octree should not use the additional longitude extent.
    {
        get => azimuthExtent ??= Size.z > 0
            ? SphericalUtils.LongitudeExtent(SphericalPos.z, sinRadii.x, sinRadii.y)
            : radii.x;
    }

    public float PolarExtent
    {
        get => polarExtent ??= radii.y;
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
        //Since this function is used when objects are added to the tree, this is where the spherical coordinates are first calculated.
        // In other words SOctree.contains will appear to be the slowest of the SAABB functions.
        if (Size.x == 2 * Mathf.PI && Size.y == Mathf.PI) { return true; }
        float azimuthDiff = Mathf.Abs(SphericalPos.x - other.SphericalPos.x);
        if (azimuthDiff > Mathf.PI) { azimuthDiff = 2f * Mathf.PI - azimuthDiff; }
        float polarDiff = Mathf.Abs(SphericalPos.y - other.SphericalPos.y);

        return !(
            polarDiff >= PolarExtent - other.PolarExtent ||
            azimuthDiff >= AzimuthalExtent - other.AzimuthalExtent
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
        if (Size.x == 2 * Mathf.PI || other.Size.x == 2 * Mathf.PI) { return true; }
        float azimuthDiff = Mathf.Abs(SphericalPos.x - other.SphericalPos.x);
        if (azimuthDiff > Mathf.PI) { azimuthDiff = 2f * Mathf.PI - azimuthDiff; }
        float polarDiff = Mathf.Abs(SphericalPos.y - other.SphericalPos.y);

        return !(
            polarDiff > PolarExtent + other.PolarExtent ||
            azimuthDiff > AzimuthalExtent + other.AzimuthalExtent
        );
    }

    public Vector3[][] GetEdges() { return null; }
}