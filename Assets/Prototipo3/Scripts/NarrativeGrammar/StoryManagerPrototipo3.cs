using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Prototipo3.PCG;
using Prototipo3.Combat;
using Prototipo3.Cinematics;

namespace Prototipo3.Narrative
{
    public class StoryManagerPrototipo3 : MonoBehaviour
    {
        public static StoryManagerPrototipo3 Instance { get; private set; }

        [Header("Generación Narrativa")]
        public int storySeed = 2026;
        public StoryGrammarEnginePrototipo3 grammarEngine;
        public StoryWorldBinderPrototipo3 worldBinder;

        [Header("Referencias del Jugador y Misión")]
        public Transform player;
        public Prototipo3.UI.MissionPointerPrototipo3 missionPointer;

        [Header("Estado Actual")]
        [SerializeField] private StoryGraphPrototipo3 currentGraph;
        public StoryGraphPrototipo3 CurrentGraph => currentGraph;

        private StoryNodePrototipo3 currentActiveNode;
        public StoryNodePrototipo3 CurrentActiveNode => currentActiveNode;

        public event Action<StoryNodePrototipo3> OnActiveNodeChanged;
        public event Action<string, string> OnDialogueTriggered; // (Speaker, Text)
        public event Action OnStoryCompleted;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else if (Instance != this) Destroy(this);

            if (grammarEngine == null)
            {
                grammarEngine = GetComponent<StoryGrammarEnginePrototipo3>() ?? gameObject.AddComponent<StoryGrammarEnginePrototipo3>();
            }

            if (worldBinder == null)
            {
                worldBinder = GetComponent<StoryWorldBinderPrototipo3>() ?? gameObject.AddComponent<StoryWorldBinderPrototipo3>();
            }
        }

        private void Start()
        {
            if (player == null)
            {
                var hc = FindFirstObjectByType<Prototipo3.Player.HamsterControllerPrototipo3>();
                if (hc != null) player = hc.transform;
            }

            if (missionPointer == null)
            {
                missionPointer = FindFirstObjectByType<Prototipo3.UI.MissionPointerPrototipo3>();
            }

            var cityGen = FindFirstObjectByType<CityGeneratorPrototipo3>();
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

        private void OnDestroy()
        {
            var cityGen = FindFirstObjectByType<CityGeneratorPrototipo3>();
            if (cityGen != null)
            {
                cityGen.OnCityGenerated -= InicializarHistoria;
            }
        }

        public void InicializarHistoria()
        {
            currentGraph = grammarEngine.GenerateStoryGraph(storySeed);
            var cityGen = FindFirstObjectByType<CityGeneratorPrototipo3>();

            if (cityGen != null && worldBinder != null)
            {
                worldBinder.BindGraphToWorld(currentGraph, cityGen);
            }

            // Reproducir Intro Cinemática MP4
            if (CinematicsManagerPrototipo3.Instance != null)
            {
                CinematicsManagerPrototipo3.Instance.PlayCinematic("Intro_FiltroCarmesi_Prototipo3", () =>
                {
                    SetNodeActive("node_start");
                });
            }
            else
            {
                SetNodeActive("node_start");
            }
        }

        private void Update()
        {
            if (currentActiveNode == null) return;

            if (player == null)
            {
                var hc = FindFirstObjectByType<Prototipo3.Player.HamsterControllerPrototipo3>();
                if (hc != null) player = hc.transform;
                return;
            }

            float distToTarget = Vector3.Distance(player.position, currentActiveNode.worldPosition);

            // 1. Nodo Start: Barnaby avanza fuera del punto de inicio
            if (currentActiveNode.id == "node_start" && distToTarget > 3.0f)
            {
                CompletarNodoActual();
                SetNodeActive("node_lulu");
                return;
            }

            // 2. Nodo Lock: Llegar a la barricada con la Keycard
            if (currentActiveNode.id == "node_lock" && distToTarget <= 4.5f)
            {
                var hc = player.GetComponent<Prototipo3.Player.HamsterControllerPrototipo3>();
                if (hc != null && hc.hasKeycardTorreNorte)
                {
                    if (worldBinder != null) worldBinder.DesactivarBarricadaLock();
                    CompletarNodoActual();
                    SetNodeActive("node_final_boss");
                }
                else
                {
                    OnDialogueTriggered?.Invoke("Consola Torre", "Acceso denegado. Se requiere Keycard_TorreNorte.");
                }
                return;
            }

            // 3. Nodo Exit: Punto de extracción final
            if (currentActiveNode.id == "node_exit" && distToTarget <= 4.0f)
            {
                CompletarNodoActual();
                OnStoryCompleted?.Invoke();
                return;
            }
        }

        public void OnNPCInteracted(string npcId)
        {
            if (currentGraph == null || currentActiveNode == null) return;

            if (npcId == "LuluNPC" && (currentActiveNode.id == "node_lulu" || currentActiveNode.id == "node_start"))
            {
                var luluNode = currentGraph.GetNode("node_lulu");
                string dlg = luluNode != null ? luluNode.dialogueText : "Ten cuidado con los filtros carmesí, Barnaby.";
                OnDialogueTriggered?.Invoke("Lulú", dlg);

                CompletarNodoActual();
                SetNodeActive("node_loot_filters");
            }
            else if (npcId == "BaronNPC" && currentActiveNode.id == "node_baron")
            {
                var baronNode = currentGraph.GetNode("node_baron");
                string dlg = baronNode != null ? baronNode.dialogueText : "Toma la tarjeta de la torre. Hazlos pagar.";
                OnDialogueTriggered?.Invoke("Barón de Alquitrán", dlg);

                CompletarNodoActual();
                currentGraph.CompleteNode("node_key");
                SetNodeActive("node_lock");
            }
        }

        public void OnItemCollected(string itemName, int currentTotal)
        {
            if (currentGraph == null || currentActiveNode == null) return;

            if (itemName == "Filtro Carmesí" && currentActiveNode.id == "node_loot_filters")
            {
                currentActiveNode.currentItemProgress = currentTotal;
                var hud = FindFirstObjectByType<Prototipo3.UI.NarrativeHUDPrototipo3>();
                if (hud != null) hud.ActualizarMision(currentActiveNode);

                if (currentTotal >= currentActiveNode.requiredItemCount)
                {
                    CompletarNodoActual();
                    SetNodeActive("node_miniboss");
                }
            }
        }

        public void OnMiniBossDefeated()
        {
            if (currentGraph == null) return;

            if (currentActiveNode != null && currentActiveNode.id == "node_miniboss")
            {
                CompletarNodoActual();
                SetNodeActive("node_baron");
            }
        }

        public void OnBossReactorDefeated()
        {
            if (currentGraph == null) return;

            if (currentActiveNode != null && currentActiveNode.id == "node_final_boss")
            {
                CompletarNodoActual();

                if (CinematicsManagerPrototipo3.Instance != null)
                {
                    CinematicsManagerPrototipo3.Instance.PlayCinematic("Outro_Victoria_Prototipo3", () =>
                    {
                        SetNodeActive("node_exit");
                        OnStoryCompleted?.Invoke();
                    });
                }
                else
                {
                    SetNodeActive("node_exit");
                    OnStoryCompleted?.Invoke();
                }
            }
        }

        public void SetNodeActive(string nodeId)
        {
            if (currentGraph == null) return;

            var node = currentGraph.GetNode(nodeId);
            if (node == null) return;

            node.state = StoryNodeStatePrototipo3.Active;
            currentActiveNode = node;

            Debug.Log($"[StoryManagerPrototipo3] Objetivo Activo: {node.title}");

            if (missionPointer != null)
            {
                missionPointer.ActualizarObjetivoPorNodo(node);
            }

            OnActiveNodeChanged?.Invoke(node);
        }

        public void CompletarNodoActual()
        {
            if (currentActiveNode == null || currentGraph == null) return;

            currentGraph.CompleteNode(currentActiveNode.id);
            Debug.Log($"[StoryManagerPrototipo3] Nodo completado: {currentActiveNode.id}");
        }
    }
}
