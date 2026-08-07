using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class ControlScript : MonoBehaviour
{
    public ICollisionTree collisionTree;
    public float ellipseRadius;
    [SerializeField] private BodyScript _bodyPrefab;
    [SerializeField] private Material lineMaterial;
    public List<BodyScript> bodies;
    public List<SAABB> SAABBs;
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
                collisions.Clear();
                collisionTree.MaxDepth = MaxDepth;
                collisionTree.MaxItems = MaxItems;
            }
        }
    }
    (int,int,long) best = ( 0, 0, long.MaxValue );
    (int,int,long) depthBest = ( 0, 0, long.MaxValue );
    int iteration = 1;
    public int Iterations { 
        get {
            if (!int.TryParse(IterationInput.text, out int number) || number < 1)
            {
                IterationInput.text = "1";
                return 1;
            }
            return number;
        }
        set
        {
            if (value >= 1)
            {
                IterationInput.text = $"{value}";
            }
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
    public float MinSize
    {
        get
        {
            if (!float.TryParse(minSizeInput.text, out float number))
            {
                number = 0.01f;
                
            }
            number = Mathf.Clamp(number, 0.01f, 1.99f);
            minSizeInput.text = $"{number}";
            return number;
        }
    }
    public float MaxSize
    {
        get
        {
            if (!float.TryParse(maxSizeInput.text, out float number))
            {
                number = 1.00f;
                
            }
            number = Mathf.Clamp(number, 0.01f, 1.99f);
            maxSizeInput.text = $"{number}";
            return number;
        }
    }
    private (int, int) optimizeEnd = (0, 0);
    private bool optimize = false;
    public bool Optimize
    {
        get => optimize;
        set
        {
            if (!optimize && value)
            {
                optimizeEnd = (MaxDepth, MaxItems);
                MaxDepth = 0;
                MaxItems = 0;
                optimize = true;
            } else if (optimize && !value)
            {
                optimizeEnd = (0, 0);
                optimize = false;
            }
        }
    }
    public TMP_InputField bodyNumperInput;
    public TMP_InputField lastStepInput;
    public TMP_InputField maxDepthInput;
    public TMP_InputField maxItemsInput;
    public TMP_InputField IterationInput;
    public TMP_InputField minSizeInput;
    public TMP_InputField maxSizeInput;
    public TMP_Dropdown MethodDropdown;
    public TMP_Dropdown RecordDropdown;
    public TMP_Text AccuracyText;
    public TMP_Text RuntimeText;
    public TMP_Text CurrentSequenceListText;
    public Toggle CirclesInput;
    public Toggle RectanglesInput;
    public Toggle RenderInput;
    public int body_n;
    public int saabb_n;
    private int lastStep;
    private List<Sequence> sequences;
    private List<(IItem, IItem)> collisions;
    public string currentSequenceList = null;
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
        int circleN = 0;
        int rectangleN = 0;
        foreach (Sequence s in sequences)
        {
            BodyScript body = Instantiate(_bodyPrefab);
            body.transform.rotation = s.SlerpOrientation(0);
            body.sequence = s;
            body.color = UnityEngine.Random.ColorHSV();
            body.control = this;
            bodies.Add(body);

            circleN = s.BodyType == "circle" ? circleN + 1 : circleN;
            rectangleN = s.BodyType == "rectangle" ? rectangleN + 1 : rectangleN;
        }

        UnityEngine.Debug.Log($"circles: {circleN}");
        UnityEngine.Debug.Log($"rectangles: {rectangleN}");
    }

    public bool Ready() => !(body_n <= 0 || lastStep <= 0);
    void Awake()
    {
        step = 0;
        Active = false;
        Iterations = 0;
    }

    void DestroyBodies()
    {
        foreach (BodyScript body in bodies)
        {
            Destroy(body.gameObject);
        }
        bodies.Clear();
        sequences.Clear();
        baselineList.Reset();
        baselineList.method = "";
        artifactList.Reset();
        artifactList.method = "";
        runtimeRecord.Reset();
        AccuracyText.text = "Baseline: \nArtifact: \nType            | Precision | Recall    | F1";
        RuntimeText.text = "Class           | Method               | Runtime    | Calls     ";
        currentSequenceList = null;
        CurrentSequenceListText.text = "Current sequence list: none | Step NaN : NaN | Iteration NaN : NaN";
    }

    public void GenerateBodies()
    {
        step = 0;
        DestroyBodies();
        body_n = ParseInputNumber(bodyNumperInput);
        lastStep = ParseInputNumber(lastStepInput);

        List<string> selectedBodies = new();
        if (CirclesInput.isOn) { selectedBodies.Add("circle"); }
        if (RectanglesInput.isOn) { selectedBodies.Add("rectangle"); }
        if (selectedBodies.Count == 0) {
            body_n = 0;
            lastStep = 0;
            return;
        }
        for (int i = 0; i < body_n; i++)
        {
            Sequence s = Sequence.RandomSequence(lastStep, ellipseRadius, selectedBodies, MinSize, MaxSize);
            s.Id = i;
            sequences.Add(s);
        }
        SpawnBodies();
        SetMethod();
        CurrentSequenceListText.text = $"Current sequence list: Undefined* | Step {step} : {lastStep} | Iteration {iteration} : {Iterations}";
    }

    public void SetMethod()
    {
        float _tree_width = 2*ellipseRadius;
        switch (MethodDropdown.value)
        {
            case 0:
                _tree_width = 2*ellipseRadius + 2.5f;
                collisionTree = new Octree<BodyScript>
                {
                    Size = new Vector3(_tree_width, _tree_width, _tree_width)
                };
                break;
            case 1:
                _tree_width = 2*ellipseRadius + 2.5f;
                collisionTree = new Octree<BodyScript>
                {
                    Size = new Vector3(_tree_width, _tree_width, _tree_width)
                };
                break;
            case 2:
                _tree_width = 2*ellipseRadius + 2.5f;
                collisionTree = new Octree<BodyScript>
                {
                    Size = new Vector3(_tree_width, _tree_width, _tree_width)
                };
                break;
            case 3:
                collisionTree = new SOctree<BodyScript>();
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
        string wd = Directory.GetCurrentDirectory();
        string f = EditorUtility.OpenFilePanel("Load sequence from file", wd, "xml");
        if (f == "") { return; }
        List<Sequence> sl = SequenceUtils.FromFile(f);
        DestroyBodies();
        currentSequenceList = Path.GetRelativePath(wd, f);
        CurrentSequenceListText.text = $"Current sequence list: {currentSequenceList} | Step {step} : {lastStep} | Iteration {iteration} : {Iterations}";
        sequences = sl;
        lastStep = SequenceUtils.GetLastStep(sl);
        step = 0;
        iteration = 1;
        body_n = sequences.Count;
        SpawnBodies();
        SetMethod();
    }

    public void SaveToFile(string timestamp = null)
    {
        if (string.IsNullOrWhiteSpace(timestamp))
        {
            timestamp = $"{DateTime.Now:yyyy.MM.dd_HH:mm:ss}";
        }
        string wd = Directory.GetCurrentDirectory();
        currentSequenceList = $"out/{timestamp}-{body_n}-{lastStep}.xml";
        SequenceUtils.SaveToFile(Path.Combine(wd, currentSequenceList), sequences);
        CurrentSequenceListText.text = $"Current sequence list: {currentSequenceList} | Step {step} : {lastStep} | Iteration {iteration} : {Iterations}";
    }

    int ParseInputNumber(TMP_InputField input)
    {
        if (!int.TryParse(input.text, out int number))
        {
            UnityEngine.Debug.LogError("Invalid number input: " + input.text);
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
        collisions = new();
    }

    void Step()
    {
        foreach (BodyScript body in bodies)
        {
            if (body.Move(step))
            {
                collisionTree.Remove(body);
                body.UpdateBBox(true);    
                collisionTree.Add(body);
            }
        }
        
        collisionTree.CheckAllCollisions(collisions);
        foreach ((IItem, IItem) collision in collisions)
        {
            collision.Item1.Collision();
            collision.Item2.Collision();
        }
        if (CollisionList != null)
        {
            CollisionList.RecordCollisions(collisions, step);    
        }
        collisions.Clear();
        step++;
    }

    void Restart()
    {
        if (CollisionList != null)
        {
            //CollisionList.Finish(step);
            CollisionList.StopRecording(step);
            Dictionary<string, SumData> accuracy_data = CollisionRecord.CalculateAccuracy(baselineList, artifactList);
            AccuracyText.text = $"Baseline: {baselineList.method}\nArtifact: {artifactList.method}\n{accuracy_data["all"].Header()}";
            foreach (string key in accuracy_data.Keys)
            {
                if (key == "all") { continue; }
                AccuracyText.text += $"{accuracy_data[key]}";
            }
            AccuracyText.text += $"{accuracy_data["all"]}";
        }
        RuntimeText.text = runtimeRecord.ToString();
        step = 0;
        foreach (BodyScript body in bodies)
        {
            body.Restart();
        }
    }

    public void SaveRuntimeMetrics()
    {
        string currentDir = Directory.GetCurrentDirectory();
        string timeStamp = $"{DateTime.Now:yyyy.MM.dd_hh:mm:ss}";
        if (currentSequenceList == null) { SaveToFile(timeStamp); }

        string runtimeText = 
            $"#Sequence: {currentSequenceList}\n" +
            $"#Iterations: {Iterations}\n" +
            $"#Tree depth: {MaxDepth}\n" +
            $"#Tree items: {MaxItems}\n" +
            "Class,Method,Runtime,Calls,Average\n";
        foreach (var kvp in runtimeRecord.ids
            .OrderBy(kvp => kvp.Key.type)
            .ThenBy(kvp => kvp.Key.method))
        {
            var key = kvp.Key;
            (long time, int n) = runtimeRecord.records[kvp.Value];

            string className = key.type;
            string functionName = key.method;
            
            double microseconds = time * 1_000_000.0 / Stopwatch.Frequency;
            double average = (double)time / n * 1_000_000.0 / Stopwatch.Frequency;

            runtimeText += $"{className},{functionName},{microseconds},{n},{average}\n";
        }
        double total_microseconds = runtimeRecord.total_time * 1_000_000.0 / Stopwatch.Frequency;
        double total_average = (double)runtimeRecord.total_time / runtimeRecord.total_n * 1_000_000.0 / Stopwatch.Frequency;
        runtimeText += $"Total,,{total_microseconds},{runtimeRecord.total_n},{total_average}\n";
        File.WriteAllText(
            Path.Combine(currentDir, $"out/runtime-{timeStamp}.csv"),
            runtimeText
        );
    }

    public void SaveAccuracyMetrics()
    {
        string currentDir = Directory.GetCurrentDirectory();
        string timeStamp = $"{DateTime.Now:yyyy.MM.dd_hh:mm:ss}";
        if (currentSequenceList == null) { SaveToFile(timeStamp); }

        Dictionary<string, SumData> accuracy_data = CollisionRecord.CalculateAccuracy(baselineList, artifactList);
        string accuracyText = 
            $"#Sequence: {currentSequenceList}\n" +
            $"#Baseline: {baselineList.method}\n" +
            $"#Artifact: {artifactList.method}\n" +
            "Type,Precision,Recall,F1\n";
        foreach (string key in accuracy_data.Keys)
        {
            if (key == "all") { continue; }
            accuracyText += $"{accuracy_data[key].type},{accuracy_data[key].Precision},{accuracy_data[key].Recall},{accuracy_data[key].F1}\n";
        }
        accuracyText += $"{accuracy_data["all"].type},{accuracy_data["all"].Precision},{accuracy_data["all"].Recall},{accuracy_data["all"].F1}\n";
        File.WriteAllText(
            Path.Combine(currentDir, $"out/accuracy-{timeStamp}.csv"),
            accuracyText
        );
    }

    void Stop()
    {
        Active = false;
        iteration = 1;
        Restart();
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
                if (iteration < Iterations)
                {
                    iteration++;
                    Restart();
                } else
                {
                    Stop();    
                    if (Optimize)
                    {
                        //long averageRuntime = (long)Math.Round(runtimeRecord.total_time / (double)Iterations);
                        
                        long score = collisionTree.GetScore();

                        //UnityEngine.Debug.Log($"New: ({MaxDepth}, {MaxItems}, {averageRuntime})");
                        UnityEngine.Debug.Log($"New: ({MaxDepth}, {MaxItems}, {score})");
                        UnityEngine.Debug.Log($"Depth best: {depthBest}");
                        UnityEngine.Debug.Log($"Best: {best}");

                        bool newDepthBest = depthBest.Item3 >= score;//averageRuntime;
                        depthBest = newDepthBest
                            ? (MaxDepth, MaxItems, score)//averageRuntime)
                            : depthBest;

                        bool skipToNextDepth = depthBest.Item3 <= best.Item3 && (
                            (MaxDepth < optimizeEnd.Item1 && MaxItems == optimizeEnd.Item2)
                            || !newDepthBest
                        );

                        if (skipToNextDepth)
                        {
                            best = (depthBest.Item1, depthBest.Item2, depthBest.Item3);
                            depthBest = (0, 0, long.MaxValue);
                            MaxItems = 1;
                            MaxDepth++;
                            Active = true;
                        } 
                        else if (MaxDepth == 0)
                        {
                            best = (0, 0, score);//averageRuntime);
                            MaxDepth = 1;
                            MaxItems = 1;
                            Active = true;
                        }
                        else if (collisionTree.MaxItems < optimizeEnd.Item2 && newDepthBest)
                        {
                            MaxItems++;    
                            Active = true;
                        }
                        else
                        {
                            MaxDepth = best.Item1;
                            MaxItems = best.Item2;
                            Optimize = false;
                            UnityEngine.Debug.Log($"Best item limit: {MaxItems}");
                            UnityEngine.Debug.Log($"Best depth: {MaxDepth}");
                            depthBest = (0,0,long.MaxValue);
                            best = (0,0,long.MaxValue);
                        }
                    }
                }
            }
            CurrentSequenceListText.text = $"Current sequence list: {currentSequenceList ?? "Undefined*"} | Step {step} : {lastStep} | Iteration {iteration} : {Iterations}";
        }
        if (saabb_n > 0) { 
            SAABBs.Clear();
            saabb_n = 0;
        }
    }
    
    void OnDrawGizmos()
    {

        

        switch (collisionTree)
        {
            case Octree<BodyScript> octree:
                Vector3[][] _edges = octree.GetTreeEdges();
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
                    UnityEngine.Debug.DrawLine(edge[0], edge[1], Color.Lerp(Color.magenta, Color.black, t));
                }
                break;
            case SOctree<BodyScript> soctree:
                SAABBs = soctree.GetSAABBs();
                foreach (BodyScript body in bodies)
                {
                    if (body.BBox.Simple is SAABB saabb)
                    {
                        SAABBs.Add(saabb);
                    }
                }
                saabb_n = SAABBs.Count;
                break;
        }
    }
}
