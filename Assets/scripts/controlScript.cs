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
    public List<BodyScript> bodies;
    public int step;
    bool active;
    public bool Active
    {
        get => active;
        set
        {
            active = value;
            if (active && step == 0)
            {
                if (CollisionList != null)
                {
                    CollisionList.Reset();
                    CollisionList.method = MethodDropdown.options[MethodDropdown.value].text;    
                }
                runtimeRecord.Reset();
                collisionTree.Clear();
                collisionTree.MaxDepth = MaxDepth;
                collisionTree.MaxItems = MaxItems;
            }
        }
    }
    (int,int,long) best;
    private int optimize;
    public int Optimize { 
        get => optimize;
        set
        {
            if (value > 0)
            {
                best = (0,0,long.MaxValue);
                MaxDepth = 0;
                MaxItems = 0;
            }
            
            optimize = value;
        }
    }
    public int MaxDepth {
        get
        {
            if (!int.TryParse(maxDepthInput.text, out int number))
            {
                number = 0;
                maxDepthInput.text = "0";
                collisionTree.MaxDepth = 0;
            }
            return number;
        }
        set
        {
            maxDepthInput.text = value.ToString();
            collisionTree.MaxDepth = value;
        } 
    }
    public int MaxItems {
        get
        {
            if (!int.TryParse(maxItemsInput.text, out int number))
            {
                number = 0;
                maxItemsInput.text = "0";
                collisionTree.MaxItems = 0;
            }
            return number;
        }
        set
        {
            maxItemsInput.text = value.ToString();
            collisionTree.MaxItems = value;
        } 
    }
    public TMP_InputField bodyNumperInput;
    public TMP_InputField lastStepInput;
    public TMP_InputField maxDepthInput;
    public TMP_InputField maxItemsInput;
    public TMP_Dropdown MethodDropdown;
    public TMP_Dropdown RecordDropdown;
    public TMP_Text AccuracyText;
    public TMP_Text RuntimeText;
    public int body_n;
    private int lastStep;
    private List<Sequence> sequences;
    public CameraScript cam;
    public CollisionRecord baselineList;
    public CollisionRecord artifactList;
    public CollisionRecord CollisionList
    {
        get
        {
            switch (RecordDropdown.value)
            {
                case 1:
                    return baselineList;
                case 2:
                    return artifactList;
                case 0:
                default:
                    return null;
            }
        }
    }
    public RuntimeRecord runtimeRecord;
    
    void SpawnBodies()
    {
        baselineList.IdN = sequences.Count;
        artifactList.IdN = sequences.Count;
        foreach (Sequence s in sequences)
        {
            BodyScript body = Instantiate(_bodyPrefab);
            body.transform.rotation = s.SlerpOrientation(0);
            body.Position = s.SlerpPosition(0);
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
        Optimize = 0;
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
        float _tree_width = 2*ellipseRadius + 0.5f;
        switch (MethodDropdown.value)
        {
            case 0:
                collisionTree = new Octree<BodyScript>();
                collisionTree.Size = new Vector3(_tree_width,_tree_width,_tree_width);
                break;
            case 1:
                collisionTree = new Octree<BodyScript>();
                collisionTree.Size = new Vector3(_tree_width,_tree_width,_tree_width);
                break;
        }
        collisionTree.Record = runtimeRecord;
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
        artifactList = new();
        runtimeRecord = new();
    }

    void Step()
    {
        foreach (BodyScript body in bodies) 
        {
            if (body.Move(step))
            {
                collisionTree.Remove(body);
                body.UpdateBBox();
                collisionTree.Add(body);
            }
        }
        foreach (BodyScript body in bodies) 
        {
            body.CheckForCollision(step);
        }
        step++;
    }

    void Stop()
    {
        Active = false;
        if (CollisionList != null)
        {
            CollisionList.Finish(step);
            float[] accuracy_data = CollisionRecord.CalculateAccuracy(baselineList, artifactList);
            AccuracyText.text = $"Baseline: {baselineList.method}\nArtifact: {artifactList.method}\nPrecision: {accuracy_data[0]}\nRecall: {accuracy_data[1]}\nF1: {accuracy_data[2]}";
        }
        RuntimeText.text = runtimeRecord.ToString();
        step = 0;
        foreach (BodyScript body in bodies)
        {
            body.Stop();
        }
    }

    void LateUpdate()
    {   
        if (Active) {
            if (step == 0)
            {
                foreach (BodyScript body in bodies)
                {
                    collisionTree.Add(body);
                }
            }
            Step();
            if (step >= lastStep)
            {
                Stop();
                if (Optimize > 0)
                {
                    best = best.Item3 > runtimeRecord.total_time
                        ? (MaxItems, MaxDepth, runtimeRecord.total_time)
                        : best;
                    if (MaxDepth < Optimize && MaxItems == Optimize)
                    {
                        MaxItems = 0;
                        MaxDepth++;
                        Active = true;
                    } else if (collisionTree.MaxItems < Optimize)
                    {
                        MaxItems++;
                        Active = true;
                    } else
                    {
                        MaxItems = best.Item1;
                        MaxDepth = best.Item2;
                        Optimize = 0;
                        Debug.Log($"Best item limit: {MaxItems}");
                        Debug.Log($"Best depth: {MaxDepth}");
                    }
                }
            }
        }
    }
    
    void OnDrawGizmos()
    {
        if (collisionTree == null) { return; }
        Vector3[][] _edges = collisionTree.GetTreeEdges();
        foreach (BodyScript body in bodies)
        {
            Vector3[][] _body_edges = body.BBox.Simple.GetEdges();
            Vector3[][] _new_edges = new Vector3[_edges.Length + _body_edges.Length][];
            _edges.CopyTo(_new_edges, 0);
            _body_edges.CopyTo(_new_edges, _edges.Length);
            _edges = _new_edges;
        }
        foreach (Vector3[] edge in _edges)
        {
            Vector3 cam_pos = cam.transform.position;
            float dist = Mathf.Min((cam_pos - edge[0]).magnitude, (cam_pos - edge[1]).magnitude);
            float t = (dist - (cam_pos.magnitude - 1f))/2f;            
            Debug.DrawLine(edge[0], edge[1], Color.Lerp(Color.magenta, Color.black, t));
        }
    }
}
