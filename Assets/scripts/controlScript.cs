using UnityEngine;

public class controlScript : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public Octree<SphereScript> collisionTree;
    public float ellipseRadius;
    [SerializeField] private SphereScript _spherePrefab;
    void SpawnSphere(Vector3 pos, Vector3 rotAxis, float rotAngle, Color color)
    {
        SphereScript sphere = Instantiate(_spherePrefab);
        sphere.transform.position = pos;
        sphere.rotation = Quaternion.AngleAxis(rotAngle, rotAxis);
        sphere.color = color;
        sphere.control = this;
    }
    Vector3 randomVector3(float magnitude)
    {
        Vector3 _vec = Vector3.zero;
        while (_vec.magnitude == 0)
        {
            _vec = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f));
        }
        _vec.Normalize();
        _vec *= magnitude;
        return _vec;
    }
    Vector3 randomOrthogonalVector3(float magnitude, Vector3 vec)
    {
        Vector3 _vec = randomVector3(1f);
        while (Vector3.Dot(vec, _vec) > 0.999f)
        {
            _vec = randomVector3(1f);
        }
        _vec = Vector3.Cross(_vec, vec);
        _vec.Normalize();
        return _vec;

    }
    void Start()
    {
        collisionTree = new Octree<SphereScript>(Vector3.zero, 50);
        for (int i = 0; i < 10; i++)
        {
            Vector3 _pos = randomVector3(ellipseRadius);
            Debug.Log(_pos);
            Vector3 _rotAxis = randomOrthogonalVector3(1, _pos);
            float _rotAngle = Random.Range(0.5f, 2f);
            _pos.Normalize();
            _pos *= ellipseRadius;
            SpawnSphere(_pos, _rotAxis, _rotAngle, Random.ColorHSV());
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
