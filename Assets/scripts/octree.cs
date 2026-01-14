using System;
using System.Collections.Generic;
using System.Numerics;
using UnityEditor.Search;

class Octree : BBox
{
    private int _depth;
    Octree[] octants;
    List<BBox> items;
    Octree parent;
    public Octree(Vector3 bboxCenter, float bboxWidth, int depth=0, Octree p = null) : base(bboxCenter, bboxWidth)
    {
        _depth = depth;
        octants = new Octree[8];
        items = new List<BBox>();
        parent = p;
    }

    void Split()
    {
        if (octants[0] != null)
        {
            return;
        }

        for (int i = 0; i < octants.Length; i++)
        {
            int x = ((i & 1) == 0) ? -1 : 1;
            int y = ((i & 2) == 0) ? -1 : 1;
            int z = ((i & 4) == 0) ? -1 : 1;
            octants[i] = new Octree(position + new Vector3(x*width/4f, y*width/4f, z*width/4f), width/2f, _depth + 1, this);
        }
    }

    public Octree Add(BBox item)
    {
		if (octants.Length == 0)
        {
            Split();
        }
		for (int i = 0; i < octants.Length; i++) {
			if (octants[i].CheckContains(item))
            { 
                return octants[i].Add(item);
			}
		}
		items.Add(item);
        return this;
	}

    public void Remove(BBox item)
    {
        items.Remove(item);
	}

    public void Clear() {
        if (octants[0] != null)
        {
            for (int i = 0; i < octants.Length; i++) {
                octants[i].Clear();
                octants[i] = null;
            }
        }
		items.Clear();
	}

    public (BBox, Vector3) Search(BBox collider, BBox _best_item = null, Vector3 _best_col_norm = new Vector3()) 
    {
        foreach (var item in items)
        {
			if (item == collider)
            {
                continue;
            }
			Vector3 col_norm = collider.CheckCollision(item);
			if (col_norm.Length() >= _best_col_norm.Length()) {
				_best_item = item;
                _best_col_norm = col_norm;
			}
		}
        for (int i = 0; i < octants.Length; i++) {
			if (octants[i].CheckContains(collider))
            { 
                (_best_item, _best_col_norm) = octants[i].Search(collider);
                break;
			}
		}
        return (_best_item, _best_col_norm);
	}
}