using System;
using System.Collections.Generic;
using UnityEngine;

public interface IItem
{
    BBox BBox { get; }
}

public class Octree<T> : BBox where T : class, IItem
{
    private int depth;
    int max_depth = 5;
    int max_items = 5;
    Octree<T>[] octants;
    List<T> items;
    Octree<T> parent;
    public Octree(Vector3 bboxCenter, float bboxWidth, int d=0, Octree<T> p = null) : base(bboxCenter, bboxWidth)
    {
        depth = d;
        octants = new Octree<T>[8];
        items = new List<T>();
        parent = p;
    }

    public bool IsLeaf()
    {
        return octants[0] == null;
    }

    void Split()
    {
        if (depth >= max_depth || !IsLeaf())
        {
            return;
        }

        for (int i = 0; i < octants.Length; i++)
        {
            int x = ((i & 1) == 0) ? -1 : 1;
            int y = ((i & 2) == 0) ? -1 : 1;
            int z = ((i & 4) == 0) ? -1 : 1;
            Vector3 new_center = position + new Vector3(x*width/4f, y*width/4f, z*width/4f);
            octants[i] = new Octree<T>(new_center, width/2f, depth + 1, this);
        }
    }

    public bool Add(T item)
    {
        if (!CheckContains(item.BBox)) { return false; }
        if (depth == max_depth || items.Count < max_items)
        {
            items.Add(item);
            return true;
        }

		if (IsLeaf()) { Split(); }
        foreach (Octree<T> octant in octants)
        {
            if (octant.Add(item)) { return true; }
        }
        return false;
	}

    public void Query(BBox collider, List<T> found_items)
    {
        if (!CheckOverlaps(collider)) { return; } 

        foreach (T item in items)
        {
            if (collider.CheckOverlaps(item.BBox)) { found_items.Add(item); }
        }

        if (!IsLeaf())
        {
            foreach (Octree<T> octant in octants)
            {
                octant.Query(collider, found_items);
            }
        }
    }

    public bool CheckCollisions(T item)
    {
        List<T> found_items = new List<T>();
        Query(item.BBox, found_items);
        
        foreach (T other in found_items)
        {
            if (item != other && item.BBox.CheckOverlaps(other.BBox))
            {
                return true;
            }
        }
        return false;
    }

    public bool Remove(T item)
    {
        if (!CheckContains(item.BBox)) { return false; }
        items.Remove(item);

		if (IsLeaf()) { return true; }
        foreach (Octree<T> octant in octants)
        {
            if (octant.Remove(item)) { return true; }
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

    public Face[] GetTreeFaces()
    {
        Face[] _faces = GetAABBFaces();
        if (!IsLeaf())
        {
            foreach (Octree<T> octant in octants)
            {
                Face[] _child_faces = octant.GetTreeFaces();
                Face[] _new_faces = new Face[_faces.Length + _child_faces.Length];
                _faces.CopyTo(_new_faces, 0);
                _child_faces.CopyTo(_new_faces, _faces.Length);
                _faces = _new_faces;
            }
        }
        return _faces;
    }
}