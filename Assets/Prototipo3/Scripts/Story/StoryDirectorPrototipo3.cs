using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Prototipo3
{
    public enum StoryPhase
    {
        SandboxDisabled = -1,
        TalkToMarcoFirst = 0,
        MarcoDayOrNightMission = 1,
        TrapTutorialPuasOrRed = 2,
        EscapeMarcoFeral = 3,
        CraftAllThreeTraps = 4,
        PlaceTrapAtTobyBuildingExits = 5,
        TalkToTobyAboutSchool = 6,
        EnterSchoolFirstTime = 7,
        ExploreSchoolAndMeetOwl = 8,
        FleeSchoolFirstTime = 9,
        ReturnToTobyForNetGun = 10,
        EnterSchoolFinalBattle = 11,
        FinalBossOwlBasketball = 12,
        DemoCompleted = 13
    }

    /// <summary>
    /// Director persistente de la Historia Lineal de Prototipo 3 (Ciudad Procedural + Escuela Infiltración + Cancha Final).
    /// Implementa exactamente el flujo narrativo, las bifurcaciones día/noche de Marco, la llamada cortada de la Dra. Elena,
    /// las misiones de Toby el Mapache, los 3 jefes y el cierre de la Demo.
    /// </summary>
    [DefaultExecutionOrder(-85)]
    public class StoryDirectorPrototipo3 : MonoBehaviour
    {
        public static StoryDirectorPrototipo3 Instance { get; private set; }

        public const string CitySceneName = "Prototipo3";
        public const string SchoolEscapeSceneName = "Escuela_Infiltracion_Prototipo3";
        public const string SchoolBasketballSceneName = "Escuela_CanchaFinal_Prototipo3";

        public const string CityScenePath = "Assets/Prototipo3/Prototipo3.unity";
        public const string SchoolEscapeScenePath = "Assets/Prototipo3/Scenes/Escuela_Infiltracion_Prototipo3.unity";
        public const string SchoolBasketballScenePath = "Assets/Prototipo3/Scenes/Escuela_CanchaFinal_Prototipo3.unity";

        private static bool pendingStoryMode = true;
        public static bool PendingStoryMode
        {
            get => pendingStoryMode;
            set => pendingStoryMode = value;
        }

        public static bool AllowFeralGlobally =>
            Instance == null || !Instance.storyModeEnabled || Instance.CurrentPhase != StoryPhase.TalkToMarcoFirst;

        public static bool IsDemoVictoryOpen => Instance != null && Instance.victoryModalOpen;

        [Header("Estado de la Campaña Lineal")]
        public bool storyModeEnabled = true;
        public StoryPhase CurrentPhase { get; private set; } = StoryPhase.TalkToMarcoFirst;

        public bool AllowFeralNight => !storyModeEnabled || CurrentPhase != StoryPhase.TalkToMarcoFirst;
        public bool CanEnterSchoolFromCity =>
            CurrentPhase == StoryPhase.EnterSchoolFirstTime || CurrentPhase == StoryPhase.EnterSchoolFinalBattle;

        // Referencias instanciadas en la escena actual
        private StoryObjectivePointerPrototipo3 objectivePointer;
        private GameObject marcoDayNpcInstance;
        private MarcoFeralBossAI marcoFeralBossInstance;
        private GameObject tobyCompoundRoot;
        private GameObject tobyNpcInstance;
        private GameObject schoolCityEntranceObj;
        private GameObject roamingGuardDogInstance;
        private Vector3 cachedMarcoEncounterSpot;
        private float nextPointerRefreshTime = 0f;
        private CraftingTable cachedCraftingTable;

        // Persistencia de la ciudad al viajar a la Escuela y volver
        private int savedCitySeed = 12345;
        private int savedCityWidth = 30;
        private int savedCityLength = 30;
        private bool returnSpawnNearSchoolHole = false;

        // Pantalla Final de Demo
        private Canvas victoryCanvas;
        private GameObject victoryRoot;
        private bool victoryModalOpen = false;

        // Colores de acento de diálogo
        private static readonly Color ColKilo = new Color(0.88f, 0.58f, 0.16f, 1f);
        private static readonly Color ColMarco = new Color(0.24f, 0.52f, 0.78f, 1f);
        private static readonly Color ColFeral = new Color(0.92f, 0.22f, 0.10f, 1f);
        private static readonly Color ColElena = new Color(0.16f, 0.72f, 0.88f, 1f);
        private static readonly Color ColToby = new Color(0.82f, 0.48f, 0.16f, 1f);
        private static readonly Color ColOwl = new Color(0.68f, 0.14f, 0.26f, 1f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Instance = null;
            pendingStoryMode = true;
            SceneManager.sceneLoaded -= OnAnySceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterBootstrap()
        {
            SceneManager.sceneLoaded -= OnAnySceneLoaded;
            SceneManager.sceneLoaded += OnAnySceneLoaded;
        }

        private static void OnAnySceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (IsStorySupportedScene(scene.name))
            {
                EnsureInstance().OnSupportedSceneEntered(scene);
            }
        }

        public static bool IsStorySupportedScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName)) return false;
            return sceneName.IndexOf("Prototipo3", StringComparison.OrdinalIgnoreCase) >= 0 &&
                   sceneName.IndexOf("MainMenu", StringComparison.OrdinalIgnoreCase) < 0 &&
                   sceneName.IndexOf("UIValidation", StringComparison.OrdinalIgnoreCase) < 0;
        }

        public static StoryDirectorPrototipo3 EnsureInstance()
        {
            if (Instance != null) return Instance;
            GameObject go = new GameObject("StoryDirector_Prototipo3");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<StoryDirectorPrototipo3>();
            return Instance;
        }

        private bool initializedActiveSceneOnce = false;
        private string lastInitializedSceneName = "";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            EnsureCrispGraphicsQuality();

            storyModeEnabled = pendingStoryMode;
            if (!storyModeEnabled)
            {
                CurrentPhase = StoryPhase.SandboxDisabled;
            }

            objectivePointer = gameObject.AddComponent<StoryObjectivePointerPrototipo3>();
            PortraitDialogueUIPrototipo3.EnsureInstance();
            BossHudPrototipo3.EnsureInstance();
            NetLauncherWeaponPrototipo3.EnsureInstance();
            BuildVictoryModalUI();
        }

        private void Start()
        {
            Scene active = SceneManager.GetActiveScene();
            if (IsStorySupportedScene(active.name) && (!initializedActiveSceneOnce || lastInitializedSceneName != active.name))
            {
                OnSupportedSceneEntered(active);
            }
        }

        public void ResetCampaignForNewGame(bool enableStory)
        {
            storyModeEnabled = enableStory;
            pendingStoryMode = enableStory;
            victoryModalOpen = false;
            if (victoryRoot != null) victoryRoot.SetActive(false);
            returnSpawnNearSchoolHole = false;

            if (NetLauncherWeaponPrototipo3.Instance != null)
            {
                NetLauncherWeaponPrototipo3.Instance.isUnlocked = false;
            }

            CurrentPhase = enableStory ? StoryPhase.TalkToMarcoFirst : StoryPhase.SandboxDisabled;

            if (MusicManagerPrototipo3.Instance != null)
            {
                MusicManagerPrototipo3.Instance.ClearVictoryOverride();
                MusicManagerPrototipo3.Instance.RefreshMusicImmediate();
            }
        }

        private void OnEnable()
        {
            TrapSystemPrototipo3.OnProto3TrapCrafted += HandleTrapCrafted;
        }

        private void OnDisable()
        {
            TrapSystemPrototipo3.OnProto3TrapCrafted -= HandleTrapCrafted;
        }

        private void OnSupportedSceneEntered(Scene scene)
        {
            initializedActiveSceneOnce = true;
            lastInitializedSceneName = scene.name;
            StopAllCoroutines();
            StartCoroutine(SetupSceneAfterLoadRoutine(scene.name));
        }

        private IEnumerator SetupSceneAfterLoadRoutine(string sceneName)
        {
            // Esperar un frame a que CityGeneratorPrototipo3 o los objetos de la Escuela inicialicen su Awake/Start
            yield return null;
            yield return null;

            if (CurrentPhase != StoryPhase.DemoCompleted && MusicManagerPrototipo3.Instance != null)
            {
                MusicManagerPrototipo3.Instance.ClearVictoryOverride();
            }
            MusicManagerPrototipo3.EnsureInstance().RefreshMusicImmediate();

            if (!storyModeEnabled)
            {
                objectivePointer.ClearTarget();
                yield break;
            }

            if (sceneName.IndexOf("Escuela_Infiltracion", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                SetupSchoolInfiltrationScene();
            }
            else if (sceneName.IndexOf("Escuela_CanchaFinal", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                SetupSchoolBasketballScene();
            }
            else
            {
                SetupProceduralCityStoryObjects();
            }
        }

        private void Update()
        {
            if (!storyModeEnabled) return;

            // Regla 1 estricta: Antes de hablar con Marco el Tejón NUNCA puede hacerse de noche / Feral
            if (CurrentPhase == StoryPhase.TalkToMarcoFirst && TimeManager.Instance != null)
            {
                TimeManager.Instance.pauseAutoAdvance = true;
                if (TimeManager.Instance.CurrentCycleState == TimeManager.CycleState.Night || TimeManager.Instance.CurrentHourFloat >= 16.5f)
                {
                    TimeManager.Instance.SetExactHour(9.5f);
                }
            }

            // Verificar progreso automático de crafteo de las 3 trampas (Fase 4)
            if (CurrentPhase == StoryPhase.CraftAllThreeTraps && HasAtLeastOneOfEveryTrap())
            {
                AdvanceToPlaceTrapAtTobyExits();
            }

            if (Time.unscaledTime >= nextPointerRefreshTime)
            {
                nextPointerRefreshTime = Time.unscaledTime + 0.20f;
                RefreshPointerTarget();
            }
        }

        private static void EnsureCrispGraphicsQuality()
        {
            if (QualitySettings.globalTextureMipmapLimit > 0)
            {
                QualitySettings.globalTextureMipmapLimit = 0;
            }
            if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp && urp.renderScale < 1f)
            {
                urp.renderScale = 1f;
            }
        }

        // ==========================================================================================
        //  TEXTOS DEL PANEL DE MISIONES EN FERALGAMEUI
        // ==========================================================================================

        public string GetHudPhaseTitle()
        {
            if (!storyModeEnabled) return null;
            switch (CurrentPhase)
            {
                case StoryPhase.TalkToMarcoFirst:
                case StoryPhase.MarcoDayOrNightMission:
                case StoryPhase.TrapTutorialPuasOrRed:
                case StoryPhase.EscapeMarcoFeral:
                    return "ACTO I  ·  EL HUMO DE SOLIS";

                case StoryPhase.CraftAllThreeTraps:
                case StoryPhase.PlaceTrapAtTobyBuildingExits:
                case StoryPhase.TalkToTobyAboutSchool:
                case StoryPhase.EnterSchoolFirstTime:
                    return "ACTO II  ·  EL TALLER DEL MAPACHE";

                case StoryPhase.ExploreSchoolAndMeetOwl:
                case StoryPhase.FleeSchoolFirstTime:
                case StoryPhase.ReturnToTobyForNetGun:
                    return "ACTO III  ·  ESCUELA BELLAVISTA";

                case StoryPhase.EnterSchoolFinalBattle:
                case StoryPhase.FinalBossOwlBasketball:
                    return "ACTO FINAL  ·  EL SILENCIADOR";

                case StoryPhase.DemoCompleted:
                    return "DEMO COMPLETADA  ·  VICTORIA";

                default:
                    return "MODO HISTORIA";
            }
        }

        public string GetHudObjectiveText()
        {
            if (!storyModeEnabled) return null;
            InventoryManager inv = InventoryManager.Instance;

            switch (CurrentPhase)
            {
                case StoryPhase.TalkToMarcoFirst:
                    return "Sigue la flecha dorada por la calle y habla con Marco el Tejón [F].";

                case StoryPhase.MarcoDayOrNightMission:
                    int m = inv != null ? inv.GetCount(ItemType.Metal) : 0;
                    int d = inv != null ? inv.GetCount(ItemType.Desperdicios) : 0;
                    return $"Misión de Marco: Reúne 2 Metal ({m}/2) y 1 Desperdicios ({d}/1) en contenedores y llévaselos a Marco [F].";

                case StoryPhase.TrapTutorialPuasOrRed:
                    return "Tutorial de Trampas: Ve a la Mesa de Fabricación [F] en tu refugio y fabrica una Trampa de Púas o de Red.";

                case StoryPhase.EscapeMarcoFeral:
                    return "¡Marco se volvió FERAL y es INVENCIBLE! ¡Corre a tu Refugio Seguro o aléjate por las calles para escapar!";

                case StoryPhase.CraftAllThreeTraps:
                    int cP = inv != null ? inv.GetTrapCount(TipoTrampa.Puas) : 0;
                    int cR = inv != null ? inv.GetTrapCount(TipoTrampa.Red) : 0;
                    int cM = inv != null ? inv.GetTrapCount(TipoTrampa.Madera) : 0;
                    return $"Prepara tus defensas antes de ir con el Mapache: ten 1 trampa de cada tipo — Púas ({cP}/1), Red ({cR}/1), Madera ({cM}/1).";

                case StoryPhase.PlaceTrapAtTobyBuildingExits:
                    return "Ve al Edificio del Mapache y coloca al menos 1 trampa en una de sus salidas ([1/2/3] + [F] en la puerta o en el umbral).";

                case StoryPhase.TalkToTobyAboutSchool:
                    return "Perímetro asegurado. Habla con Toby el Mapache [F] frente a su taller.";

                case StoryPhase.EnterSchoolFirstTime:
                    return "Dirígete a la alcantarilla de acceso a la High School y baja con [F] (¡Cuidado con el Perro Guardián!).";

                case StoryPhase.ExploreSchoolAndMeetOwl:
                    return "Explora los salones y pasillos de la High School hasta recuperar los Papeles de Helios-9 en la Dirección/Laboratorio [F].";

                case StoryPhase.FleeSchoolFirstTime:
                    return "¡EL BÚHO RECTOR TE PERSIGUE! ¡Corre por los salones hasta la alcantarilla de salida [F] para escapar!";

                case StoryPhase.ReturnToTobyForNetGun:
                    return "Regresa al taller y entrégale los papeles a Toby el Mapache [F].";

                case StoryPhase.EnterSchoolFinalBattle:
                    return "¡Pistola Lanza Redes equipada! Vuelve a la alcantarilla de la High School [F] para ir directo a la Cancha de Básquet.";

                case StoryPhase.FinalBossOwlBasketball:
                    return "Derriba al Búho con Reflectores UV [F] o esquivando sus picadas, y captúralo en el suelo con el Lanza Redes [CLICK IZQ / E].";

                case StoryPhase.DemoCompleted:
                    return "¡Has derrotado al Búho Rector y completado la Demo de FERAL!";

                default:
                    return "";
            }
        }

        // ==========================================================================================
        //  CONFIGURACIÓN EN LA CIUDAD PROCEDURAL (PROTOTIPO 3)
        // ==========================================================================================

        private void SetupProceduralCityStoryObjects()
        {
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.SetDayDuration(2.0f);
            }

            CityGeneratorPrototipo3 city = CityGeneratorPrototipo3.Instance;
            if (city != null)
            {
                savedCitySeed = city.seed;
                savedCityWidth = city.mapWidth;
                savedCityLength = city.mapLength;

                city.OnCityGenerated -= RebuildCityStoryActors;
                city.OnCityGenerated += RebuildCityStoryActors;
            }

            RebuildCityStoryActors();
        }

        private void RebuildCityStoryActors()
        {
            if (!storyModeEnabled) return;
            CityGeneratorPrototipo3 city = CityGeneratorPrototipo3.Instance;
            if (city == null) return;

            if (marcoDayNpcInstance != null) Destroy(marcoDayNpcInstance);
            if (tobyCompoundRoot != null) Destroy(tobyCompoundRoot);
            if (schoolCityEntranceObj != null) Destroy(schoolCityEntranceObj);
            if (roamingGuardDogInstance != null) Destroy(roamingGuardDogInstance);
            cachedCraftingTable = null;

            // Lista de exclusión mutua para garantizar que Marco, Toby y la Alcantarilla de la Escuela JAMÁS coincidan ni queden pegados!
            List<Vector3> occupiedLandmarks = new List<Vector3>
            {
                city.SafeRefugeCenter,
                city.SafeRefugeEntrancePoint
            };

            // 1. Sector Noreste: Marco el Tejón reparando tuberías (~46-58m del Refugio Seguro)
            Vector3 marcoPos = PickDistinctLandmarkSpot(city, occupiedLandmarks, 42f, 52f, 42f, new Vector2(0.86f, 0.50f));
            cachedMarcoEncounterSpot = marcoPos;
            occupiedLandmarks.Add(marcoPos);

            if (CurrentPhase <= StoryPhase.TrapTutorialPuasOrRed)
            {
                marcoDayNpcInstance = SpawnNpcCharacter(
                    "NPC_Marco_Tejon",
                    "Characters/Prefab_Marco_Day",
                    marcoPos,
                    Quaternion.LookRotation((city.SafeRefugeEntrancePoint - marcoPos).sqrMagnitude > 0.1f
                        ? new Vector3(city.SafeRefugeEntrancePoint.x - marcoPos.x, 0f, city.SafeRefugeEntrancePoint.z - marcoPos.z).normalized
                        : Vector3.back, Vector3.up),
                    StoryInteractableKind.MarcoNPC,
                    new Color(0.22f, 0.34f, 0.48f));
            }

            // 2. Sector Noroeste (opuesto a Marco, separación estricta >= 52m): Edificio/Taller de Toby el Mapache
            Vector3 tobySpot = PickDistinctLandmarkSpot(city, occupiedLandmarks, 46f, 68f, 52f, new Vector2(-0.86f, 0.50f));
            occupiedLandmarks.Add(tobySpot);
            BuildTobyBuildingCompound(tobySpot);

            // 3. Sector Sur (separado tanto de Marco como de Toby >= 50m): Alcantarilla de Acceso a la High School
            Vector3 schoolSpot = PickDistinctLandmarkSpot(city, occupiedLandmarks, 48f, 74f, 50f, new Vector2(0.05f, -0.99f));
            occupiedLandmarks.Add(schoolSpot);
            BuildSchoolCityEntrance(schoolSpot);

            // 4. Instanciar al Jefe Errante: EL PERRO GUARDIÁN patrullando entre el Taller del Mapache y la Escuela (lejos del refugio y de Marco)
            Vector3 dogDesired = Vector3.Lerp(tobySpot, schoolSpot, 0.52f);
            if (Vector3.Distance(dogDesired, city.SafeRefugeCenter) < 34f)
            {
                dogDesired = schoolSpot + (tobySpot - schoolSpot).normalized * 16f;
            }
            Vector3 dogSpot = FindClearNavMeshSpotNear(dogDesired, 14f);
            SpawnRoamingGuardDogBoss(dogSpot);

            // 5. Si regresamos huyendo de la Escuela (Fase 10), colocar a Kilo justo afuera del agujero de la Escuela y lanzar su reacción!
            if (returnSpawnNearSchoolHole && schoolCityEntranceObj != null)
            {
                returnSpawnNearSchoolHole = false;
                HamsterController hc = FindAnyObjectByType<HamsterControllerPrototipo3>() ?? FindAnyObjectByType<HamsterController>();
                if (hc != null)
                {
                    Vector3 safeOut = FindClearNavMeshSpotNear(schoolCityEntranceObj.transform.position + schoolCityEntranceObj.transform.forward * 3.2f, 4f);
                    hc.RespawnOrHeal(safeOut + Vector3.up * 0.08f);
                }

                if (TimeManager.Instance != null) TimeManager.Instance.SetExactHour(10.0f);

                PortraitDialogueUIPrototipo3.EnsureInstance().PlaySequence(new[]
                {
                    new DialogueLine("Kilo", "¡Uff, uff...! Por un pelo... Ese Búho Rector domina todo el aire de los pasillos.", SpeakerSide.Left, DialoguePortraitId.KiloScared, DialoguePortraitId.None, ColKilo),
                    new DialogueLine("Kilo", "Mis trampas del suelo no bastan para bajarlo del techo. Tengo que volver con Toby el Mapache, ¡seguro puede inventar algo para atraparlo!", SpeakerSide.Left, DialoguePortraitId.KiloNormal, DialoguePortraitId.TobyNormal, ColKilo)
                });
            }

            // 6. Si es inicio fresco de partida (Fase 0), asegurar horario matutino y diálogo breve de apertura
            if (CurrentPhase == StoryPhase.TalkToMarcoFirst && TimeManager.Instance != null)
            {
                TimeManager.Instance.SetExactHour(9.5f);
                TimeManager.Instance.pauseAutoAdvance = true;
            }

            if (NetLauncherWeaponPrototipo3.Instance != null && CurrentPhase >= StoryPhase.EnterSchoolFinalBattle)
            {
                NetLauncherWeaponPrototipo3.Instance.UnlockNetLauncher();
            }
        }

        private Vector3 PickDistinctLandmarkSpot(
            CityGeneratorPrototipo3 city,
            List<Vector3> occupiedPoints,
            float minDistFromRefuge,
            float idealDistFromRefuge,
            float minDistFromOccupied,
            Vector2 preferredSectorDir)
        {
            List<Vector3> candidates = new List<Vector3>(256);
            if (city.IntersectionPoints != null) candidates.AddRange(city.IntersectionPoints);
            if (city.LoopCenterPoints != null) candidates.AddRange(city.LoopCenterPoints);
            if (city.DeadEndPoints != null) candidates.AddRange(city.DeadEndPoints);

            // Incluir todas las celdas de calle para tener cientos de candidatos distribuidos en los 4 cuadrantes
            if (city.Grid != null)
            {
                for (int x = 2; x < city.Grid.Width - 2; x++)
                {
                    for (int y = 2; y < city.Grid.Length - 2; y++)
                    {
                        Vector2Int cell = new Vector2Int(x, y);
                        if (city.Grid.IsRoad(cell))
                        {
                            candidates.Add(city.CellToWorldCenter(cell));
                        }
                    }
                }
            }

            Vector2 prefDir = preferredSectorDir.sqrMagnitude > 0.001f ? preferredSectorDir.normalized : Vector2.up;
            Vector3 refuge = city.SafeRefugeCenter;
            Vector3 best = refuge + new Vector3(prefDir.x * idealDistFromRefuge, 0f, prefDir.y * idealDistFromRefuge);
            bool foundStrict = false;
            float bestScore = float.MinValue;

            // Pasada 1: cumple distancias mínimas estrictas respecto al Refugio y a todos los demás puntos clave
            for (int i = 0; i < candidates.Count; i++)
            {
                Vector3 c = candidates[i];
                float dRefuge = Vector3.Distance(c, refuge);
                if (dRefuge < minDistFromRefuge) continue;

                float minOther = float.MaxValue;
                bool tooClose = false;
                for (int j = 0; j < occupiedPoints.Count; j++)
                {
                    float dOcc = Vector3.Distance(c, occupiedPoints[j]);
                    if (dOcc < minOther) minOther = dOcc;
                    if (dOcc < minDistFromOccupied)
                    {
                        tooClose = true;
                        break;
                    }
                }
                if (tooClose) continue;

                Vector2 dir2D = new Vector2(c.x - refuge.x, c.z - refuge.z).normalized;
                float sectorDot = Vector2.Dot(dir2D, prefDir);
                float score = sectorDot * 35f - Mathf.Abs(dRefuge - idealDistFromRefuge) * 0.65f + Mathf.Min(minOther, 85f) * 0.25f;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = c;
                    foundStrict = true;
                }
            }

            // Pasada 2 de respaldo (mapas pequeños 20x20): maximizar la distancia mínima contra cualquier landmark ya colocado
            if (!foundStrict && candidates.Count > 0)
            {
                bestScore = float.MinValue;
                for (int i = 0; i < candidates.Count; i++)
                {
                    Vector3 c = candidates[i];
                    float dRefuge = Vector3.Distance(c, refuge);
                    if (dRefuge < 24f) continue;

                    float minOther = float.MaxValue;
                    for (int j = 0; j < occupiedPoints.Count; j++)
                    {
                        float dOcc = Vector3.Distance(c, occupiedPoints[j]);
                        if (dOcc < minOther) minOther = dOcc;
                    }
                    if (minOther < 26f) continue;

                    Vector2 dir2D = new Vector2(c.x - refuge.x, c.z - refuge.z).normalized;
                    float score = minOther * 1.5f + Vector2.Dot(dir2D, prefDir) * 20f;
                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = c;
                    }
                }
            }

            return FindClearNavMeshSpotNear(best, 10f);
        }

        private static Vector3 FindClearNavMeshSpotNear(Vector3 desired, float searchRadius)
        {
            if (NavMesh.SamplePosition(desired, out NavMeshHit hit, searchRadius, NavMesh.AllAreas))
            {
                return new Vector3(hit.position.x, 0.02f, hit.position.z);
            }
            return new Vector3(desired.x, 0.02f, desired.z);
        }

        private GameObject SpawnNpcCharacter(string name, string resPrefabPath, Vector3 pos, Quaternion rot, StoryInteractableKind kind, Color fallbackColor)
        {
            GameObject root = new GameObject(name);
            root.tag = "Interactable";
            root.transform.position = pos;
            root.transform.rotation = rot;

            BoxCollider trigger = root.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 1.0f, 0f);
            trigger.size = new Vector3(3.0f, 2.4f, 3.0f);

            CapsuleCollider solid = root.AddComponent<CapsuleCollider>();
            solid.isTrigger = false;
            solid.center = new Vector3(0f, 0.85f, 0f);
            solid.radius = 0.42f;
            solid.height = 1.7f;

            StoryInteractablePrototipo3 inter = root.AddComponent<StoryInteractablePrototipo3>();
            inter.kind = kind;

            GameObject prefab = Resources.Load<GameObject>(resPrefabPath);
            if (prefab != null)
            {
                GameObject vis = Instantiate(prefab, root.transform, false);
                vis.name = "VisualModel";
                vis.transform.localPosition = Vector3.zero;
                vis.transform.localRotation = Quaternion.identity;
            }
            else
            {
                GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                Destroy(fallback.GetComponent<Collider>());
                fallback.transform.SetParent(root.transform, false);
                fallback.transform.localPosition = new Vector3(0f, 0.9f, 0f);
                Shader sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                Material m = new Material(sh);
                m.color = fallbackColor;
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", fallbackColor);
                fallback.GetComponent<Renderer>().material = m;
            }

            return root;
        }

        private void BuildTobyBuildingCompound(Vector3 centerPos)
        {
            tobyCompoundRoot = new GameObject("Edificio_Taller_TobyMapache");
            tobyCompoundRoot.transform.position = centerPos;

            // Fachada del taller de Toby (modelo 3D de Blender SM_TobyWorkshopFacade)
            GameObject facadePrefab = Resources.Load<GameObject>("Weapons/Prefab_TobyWorkshopFacade");
            if (facadePrefab != null)
            {
                GameObject fac = Instantiate(facadePrefab, tobyCompoundRoot.transform, false);
                fac.transform.localPosition = new Vector3(0f, 0f, -2.2f);
            }

            // Cuerpo compacto del taller con 2 salidas con PuertaTrampa para la misión de prevención
            Shader sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material wallMat = new Material(sh);
            wallMat.color = new Color(0.22f, 0.25f, 0.30f, 1f);
            if (wallMat.HasProperty("_BaseColor")) wallMat.SetColor("_BaseColor", wallMat.color);

            GameObject bunker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bunker.name = "WorkshopStructure";
            bunker.transform.SetParent(tobyCompoundRoot.transform, false);
            bunker.transform.localPosition = new Vector3(0f, 1.6f, -4.4f);
            bunker.transform.localScale = new Vector3(5.6f, 3.2f, 3.8f);
            bunker.GetComponent<Renderer>().material = wallMat;
            NavMeshObstacle navObs = bunker.AddComponent<NavMeshObstacle>();
            navObs.carving = true;

            // Salida 1 (Frontal Este) y Salida 2 (Lateral Oeste) con PuertaTrampa + TobyExitTrapZone
            CreateTobyExitDoorway(tobyCompoundRoot.transform, "Salida_Taller_1", new Vector3(2.4f, 0.02f, -2.1f), Quaternion.identity);
            CreateTobyExitDoorway(tobyCompoundRoot.transform, "Salida_Taller_2", new Vector3(-2.4f, 0.02f, -2.1f), Quaternion.identity);

            tobyCompoundRoot.AddComponent<TobyExitTrapZonePrototipo3>();

            // Toby el Mapache esperando frente a su taller
            tobyNpcInstance = SpawnNpcCharacter(
                "NPC_Toby_Mapache",
                "Characters/Prefab_Toby_Mapache",
                centerPos + new Vector3(0f, 0f, 0.4f),
                Quaternion.identity,
                StoryInteractableKind.TobyNPC,
                new Color(0.45f, 0.42f, 0.46f));
            tobyNpcInstance.transform.SetParent(tobyCompoundRoot.transform, true);
        }

        private static void CreateTobyExitDoorway(Transform parent, string name, Vector3 localPos, Quaternion localRot)
        {
            GameObject doorRoot = new GameObject(name);
            doorRoot.tag = "Door";
            doorRoot.transform.SetParent(parent, false);
            doorRoot.transform.localPosition = localPos;
            doorRoot.transform.localRotation = localRot;

            Shader sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material frameMat = new Material(sh);
            frameMat.color = new Color(0.82f, 0.56f, 0.16f, 1f);
            if (frameMat.HasProperty("_BaseColor")) frameMat.SetColor("_BaseColor", frameMat.color);

            for (int side = -1; side <= 1; side += 2)
            {
                GameObject post = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(post.GetComponent<Collider>());
                post.transform.SetParent(doorRoot.transform, false);
                post.transform.localPosition = new Vector3(side * 0.9f, 1.05f, 0f);
                post.transform.localScale = new Vector3(0.18f, 2.1f, 0.22f);
                post.GetComponent<Renderer>().material = frameMat;
            }

            GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(pad.GetComponent<Collider>());
            pad.transform.SetParent(doorRoot.transform, false);
            pad.transform.localPosition = new Vector3(0f, 0.03f, 0f);
            pad.transform.localScale = new Vector3(1.85f, 0.06f, 1.25f);
            pad.GetComponent<Renderer>().material = frameMat;

            BoxCollider trig = doorRoot.AddComponent<BoxCollider>();
            trig.isTrigger = true;
            trig.center = new Vector3(0f, 1.0f, 0f);
            trig.size = new Vector3(2.4f, 2.2f, 2.2f);

            doorRoot.AddComponent<PuertaTrampa>();
        }

        private void BuildSchoolCityEntrance(Vector3 spot)
        {
            schoolCityEntranceObj = new GameObject("Alcantarilla_Entrada_HighSchool");
            schoolCityEntranceObj.tag = "Interactable";
            schoolCityEntranceObj.transform.position = new Vector3(spot.x, 0f, spot.z);

            GameObject holePrefab = Resources.Load<GameObject>("Weapons/Prefab_SchoolHoleEntrance");
            if (holePrefab != null)
            {
                GameObject vis = Instantiate(holePrefab, schoolCityEntranceObj.transform, false);
                vis.name = "VisualModel";
                vis.transform.localPosition = Vector3.zero;
            }
            else
            {
                GameObject fallback = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                Destroy(fallback.GetComponent<Collider>());
                fallback.transform.SetParent(schoolCityEntranceObj.transform, false);
                fallback.transform.localPosition = new Vector3(0f, 0.08f, 0f);
                fallback.transform.localScale = new Vector3(2.2f, 0.08f, 2.2f);
            }

            // Resplandor subterráneo sutil para ubicar claramente la alcantarilla en la calle
            GameObject sewerGlow = new GameObject("SewerSubterraneanGlow");
            sewerGlow.transform.SetParent(schoolCityEntranceObj.transform, false);
            sewerGlow.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            Light gl = sewerGlow.AddComponent<Light>();
            gl.type = LightType.Point;
            gl.range = 6.5f;
            gl.intensity = 2.2f;
            gl.color = new Color(0.20f, 0.88f, 1f, 1f);

            BoxCollider trig = schoolCityEntranceObj.AddComponent<BoxCollider>();
            trig.isTrigger = true;
            trig.center = new Vector3(0f, 0.9f, 0f);
            trig.size = new Vector3(3.6f, 2.2f, 3.6f);

            StoryInteractablePrototipo3 inter = schoolCityEntranceObj.AddComponent<StoryInteractablePrototipo3>();
            inter.kind = StoryInteractableKind.SchoolCityEntranceHole;
        }

        private void SpawnRoamingGuardDogBoss(Vector3 pos)
        {
            GameObject dogPrefab = Resources.Load<GameObject>("Characters/Prefab_Boss_PerroGuardian");
            if (dogPrefab != null)
            {
                roamingGuardDogInstance = Instantiate(dogPrefab, pos, Quaternion.identity);
                roamingGuardDogInstance.name = "Boss_PerroGuardian_Mapa";
                if (NavMesh.SamplePosition(pos, out NavMeshHit hit, 6f, NavMesh.AllAreas))
                {
                    NavMeshAgent ag = roamingGuardDogInstance.GetComponent<NavMeshAgent>();
                    if (ag != null) ag.Warp(hit.position);
                }
            }
        }

        // ==========================================================================================
        //  MANEJO DE INTERACCIONES Y SECUENCIAS DE HISTORIA
        // ==========================================================================================

        public void HandleInteractableUsed(StoryInteractablePrototipo3 inter, HamsterController player)
        {
            if (inter == null || PortraitDialogueUIPrototipo3.IsConversationActive) return;

            switch (inter.kind)
            {
                case StoryInteractableKind.MarcoNPC:
                    HandleMarcoTalk();
                    break;

                case StoryInteractableKind.TobyNPC:
                    HandleTobyTalk();
                    break;

                case StoryInteractableKind.SchoolCityEntranceHole:
                    HandleSchoolEntranceInCity();
                    break;

                case StoryInteractableKind.SchoolPandemicClueLab:
                    HandleSchoolLabClueInspected();
                    break;

                case StoryInteractableKind.SchoolEscapeHoleToCity:
                    HandleEscapeSchoolBackToCity();
                    break;
            }
        }

        private void HandleMarcoTalk()
        {
            InventoryManager inv = InventoryManager.Instance;

            if (CurrentPhase == StoryPhase.TalkToMarcoFirst)
            {
                PortraitDialogueUIPrototipo3.EnsureInstance().PlaySequence(new[]
                {
                    new DialogueLine("Marco el Tejón", "¡Hombre, pequeño Kilo! Qué gusto verte despierto. Desde que este humo ambarino de los cigarrillos Solis cubrió la cuadra, tengo una jaqueca terrible...", SpeakerSide.Right, DialoguePortraitId.KiloNormal, DialoguePortraitId.MarcoDay, ColMarco),
                    new DialogueLine("Kilo", "¡Hola Marco! La Dra. Elena se llevó a la pequeña Sofía al hospital por el humo. ¿Estás bien? Tienes los ojos un poco irritados.", SpeakerSide.Left, DialoguePortraitId.KiloNormal, DialoguePortraitId.MarcoDay, ColKilo),
                    new DialogueLine("Marco el Tejón", "No es nada, pequeñín, solo el cansancio. Oye, ¿me echas una pata antes de que oscurezca? Se reventó la válvula del edificio y necesito 2 de Metal y 1 de Desperdicios de los contenedores.", SpeakerSide.Right, DialoguePortraitId.KiloNormal, DialoguePortraitId.MarcoDay, ColMarco),
                    new DialogueLine("Kilo", "¡Cuenta conmigo! Iré a buscarlos por las calles y vuelvo enseguida.", SpeakerSide.Left, DialoguePortraitId.KiloNormal, DialoguePortraitId.MarcoDay, ColKilo)
                }, () =>
                {
                    CurrentPhase = StoryPhase.MarcoDayOrNightMission;
                    // Ciclo de día/noche más ágil (1.8 min por día completo y arrancando a las 13:30 PM para que la tarde/noche sí alcance al jugador!)
                    if (TimeManager.Instance != null)
                    {
                        TimeManager.Instance.SetExactHour(13.5f);
                        TimeManager.Instance.SetDayDuration(1.8f);
                        TimeManager.Instance.pauseAutoAdvance = false;
                    }
                    EnsureBonusLootNearRefuge();
                });
                return;
            }

            if (CurrentPhase == StoryPhase.MarcoDayOrNightMission)
            {
                bool hasItems = inv != null && inv.HasEnoughItem(ItemType.Metal, 2) && inv.HasEnoughItem(ItemType.Desperdicios, 1);
                if (!hasItems)
                {
                    PortraitDialogueUIPrototipo3.EnsureInstance().PlaySequence(new[]
                    {
                        new DialogueLine("Marco el Tejón", "Aún me faltan piezas, pequeñín: necesito 2 de Metal y 1 de Desperdicios de los contenedores de basura de la calle. ¡Date prisa, la cabeza me zumba peor con la tarde!", SpeakerSide.Right, DialoguePortraitId.KiloNormal, DialoguePortraitId.MarcoDay, ColMarco)
                    });
                    return;
                }

                inv.RemoveItem(ItemType.Metal, 2);
                inv.RemoveItem(ItemType.Desperdicios, 1);

                bool isNightNow = TimeManager.Instance != null && TimeManager.Instance.CurrentCycleState == TimeManager.CycleState.Night;
                if (isNightNow)
                {
                    // RAMA A: Cumplió la misión durante la noche -> Marco se vuelve Feral inmediatamente!
                    TriggerMarcoFeralTransformationSequence(fromNightTurnIn: true);
                }
                else
                {
                    // RAMA B: Cumplió la misión de día -> Llega tutorial de trampas (Púas o Red)!
                    EnsureStarterTrapTutorialMaterials();
                    PortraitDialogueUIPrototipo3.EnsureInstance().PlaySequence(new[]
                    {
                        new DialogueLine("Marco el Tejón", "¡Justo lo que necesitaba, gracias Kilo! Toma estas sobras de madera y goma del cuarto de calderas, a mí ya me tiemblan las patas...", SpeakerSide.Right, DialoguePortraitId.KiloNormal, DialoguePortraitId.MarcoDay, ColMarco),
                        new DialogueLine("Marco el Tejón", "Las calles se están poniendo raras al caer el sol. Ve a tu Mesa de Fabricación dentro del refugio y arma primero una Trampa de Púas o una Trampa de Red para saber defenderte.", SpeakerSide.Right, DialoguePortraitId.KiloNormal, DialoguePortraitId.MarcoDay, ColMarco),
                        new DialogueLine("Kilo", "¡Buena idea! Iré a fabricar una Trampa de Púas o de Red en mi mesa ahora mismo.", SpeakerSide.Left, DialoguePortraitId.KiloNormal, DialoguePortraitId.MarcoDay, ColKilo)
                    }, () =>
                    {
                        CurrentPhase = StoryPhase.TrapTutorialPuasOrRed;
                    });
                }
                return;
            }

            if (CurrentPhase == StoryPhase.TrapTutorialPuasOrRed)
            {
                PortraitDialogueUIPrototipo3.EnsureInstance().PlaySequence(new[]
                {
                    new DialogueLine("Marco el Tejón", "Entra a tu Refugio Seguro y usa la Mesa de Fabricación [F] para crear una Trampa de Púas o de Red... Ugh, ¡qué calor siento en las venas!", SpeakerSide.Right, DialoguePortraitId.KiloNormal, DialoguePortraitId.MarcoDay, ColMarco)
                });
            }
        }

        private void HandleTrapCrafted(TipoTrampa craftedType)
        {
            if (!storyModeEnabled) return;

            // Al terminar el Tutorial de Trampas (Púas o Red), forzamos sí o sí que sea de noche y salta la transformación Feral de Marco!
            if (CurrentPhase == StoryPhase.TrapTutorialPuasOrRed &&
                (craftedType == TipoTrampa.Puas || craftedType == TipoTrampa.Red))
            {
                if (UI.FeralGameUI.Instance != null && UI.FeralGameUI.Instance.CurrentPage == UI.FeralGameUI.Page.Crafting)
                {
                    UI.FeralGameUI.Instance.Close();
                }

                StartCoroutine(ForceNightAndMarcoFeralAfterTutorialRoutine());
                return;
            }

            if (CurrentPhase == StoryPhase.CraftAllThreeTraps && HasAtLeastOneOfEveryTrap())
            {
                if (UI.FeralGameUI.Instance != null && UI.FeralGameUI.Instance.CurrentPage == UI.FeralGameUI.Page.Crafting)
                {
                    UI.FeralGameUI.Instance.Close();
                }
                AdvanceToPlaceTrapAtTobyExits();
            }
        }

        private IEnumerator ForceNightAndMarcoFeralAfterTutorialRoutine()
        {
            yield return new WaitForSecondsRealtime(0.25f);
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.SetExactHour(19.2f);
            }
            TriggerMarcoFeralTransformationSequence(fromNightTurnIn: false);
        }

        private void TriggerMarcoFeralTransformationSequence(bool fromNightTurnIn)
        {
            CurrentPhase = StoryPhase.EscapeMarcoFeral;
            if (TimeManager.Instance != null && TimeManager.Instance.CurrentCycleState != TimeManager.CycleState.Night)
            {
                TimeManager.Instance.SetExactHour(19.2f);
            }

            BossHudPrototipo3.EnsureInstance().TriggerFlash(new Color(1f, 0.30f, 0.04f), 0.45f);

            // Si el jugador estaba en su refugio al terminar el tutorial de trampas, trasladar la escena a la plaza donde Marco colapsa (~46m del refugio)
            // para que tenga una persecución urbana completa de regreso hasta su Refugio Seguro!
            CityGeneratorPrototipo3 city = CityGeneratorPrototipo3.Instance;
            HamsterController hc = FindAnyObjectByType<HamsterControllerPrototipo3>() ?? FindAnyObjectByType<HamsterController>();
            Vector3 marcoSpawnPos = marcoDayNpcInstance != null
                ? marcoDayNpcInstance.transform.position
                : (cachedMarcoEncounterSpot.sqrMagnitude > 1f ? cachedMarcoEncounterSpot : Vector3.forward * 46f);

            if (!fromNightTurnIn && hc != null)
            {
                Vector3 toRefuge = city != null ? (city.SafeRefugeEntrancePoint - marcoSpawnPos) : Vector3.back;
                toRefuge.y = 0f;
                if (toRefuge.sqrMagnitude < 0.1f) toRefuge = Vector3.back;
                Vector3 playerStartNearMarco = FindClearNavMeshSpotNear(marcoSpawnPos + toRefuge.normalized * 7.5f, 6f);
                hc.RespawnOrHeal(playerStartNearMarco + Vector3.up * 0.05f);
            }

            if (marcoDayNpcInstance != null)
            {
                Destroy(marcoDayNpcInstance);
                marcoDayNpcInstance = null;
            }

            SpawnMarcoFeralBossAt(marcoSpawnPos);
            MusicManagerPrototipo3.EnsureInstance().RefreshMusicImmediate();

            PortraitDialogueUIPrototipo3.EnsureInstance().PlaySequence(new[]
            {
                new DialogueLine("Marco el Tejón", "¡AAAGH! ¡La cabeza...! ¡El humo de Solis me quema la sangre al ocultarse el sol!", SpeakerSide.Right, DialoguePortraitId.KiloScared, DialoguePortraitId.MarcoDay, ColFeral, glitch: true),
                new DialogueLine("MARCO (FERAL)", "¡¡GRRRRAAAAAGGGHH!!", SpeakerSide.Right, DialoguePortraitId.KiloScared, DialoguePortraitId.MarcoFeral, ColFeral),
                new DialogueLine("Kilo", "¡¿Marco?! ¡Sus venas brillan como brasas! ¡Es demasiado grande y fuerte, NO PUEDO VENCERLO... TENGO QUE ESCAPAR!", SpeakerSide.Left, DialoguePortraitId.KiloScared, DialoguePortraitId.MarcoFeral, ColFeral)
            });
        }

        private void SpawnMarcoFeralBossAt(Vector3 pos)
        {
            if (marcoFeralBossInstance != null) Destroy(marcoFeralBossInstance.gameObject);

            GameObject bossPrefab = Resources.Load<GameObject>("Characters/Prefab_Marco_Feral");
            GameObject bossGo;
            if (bossPrefab != null)
            {
                bossGo = Instantiate(bossPrefab, pos, Quaternion.identity);
            }
            else
            {
                bossGo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                bossGo.transform.position = pos;
                bossGo.transform.localScale = new Vector3(1.4f, 1.5f, 1.4f);
                bossGo.AddComponent<NavMeshAgent>();
                bossGo.AddComponent<MarcoFeralBossAI>();
            }

            bossGo.name = "Boss_Marco_Tejon_Feral";
            if (NavMesh.SamplePosition(pos, out NavMeshHit hit, 6f, NavMesh.AllAreas))
            {
                NavMeshAgent ag = bossGo.GetComponent<NavMeshAgent>();
                if (ag != null) ag.Warp(hit.position);
            }

            marcoFeralBossInstance = bossGo.GetComponent<MarcoFeralBossAI>();
            if (marcoFeralBossInstance == null) marcoFeralBossInstance = bossGo.AddComponent<MarcoFeralBossAI>();
            marcoFeralBossInstance.OnPlayerEscapedMarco += HandleEscapedFromMarcoFeral;
        }

        private void HandleEscapedFromMarcoFeral()
        {
            if (marcoFeralBossInstance != null)
            {
                marcoFeralBossInstance.OnPlayerEscapedMarco -= HandleEscapedFromMarcoFeral;
                Destroy(marcoFeralBossInstance.gameObject);
                marcoFeralBossInstance = null;
            }

            // Amanecer tras sobrevivir y escapar de Marco Feral + Cuadro de texto de escape + Llamada de Elena cortada + Mapache!
            if (TimeManager.Instance != null)
            {
                TimeManager.Instance.ForceDawnSunrise();
            }
            MusicManagerPrototipo3.EnsureInstance().RefreshMusicImmediate();

            EnsureExtraCraftingLootAroundRefuge();

            PortraitDialogueUIPrototipo3.EnsureInstance().PlaySequence(new[]
            {
                // 1) Cuadro de texto inmediato al escapar de Marco
                new DialogueLine("Kilo", "*Jadeando*... Lo perdí de vista... Pobre Marco, al caer la noche dejó de reconocernos por completo.", SpeakerSide.Left, DialoguePortraitId.KiloScared, DialoguePortraitId.None, ColKilo),
                // 2) Llamada de la Dra. Elena Vargas (mamá adoptiva doctora de la conejita Sofía)
                new DialogueLine("Dra. Elena (Radio)", "¿Kilo? ¡Kilo, me copias por el collar! Soy Elena desde el Hospital San Judas... Sofía sigue débil en la carpa de oxígeno...", SpeakerSide.Right, DialoguePortraitId.KiloNormal, DialoguePortraitId.ElenaRadio, ColElena),
                new DialogueLine("Dra. Elena (Radio)", "Escúchame, el compuesto Helios-9 del tabaco está mutando de noche. Para sintetizar la cura de Sofía necesito que busques— ¡KZZZT! —no confíes en Aethel— ¡BZZZT—!", SpeakerSide.Right, DialoguePortraitId.KiloScared, DialoguePortraitId.ElenaRadio, ColElena, glitch: true),
                new DialogueLine("Kilo", "¡¿Doctora Elena?! ¡¿Elena?! ¡Se cortó la llamada!", SpeakerSide.Left, DialoguePortraitId.KiloScared, DialoguePortraitId.None, ColFeral),
                // 3) Entra Toby el Mapache en la frecuencia
                new DialogueLine("??? (Mapache en Radio)", "¿Hola? ¿Hola, roedor del collar? Soy Toby. Las antenas del hospital acaban de ser bloqueadas, pero intercepté tu señal.", SpeakerSide.Right, DialoguePortraitId.KiloNormal, DialoguePortraitId.TobyNormal, ColToby),
                new DialogueLine("Toby el Mapache", "Tienes que confiar en mí: ven a mi edificio-taller en el sector este e intentaremos solucionarlo juntos. Sigue mi radiobaliza.", SpeakerSide.Right, DialoguePortraitId.KiloNormal, DialoguePortraitId.TobyNormal, ColToby),
                // 4) Prevención de Kilo: craftear 1 trampa de cada tipo antes de ir con el Mapache
                new DialogueLine("Kilo", "No conozco a ese mapache... y después de lo que pasó con Marco, no pienso ir desprevenido. Antes de ir a su edificio, fabricaré las 3 trampas —una de cada tipo: Púas, Red y Madera— para estar listo.", SpeakerSide.Left, DialoguePortraitId.KiloNormal, DialoguePortraitId.TobyNormal, ColKilo)
            }, () =>
            {
                CurrentPhase = StoryPhase.CraftAllThreeTraps;
                if (HasAtLeastOneOfEveryTrap())
                {
                    AdvanceToPlaceTrapAtTobyExits();
                }
            });
        }

        private static bool HasAtLeastOneOfEveryTrap()
        {
            InventoryManager inv = InventoryManager.Instance;
            if (inv == null) return false;
            return inv.GetTrapCount(TipoTrampa.Puas) >= 1 &&
                   inv.GetTrapCount(TipoTrampa.Red) >= 1 &&
                   inv.GetTrapCount(TipoTrampa.Madera) >= 1;
        }

        private void AdvanceToPlaceTrapAtTobyExits()
        {
            if (CurrentPhase != StoryPhase.CraftAllThreeTraps) return;
            CurrentPhase = StoryPhase.PlaceTrapAtTobyBuildingExits;

            PortraitDialogueUIPrototipo3.EnsureInstance().PlaySequence(new[]
            {
                new DialogueLine("Kilo", "¡Listo! Ya llevo Trampa de Púas, de Red y de Madera en la mochila. Ahora vamos hacia el edificio del Mapache.", SpeakerSide.Left, DialoguePortraitId.KiloNormal, DialoguePortraitId.TobyNormal, ColKilo),
                new DialogueLine("Toby (Radio)", "¡Oye pequeñín! Cuando llegues a mi edificio, pon al menos una trampa en una de las salidas exteriores. Un Perro Guardián enorme anda rondando estas calles.", SpeakerSide.Right, DialoguePortraitId.KiloNormal, DialoguePortraitId.TobyNormal, ColToby)
            });
        }

        public void NotifyTrapPlacedAtTobyExit(TipoTrampa tipo)
        {
            if (!storyModeEnabled || CurrentPhase != StoryPhase.PlaceTrapAtTobyBuildingExits) return;
            CurrentPhase = StoryPhase.TalkToTobyAboutSchool;

            if (UI.FeralGameUI.Instance != null)
            {
                UI.FeralGameUI.Instance.ShowLootFeedback($"¡Salida del edificio asegurada con {TrapSystemPrototipo3.GetTrapDisplayName(tipo)}! Habla con Toby [F].", 3.5f);
            }
        }

        private void HandleTobyTalk()
        {
            if (CurrentPhase < StoryPhase.PlaceTrapAtTobyBuildingExits)
            {
                PortraitDialogueUIPrototipo3.EnsureInstance().PlaySequence(new[]
                {
                    new DialogueLine("Toby el Mapache", "¡Veo que ya encontraste mi taller! Pero primero asegúrate de traer fabricada una trampa de cada tipo (Púas, Red y Madera) por seguridad.", SpeakerSide.Right, DialoguePortraitId.KiloNormal, DialoguePortraitId.TobyNormal, ColToby)
                });
                return;
            }

            if (CurrentPhase == StoryPhase.PlaceTrapAtTobyBuildingExits)
            {
                PortraitDialogueUIPrototipo3.EnsureInstance().PlaySequence(new[]
                {
                    new DialogueLine("Toby el Mapache", "¡Casi listos, compañero! Antes de revisar mis planos, coloca al menos una trampa en cualquiera de las dos puertas de salida de mi edificio ([1/2/3] + [F] en la puerta).", SpeakerSide.Right, DialoguePortraitId.KiloNormal, DialoguePortraitId.TobyNormal, ColToby)
                });
                return;
            }

            if (CurrentPhase == StoryPhase.TalkToTobyAboutSchool)
            {
                PortraitDialogueUIPrototipo3.EnsureInstance().PlaySequence(new[]
                {
                    new DialogueLine("Toby el Mapache", "¡Buen trabajo asegurando las salidas! Soy Toby Méndez. Escucha, rastreé la señal de la Dra. Elena y los registros químicos de la ciudad.", SpeakerSide.Right, DialoguePortraitId.KiloNormal, DialoguePortraitId.TobyNormal, ColToby),
                    new DialogueLine("Toby el Mapache", "Por mis últimas investigaciones, descubrí que uno de los focos principales de la pandemia se originó en la High School Bellavista. Tienes que ir a investigar el laboratorio.", SpeakerSide.Right, DialoguePortraitId.KiloNormal, DialoguePortraitId.TobyNormal, ColToby),
                    new DialogueLine("Toby el Mapache", "Pero ten muchísimo cuidado, Kilo... Hay reportes de un animal extremadamente peligroso acechando ahí dentro. Entra por el agujero del muro escolar.", SpeakerSide.Right, DialoguePortraitId.KiloScared, DialoguePortraitId.TobyNormal, ColFeral),
                    new DialogueLine("Kilo", "Entendido. Me colaré por el agujero de la High School, investigaré el foco de infección y volveré con pruebas para ayudar a Sofía.", SpeakerSide.Left, DialoguePortraitId.KiloNormal, DialoguePortraitId.TobyNormal, ColKilo)
                }, () =>
                {
                    CurrentPhase = StoryPhase.EnterSchoolFirstTime;
                });
                return;
            }

            if (CurrentPhase == StoryPhase.EnterSchoolFirstTime)
            {
                PortraitDialogueUIPrototipo3.EnsureInstance().PlaySequence(new[]
                {
                    new DialogueLine("Toby el Mapache", "La High School está siguiendo la flecha dorada. Cuela tu cuerpo por el agujero en la pared de ladrillos y mantén los ojos bien abiertos.", SpeakerSide.Right, DialoguePortraitId.KiloNormal, DialoguePortraitId.TobyNormal, ColToby)
                });
                return;
            }

            if (CurrentPhase == StoryPhase.ReturnToTobyForNetGun)
            {
                PortraitDialogueUIPrototipo3.EnsureInstance().PlaySequence(new[]
                {
                    new DialogueLine("Kilo", "¡Toby! ¡El foco de Helios-9 sí estaba en el laboratorio de la escuela, pero el Rector Morales —un búho gigante— se volvió feral y casi me atrapa!", SpeakerSide.Left, DialoguePortraitId.KiloScared, DialoguePortraitId.TobyNormal, ColKilo),
                    new DialogueLine("Toby el Mapache", "¡¿El Rector Morales?! ¡Con razón nadie salía! Contra un búho volador las trampas de piso no bastan... ¡Por suerte acabo de terminar mi mejor invento!", SpeakerSide.Right, DialoguePortraitId.KiloNormal, DialoguePortraitId.TobyNormal, ColToby),
                    new DialogueLine("Toby el Mapache", "¡Toma esta PISTOLA LANZA REDES neumática! Dispara con [CLICK IZQUIERDO] o [E] para enredar sus alas, derribarlo e inmovilizarlo sin matarlo.", SpeakerSide.Right, DialoguePortraitId.KiloNormal, DialoguePortraitId.TobyNormal, ColElena),
                    new DialogueLine("Kilo", "¡Increíble, calza perfecto en mi arnés! Volveré a la High School ahora mismo y detendré al Búho Rector.", SpeakerSide.Left, DialoguePortraitId.KiloNormal, DialoguePortraitId.TobyNormal, ColKilo)
                }, () =>
                {
                    NetLauncherWeaponPrototipo3.EnsureInstance().UnlockNetLauncher();
                    CurrentPhase = StoryPhase.EnterSchoolFinalBattle;
                    if (UI.FeralGameUI.Instance != null)
                    {
                        UI.FeralGameUI.Instance.ShowLootFeedback("¡PISTOLA LANZA REDES OBTENIDA! Dispara con [CLICK IZQUIERDO] o [E].", 4.2f);
                    }
                });
                return;
            }

            if (CurrentPhase == StoryPhase.EnterSchoolFinalBattle)
            {
                PortraitDialogueUIPrototipo3.EnsureInstance().PlaySequence(new[]
                {
                    new DialogueLine("Toby el Mapache", "¡El Búho Rector se atrincheró en la Cancha de Básquetbol! Entra a la escuela y derrótalo con el Lanza Redes [CLICK IZQ / E].", SpeakerSide.Right, DialoguePortraitId.KiloNormal, DialoguePortraitId.TobyNormal, ColToby)
                });
            }
        }

        private void HandleSchoolEntranceInCity()
        {
            if (CurrentPhase == StoryPhase.EnterSchoolFirstTime)
            {
                SaveCityParametersBeforeTransition();
                CurrentPhase = StoryPhase.ExploreSchoolAndMeetOwl;
                LoadSceneSafe(SchoolEscapeScenePath, SchoolEscapeSceneName);
            }
            else if (CurrentPhase == StoryPhase.EnterSchoolFinalBattle)
            {
                SaveCityParametersBeforeTransition();
                CurrentPhase = StoryPhase.FinalBossOwlBasketball;
                LoadSceneSafe(SchoolBasketballScenePath, SchoolBasketballSceneName);
            }
            else
            {
                if (UI.FeralGameUI.Instance != null)
                {
                    UI.FeralGameUI.Instance.ShowLootFeedback("Aún no tienes motivos para entrar aquí. Sigue tu misión actual.", 2.6f);
                }
            }
        }

        private void SaveCityParametersBeforeTransition()
        {
            CityGeneratorPrototipo3 city = CityGeneratorPrototipo3.Instance;
            if (city != null)
            {
                savedCitySeed = city.seed;
                savedCityWidth = city.mapWidth;
                savedCityLength = city.mapLength;
            }
        }

        // ==========================================================================================
        //  ESCENARIO 1 DE LA ESCUELA: INFILTRACIÓN, DESCUBRIMIENTO Y HUIDA DEL BÚHO RECTOR
        // ==========================================================================================

        private void SetupSchoolInfiltrationScene()
        {
            CurrentPhase = StoryPhase.ExploreSchoolAndMeetOwl;
            EnsureSolidFloorSafetyRuntime(62f, 44f, new Color(0.24f, 0.26f, 0.30f));

            OwlRectorBossAI owl = FindAnyObjectByType<OwlRectorBossAI>(FindObjectsInactive.Include);
            if (owl != null)
            {
                owl.mode = OwlRectorBossAI.EncounterMode.SchoolFirstEscape;
                owl.gameObject.SetActive(false);
            }

            StoryInteractablePrototipo3[] inters = FindObjectsByType<StoryInteractablePrototipo3>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < inters.Length; i++)
            {
                if (inters[i].kind == StoryInteractableKind.SchoolEscapeHoleToCity)
                {
                    inters[i].gameObject.SetActive(false);
                }
            }

            PortraitDialogueUIPrototipo3.EnsureInstance().PlaySequence(new[]
            {
                new DialogueLine("Kilo", "Ya estoy dentro de la High School Bellavista... Es enorme, hay salones, casilleros y oficinas en penumbras.", SpeakerSide.Left, DialoguePortraitId.KiloNormal, DialoguePortraitId.None, ColKilo),
                new DialogueLine("Toby (Radio)", "Cruza los pasillos y salones hasta el ala este, Kilo. Busca los papeles confidenciales del experimento Helios-9 en el escritorio de Dirección/Laboratorio.", SpeakerSide.Right, DialoguePortraitId.KiloNormal, DialoguePortraitId.TobyNormal, ColToby)
            });
        }

        private static void EnsureSolidFloorSafetyRuntime(float width, float length, Color fallbackColor)
        {
            if (GameObject.Find("RuntimeSafetyFoundationFloor") != null) return;
            // Garantizar siempre una losa sólida de respaldo bajo y=0 por si algún FBX tardó en importar
            GameObject safety = GameObject.CreatePrimitive(PrimitiveType.Cube);
            safety.name = "RuntimeSafetyFoundationFloor";
            safety.transform.position = new Vector3(0f, -0.52f, 0f);
            safety.transform.localScale = new Vector3(width, 1.0f, length);
            Shader sh = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material m = new Material(sh);
            m.color = fallbackColor;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", fallbackColor);
            safety.GetComponent<Renderer>().sharedMaterial = m;
        }

        private void HandleSchoolLabClueInspected()
        {
            if (CurrentPhase != StoryPhase.ExploreSchoolAndMeetOwl) return;
            CurrentPhase = StoryPhase.FleeSchoolFirstTime;

            // Activar al Búho Rector en modo emboscada/escape y habilitar el agujero de salida
            OwlRectorBossAI owl = FindAnyObjectByType<OwlRectorBossAI>(FindObjectsInactive.Include);
            if (owl != null)
            {
                owl.mode = OwlRectorBossAI.EncounterMode.SchoolFirstEscape;
                owl.gameObject.SetActive(true);
            }

            StoryInteractablePrototipo3[] inters = FindObjectsByType<StoryInteractablePrototipo3>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < inters.Length; i++)
            {
                if (inters[i].kind == StoryInteractableKind.SchoolEscapeHoleToCity)
                {
                    inters[i].gameObject.SetActive(true);
                }
                else if (inters[i].kind == StoryInteractableKind.SchoolPandemicClueLab)
                {
                    inters[i].gameObject.SetActive(false);
                }
            }

            BossHudPrototipo3.EnsureInstance().TriggerFlash(new Color(0.95f, 0.22f, 0.08f), 0.42f);
            MusicManagerPrototipo3.EnsureInstance().RefreshMusicImmediate();

            PortraitDialogueUIPrototipo3.EnsureInstance().PlaySequence(new[]
            {
                new DialogueLine("Kilo", "¡Cápsulas presurizadas de Helios-9 rotas! Aquí experimentaron con la fórmula concentrada... ¡Toby tenía razón!", SpeakerSide.Left, DialoguePortraitId.KiloNormal, DialoguePortraitId.None, ColKilo),
                new DialogueLine("BÚHO RECTOR", "¡¡Uuuu-HUUU!! ¡SILENCIO EN LOS PASILLOS! ¡Ningún intruso saldrá del plantel sin castigo!", SpeakerSide.Right, DialoguePortraitId.KiloScared, DialoguePortraitId.BuhoFeral, ColOwl),
                new DialogueLine("Kilo", "¡¿Ese es el Búho Rector?! ¡Está volando sobre los casilleros, no tengo con qué bajarlo! ¡TENGO QUE HUIR POR EL AGUJERO DE ENTRADA!", SpeakerSide.Left, DialoguePortraitId.KiloScared, DialoguePortraitId.BuhoFeral, ColFeral)
            });
        }

        private void HandleEscapeSchoolBackToCity()
        {
            if (CurrentPhase != StoryPhase.FleeSchoolFirstTime) return;

            BossHudPrototipo3.EnsureInstance().HideBoss();
            CurrentPhase = StoryPhase.ReturnToTobyForNetGun;
            returnSpawnNearSchoolHole = true;

            CityGeneratorPrototipo3.SetPendingCityConfig(savedCitySeed, savedCityWidth, savedCityLength);
            LoadSceneSafe(CityScenePath, CitySceneName);
        }

        // ==========================================================================================
        //  ESCENARIO 2 DE LA ESCUELA: CANCHA DE BÁSQUETBOL (PELEA FINAL Y FIN DE DEMO)
        // ==========================================================================================

        private void SetupSchoolBasketballScene()
        {
            CurrentPhase = StoryPhase.FinalBossOwlBasketball;
            EnsureSolidFloorSafetyRuntime(44f, 32f, new Color(0.42f, 0.12f, 0.14f));
            NetLauncherWeaponPrototipo3.EnsureInstance().UnlockNetLauncher();

            HamsterController hc = FindAnyObjectByType<HamsterControllerPrototipo3>() ?? FindAnyObjectByType<HamsterController>();
            if (hc != null) hc.RespawnOrHeal();

            OwlRectorBossAI owl = FindAnyObjectByType<OwlRectorBossAI>(FindObjectsInactive.Include);
            if (owl != null)
            {
                owl.mode = OwlRectorBossAI.EncounterMode.BasketballFinalBoss;
                owl.gameObject.SetActive(true);
                owl.OnOwlDefeated -= HandleFinalOwlBossDefeated;
                owl.OnOwlDefeated += HandleFinalOwlBossDefeated;
            }

            PortraitDialogueUIPrototipo3.EnsureInstance().PlaySequence(new[]
            {
                new DialogueLine("BÚHO RECTOR", "¡¿Te atreves a regresar a mi gimnasio, pequeña alimaña?! ¡Esta cancha será tu jaula eterna!", SpeakerSide.Right, DialoguePortraitId.KiloNormal, DialoguePortraitId.BuhoFeral, ColOwl),
                new DialogueLine("Toby (Radio)", "¡Usa la Pistola Lanza Redes con [CLICK IZQUIERDO] o [E], Kilo! También puedes encender los Reflectores UV de la cancha con [F] para cegarlo y derribarlo.", SpeakerSide.Right, DialoguePortraitId.KiloNormal, DialoguePortraitId.TobyNormal, ColElena),
                new DialogueLine("Kilo", "¡No vengo a hacerte daño, Rector Morales, vengo a detener esta locura y salvar a Sofía! ¡Vamos allá!", SpeakerSide.Left, DialoguePortraitId.KiloNormal, DialoguePortraitId.BuhoFeral, ColKilo)
            });
        }

        private void HandleFinalOwlBossDefeated()
        {
            CurrentPhase = StoryPhase.DemoCompleted;
            MusicManagerPrototipo3.EnsureInstance().PlayDemoVictoryMusicImmediate();
            BossHudPrototipo3.EnsureInstance().TriggerFlash(new Color(0.25f, 0.95f, 1f), 0.55f);

            PortraitDialogueUIPrototipo3.EnsureInstance().PlaySequence(new[]
            {
                new DialogueLine("Búho Rector", "*Envuelto en la red energética*... Ugh... ¿Qué... qué estoy haciendo en el gimnasio...? Mi cabeza empieza a despejarse...", SpeakerSide.Right, DialoguePortraitId.KiloNormal, DialoguePortraitId.BuhoFeral, ColElena),
                new DialogueLine("Toby el Mapache", "¡¡LO LOGRASTE, KILO!! ¡La red descargó su estática bioeléctrica y aseguramos las muestras puras de la escuela!", SpeakerSide.Right, DialoguePortraitId.KiloNormal, DialoguePortraitId.TobyNormal, ColToby),
                new DialogueLine("Dra. Elena (Radio)", "*Señal restablecida* ¡Kilo, Toby! ¡Recibo los datos de la Enzima Base! Con esto podremos estabilizar a Sofía en el hospital... ¡Gracias, pequeño héroe!", SpeakerSide.Right, DialoguePortraitId.KiloNormal, DialoguePortraitId.ElenaRadio, ColElena),
                new DialogueLine("Kilo", "¡Resiste un poco más, Sofía! Esto apenas comienza, pero juntos devolveremos el amanecer a toda Nueva Ocaso.", SpeakerSide.Left, DialoguePortraitId.KiloNormal, DialoguePortraitId.ElenaRadio, ColKilo)
            }, () =>
            {
                OpenDemoVictoryScreen();
            });
        }

        // ==========================================================================================
        //  PUNTERO DE OBJETIVOS Y AYUDAS ANTI-BLOQUEO DE RECURSOS
        // ==========================================================================================

        private void RefreshPointerTarget()
        {
            if (objectivePointer == null) return;

            switch (CurrentPhase)
            {
                case StoryPhase.TalkToMarcoFirst:
                    objectivePointer.SetTarget(marcoDayNpcInstance != null ? marcoDayNpcInstance.transform : null);
                    break;

                case StoryPhase.MarcoDayOrNightMission:
                    InventoryManager inv = InventoryManager.Instance;
                    bool ready = inv != null && inv.HasEnoughItem(ItemType.Metal, 2) && inv.HasEnoughItem(ItemType.Desperdicios, 1);
                    if (ready && marcoDayNpcInstance != null)
                    {
                        objectivePointer.SetTarget(marcoDayNpcInstance.transform);
                    }
                    else
                    {
                        LootContainer nearestLoot = FindNearestUnlootedContainer();
                        objectivePointer.SetTarget(nearestLoot != null ? nearestLoot.transform : (marcoDayNpcInstance != null ? marcoDayNpcInstance.transform : null));
                    }
                    break;

                case StoryPhase.TrapTutorialPuasOrRed:
                case StoryPhase.CraftAllThreeTraps:
                    if (cachedCraftingTable == null || !cachedCraftingTable.gameObject.activeInHierarchy)
                        cachedCraftingTable = FindAnyObjectByType<CraftingTable>();
                    if (CurrentPhase == StoryPhase.CraftAllThreeTraps && !CanCraftRemainingTrapsSoon())
                    {
                        LootContainer lc = FindNearestUnlootedContainer();
                        if (lc != null) { objectivePointer.SetTarget(lc.transform); break; }
                    }
                    objectivePointer.SetTarget(cachedCraftingTable != null ? cachedCraftingTable.transform : null);
                    break;

                case StoryPhase.EscapeMarcoFeral:
                    if (SafeRefugeZone.ActiveZone != null)
                        objectivePointer.SetTarget(SafeRefugeZone.ActiveZone.transform);
                    else if (CityGeneratorPrototipo3.Instance != null && CityGeneratorPrototipo3.Instance.SafeRefugeCenter != Vector3.zero)
                        objectivePointer.SetTarget(null, CityGeneratorPrototipo3.Instance.SafeRefugeCenter);
                    else
                        objectivePointer.ClearTarget();
                    break;

                case StoryPhase.PlaceTrapAtTobyBuildingExits:
                case StoryPhase.TalkToTobyAboutSchool:
                case StoryPhase.ReturnToTobyForNetGun:
                    objectivePointer.SetTarget(tobyNpcInstance != null ? tobyNpcInstance.transform : (tobyCompoundRoot != null ? tobyCompoundRoot.transform : null));
                    break;

                case StoryPhase.EnterSchoolFirstTime:
                case StoryPhase.EnterSchoolFinalBattle:
                    objectivePointer.SetTarget(schoolCityEntranceObj != null ? schoolCityEntranceObj.transform : null);
                    break;

                case StoryPhase.ExploreSchoolAndMeetOwl:
                    StoryInteractablePrototipo3 clue = FindInteractableByKind(StoryInteractableKind.SchoolPandemicClueLab);
                    objectivePointer.SetTarget(clue != null ? clue.transform : null);
                    break;

                case StoryPhase.FleeSchoolFirstTime:
                    StoryInteractablePrototipo3 hole = FindInteractableByKind(StoryInteractableKind.SchoolEscapeHoleToCity);
                    objectivePointer.SetTarget(hole != null ? hole.transform : null);
                    break;

                default:
                    objectivePointer.ClearTarget();
                    break;
            }
        }

        private static StoryInteractablePrototipo3 FindInteractableByKind(StoryInteractableKind k)
        {
            StoryInteractablePrototipo3[] all = FindObjectsByType<StoryInteractablePrototipo3>(FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].isActiveAndEnabled && all[i].kind == k) return all[i];
            }
            return null;
        }

        private static LootContainer FindNearestUnlootedContainer()
        {
            HamsterController hc = FindAnyObjectByType<HamsterControllerPrototipo3>() ?? FindAnyObjectByType<HamsterController>();
            Vector3 refPos = hc != null ? hc.transform.position : Vector3.zero;
            LootContainer[] all = FindObjectsByType<LootContainer>(FindObjectsSortMode.None);
            LootContainer best = null;
            float bestDist = float.MaxValue;
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] == null || all[i].YaFueSaqueado) continue;
                float d = (all[i].transform.position - refPos).sqrMagnitude;
                if (d < bestDist)
                {
                    bestDist = d;
                    best = all[i];
                }
            }
            return best;
        }

        private static bool CanCraftRemainingTrapsSoon()
        {
            InventoryManager inv = InventoryManager.Instance;
            if (inv == null) return false;
            bool needPuas = inv.GetTrapCount(TipoTrampa.Puas) == 0 && !TrapSystemPrototipo3.CanCraftTrap(TipoTrampa.Puas);
            bool needRed = inv.GetTrapCount(TipoTrampa.Red) == 0 && !TrapSystemPrototipo3.CanCraftTrap(TipoTrampa.Red);
            bool needMad = inv.GetTrapCount(TipoTrampa.Madera) == 0 && !TrapSystemPrototipo3.CanCraftTrap(TipoTrampa.Madera);
            return !(needPuas || needRed || needMad);
        }

        private static void EnsureBonusLootNearRefuge()
        {
            CityGeneratorPrototipo3 city = CityGeneratorPrototipo3.Instance;
            if (city == null || city.lootPrefab == null) return;
            Vector3 basePt = city.SafeRefugeEntrancePoint;
            Vector3[] offsets = { new Vector3(6f, 0f, 4f), new Vector3(-6f, 0f, 5f), new Vector3(4f, 0f, -7f) };
            for (int i = 0; i < offsets.Length; i++)
            {
                Vector3 spot = FindClearNavMeshSpotNear(basePt + offsets[i], 5f);
                GameObject lcGo = Instantiate(city.lootPrefab, spot, Quaternion.identity, city.transform);
                lcGo.tag = "Interactable";
                LootContainer lc = lcGo.GetComponentInChildren<LootContainer>();
                if (lc != null)
                {
                    lc.posiblesLoot = new List<LootDrop>
                    {
                        new LootDrop { item = ItemType.Metal, dropChance = 100f },
                        new LootDrop { item = ItemType.Desperdicios, dropChance = 100f },
                        new LootDrop { item = ItemType.Madera, dropChance = 80f }
                    };
                }
            }
        }

        private static void EnsureStarterTrapTutorialMaterials()
        {
            InventoryManager inv = InventoryManager.Instance;
            if (inv == null) return;
            // Garantizar que tras entregarle materiales a Marco, Kilo tenga al menos para fabricar 1 Trampa de Púas o Red
            if (!TrapSystemPrototipo3.CanCraftTrap(TipoTrampa.Puas) && !TrapSystemPrototipo3.CanCraftTrap(TipoTrampa.Red))
            {
                inv.AddItem(ItemType.Metal, 2);
                inv.AddItem(ItemType.Madera, 1);
                inv.AddItem(ItemType.Goma, 1);
                inv.AddItem(ItemType.Desperdicios, 1);
            }
        }

        private static void EnsureExtraCraftingLootAroundRefuge()
        {
            CityGeneratorPrototipo3 city = CityGeneratorPrototipo3.Instance;
            if (city == null || city.lootPrefab == null) return;
            Vector3 basePt = city.SafeRefugeEntrancePoint;
            Vector3[] offsets = { new Vector3(8f, 0f, 3f), new Vector3(-8f, 0f, 4f), new Vector3(3f, 0f, 9f), new Vector3(-5f, 0f, -8f) };
            for (int i = 0; i < offsets.Length; i++)
            {
                Vector3 spot = FindClearNavMeshSpotNear(basePt + offsets[i], 6f);
                GameObject lcGo = Instantiate(city.lootPrefab, spot, Quaternion.identity, city.transform);
                lcGo.tag = "Interactable";
                LootContainer lc = lcGo.GetComponentInChildren<LootContainer>();
                if (lc != null)
                {
                    lc.posiblesLoot = new List<LootDrop>
                    {
                        new LootDrop { item = ItemType.Madera, dropChance = 100f },
                        new LootDrop { item = ItemType.Metal, dropChance = 100f },
                        new LootDrop { item = ItemType.Goma, dropChance = 100f },
                        new LootDrop { item = ItemType.Desperdicios, dropChance = 100f }
                    };
                }
            }
        }

        private static void LoadSceneSafe(string sceneAssetPath, string shortName)
        {
            Time.timeScale = 1f;
            if (Application.CanStreamedLevelBeLoaded(sceneAssetPath))
            {
                SceneManager.LoadScene(sceneAssetPath);
            }
            else if (Application.CanStreamedLevelBeLoaded(shortName))
            {
                SceneManager.LoadScene(shortName);
            }
            else
            {
                Debug.LogError($"[StoryDirectorPrototipo3] La escena '{shortName}' ({sceneAssetPath}) no está registrada en EditorBuildSettings.");
            }
        }

        // ==========================================================================================
        //  PANTALLA DE FIN DE LA DEMO (VICTORIA EN LA CANCHA DE BÁSQUETBOL)
        // ==========================================================================================

        private void BuildVictoryModalUI()
        {
            GameObject cGo = new GameObject("DemoVictoryCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            cGo.transform.SetParent(transform, false);
            victoryCanvas = cGo.GetComponent<Canvas>();
            victoryCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            victoryCanvas.sortingOrder = 460;

            CanvasScaler sc = cGo.GetComponent<CanvasScaler>();
            sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            sc.referenceResolution = new Vector2(1920f, 1080f);

            victoryRoot = new GameObject("VictoryOverlay", typeof(RectTransform), typeof(Image));
            victoryRoot.transform.SetParent(cGo.transform, false);
            RectTransform rt = victoryRoot.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            victoryRoot.GetComponent<Image>().color = new Color(0.02f, 0.02f, 0.03f, 0.92f);

            GameObject card = new GameObject("Card", typeof(RectTransform), typeof(CanvasRenderer), typeof(AngularComicBoxGraphic));
            card.transform.SetParent(victoryRoot.transform, false);
            RectTransform cRt = card.GetComponent<RectTransform>();
            cRt.anchorMin = cRt.anchorMax = cRt.pivot = new Vector2(0.5f, 0.5f);
            cRt.sizeDelta = new Vector2(1020f, 590f);
            card.GetComponent<AngularComicBoxGraphic>().color = new Color(0.08f, 0.07f, 0.09f, 0.98f);

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            CreateLabel(card.transform, "Kicker", "FERAL  ·  CRÓNICAS DEL HUMO SOLAR", new Vector2(0f, 220f), new Vector2(880f, 36f), 20, new Color(0.95f, 0.75f, 0.32f, 1f), font);
            CreateLabel(card.transform, "Title", "¡FIN DE LA DEMO!", new Vector2(0f, 155f), new Vector2(900f, 70f), 54, new Color(0.98f, 0.95f, 0.88f, 1f), font);
            CreateLabel(card.transform, "Summary",
                "Lograste sobrevivir a la transformación feral de Marco, aliarte con Toby el Mapache,\n" +
                "infiltrarte en la High School Bellavista y capturar al Búho Rector en la Cancha de Básquet\n" +
                "con la Pistola Lanza Redes para asegurar la Enzima Base que salvará a Sofía.\n\n" +
                "¡Gracias por jugar la Demo de FERAL!",
                new Vector2(0f, 18f), new Vector2(880f, 190f), 24, new Color(0.88f, 0.84f, 0.76f, 1f), font);

            CreateActionBtn(card.transform, "BtnSandbox", "Explorar Ciudad Procedural (Modo Libre)", new Vector2(0f, -135f), new Color(0.82f, 0.22f, 0.14f, 1f), font, () =>
            {
                victoryModalOpen = false;
                victoryRoot.SetActive(false);
                Time.timeScale = 1f;
                ResetCampaignForNewGame(enableStory: false);
                CityGeneratorPrototipo3.SetPendingCityConfig(UnityEngine.Random.Range(100000, 999999), 30, 30);
                LoadSceneSafe(CityScenePath, CitySceneName);
            });

            CreateActionBtn(card.transform, "BtnMenu", "Volver al Menú Principal", new Vector2(0f, -215f), new Color(0.22f, 0.20f, 0.24f, 1f), font, () =>
            {
                victoryModalOpen = false;
                victoryRoot.SetActive(false);
                Time.timeScale = 1f;
                ResetCampaignForNewGame(enableStory: true);
                if (InventoryManager.Instance != null) InventoryManager.Instance.Clear();
                LoadSceneSafe("Assets/Prototipo3/Scenes/MainMenu_Prototipo3.unity", "MainMenu_Prototipo3");
            });

            victoryRoot.SetActive(false);
        }

        private static void CreateLabel(Transform parent, string name, string txt, Vector2 pos, Vector2 size, int fontSize, Color col, Font font)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            Text t = go.GetComponent<Text>();
            t.font = font;
            t.fontSize = fontSize;
            t.fontStyle = fontSize >= 36 ? FontStyle.Bold : FontStyle.Normal;
            t.color = col;
            t.alignment = TextAnchor.MiddleCenter;
            t.text = txt;
        }

        private static void CreateActionBtn(Transform parent, string name, string label, Vector2 pos, Color bgCol, Font font, Action onClick)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(620f, 62f);
            go.GetComponent<Image>().color = bgCol;
            Button b = go.GetComponent<Button>();
            b.onClick.AddListener(() => onClick?.Invoke());
            CreateLabel(go.transform, "Lbl", label, Vector2.zero, new Vector2(580f, 54f), 24, Color.white, font);
        }

        private void OpenDemoVictoryScreen()
        {
            victoryModalOpen = true;
            victoryRoot.SetActive(true);
            Time.timeScale = 0f;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }
}
