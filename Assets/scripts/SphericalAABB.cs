using UnityEngine;
using System;

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
    private Vector3? position;
    private Vector2 size;
    public RuntimeRecord Record { get; set; }

    private float? azimuthExtent;
    private float? polarExtent;
    
    public Vector3 Position {
        get {
            if (sphericalPos == null && position == null) {
                Debug.Log("Error: Both cartesian and spherical coordinates are undefined.");
                return new(0,0,0);
            }
            if (position == null)
            {
                return SphericalUtils.SphericalToCartesian(SphericalPos);
            } else
            {
                return position.Value;
            }
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
                Debug.Log("Error: Both cartesian and spherical coordinates are undefined.");
                return new(0,0,0);
            }
            if (sphericalPos == null)
            {
                return SphericalUtils.CartesianToSpherical(Position);
            } else
            {
                return sphericalPos.Value;
            }
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
            }
            if (size.y != value.y)
            {
                polarExtent = null;
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
            ? SphericalUtils.LongitudeExtent(SphericalPos, Size.y * 0.5f)
            : Size.x * 0.5f;
    }

    public float PolarExtent
    {
        get => polarExtent ??= size.y * 0.5f;
    }

    public bool SimpleContains(ISimpleBoundingVolume other)
    {
        return other switch
        {
            SAABB saabb => Contains(saabb),
            _ => false
        };
    }

    public bool Contains (SAABB other)
    {
        if (Size.x == 2 * Mathf.PI && Size.y == Mathf.PI) { 
            return true;
        }

        float azimuthDiff = Mathf.Abs(SphericalPos.x - other.SphericalPos.x) + other.AzimuthalExtent;
        float polarDiff = Mathf.Abs(SphericalPos.y - other.SphericalPos.y) + other.PolarExtent;
        return azimuthDiff <= AzimuthalExtent &&
            polarDiff <= PolarExtent;

        /*
        float azimuthDiff = SphericalUtils.FastAzimuthAbsDifference(SphericalPos.x, other.SphericalPos.x, Position, other.Position);
        azimuthDiff = SphericalUtils.TangentSum(azimuthDiff, other.AzimuthalExtent);

        if ((azimuthDiff < 0 && AzimuthalExtent < 0) || (azimuthDiff > 0 && AzimuthalExtent > 0)) {
            return azimuthDiff <= AzimuthalExtent &&
            Mathf.Abs(SphericalPos.y - other.SphericalPos.y) + other.PolarExtent < PolarExtent;
        } else {
            return azimuthDiff > AzimuthalExtent &&
            Mathf.Abs(SphericalPos.y - other.SphericalPos.y) + other.PolarExtent < PolarExtent;
        }
        */
    }

    public bool SimpleIntersects(ISimpleBoundingVolume other)
    {
        return other switch
        {
            SAABB saabb => Intersects(saabb),
            _ => false
        };
    }

    public virtual bool Intersects(SAABB other)
    {
        if (Size.x == 2 * Mathf.PI || other.Size.x == 2 * Mathf.PI) { return true; }
        float azimuthDiff = Mathf.Abs(SphericalPos.x - other.SphericalPos.x);
        float polarDiff = Mathf.Abs(SphericalPos.y - other.SphericalPos.y);
        return azimuthDiff <= AzimuthalExtent + other.AzimuthalExtent &&
            polarDiff <= PolarExtent + other.PolarExtent;
        /*
        float azimuthDiff = SphericalUtils.FastAzimuthAbsDifference(SphericalPos.x, other.SphericalPos.x, Position, other.Position);
        azimuthDiff = SphericalUtils.TangentDiff(azimuthDiff, other.AzimuthalExtent);
        
        if ((azimuthDiff < 0 && AzimuthalExtent < 0) || (azimuthDiff > 0 && AzimuthalExtent > 0)) {
            return azimuthDiff <= AzimuthalExtent &&
            Mathf.Abs(SphericalPos.y - other.SphericalPos.y) - other.PolarExtent < PolarExtent;
        } else {
            return azimuthDiff > AzimuthalExtent &&
            Mathf.Abs(SphericalPos.y - other.SphericalPos.y) - other.PolarExtent < PolarExtent;
        }
        */
    }

    public Vector3[][] GetEdges() { return null; }
}