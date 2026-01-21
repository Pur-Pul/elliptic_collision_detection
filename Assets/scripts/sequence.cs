using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
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
    public Vector3 Get(int step)
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
    public static Sequence FromFile(string filePath)
    {
        XmlSerializer formatter = new(typeof(List<Keyframe>));
        FileStream f = new(filePath, FileMode.Open);
        byte[] buffer = new byte[f.Length];
        f.Read(buffer, 0, (int)f.Length);
        MemoryStream stream = new(buffer);

        Sequence s = new();
        s.Set((List<Keyframe>)formatter.Deserialize(stream));

        return s;
    }

    public void SaveToFile(string filePath)
    {
        FileStream outFile = File.Create(filePath);
        XmlSerializer formatter = new(typeof(List<Keyframe>));
        formatter.Serialize(outFile, keyframes);
    }
}