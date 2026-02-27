using System;
using System.Collections.Generic;
using System.Linq;

public class CollisionList
{
    List<int>[] activeColliderIds;
    Dictionary<long, List<(int, int)>> collisions;
    int idN;
    public CollisionList(int id_n){
        activeColliderIds = new List<int>[id_n];
        collisions = new();
        idN = id_n;
    }

    long GetCollisionId(int id1, int id2)
    {
        int a = Math.Min(id1, id2);
        int b = Math.Max(id1, id2);
        return a + (long)b * idN;
    }

    void Reset()
    {
        collisions.Clear();
        foreach (List<int> colliderIdList in activeColliderIds) {
            colliderIdList.Clear();
        }
    }

    void Collision(int id, List<int> colliderIds, int step)
    {
        List<int> newColliderIds = colliderIds.Except(activeColliderIds[id]).ToList();
        List<int> stoppedColliderIds = activeColliderIds[id].Except(colliderIds).ToList();

        foreach (int colliderId in stoppedColliderIds)
        {
            long collisionId = GetCollisionId(colliderId, id);
            int start = collisions[collisionId][^1].Item1;
            collisions[collisionId][^1] = (start, step);
        }
        foreach (int colliderId in newColliderIds)
        {
            long collisionId = GetCollisionId(colliderId, id);
            collisions[collisionId] = collisions.TryGetValue(collisionId, out var list)
                ? list
                : new List<(int,int)>();
            collisions[collisionId].Add((step, -1));
        }
        activeColliderIds[id] = activeColliderIds[id].Except(stoppedColliderIds).Concat(newColliderIds).ToList();
    }
}