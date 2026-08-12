using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

public class RuntimeRecord
{
    public Dictionary<(string type, string method), int> ids;
    public List<(long runtime, int calls)> records;
    public long total_time = 0;
    public int total_n = 0;
    public RuntimeRecord() {
        records = new();
        ids = new();
    }

    public void Reset()
    {
        records.Clear();
        for (int i = 0; i < ids.Count; i++)
        {
            records.Add((0,0));
        }
        total_time = 0;
        total_n = 0;
    }

    public void Write (long start, long end, int id)
    {
        (long runtime, int calls) record = records[id];
        long time = end - start;
        records[id] = (record.runtime + time, record.calls + 1);
        total_time += time;
        total_n++;
    }

    public int GetId(string type, string method)
    {
        (string type, string method) key = (type, method);
        if (!ids.TryGetValue(key, out int id)) {
            id = ids.Count();
            ids.Add(key, id);
            records.Add((0,0));
        }
        return id;
    }

    public long CollisionTreeScore(string treeType, string simpleType) {
        (string type, string method)[] keys =
        {
            (treeType, "Intersects"),
            (treeType, "Contains"),
            (simpleType, "Intersects"),
            (simpleType, "Contains")
        };
    
        long sum = 0;
        foreach ((string type, string method) key in keys)
        {
            int id = GetId(key.type, key.method);
            sum += records[id].calls;
        }
        return sum;
    }

    public override string ToString()
    {
        string stats = $"{"Class", -15} | {"Method", -20} | {"Runtime", 10} | {"Calls", 5} | {"Average", 10}\n";
        foreach (var kvp in ids
            .OrderBy(kvp => kvp.Key.type)
            .ThenBy(kvp => kvp.Key.method))
        {
            var key = kvp.Key;
            (long time, int n) = records[kvp.Value];
            if (n == 0) { continue; }
            string className = key.type;
            string functionName = key.method;

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

    public void SaveToFile(string file, string runtimeText="")
    {
        runtimeText += "Class,Method,Runtime,Calls,Average\n";
        foreach (var kvp in ids
            .OrderBy(kvp => kvp.Key.type)
            .ThenBy(kvp => kvp.Key.method))
        {
            var (type, method) = kvp.Key;
            (long time, int n) = records[kvp.Value];
            if (n == 0) { continue; }

            string className = type;
            string functionName = method;
            
            double microseconds = time * 1_000_000.0 / Stopwatch.Frequency;
            double average = (double)time / n * 1_000_000.0 / Stopwatch.Frequency;

            runtimeText += $"{className},{functionName},{microseconds},{n},{average}\n";
        }
        double total_microseconds = total_time * 1_000_000.0 / Stopwatch.Frequency;
        double total_average = (double)total_time / total_n * 1_000_000.0 / Stopwatch.Frequency;
        runtimeText += $"Total,,{total_microseconds},{total_n},{total_average}\n";
        File.WriteAllText(file,runtimeText);
    }
}