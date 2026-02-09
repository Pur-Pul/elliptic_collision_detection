using UnityEngine;

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
    void Awake()
    {
        Renderer r = GetComponent<Renderer>();
        bodyMat = r.material;
        prevStep = -1;
    }
    
    void Start()
    {
        control.collisionTree.Add(this);
        bodyMat.SetColor("_Color", color);
        drawColor = color;
        //GetComponent<MeshRenderer>().enabled= false;
    }

    void Move()
    {
        transform.position = sequence.SlerpPosition(control.step);
        transform.rotation = sequence.SlerpOrientation(control.step);
        BBox.Position = transform.position;
        BBox.Width = transform.localScale.x;
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
