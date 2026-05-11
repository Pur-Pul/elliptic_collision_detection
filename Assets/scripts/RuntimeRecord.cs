using System;
using System.Collections.Generic;
using System.Diagnostics;

public class RuntimeRecord
{
    public Dictionary<(Type, System.Reflection.MethodInfo), (long, int)> records;
    public long total_time = 0;
    public int total_n = 0;
    public RuntimeRecord() {
        records = new();
    }

    public void Reset()
    {
        records.Clear();
        total_time = 0;
        total_n = 0;
    }

    public void Write (long start, long end, (Type, System.Reflection.MethodInfo) key)
    {
        records.TryGetValue(key, out var record);
        long time = end - start;
        records[key] = (record.Item1 + time, record.Item2 + 1);
        //total_time += time;
        total_n++;
    }

    public override string ToString()
    {
        string stats = $"{"Class", -15} | {"Method", -20} | {"Runtime", 10} | {"Calls", 5}\n";
        foreach ((Type, System.Reflection.MethodInfo) key in records.Keys)
        {
            string className = key.Item1.Name;
            string functionName = key.Item2.Name;
            (long time, int n) = records[key];
            stats += $"{className,-15} | {functionName,-20} | {time,10} | {n,5}\n";
        }
        stats += $"{"Total",-15} | {"",-20} | {total_time,10} | {total_n,5}\n";
        UnityEngine.Debug.Log(stats);
        return stats;
    }
}