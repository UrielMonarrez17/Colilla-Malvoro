using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using TMPro;
using Prototipo3.Narrative;

namespace Prototipo3
{
    public enum CharacterRoleType
    {
        PureAlly,
        DualCitizenFeral,
        PacifiableBoss
    }

    public enum BossProfileType
    {
        None,
        Marco_Tejon,
        Morales_Buho,
        Sultan_Lobo,
        Clara_Cisne,
        Kross_Murcielago
    }

    /// <summary>
    /// Gestiona el estado dual Ciudadano de Día / Feral de Noche para NPCs, enemigos y Jefes de Feral (Prototipo 3).
    /// Conectado a TimeManager.Instance.OnCycleStateChanged.
    /// </summary>
    [SelectionBase]
    public class DualRoleCharacter : MonoBehaviour
    {
        public static readonly List<DualRoleCharacter> ActiveCharacters = new List<DualRoleCharacter>();

        [Header("Identidad y Rol")]
        public string characterId = "NPC";
        public string displayName = "Ciudadano";
        public string speciesTitle = "Habitante de Solis";
        public CharacterRoleType roleType = CharacterRoleType.DualCitizenFeral;
        public BossProfileType bossProfile = BossProfileType.None;

        [Header("Estado de Pacificación y Feral")]
        [Tooltip("En PacifiableBoss, cuando se completa su mecánica nocturna pasa a true permanentemente.")]
        public bool isPacified = false;

        [Tooltip("Permite forzar estado feral por evento de historia (ej. Apagón del Búho Morales o Sobrecarga de Kross).")]
        public bool forceFeralOverride = false;

        [Header("Restricción Narrativa Feral")]
        [Tooltip("Nodos de la historia requeridos para que este personaje pueda transformarse en Feral. Si está vacía, no hay restricción.")]
        public List<string> feralStoryNodeIDs = new List<string>();

        [SerializeField] private bool isCurrentlyFeral = false;
        public bool IsCurrentlyFeral => isCurrentlyFeral;

        [Header("Diálogos por Estado")]
        [TextArea(2, 4)]
        public string dayDialogue = "El humo de la corporación Aethelgard hace que nos duela la cabeza al caer la tarde... Ten cuidado cuando se oculte el sol.";
        [TextArea(2, 4)]
        public string postNightApologyDialogue = "Perdóname, Kilo... Anoche perdí el control otra vez. No recuerdo lo que hice en la oscuridad, ¡ayúdame antes de que vuelva a caer la noche!";
        [TextArea(2, 4)]
        public string pacifiedDialogue = "Gracias a ti mi mente está despejada. Cuenta conmigo para detener al Dr. Julián Kross.";

        [Header("Ojos Emisivos (Estado Feral)")]
        public Renderer[] eyeRenderers;
        public Color peacefulEyeColor = new Color(0.15f, 0.15f, 0.18f, 1f);
        public Color feralEyeColor = new Color(1.0f, 0.22f, 0.05f, 1f);
        public Light feralEyeLight;

        [Header("Patrulla Pacífica Diurna")]
        public bool strollDuringDay = true;
        public float dayStrollRadius = 5.5f;
        public float dayStrollSpeed = 2.2f;
        public float interactDistance = 3.8f;

        // Referencias internas
        private EnemyAIPrototipo3 enemyAI;
        private EnemyOnCollision enemyCollision;
        private NavMeshAgent agent;
        private Animator animator;
        private Transform playerTransform;
        private Vector3 homeAnchorPosition;
        private float dayStrollTimer = 0f;
        private bool experiencedAtLeastOneNight = false;
        private bool isSubscribedToTime = false;
        private Material instancedEyeMat;

        // Cartel flotante 3D
        private GameObject promptObj;
        private TextMeshPro promptLabel;

        public Vector3 HomeAnchorPosition => homeAnchorPosition;

        private void Awake()
        {
            if (!ActiveCharacters.Contains(this))
            {
                ActiveCharacters.Add(this);
            }

            // Desactivar scripts de control manual del paquete Animals_FREE
            MonoBehaviour[] scripts = GetComponents<MonoBehaviour>();
            for (int i = 0; i < scripts.Length; i++)
            {
                if (scripts[i] == null || scripts[i] == this) continue;
                string tName = scripts[i].GetType().Name;
                if (tName == "MovePlayerInput" || tName == "CreatureMover" || tName == "AnimationShowcase")
                {
                    scripts[i].enabled = false;
                }
            }

            CharacterController cc = GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            agent = GetComponent<NavMeshAgent>();
            enemyAI = GetComponent<EnemyAIPrototipo3>();
            enemyCollision = GetComponent<EnemyOnCollision>();
            animator = GetComponentInChildren<Animator>();

            homeAnchorPosition = transform.position;
        }

        private void Start()
        {
            if (homeAnchorPosition == Vector3.zero)
            {
                homeAnchorPosition = transform.position;
            }

            EnsureEyeVisuals();
            CreateFloatingPrompt();
            ApplyBossProfileConfiguration();
            SubscribeToTimeEvents();

            // Dr. Julián Kross solo se sobrecarga cuando su hito narrativo (node_d2_kross) está activo
            if (bossProfile == BossProfileType.Kross_Murcielago && !isPacified && IsFeralUnlockedByStory())
            {
                forceFeralOverride = true;
            }

            EvaluateCycleStateImmediate();
        }

        private void OnEnable()
        {
            if (!ActiveCharacters.Contains(this))
            {
                ActiveCharacters.Add(this);
            }
            SubscribeToTimeEvents();
        }

        private void OnDisable()
        {
            UnsubscribeFromTimeEvents();
        }

        private void OnDestroy()
        {
            ActiveCharacters.Remove(this);
            UnsubscribeFromTimeEvents();
            if (instancedEyeMat != null)
            {
                Destroy(instancedEyeMat);
            }
        }

        public void SetHomeAnchor(Vector3 pos)
        {
            homeAnchorPosition = pos;
            if (agent != null && agent.enabled && NavMesh.SamplePosition(pos, out NavMeshHit hit, 5f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }
        }

        private void SubscribeToTimeEvents()
        {
            if (!isSubscribedToTime && TimeManager.Instance != null)
            {
                TimeManager.Instance.OnDayStart += OnDayStart;
                TimeManager.Instance.OnNightStart += OnNightStart;
                isSubscribedToTime = true;
            }
        }

        private void UnsubscribeFromTimeEvents()
        {
            if (isSubscribedToTime && TimeManager.Instance != null)
            {
                TimeManager.Instance.OnDayStart -= OnDayStart;
                TimeManager.Instance.OnNightStart -= OnNightStart;
                isSubscribedToTime = false;
            }
        }

        public void EvaluateCycleStateImmediate()
        {
            bool isNight = TimeManager.Instance != null && TimeManager.Instance.CurrentCycleState == TimeManager.CycleState.Night;
            if (isNight)
            {
                OnNightStart();
            }
            else
            {
                OnDayStart();
            }
        }

        /// <summary>
        /// Comprueba si el estado feral de este personaje está habilitado por el grafo narrativo de la historia.
        /// Si feralStoryNodeIDs está vacío, no hay restricción. Si contiene IDs, al menos uno debe estar Active o Completed.
        /// <summary>
        /// Comprueba si el estado feral de este personaje está habilitado por el grafo narrativo de la historia.
        /// Si es Marco, requiere haber avanzado en la apertura (hablar con Marco de día y preparar herramientas/saquear).
        /// Si feralStoryNodeIDs contiene IDs, al menos uno debe estar Active o Completed.
        /// </summary>
        public bool IsFeralUnlockedByStory()
        {
            if (characterId != null && characterId.Equals("Marco", StringComparison.OrdinalIgnoreCase))
            {
                if (StoryManagerPrototipo3.Instance != null)
                {
                    return StoryManagerPrototipo3.Instance.hasSpokenToMarcoDay &&
                           (StoryManagerPrototipo3.Instance.hasCraftedInitialTool || StoryManagerPrototipo3.Instance.containersLootedCount >= 1);
                }
                return false;
            }

            if (feralStoryNodeIDs == null || feralStoryNodeIDs.Count == 0)
            {
                return true;
            }

            if (StoryManagerPrototipo3.Instance == null || StoryManagerPrototipo3.Instance.CurrentGraph == null)
            {
                return false;
            }

            StoryGraph graph = StoryManagerPrototipo3.Instance.CurrentGraph;
            for (int i = 0; i < feralStoryNodeIDs.Count; i++)
            {
                string nodeId = feralStoryNodeIDs[i];
                if (string.IsNullOrEmpty(nodeId)) continue;
                StoryNode node = graph.GetNode(nodeId);
                if (node != null && (node.state == StoryNodeState.Active || node.state == StoryNodeState.Completed))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Evento al Amanecer (OnDayStart): vuelve a su estado pacífico si no tiene override forzado y desbloqueado.
        /// </summary>
        public void OnDayStart()
        {
            if (roleType == CharacterRoleType.PureAlly || isPacified || !forceFeralOverride || !IsFeralUnlockedByStory())
            {
                SetFeralMode(false);
            }
            else
            {
                SetFeralMode(true);
            }
        }

        /// <summary>
        /// Evento al Anochecer (OnNightStart): si su historia está desbloqueada, enciende ojos emisivos y activa persecución;
        /// de lo contrario permanece pacífico/dócil.
        /// </summary>
        public void OnNightStart()
        {
            if (roleType == CharacterRoleType.PureAlly || isPacified || !IsFeralUnlockedByStory())
            {
                SetFeralMode(false);
                return;
            }

            experiencedAtLeastOneNight = true;

            // Cerrar cualquier diálogo abierto en la UI
            if (Prototipo3.UI.FeralGameUI.Instance != null && Prototipo3.UI.FeralGameUI.Instance.DialogueOpen)
            {
                Prototipo3.UI.FeralGameUI.Instance.Close();
            }

            SetFeralMode(true);

            // Notificar a StoryManager por si es la primera noche de Marco (Nodo 0)
            if (StoryManagerPrototipo3.Instance != null)
            {
                StoryManagerPrototipo3.Instance.NotifyCharacterTurnedFeral(this);
            }
        }

        public void ForceFeralState(bool feral)
        {
            if (roleType == CharacterRoleType.PureAlly || isPacified || (feral && !IsFeralUnlockedByStory()))
            {
                forceFeralOverride = false;
                SetFeralMode(false);
                return;
            }

            forceFeralOverride = feral;
            SetFeralMode(feral);
        }

        /// <summary>
        /// Pacifica permanentemente a un Jefe (PacifiableBoss) tras completar su mecánica nocturna.
        /// </summary>
        public void PacifyBoss()
        {
            var plotGate = GetComponent<PlotGate>();
            if (plotGate != null && !plotGate.IsGateOpen())
            {
                Debug.LogWarning($"[DualRoleCharacter] {displayName} no puede ser pacificado prematuramente (PlotGate cerrado para {plotGate.targetStoryNodeID}).");
                return;
            }

            isPacified = true;
            forceFeralOverride = false;
            SetFeralMode(false);
            if (QuestCompiler.Instance != null)
            {
                QuestCompiler.Instance.AdvanceActionIfMatches("inyectar_antidoto_vial", characterId, transform);
                QuestCompiler.Instance.AdvanceActionIfMatches("hablar_npc", "jefe_pacificado", transform);
            }
            Debug.Log($"[DualRoleCharacter] ¡{displayName} ({speciesTitle}) ha sido pacificado permanentemente!");
        }

        private void SetFeralMode(bool feral)
        {
            isCurrentlyFeral = feral;

            if (feral)
            {
                gameObject.tag = "Enemy";

                if (enemyAI == null) enemyAI = gameObject.AddComponent<EnemyAIPrototipo3>();
                ApplyBossProfileConfiguration();
                enemyAI.enabled = true;
                enemyAI.showVisionCone = true;

                if (enemyCollision == null) enemyCollision = gameObject.AddComponent<EnemyOnCollision>();
                enemyCollision.enabled = true;

                if (agent != null && agent.enabled && agent.isOnNavMesh)
                {
                    agent.isStopped = false;
                    agent.speed = enemyAI.wanderSpeed;
                }

                UpdateEyeVisuals(true);
            }
            else
            {
                gameObject.tag = "Interactable";

                if (enemyAI != null)
                {
                    MarcoBossController marcoCtrl = GetComponent<MarcoBossController>();
                    if (marcoCtrl != null) marcoCtrl.InterruptActiveAttack(false, 0f);

                    MoralesOwlBossController moralesCtrl = GetComponent<MoralesOwlBossController>();
                    if (moralesCtrl != null) moralesCtrl.InterruptActiveAttack();

                    enemyAI.HideVisionConeImmediate();
                    enemyAI.enabled = false;
                }

                if (enemyCollision != null)
                {
                    enemyCollision.enabled = false;
                }

                if (agent != null && agent.enabled && agent.isOnNavMesh)
                {
                    agent.speed = dayStrollSpeed;
                    agent.isStopped = !strollDuringDay;
                }

                UpdateEyeVisuals(false);
            }
        }

        public void ApplyBossProfileConfiguration()
        {
            if (enemyAI == null) enemyAI = GetComponent<EnemyAIPrototipo3>();
            if (enemyAI == null) return;

            enemyAI.bossProfile = bossProfile;

            switch (bossProfile)
            {
                case BossProfileType.Marco_Tejon:
                    // Velocidad media, cono de visión estándar; patrulla cerca de la Base
                    enemyAI.wanderSpeed = 3.8f;
                    enemyAI.chaseSpeed = 7.2f;
                    enemyAI.visionAngle = 95f;
                    enemyAI.visionRadiusNormal = 14f;
                    enemyAI.visionRadiusCrouched = 5.5f;
                    enemyAI.backDetectionRadius = 2.5f;
                    enemyAI.wanderRadius = 22f;
                    MarcoBossController marcoCtrl = GetComponent<MarcoBossController>();
                    if (marcoCtrl == null) marcoCtrl = gameObject.AddComponent<MarcoBossController>();
                    marcoCtrl.Initialize(enemyAI, this);
                    break;

                case BossProfileType.Morales_Buho:
                    // Amplio ángulo de visión (casi 300 grados) y alta sensibilidad a carreras ruidosas, muy vulnerable al Señuelo Acústico
                    enemyAI.wanderSpeed = 4.2f;
                    enemyAI.chaseSpeed = 8.4f;
                    enemyAI.visionAngle = 300f;
                    enemyAI.visionRadiusNormal = 17f;
                    enemyAI.visionRadiusCrouched = 6.0f;
                    enemyAI.backDetectionRadius = 3.5f;
                    enemyAI.sensitiveToNoisySprint = true;
                    enemyAI.noisySprintDetectionRadius = 25f;
                    enemyAI.acousticDecoyVulnerabilityMultiplier = 1.75f;
                    MoralesOwlBossController moralesCtrl = GetComponent<MoralesOwlBossController>();
                    if (moralesCtrl == null) moralesCtrl = gameObject.AddComponent<MoralesOwlBossController>();
                    moralesCtrl.Initialize(enemyAI, this);
                    break;

                case BossProfileType.Sultan_Lobo:
                    // Radio de detección por proximidad extrema (olfato) multiplicado x2.5 (reducido al mínimo con Bomba de Polen)
                    enemyAI.wanderSpeed = 4.5f;
                    enemyAI.chaseSpeed = 8.8f;
                    enemyAI.visionAngle = 110f;
                    enemyAI.visionRadiusNormal = 15f;
                    enemyAI.visionRadiusCrouched = 5.5f;
                    enemyAI.baseSmellRadius = 5.0f;
                    enemyAI.backDetectionRadius = 5.0f * 2.5f; // 12.5m olfato
                    enemyAI.smellMultiplier = 2.5f;
                    break;

                case BossProfileType.Clara_Cisne:
                    // Deja un rastro temporal de nubes de gas anestésico que ralentizan a Kilo
                    enemyAI.wanderSpeed = 4.0f;
                    enemyAI.chaseSpeed = 7.8f;
                    enemyAI.visionAngle = 120f;
                    enemyAI.visionRadiusNormal = 15f;
                    enemyAI.visionRadiusCrouched = 5.0f;
                    enemyAI.backDetectionRadius = 3.0f;
                    enemyAI.leavesAnestheticGasTrail = true;
                    enemyAI.gasSpawnInterval = 1.6f;
                    break;

                case BossProfileType.Kross_Murcielago:
                    // Sobrecargado continuamente por los 3 Condensadores Fotónicos UV
                    enemyAI.wanderSpeed = 4.8f;
                    enemyAI.chaseSpeed = 9.0f;
                    enemyAI.visionAngle = 150f;
                    enemyAI.visionRadiusNormal = 18f;
                    enemyAI.visionRadiusCrouched = 6.5f;
                    enemyAI.backDetectionRadius = 4.0f;
                    break;
            }
        }

        private void EnsureEyeVisuals()
        {
            Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Sprites/Default");
            instancedEyeMat = new Material(litShader);
            instancedEyeMat.name = $"EyeMat_{characterId}";

            if (eyeRenderers == null || eyeRenderers.Length == 0)
            {
                // Crear un par de ojos esféricos sobre la cabeza para reflejar claramente el cambio emisivo ámbar/rojo
                GameObject eyesRoot = new GameObject("DualRole_Eyes");
                eyesRoot.transform.SetParent(transform, false);
                eyesRoot.transform.localPosition = new Vector3(0f, 0.9f, 0.35f);

                GameObject leftEye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                leftEye.name = "LeftEye";
                leftEye.transform.SetParent(eyesRoot.transform, false);
                leftEye.transform.localPosition = new Vector3(-0.12f, 0f, 0f);
                leftEye.transform.localScale = Vector3.one * 0.11f;
                Collider c1 = leftEye.GetComponent<Collider>();
                if (c1 != null) Destroy(c1);

                GameObject rightEye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                rightEye.name = "RightEye";
                rightEye.transform.SetParent(eyesRoot.transform, false);
                rightEye.transform.localPosition = new Vector3(0.12f, 0f, 0f);
                rightEye.transform.localScale = Vector3.one * 0.11f;
                Collider c2 = rightEye.GetComponent<Collider>();
                if (c2 != null) Destroy(c2);

                eyeRenderers = new Renderer[] { leftEye.GetComponent<Renderer>(), rightEye.GetComponent<Renderer>() };

                GameObject lightObj = new GameObject("FeralEyeGlowLight");
                lightObj.transform.SetParent(eyesRoot.transform, false);
                lightObj.transform.localPosition = new Vector3(0f, 0f, 0.1f);
                feralEyeLight = lightObj.AddComponent<Light>();
                feralEyeLight.type = LightType.Point;
                feralEyeLight.color = feralEyeColor;
                feralEyeLight.range = 3.5f;
                feralEyeLight.intensity = 2.8f;
                feralEyeLight.enabled = false;
            }
        }

        private void UpdateEyeVisuals(bool feral)
        {
            if (instancedEyeMat != null)
            {
                Color targetCol = feral ? feralEyeColor : peacefulEyeColor;
                instancedEyeMat.color = targetCol;
                if (instancedEyeMat.HasProperty("_BaseColor"))
                {
                    instancedEyeMat.SetColor("_BaseColor", targetCol);
                }
                if (instancedEyeMat.HasProperty("_EmissionColor"))
                {
                    if (feral)
                    {
                        instancedEyeMat.EnableKeyword("_EMISSION");
                        instancedEyeMat.SetColor("_EmissionColor", feralEyeColor * 3.5f);
                    }
                    else
                    {
                        instancedEyeMat.SetColor("_EmissionColor", Color.black);
                    }
                }
            }

            if (eyeRenderers != null)
            {
                for (int i = 0; i < eyeRenderers.Length; i++)
                {
                    if (eyeRenderers[i] != null && instancedEyeMat != null)
                    {
                        eyeRenderers[i].sharedMaterial = instancedEyeMat;
                    }
                }
            }

            if (feralEyeLight != null)
            {
                feralEyeLight.enabled = feral;
                feralEyeLight.color = feralEyeColor;
            }
        }

        private void CreateFloatingPrompt()
        {
            if (promptObj != null) return;

            promptObj = new GameObject("CharacterPrompt_3D");
            promptObj.transform.SetParent(transform, false);
            float invScale = 1f / Mathf.Max(0.1f, transform.localScale.y);
            promptObj.transform.localScale = Vector3.one * invScale;
            promptObj.transform.localPosition = new Vector3(0f, 1.9f * invScale, 0f);

            promptLabel = promptObj.AddComponent<TextMeshPro>();
            promptLabel.fontSize = 3.0f;
            promptLabel.alignment = TextAlignmentOptions.Center;
            promptLabel.color = Color.white;
            promptLabel.outlineWidth = 0.2f;
            promptLabel.outlineColor = Color.black;
        }

        private float bossWarningCooldownTimer = 0f;

        public bool IsBossCharacter()
        {
            return bossProfile != BossProfileType.None || roleType == CharacterRoleType.PacifiableBoss;
        }

        private void CheckBossTerritoryAndFeedback(float distToPlayer)
        {
            if (!IsBossCharacter() || isPacified) return;

            if (bossWarningCooldownTimer > 0f)
            {
                bossWarningCooldownTimer -= Time.deltaTime;
            }

            bool isNight = TimeManager.Instance != null && TimeManager.Instance.CurrentCycleState == TimeManager.CycleState.Night;
            bool storyUnlocked = IsFeralUnlockedByStory();

            // Si el jugador entra al territorio del jefe (< 16 metros)
            if (distToPlayer <= 16f)
            {
                // Si la historia aún no desbloquea el enfrentamiento a este jefe, alertar al jugador
                if (!storyUnlocked && bossWarningCooldownTimer <= 0f)
                {
                    TriggerBossPreparationWarning();
                }

                // Si es de noche y el jugador se acerca peligrosamente (< 9 metros),
                // el jefe se vuelve feral advirtiendo al jugador con feedback inmediato
                if (isNight && !isCurrentlyFeral && distToPlayer <= 9f)
                {
                    TriggerBossPreparationWarning();
                    SetFeralMode(true);
                }
            }
        }

        public void TriggerBossPreparationWarning()
        {
            bossWarningCooldownTimer = 12f;
            string warning = GetBossWarningFeedback();

            if (Prototipo3.UI.FeralGameUI.Instance != null)
            {
                Prototipo3.UI.FeralGameUI.Instance.ShowLootFeedback(warning, 5.5f);
            }

            if (StoryManagerPrototipo3.Instance != null)
            {
                StoryManagerPrototipo3.Instance.EmitRadioTransmission("Alerta Táctica", warning);
            }
        }

        public string GetBossWarningFeedback()
        {
            switch (bossProfile)
            {
                case BossProfileType.Marco_Tejon:
                    return "¡ADVERTENCIA! Marco muestra síntomas de fiebre por el gas. Habla con él de día [F] y fabrica una herramienta antes de salir de noche.";
                case BossProfileType.Morales_Buho:
                    return "¡ADVERTENCIA DE JEFE! El Rector Morales tiene oído ultrasensible y es invencible sin el Señuelo Acústico [Tecla 1]. ¡Habla primero con Toby en su taller de robótica (Edificio Especial)!";
                case BossProfileType.Sultan_Lobo:
                    return "¡ADVERTENCIA DE JEFE! Sultán 'Colmillo de Brasa' es letal en la noche. ¡Necesitas fabricar la Bomba de Polen [Tecla 2] y reparar los 3 postes reflectores!";
                case BossProfileType.Clara_Cisne:
                    return "¡ADVERTENCIA DE JEFE! Clara Valdés emana gas anestésico. ¡Necesitas el Descargador Estático [Tecla 3] y activar la cámara hiperbárica!";
                case BossProfileType.Kross_Murcielago:
                    return "¡ADVERTENCIA DE JEFE! El Dr. Julián Kross está protegido por 3 condensadores UV. ¡Sobrecárgalos antes de intentar pacificarlo!";
                default:
                    return $"¡PELIGRO! {displayName} es invencible sin la preparación adecuada. ¡Retírate y consigue los recursos necesarios!";
            }
        }

        private void Update()
        {
            SubscribeToTimeEvents();

            // Si es el Dr. Kross y se activa su hito narrativo, activar su sobrecarga feral permanente
            if (bossProfile == BossProfileType.Kross_Murcielago && !isPacified && !forceFeralOverride && IsFeralUnlockedByStory())
            {
                ForceFeralState(true);
            }

            // Si es de noche pero el personaje permanecía pacífico y acaba de desbloquearse su hito feral
            if (TimeManager.Instance != null && TimeManager.Instance.CurrentCycleState == TimeManager.CycleState.Night)
            {
                if (!isCurrentlyFeral && !isPacified && roleType != CharacterRoleType.PureAlly && IsFeralUnlockedByStory())
                {
                    OnNightStart();
                }
            }

            if (playerTransform == null || !playerTransform.gameObject.activeInHierarchy)
            {
                HamsterController hc = FindAnyObjectByType<HamsterController>();
                if (hc != null) playerTransform = hc.transform;
            }

            float distToPlayer = playerTransform != null
                ? Vector3.Distance(transform.position, playerTransform.position)
                : float.MaxValue;

            UpdateFloatingPrompt(distToPlayer);
            CheckBossTerritoryAndFeedback(distToPlayer);

            if (!isCurrentlyFeral)
            {
                UpdatePeacefulDayBehavior(distToPlayer);
            }
        }

        private void UpdatePeacefulDayBehavior(float distToPlayer)
        {
            if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;

            if (distToPlayer <= interactDistance * 1.3f && playerTransform != null)
            {
                agent.isStopped = true;
                Vector3 lookDir = playerTransform.position - transform.position;
                lookDir.y = 0f;
                if (lookDir.sqrMagnitude > 0.01f)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir.normalized), Time.deltaTime * 6f);
                }
                return;
            }

            if (!strollDuringDay)
            {
                agent.isStopped = true;
                return;
            }

            dayStrollTimer -= Time.deltaTime;
            if (dayStrollTimer <= 0f || (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.3f))
            {
                dayStrollTimer = UnityEngine.Random.Range(4f, 8f);
                Vector2 rnd = UnityEngine.Random.insideUnitCircle * dayStrollRadius;
                Vector3 candidate = homeAnchorPosition + new Vector3(rnd.x, 0f, rnd.y);
                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 4f, NavMesh.AllAreas))
                {
                    agent.isStopped = false;
                    agent.speed = dayStrollSpeed;
                    agent.SetDestination(hit.position);
                }
            }
        }

        private void UpdateFloatingPrompt(float distToPlayer)
        {
            if (promptObj == null || promptLabel == null) return;

            bool visible = distToPlayer <= 16f;
            if (promptObj.activeSelf != visible)
            {
                promptObj.SetActive(visible);
            }

            if (!visible) return;

            if (Camera.main != null)
            {
                promptObj.transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward, Camera.main.transform.up);
            }

            if (isCurrentlyFeral)
            {
                string feralTag = IsBossCharacter() ? "<color=#FF2200><b>" + displayName + " (JEFE FERAL)</b></color>" : $"<color=#FF3322><b>{displayName} (FERAL)</b></color>";
                string extraWarning = IsBossCharacter() ? "\n<size=65%><color=#FFCC00>¡Territorio Hostil!</color></size>" : "";
                promptLabel.text = $"{feralTag}\n<size=70%>{speciesTitle}</size>{extraWarning}";
            }
            else
            {
                string tagColor = (roleType == CharacterRoleType.PureAlly || isPacified) ? "#44FF88" : (IsBossCharacter() ? "#FFAA00" : "#00E5FF");
                string bossTag = IsBossCharacter() ? " (Jefe Territorial)" : "";
                string actionHint = distToPlayer <= interactDistance ? "\n<color=#FFD700>[F] Hablar</color>" : "";
                promptLabel.text = $"<color={tagColor}><b>{displayName}{bossTag}</b></color>\n<size=72%>{speciesTitle}</size>{actionHint}";
            }
        }

        /// <summary>
        /// Llamado cuando el jugador presiona [F] cerca del personaje en estado pacífico / aliado.
        /// </summary>
        public void InteractWithCharacter()
        {
            if (isCurrentlyFeral) return;
            if (Prototipo3.UI.FeralGameUI.BlocksGameplay) return;

            var plotGate = GetComponent<PlotGate>();
            if (plotGate != null && !plotGate.IsGateOpen())
            {
                if (Prototipo3.UI.FeralGameUI.Instance != null)
                {
                    Prototipo3.UI.FeralGameUI.Instance.ShowDialogue($"{displayName} ({speciesTitle})", plotGate.mundaneDayDialogue);
                }
                return;
            }

            if (QuestCompiler.Instance != null)
            {
                QuestCompiler.Instance.AdvanceActionIfMatches("hablar_npc", characterId, transform);
                QuestCompiler.Instance.AdvanceActionIfMatches("entregar_recurso", characterId, transform);
            }

            if (StoryManagerPrototipo3.Instance != null)
            {
                StoryManagerPrototipo3.Instance.HandleCharacterInteraction(this);
                return;
            }

            string textToUse = GetContextualDialogue();
            if (Prototipo3.UI.FeralGameUI.Instance != null)
            {
                Prototipo3.UI.FeralGameUI.Instance.ShowDialogue($"{displayName} ({speciesTitle})", textToUse);
            }
        }

        public string GetContextualDialogue()
        {
            var plotGate = GetComponent<PlotGate>();
            if (plotGate != null && !plotGate.IsGateOpen())
            {
                return plotGate.mundaneDayDialogue;
            }

            if (isPacified && !string.IsNullOrEmpty(pacifiedDialogue))
            {
                return pacifiedDialogue;
            }
            if (experiencedAtLeastOneNight && roleType != CharacterRoleType.PureAlly && !string.IsNullOrEmpty(postNightApologyDialogue))
            {
                return postNightApologyDialogue;
            }
            return dayDialogue;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!isCurrentlyFeral || isPacified || roleType == CharacterRoleType.PureAlly) return;
            if (enemyAI != null && (enemyAI.IsParalyzedByStatic || enemyAI.IsDistractedByDecoy || enemyAI.IsNetImmobilized)) return;

            // EnemyOnCollision ya aplica daño cuando TimeManager está en Night;
            // aquí cubrimos a los jefes en estado Feral forzado durante el día (ej. Morales en el apagón o Kross sobrecargado).
            if (TimeManager.Instance != null && TimeManager.Instance.CurrentCycleState == TimeManager.CycleState.Day)
            {
                if (collision.gameObject.CompareTag("Player"))
                {
                    HamsterController hamster = collision.gameObject.GetComponent<HamsterController>();
                    if (hamster != null)
                    {
                        hamster.ReceiveDamage();
                    }
                }
            }
        }
    }
}
