using System;
using System.Collections.Generic;
using UnityEngine;

public interface IEllipcticItem
{
    EllipticBBox BBox { get; }
}

public class IcoTree<T>: EllipticBBox where T : class, IEllipcticItem
{
    List<IcoTree<T>> children;
    List<IEllipcticItem> items;
    private int d;
    EllipticTriangle t;
    int max_depth = 5;
    int max_items = 2;
    int number_contained_items = 0;
    public IcoTree(EllipticTriangle triangle=null, int depth=0)
    {
        children = new();
        items = new();
        t = triangle;
        d = depth;
        Position = t == null ? Vector3.zero : t.InCenter;
        ERad = t == null ? 1f : t.InRadius;
    }

    public bool IsLeaf() => children.Count == 0;

    public void Split() {
		if (children.Count > 0) { return; }
		if (d == 0) {
			float _aspect = (1f + (float)Math.Sqrt(5))/2f;
			Vector3[] vertices = {
				new(-1,				_aspect,		0),
				new(1,				_aspect,		0),
				new(-1,				-_aspect,		0),
				new(1,				-_aspect,		0),
		
				new(0,				-1,				_aspect),
				new(0,				1,				_aspect),
				new(0,				-1,				-_aspect),
				new(0,				1,				-_aspect),
		
				new(_aspect,		0,				-1),
				new(_aspect,		0,				1),
				new(-_aspect,		0,				-1),
				new(-_aspect,		0,				1)
            };
			for (var _i=0; _i < vertices.Length; _i++) {
                vertices[_i].Normalize();
				//vertices[_i] + position
			}
	
			//polygons for an icosahedron
			children = new List<IcoTree<T>>() {
				new(new() { C1=vertices[5],     C2=vertices[11],    C3=vertices[0] },   d+1),
				new(new() { C1=vertices[1],	    C2=vertices[5],	    C3=vertices[0] },   d+1),
				new(new() { C1=vertices[7],	    C2=vertices[1],	    C3=vertices[0] },   d+1),
				new(new() { C1=vertices[10],	C2=vertices[7],	    C3=vertices[0] },   d+1),
				new(new() { C1=vertices[11],	C2=vertices[10],	C3=vertices[0] },   d+1),
				new(new() { C1=vertices[9],	    C2=vertices[5],	    C3=vertices[1] },   d+1),
				new(new() { C1=vertices[4],	    C2=vertices[11],	C3=vertices[5] },   d+1),
				new(new() { C1=vertices[2],	    C2=vertices[10],	C3=vertices[11] },  d+1),
				new(new() { C1=vertices[6],	    C2=vertices[7],	    C3=vertices[10] },  d+1),
				new(new() { C1=vertices[8],	    C2=vertices[1],	    C3=vertices[7] },   d+1),
				new(new() { C1=vertices[4],	    C2=vertices[9],	    C3=vertices[3] },   d+1),
				new(new() { C1=vertices[2],	    C2=vertices[4],	    C3=vertices[3] },   d+1),
				new(new() { C1=vertices[6],	    C2=vertices[2],	    C3=vertices[3] },   d+1),
				new(new() { C1=vertices[8],	    C2=vertices[6],	    C3=vertices[3] },   d+1),
				new(new() { C1=vertices[9],	    C2=vertices[8],	    C3=vertices[3] },   d+1),
				new(new() { C1=vertices[5],	    C2=vertices[9],	    C3=vertices[4] },   d+1),
				new(new() { C1=vertices[11],	C2=vertices[4],	    C3=vertices[2] },   d+1),
				new(new() { C1=vertices[10],	C2=vertices[2],	    C3=vertices[6] },   d+1),
				new(new() { C1=vertices[7],	    C2=vertices[6],	    C3=vertices[8] },   d+1),
				new(new() { C1=vertices[1],	    C2=vertices[8],	    C3=vertices[9] },   d+1)
            };
		} else {
            children = new List<IcoTree<T>>();
            foreach (EllipticTriangle triangle in t.Subdivide()) 
            {
                children.Add(new(triangle, d+1));
            }
		}
	}

    public bool Add(T item)
    {
        if (!CheckContains(item.BBox)) { return false; }
        number_contained_items++;
        if (items.Count < max_items || d == max_depth)
        {
            items.Add(item);
            return true;
        }

		if (IsLeaf()) { Split(); }

        foreach (IcoTree<T> child in children)
        {
            if (child.Add(item)) { return true; }
        }
        items.Add(item);
        return true;
	}

    public void Query(EllipticBBox collider, List<T> found_items)
    {
        if (!CheckOverlaps(collider)) { return; } 
        foreach (T item in items)
        {
            if (collider.CheckOverlaps(item.BBox)) { found_items.Add(item); }
        }

        if (!IsLeaf())
        {
            foreach (IcoTree<T> child in children)
            {
                child.Query(collider, found_items);
            }
        }
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
                return true;
            }
        }
        return false;
    }

    public bool Remove(T item)
    {
        if (!CheckContains(item.BBox)) { return false; }
        if (items.Remove(item)) {
            number_contained_items--;
            return true;
        }

		if (IsLeaf()) { return false; }
        foreach (IcoTree<T> child in children)
        {
            if (child.Remove(item)) {
                number_contained_items--;
                if (number_contained_items == items.Count)
                {
                    children.Clear();
                }
                return true;
            }
        }
        return false;
	}

    public void Clear() {
        if (!IsLeaf())
        {
            for (int i = 0; i < children.Count; i++) {
                children[i].Clear();
            }
            children.Clear();
        }
		items.Clear();
	}

    public Vector3 [][] GetTreeEdges()
    {
        Vector3[][] _edges = t == null 
            ? new Vector3[][] {} 
            : new Vector3[][] { t.Edges };
        if (!IsLeaf())
        {
            foreach (IcoTree<T> child in children)
            {
                Vector3[][] _child_edges = child.GetTreeEdges();
                Vector3[][] _new_edges = new Vector3[_edges.Length + _child_edges.Length][];
                _edges.CopyTo(_new_edges, 0);
                _child_edges.CopyTo(_new_edges, _edges.Length);
                _edges = _new_edges;
            }
        }
        return _edges;
    }
}