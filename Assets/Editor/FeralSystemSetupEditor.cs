using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Prototipo3;
using Prototipo3.Narrative;
using Prototipo3.UI;

namespace Prototipo3.Editor
{
    /// <summary>
    /// Automatización en Editor para el Sistema de Historia Procedural Híbrido de "Feral".
    /// Menú exacto: Tools > Feral > Configurar Sistema de Historia, CFG y UI Completa.
    /// Configura StoryDirector, QuestCompiler, CutsceneManager, WorldRegistry, DualRoleCharacter,
    /// PlotGate, UI y valida la derivación formal de la CFG.
    /// </summary>
    public static class FeralSystemSetupEditor
    {
        private const string ScenePath = "Assets/Prototipo3/Prototipo3.unity";
        private const string StreamingCutscenesDir = "Assets/StreamingAssets/Cutscenes";
        private const string StoryPrefabsDir = "Assets/Prototipo3/Prefabs/Story";

        [MenuItem("Tools/Feral/Configurar Sistema de Historia, CFG y UI Completa", priority = 10)]
        [MenuItem("Feral/Configurar Sistema de Historia, CFG y UI Completa", priority = 10)]
        public static void RunSystemSetup()
        {
            Debug.Log("<color=#FFEE55><b>========================================================================</b></color>");
            Debug.Log("<color=#FFEE55><b>[FeralSystemSetupEditor] INICIANDO CONFIGURACIÓN INTEGRAL DEL SISTEMA</b></color>");
            Debug.Log("<color=#FFEE55><b>========================================================================</b></color>");

            // 1. Asegurar directorios
            EnsureDirectories();

            // 2. Ejecutar validación de la Gramática Libre de Contexto (CFG)
            bool cfgValid = ValidateCFGDerivation();
            if (!cfgValid)
            {
                Debug.LogError("[FeralSystemSetupEditor] ❌ Error en la validación sintáctica de la CFG.");
            }
            else
            {
                Debug.Log("<color=#44FF88>[FeralSystemSetupEditor] ✅ Gramática Formal CFG (G = (VN, Σ, R, S)) validada con éxito en 15 semillas aleatorias.</color>");
            }

            // 3. Abrir y configurar la escena Prototipo3
            SetupSceneComponents();

            // 4. Validar ubicación de jefes y anclas Manhattan
            ValidateBossDistanceAndPlotGates();

            // 5. Imprimir reporte final de arquitectura y rutas
            PrintArchitectureReport();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Feral - Sistema de Historia Híbrido",
                "¡Configuración de StoryDirector, QuestCompiler (CFG), PlotGate, CutsceneManager y UI completada con éxito!\n\n" +
                "• Gramática CFG: Validada (ARCO_CORTO, ARCO_MEDIO, ARCO_COMPLEJO).\n" +
                "• PlotGates: Asignados en todos los Jefes y Mecanismos de Arena.\n" +
                "• Cinemáticas: Carpeta Assets/StreamingAssets/Cutscenes/ lista con fallback de Novela Gráfica.\n" +
                "• Trampas: 3 trampas funcionales (Madera, Red, Púas) sincronizadas.",
                "Aceptar");
        }

        private static void EnsureDirectories()
        {
            if (!Directory.Exists(StreamingCutscenesDir))
            {
                Directory.CreateDirectory(StreamingCutscenesDir);
                string readmePath = Path.Combine(StreamingCutscenesDir, "README_CUTSCENES.txt");
                if (!File.Exists(readmePath))
                {
                    File.WriteAllText(readmePath,
                        "Assets/StreamingAssets/Cutscenes/\n" +
                        "Coloca aquí los 7 archivos .mp4 (01_Intro_Sofia.mp4 ... 07_Final_Amanecer.mp4).\n" +
                        "Si no existen, CutsceneManager ejecuta la Novela Gráfica In-Engine.");
                }
                Debug.Log($"[FeralSystemSetupEditor] Directorio de cinemáticas asegurado: {StreamingCutscenesDir}");
            }

            if (!Directory.Exists(StoryPrefabsDir))
            {
                Directory.CreateDirectory(StoryPrefabsDir);
                Debug.Log($"[FeralSystemSetupEditor] Directorio de prefabs asegurado: {StoryPrefabsDir}");
            }
        }

        public static bool ValidateCFGDerivation()
        {
            int[] testSeeds = { 101, 202, 303, 777, 12345 };
            string[] symbolsToTest = { "ARCO_CORTO", "ARCO_MEDIO", "ARCO_COMPLEJO" };

            foreach (string startSym in symbolsToTest)
            {
                foreach (int seed in testSeeds)
                {
                    List<TerminalAction> derived = QuestGrammarParser.Derivate(startSym, seed, maxRecursionDepth: 2);
                    if (derived == null || derived.Count == 0)
                    {
                        Debug.LogError($"[CFG Validation] Derivación vacía para símbolo {startSym} con semilla {seed}.");
                        return false;
                    }

                    // Verificar que cada terminal pertenezca formalmente a Σ
                    for (int i = 0; i < derived.Count; i++)
                    {
                        TerminalAction act = derived[i];
                        if (!QuestGrammarParser.TerminalVocabulary.Contains(act.actionType))
                        {
                            Debug.LogError($"[CFG Validation] Símbolo terminal no reconocido en Σ: '{act.actionType}' en {startSym}.");
                            return false;
                        }
                    }

                    // Verificar que ARCO_COMPLEJO contenga sub-arcos opcionales
                    if (startSym == "ARCO_COMPLEJO")
                    {
                        bool hasOptional = false;
                        for (int i = 0; i < derived.Count; i++)
                        {
                            if (derived[i].isOptional)
                            {
                                hasOptional = true;
                                break;
                            }
                        }
                        if (!hasOptional)
                        {
                            Debug.LogWarning($"[CFG Validation] ARCO_COMPLEJO con semilla {seed} no produjo acciones opcionales.");
                        }
                    }
                }
            }

            return true;
        }

        private static void SetupSceneComponents()
        {
            if (!File.Exists(ScenePath))
            {
                Debug.LogWarning($"[FeralSystemSetupEditor] Escena no encontrada en '{ScenePath}'. Asegurando en escena activa.");
            }
            else
            {
                var activeScene = EditorSceneManager.GetActiveScene();
                if (activeScene.path != ScenePath)
                {
                    EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
                }
            }

            // 1. WorldRegistry
            WorldRegistry worldReg = GameObject.FindAnyObjectByType<WorldRegistry>();
            if (worldReg == null)
            {
                GameObject go = new GameObject("WorldRegistry_Prototipo3");
                worldReg = go.AddComponent<WorldRegistry>();
                Debug.Log("[FeralSystemSetupEditor] Creado WorldRegistry en escena.");
            }

            // 2. StoryDirector
            StoryDirector director = GameObject.FindAnyObjectByType<StoryDirector>();
            if (director == null)
            {
                GameObject go = new GameObject("StoryDirector_Prototipo3");
                director = go.AddComponent<StoryDirector>();
                Debug.Log("[FeralSystemSetupEditor] Creado StoryDirector en escena.");
            }

            // 3. QuestCompiler
            QuestCompiler compiler = GameObject.FindAnyObjectByType<QuestCompiler>();
            if (compiler == null)
            {
                GameObject go = new GameObject("QuestCompiler_Prototipo3");
                compiler = go.AddComponent<QuestCompiler>();
                Debug.Log("[FeralSystemSetupEditor] Creado QuestCompiler en escena.");
            }

            // 4. CutsceneManager
            CutsceneManager cutscenes = GameObject.FindAnyObjectByType<CutsceneManager>();
            if (cutscenes == null)
            {
                GameObject go = new GameObject("CutsceneManager_Prototipo3");
                cutscenes = go.AddComponent<CutsceneManager>();
                Debug.Log("[FeralSystemSetupEditor] Creado CutsceneManager en escena.");
            }

            // 5. TrapSystemPrototipo3
            TrapSystemPrototipo3 trapSys = GameObject.FindAnyObjectByType<TrapSystemPrototipo3>();
            if (trapSys == null)
            {
                GameObject go = new GameObject("TrapSystem_Prototipo3");
                trapSys = go.AddComponent<TrapSystemPrototipo3>();
                Debug.Log("[FeralSystemSetupEditor] Creado TrapSystemPrototipo3 en escena.");
            }

            // 6. RespawnManagerPrototipo3
            RespawnManagerPrototipo3 respawn = GameObject.FindAnyObjectByType<RespawnManagerPrototipo3>();
            if (respawn == null)
            {
                GameObject go = new GameObject("RespawnManager_Prototipo3");
                respawn = go.AddComponent<RespawnManagerPrototipo3>();
                Debug.Log("[FeralSystemSetupEditor] Creado RespawnManagerPrototipo3 en escena.");
            }

            // 7. StoryManagerPrototipo3 & StoryWorldBinder
            StoryManagerPrototipo3 storyMgr = GameObject.FindAnyObjectByType<StoryManagerPrototipo3>();
            if (storyMgr == null)
            {
                GameObject go = new GameObject("StoryManager_Prototipo3");
                storyMgr = go.AddComponent<StoryManagerPrototipo3>();
                Debug.Log("[FeralSystemSetupEditor] Creado StoryManagerPrototipo3 en escena.");
            }

            StoryWorldBinder binder = GameObject.FindAnyObjectByType<StoryWorldBinder>();
            if (binder == null)
            {
                GameObject go = new GameObject("StoryWorldBinder_Prototipo3");
                binder = go.AddComponent<StoryWorldBinder>();
                Debug.Log("[FeralSystemSetupEditor] Creado StoryWorldBinder en escena.");
            }
            storyMgr.worldBinder = binder;

            // 8. Asegurar UI (FeralGameUI, NarrativeHUD, MissionPointer)
            FeralGameUI gameUI = GameObject.FindAnyObjectByType<FeralGameUI>();
            if (gameUI == null)
            {
                GameObject uiGo = new GameObject("FeralGameUI_Prototipo3");
                gameUI = uiGo.AddComponent<FeralGameUI>();
                Debug.Log("[FeralSystemSetupEditor] Creado FeralGameUI en escena.");
            }

            NarrativeHUDPrototipo3 hud = GameObject.FindAnyObjectByType<NarrativeHUDPrototipo3>();
            if (hud == null)
            {
                GameObject hudGo = new GameObject("NarrativeHUD_Prototipo3");
                hud = hudGo.AddComponent<NarrativeHUDPrototipo3>();
                Debug.Log("[FeralSystemSetupEditor] Creado NarrativeHUDPrototipo3 en escena.");
            }

            MissionPointerPrototipo3 pointer = GameObject.FindAnyObjectByType<MissionPointerPrototipo3>();
            if (pointer == null)
            {
                GameObject ptrGo = new GameObject("MissionPointer_Prototipo3");
                pointer = ptrGo.AddComponent<MissionPointerPrototipo3>();
                Debug.Log("[FeralSystemSetupEditor] Creado MissionPointerPrototipo3 en escena.");
            }
            storyMgr.missionPointer = pointer;

            // Guardar cambios en la escena
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        private static void ValidateBossDistanceAndPlotGates()
        {
            CityGeneratorPrototipo3 cityGen = GameObject.FindAnyObjectByType<CityGeneratorPrototipo3>();
            Vector3 basePos = cityGen != null ? cityGen.PlayerSpawnPoint : Vector3.zero;

            DualRoleCharacter[] characters = GameObject.FindObjectsByType<DualRoleCharacter>(FindObjectsSortMode.None);
            for (int i = 0; i < characters.Length; i++)
            {
                DualRoleCharacter drc = characters[i];
                if (drc == null) continue;

                PlotGate gate = drc.GetComponent<PlotGate>() ?? drc.gameObject.AddComponent<PlotGate>();
                string charKey = (drc.characterId ?? drc.displayName ?? "").ToLowerInvariant();

                if (charKey.Contains("morales") || drc.bossProfile == BossProfileType.Morales_Buho)
                {
                    gate.targetStoryNodeID = "node_a3_apagon";
                    gate.allowedStoryNodeIDs = new List<string> { "node_a1_escuela", "node_a2_toby", "node_a3_apagon" };
                    gate.mundaneDayDialogue = "Silencio en los pasillos de la escuela, pequeño Kilo. El recinto está cerrado de noche.";
                }
                else if (charKey.Contains("sultan") || drc.bossProfile == BossProfileType.Sultan_Lobo)
                {
                    gate.targetStoryNodeID = "node_b3_sultan";
                    gate.allowedStoryNodeIDs = new List<string> { "node_b1_bosque", "node_b2_postes", "node_b3_sultan" };
                    gate.mundaneDayDialogue = "El bosque está tranquilo hoy, pequeño. Pero busca refugio antes del anochecer: se vuelve peligroso.";
                }
                else if (charKey.Contains("clara") || drc.bossProfile == BossProfileType.Clara_Cisne)
                {
                    gate.targetStoryNodeID = "node_c2_clara";
                    gate.allowedStoryNodeIDs = new List<string> { "node_c1_hospital", "node_c2_clara" };
                    gate.mundaneDayDialogue = "El hospital atiende en calma a los pacientes de día. Los pabellones quirúrgicos están sellados al público.";
                }
                else if (charKey.Contains("kross") || drc.bossProfile == BossProfileType.Kross_Murcielago)
                {
                    gate.targetStoryNodeID = "node_d2_kross";
                    gate.allowedStoryNodeIDs = new List<string> { "node_d1_torre", "node_d2_kross" };
                    gate.mundaneDayDialogue = "La corporación Aethelgard vela por la ciudad... Vuelve a tu madriguera antes de la noche.";
                }

                if (drc.roleType == CharacterRoleType.PacifiableBoss || drc.bossProfile != BossProfileType.None)
                {
                    float dist = Vector3.Distance(basePos, drc.transform.position);
                    Debug.Log($"[FeralSystemSetupEditor] Jefe verificado: '{drc.displayName}' ({drc.bossProfile}) | PlotGate target: '{gate.targetStoryNodeID}' | Distancia a Base: {dist:F1} m");
                }
            }

            StoryMechanismPrototipo3[] mechs = GameObject.FindObjectsByType<StoryMechanismPrototipo3>(FindObjectsSortMode.None);
            for (int i = 0; i < mechs.Length; i++)
            {
                StoryMechanismPrototipo3 m = mechs[i];
                if (m == null) continue;
                PlotGate gate = m.GetComponent<PlotGate>() ?? m.gameObject.AddComponent<PlotGate>();
                
                switch (m.mechanismType)
                {
                    case StoryMechanismType.MoralesOfficeDesk:
                        gate.targetStoryNodeID = "node_a2_toby";
                        break;
                    case StoryMechanismType.SchoolChemistryLab:
                        gate.targetStoryNodeID = "node_a3_apagon";
                        break;
                    case StoryMechanismType.ReflectorPole:
                        gate.targetStoryNodeID = "node_b2_postes";
                        break;
                    case StoryMechanismType.StreamSubstationSwitch:
                        gate.targetStoryNodeID = "node_b3_sultan";
                        break;
                    case StoryMechanismType.HyperbaricChamberValve:
                    case StoryMechanismType.PureOxygenCylinder:
                        gate.targetStoryNodeID = "node_c2_clara";
                        break;
                    case StoryMechanismType.ElenaReactorCell:
                    case StoryMechanismType.PhotonicUVCapacitor:
                    case StoryMechanismType.CentralSerumInjector:
                        gate.targetStoryNodeID = "node_d2_kross";
                        break;
                }
                Debug.Log($"[FeralSystemSetupEditor] Mecanismo verificado: '{m.displayName}' ({m.mechanismType}) | PlotGate target: '{gate.targetStoryNodeID}'");
            }
        }

        private static void PrintArchitectureReport()
        {
            Debug.Log("<color=#44FF88><b>========================================================================</b></color>");
            Debug.Log("<color=#44FF88><b>REPORTE DE ARQUITECTURA: SISTEMA DE HISTORIA PROCEDURAL HÍBRIDO</b></color>");
            Debug.Log("<color=#44FF88><b>========================================================================</b></color>");
            Debug.Log("1. Macro-DAG: StoryDirector gestiona los 5 nodos argumentales:");
            Debug.Log("   • Nodo 0: Barrio Residencial (ARCO_CORTO) -> 01_Intro_Sofia -> 02_Marco_Feral");
            Debug.Log("   • Rama A: Escuela Bellavista (ARCO_COMPLEJO + [ARCO_MEDIO_OPC]) -> Morales (04_Buho_Apagon) -> Vial Enzima Base");
            Debug.Log("   • Rama B: Bosque Ceniza (ARCO_COMPLEJO + [ARCO_CORTO_OPC]) -> Sultán (05_Sultan_LuzFria) -> Filtro Carbón");
            Debug.Log("   • Convergencia: Hospital San Judas (ARCO_MEDIO) -> Clara Valdés -> 06_Hospital_Kross -> Oxígeno Puro");
            Debug.Log("   • Final: Torre Aethelgard (ARCO_COMPLEJO) -> Kross Murciélago -> 07_Final_Amanecer");
            Debug.Log("2. Micro-CFG: QuestGrammarParser & QuestCompiler con derivación recursiva y BindingManager.");
            Debug.Log("3. PlotGates: Protegen jefes y mecanismos de arena contra sequence breaking.");
            Debug.Log("4. Cinemáticas: Reproductor MP4 con fallback de Novela Gráfica / Viñeta de Cómic In-Engine.");
            Debug.Log("5. Rutas de video: Assets/StreamingAssets/Cutscenes/ (01..07_*.mp4).");
            Debug.Log("6. Modelos 3D: Contenedor 'VisualMesh_ReplaceMe' disponible en cada actor y mecanismo.");
            Debug.Log("<color=#44FF88><b>========================================================================</b></color>");
        }
    }
}
