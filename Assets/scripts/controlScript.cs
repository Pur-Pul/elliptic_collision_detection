using UnityEngine;

public class controlScript : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public Octree<rotScript> collisionTree;
    void Start()
    {
        collisionTree = new Octree<rotScript>(Vector3.zero, 50);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
