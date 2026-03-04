using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CollisionRecord
{
    List<int>[] activeColliderIds;
    public string method;
    Dictionary<long, List<(int, int)>> collisions;
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
        activeColliderIds = new List<int>[idN];
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
        activeColliderIds = new List<int>[idN];
        collisions = new();
    }

    public void Collision(int id, List<int> colliderIds, int step)
    {
        activeColliderIds[id] ??= new();
        List<int> newColliderIds = colliderIds.Except(activeColliderIds[id]).ToList();
        List<int> stoppedColliderIds = activeColliderIds[id].Except(colliderIds).ToList();

        foreach (int colliderId in stoppedColliderIds)
        {
            if (id > colliderId) { continue; } //Stop a collision only once.
            long collisionId = GetCollisionId(id, colliderId);
            int start = collisions[collisionId][^1].Item1;
            collisions[collisionId][^1] = (start, step);
        }
        foreach (int colliderId in newColliderIds)
        {
            if (id > colliderId) { continue; } //Record a collision only once.
            long collisionId = GetCollisionId(id, colliderId);
            collisions[collisionId] = collisions.TryGetValue(collisionId, out var list)
                ? list
                : new List<(int,int)>();
            collisions[collisionId].Add((step, -1));
        }
        activeColliderIds[id] = activeColliderIds[id].Except(stoppedColliderIds).Concat(newColliderIds).ToList();
    }

    public void Finish(int step)
    {
        for (int id = 0; id < IdN; id++) {
            foreach (int colliderId in activeColliderIds[id])
            {
                long collisionId = GetCollisionId(colliderId, id);
                int start = collisions[collisionId][^1].Item1;
                collisions[collisionId][^1] = (start, step);
            }
            activeColliderIds[id].Clear();
        }
    }

    public static float[] CalculateAccuracy(CollisionRecord baseline, CollisionRecord artifact)
    {
        int intersect = 0;
        int basePositives = 0;
        int artifactPositives = 0;
        foreach (int collisionId in artifact.collisions.Keys)
        {
            List<(int, int)> baseList = baseline.collisions.TryGetValue(collisionId, out var list)
                ? list
                : new List<(int,int)>();
            List<(int, int)> artifactList = artifact.collisions[collisionId];

            // Count the number of reported positives in both lists.
            foreach ((int start, int end) in artifactList)
            {
                artifactPositives += end - start;
            }

            foreach ((int start, int end) in baseList)
            {
                basePositives += end - start;
            }
            int i = 0;
            int j = 0;
            while (i < artifactList.Count && j < baseList.Count)
            {
                (int e_start, int e_end) = artifactList[i];
                (int b_start, int b_end) = baseList[j];

                // Compute intersect.
                int start = Math.Max(e_start, b_start);
                int end = Math.Min(e_end, b_end);

                if (start < end)
                {
                    intersect += end - start;
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
        float precision = (float)intersect/artifactPositives;
        float recall = (float)intersect/basePositives;
        float f1 = 2f*(float)precision*recall/(precision + recall);
 
        Debug.Log($"{artifactPositives} : {basePositives}");

        float[] return_array =
        {
            precision,
            recall,
            f1
        };

        return return_array;
    }
}