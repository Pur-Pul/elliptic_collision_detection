using System.Collections.Generic;
using UnityEngine;

public class Sequence
{
    List<(int, Vector3)> keyframes;
    int cursor;
    public Sequence()
    {
        keyframes = new List<(int, Vector3)>();
        cursor = 1;
    }
    public void Insert(int step, Vector3 keyframe)
    {
        keyframes.Add((step, keyframe));
    }
    Vector3 Slerp(int step)
    {
        float t = (step - keyframes[cursor-1].Item1) / (float)(keyframes[cursor].Item1 - keyframes[cursor-1].Item1);
        return Vector3.Slerp(keyframes[cursor-1].Item2, keyframes[cursor].Item2, t);
    }
    public Vector3 Get(int step)
    {
        if (keyframes[cursor].Item1 < step && cursor < keyframes.Count-1) { cursor++; }
        return Slerp(step);
    }
    Vector3 RandomVector3(float magnitude)
    {
        Vector3 _vec = Vector3.zero;
        while (_vec.magnitude == 0)
        {
            _vec = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), Random.Range(-1f, 1f));
        }
        _vec.Normalize();
        _vec *= magnitude;
        return _vec;
    }
    public void Randomize(int lastStep, float radius)
    {
        keyframes.Clear();
        int keyframe_n = Random.Range(2, 10);
        for (int i = 0; i < keyframe_n; i++)
        {
            int step = Mathf.RoundToInt(i / (float)(keyframe_n - 1) * lastStep);
            Insert(step, RandomVector3(radius));
        }
    }
    public void Reset()
    {
        cursor = 1;
    }
    public static Sequence RandomSequence(int lastStep, float radius)
    {
        Sequence seq = new Sequence();
        seq.Randomize(lastStep, radius);
        return seq;
    }
}