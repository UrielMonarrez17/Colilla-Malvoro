using System;
using UnityEngine;

public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance { get; private set; }

    public enum CycleState {
        Day,
        Night
    }

    [Header("Configuración del Tiempo")]
    [Tooltip("Duración total de un día de juego en minutos de la vida real.")]
    [SerializeField] private float realMinutesPerFullDay = 24f;

    [Tooltip("Hora inicial del juego (0 a 24).")]
    [Range(0f, 24f)]
    [SerializeField] private float startingHour = 0f;

    [Header("Umbrales del Ciclo (Horas 0-24)")]
    [Range(0f, 24f)]
    [SerializeField] private float sunriseHour = 18f;

    [Range(0f, 24f)]
    [SerializeField] private float nightfallHour = 18f;

    private float currentInGameHour;


    public int CurrentHour => Mathf.FloorToInt(currentInGameHour);
    public int CurrentMinute => Mathf.FloorToInt((currentInGameHour - CurrentHour) * 60f);
    public float CurrentHourFloat => currentInGameHour;

    public float NormalizedTime => currentInGameHour / 24f;

    public CycleState CurrentCycleState => IsDay() ? CycleState.Day : CycleState.Night;

    // Eventos opcionales por si otros scripts necesitan reaccionar a cambios de estado
    public event Action<CycleState> OnCycleStateChanged;
    private CycleState previousState;

    [Header("Debug")]
    [SerializeField] private bool showDebugLog = false;
    private float logTimer = 0f;
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        currentInGameHour = Mathf.Clamp(startingHour, 0f, 24f);
        previousState = CurrentCycleState;
    }

    private void Update()
    {
        AdvanceTime();
        CheckCycleChange();
        
        if (showDebugLog)
        {
            logTimer += Time.deltaTime;
            if (logTimer >= 1f)
            {
                logTimer = 0f;
                Debug.Log($"[TimeManager] Hora: {CurrentHour:D2}:{CurrentMinute:D2} | Estado: {CurrentCycleState} | Normalizado: {NormalizedTime:F2}");
            }
        }
    }

    private void AdvanceTime()
    {
        // Factores de conversión:
        // realMinutesPerFullDay minutos reales = 24 horas de juego
        // (realMinutesPerFullDay * 60) segundos reales = 24 horas de juego
        float inGameHoursPerRealSecond = 24f / (realMinutesPerFullDay * 60f);
        currentInGameHour += Time.deltaTime * inGameHoursPerRealSecond;

        if (currentInGameHour >= 24f)
        {
            currentInGameHour %= 24f;
        }
    }

    private bool IsDay() {
        if (sunriseHour < nightfallHour)
        {
            // Caso estándar (ej. amanece 6:00, anochece 18:00)
            return currentInGameHour >= sunriseHour && currentInGameHour < nightfallHour;
        }
        
        return currentInGameHour >= sunriseHour || currentInGameHour < nightfallHour;
    }

    private void CheckCycleChange() {
        CycleState currentState = CurrentCycleState;
        if (currentState != previousState)
        {
            previousState = currentState;
            OnCycleStateChanged?.Invoke(currentState);
        }
    }

    public void SetTime() {
        currentInGameHour = Mathf.Clamp(6, 0f, 24f) % 24f;
    }

    public void SetDayDuration(float realMinutes) {
        realMinutesPerFullDay = Mathf.Max(0.1f, realMinutes);
    }
}