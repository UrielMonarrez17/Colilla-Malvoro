using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Prototipo3
{
    public enum StoryMechanismType
    {
        MoralesOfficeDesk,
        SchoolChemistryLab,
        ReflectorPole,
        StreamSubstationSwitch,
        HyperbaricChamberValve,
        PureOxygenCylinder,
        ElenaReactorCell,
        PhotonicUVCapacitor,
        CentralSerumInjector
    }

    /// <summary>
    /// Componente para mecanismos interactuables de historia en los 4 Edificios Importantes de Prototipo 3.
    /// Soporta interacción con [F] y sobrecarga eléctrica con Trampa 3 (Descargador Estático).
    /// </summary>
    [SelectionBase]
    public class StoryMechanismPrototipo3 : MonoBehaviour
    {
        public static readonly List<StoryMechanismPrototipo3> ActiveMechanisms = new List<StoryMechanismPrototipo3>();

        [Header("Configuración del Mecanismo")]
        public string mechanismId = "Mechanism";
        public string displayName = "Mecanismo";
        public StoryMechanismType mechanismType = StoryMechanismType.ReflectorPole;
        public int indexNumber = 1; // Ej. Poste 1, 2, 3 o Condensador 1, 2, 3

        [Header("Estado")]
        public bool isCompleted = false;
        public float interactDistance = 3.8f;

        public bool CanInteract => !isCompleted || mechanismType == StoryMechanismType.StreamSubstationSwitch || mechanismType == StoryMechanismType.CentralSerumInjector;

        private Transform playerTransform;
        private GameObject promptObj;
        private TextMeshPro promptLabel;
        private Light statusLight;

        private void Awake()
        {
            if (!ActiveMechanisms.Contains(this))
            {
                ActiveMechanisms.Add(this);
            }
            if (!gameObject.CompareTag("Interactable"))
            {
                gameObject.tag = "Interactable";
            }
        }

        private void Start()
        {
            EnsureColliderAndLight();
            CreateFloatingLabel();
            UpdateVisualState();
        }

        private void OnEnable()
        {
            if (!ActiveMechanisms.Contains(this))
            {
                ActiveMechanisms.Add(this);
            }
        }

        private void OnDestroy()
        {
            ActiveMechanisms.Remove(this);
        }

        private void EnsureColliderAndLight()
        {
            BoxCollider box = GetComponent<BoxCollider>();
            if (box == null)
            {
                box = gameObject.AddComponent<BoxCollider>();
                box.size = new Vector3(2.2f, 2.2f, 2.2f);
            }
            box.isTrigger = true;

            if (statusLight == null)
            {
                GameObject lGo = new GameObject("MechanismStatusLight");
                lGo.transform.SetParent(transform, false);
                lGo.transform.localPosition = new Vector3(0f, 1.4f, 0f);
                statusLight = lGo.AddComponent<Light>();
                statusLight.type = LightType.Point;
                statusLight.range = 5.5f;
                statusLight.intensity = 2.4f;
            }
        }

        private void CreateFloatingLabel()
        {
            if (promptObj != null) return;

            promptObj = new GameObject("MechanismPrompt_3D");
            promptObj.transform.SetParent(transform, false);
            float invScale = 1f / Mathf.Max(0.1f, transform.localScale.y);
            promptObj.transform.localScale = Vector3.one * invScale;
            promptObj.transform.localPosition = new Vector3(0f, 1.8f * invScale, 0f);

            promptLabel = promptObj.AddComponent<TextMeshPro>();
            promptLabel.fontSize = 2.8f;
            promptLabel.alignment = TextAlignmentOptions.Center;
            promptLabel.color = Color.white;
            promptLabel.outlineWidth = 0.2f;
            promptLabel.outlineColor = Color.black;
        }

        private void Update()
        {
            if (playerTransform == null || !playerTransform.gameObject.activeInHierarchy)
            {
                HamsterController hc = FindAnyObjectByType<HamsterController>();
                if (hc != null) playerTransform = hc.transform;
            }

            if (promptObj == null || promptLabel == null) return;

            float dist = playerTransform != null
                ? Vector3.Distance(transform.position, playerTransform.position)
                : float.MaxValue;

            bool visible = dist <= 18f;
            if (promptObj.activeSelf != visible)
            {
                promptObj.SetActive(visible);
            }

            if (!visible) return;

            if (Camera.main != null)
            {
                promptObj.transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward, Camera.main.transform.up);
            }

            string actionLine = "";
            if (!isCompleted && dist <= interactDistance)
            {
                actionLine = "\n<color=#FFD700>" + GetActionPromptText() + "</color>";
            }
            else if (isCompleted)
            {
                actionLine = "\n<color=#44FF88>[Completado]</color>";
            }

            promptLabel.text = $"<color=#FFEE55><b>{displayName}</b></color>{actionLine}";
        }

        public string GetActionPromptText()
        {
            switch (mechanismType)
            {
                case StoryMechanismType.MoralesOfficeDesk:
                    return "[F] Espiar bitácora del Rector Morales";
                case StoryMechanismType.SchoolChemistryLab:
                    return "[F] Tomar Llave Maestra y Vial de Enzima Base";
                case StoryMechanismType.ReflectorPole:
                    return "[F] Reparar Poste Reflector (Madera/Goma/Metal)";
                case StoryMechanismType.StreamSubstationSwitch:
                    return "[F] Encender Reflectores de Luz Fría";
                case StoryMechanismType.HyperbaricChamberValve:
                    return "[F / Tecla 3] Activar Válvula Hiperbárica (Descargador Estático)";
                case StoryMechanismType.PureOxygenCylinder:
                    return "[F] Recuperar Cilindro de Oxígeno Puro";
                case StoryMechanismType.ElenaReactorCell:
                    return "[F] Liberar a la Dra. Elena Vargas";
                case StoryMechanismType.PhotonicUVCapacitor:
                    return "[F / Tecla 3] Sobrecargar Condensador UV";
                case StoryMechanismType.CentralSerumInjector:
                    return "[F] Insertar Suero Alba-Puro en el Inyector Central";
                default:
                    return "[F] Interactuar";
            }
        }

        public void Interact()
        {
            if (Prototipo3.UI.FeralGameUI.BlocksGameplay) return;
            if (StoryManagerPrototipo3.Instance != null)
            {
                StoryManagerPrototipo3.Instance.HandleMechanismInteraction(this, false);
            }
        }

        /// <summary>
        /// Llamado cuando el jugador activa la Trampa 3 (Descargador Estático) en un radio de 8 metros.
        /// </summary>
        public void OnStaticDischargeHit()
        {
            if (isCompleted) return;
            if (mechanismType == StoryMechanismType.HyperbaricChamberValve ||
                mechanismType == StoryMechanismType.PhotonicUVCapacitor)
            {
                if (StoryManagerPrototipo3.Instance != null)
                {
                    StoryManagerPrototipo3.Instance.HandleMechanismInteraction(this, true);
                }
            }
        }

        public void MarkCompleted()
        {
            isCompleted = true;
            UpdateVisualState();
        }

        public void UpdateVisualState()
        {
            Color targetColor = isCompleted
                ? new Color(0.2f, 0.95f, 0.45f, 1f)
                : new Color(1.0f, 0.78f, 0.18f, 1f);

            if (statusLight != null)
            {
                statusLight.color = targetColor;
                statusLight.intensity = isCompleted ? 3.5f : 2.2f;
            }

            Renderer[] rens = GetComponentsInChildren<Renderer>();
            for (int i = 0; i < rens.Length; i++)
            {
                if (rens[i] == null || rens[i].GetComponent<TextMeshPro>() != null) continue;
                if (rens[i].material != null)
                {
                    rens[i].material.color = targetColor;
                }
            }
        }
    }
}
