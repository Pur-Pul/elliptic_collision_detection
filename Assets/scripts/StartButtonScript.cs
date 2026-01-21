using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StartButtonScript : MonoBehaviour
{
    public ControlScript control;
    public TMP_Text text;
    public Button button;
    public void ToggleActive()
    {
        control.active = !control.active;
    }
    void Update()
    {
        if (!control.Ready()) { button.interactable = false; }
        else { button.interactable = true; }
        if (control.active)
        {
            text.text = "Pause";
        } else if (!control.active && control.step > 0)
        {
            text.text = "Continue";
        } else
        {
            text.text = "Start";
        }
    }
}
