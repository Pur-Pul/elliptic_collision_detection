using UnityEngine;
using UnityEngine.SocialPlatforms;

public class SphereScript : MonoBehaviour, IItem
{   
    public Quaternion rotation;
    public BBox BBox { get; private set;}
    public controlScript control;
    public Material sphereMat;

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
        BBox = new BBox(transform.position, transform.localScale.x);
        control.collisionTree.Add(this);
        sphereMat.SetColor("_Color", color);
    }

    void Update()
    {
        if (prevStep < control.step)
        {
            control.collisionTree.Remove(this);
            transform.position = rotation * transform.position;
            BBox.position = transform.position;
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
}
