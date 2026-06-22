using System;
using UnityEngine;

class VectorUtils
{
    public static Vector3 RandomVector3(float magnitude)
        {
            Vector3 _vec = Vector3.zero;
            while (_vec.magnitude == 0)
            {
                _vec = new Vector3(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f));
            }
            _vec.Normalize();
            _vec *= magnitude;
            return _vec;
        }
    public static Vector3 RandomOrthogonalVector3(float magnitude, Vector3 vec)
    {
        Vector3 _vec = RandomVector3(1f);
        while (Vector3.Dot(vec, _vec) > 0.999f)
        {
            _vec = RandomVector3(1f);
        }
        _vec = Vector3.Cross(_vec, vec);
        _vec.Normalize();
        return _vec;
    }

    public static (Quaternion, Quaternion) TwistSwingDecomposition(Quaternion q, Vector3 twistAxis)
    {
        // https://arxiv.org/pdf/1506.05481 page 3
        // The original quaternion is defined like: q = [w, v]
        // u_t is a unit vector representing the twist axis.
        // q_p = [w, (v · u_t)u_t]
        // q_t = q_p/|q_p|

        Quaternion twist = Quaternion.identity;
        float d = Vector3.Dot(new Vector3(q.x, q.y, q.z), twistAxis);
		Vector3 proj = twistAxis * d;

		if (Mathf.Abs(d) > 0) {
			twist.w = q.w;
            twist.x = proj.x;
            twist.y = proj.y;
            twist.z = proj.z;
            twist = twist.normalized;
		}
		Quaternion swing = q * Quaternion.Inverse(twist); 
		return (swing, twist);
    }

    public static (Quaternion, Quaternion) SwingTwistDecomposition(Quaternion q, Vector3 twistAxis)
    {   
        Vector3 rotatedAxis = q * twistAxis;

        Vector3 v = new(q.x, q.y, q.z);
        float d = Vector3.Dot(v, rotatedAxis);
        Vector3 proj = rotatedAxis * d;

        Quaternion twist = new Quaternion(proj.x, proj.y, proj.z, q.w).normalized;

        Quaternion swing = Quaternion.Inverse(twist) * q;

        return (swing, twist);
    }

    public static float TwistCosineInverseSquare(Quaternion q, Vector3 twistAxis)
    {
        float d = Vector3.Dot(new Vector3(q.x, q.y, q.z), twistAxis);
        float cosine = 1;
		Vector3 proj = twistAxis * d;
        if (Mathf.Abs(d) > 1e-6f) {
            float sqrTwistMagnitude = q.w*q.w + proj.sqrMagnitude;
			cosine = q.w / sqrTwistMagnitude;
		}
        return cosine;
    }
}

class SphericalUtils {
    public static float ChordToDot(float chord, bool squared=false)
    {
        /* 
        law of cosines 
        assumes the distance is bewtween two points on the unitsphere.

            c
        A-------B
         \     /
        b \   / a
           \ /
            C

        c is the Euclidean distance AB.
        a and b are the Euclidean distances AC and BC.
        We assume the points A and B are on a unitsphere and that C is the origin of said sphere.
        Therefore a and b are both equal to the radius of the unitsphere, or in other words 1.
        According to the law of cosines:
            c^2 = a^2 + b^2 − 2ab cos(C)    | a = b = 1
            c^2 = 1 + 1 - 2 cos(C)          | -2
            c^2 - 2 = -2 cos(C)             | /(-2)
            cos(C) = (c^2 - 2) / (-2)
        where cos(C) equals the dot product between points A and B.
        */

        return ((squared ? chord : chord*chord) - 2f) * (-0.5f);
    }

    public static float ChordToAngle(float chord, bool squared=false)
    {
        // The dot product between two unit vectors equals cosine of the angle between them.
        return Mathf.Acos(ChordToDot(chord, squared));
    }

    public static float CalculateChordHeight(float chord, bool squared=false)
    {
        float chordSquared = squared ? chord : chord * chord;
        return Mathf.Sqrt(1f -  0.25f * chordSquared);
    }

    public static float CalculateSagitta(float chord, bool squared=false)
    {
        return 1f - CalculateChordHeight(chord, squared);
    }

    public static Vector2 CartesianToSpherical(Vector3 pos)
    {
        /*
            Since we are dealing with a unit sphere, the magnitude of all vectors are assumed to be 1.
            ϕ = arccos(z / sqrt(x^2 + y^2 + z^2)) = arccos(z)
            θ = arctan2(y, x)
        */
        float polar = Mathf.Acos(pos.z);
        float azimuth = Mathf.Atan2(pos.y, pos.x);

        return new(azimuth, polar);
    }

    public static Vector3 SphericalToCartesian(Vector2 pos)
    {
        return new(
            Mathf.Sin(pos.y) * Mathf.Cos(pos.x),
            Mathf.Sin(pos.y) * Mathf.Sin(pos.x),
            Mathf.Cos(pos.y)
        );
    }

    public static Vector3 CartesianToFastSpherical(Vector3 pos)
    {
        float polar = Mathf.Acos(pos.z);
        float azimuth = pos.y/pos.x;
        float azimuthSign = Mathf.Sign(pos.x);

        if(pos.x == 0) {
            azimuth = Mathf.Sign(pos.y) * Mathf.Infinity;
        }

        return new Vector3(azimuth, polar, azimuthSign);
    }

    public static float LongitudeExtent(Vector2 sphericalPos, float radius)
    {
        float sinPolar = Mathf.Sin(sphericalPos.y);
        float cosPolar = Mathf.Cos(sphericalPos.y);
        float sinRadius = Mathf.Sin(radius);

        if (sinPolar <= sinRadius)
        {
            return Mathf.PI;
        }

        return Mathf.Asin(sinRadius / Mathf.Sqrt(1 - cosPolar));
    }

    public static float FastLongitudeExtent(Vector3 pos, float radius, float cosR)
    {
        /*
            For small angles on the radius close to the equator of the sphere, function asin(sin(radius) / sqrt(1 - cos^2(polar)))
            is rougly equal to radius/sin^2(polar). As we move closer to the poles or when the radius grows, the function diverge more and more.
            To limit the diversion of the function, we can clamp the result of the second function. 
            - The first function approaches π/2 close to the poles, which means we can limit clamp the second function at π/2 as we move closer to the poles.
            - When the shapes overlap with the poles the value of the first function jumps to π in order to contain the whole shape, and we can do the same in the second function but with cosines instead of sines.
            We end up with the following two functions:
            f(polar)=If(
                sin(polar) ≤ sin(radius), π,
                asin(sin(radius) / sqrt(1 - cos^2(polar)))
            )
            g(polar)=If(
                cos(polar) ≥ cos(radius), π,
                Min(radius / Max(sin^2(polar), 0.001), π/2)
            )
            g(polar) does not actually require using any trigonometric functions since:
            cos(polar) = pos.z
            sin^2(polar) = 1 - cos(polar)
            and cos(radius) can be precalculated.
        */

        //1 - (1 - cos(radius))/sin^2(polar) ~ cos(g(polar))
        //since sin^2(x)/cos(x)

        float sinSquaredC = Mathf.Max(1.0f - pos.z * pos.z, 1e-5f);

        if (Mathf.Abs(pos.z) >= Mathf.Abs(cosR)) { // When the shape overlaps with the pole, the SAABB is expanded to cover the spherical cap.
            return -1e-5f;
        }
        
        // Approximated longitude extent / Approximated cosine longitude extent 
        //  ~ longEx / cosLongEx
        //  ~ sin(longEx) / cos(longEx)
        //  = tan(longEx)
        return  Mathf.Min(radius / sinSquaredC, Mathf.PI * 0.5f) / Mathf.Max(1 - (1 - cosR) / sinSquaredC, 0); 
    }

    public static float FastAzimuthAbsDifference(float a1, float a2, Vector3 c, Vector3 p)
    {
        // We require the absolute value of the angle difference, but that is not directly possible tangent difference function.
        // tan(alpha - beta) = (tan(alpha) - tan(beta)) / (1 + tan(alpha)tan(beta))
        // By multiplying tan(alpha) and tan(beta) with the sign of the angle difference, we can still obtain the absolute value.
        // The sign of the angle difference can be obtained with sign(dot(cross(center.xy, pos.xy), spole))
    
        float s = Mathf.Sign(c.x * p.y - c.y * p.x) * -1;
        return (s*a1 - s*a2) / (1.0f + a1 * a2);
    }

    public static float TangentSum(float t1, float t2) => (t1 + t2) / (1.0f - t1 * t2);
    public static float TangentDiff(float t1, float t2) => (t1 - t2) / (1.0f + t1 * t2);
}