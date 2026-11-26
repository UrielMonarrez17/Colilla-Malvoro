using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    [Header("Día (06:00 - 16:30)")]
    public Color daySunColor = new Color(1f, 0.96f, 0.88f);
    [Range(0f, 4f)] public float daySunIntensity = 1.85f;
    public Color dayAmbientColor = new Color(0.55f, 0.58f, 0.65f);

    [Header("Atardecer (16:30 - 18:00)")]
    public Color duskStartColor = new Color(1f, 0.72f, 0.38f);
    public Color duskEndColor = new Color(0.85f, 0.32f, 0.22f);
    [Range(0f, 3f)] public float duskStartIntensity = 1.4f;
    [Range(0f, 3f)] public float duskEndIntensity = 0.5f;
    public Color duskAmbientColor = new Color(0.38f, 0.25f, 0.28f);

    [Header("Noche / Luna (18:00 - 06:00)")]
    [Tooltip("Multiplicador rápido de brillo nocturno general (1 = por defecto, >1 = noche más iluminada, <1 = noche más oscura).")]
    [Range(0.05f, 4.0f)] public float nightBrightnessMultiplier = 1.0f;
    public Color nightMoonColor = new Color(0.22f, 0.28f, 0.48f);
    [Range(0f, 3f)] public float nightMoonIntensity = 0.22f;
    public Color nightAmbientColor = new Color(0.11f, 0.13f, 0.22f);

    [Header("Orientación Solar y Lunar")]
    [Tooltip("Mantiene la luz direccional apuntando desde arriba como luz de luna en la noche en vez de girar por debajo del suelo.")]
    public bool keepOverheadMoonAngleAtNight = true;
    [Range(20f, 80f)] public float nightMoonPitchAngle = 48f;
    public float sunYawAngle = -30f;

    private Light sunLight;

    private void Awake()
    {
        sunLight = GetComponent<Light>();
    }

    private void Update()
    {
        if (TimeManager.Instance == null) return;

        float hour = TimeManager.Instance.CurrentHourFloat;
        bool isDayOrDusk = hour >= 6f && hour < 18f;

        Quaternion targetRotation;
        if (isDayOrDusk || !keepOverheadMoonAngleAtNight)
        {
            float pitch = isDayOrDusk
                ? Mathf.Lerp(8f, 172f, (hour - 6f) / 12f)
                : (TimeManager.Instance.NormalizedTime * 360f) - 90f;
            targetRotation = Quaternion.Euler(pitch, sunYawAngle, 0f);
        }
        else
        {
            targetRotation = Quaternion.Euler(nightMoonPitchAngle, sunYawAngle + 180f, 0f);
        }

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 4f);

        if (sunLight == null) return;

        // Transición atmosférica entre Amanecer (6h), Día (12h), Atardecer (16:30-18h) y Noche (18h-6h)
        if (hour >= 6f && hour < 16.5f)
        {
            sunLight.color = Color.Lerp(sunLight.color, daySunColor, Time.deltaTime * 2f);
            sunLight.intensity = Mathf.Lerp(sunLight.intensity, daySunIntensity, Time.deltaTime * 2f);
            RenderSettings.ambientLight = Color.Lerp(RenderSettings.ambientLight, dayAmbientColor, Time.deltaTime * 2f);
        }
        else if (hour >= 16.5f && hour < 18f)
        {
            float t = (hour - 16.5f) / 1.5f;
            Color duskColor = Color.Lerp(duskStartColor, duskEndColor, t);
            sunLight.color = Color.Lerp(sunLight.color, duskColor, Time.deltaTime * 3f);
            sunLight.intensity = Mathf.Lerp(sunLight.intensity, Mathf.Lerp(duskStartIntensity, duskEndIntensity, t), Time.deltaTime * 3f);
            RenderSettings.ambientLight = Color.Lerp(RenderSettings.ambientLight, duskAmbientColor, Time.deltaTime * 3f);
        }
        else
        {
            float mult = Mathf.Max(0.05f, nightBrightnessMultiplier);
            Color scaledAmbient = new Color(
                Mathf.Clamp01(nightAmbientColor.r * mult),
                Mathf.Clamp01(nightAmbientColor.g * mult),
                Mathf.Clamp01(nightAmbientColor.b * mult),
                1f);

            sunLight.color = Color.Lerp(sunLight.color, nightMoonColor, Time.deltaTime * 3f);
            sunLight.intensity = Mathf.Lerp(sunLight.intensity, nightMoonIntensity * mult, Time.deltaTime * 3f);
            RenderSettings.ambientLight = Color.Lerp(RenderSettings.ambientLight, scaledAmbient, Time.deltaTime * 3f);
        }
    }
}
