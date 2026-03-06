using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StartButtonScript : MonoBehaviour
{
    public ControlScript control;
    public TMP_Text text;
    public TMP_InputField OptimizeInput;
    public Button button;
    public void ToggleActive()
    {
        if (!int.TryParse(OptimizeInput.text, out int number))
        {
            number = 0;
        }
        control.Optimize = number;
        control.Active = !control.Active;
    }
    void Update()
    {
        if (!control.Ready()) { button.interactable = false; }
        else { button.interactable = true; }
        if (control.Active)
        {
            text.text = "Pause";
        } else if (!control.Active && control.step > 0)
        {
            text.text = "Continue";
        } else
        {
            text.text = "Start";
        }
    }
}
