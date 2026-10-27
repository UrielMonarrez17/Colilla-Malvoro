using System;
using UnityEngine;
using TMPro;

namespace Prototipo3.Narrative
{
    [SelectionBase]
    public class BaronAlquitranNPCPrototipo3 : MonoBehaviour
    {
        [Header("Configuración")]
        public float interactDistance = 4.5f;
        public float lookDistance = 7.0f;

        [Header("Referencias")]
        public Transform playerTransform;
        private float lastInteractTime = -10f;
        private bool hasDeliveredKeycard = false;

        private GameObject promptRootObj;
        private TextMeshPro promptTextMesh;

        private void Awake()
        {
            // Desactivar componentes extraños si provienen de modelos importados
            MonoBehaviour[] scripts = GetComponents<MonoBehaviour>();
            for (int i = 0; i < scripts.Length; i++)
            {
                if (scripts[i] == null || scripts[i] == this) continue;
                string typeName = scripts[i].GetType().Name;
                if (typeName == "MovePlayerInput" || typeName == "CreatureMover" || typeName == "AnimationShowcase")
                {
                    scripts[i].enabled = false;
                }
            }

            CreateWorldPromptUI();
        }

        private void Start()
        {
            if (playerTransform == null)
            {
                var player = FindFirstObjectByType<Prototipo3.Player.HamsterControllerPrototipo3>();
                if (player != null) playerTransform = player.transform;
            }
        }

        private void Update()
        {
            if (playerTransform == null)
            {
                var player = FindFirstObjectByType<Prototipo3.Player.HamsterControllerPrototipo3>();
                if (player != null) playerTransform = player.transform;
                return;
            }

            float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);

            if (promptRootObj != null)
            {
                bool canInteract = distToPlayer <= interactDistance;
                promptRootObj.SetActive(canInteract);
                if (canInteract && Camera.main != null)
                {
                    promptRootObj.transform.rotation = Camera.main.transform.rotation;
                }
            }

            if (distToPlayer <= lookDistance)
            {
                Vector3 lookTarget = playerTransform.position;
                lookTarget.y = transform.position.y;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookTarget - transform.position), Time.deltaTime * 3.0f);
            }

            if (distToPlayer <= interactDistance && (Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.E)))
            {
                if (Time.time - lastInteractTime > 0.5f)
                {
                    lastInteractTime = Time.time;
                    Interact();
                }
            }
        }

        public void Interact()
        {
            Debug.Log("[BaronAlquitranNPCPrototipo3] Interacción con el Barón de Alquitrán");

            if (!hasDeliveredKeycard)
            {
                hasDeliveredKeycard = true;
                var player = FindFirstObjectByType<Prototipo3.Player.HamsterControllerPrototipo3>();
                if (player != null)
                {
                    player.AcquireKeycard();
                }
                if (promptTextMesh != null)
                {
                    promptTextMesh.text = "Barón: «¡Ve a la Torre Norte y hazlos pagar!»";
                }
            }

            if (StoryManagerPrototipo3.Instance != null)
            {
                StoryManagerPrototipo3.Instance.OnNPCInteracted("BaronNPC");
            }
        }

        public void Interactuar(Prototipo3.Player.HamsterControllerPrototipo3 player)
        {
            Interact();
        }

        private void CreateWorldPromptUI()
        {
            promptRootObj = new GameObject("BaronWorldPromptUI");
            promptRootObj.transform.SetParent(this.transform);
            promptRootObj.transform.localPosition = new Vector3(0f, 1.4f, 0f);

            promptTextMesh = promptRootObj.AddComponent<TextMeshPro>();
            promptTextMesh.text = "[F] Negociar con el Barón de Alquitrán";
            promptTextMesh.fontSize = 3.6f;
            promptTextMesh.alignment = TextAlignmentOptions.Center;
            promptTextMesh.color = new Color(1f, 0.65f, 0.2f, 1f);

            promptRootObj.SetActive(false);
        }
    }
}
