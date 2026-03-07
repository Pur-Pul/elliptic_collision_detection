using UnityEngine;

public class Face
{
    public Vector3 tl;
    public Vector3 tr;
    public Vector3 br;
    public Vector3 bl;
    
    public Vector3[][] Edges {
        get => new[] {
            new[] { tl, tr },
            new[] { tr, br },
            new[] { br, bl },
            new[] { bl, tl }
        };
    }
    public Face(Vector3 top_left, Vector3 top_right, Vector3 bottom_right, Vector3 bottom_left)
    {
        tl = top_left;
        tr = top_right;
        br = bottom_right;
        bl = bottom_left;
    }
    public Vector3[] GetVertices()
    {
        Vector3[] _verts = { tl, tr, br, bl };
        return _verts;
    }
}