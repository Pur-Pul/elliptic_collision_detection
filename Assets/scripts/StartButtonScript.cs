using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StartButtonScript : MonoBehaviour
{
    public controlScript control;
    public TMP_Text text;
    public void ToggleActive()
    {
        control.active = !control.active;
        if (control.active)
        {
            text.text = "Pause";
        } else
        {
            text.text = "Start";
        }
    }
}
