using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Prototipo3.UI;

namespace Prototipo3
{
    /// <summary>
    /// Gestiona la fachada y puerta de acceso a un edificio especial (ej. Taller de Robótica de Toby).
    /// Sirve como punto de anclaje para la interacción con [F], mostrando retroalimentación en el HUD
    /// y preparando la transición fluida a la escena interior cuando esté disponible.
    /// </summary>
    [SelectionBase]
    public class SpecialBuildingEntrance : MonoBehaviour
    {
        public static readonly List<SpecialBuildingEntrance> ActiveEntrances = new List<SpecialBuildingEntrance>();

        [Header("Identificación")]
        [Tooltip("Identificador único del edificio (ej. TallerToby)")]
        public string buildingId = "TallerToby";

        [Tooltip("Nombre legible para el HUD")]
        public string buildingDisplayName = "Taller de Robótica de Toby";

        [Header("Escena Interior Futura")]
        [Tooltip("Nombre de la escena interior a cargar al ingresar")]
        public string targetSceneName = "TallerToby_Interior";

        [Header("Punto de Acceso")]
        [Tooltip("Transform del socket frontal o punto de puerta")]
        public Transform entranceSocket;

        [Tooltip("Radio de proximidad para interactuar directamente")]
        public float interactionRadius = 2.2f;

        [Header("NPC Residente")]
        [Tooltip("Referencia al NPC asignado a este edificio (ej. Toby)")]
        public DualRoleCharacter residentNPC;

        [Header("Referencias Opcionales")]
        [Tooltip("Transform del jugador para chequeos de proximidad")]
        public Transform playerTransform;

        [Tooltip("Objeto raíz del indicador visual/prompt de la puerta")]
        public GameObject promptRoot;

        private Collider triggerCollider;
        private float lastInteractionTime = -10f;

        private void OnEnable()
        {
            if (!ActiveEntrances.Contains(this))
            {
                ActiveEntrances.Add(this);
            }
        }

        private void OnDisable()
        {
            ActiveEntrances.Remove(this);
        }

        private void Awake()
        {
            EnsureEntranceSocket();
            EnsureTriggerCollider();
        }

        private void Start()
        {
            EnsureEntranceSocket();
            EnsureTriggerCollider();
        }

        /// <summary>
        /// Configura los parámetros clave del edificio y su punto de entrada.
        /// </summary>
        public void Configure(string id, string displayName, string sceneName, Transform socket, DualRoleCharacter resident = null)
        {
            buildingId = id;
            buildingDisplayName = displayName;
            targetSceneName = sceneName;
            entranceSocket = socket;
            residentNPC = resident;

            EnsureEntranceSocket();
            EnsureTriggerCollider();
        }

        private void EnsureEntranceSocket()
        {
            if (entranceSocket == null)
            {
                CitySocket cs = GetComponent<CitySocket>();
                if (cs == null)
                {
                    cs = GetComponentInChildren<CitySocket>();
                }

                if (cs != null && cs.frontSocket != null)
                {
                    entranceSocket = cs.frontSocket;
                }
                else
                {
                    Transform front = transform.Find("FrontSocket");
                    if (front == null) front = transform.Find("Door");
                    if (front == null) front = transform.Find("Entrance");
                    entranceSocket = front != null ? front : transform;
                }
            }
        }

        private void EnsureTriggerCollider()
        {
            if (triggerCollider == null)
            {
                Transform targetParent = entranceSocket != null ? entranceSocket : transform;
                Transform trigChild = targetParent.Find("EntranceTriggerZone");
                GameObject trigObj;
                if (trigChild == null)
                {
                    trigObj = new GameObject("EntranceTriggerZone");
                    trigObj.transform.SetParent(targetParent, false);
                    trigObj.transform.localPosition = Vector3.zero;
                }
                else
                {
                    trigObj = trigChild.gameObject;
                }

                try
                {
                    trigObj.tag = "Door";
                }
                catch (Exception)
                {
                    // Tag Door no registrado o no accesible
                }

                BoxCollider box = trigObj.GetComponent<BoxCollider>();
                if (box == null)
                {
                    box = trigObj.AddComponent<BoxCollider>();
                }
                box.isTrigger = true;
                box.size = new Vector3(interactionRadius, 2.5f, interactionRadius);
                box.center = new Vector3(0f, 1.2f, 0f);
                triggerCollider = box;
            }
        }

        private void EnsurePlayerReference()
        {
            if (playerTransform == null || !playerTransform.gameObject.activeInHierarchy)
            {
                HamsterControllerPrototipo3 hc3 = FindAnyObjectByType<HamsterControllerPrototipo3>();
                if (hc3 != null) { playerTransform = hc3.transform; return; }
                HamsterController hc = FindAnyObjectByType<HamsterController>();
                if (hc != null) playerTransform = hc.transform;
            }
        }

        private void Update()
        {
            EnsurePlayerReference();

            if (residentNPC != null && playerTransform != null)
            {
                float distToResident = Vector3.Distance(playerTransform.position, residentNPC.transform.position);
                if (distToResident <= residentNPC.interactDistance)
                {
                    if (promptRoot != null) promptRoot.SetActive(false);
                    return;
                }
            }

            if (playerTransform != null)
            {
                Vector3 doorPos = entranceSocket != null ? entranceSocket.position : transform.position;
                float distToDoor = Vector3.Distance(playerTransform.position, doorPos);
                bool inRange = distToDoor <= interactionRadius;

                if (promptRoot != null && promptRoot.activeSelf != inRange)
                {
                    promptRoot.SetActive(inRange);
                }

                if (inRange && (FeralInput.Pressed(KeyCode.F) || Input.GetKeyDown(KeyCode.F)))
                {
                    Interact();
                }
            }
        }

        /// <summary>
        /// Ejecuta la acción al interactuar con la puerta del edificio especial.
        /// Si la escena interior existe en la compilación, la carga; de lo contrario, muestra feedback contextual en el HUD.
        /// </summary>
        public void Interact()
        {
            EnsurePlayerReference();
            if (residentNPC != null && playerTransform != null)
            {
                float distToResident = Vector3.Distance(playerTransform.position, residentNPC.transform.position);
                if (distToResident <= residentNPC.interactDistance)
                {
                    if (promptRoot != null) promptRoot.SetActive(false);
                    return;
                }
            }

            if (Time.unscaledTime - lastInteractionTime < 0.6f) return;
            lastInteractionTime = Time.unscaledTime;

            if (!string.IsNullOrEmpty(targetSceneName) && Application.CanStreamedLevelBeLoaded(targetSceneName))
            {
                Debug.Log($"[SpecialBuildingEntrance] Cargando escena interior: {targetSceneName}...");
                SceneManager.LoadScene(targetSceneName);
                return;
            }

            // Si la escena interior aún no está incorporada en el build, ofrecer feedback contextual claro
            string message = residentNPC != null
                ? $"La puerta del {buildingDisplayName} está cerrada por mantenimiento de robótica. {residentNPC.displayName} está trabajando afuera preparando prototipos. ¡Habla con él!"
                : $"La puerta de {buildingDisplayName} se encuentra asegurada. El acceso al interior estará disponible próximamente.";

            if (FeralGameUI.Instance != null)
            {
                FeralGameUI.Instance.ShowDialogue(buildingDisplayName, message);
            }
            else if (StoryManagerPrototipo3.Instance != null)
            {
                StoryManagerPrototipo3.Instance.EmitRadioTransmission(buildingDisplayName, message);
            }
            else
            {
                Debug.Log($"[SpecialBuildingEntrance] {buildingDisplayName}: {message}");
            }
        }
    }
}
