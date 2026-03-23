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
}

class SphericalUtils {
    public static float SphericalDistance(Vector3 a, Vector3 b)
    {
        // assumes a unitsphere.
        // dot product is in the range (-1, 1)
        // distance score needs to be in the range (0, 1) where 0 is close and 1 is opposite sides of the sphere.
        float d = Vector3.Dot(a, b);
        return (d - 1) / (-2f);
    }

    public static float EuclideanToSphericalDistance(float dist)
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

        float d = (dist*dist - 2) / (-2);
        return (d - 1) / (-2f);
    }

    public static float CalculateSagitta(float coord, bool squared=false)
    {
        float x = 0.25f * (squared ? coord : coord * coord);
        return 1f - Mathf.Sqrt(1f - x);
    }
}