using System;
using UnityEngine;
using UnityEngine.AI;
using TMPro;

namespace Prototipo3.Narrative
{
    [SelectionBase]
    public class LuluNPCPrototipo3 : MonoBehaviour
    {
        public enum LuluBehaviorState
        {
            Idle,
            Stroll,
            GreetPlayer,
            VanishToCatwalk
        }

        [Header("Estado")]
        [SerializeField] private LuluBehaviorState currentState = LuluBehaviorState.Idle;
        public LuluBehaviorState CurrentState => currentState;

        [Header("Configuración")]
        public float strollRadius = 5f;
        public float strollSpeed = 2.0f;
        public float greetDistance = 6.0f;
        public float interactDistance = 4.0f;

        [Header("Referencias")]
        public Transform playerTransform;
        private NavMeshAgent agent;
        private Animator animator;
        private float stateTimer = 0f;
        private float lastInteractTime = -10f;
        private bool hasMetPlayerInEncounter = false;

        // UI flotante sobre Lulú
        private GameObject promptRootObj;
        private TextMeshPro promptTextMesh;

        private void Awake()
        {
            // Desactivar scripts ajenos
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

            agent = GetComponent<NavMeshAgent>();
            animator = GetComponent<Animator>();

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

            // Actualizar visibilidad de UI interactiva
            if (promptRootObj != null)
            {
                bool canInteract = distToPlayer <= interactDistance;
                promptRootObj.SetActive(canInteract);
                if (canInteract && Camera.main != null)
                {
                    promptRootObj.transform.rotation = Camera.main.transform.rotation;
                }
            }

            // Detección de tecla de interacción
            if (distToPlayer <= interactDistance && (Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.E)))
            {
                if (Time.time - lastInteractTime > 0.5f)
                {
                    lastInteractTime = Time.time;
                    Interact();
                }
            }

            // Lógica de IA simple si no hay NavMesh activo
            stateTimer -= Time.deltaTime;
            if (distToPlayer <= greetDistance)
            {
                currentState = LuluBehaviorState.GreetPlayer;
                // Mirar suavemente al hámster Barnaby
                Vector3 lookTarget = playerTransform.position;
                lookTarget.y = transform.position.y;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookTarget - transform.position), Time.deltaTime * 3.5f);
            }
            else if (currentState == LuluBehaviorState.GreetPlayer)
            {
                currentState = LuluBehaviorState.Idle;
            }
        }

        public void Interact()
        {
            Debug.Log("[LuluNPCPrototipo3] Interacción con Lulú");
            hasMetPlayerInEncounter = true;

            if (StoryManagerPrototipo3.Instance != null)
            {
                StoryManagerPrototipo3.Instance.OnNPCInteracted("LuluNPC");
            }
        }

        public void Interactuar(Prototipo3.Player.HamsterControllerPrototipo3 player)
        {
            Interact();
        }

        private void CreateWorldPromptUI()
        {
            promptRootObj = new GameObject("LuluWorldPromptUI");
            promptRootObj.transform.SetParent(this.transform);
            promptRootObj.transform.localPosition = new Vector3(0f, 1.2f, 0f);

            promptTextMesh = promptRootObj.AddComponent<TextMeshPro>();
            promptTextMesh.text = "[F] Hablar con Lulú";
            promptTextMesh.fontSize = 3.6f;
            promptTextMesh.alignment = TextAlignmentOptions.Center;
            promptTextMesh.color = new Color(1f, 0.85f, 0.4f, 1f);

            promptRootObj.SetActive(false);
        }
    }
}
