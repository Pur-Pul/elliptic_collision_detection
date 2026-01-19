using System.Collections.Generic;
using UnityEngine;

public class controlScript : MonoBehaviour
{
    public Octree<SphereScript> collisionTree;
    public float ellipseRadius;
    [SerializeField] private SphereScript _spherePrefab;
    [SerializeField] private Material lineMaterial;
    private Face[] faces;
    private List<SphereScript> spheres;
    public int step;
    public bool active;
    
    SphereScript SpawnSphere(Vector3 pos, Vector3 rotAxis, float rotAngle, Color color)
    {
        SphereScript sphere = Instantiate(_spherePrefab);
        sphere.transform.position = pos;
        sphere.rotation = Quaternion.AngleAxis(rotAngle, rotAxis);
        sphere.color = color;
        sphere.control = this;
        return sphere;
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
    void Awake()
    {
        step = 0;
        active = false;
    }
    void Start()
    {
        spheres = new List<SphereScript>();
        collisionTree = new Octree<SphereScript>(Vector3.zero, 2*ellipseRadius + 2);
        for (int i = 0; i < 10; i++)
        {
            Vector3 _pos = randomVector3(ellipseRadius);
            Vector3 _rotAxis = randomOrthogonalVector3(1, _pos);
            float _rotAngle = Random.Range(0.1f, 0.25f);
            _pos.Normalize();
            _pos *= ellipseRadius;
            spheres.Add(SpawnSphere(_pos, _rotAxis, _rotAngle, Random.ColorHSV()));
        }
    }
    void LateUpdate()
    {
        faces = collisionTree.GetTreeFaces();
        foreach (SphereScript sphere in spheres)
        {
            Face[] _sphere_faces = sphere.BBox.GetAABBFaces();
            Face[] _new_faces = new Face[faces.Length + _sphere_faces.Length];
            faces.CopyTo(_new_faces, 0);
            _sphere_faces.CopyTo(_new_faces, faces.Length);
            faces = _new_faces;
        }
        if (active) { step++; }
    }
    void OnRenderObject()
    {
        if (faces == null || faces.Length == 0) return;
        if (lineMaterial == null) return;

        lineMaterial.SetPass(0);
        lineMaterial.color = Color.magenta;
        GL.Begin(GL.LINES);
        foreach (Face f in faces)
        {
            DrawEdge(f.tl, f.tr);
            DrawEdge(f.tr, f.br);
            DrawEdge(f.br, f.bl);
            DrawEdge(f.bl, f.tl);
        }

        GL.End();
    }

    void DrawEdge(Vector3 a, Vector3 b)
    {
        Vector3 _right = Vector3.Cross(a, b);
        _right.Normalize();
        Vector3 offset = _right * 0.01f;
        GL.Vertex(a);
        GL.Vertex(b);
        GL.Vertex(a + offset);
        GL.Vertex(b + offset);
        GL.Vertex(a - offset);
        GL.Vertex(b - offset);
    }
}
