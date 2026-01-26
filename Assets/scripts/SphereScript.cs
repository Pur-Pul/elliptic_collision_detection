using UnityEngine;

public class SphereScript : MonoBehaviour, IItem
{   
    //public BBox BBox { get; private set;}
    public IBoundingVolume BBox { get; set; }
    public ControlScript control;
    public Material sphereMat;
    public Sequence sequence;
    public Color color;
    private int prevStep;
    void Awake()
    {
        Renderer r = GetComponent<Renderer>();
        sphereMat = r.material;
        prevStep = -1;
    }
    
    void Start()
    {
        control.collisionTree.Add(this);
        sphereMat.SetColor("_Color", color);
    }

    void Move()
    {
        transform.position = sequence.SlerpGet(control.step);
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
            sphereMat.SetColor("_Color", Color.red);
        } else
        {
            sphereMat.SetColor("_Color", color);
        }
    }

    void OnDestroy()
    {
        control.collisionTree.Remove(this);
    }
}
