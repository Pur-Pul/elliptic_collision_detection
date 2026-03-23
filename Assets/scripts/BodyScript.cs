using System;
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
    public Vector3 Position { get; set; }
    public int Id { get => sequence.Id; }
    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;
    public string BodyType {
        get => sequence.BodyType;
    }
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
                switch (sequence.BodyType)
                {
                    case "sphere":
                        BBox = new SBC();
                        break;
                    case "obb":
                        BBox = new SOBR();
                        break;
                }
                break;
        }
        BBox.Simple = new AABB();
        BBox.Record = control.runtimeRecord;
        UpdateBBox();
    }

    public void UpdateBBox ()
    {
        BBox.Right = transform.right;
        BBox.Up = transform.up;
        BBox.Forward = transform.forward;
        BBox.Size = sequence.Size;
        BBox.Position = Position;
        BBox.UpdateSimpleSize();

        transform.position = BBox.Position;
        transform.localScale = BBox.Size;
    }

    public bool Move(int step)
    {
        Quaternion newRotation = sequence.SlerpOrientation(step);
        Vector3 newPosition = sequence.SlerpPosition(step);

        if (newRotation == transform.rotation && newPosition == Position)
        {
            return false;
        }

        transform.rotation = newRotation;
        Position = newPosition;
        return true;
    }

    public void Restart()
    {
        control.collisionTree.Remove(this);
        sequence.Reset();
        Move(0);
        UpdateBBox();
        bodyMat.SetColor("_BaseColor", color);
        drawColor = color;
    }

    public void CheckForCollision(int step)
    {
        List<IItem> collisions = control.collisionTree.CheckCollisions(this);
        /*
        if (sequence.BodyType == "sphere")
        {
            foreach (IItem collider in collisions)
            {
                Debug.Log(collider.BodyType);
            }
        }
        */
        
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
            //Debug.Log(control.CollisionList.method);
            control.CollisionList.Collision(Id, collisions.Select(item => item.Id).ToList(), step);
        } 
    }

    void OnDestroy()
    {
        control.collisionTree.Remove(this);
    }
}
