using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Prototipo2.Narrative;

namespace Prototipo2
{
    public class StoryManagerPrototipo2 : MonoBehaviour
    {
        public static StoryManagerPrototipo2 Instance { get; private set; }

        [Header("Generación Narrativa")]
        public int storySeed = 54321;
        public StoryWorldBinder worldBinder;

        [Header("Referencias del Jugador y Misión")]
        public Transform player;
        public MissionPointerPrototipo2 missionPointer;

        [Header("Estado Actual")]
        [SerializeField] private StoryGraph currentGraph;
        public StoryGraph CurrentGraph => currentGraph;

        private StoryNode currentActiveNode;
        public StoryNode CurrentActiveNode => currentActiveNode;

        private int containersLootedCount = 0;

        public event Action<StoryNode> OnActiveNodeChanged;
        public event Action<string, string> OnDialogueTriggered; // (Speaker, Text)
        public event Action OnDemoCompleted;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) Destroy(this);

            if (worldBinder == null)
            {
                worldBinder = GetComponent<StoryWorldBinder>() ?? gameObject.AddComponent<StoryWorldBinder>();
            }
        }

        private void OnEnable()
        {
            LootContainer.OnContainerLooted += HandleContainerLooted;
            CraftingTable.OnTrapCrafted += HandleTrapCrafted;
        }

        private void OnDisable()
        {
            LootContainer.OnContainerLooted -= HandleContainerLooted;
            CraftingTable.OnTrapCrafted -= HandleTrapCrafted;

            CityGeneratorPrototipo2 cityGen = FindAnyObjectByType<CityGeneratorPrototipo2>();
            if (cityGen != null)
            {
                cityGen.OnCityGenerated -= InicializarHistoria;
            }
        }

        private void Start()
        {
            if (player == null)
            {
                HamsterController hc = FindAnyObjectByType<HamsterController>();
                if (hc != null) player = hc.transform;
            }

            if (missionPointer == null)
            {
                missionPointer = FindAnyObjectByType<MissionPointerPrototipo2>();
            }

            // Iniciar tras la generación de la ciudad
            CityGeneratorPrototipo2 cityGen = FindAnyObjectByType<CityGeneratorPrototipo2>();
            if (cityGen != null)
            {
                cityGen.OnCityGenerated -= InicializarHistoria;
                cityGen.OnCityGenerated += InicializarHistoria;
                if (cityGen.Grid != null)
                {
                    InicializarHistoria();
                }
            }
            else
            {
                InicializarHistoria();
            }
        }

        public void InicializarHistoria()
        {
            currentGraph = StoryGrammarEngine.GenerateStory(storySeed);
            CityGeneratorPrototipo2 cityGen = FindAnyObjectByType<CityGeneratorPrototipo2>();

            if (cityGen != null && worldBinder != null)
            {
                worldBinder.BindGraphToWorld(currentGraph, cityGen);
            }

            // Activar nodo inicial
            SetNodeActive("node_start");
        }

        private void Update()
        {
            if (currentActiveNode == null || player == null)
            {
                if (player == null)
                {
                    HamsterController hc = FindAnyObjectByType<HamsterController>();
                    if (hc != null) player = hc.transform;
                }
                return;
            }

            // Si el nodo activo es el encuentro con Lulú y existe su instancia NPC, actualizar posición en tiempo real
            if (currentActiveNode.id == "node_encounter" && worldBinder != null && worldBinder.SpawnedLuluInstance != null)
            {
                currentActiveNode.worldPosition = worldBinder.SpawnedLuluInstance.transform.position;
            }

            float distToTarget = Vector3.Distance(player.position, currentActiveNode.worldPosition);

            // 1. Nodo Start: Tras dar los primeros pasos, avanzar a Encounter
            if (currentActiveNode.id == "node_start" && distToTarget > 2.0f)
            {
                CompletarNodoActual();
                SetNodeActive("node_encounter");
                return;
            }

            // 2. Nodo Encounter: Al acercarse e interactuar con F
            if (currentActiveNode.id == "node_encounter" && distToTarget <= 3.8f)
            {
                if (FeralInput.Pressed(KeyCode.F) || Input.GetKeyDown(KeyCode.F))
                {
                    InteractuarConLulu(worldBinder != null ? worldBinder.SpawnedLuluNPC : null);
                }
                return;
            }

            // 3. Nodo Key: Fabricar trampa o pulsar F en la mesa
            if (currentActiveNode.id == "node_key" && distToTarget <= 3.0f)
            {
                if (FeralInput.Pressed(KeyCode.F) || Input.GetKeyDown(KeyCode.F))
                {
                    CompletarNodoActual();
                    if (worldBinder != null) worldBinder.DesactivarBarricadaLock();
                    SetNodeActive("node_lock");
                }
                return;
            }

            // 4. Nodo Lock: Llegar a la barricada y desbloquear con F
            if (currentActiveNode.id == "node_lock" && distToTarget <= 4.0f)
            {
                if (FeralInput.Pressed(KeyCode.F) || Input.GetKeyDown(KeyCode.F) || distToTarget <= 2.5f)
                {
                    if (worldBinder != null) worldBinder.DesactivarBarricadaLock();
                    CompletarNodoActual();
                    SetNodeActive("node_boss");
                }
                return;
            }

            // 5. Nodo Boss: Atravesar o evadir territorio alfa
            if (currentActiveNode.id == "node_boss" && distToTarget <= 15f)
            {
                // El jugador ha confrontado/avanzado hacia la zona del jefe
                CompletarNodoActual();
                SetNodeActive("node_exit");
                return;
            }

            // 6. Nodo Exit: Llegar al punto de evacuación
            if (currentActiveNode.id == "node_exit" && distToTarget <= 3.5f)
            {
                CompletarNodoActual();
                OnDemoCompleted?.Invoke();
                return;
            }
        }

        public void InteractuarConLulu(LuluNPCPrototipo2 luluNpc = null)
        {
            if (currentGraph == null) return;

            if (currentActiveNode != null && currentActiveNode.id == "node_start")
            {
                CompletarNodoActual();
                SetNodeActive("node_encounter");
            }

            if (currentActiveNode != null && currentActiveNode.id == "node_encounter")
            {
                OnDialogueTriggered?.Invoke(currentActiveNode.targetActorName, currentActiveNode.dialogueText);
                CompletarNodoActual();
                SetNodeActive("node_quest");
                return;
            }

            // Diálogos contextuales adicionales de Lulú según la fase de la historia procedural
            string activeId = currentActiveNode != null ? currentActiveNode.id : "";
            string contextualFollowUp;
            switch (activeId)
            {
                case "node_quest":
                    contextualFollowUp = $"Aún necesitas recolectar suministros ({containersLootedCount}/{currentActiveNode.requiredItemCount}). Revisa los contenedores de basura en las esquinas y callejones.";
                    break;
                case "node_key":
                    contextualFollowUp = "¡Ya tienes los materiales! Corre a la mesa de trabajo junto a tu refugio y pulsa [F] para preparar la herramienta de apertura.";
                    break;
                case "node_lock":
                    contextualFollowUp = "La barricada central ya puede despejarse. Dirígete al paso bloqueado y ábrelo antes de que te rodeen.";
                    break;
                case "node_boss":
                    contextualFollowUp = "La rotonda más adelante es territorio del Alfa. Agáchate con [C] para reducir su detección o usa una trampa.";
                    break;
                default:
                    contextualFollowUp = "¡El camino hacia la madriguera de extracción está despejado! No mires atrás, pequeñín.";
                    break;
            }

            OnDialogueTriggered?.Invoke("Lulú", contextualFollowUp);
        }

        private void HandleContainerLooted(LootContainer container, List<ItemType> items)
        {
            if (currentActiveNode != null && currentActiveNode.id == "node_quest")
            {
                containersLootedCount++;
                currentActiveNode.currentItemProgress = containersLootedCount;

                if (containersLootedCount >= currentActiveNode.requiredItemCount)
                {
                    CompletarNodoActual();
                    SetNodeActive("node_key");
                }
                else
                {
                    OnActiveNodeChanged?.Invoke(currentActiveNode);
                }
            }
        }

        private void HandleTrapCrafted(TipoTrampa tipo)
        {
            if (currentActiveNode != null && currentActiveNode.id == "node_key")
            {
                CompletarNodoActual();
                if (worldBinder != null) worldBinder.DesactivarBarricadaLock();
                SetNodeActive("node_lock");
            }
        }

        private void SetNodeActive(string nodeId)
        {
            StoryNode targetNode = currentGraph.GetNode(nodeId);
            if (targetNode == null) return;

            // Desactivar el anterior si no está completado
            if (currentActiveNode != null && currentActiveNode.state == StoryNodeState.Active)
            {
                currentActiveNode.state = StoryNodeState.Available;
            }

            targetNode.state = StoryNodeState.Active;
            currentActiveNode = targetNode;

            Debug.Log($"[StoryManager] Nuevo Objetivo Activo: {targetNode.title} ({targetNode.type})");
            if (missionPointer != null)
            {
                missionPointer.ActualizarObjetivoPorNodo(targetNode);
            }
            OnActiveNodeChanged?.Invoke(targetNode);
        }

        private void CompletarNodoActual()
        {
            if (currentActiveNode == null) return;
            currentActiveNode.state = StoryNodeState.Completed;
            Debug.Log($"[StoryManager] ¡Objetivo Completado!: {currentActiveNode.title}");
        }
    }
}
