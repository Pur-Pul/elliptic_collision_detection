using UnityEngine;
using UnityEngine.UI;

public class AccuracyMetricsButtonScript : MonoBehaviour
{
    public ControlScript control;
    public Button button;

    void Update()
    {
        if (
            string.IsNullOrWhiteSpace(control.baselineList.method) || 
            string.IsNullOrWhiteSpace(control.artifactList.method) ||
            control.Active || 
            control.step > 0
        ) { button.interactable = false; }
        else { button.interactable = true; }
    }
}
