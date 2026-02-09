using UnityEngine;
using UnityEngine.InputSystem;

public class CameraScript : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Quaternion upRot = Quaternion.AngleAxis(0.25f, transform.right);
        Quaternion rightRot = Quaternion.AngleAxis(0.25f, transform.up);
        
        if (Keyboard.current.upArrowKey.IsPressed())
        {
            transform.position = upRot * transform.position;
            transform.rotation = upRot * transform.rotation;
        }
        if (Keyboard.current.downArrowKey.IsPressed())
        {
            transform.position = Quaternion.Inverse(upRot) * transform.position;
            transform.rotation = Quaternion.Inverse(upRot) * transform.rotation;
        }
        if (Keyboard.current.leftArrowKey.IsPressed())
        {
            transform.position = rightRot * transform.position;
            transform.rotation = rightRot * transform.rotation;
        }
        if (Keyboard.current.rightArrowKey.IsPressed())
        {
            transform.position = Quaternion.Inverse(rightRot) * transform.position;
            transform.rotation = Quaternion.Inverse(rightRot) * transform.rotation;
        }
    }
}
