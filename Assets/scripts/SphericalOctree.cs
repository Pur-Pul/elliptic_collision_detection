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
    int number_contained_items = 0;
    
    SOctree<T>[] segments;
    List<T> items;
    public SOctree(Vector3? center = null, Vector2? size = null, int d = 0)
    {
        Size = size ?? new(2*Mathf.PI, Mathf.PI);
        SphericalPos = center ?? new(0, Mathf.PI * 0.5f, 0);
        depth = d;
        segments = new SOctree<T>[depth == 0 ? 8 : 4];
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
                new(Mathf.PI * 0.25f, Mathf.PI * 0.25f),
                new(Mathf.PI * 0.75f, Mathf.PI * 0.25f),
                new(-Mathf.PI * 0.75f, Mathf.PI * 0.25f),
                new(-Mathf.PI * 0.25f, Mathf.PI * 0.25f),
                new(Mathf.PI * 0.25f,  Mathf.PI * 0.75f),
                new(Mathf.PI * 0.75f,  Mathf.PI * 0.75f),
                new(-Mathf.PI * 0.75f, Mathf.PI * 0.75f),
                new(-Mathf.PI * 0.25f, Mathf.PI * 0.75f)
            };

            for (int i = 0; i < 8; i++)
            {
                segments[i] = new SOctree<T>(centers[i], new(Mathf.PI * 0.5f, Mathf.PI * 0.5f), depth + 1)
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
                if (i == segments.Length)
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
        number_contained_items++;
        if (items.Count < MaxItems || depth == MaxDepth)
        {
            items.Add(item);
            return true;
        }

		if (IsLeaf()) { Split(); }
        foreach (SOctree<T> segment in segments)
        {
            if (segment.Add(item)) { return true; }
        }
        items.Add(item);
        return true;
	}

    public bool Remove(T item)
    {
        if (items.Remove(item)) {
            number_contained_items--;
            return true;
        }

		if (IsLeaf()) { return false; }
        foreach (SOctree<T> segment in segments)
        {
            if (!segment.SimpleContains(item.BBox.Simple)) { continue; }
            if (segment.Remove(item)) {
                number_contained_items--;
                if (number_contained_items == items.Count) //Delete children if they are empty.
                {
                    Array.Clear(segments, 0, segments.Length);
                }
                return true;
            }
        }
        return false;
    }

    public void Clear()
    {
        if (!IsLeaf())
        {
            for (int i = 0; i < segments.Length; i++) {
                segments[i].Clear();
                segments[i] = null;
            }
        }
		items.Clear();
    }

    public void Query(IBoundingVolume collider, List<T> found_items)
    {
        if (!SimpleIntersects(collider.Simple)) { 
            if (depth == 0) { Debug.Log("Tree Query ERROR: Item does not fit tree."); }
            return;
        }
        foreach (T item in items)
        {
            if (collider.CheckFastOverlaps(item.BBox))
            {
                found_items.Add(item); 
            }
        }

        if (!IsLeaf())
        {
            foreach (SOctree<T> segment in segments)
            {
                segment.Query(collider, found_items);
            }
        }
    }

    public List<IItem> CheckCollisions(IItem item)
    {
        return item switch
        {
            T i => CheckCollisions(i),
            _ =>  new ()
        };
    }
    public List<IItem> CheckCollisions(T item)
    {
        List<T> found_items = new ();
        Query(item.BBox, found_items);
        List<IItem> collisions = new();
        foreach (T other in found_items)
        {
            if (item == other) { continue; }
            if (item.BBox.CheckCollision(other.BBox))
            {
                collisions.Add(other);
            }
        }
        return collisions;
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
                    if (a.BBox.CheckFastOverlaps(b.BBox) && a.BBox.CheckCollision(b.BBox))
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
}