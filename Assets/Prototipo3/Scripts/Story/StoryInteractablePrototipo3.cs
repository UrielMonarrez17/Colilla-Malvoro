using System;
using UnityEngine;

namespace Prototipo3
{
    public enum StoryInteractableKind
    {
        MarcoNPC,
        TobyNPC,
        SchoolCityEntranceHole,
        SchoolPandemicClueLab,
        SchoolEscapeHoleToCity,
        GymFloodlightSwitch
    }

    /// <summary>
    /// Componente de interacción contextual [F] para NPCs y puntos clave de la Historia Lineal en Prototipo 3.
    /// </summary>
    public class StoryInteractablePrototipo3 : MonoBehaviour
    {
        public StoryInteractableKind kind;
        public string customPrompt = "";
        public float cooldownAfterUse = 0.6f;

        private float nextUseTime = 0f;
        private Light linkedSpotlight;
        private float switchCooldownRemaining = 0f;

        private void Awake()
        {
            if (!gameObject.CompareTag("Interactable") && !gameObject.CompareTag("Door"))
            {
                gameObject.tag = "Interactable";
            }

            if (kind == StoryInteractableKind.GymFloodlightSwitch)
            {
                linkedSpotlight = GetComponentInChildren<Light>(true);
            }
        }

        private void Update()
        {
            if (switchCooldownRemaining > 0f)
            {
                switchCooldownRemaining = Mathf.Max(0f, switchCooldownRemaining - Time.deltaTime);
                if (linkedSpotlight != null && switchCooldownRemaining <= 8.5f)
                {
                    linkedSpotlight.intensity = 1.2f;
                }
            }
        }

        public string GetPromptText()
        {
            if (!string.IsNullOrEmpty(customPrompt)) return customPrompt;

            switch (kind)
            {
                case StoryInteractableKind.MarcoNPC:
                    return "[ F ]  Hablar con Marco el Tejón";

                case StoryInteractableKind.TobyNPC:
                    return "[ F ]  Hablar con Toby el Mapache";

                case StoryInteractableKind.SchoolCityEntranceHole:
                    if (StoryDirectorPrototipo3.Instance != null && !StoryDirectorPrototipo3.Instance.CanEnterSchoolFromCity)
                        return "Alcantarilla sellada hacia la High School (Investiga primero la ciudad)";
                    return StoryDirectorPrototipo3.Instance != null && StoryDirectorPrototipo3.Instance.CurrentPhase == StoryPhase.EnterSchoolFinalBattle
                        ? "[ F ]  Bajar por la alcantarilla a la Cancha de Básquet (Batalla Final)"
                        : "[ F ]  Bajar por la alcantarilla hacia la High School";

                case StoryInteractableKind.SchoolPandemicClueLab:
                    return "[ F ]  Tomar papeles confidenciales de Helios-9";

                case StoryInteractableKind.SchoolEscapeHoleToCity:
                    return "[ F ]  Escapar por la alcantarilla hacia la Ciudad";

                case StoryInteractableKind.GymFloodlightSwitch:
                    return switchCooldownRemaining > 0f
                        ? $"Reflector UV recargando ({switchCooldownRemaining:F0}s)"
                        : "[ F ]  Sobrecargar Reflector UV (Derriba al Búho)";

                default:
                    return "[ F ]  Interactuar";
            }
        }

        public void Interact(HamsterController player)
        {
            if (Time.unscaledTime < nextUseTime) return;
            nextUseTime = Time.unscaledTime + cooldownAfterUse;

            if (kind == StoryInteractableKind.GymFloodlightSwitch)
            {
                TriggerGymFloodlight();
                return;
            }

            if (StoryDirectorPrototipo3.Instance != null)
            {
                StoryDirectorPrototipo3.Instance.HandleInteractableUsed(this, player);
            }
        }

        private void TriggerGymFloodlight()
        {
            if (switchCooldownRemaining > 0f) return;

            OwlRectorBossAI owl = FindAnyObjectByType<OwlRectorBossAI>();
            if (owl != null && !owl.IsDefeated)
            {
                // Requiere que el Búho no esté demasiado lejos del reflector (<= 19m) para premiar el posicionamiento táctico!
                float distToOwl = Vector2.Distance(
                    new Vector2(transform.position.x, transform.position.z),
                    new Vector2(owl.transform.position.x, owl.transform.position.z));

                if (distToOwl > 20.5f)
                {
                    if (UI.FeralGameUI.Instance != null)
                        UI.FeralGameUI.Instance.ShowLootFeedback("¡El Búho está muy lejos de este reflector! Espera a que vuele más cerca o ve a su esquina.", 2.4f);
                    return;
                }

                switchCooldownRemaining = 22f;
                if (linkedSpotlight != null)
                {
                    linkedSpotlight.color = new Color(0.25f, 0.92f, 1f, 1f);
                    linkedSpotlight.intensity = 15f;
                }

                BossHudPrototipo3.EnsureInstance().TriggerFlash(new Color(0.25f, 0.95f, 1f), 0.35f);
                owl.ForceDazzleGrounding(2.7f, "¡Reflector UV impactó! ¡Cayó a la duela: DISPÁRALE UNA RED YA [CLICK IZQ / E]!");
            }
        }
    }

    /// <summary>
    /// Detector en las salidas del edificio del Mapache: registra tanto instalar trampa en su PuertaTrampa como desplegar [1/2/3] en el umbral.
    /// </summary>
    public class TobyExitTrapZonePrototipo3 : MonoBehaviour
    {
        public float detectionRadius = 5.2f;

        private void OnEnable()
        {
            PuertaTrampa.OnTrapInstalledOnDoor += OnDoorTrapped;
            TrapSystemPrototipo3.OnProto3TrapDeployed += OnTacticalTrapDeployed;
        }

        private void OnDisable()
        {
            PuertaTrampa.OnTrapInstalledOnDoor -= OnDoorTrapped;
            TrapSystemPrototipo3.OnProto3TrapDeployed -= OnTacticalTrapDeployed;
        }

        private void OnDoorTrapped(PuertaTrampa door, TipoTrampa tipo)
        {
            if (door == null || StoryDirectorPrototipo3.Instance == null) return;
            if (door.transform.IsChildOf(transform) || Vector3.Distance(door.transform.position, transform.position) <= detectionRadius + 3.5f)
            {
                StoryDirectorPrototipo3.Instance.NotifyTrapPlacedAtTobyExit(tipo);
            }
        }

        private void OnTacticalTrapDeployed(TipoTrampa tipo)
        {
            if (StoryDirectorPrototipo3.Instance == null) return;
            HamsterController hc = FindAnyObjectByType<HamsterControllerPrototipo3>() ?? FindAnyObjectByType<HamsterController>();
            if (hc != null && Vector3.Distance(hc.transform.position, transform.position) <= detectionRadius + 2.5f)
            {
                StoryDirectorPrototipo3.Instance.NotifyTrapPlacedAtTobyExit(tipo);
            }
        }
    }
}
