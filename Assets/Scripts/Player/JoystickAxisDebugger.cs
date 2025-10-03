using UnityEngine;

public class JoystickAxisDebugger : MonoBehaviour
{
    void Update()
    {
        string report = "";
        for (int i = 1; i <= 10; i++) // check first 10 axes
        {
            float val = Input.GetAxis("Axis " + i);
            if (Mathf.Abs(val) > 0.1f) // show only active axes
                report += "Axis " + i + ": " + val.ToString("F2") + " | ";
        }
        if (!string.IsNullOrEmpty(report))
            Debug.Log(report);
    }
}