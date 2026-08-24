using System;
using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    private void Update()
    {
        if (TimeManager.Instance == null) return;
        float rotationAngle = (TimeManager.Instance.NormalizedTime * 360f) - 90f;
        
        transform.rotation = Quaternion.Euler(rotationAngle, 0f, 0f);
    }
}
