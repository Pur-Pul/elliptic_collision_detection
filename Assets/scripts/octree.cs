using System;
using System.Collections.Generic;
using UnityEngine;

public class Octree<T>: BBox, ICollisionTree where T : class, IItem
{
    private int depth;
    int max_depth = 5;
    int max_items = 5;
    int number_contained_items = 0;
    Octree<T>[] octants;
    List<T> items;
    public Octree(Vector3? bboxCenter = null, float bboxWidth = 0, int d = 0)
    {
        Position = bboxCenter ?? Vector3.zero;
        Width = bboxWidth;
        depth = d;
        octants = new Octree<T>[8];
        items = new List<T>();
    }

    public bool IsLeaf()
    {
        return octants[0] == null;
    }

    void Split()
    {
        if (depth >= max_depth || !IsLeaf()) { return; }

        for (int i = 0; i < octants.Length; i++)
        {
            int x = ((i & 1) == 0) ? -1 : 1;
            int y = ((i & 2) == 0) ? -1 : 1;
            int z = ((i & 4) == 0) ? -1 : 1;
            Vector3 new_center = Position + new Vector3(x*Width/4f, y*Width/4f, z*Width/4f);
            octants[i] = new Octree<T>(new_center, Width/2f, depth + 1);
        }
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
        if (!CheckContains(item.BBox)) { return false; }
        number_contained_items++;
        if (items.Count < max_items || depth == max_depth)
        {
            items.Add(item);
            return true;
        }

		if (IsLeaf()) { Split(); }

        foreach (Octree<T> octant in octants)
        {
            if (octant.Add(item)) { return true; }
        }
        items.Add(item);
        return true;
	}

    public void Query(IBoundingVolume collider, List<T> found_items)
    {
        if (!CheckFastOverlaps(collider)) { return; } 
        foreach (T item in items)
        {
            if (collider.CheckFastOverlaps(item.BBox)) { found_items.Add(item); }
        }

        if (!IsLeaf())
        {
            foreach (Octree<T> octant in octants)
            {
                octant.Query(collider, found_items);
            }
        }
    }
    public bool CheckCollisions(IItem item)
    {
        return item switch
        {
            T i => CheckCollisions(i),
            _ => false
        };
    }
    public bool CheckCollisions(T item)
    {
        List<T> found_items = new ();
        Query(item.BBox, found_items);
        foreach (T other in found_items)
        {
            if (item == other) { continue; }
            if (item.BBox.CheckCollision(other.BBox))
            {
                Debug.Log("collision");
                return true;
            }
        }
        return false;
    }
    public bool Remove(IItem item)
    {
        return item switch
        {
            T i => Remove(i),
            _ => false
        };
    }
    public bool Remove(T item)
    {
        if (!CheckContains(item.BBox)) { return false; }
        if (items.Remove(item)) {
            number_contained_items--;
            return true;
        }

		if (IsLeaf()) { return false; }
        foreach (Octree<T> octant in octants)
        {
            if (octant.Remove(item)) {
                number_contained_items--;
                if (number_contained_items == items.Count) //Delete children if they are empty.
                {
                    Array.Clear(octants, 0, octants.Length);
                }
                return true;
            }
        }
        return false;
	}

    public void Clear() {
        if (!IsLeaf())
        {
            for (int i = 0; i < octants.Length; i++) {
                octants[i].Clear();
                octants[i] = null;
            }
        }
		items.Clear();
	}

    public Vector3[][] GetTreeEdges()
    {
        Vector3[][] _edges = GetAABBEdges();
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
}