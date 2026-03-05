using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class BodyScript : MonoBehaviour, IItem
{   
    public IBoundingVolume BBox { get; set; }
    public ControlScript control;
    public Material bodyMat;
    public Sequence sequence;
    public Color color;
    public Color drawColor;
    public int Id { get => sequence.Id; }
    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;
    void Awake()
    {
        Renderer r = GetComponent<Renderer>();
        meshRenderer = GetComponent<MeshRenderer>();
        meshFilter = GetComponent<MeshFilter>();
        bodyMat = r.material;
    }
    
    void Start()
    {
        bodyMat.SetColor("_BaseColor", color);
        drawColor = color;
    }

    public void SetMethod(int method)
    {
        switch (method)
        {
            case 0:
                meshRenderer.enabled = true;
                switch (sequence.BodyType)
                {
                    case "sphere":
                        meshFilter.mesh = GameObject.CreatePrimitive(PrimitiveType.Sphere).GetComponent<MeshFilter>().sharedMesh;
                        BBox = new BBoxSphere();
                        break;
                    case "obb":
                        meshFilter.mesh = GameObject.CreatePrimitive(PrimitiveType.Cube).GetComponent<MeshFilter>().sharedMesh;
                        BBox = new OBBox();    
                        break;
                }
                break;
            case 1:
                meshRenderer.enabled = false;
                BBox = new EllipticBBox();
                break;
        }
        BBox.Record = control.runtimeRecord;
        UpdateBBox();
    }

    void UpdateBBox ()
    {
        BBox.Position = transform.position;
        BBox.Width = transform.localScale.x;

        if (BBox is OBBox obbox)
        {
            obbox.Right = transform.right;
            obbox.Up = transform.up;
            obbox.Forward = transform.forward;
        }
    }

    public void Move(int step)
    {
        transform.position = sequence.SlerpPosition(step);
        transform.rotation = sequence.SlerpOrientation(step);
        UpdateBBox();
    }

    public void Reset()
    {
        control.collisionTree.Remove(this);
        sequence.Reset();
        Move(0);
        control.collisionTree.Add(this);
    }

    public void CheckForCollision(int step)
    {
        List<IItem> collisions = control.collisionTree.CheckCollisions(this);
        if (collisions.Count > 0)
        {
            bodyMat.SetColor("_BaseColor", Color.red);
            drawColor = Color.red;
        } else
        {
            bodyMat.SetColor("_BaseColor", color);
            drawColor = color;
        }
        if (control.CollisionList != null)
        {
            control.CollisionList.Collision(Id, collisions.Select(item => item.Id).ToList(), step);
        } 
    }

    void OnDestroy()
    {
        control.collisionTree.Remove(this);
    }
}
