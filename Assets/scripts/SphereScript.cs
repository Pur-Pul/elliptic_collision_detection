using UnityEngine;
using UnityEngine.SocialPlatforms;

public class SphereScript : MonoBehaviour, IItem
{   
    public Quaternion rotation;
    public BBox BBox { get; private set;}
    public controlScript control;
    public Material sphereMat;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public Color color;
    void Awake()
    {
        Renderer r = GetComponent<Renderer>();
        sphereMat = r.material; // 👈 this creates a unique instance
    }
    
    void Start()
    {
        BBox = new BBox(transform.position, transform.localScale.x);
        sphereMat.SetColor("_Color", color);
    }

    // Update is called once per frame
    void Update()
    {
        control.collisionTree.Remove(this);
        transform.position = rotation * transform.position;
        BBox.position = transform.position;
        control.collisionTree.Add(this);
        if (control.collisionTree.CheckCollisions(this))
        {
            //rotation = Quaternion.Inverse(rotation);
            sphereMat.SetColor("_Color", Color.red);
        } else
        {
            sphereMat.SetColor("_Color", color);
        }
    }
}
