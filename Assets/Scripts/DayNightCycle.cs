using System;
using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    private Light sunLight;

    private void Awake()
    {
        sunLight = GetComponent<Light>();
    }

    private void Update()
    {
        if (TimeManager.Instance == null) return;
        float norm = TimeManager.Instance.NormalizedTime;
        float rotationAngle = (norm * 360f) - 90f;
        
        transform.rotation = Quaternion.Euler(rotationAngle, -30f, 0f);

        if (sunLight != null)
        {
            float hour = TimeManager.Instance.CurrentHourFloat;
            // Transición atmosférica entre Amanecer (6h), Día (12h), Atardecer (17h-18h) y Noche (18h-6h)
            if (hour >= 6f && hour < 16.5f)
            {
                // Día soleado
                sunLight.color = Color.Lerp(sunLight.color, new Color(1f, 0.96f, 0.88f), Time.deltaTime * 2f);
                sunLight.intensity = Mathf.Lerp(sunLight.intensity, 1.85f, Time.deltaTime * 2f);
                RenderSettings.ambientLight = Color.Lerp(RenderSettings.ambientLight, new Color(0.55f, 0.58f, 0.65f), Time.deltaTime * 2f);
            }
            else if (hour >= 16.5f && hour < 18f)
            {
                // Atardecer / Anuncio del Anochecer (tonos naranjas/rojizos)
                float t = (hour - 16.5f) / 1.5f;
                Color duskColor = Color.Lerp(new Color(1f, 0.72f, 0.38f), new Color(0.85f, 0.32f, 0.22f), t);
                sunLight.color = Color.Lerp(sunLight.color, duskColor, Time.deltaTime * 3f);
                sunLight.intensity = Mathf.Lerp(sunLight.intensity, Mathf.Lerp(1.4f, 0.5f, t), Time.deltaTime * 3f);
                RenderSettings.ambientLight = Color.Lerp(RenderSettings.ambientLight, new Color(0.38f, 0.25f, 0.28f), Time.deltaTime * 3f);
            }
            else
            {
                // Noche Feral (oscuridad tensa con luna azulada/rojiza)
                sunLight.color = Color.Lerp(sunLight.color, new Color(0.22f, 0.28f, 0.48f), Time.deltaTime * 3f);
                sunLight.intensity = Mathf.Lerp(sunLight.intensity, 0.22f, Time.deltaTime * 3f);
                RenderSettings.ambientLight = Color.Lerp(RenderSettings.ambientLight, new Color(0.11f, 0.13f, 0.22f), Time.deltaTime * 3f);
            }
        }
    }
}
