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
            }
        }
    }
    public TMP_InputField bodyNumperInput;
    public TMP_InputField lastStepInput;
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

    void LateUpdate()
    {   
        if (Active) {
            foreach (BodyScript body in bodies) 
            {
                collisionTree.Remove(body);
                body.Move(step);
                collisionTree.Add(body);
            }
            foreach (BodyScript body in bodies) 
            {
                body.CheckForCollision(step);
            }
            if (step >= lastStep)
            {
                Active = false;
                if (CollisionList != null)
                {
                    CollisionList.Finish(step);
                    float[] accuracy_data = CollisionRecord.CalculateAccuracy(baselineList, artifactList);
                    AccuracyText.text = $"Baseline: {baselineList.method}\nArtifact: {artifactList.method}\nPrecision: {accuracy_data[0]}\nRecall: {accuracy_data[1]}\nF1: {accuracy_data[2]}";
                }
                RuntimeText.text = runtimeRecord.Stats();
                step = 0;
                foreach (BodyScript body in bodies)
                {
                    body.Reset();
                }
            }
            else
            {
                step++;
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
