using UnityEngine;
using System.Collections.Generic;

//http://journal.dcs.or.kr/_common/do.php?a=full&b=12&bidx=2868&aidx=32357
public class SOctree<T>//: ICollisionTree where T : class, IItem
{
    private int depth;
    public int MaxDepth { get; set; }
    public int MaxItems { get; set; }
    int number_contained_items = 0;
    
    SOctree<T>[] octants;
    List<T> items;
    public SOctree(Vector3? bboxCenter = null, Vector3? size = null, int d = 0)
    {
        //Position = bboxCenter ?? Vector3.zero;
        //Size = size ?? Vector3.zero;
        depth = d;
        octants = new SOctree<T>[8];
        items = new List<T>();
    }

    public bool IsLeaf()
    {
        return octants[0] == null;
    }

    void Split()
    {
        
    }
}