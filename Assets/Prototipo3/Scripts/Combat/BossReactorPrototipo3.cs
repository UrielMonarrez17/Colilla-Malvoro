using System;
using System.Collections;
using UnityEngine;
using TMPro;

namespace Prototipo3.Combat
{
    [SelectionBase]
    public class BossReactorPrototipo3 : MonoBehaviour
    {
        [Header("Configuración del Reactor")]
        public int totalSwitches = 3;
        public int switchesActivated = 0;
        public bool isOverloaded = false;

        [Header("Referencias Visuales")]
        public Light hazardLight;
        public ParticleSystem toxicSmokeParticles;
        public ParticleSystem explosionParticles;

        private GameObject promptRootObj;
        private TextMeshPro statusTextMesh;
        private float lastInteractTime = -10f;
        private bool introCinematicTriggered = false;

        public event Action OnReactorDestroyed;

        private void Awake()
        {
            CreateStatusUI();
            if (hazardLight == null)
            {
                var lightObj = new GameObject("HazardLight");
                lightObj.transform.SetParent(this.transform);
                lightObj.transform.localPosition = new Vector3(0f, 3f, 0f);
                hazardLight = lightObj.AddComponent<Light>();
                hazardLight.type = LightType.Point;
                hazardLight.color = new Color(1f, 0.1f, 0.1f);
                hazardLight.range = 15f;
                hazardLight.intensity = 3f;
            }
        }

        private void Update()
        {
            if (isOverloaded) return;

            // Pulso de luz roja del reactor
            if (hazardLight != null)
            {
                hazardLight.intensity = 2f + Mathf.PingPong(Time.time * 4f, 3f);
            }

            var player = FindFirstObjectByType<Prototipo3.Player.HamsterControllerPrototipo3>();
            if (player == null) return;

            float dist = Vector3.Distance(transform.position, player.transform.position);

            if (!introCinematicTriggered && dist <= 12f)
            {
                introCinematicTriggered = true;
                if (Prototipo3.Cinematics.CinematicsManagerPrototipo3.Instance != null)
                {
                    Prototipo3.Cinematics.CinematicsManagerPrototipo3.Instance.PlayCinematic("Traicion_Lulu_Prototipo3");
                }
            }

            if (statusTextMesh != null && Camera.main != null)
            {
                promptRootObj.transform.rotation = Camera.main.transform.rotation;
            }

            // Si está cerca, permitir accionar con [F]
            if (dist <= 4.5f && (Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.E)))
            {
                if (Time.time - lastInteractTime > 0.6f)
                {
                    lastInteractTime = Time.time;
                    ActivateSwitch();
                }
            }
        }

        public void ActivateSwitch()
        {
            if (isOverloaded) return;

            switchesActivated++;
            Debug.Log($"[BossReactorPrototipo3] Válvula de enfriamiento accionada: {switchesActivated}/{totalSwitches}");

            UpdateStatusText($"¡Sobrecarga: {switchesActivated}/{totalSwitches}!");

            if (switchesActivated >= totalSwitches)
            {
                StartCoroutine(OverloadReactorRoutine());
            }
        }

        public void Interactuar(Prototipo3.Player.HamsterControllerPrototipo3 player)
        {
            ActivateSwitch();
        }

        private IEnumerator OverloadReactorRoutine()
        {
            isOverloaded = true;
            UpdateStatusText("¡REACTOR CARMESÍ COLAPSANDO!");
            Debug.Log("[BossReactorPrototipo3] ¡Reactor destruido!");

            if (hazardLight != null)
            {
                hazardLight.color = Color.white;
                hazardLight.intensity = 10f;
            }

            yield return new WaitForSeconds(1.5f);

            OnReactorDestroyed?.Invoke();

            if (Prototipo3.Narrative.StoryManagerPrototipo3.Instance != null)
            {
                Prototipo3.Narrative.StoryManagerPrototipo3.Instance.OnBossReactorDefeated();
            }
        }

        private void CreateStatusUI()
        {
            promptRootObj = new GameObject("ReactorStatusUI");
            promptRootObj.transform.SetParent(this.transform);
            promptRootObj.transform.localPosition = new Vector3(0f, 3.8f, 0f);

            statusTextMesh = promptRootObj.AddComponent<TextMeshPro>();
            statusTextMesh.text = "REACTOR CARMESÍ\n<size=70%>[F] Sabotear Válvula (0/3)</size>";
            statusTextMesh.fontSize = 4.0f;
            statusTextMesh.alignment = TextAlignmentOptions.Center;
            statusTextMesh.color = new Color(1f, 0.3f, 0.3f, 1f);
        }

        private void UpdateStatusText(string msg)
        {
            if (statusTextMesh != null)
            {
                statusTextMesh.text = $"REACTOR CARMESÍ\n<size=70%>{msg}</size>";
            }
        }
    }
}
