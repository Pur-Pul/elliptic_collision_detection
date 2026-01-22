using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

public class ControlScript : MonoBehaviour
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
    private List<Sequence> sequences;
    
    void SpawnSpheres()
    {
        foreach (Sequence s in sequences)
        {
            SphereScript sphere = Instantiate(_spherePrefab);
            spheres.Add(sphere);
            sphere.transform.position = s.SlerpGet(0);
            sphere.sequence = s;
            sphere.color = UnityEngine.Random.ColorHSV();
            sphere.control = this;
        }
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
        sequences.Clear();
    }

    public void GenerateSpheres()
    {
        step = 0;
        DestroySpheres();
        sphere_n = ParseInputNumber(bodyNumperInput);
        lastStep = ParseInputNumber(lastStepInput);
        
        for (int i = 0; i < sphere_n; i++)
        {
            Sequence s = Sequence.RandomSequence(lastStep, ellipseRadius);
            sequences.Add(s);
        }
        SpawnSpheres();
    }

    public void LoadFromFile()
    {
        string f = EditorUtility.OpenFilePanel("Load sequence from file", Directory.GetCurrentDirectory(), "xml");
        if (f == "") { return; }
        List<Sequence> sl = SequenceUtils.FromFile(f);
        DestroySpheres();
        sequences = sl;
        lastStep = SequenceUtils.GetLastStep(sl);
        step = 0;
        sphere_n = sequences.Count;
        SpawnSpheres();
    }

    public void SaveToFile()
    {
        string currentDir = Directory.GetCurrentDirectory();
        Debug.Log(Path.Combine(currentDir, $"out/{DateTime.Now.ToString("yyyy.MM.dd_hh:mm:ss")}.xml"));//, sequences);
    }

    int ParseInputNumber(TMP_InputField input)
    {
        if (!int.TryParse(input.text, out int number))
        {
            Debug.LogError("Invalid number input: " + input.text);
            number = 0;
        }
        return number;
    }

    void Start()
    {
        spheres = new();
        sequences = new();
        collisionTree = new(Vector3.zero, 2*ellipseRadius + 2);
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
