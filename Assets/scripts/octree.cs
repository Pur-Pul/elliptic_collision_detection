using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Octree<T>: AABB,
    ICollisionTree where T : class, 
    IItem
{
    private int depth;
    public int MaxDepth { get; set; }
    public int MaxItems { get; set; }
    Octree<T>[] octants;
    Octree<T> _parent;
    List<T> items;
    public Octree(Vector3? bboxCenter = null, Vector3? size = null, int d = 0, Octree<T> parent = null)
    {
        Position = bboxCenter ?? Vector3.zero;
        Size = size ?? Vector3.zero;
        depth = d;
        octants = new Octree<T>[8];
        _parent = parent;
        items = new List<T>();
    }

    public bool IsLeaf()
    {
        return octants[0] == null;
    }

    void Split()
    {
        if (depth >= MaxDepth || !IsLeaf()) { return; }

        for (int i = 0; i < octants.Length; i++)
        {
            int x = ((i & 1) == 0) ? -1 : 1;
            int y = ((i & 2) == 0) ? -1 : 1;
            int z = ((i & 4) == 0) ? -1 : 1;
            Vector3 new_center = Position + Vector3.Scale(new Vector3(x,y,z), Size * 0.25f);
            octants[i] = new Octree<T>(new_center, Size * 0.5f, depth + 1)
            {
                Record = Record,
                MaxDepth = MaxDepth,
                MaxItems = MaxItems
                
            };
        }
        
        List<T> newItems = new();
        // Distribute the current items into the new octants if they fit.
        foreach (T item in items)
        {
            int i = 0;
            foreach (Octree<T> octant in octants)
            {
                if (octant.Add(item)) {
                    break;
                }
                if (i == octants.Count()-1)
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

    public bool Add(T item)
    {
        if (!SimpleContains(item.BBox.Simple)) { return false; }
        if (items.Count < MaxItems || depth == MaxDepth)
        {
            items.Add(item);
            item.CollisionNode = this;
            return true;
        }

		if (IsLeaf()) { Split(); }
        foreach (Octree<T> octant in octants)
        {
            if (octant.Add(item)) { return true; }
        }
        items.Add(item);
        item.CollisionNode = this;
        return true;
	}
    public bool Remove(IItem item)
    {
        return item switch
        {
            T i => Remove(i),
            _ => false
        };
    }

    bool IsEmptySubtree()
    {
        if (items.Count != 0)
            return false;

        if (IsLeaf())
            return true;

        foreach (var child in octants)
        {
            if (!child.IsEmptySubtree())
                return false;
        }

        return true;
    }

    public bool Remove(T item)
    {
        if (items.Remove(item)) {
            item.CollisionNode = null;
            if (IsEmptySubtree())
            {
                Clear();
            }
            return true;
        }

		if (IsLeaf()) { return false; }

        foreach (Octree<T> octant in octants)
        {
            if (!octant.SimpleContains(item.BBox.Simple)) { continue; }
            if (octant.Remove(item)) {
                return true;
            }
        }
        return false;
	}

    public void Clear() {
        Array.Clear(octants, 0, octants.Length);
		items.Clear();
	}

    public Vector3[][] GetTreeEdges()
    {
        Vector3[][] _edges = GetEdges();
        if (!IsLeaf())
        {
            foreach (Octree<T> octant in octants)
            {
                Vector3[][] _child_edges = octant.GetTreeEdges();
                Vector3[][] _new_edges = new Vector3[_edges.Length + _child_edges.Length][];
                _edges.CopyTo(_new_edges, 0);
                _child_edges.CopyTo(_new_edges, _edges.Length);
                _edges = _new_edges;
            }
        }
        return _edges;
    }

    public void CheckAllCollisions(List<(IItem, IItem)> collisions)
    {
        CheckAllCollisions(collisions, null);
    }

    public void CheckAllCollisions(List<(IItem,IItem)> collisions, Octree<T>[] ancestors = null)
    {
        ancestors ??= new Octree<T>[MaxDepth+1];
        ancestors[depth] = this;
        // Performs intersection checks between all items in each branch.
        // Does not perform intersections checks between items in separated branches.
        // No collision pair is checked more than once.
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
            foreach (Octree<T> octant in octants)
            {
                octant.CheckAllCollisions(collisions, ancestors);
            }    
        }
    }

    public long GetScore()
    {
        return Record.CollisionTreeScore(this.GetType().Name, "AABB");
    }
}