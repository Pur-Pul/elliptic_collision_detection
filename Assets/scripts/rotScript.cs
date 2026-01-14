using UnityEngine;
using UnityEngine.SocialPlatforms;

public class rotScript : MonoBehaviour
{   
    public Quaternion rotation;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = rotation * transform.position;
    }
}
