using System;
using System.Collections.Generic;
using System.Diagnostics;

public class RuntimeRecord
{
    Dictionary<(Type, System.Reflection.MethodInfo), (long, int)> collisions;
    public RuntimeRecord() {
        collisions = new();
    }

    public void Reset()
    {
        collisions.Clear();
    }

    public void Collision (long start, long end, (Type, System.Reflection.MethodInfo) key)
    {
        collisions.TryGetValue(key, out var record);
        collisions[key] = (record.Item1 + end - start, record.Item2 + 1);
    }

    public string Stats()
    {
        string stats = $"{"Class", -15} | {"Method", -20} | {"Runtime", 10} | {"Calls", 5}\n";
        long total_time = 0;
        int total_n = 0;
        foreach ((Type, System.Reflection.MethodInfo) key in collisions.Keys)
        {
            string className = key.Item1.Name;
            string functionName = key.Item2.Name;
            (long time, int n) = collisions[key];
            stats += $"{className,-15} | {functionName,-20} | {time,10} | {n,5}\n";
            total_time += time;
            total_n += n;
        }
        stats += $"{"Total",-15} | {"",-20} | {total_time,10} | {total_n,5}\n";
        UnityEngine.Debug.Log(stats);
        return stats;
    }
}