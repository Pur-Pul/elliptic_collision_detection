using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
public class BodyScript : MonoBehaviour, IItem
{   
    //public BBox BBox { get; private set;}
    public IBoundingVolume BBox { get; set; }
    public ControlScript control;
    public Material bodyMat;
    public Sequence sequence;
    public Color color;
    public Color drawColor;
    private int prevStep;
    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;
    void Awake()
    {
        Renderer r = GetComponent<Renderer>();
        meshRenderer = GetComponent<MeshRenderer>();
        meshFilter = GetComponent<MeshFilter>();
        bodyMat = r.material;
        prevStep = -1;
    }
    
    void Start()
    {
        control.collisionTree.Add(this);
        bodyMat.SetColor("_Color", color);
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
    }

    void Move()
    {
        transform.position = sequence.SlerpPosition(control.step);
        transform.rotation = sequence.SlerpOrientation(control.step);
        BBox.Position = transform.position;
        BBox.Width = transform.localScale.x;

        if (BBox is OBBox obbox)
        {
            obbox.Right = transform.right;
            obbox.Up = transform.up;
            obbox.Forward = transform.forward;
        }
    }

    public void Reset()
    {
        control.collisionTree.Remove(this);
        prevStep = -1;
        sequence.Reset();
        Move();
        control.collisionTree.Add(this);
    }

    void Update()
    {
        
        if (prevStep != control.step)
        {
            control.collisionTree.Remove(this);

            Move();
            control.collisionTree.Add(this);
            
            prevStep = control.step;
        }
        
    }
    void LateUpdate()
    {
        if (control.collisionTree.CheckCollisions(this))
        {
            bodyMat.SetColor("_Color", Color.red);
            drawColor = Color.red;
        } else
        {
            bodyMat.SetColor("_Color", color);
            drawColor = color;
        }
    }

    void OnDestroy()
    {
        control.collisionTree.Remove(this);
    }
}
