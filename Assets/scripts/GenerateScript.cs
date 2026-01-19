using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GenerateScript : MonoBehaviour
{
    public TMP_InputField bodyNumperInput;
    public void Generate()
    {
        string t = bodyNumperInput.text;
        Debug.Log(t);
    }
}
