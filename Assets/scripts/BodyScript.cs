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
    private Vector3 meshOffset = Vector3.zero;
    Vector3 MeshOffset
    {
        get => meshOffset;
        set
        {
            meshOffset = value;
            transform.position = position 
                + transform.right * meshOffset.x
                + transform.up * meshOffset.y
                + transform.forward * meshOffset.z;
        }
    }

    private Vector3 position;
    public Vector3 Position {
        get => position;
        set
        {
            position = value;
            transform.position = position 
                + transform.right * meshOffset.x
                + transform.up * meshOffset.y
                + transform.forward * meshOffset.z;
        }
    }
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
                        MeshOffset = new Vector3(0, 0, 0);
                        transform.localScale = new Vector3(sequence.Size.x, sequence.Size.x, sequence.Size.x);
                        break;
                    case "obb":
                        
                        meshFilter.mesh = GameObject.CreatePrimitive(PrimitiveType.Cube).GetComponent<MeshFilter>().sharedMesh;
                        float cord = Mathf.Sqrt(sequence.Size.x * sequence.Size.x + sequence.Size.y * sequence.Size.y);
                        float sagitta = 1 - Mathf.Sqrt(1 - (cord/2) * (cord/2));
                        MeshOffset = new Vector3(0, 0, sagitta/2);
                        transform.localScale = new Vector3(
                            sequence.Size.x,
                            sequence.Size.y,
                            sagitta
                        );
                        
                        BBox = new OBBox();
                        break;
                }
                break;
            case 1:
                meshRenderer.enabled = false;
                switch (sequence.BodyType)
                {
                    case "sphere":
                        transform.localScale = new Vector3(sequence.Size.x, sequence.Size.x, sequence.Size.x);
                        MeshOffset = new Vector3(0, 0, 0);
                        BBox = new SBC();
                        break;
                    case "obb":
                        float cord = Mathf.Sqrt(sequence.Size.x * sequence.Size.x + sequence.Size.y * sequence.Size.y);
                        float sagitta = 1 - Mathf.Sqrt(1 - (cord/2) * (cord/2));
                        transform.localScale = new Vector3(
                            sequence.Size.x,
                            sequence.Size.y,
                            sagitta
                        );
                        MeshOffset = new Vector3(0, 0, 0);
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
        BBox.Position = Position;
        BBox.Size = transform.localScale;

        if (BBox is OBBox obbox)
        {
            obbox.Right = transform.right;
            obbox.Up = transform.up;
            obbox.Forward = transform.forward;
        }
        if (BBox is SOBR sobr)
        {
            sobr.Right = transform.right;
            sobr.Up = transform.up;
            sobr.Forward = transform.forward;
        }
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
