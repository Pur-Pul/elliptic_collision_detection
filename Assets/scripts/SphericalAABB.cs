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
sin^2(ϕ) = 1 - cos^2(ϕ) = 1 - z^2
<=> sin(ϕ) = sqrt(1 - z^2) = sqrt(x^2 + y^2)


*/

public class SAABB: ISimpleSphericalBoundingVolume
{
    private Vector2 position;
    private Vector2 size;
    public RuntimeRecord Record { get; set; }
    private float? max_azimuthal;
    private float? max_polar;
    private float? min_azimuthal;
    private float? min_polar;

    public Vector2 Position 
    {
        get => position;
        set
        {
            max_azimuthal = null;
            max_polar = null;
            min_azimuthal = null;
            min_polar = null;
            position = value;
        }
    }

    public Vector2 Size {
        get => size;
        set
        {
            if (size.x != value.x)
            {
                max_azimuthal = null;
                min_azimuthal = null;
            }
            if (size.y != value.y)
            {
                max_polar = null;
                min_polar = null;
            }
            size = value;
        }
    }

    public float MaxAzimuthal
    {
        get => max_azimuthal ??= Position.x + Size.x * 0.5f;
    }
    public float MaxPolar
    {
        get => max_polar ??= Position.y + Size.y * 0.5f;
    }
    public float MinAziumthal
    {
        get => min_azimuthal ??= Position.x - Size.x * 0.5f;
    }
    public float MinPolar
    {
        get => min_polar ??= Position.y - Size.y * 0.5f;
    }

    public bool SimpleContains(ISimpleSphericalBoundingVolume other)
    {
        return other switch
        {
            SAABB saabb => Contains(saabb),
            _ => false
        };
    }

    public bool Contains (SAABB other)
    {
        return (
            MaxAzimuthal >= other.MaxAzimuthal &&
            MaxPolar >= other.MaxPolar &&
            MinAziumthal <= other.MinAziumthal &&
            MinPolar <= other.MinPolar
		);
    }

    public bool SimpleIntersects(ISimpleSphericalBoundingVolume other)
    {
        return other switch
        {
            SAABB saabb => Intersects(saabb),
            _ => false
        };
    }

    public virtual bool Intersects(SAABB other)
    {
        return !(
            MaxAzimuthal < other.MinAziumthal || MinAziumthal > other.MaxAzimuthal ||
            MaxPolar < other.MinPolar || MinPolar > other.MaxPolar
		);
    }
}