using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ItemIdComparer : IEqualityComparer<IItem>
{
    public bool Equals(IItem x, IItem y)
    {
        if (ReferenceEquals(x, y))
            return true;

        if (x is null || y is null)
            return false;

        return x.Id == y.Id;
    }

    public int GetHashCode(IItem obj)
    {
        return obj.Id;
    }
}

public class SumData
{
    public int Intersect = 0;
    public int BasePositives = 0;
    public int ArtifactPositives = 0;
    public string type;

    public float Precision { get => (float)Intersect/ArtifactPositives; }
    public float Recall { get => (float)Intersect/BasePositives; }
    public float F1 { get => 2f*(float)Precision*Recall/(Precision + Recall); }

    public string Header()
    {
        return $"{"Type",-15} | {"Precision",-9} | {"Recall",-9} | {"F1",-9} \n";
    }

    public override string ToString()
    {
        return $"{type,-15} | {Precision,-9} | {Recall,-9} | {F1,-9} \n";
    }
}

public class CollisionRecord
{
    List<IItem>[] activeColliders;
    ItemIdComparer comparer = new();
    public string method;
    Dictionary<long, (List<(int start, int stop)> list, string type)> collisions;
    private int idN = 0;
    public int IdN { 
        get => idN;
        set
        {
            idN = value;
            Reset();
        }
    }
    public CollisionRecord(){
        activeColliders = new List<IItem>[idN];
        collisions = new();
    }

    long GetCollisionId(int id1, int id2)
    {
        int a = Math.Min(id1, id2);
        int b = Math.Max(id1, id2);
        return a + (long)b * idN;
    }

    public void Reset()
    {
        activeColliders = new List<IItem>[idN];
        collisions = new();
    }

    //public void Collision(int id, List<int> colliderIds, int step)
    public void Collision(IItem item, List<IItem> colliders, int step)
    {
        activeColliders[item.Id] ??= new();

        List<IItem> newColliders = colliders
            .Except(activeColliders[item.Id], comparer)
            .ToList();

        List<IItem> stoppedColliders = activeColliders[item.Id]
            .Except(colliders, comparer)
            .ToList();

        foreach (IItem collider in stoppedColliders)
        {
            if (item.Id > collider.Id) { continue; } //Stop a collision only once.
            long collisionId = GetCollisionId(item.Id, collider.Id);
            var list = collisions[collisionId].list;
            list[^1] = (list[^1].start, step);
            collisions[collisionId] = (
                list,
                collisions[collisionId].type
            );
        }
        foreach (IItem collider in newColliders)
        {
            if (item.Id > collider.Id) { continue; } //Record a collision only once.
            long collisionId = GetCollisionId(item.Id, collider.Id);
            collisions[collisionId] = (
                collisions.TryGetValue(collisionId, out var collision)
                    ? collision.list
                    : new List<(int,int)>(),
                string.Compare(item.BodyType, collider.BodyType) < 0 
                    ? $"{item.BodyType}-{collider.BodyType}"
                    : $"{collider.BodyType}-{item.BodyType}"
            );

            collisions[collisionId].list.Add((step, -1));
        }
        activeColliders[item.Id] = activeColliders[item.Id]
            .Except(stoppedColliders, comparer)
            .Concat(newColliders)
            .ToList();
    }

    public void Finish(int step)
    {
        for (int id = 0; id < IdN; id++) {
            foreach (IItem collider in activeColliders[id])
            {
                long collisionId = GetCollisionId(collider.Id, id);
                var list = collisions[collisionId].list;
                list[^1] = (list[^1].start, step);
                collisions[collisionId] = (
                    list,
                    collisions[collisionId].type
                );
            }
            activeColliders[id].Clear();
        }
    }

    public static Dictionary<string, SumData> CalculateAccuracy(CollisionRecord baseline, CollisionRecord artifact)
    {
        Dictionary<string, SumData> sums = new();
        sums["all"] = new();
        sums["all"].type = "all";

        List<long> allKeys = baseline.collisions.Keys.Union(artifact.collisions.Keys).ToList();

        foreach (long collisionId in allKeys)
        {
            if (!baseline.collisions.TryGetValue(collisionId, out (List<(int start, int stop)> list, string type) b)) { b.list = new(); }
            if (!artifact.collisions.TryGetValue(collisionId, out (List<(int start, int stop)> list, string type) a)) { a.list = new(); }

            string type = baseline.collisions.ContainsKey(collisionId)
                ? b.type
                : a.type;

            // Count the number of reported positives in both lists.
            foreach ((int start, int end) in a.list)
            {
                
                if (!sums.ContainsKey(type)) { 
                    sums[type] = new();
                    sums[type].type = type;
                }
                sums[type].ArtifactPositives += end - start;
                sums["all"].ArtifactPositives += end - start;
            }

            foreach ((int start, int end) in b.list)
            {
                if (!sums.ContainsKey(type)) { 
                    sums[type] = new();
                    sums[type].type = type;
                }
                sums[type].BasePositives += end - start;
                sums["all"].BasePositives += end - start;
            }
            int i = 0;
            int j = 0;
            while (i < a.list.Count && j < b.list.Count)
            {
                (int e_start, int e_end) = a.list[i];
                (int b_start, int b_end) = b.list[j];

                // Compute intersect.
                int start = Math.Max(e_start, b_start);
                int end = Math.Min(e_end, b_end);

                if (start < end)
                {
                    sums[type].Intersect += end - start;
                    sums["all"].Intersect += end - start;
                }

                // Move the pointer that ends first.
                if (e_end < b_end) 
                {
                    i++;
                } else
                {
                    j++;
                }
            }
        }
        Debug.Log($"{sums["all"].ArtifactPositives} : {sums["all"].BasePositives}");

        return sums;
    }
}