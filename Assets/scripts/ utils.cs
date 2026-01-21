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