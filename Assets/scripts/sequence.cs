using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using UnityEngine;

public class Keyframe
{
    public int Frame { get; set; }
    public Vector3 Position { get; set; }
    public Quaternion Orientation { get; set; }
    public float Angle { get; set; }
}

public class KeyframeList
{
    public List<Keyframe> Keyframes { get; set; } = new ();
    public string BodyType { get; set; } = "sphere";
    public Vector2 Size { get; set; }
    public int id = -1;

    public int Count => Keyframes.Count;
    public void Add(Keyframe keyframe) => Keyframes.Add(keyframe);
    public Keyframe Last() => Keyframes.Last();
    public Keyframe this[int index]
    {
        get => Keyframes[index];
        set => Keyframes[index] = value;
    }
    public void Clear() => Keyframes.Clear();
}

public class Sequence
{
    KeyframeList keyframes;
    int cursor;
    private Vector3 origin = Vector3.back;
    public string BodyType { 
        get => keyframes.BodyType; 
        set
        {
            keyframes.BodyType = value;
        }
    }
    public Vector2 Size
    {
        get => keyframes.Size;
        set
        {
            keyframes.Size = value;
        }
    }
    public int Id { 
        get => keyframes.id; set
        {
            keyframes.id = value;
        }
    }
    public Sequence()
    {
        keyframes = new KeyframeList();
        cursor = 1;
    }
    public void Set(KeyframeList new_keyframes)
    {
        keyframes = new_keyframes;
    }
    public KeyframeList Get()
    {
        return keyframes;
    }
    public int GetLastStep()
    {
        if (keyframes.Count == 0) { return 0; }
        return keyframes.Last().Frame;
    }
    public Vector3 SlerpPosition(int step)
    {
        if (keyframes[cursor].Frame < step && cursor < keyframes.Count-1) { cursor++; }
        float t = (step - keyframes[cursor-1].Frame) / (float)(keyframes[cursor].Frame - keyframes[cursor-1].Frame);
        return Vector3.Slerp(keyframes[cursor-1].Position, keyframes[cursor].Position, t);
    }
    public Quaternion SlerpOrientation(int step)
    {
        if (keyframes[cursor].Frame < step && cursor < keyframes.Count-1) { cursor++; }
        float t = (step - keyframes[cursor-1].Frame) / (float)(keyframes[cursor].Frame - keyframes[cursor-1].Frame);
        Quaternion interpolatedOrientation = Quaternion.Slerp(keyframes[cursor-1].Orientation, keyframes[cursor].Orientation, t);
        float interpolatedAngle = Mathf.Lerp(keyframes[cursor-1].Angle, keyframes[cursor].Angle, t);
        Vector3 interpolatedAxis = Vector3.Slerp(keyframes[cursor-1].Position, keyframes[cursor].Position, t).normalized;
        
        return Quaternion.AngleAxis(interpolatedAngle, interpolatedAxis) * interpolatedOrientation;
        
    }

    public void Randomize(int lastStep, float radius)
    {
        keyframes.Clear();
        int keyframe_n = UnityEngine.Random.Range(2, 10);
        for (int i = 0; i < keyframe_n; i++)
        {
            Vector3 prevPos = i == 0
                ? origin
                : keyframes[i - 1].Position;
            Quaternion prevOrientation = i == 0
                ? Quaternion.identity
                : keyframes[i - 1].Orientation;

            Keyframe k = new()
            {
                Frame = Mathf.RoundToInt(i / (float)(keyframe_n - 1) * lastStep),
                Position = prevPos,
                Angle = i == 0 ? 0 : keyframes[i - 1].Angle,
                Orientation = prevOrientation
            };

            if (i == 0 || UnityEngine.Random.Range(0.0f, 1.0f) > 0.3)
            {
                k.Position = VectorUtils.RandomVector3(radius);
                k.Angle = UnityEngine.Random.Range(0, 360);

                Vector3 axis = Vector3.Cross(prevPos, k.Position);
                axis.Normalize();
                float angle = Vector3.Angle(prevPos, k.Position);
                k.Orientation = Quaternion.AngleAxis(angle, axis) * prevOrientation;
            }
            keyframes.Add(k);
        }
    }
    public void Reset()
    {
        cursor = 1;
    }
    public static Sequence RandomSequence(int lastStep, float radius, List<string> selectedBodies)
    {
        Sequence seq = new()
        {

            BodyType = selectedBodies[UnityEngine.Random.Range(0, selectedBodies.Count)],
            Size = new Vector2(
                UnityEngine.Random.Range(0.01f, 1f),
                UnityEngine.Random.Range(0.01f, 1f)
            )
            
        };
        seq.Randomize(lastStep, radius);
        return seq;
    }
    public static readonly string[] BODY_TYPES = { "sphere", "obb" };
}

public class SequenceUtils
{
    public static List<Sequence> GetSequenceList(List<KeyframeList> keyframeListList)
    {
        List<Sequence> sequences = new();
        foreach (KeyframeList keyframelist in keyframeListList)
        {
            Sequence s = new();
            s.Set(keyframelist);
            sequences.Add(s);
        }
        return sequences;
    }

    public static List<KeyframeList> GetKeyframeList(List<Sequence> sequences)
    {
        List<KeyframeList> kll = new();
        foreach (Sequence seq in sequences)
        {
            kll.Add(seq.Get());
        }
        return kll;
    }

    public static void SaveToFile(string filePath, List<Sequence> sequences)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(filePath));
        FileStream outFile = File.Create(filePath);
        XmlSerializer formatter = new(typeof(List<KeyframeList>));
        formatter.Serialize(outFile, GetKeyframeList(sequences));
    }

    public static List<Sequence> FromFile(string filePath)
    {
        XmlSerializer formatter = new(typeof(List<KeyframeList>));
        FileStream f = new(filePath, FileMode.Open);
        byte[] buffer = new byte[f.Length];
        f.Read(buffer, 0, (int)f.Length);
        MemoryStream stream = new(buffer);

        List<KeyframeList> keyframeListList = (List<KeyframeList>)formatter.Deserialize(stream);
        List<Sequence> sl = GetSequenceList(keyframeListList);
        
        return sl;
    }
    public static int GetLastStep(List<Sequence> sl)
    {
        int LastStep = -1;
        foreach (Sequence s in sl)
        {
            if (LastStep == -1) { LastStep = s.GetLastStep(); }
            else if (LastStep != s.GetLastStep()) { Debug.LogError("Sequence list contains varying length sequences."); }
        }
        return LastStep == -1 ? 0 : LastStep;
    }
}