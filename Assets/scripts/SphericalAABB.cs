using UnityEngine;
using System;
using Unity.VisualScripting;
using System.Diagnostics;
using System.Runtime.CompilerServices;

/*
I should first optimize the Eulicdean octree+aabb as well as possible.
A spherical AABB can be represented as the intersection of three or four great circles.

I can use a similar method to SOBR-SOBR intersection detection to find out if two SAABBs are intersecting.
To make it even faster, since I know which edges of the SAABBs that are parallel, I can check if an edge is inbetween the parallel edges on the other SAABB.
This is probably still slower than Euclidan AABB overlap checking, since it would utilize cross adn dot products rather than simple comparisons.

Another option is to use spherical coordinates to define the position and size of the AABBs and do similar comparasion operations to Euclidan AABBs.
This would require converting the cartesian positions of the shapes into spherical coordinates, which normally is very slow since it requires using atan2 and arccos.
There is a possiblility that swing-twist decomposition of the orientation quaternion could be used to get the spherical coordinates without using trigonometric functions.

The is also a possibility that the trigonometric function used to convert cartesian to spherical coordinates can be skipped similarly to how cosine values are used in place of angles in the artifact.
The problem is that tan(θ) loses information about the original angle. atan(tan(θ)) = θ - kπ, where k ancors the result into the range (-π/2, π/2).
In other words using tan(θ) directly would limit the angle of the azimuth to a range of 180 degrees. 
Atan2 solves the problem, but is expensive. By reverse engineering atan2 it could be possible to compare the tangent values directly for the entire angle range (0, 2π).
another problem would be that the cosine addition formula relies on the cosine and sine of the angles to be added. In other words the sine of the polar angles are required.

cos(ϕ) = dot((0,0,1), (x,y,z)) / |(0,0,1)| * |(x,y,z)|      , (|(0,0,1)| = |(x,y,z)| = 1)
    = 0 * x + 0 * y + 1 * z
    = z

sin^2(ϕ) = 1 - cos^2(ϕ)             , (cos^2(x) + sin^2(x) = 1)
    = 1 - z^2                       , (cos(ϕ) = z)
    = x^2 + y^2                     , (x^2 + y^2 + z^2 = 1)
<=> sin(ϕ) = sqrt(x^2 + y^2)

sin^2(x) in combination with the sign of sin(x) could in theory be used to approximate the sine function.
The sign can be obtained in this context by comparing the cross product of the position and north pole.
sgn(sin(ϕ)) = sign(dot(cross((0,0,1), (x,y,z)), (1,0,0)))


*/

public class SAABB: ISimpleBoundingVolume
{
    private Vector3? sphericalPos;
    private Vector3? fastSphericalPos;
    private Vector3? position;
    private Vector3 size;
    private Vector2 radii;
    private Vector3 fastRadii;
    public RuntimeRecord Record { get; set; }

    private float? azimuthExtent;
    private float? polarExtent;
    private float? tanAzimuthExtent;
    private float? cosPolarExtent;
    private float? sinPolarExtent;
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
                tanAzimuthExtent = null;
            }

            position = value;
            sphericalPos = null;
            fastSphericalPos = null;
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
                tanAzimuthExtent = null;
            }

            position = null;
            sphericalPos = value;
            fastSphericalPos = null;
        }
    }

    public Vector3 FastSphericalPos
    {
        get
        {
            if (fastSphericalPos == null && sphericalPos == null && position == null) {
                UnityEngine.Debug.Log("Error: Both cartesian and spherical coordinates are undefined.");
                return new(0,0,0);
            }
            return fastSphericalPos ??= (position != null
                ? SphericalUtils.CartesianToFastSpherical(Position)
                : new(Mathf.Tan(SphericalPos.x), Mathf.Cos(SphericalPos.y), Mathf.Sin(SphericalPos.y))
            );
        }
    }

    public Vector3 Size {
        get => size;
        set
        {
            if (size.x != value.x)
            {
                azimuthExtent = null;
                tanAzimuthExtent = null;
                radii.x = value.x * 0.5f;
                fastRadii.x = Mathf.Tan(radii.x);
            }
            if (size.y != value.y)
            {
                polarExtent = null;
                cosPolarExtent = null;
                sinPolarExtent = null;
                radii.y = value.y * 0.5f;
                fastRadii.y = Mathf.Cos(radii.y);
                fastRadii.z = Mathf.Sin(radii.y);
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
            ? SphericalUtils.LongitudeExtent(SphericalPos.z, fastRadii.z)
            : radii.x;
    }

    public float PolarExtent
    {
        get => polarExtent ??= radii.y;
    }

    public float TanAzimuthalExtent
    {
        get 
        {
            return tanAzimuthExtent ??= Size.z > 0 
                ? SphericalUtils.TanLongitudeExtent(FastSphericalPos.y, radii.y, fastRadii.y)
                : (Size.x == 2 * Mathf.PI
                    ? -1e-5f
                    : fastRadii.x
                );
        }
    }

    public float CosPolarExtent
    {
        get => cosPolarExtent ??= fastRadii.y;
    }

    public float SinPolarExtent
    {
        get => sinPolarExtent ??= fastRadii.z;
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

        return polarDiff <= PolarExtent - other.PolarExtent &&
            azimuthDiff <= AzimuthalExtent - other.AzimuthalExtent;
    }

    public bool FastAzimuthContain (SAABB other)
    {
        if (SphericalUtils.TanAzimuthLargerThan(other.TanAzimuthalExtent, TanAzimuthalExtent)) { return false; }
        float tanAzimuthDiff = SphericalUtils.TanAzimuthDifference(
            FastSphericalPos.x,
            other.FastSphericalPos.x,
            Position,
            other.Position
        );
        float tanAzimuthExtDiff = SphericalUtils.TangentDiff(TanAzimuthalExtent, other.TanAzimuthalExtent);
        return SphericalUtils.TanAzimuthLargerThan(tanAzimuthExtDiff, tanAzimuthDiff);
    }

    public bool FastPolarContain (SAABB other)
    {
        if (other.CosPolarExtent < CosPolarExtent) { return false; }
        Vector2 polarDiff = SphericalUtils.PolarDifference(
            new (FastSphericalPos.y, FastSphericalPos.z), 
            new (other.FastSphericalPos.y, other.FastSphericalPos.z)
        );
        Vector2 polarExtDiff = SphericalUtils.PolarDifference(
            new(CosPolarExtent, SinPolarExtent),
            new(other.CosPolarExtent, other.SinPolarExtent)
        );

        return polarDiff.x > polarExtDiff.x;
    }

    public bool FastContains (SAABB other)
    {
        if (Size.x == 2 * Mathf.PI && Size.y == Mathf.PI) { return true; }
        return FastPolarContain(other) && FastAzimuthContain(other);
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

        return polarDiff <= PolarExtent + other.PolarExtent &&
            azimuthDiff <= AzimuthalExtent + other.AzimuthalExtent;
    }

    public bool FastAzimuthOverlap(SAABB other)
    {
        if (Size.x + other.Size.x > Math.PI) { return true; }
        
        float tanAzimuthDiff = SphericalUtils.TanAzimuthDifference(
            FastSphericalPos.x,
            other.FastSphericalPos.x,
            Position,
            other.Position
        );

        float tanAzimuthExtSum = SphericalUtils.TangentSum(TanAzimuthalExtent, other.TanAzimuthalExtent);
        return SphericalUtils.TanAzimuthLargerThan(tanAzimuthExtSum, tanAzimuthDiff);
    }

    public bool FastPolarOverlap(SAABB other)
    {
        if (Size.y + other.Size.y > Math.PI) { return true; }
        
        Vector2 polarDiff = SphericalUtils.PolarDifference(
            new (FastSphericalPos.y, FastSphericalPos.z), 
            new (other.FastSphericalPos.y, other.FastSphericalPos.z)
        );
        Vector2 polarExtSum = SphericalUtils.PolarAddition(
            new(CosPolarExtent, SinPolarExtent),
            new(other.CosPolarExtent, other.SinPolarExtent)
        );

        return polarDiff.x > polarExtSum.x;
    }

    public virtual bool FastIntersects(SAABB other)
    {
        /*
            For some reason this function is slower than the normal intersects function.
            The normal SphericalPos function is not called when this is used, which means the problem lies elsewhere.

        */
        if (Size.x == 2 * Mathf.PI || other.Size.x == 2 * Mathf.PI) { return true; }

        return FastPolarOverlap(other) && FastAzimuthOverlap(other);
    }

    public Vector3[][] GetEdges() { return null; }
}