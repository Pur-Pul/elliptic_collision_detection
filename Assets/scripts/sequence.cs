using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using NUnit.Framework.Internal;
using UnityEngine;

public class Keyframe
{
    public int Frame { get; set; }
    public Vector3 Position { get; set; }
}

public class Sequence
{
    List<Keyframe> keyframes;
    int cursor;
    public Sequence()
    {
        keyframes = new List<Keyframe>();
        cursor = 1;
    }
    public void Set(List<Keyframe> new_keyframes)
    {
        keyframes = new_keyframes;
    }
    public List<Keyframe> Get()
    {
        return keyframes;
    }
    public int GetLastStep()
    {
        if (keyframes.Count == 0) { return 0; }
        return keyframes.Last().Frame;
    }
    public void Insert(int step, Vector3 position)
    {
        Keyframe k = new Keyframe();
        k.Frame = step;
        k.Position = position;
        keyframes.Add(k);
    }
    Vector3 Slerp(int step)
    {
        float t = (step - keyframes[cursor-1].Frame) / (float)(keyframes[cursor].Frame - keyframes[cursor-1].Frame);
        return Vector3.Slerp(keyframes[cursor-1].Position, keyframes[cursor].Position, t);
    }
    public Vector3 SlerpGet(int step)
    {
        if (keyframes[cursor].Frame < step && cursor < keyframes.Count-1) { cursor++; }
        return Slerp(step);
    }
    public void Randomize(int lastStep, float radius)
    {
        keyframes.Clear();
        int keyframe_n = Random.Range(2, 10);
        for (int i = 0; i < keyframe_n; i++)
        {
            int step = Mathf.RoundToInt(i / (float)(keyframe_n - 1) * lastStep);
            Insert(step, VectorUtils.RandomVector3(radius));
        }
    }
    public void Reset()
    {
        cursor = 1;
    }
    public static Sequence RandomSequence(int lastStep, float radius)
    {
        Sequence seq = new();
        seq.Randomize(lastStep, radius);
        return seq;
    }
}

public class SequenceUtils
{
    public static List<Sequence> GetSequenceList(List<List<Keyframe>> keyframeListList)
    {
        List<Sequence> sequences = new();
        foreach (List<Keyframe> keyframelist in keyframeListList)
        {
            Sequence s = new();
            s.Set(keyframelist);
            sequences.Add(s);
        }
        return sequences;
    }

    public static List<List<Keyframe>> GetKeyframeList(List<Sequence> sequences)
    {
        List<List<Keyframe>> kll = new();
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
        XmlSerializer formatter = new(typeof(List<List<Keyframe>>));
        formatter.Serialize(outFile, GetKeyframeList(sequences));
    }

    public static List<Sequence> FromFile(string filePath)
    {
        XmlSerializer formatter = new(typeof(List<List<Keyframe>>));
        FileStream f = new(filePath, FileMode.Open);
        byte[] buffer = new byte[f.Length];
        f.Read(buffer, 0, (int)f.Length);
        MemoryStream stream = new(buffer);

        List<List<Keyframe>> keyframeListList = (List<List<Keyframe>>)formatter.Deserialize(stream);
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