using System.Collections.Generic;
using System.Diagnostics;

public class RuntimeRecord
{
    Dictionary<(BoundingType, BoundingType), (long, int)> collisions;
    public RuntimeRecord() {
        collisions = new();
    }

    public void Reset()
    {
        collisions.Clear();
    }

    public void Collision (long start, long end, (BoundingType, BoundingType) key)
    {
        collisions.TryGetValue(key, out var record);
        collisions[key] = (record.Item1 + end - start, record.Item2 + 1);
    }

    public string Stats()
    {
        string stats = $"{"Method", -10} | {"Runtime", 10} | {"Calls", 5}\n";
        long total_time = 0;
        int total_n = 0;
        foreach ((BoundingType, BoundingType) key in collisions.Keys)
        {
            BoundingType A = key.Item1;
            BoundingType B = key.Item2;
            (long time, int n) = collisions[key];
            stats += $"{$"{A}-{B}",-10} | {time,10} | {n,5}\n";
            total_time += time;
            total_n += n;
        }
        stats += $"{"Total",-10} | {total_time,10} | {total_n,5}\n";
        UnityEngine.Debug.Log(stats);
        return stats;
    }
}