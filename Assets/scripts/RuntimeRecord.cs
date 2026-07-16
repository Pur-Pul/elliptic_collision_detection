using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

public class RuntimeRecord
{
    public Dictionary<(Type type, MethodInfo method), (long runtime, int calls)> records;
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

    public void Write (long start, long end, (Type type, MethodInfo method) key)
    {
        records.TryGetValue(key, out var record);
        long time = end - start;
        records[key] = (record.runtime + time, record.calls + 1);
        total_time += time;
        total_n++;
    }

    public long CollisionTreeScore(Type treeType, Type simpleType) {
   
        (Type type, MethodInfo method)[] keys =
        {
            (treeType, simpleType.GetMethod("Intersects")),
            (treeType, simpleType.GetMethod("Contains")),
            (simpleType, simpleType.GetMethod("Intersects")),
            (simpleType, simpleType.GetMethod("Contains"))
        };
    
        long sum = 0;
        foreach ((Type type, MethodInfo method) key in keys)
        {
            if (records.TryGetValue(key, out var record))
            {
                sum += record.calls;
            }
        }
        
        return sum;
    
    }

    public override string ToString()
    {

        string stats = $"{"Class", -15} | {"Method", -20} | {"Runtime", 10} | {"Calls", 5} | {"Average", 10}\n";
        foreach (var kvp in records
            .OrderBy(kvp => kvp.Key.type.Name)
            .ThenBy(kvp => kvp.Key.method.Name))
        {
            var key = kvp.Key;
            (long time, int n) = kvp.Value;
            
            string className = key.type.Name;
            string functionName = key.method.Name;

            double microseconds = time * 1_000_000.0 / Stopwatch.Frequency;
            double average = (double)time / n * 1_000_000.0 / Stopwatch.Frequency;
            stats += $"{className,-15} | {functionName,-20} | {microseconds,10} | {n,5} | {average,10}\n";
        }
        double total_microseconds = total_time * 1_000_000.0 / Stopwatch.Frequency;
        double total_average = (double)total_time / total_n * 1_000_000.0 / Stopwatch.Frequency;
        stats += $"{"Total",-15} | {"",-20} | {total_microseconds,10} | {total_n,5} | {total_average,10}\n";
        UnityEngine.Debug.Log(stats);
        return stats;
    }
}