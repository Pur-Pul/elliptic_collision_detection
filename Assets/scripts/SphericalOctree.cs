using UnityEngine;
using System.Collections.Generic;
using System;

//http://journal.dcs.or.kr/_common/do.php?a=full&b=12&bidx=2868&aidx=32357
public class SOctree<T>: SAABB,
    ICollisionTree where T : class,
    IItem
{
    private int depth;
    public int MaxDepth { get; set; }
    public int MaxItems { get; set; }
    
    SOctree<T>[] segments;
    SOctree<T> _parent;
    List<T> items;
    public SOctree(Vector3? center = null, Vector2? size = null, int d = 0, SOctree<T> parent = null)
    {
        Size = size ?? new(SphericalUtils.TwoPI, Mathf.PI);
        SphericalPos = center ?? new(0, Mathf.PI * 0.5f, 0);
        depth = d;
        segments = new SOctree<T>[4];
        _parent = parent;
        items = new List<T>();
    }

    public bool IsLeaf()
    {
        return segments[0] == null;
    }

    void Split()
    {
        if (depth >= MaxDepth || !IsLeaf()) { return; }
        if (depth == 0)
        {
            Vector3[] centers = new Vector3[] {
                new(Mathf.PI * 0.25f, Mathf.PI * 0.5f),
                new(Mathf.PI * 0.75f, Mathf.PI * 0.5f),
                new(-Mathf.PI * 0.75f, Mathf.PI * 0.5f),
                new(-Mathf.PI * 0.25f, Mathf.PI * 0.5f)
            };

            for (int i = 0; i < 4; i++)
            {
                segments[i] = new SOctree<T>(centers[i], new(Mathf.PI * 0.5f, Mathf.PI), depth + 1)
                {
                    Record = Record,
                    MaxDepth = MaxDepth,
                    MaxItems = MaxItems
                };
            }
        } else
        {
            for (int i = 0; i < 4; i++)
            {
                Vector3[] centers = new Vector3[] {
                    new(SphericalPos.x - Size.x * 0.25f, SphericalPos.y - Size.y * 0.25f),
                    new(SphericalPos.x + Size.x * 0.25f, SphericalPos.y - Size.y * 0.25f),
                    new(SphericalPos.x - Size.x * 0.25f, SphericalPos.y + Size.y * 0.25f),
                    new(SphericalPos.x + Size.x * 0.25f, SphericalPos.y + Size.y * 0.25f),
                };
                segments[i] = new SOctree<T>(centers[i], Size * 0.5f, depth + 1)
                {
                    Record = Record,
                    MaxDepth = MaxDepth,
                    MaxItems = MaxItems
                };
            }
        }

        List<T> newItems = new();
        // Distribute the current items into the new octants if they fit.
        foreach (T item in items)
        {
            int i = 0;
            foreach (SOctree<T> segment in segments)
            {
                if (segment.Add(item)) {
                    break;
                }
                if (i == segments.Length-1)
                {
                    newItems.Add(item);
                }
                i++;
            }
        }
        items = newItems;
    }

    public bool Add(IItem item)
    {
        return item switch
        {
            T i => Add(i),
            _ => false
        };
    }

    public bool Remove(IItem item)
    {
        return item switch
        {
            T i => Remove(i),
            _ => false
        };
    }

    public bool Add(T item)
    {
        if (!SimpleContains(item.BBox.Simple)) {
            if (depth == 0) { Debug.Log("Tree Add ERROR: Item does not fit tree."); }
            return false;
        }
        if (items.Count < MaxItems || depth == MaxDepth)
        {
            items.Add(item);
            item.CollisionNode = this;
            return true;
        }
		if (IsLeaf()) { Split(); }
        foreach (SOctree<T> segment in segments)
        {
            if (segment.Add(item)) { return true; }
        }
        items.Add(item);
        item.CollisionNode = this;
        return true;
	}
    
    bool IsEmptySubtree()
    {
        if (items.Count != 0)
            return false;

        if (IsLeaf())
            return true;

        foreach (var segment in segments)
        {
            if (!segment.IsEmptySubtree())
                return false;
        }

        return true;
    }

    public bool Remove(T item)
    {
        if (items.Remove(item)) {
            if (IsEmptySubtree())
            {
                Clear();
            }
            item.CollisionNode = null;
            return true;
        }

		if (IsLeaf()) { return false; }
        foreach (SOctree<T> segment in segments)
        {
            if (!segment.SimpleContains(item.BBox.Simple)) { continue; }
            if (segment.Remove(item)) {
                return true;
            }
        }
        return false;
    }

    public void Clear() {
        Array.Clear(segments, 0, segments.Length);
		items.Clear();
	}
    public List<SAABB> GetSAABBs(List<SAABB> saabbs = null)
    {
        saabbs ??= new();
        saabbs.Add(this);
        if (!IsLeaf())
        {
            foreach (SOctree<T> segment in segments)
            {
                segment.GetSAABBs(saabbs);
            }    
        }
        return saabbs;
    }
    public void CheckAllCollisions(List<(IItem, IItem)> collisions)
    {
        CheckAllCollisions(collisions, null);
    }

    public void CheckAllCollisions(List<(IItem,IItem)> collisions, SOctree<T>[] ancestors = null)
    {
        ancestors ??= new SOctree<T>[MaxDepth+1];
        ancestors[depth] = this;
        for (int n = 0; n <= depth; n++) {
            foreach (T a in ancestors[n].items)
            {
                foreach (T b in items)
                {
                    if (a == b) { break; }
                    if (a.BBox.CheckCollision(b.BBox))
                    {
                        collisions.Add((a, b));
                    }
                }
            }
        }

        if (!IsLeaf())
        {
            foreach (SOctree<T> segment in segments)
            {
                segment.CheckAllCollisions(collisions, ancestors);
            }    
        }
    }

    public long GetScore()
    {
        return Record.CollisionTreeScore(this.GetType().Name, "SAABB");
    }
}