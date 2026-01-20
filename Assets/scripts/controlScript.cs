using System;
using System.Collections.Generic;
using TMPro;
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
    public TMP_InputField bodyNumperInput;
    public TMP_InputField lastStepInput;
    private int sphere_n;
    private int lastStep;
    
    SphereScript SpawnSphere(Sequence sequence, Color color)
    {
        SphereScript sphere = Instantiate(_spherePrefab);
        sphere.transform.position = sequence.Get(0);
        sphere.sequence = sequence;
        sphere.color = color;
        sphere.control = this;
        return sphere;
    }
    Vector3 RandomVector3(float magnitude)
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
    Vector3 RandomOrthogonalVector3(float magnitude, Vector3 vec)
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

    public bool Ready()
    {
        return !(sphere_n <= 0 || lastStep <= 0);
    }
    void Awake()
    {
        step = 0;
        active = false;
    }

    void DestroySpheres()
    {
        foreach (SphereScript sphere in spheres)
        {
            Destroy(sphere.gameObject);
        }
        spheres.Clear();
    }

    public void GenerateSpheres()
    {
        step = 0;
        DestroySpheres();
        sphere_n = ParseInputNumber(bodyNumperInput);
        lastStep = ParseInputNumber(lastStepInput);
        for (int i = 0; i < sphere_n; i++)
        {
            spheres.Add(SpawnSphere(Sequence.RandomSequence(lastStep, ellipseRadius), UnityEngine.Random.ColorHSV()));
        }
    }

    int ParseInputNumber(TMP_InputField input)
    {
        int number;
        if (!int.TryParse(input.text, out number))
        {
            Debug.LogError("Invalid number input: " + input.text);
            number = 0;
        }
        return number;
    }

    void Start()
    {
        spheres = new List<SphereScript>();
        collisionTree = new Octree<SphereScript>(Vector3.zero, 2*ellipseRadius + 2);
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
        if (active) {
            step++;
            if (step > lastStep)
            {
                active = false;
                step = 0;
                foreach (SphereScript sphere in spheres)
                {
                    sphere.Reset();
                }
            }
        }
    }
    void OnRenderObject()
    {
        if (faces == null || faces.Length == 0) { return; }
        if (lineMaterial == null) { return; }

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
