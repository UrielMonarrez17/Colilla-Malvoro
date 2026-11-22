using System;
using System.Collections.Generic;
using UnityEngine;

namespace Prototipo3.Narrative
{
    /// <summary>
    /// Director Macro de la Campaña de Feral.
    /// Controla el Grafo Macro de Progresión Narrativa (DAG) de 5 hitos principales:
    /// - Nodo 0: Barrio Residencial (Prólogo) -> ARCO_CORTO
    /// - Rama A: Escuela Bellavista -> ARCO_COMPLEJO con Sub-Arco Medio
    /// - Rama B: Bosque Ceniza -> ARCO_COMPLEJO con Sub-Arco Corto
    /// - Convergencia: Hospital San Judas -> ARCO_MEDIO
    /// - Nodo Final: Torre Corporativa Aethelgard -> ARCO_COMPLEJO
    /// </summary>
    public class StoryDirector : MonoBehaviour
    {
        public static StoryDirector Instance { get; private set; }

        public const string NODE_0_PROLOGO = "node_0_apertura";
        public const string RAMA_A_ESCUELA = "node_a1_escuela";
        public const string RAMA_B_BOSQUE = "node_b1_bosque";
        public const string CONVERGENCIA_HOSPITAL = "node_c1_hospital";
        public const string FINAL_TORRE = "node_d1_torre";

        [Header("Estado Macro")]
        [SerializeField] private string activeMacroNode = NODE_0_PROLOGO;
        [SerializeField] private List<string> activeNodeIDs = new List<string>();
        [SerializeField] private List<string> completedNodeIDs = new List<string>();

        [Header("Ítems Clave en Posesión")]
        public bool hasVialEnzimaBase = false;
        public bool hasFiltroCarbonBiologico = false;
        public bool hasOxigenoPuro = false;
        public bool hasSueroAlbaPuro = false;

        public string ActiveMacroNode => activeMacroNode;
        public IReadOnlyList<string> ActiveNodeIDs => activeNodeIDs;
        public IReadOnlyList<string> CompletedNodeIDs => completedNodeIDs;

        public event Action<string> OnMacroNodeActivated;
        public event Action<string> OnMacroNodeCompleted;

        private void Awake()
        {
            SetInstance();
        }

        private void OnEnable()
        {
            SetInstance();
        }

        private void SetInstance()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this && Application.isPlaying)
            {
                Destroy(gameObject);
                return;
            }

            if (!activeNodeIDs.Contains(NODE_0_PROLOGO))
            {
                activeNodeIDs.Add(NODE_0_PROLOGO);
            }
        }

        public static StoryDirector EnsureInstance()
        {
            if (Instance != null) return Instance;
            GameObject go = new GameObject("StoryDirector");
            Instance = go.AddComponent<StoryDirector>();
            return Instance;
        }

        private void Start()
        {
            if (QuestCompiler.Instance != null)
            {
                QuestCompiler.Instance.OnQuestCompleted -= HandleQuestCompletedByCompiler;
                QuestCompiler.Instance.OnQuestCompleted += HandleQuestCompletedByCompiler;
            }
        }

        private void OnDestroy()
        {
            if (QuestCompiler.Instance != null)
            {
                QuestCompiler.Instance.OnQuestCompleted -= HandleQuestCompletedByCompiler;
            }
            if (Instance == this) Instance = null;
        }

        private void HandleQuestCompletedByCompiler(string nodeID)
        {
            Debug.Log($"[StoryDirector] Misión CFG completada para el nodo macro: '{nodeID}'");
            if (string.Equals(nodeID, NODE_0_PROLOGO, StringComparison.OrdinalIgnoreCase))
            {
                CompletePrologoAndUnlockBranches(playCutscene: false);
            }
            else if (string.Equals(nodeID, RAMA_A_ESCUELA, StringComparison.OrdinalIgnoreCase))
            {
                CompleteRamaAEscuela();
            }
            else if (string.Equals(nodeID, RAMA_B_BOSQUE, StringComparison.OrdinalIgnoreCase))
            {
                CompleteRamaBBosque();
            }
            else if (string.Equals(nodeID, CONVERGENCIA_HOSPITAL, StringComparison.OrdinalIgnoreCase))
            {
                CompleteHospitalConvergence(playCutscene: false);
            }
            else if (string.Equals(nodeID, FINAL_TORRE, StringComparison.OrdinalIgnoreCase))
            {
                CompleteFinalTorre(playCutscene: false);
            }
        }

        /// <summary>
        /// Comprueba si un nodo de historia (o sus variantes de rama/subnodo) está actualmente activo.
        /// Usado directamente por PlotGate para prevenir sequence breaking.
        /// </summary>
        public bool IsNodeActive(string targetStoryNodeID)
        {
            if (string.IsNullOrEmpty(targetStoryNodeID)) return false;

            // 1. Consultar primero a StoryManagerPrototipo3.Instance.CurrentGraph
            if (StoryManagerPrototipo3.Instance != null && StoryManagerPrototipo3.Instance.CurrentGraph != null)
            {
                StoryNode node = StoryManagerPrototipo3.Instance.CurrentGraph.GetNode(targetStoryNodeID);
                if (node != null)
                {
                    return node.state == StoryNodeState.Active;
                }
            }

            string normalized = targetStoryNodeID.ToLowerInvariant();

            // Mapeo flexible de alias
            if (normalized.Contains("prologo") || normalized.Contains("apertura") || normalized.Contains("node_0"))
            {
                return activeNodeIDs.Contains(NODE_0_PROLOGO);
            }

            if (normalized.Contains("escuela") || normalized.Contains("toby") || normalized.Contains("morales") || normalized.Contains("node_a"))
            {
                return activeNodeIDs.Contains(RAMA_A_ESCUELA) ||
                       activeNodeIDs.Contains("node_a2_toby") ||
                       activeNodeIDs.Contains("node_a3_apagon");
            }

            if (normalized.Contains("bosque") || normalized.Contains("moka") || normalized.Contains("sultan") || normalized.Contains("node_b"))
            {
                return activeNodeIDs.Contains(RAMA_B_BOSQUE) ||
                       activeNodeIDs.Contains("node_b2_postes") ||
                       activeNodeIDs.Contains("node_b3_sultan");
            }

            if (normalized.Contains("hospital") || normalized.Contains("salgado") || normalized.Contains("clara") || normalized.Contains("node_c"))
            {
                return activeNodeIDs.Contains(CONVERGENCIA_HOSPITAL) ||
                       activeNodeIDs.Contains("node_c2_clara") ||
                       activeNodeIDs.Contains("node_c3_oxigeno");
            }

            if (normalized.Contains("torre") || normalized.Contains("vane") || normalized.Contains("kross") || normalized.Contains("elena") || normalized.Contains("node_d"))
            {
                return activeNodeIDs.Contains(FINAL_TORRE) ||
                       activeNodeIDs.Contains("node_d2_kross") ||
                       activeNodeIDs.Contains("node_d3_final");
            }

            return activeNodeIDs.Contains(targetStoryNodeID);
        }

        public bool IsNodeCompleted(string targetStoryNodeID)
        {
            if (string.IsNullOrEmpty(targetStoryNodeID)) return false;

            // 1. Consultar primero a StoryManagerPrototipo3.Instance.CurrentGraph
            if (StoryManagerPrototipo3.Instance != null && StoryManagerPrototipo3.Instance.CurrentGraph != null)
            {
                StoryNode node = StoryManagerPrototipo3.Instance.CurrentGraph.GetNode(targetStoryNodeID);
                if (node != null)
                {
                    return node.state == StoryNodeState.Completed;
                }
            }

            string normalized = targetStoryNodeID.ToLowerInvariant();

            if (normalized.Contains("prologo") || normalized.Contains("apertura") || normalized.Contains("node_0"))
            {
                return completedNodeIDs.Contains(NODE_0_PROLOGO);
            }
            if (normalized.Contains("escuela") || normalized.Contains("toby") || normalized.Contains("morales") || normalized.Contains("node_a"))
            {
                return completedNodeIDs.Contains(RAMA_A_ESCUELA);
            }
            if (normalized.Contains("bosque") || normalized.Contains("moka") || normalized.Contains("sultan") || normalized.Contains("node_b"))
            {
                return completedNodeIDs.Contains(RAMA_B_BOSQUE);
            }
            if (normalized.Contains("hospital") || normalized.Contains("salgado") || normalized.Contains("clara") || normalized.Contains("node_c"))
            {
                return completedNodeIDs.Contains(CONVERGENCIA_HOSPITAL);
            }
            if (normalized.Contains("torre") || normalized.Contains("vane") || normalized.Contains("kross") || normalized.Contains("elena") || normalized.Contains("node_d"))
            {
                return completedNodeIDs.Contains(FINAL_TORRE);
            }

            return completedNodeIDs.Contains(targetStoryNodeID);
        }

        // =========================================================================
        // PROGRESIÓN DEL GRAFO MACRO (DAG)
        // =========================================================================

        public void StartPrologo(bool playCutscene = false)
        {
            activeMacroNode = NODE_0_PROLOGO;
            if (!activeNodeIDs.Contains(NODE_0_PROLOGO)) activeNodeIDs.Add(NODE_0_PROLOGO);

            if (playCutscene && CutsceneManager.Instance != null)
            {
                Transform marcoTransform = WorldRegistry.Get("Marco");
                CutsceneManager.Instance.PlayCutscene(
                    "01_Intro_Sofia.mp4",
                    marcoTransform,
                    "Kilo despierta en el barrio residencial de Solis. El humo ámbar de Aethelgard cubre el horizonte.",
                    onCompleted: () =>
                    {
                        CompileMacroNodeQuest(NODE_0_PROLOGO, "ARCO_CORTO");
                    });
            }
            else
            {
                CompileMacroNodeQuest(NODE_0_PROLOGO, "ARCO_CORTO");
            }

            OnMacroNodeActivated?.Invoke(NODE_0_PROLOGO);
        }

        public void CompletePrologoAndUnlockBranches(bool playCutscene = false)
        {
            if (!completedNodeIDs.Contains(NODE_0_PROLOGO)) completedNodeIDs.Add(NODE_0_PROLOGO);
            activeNodeIDs.Remove(NODE_0_PROLOGO);

            if (playCutscene && CutsceneManager.Instance != null)
            {
                Transform marcoTransform = WorldRegistry.Get("Marco");
                CutsceneManager.Instance.PlayCutscene(
                    "02_Marco_Feral.mp4",
                    marcoTransform,
                    "Cae la noche sobre Solis. Marco se transforma en Feral y sus ojos arden en ámbar.",
                    onCompleted: () =>
                    {
                        UnlockBranchAAndB();
                    });
            }
            else
            {
                UnlockBranchAAndB();
            }

            OnMacroNodeCompleted?.Invoke(NODE_0_PROLOGO);
        }

        private void UnlockBranchAAndB()
        {
            // Progresión Lineal: Primero se activa únicamente la Rama A (Escuela Bellavista)
            activeMacroNode = RAMA_A_ESCUELA;
            if (!activeNodeIDs.Contains(RAMA_A_ESCUELA)) activeNodeIDs.Add(RAMA_A_ESCUELA);
            CompileMacroNodeQuest(RAMA_A_ESCUELA, "LINEAL");
            OnMacroNodeActivated?.Invoke(RAMA_A_ESCUELA);
        }

        public void CompleteRamaAEscuela()
        {
            hasVialEnzimaBase = true;
            if (!completedNodeIDs.Contains(RAMA_A_ESCUELA)) completedNodeIDs.Add(RAMA_A_ESCUELA);
            activeNodeIDs.Remove(RAMA_A_ESCUELA);
            activeNodeIDs.Remove("node_a2_toby");
            activeNodeIDs.Remove("node_a3_apagon");

            if (StoryManagerPrototipo3.Instance != null)
            {
                if (!StoryManagerPrototipo3.Instance.HasKeyItem("Vial de Enzima Base"))
                {
                    StoryManagerPrototipo3.Instance.GrantKeyItem("Vial de Enzima Base", "Precursor bioquímico (Rama A) requerido para sintetizar el Suero Alba-Puro.");
                }
                StoryManagerPrototipo3.Instance.CompleteNodeIfNotDone("node_a1_escuela");
                StoryManagerPrototipo3.Instance.CompleteNodeIfNotDone("node_a2_toby");
                StoryManagerPrototipo3.Instance.CompleteNodeIfNotDone("node_a3_apagon");
            }

            Debug.Log("[StoryDirector] Rama A (Escuela Bellavista) completada. Obtenido: Vial de Enzima Base. Desbloqueando Rama B (Bosque Ceniza).");
            OnMacroNodeCompleted?.Invoke(RAMA_A_ESCUELA);

            // Desbloquear y activar Rama B (Bosque Ceniza) linealmente
            activeMacroNode = RAMA_B_BOSQUE;
            if (!activeNodeIDs.Contains(RAMA_B_BOSQUE)) activeNodeIDs.Add(RAMA_B_BOSQUE);
            CompileMacroNodeQuest(RAMA_B_BOSQUE, "LINEAL");
            OnMacroNodeActivated?.Invoke(RAMA_B_BOSQUE);
        }

        public void CompleteRamaBBosque()
        {
            hasFiltroCarbonBiologico = true;
            if (!completedNodeIDs.Contains(RAMA_B_BOSQUE)) completedNodeIDs.Add(RAMA_B_BOSQUE);
            activeNodeIDs.Remove(RAMA_B_BOSQUE);
            activeNodeIDs.Remove("node_b2_postes");
            activeNodeIDs.Remove("node_b3_sultan");

            if (StoryManagerPrototipo3.Instance != null)
            {
                if (!StoryManagerPrototipo3.Instance.HasKeyItem("Filtro de Carbón Biológico"))
                {
                    StoryManagerPrototipo3.Instance.GrantKeyItem("Filtro de Carbón Biológico", "Precursor de filtración (Rama B) entregado por el guardabosques Sultán.");
                }
                StoryManagerPrototipo3.Instance.CompleteNodeIfNotDone("node_b1_bosque");
                StoryManagerPrototipo3.Instance.CompleteNodeIfNotDone("node_b2_postes");
                StoryManagerPrototipo3.Instance.CompleteNodeIfNotDone("node_b3_sultan");
            }

            Debug.Log("[StoryDirector] Rama B (Bosque Ceniza) completada. Obtenido: Filtro de Carbón Biológico.");
            OnMacroNodeCompleted?.Invoke(RAMA_B_BOSQUE);

            ActivateHospitalConvergence();
        }

        private void CheckConvergencePreconditions()
        {
            // Precondición de Convergencia: Requiere Vial de Enzima y Filtro de Carbón
            if (hasVialEnzimaBase && hasFiltroCarbonBiologico)
            {
                if (StoryManagerPrototipo3.Instance != null && !StoryManagerPrototipo3.Instance.enableArcHospitalClara)
                {
                    StoryManagerPrototipo3.Instance.CheckCampaignCompletion();
                    return;
                }
                ActivateHospitalConvergence();
            }
        }

        private void ActivateHospitalConvergence()
        {
            if (StoryManagerPrototipo3.Instance != null && !StoryManagerPrototipo3.Instance.enableArcHospitalClara)
            {
                StoryManagerPrototipo3.Instance.CheckCampaignCompletion();
                return;
            }

            activeMacroNode = CONVERGENCIA_HOSPITAL;
            if (!activeNodeIDs.Contains(CONVERGENCIA_HOSPITAL)) activeNodeIDs.Add(CONVERGENCIA_HOSPITAL);

            if (StoryManagerPrototipo3.Instance != null && StoryManagerPrototipo3.Instance.CurrentGraph != null)
            {
                StoryNode nodeC1 = StoryManagerPrototipo3.Instance.CurrentGraph.GetNode("node_c1_hospital");
                if (nodeC1 != null && nodeC1.state != StoryNodeState.Active && nodeC1.state != StoryNodeState.Completed)
                {
                    StoryManagerPrototipo3.Instance.SetNodeState("node_c1_hospital", StoryNodeState.Active, setFocus: true);
                    StoryManagerPrototipo3.Instance.RefreshPointerAndUI();
                }
            }

            CompileMacroNodeQuest(CONVERGENCIA_HOSPITAL, "ARCO_MEDIO");
            OnMacroNodeActivated?.Invoke(CONVERGENCIA_HOSPITAL);
        }

        public void CompleteHospitalConvergence(bool playCutscene = false)
        {
            hasOxigenoPuro = true;
            if (!completedNodeIDs.Contains(CONVERGENCIA_HOSPITAL)) completedNodeIDs.Add(CONVERGENCIA_HOSPITAL);
            activeNodeIDs.Remove(CONVERGENCIA_HOSPITAL);
            activeNodeIDs.Remove("node_c2_clara");

            if (StoryManagerPrototipo3.Instance != null)
            {
                if (!StoryManagerPrototipo3.Instance.HasKeyItem("Cilindro de Oxígeno Puro"))
                {
                    StoryManagerPrototipo3.Instance.GrantKeyItem("Cilindro de Oxígeno Puro", "Tercer componente médico obtenido en el quirófano del Hospital General San Judas.");
                }
                StoryManagerPrototipo3.Instance.CompleteNodeIfNotDone("node_c1_hospital");
                StoryManagerPrototipo3.Instance.CompleteNodeIfNotDone("node_c2_clara");
            }

            if (playCutscene && CutsceneManager.Instance != null)
            {
                Transform hospTransform = WorldRegistry.Get("Hospital");
                CutsceneManager.Instance.PlayCutscene(
                    "06_Hospital_Kross.mp4",
                    hospTransform,
                    "Sofía ha sido estabilizada con Oxígeno Puro. La Dra. Elena Vargas envía una señal de auxilio desde la Torre.",
                    onCompleted: () =>
                    {
                        ActivateFinalTorre();
                    });
            }
            else
            {
                ActivateFinalTorre();
            }

            OnMacroNodeCompleted?.Invoke(CONVERGENCIA_HOSPITAL);
        }

        private void ActivateFinalTorre()
        {
            if (StoryManagerPrototipo3.Instance != null && !StoryManagerPrototipo3.Instance.enableArcTorreKross)
            {
                StoryManagerPrototipo3.Instance.CheckCampaignCompletion();
                return;
            }

            activeMacroNode = FINAL_TORRE;
            if (!activeNodeIDs.Contains(FINAL_TORRE)) activeNodeIDs.Add(FINAL_TORRE);

            if (StoryManagerPrototipo3.Instance != null && StoryManagerPrototipo3.Instance.CurrentGraph != null)
            {
                StoryNode nodeD1 = StoryManagerPrototipo3.Instance.CurrentGraph.GetNode("node_d1_torre");
                if (nodeD1 != null && nodeD1.state != StoryNodeState.Active && nodeD1.state != StoryNodeState.Completed)
                {
                    StoryManagerPrototipo3.Instance.SetNodeState("node_d1_torre", StoryNodeState.Active, setFocus: true);
                    StoryManagerPrototipo3.Instance.RefreshPointerAndUI();
                }
            }

            CompileMacroNodeQuest(FINAL_TORRE, "ARCO_COMPLEJO");
            OnMacroNodeActivated?.Invoke(FINAL_TORRE);
        }

        public void CompleteFinalTorre(bool playCutscene = false)
        {
            if (!completedNodeIDs.Contains(FINAL_TORRE)) completedNodeIDs.Add(FINAL_TORRE);
            activeNodeIDs.Remove(FINAL_TORRE);
            activeNodeIDs.Remove("node_d2_kross");

            if (StoryManagerPrototipo3.Instance != null)
            {
                StoryManagerPrototipo3.Instance.CompleteNodeIfNotDone("node_d1_torre");
                StoryManagerPrototipo3.Instance.CompleteNodeIfNotDone("node_d2_kross");
            }

            if (playCutscene && CutsceneManager.Instance != null)
            {
                Transform torreTransform = WorldRegistry.Get("Torre");
                CutsceneManager.Instance.PlayCutscene(
                    "07_Final_Amanecer.mp4",
                    torreTransform,
                    "El Dr. Julián Kross ha sido pacificado. El Suero Alba-Puro se dispersa por la chimenea de Solis.",
                    onCompleted: () =>
                    {
                        Debug.Log("[StoryDirector] ¡CAMPAÑA COMPLETADA! Amanecer definitivo en Solis.");
                    });
            }

            OnMacroNodeCompleted?.Invoke(FINAL_TORRE);
        }

        private void CompileMacroNodeQuest(string nodeID, string grammarSymbol)
        {
            if (QuestCompiler.Instance != null)
            {
                int seed = CityGeneratorPrototipo3.Instance != null ? CityGeneratorPrototipo3.Instance.seed : 12345;
                QuestCompiler.Instance.CompileQuestForNode(nodeID, grammarSymbol, seed);
            }
        }
    }
}
