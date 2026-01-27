using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

public class ControlScript : MonoBehaviour
{
    public ICollisionTree collisionTree;
    public float ellipseRadius;
    [SerializeField] private SphereScript _spherePrefab;
    [SerializeField] private Material lineMaterial;
    private Vector3[][] edges;
    public List<SphereScript> spheres;
    public int step;
    public bool active;
    public TMP_InputField bodyNumperInput;
    public TMP_InputField lastStepInput;
    public TMP_Dropdown MethodDropdown;
    public int sphere_n;
    private int lastStep;
    private List<Sequence> sequences;
    Mesh mesh;
    
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
        SetMethod();
    }

    public void SetMethod()
    {
        switch (MethodDropdown.value)
        {
            case 0:
                collisionTree = new Octree<SphereScript>();
                collisionTree.Width = 2*ellipseRadius + 2;
                break;
            case 1:
                collisionTree = new IcoTree<SphereScript>();
                break;
        }
        for (int i = 0; i < sphere_n; i++)
        {
            switch (MethodDropdown.value)
            {
                case 0:
                    spheres[i].BBox = new BBoxSphere();
                    break;
                case 1:
                    spheres[i].BBox = new EllipticBBox();
                    break;
            }
        }
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
        SequenceUtils.SaveToFile(Path.Combine(currentDir, $"out/{DateTime.Now.ToString("yyyy.MM.dd_hh:mm:ss")}.xml"), sequences);
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
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
    }

    void LateUpdate()
    {
        if (collisionTree == null) { return; }
        edges = collisionTree.GetTreeEdges();
        foreach (SphereScript sphere in spheres)
        {
            Vector3[][] _sphere_edges = {};//sphere.BBox.GetAABBEdges();
            Vector3[][] _new_edges = new Vector3[edges.Length + _sphere_edges.Length][];
            edges.CopyTo(_new_edges, 0);
            _sphere_edges.CopyTo(_new_edges, edges.Length);
            edges = _new_edges;
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
        if (collisionTree == null) { return; }
        collisionTree.Draw(mesh);
        
    }
}
