using UnityEngine;
using UnityEngine.SocialPlatforms;

public class rotScript : MonoBehaviour, IItem
{   
    public Quaternion rotation;
    public BBox BBox { get; private set;}
    public controlScript control;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        BBox = new BBox(transform.position, transform.localScale.x);
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
            rotation = Quaternion.Inverse(rotation);
        }
    }
}
