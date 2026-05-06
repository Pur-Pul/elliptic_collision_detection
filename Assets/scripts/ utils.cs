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
    public static float SphericalDistance(Vector3 a, Vector3 b)
    {
        // assumes a unitsphere.
        // dot product is in the range (-1, 1)
        // distance score needs to be in the range (0, 1) where 0 is close and 1 is opposite sides of the sphere.
        float d = Vector3.Dot(a, b);
        return (1 - d) * 0.5f;
    }

    public static float ChordToDot(float chord)
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

        return (chord*chord - 2) * (-0.5f);
    }

    public static float ChordToSphericalDistance(float chord)
    {
        return (1 - ChordToDot(chord)) * 0.5f;
    }

    public static float ChordToAngle(float chord)
    {
        // The dot product between two unit vectors equals cosine of the angle between them.
        return Mathf.Acos(ChordToDot(chord));
    }

    public static float AngleToSphericalDistance(float angle)
    {
        return (1 - Mathf.Cos(angle)) * 0.5f;
    }

    public static float CalculateSagitta(float chord, bool squared=false)
    {
        float chordSquared = squared ? chord : chord * chord;
        return 1f - Mathf.Sqrt(1f -  0.25f * chordSquared);
    }
}