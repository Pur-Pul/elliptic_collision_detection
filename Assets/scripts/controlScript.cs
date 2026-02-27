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
    [SerializeField] private BodyScript _bodyPrefab;
    [SerializeField] private Material lineMaterial;
    private Vector3[][] edges;
    public List<BodyScript> bodies;
    public int step;
    bool active;
    public bool Active
    {
        get => active;
        set
        {
            active = value;
            if (active && step == 0 && CollisionList != null)
            {
                CollisionList.Reset();
            }
        }
    }
    public TMP_InputField bodyNumperInput;
    public TMP_InputField lastStepInput;
    public TMP_Dropdown MethodDropdown;
    public TMP_Dropdown RecordDropdown;
    public int body_n;
    private int lastStep;
    private List<Sequence> sequences;
    public CameraScript cam;
    public CollisionList baselineList;
    public CollisionList evaluateList;
    public CollisionList CollisionList
    {
        get
        {
            switch (RecordDropdown.value)
            {
                case 1:
                    return baselineList;
                case 2:
                    return evaluateList;
                case 0:
                default:
                    return null;
            }
        }
    }
    
    void SpawnBodies()
    {
        baselineList.IdN = sequences.Count;
        evaluateList.IdN = sequences.Count;
        foreach (Sequence s in sequences)
        {
            BodyScript body = Instantiate(_bodyPrefab);
            body.transform.position = s.SlerpPosition(0);
            body.transform.rotation = s.SlerpOrientation(0);
            body.sequence = s;
            body.color = UnityEngine.Random.ColorHSV();
            body.control = this;
            bodies.Add(body);
        }
    }

    public bool Ready()
    {
        return !(body_n <= 0 || lastStep <= 0);
    }
    void Awake()
    {
        step = 0;
        Active = false;
    }

    void DestroyBodies()
    {
        foreach (BodyScript body in bodies)
        {
            Destroy(body.gameObject);
        }
        bodies.Clear();
        sequences.Clear();
    }

    public void GenerateBodies()
    {
        step = 0;
        DestroyBodies();
        body_n = ParseInputNumber(bodyNumperInput);
        lastStep = ParseInputNumber(lastStepInput);
        
        for (int i = 0; i < body_n; i++)
        {
            Sequence s = Sequence.RandomSequence(lastStep, ellipseRadius);
            s.Id = i;
            sequences.Add(s);
        }
        SpawnBodies();
        SetMethod();
    }

    public void SetMethod()
    {
        switch (MethodDropdown.value)
        {
            case 0:
                collisionTree = new Octree<BodyScript>();
                collisionTree.Width = 2*ellipseRadius + 0.2f;
                break;
            case 1:
                collisionTree = new IcoTree<BodyScript>();
                break;
        }
        for (int i = 0; i < body_n; i++)
        {
            bodies[i].SetMethod(MethodDropdown.value);
        }
    }

    public void LoadFromFile()
    {
        string f = EditorUtility.OpenFilePanel("Load sequence from file", Directory.GetCurrentDirectory(), "xml");
        if (f == "") { return; }
        List<Sequence> sl = SequenceUtils.FromFile(f);
        DestroyBodies();
        sequences = sl;
        lastStep = SequenceUtils.GetLastStep(sl);
        step = 0;
        body_n = sequences.Count;
        SpawnBodies();
        SetMethod();
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
        bodies = new();
        sequences = new();
        baselineList = new();
        evaluateList = new();
    }

    void LateUpdate()
    {
        if (collisionTree == null) { return; }
        edges = collisionTree.GetTreeEdges();
        foreach (BodyScript body in bodies)
        {
            Vector3[][] _body_edges = {};//body.BBox.GetAABBEdges();
            Vector3[][] _new_edges = new Vector3[edges.Length + _body_edges.Length][];
            edges.CopyTo(_new_edges, 0);
            _body_edges.CopyTo(_new_edges, edges.Length);
            edges = _new_edges;
        }
        if (Active) {
            step++;
            if (step > lastStep)
            {
                Active = false;
                step = 0;
                foreach (BodyScript body in bodies)
                {
                    body.Reset();
                }
            }
        }
    }
    
    void OnDrawGizmos()
    {
        if (collisionTree == null) { return; }
        foreach (Vector3[] edge in collisionTree.GetTreeEdges())
        {
            Vector3 cam_pos = cam.transform.position;
            float dist = Mathf.Min((cam_pos - edge[0]).magnitude, (cam_pos - edge[1]).magnitude);
            float t = (dist - (cam_pos.magnitude - 1f))/2f;            
            Debug.DrawLine(edge[0], edge[1], Color.Lerp(Color.magenta, Color.black, t));
        }
    }
}
