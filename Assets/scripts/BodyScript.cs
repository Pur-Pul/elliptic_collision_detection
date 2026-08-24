using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class BodyScript : MonoBehaviour, IItem
{   
    public IBoundingVolume BBox { get; set; }
    public ICollisionTree CollisionNode { get; set; }
    public ControlScript control;
    public Material bodyMat;
    public Sequence sequence;
    public Color color;
    public Color drawColor;
    public int Id { get => sequence.Id; }
    public MeshRenderer MeshRend;
    private MeshFilter meshFilter;
    public string BodyType {
        get => sequence.BodyType;
    }
    void Awake()
    {
        Renderer r = GetComponent<Renderer>();
        MeshRend = GetComponent<MeshRenderer>();
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
                MeshRend.enabled = control.RenderInput.isOn;
                switch (sequence.BodyType)
                {
                    case "circle":
                        GameObject tempSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        meshFilter.mesh = tempSphere.GetComponent<MeshFilter>().sharedMesh;
                        Destroy(tempSphere);
                        BBox = new BBoxSphere(Id);
                        break;
                    case "rectangle":
                        GameObject tempCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        meshFilter.mesh = tempCube.GetComponent<MeshFilter>().sharedMesh;
                        Destroy(tempCube);
                        
                        BBox = new OBBox(Id);
                        break;
                }
                break;
            case 1:
                MeshRend.enabled = false;
                switch (sequence.BodyType)
                {
                    case "circle":
                        BBox = new SBC(Id);
                        break;
                    case "rectangle":
                        BBox = new SOBR(Id);
                        break;
                }
                break;
            case 2:
                MeshRend.enabled = false;
                switch (sequence.BodyType)
                {
                    case "circle":
                        BBox = new SBCA1(Id);
                        break;
                    case "rectangle":
                        BBox = new SOBRA1(Id);
                        break;
                }
                break;
            case 3:
                MeshRend.enabled = false;
                switch (sequence.BodyType)
                {
                    case "circle":
                        BBox = new SBCA2(Id);
                        break;
                    case "rectangle":
                        BBox = new SOBRA2(Id);
                        break;
                }
                break;
        }
        BBox.Record = control.runtimeRecord;
        UpdateBBox();
    }

    public void UpdateBBox (bool timed = false)
    {
        BBox.Update(sequence.Size, transform.rotation, timed);
        transform.position = BBox.Position;
        transform.localScale = BBox.Size;
    }

    public bool Move(int step)
    {
        Quaternion newRotation = sequence.SlerpOrientation(step);

        if (newRotation == transform.rotation) { return false; }

        transform.rotation = newRotation;
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

    public void Update()
    {
        bodyMat.SetColor("_BaseColor", color);
        drawColor = color;
    }

    public void Collision()
    {
        bodyMat.SetColor("_BaseColor", Color.red);
        drawColor = Color.red;
    }

    void OnDestroy()
    {
        control.collisionTree.Remove(this);
    }
}
