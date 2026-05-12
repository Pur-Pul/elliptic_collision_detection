using UnityEngine;
using UnityEngine.UI;

public class RuntimeMetricsButtonScript : MonoBehaviour
{
    public ControlScript control;
    public Button button;

    void Update()
    {
        if (
            control.runtimeRecord.records.Count == 0 ||
            control.Active || 
            control.step > 0
        ) { button.interactable = false; }
        else { button.interactable = true; }
    }
}
