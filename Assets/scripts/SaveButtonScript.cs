using UnityEngine;
using UnityEngine.UI;

public class SaveButtonScript : MonoBehaviour
{
    public ControlScript control;
    public Button button;

    void Update()
    {
        if (control.Active || control.step > 0 || control.currentSequence != null) { button.interactable = false; }
        else { button.interactable = true; }
    }
}
