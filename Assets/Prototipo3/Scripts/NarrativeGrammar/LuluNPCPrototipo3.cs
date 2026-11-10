using System;
using UnityEngine;
using UnityEngine.AI;
using TMPro;
using Prototipo3.Narrative;

namespace Prototipo3
{
    [SelectionBase]
    public class LuluNPCPrototipo3 : MonoBehaviour
    {
        public enum LuluBehaviorState
        {
            Idle,
            Stroll,
            GreetPlayer,
            GuideOrHide
        }

        [Header("Estado de Lulú NPC")]
        [SerializeField] private LuluBehaviorState currentState = LuluBehaviorState.Idle;
        public LuluBehaviorState CurrentState => currentState;

        [Header("Configuración de Movimiento y Paseo")]
        public float strollRadius = 6.5f;
        public float strollSpeed = 2.0f;
        public float guideSpeed = 3.2f;
        public float idleWaitMin = 2.5f;
        public float idleWaitMax = 5.0f;
        public float greetDistance = 6.0f;
        public float interactDistance = 3.8f;

        [Header("Referencias")]
        public Transform playerTransform;
        private NavMeshAgent agent;
        private Animator animator;
        private Vector3 homeAnchorPos;
        private Vector3 currentDestination;
        private float stateTimer = 0f;
        private bool hasMetPlayerInEncounter = false;
        private float lastInteractTime = -10f;

        private bool hasSpeedParam = false;
        private bool hasVertParam = false;
        private bool hasStateParam = false;

        private GameObject promptRootObj;
        private TextMeshPro promptTextMesh;

        private void Awake()
        {
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

            CharacterController cc = GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            if (!gameObject.CompareTag("Interactable"))
            {
                gameObject.tag = "Interactable";
            }
        }

        private void Start()
        {
            agent = GetComponent<NavMeshAgent>();
            animator = GetComponentInChildren<Animator>();
            CacheAnimatorParameters();

            homeAnchorPos = transform.position;

            if (agent != null)
            {
                agent.speed = strollSpeed;
                agent.angularSpeed = 260f;
                agent.acceleration = 8f;
                agent.stoppingDistance = 0.35f;
                agent.baseOffset = 0f;
                agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            }

            SnapToCitySurface();
            CrearIndicadorFlotante();
            TransitionToState(LuluBehaviorState.Idle);
        }

        private void CacheAnimatorParameters()
        {
            if (animator == null || animator.runtimeAnimatorController == null) return;
            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                if (param.name == "Speed") hasSpeedParam = true;
                else if (param.name == "Vert") hasVertParam = true;
                else if (param.name == "State") hasStateParam = true;
            }
        }

        private void CrearIndicadorFlotante()
        {
            if (promptRootObj != null) return;

            promptRootObj = new GameObject("LuluFloatingPrompt");
            promptRootObj.transform.SetParent(transform, false);
            float invScale = 1f / Mathf.Max(0.1f, transform.localScale.y);
            promptRootObj.transform.localScale = Vector3.one * invScale;
            promptRootObj.transform.localPosition = new Vector3(0f, 1.85f * invScale, 0f);

            promptTextMesh = promptRootObj.AddComponent<TextMeshPro>();
            promptTextMesh.text = "<color=#00FFFF><b>Lulú</b></color>\n<size=75%>[F] Hablar</size>";
            promptTextMesh.fontSize = 3.2f;
            promptTextMesh.alignment = TextAlignmentOptions.Center;
            promptTextMesh.color = Color.white;
            promptTextMesh.outlineWidth = 0.2f;
            promptTextMesh.outlineColor = Color.black;
        }

        public void SetHomeAnchor(Vector3 anchorPos)
        {
            homeAnchorPos = anchorPos;
            SnapToCitySurface();
        }

        public void SnapToCitySurface()
        {
            float minY = CityGeneratorPrototipo3.Instance != null ? CityGeneratorPrototipo3.Instance.roadHeightY : 0f;
            Vector3 pos = transform.position;
            pos.y = Mathf.Max(pos.y, minY);

            if (NavMesh.SamplePosition(pos, out NavMeshHit hit, 5.0f, NavMesh.AllAreas))
            {
                pos = hit.position;
            }
            else
            {
                Vector3 rayOrigin = new Vector3(pos.x, minY + 5f, pos.z);
                RaycastHit[] hits = Physics.RaycastAll(rayOrigin, Vector3.down, 10f, ~0, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < hits.Length; i++)
                {
                    if (hits[i].collider == null) continue;
                    if (hits[i].collider.transform == transform || hits[i].collider.transform.IsChildOf(transform)) continue;
                    if (hits[i].point.y >= minY - 0.5f && hits[i].point.y <= minY + 1.0f)
                    {
                        pos.y = hits[i].point.y;
                        break;
                    }
                }
            }

            transform.position = pos;
            if (agent != null && agent.enabled)
            {
                agent.Warp(pos);
            }
        }

        private void Update()
        {
            EnsurePlayerReference();
            EnforceSurfaceHeight();

            float distToPlayer = playerTransform != null
                ? Vector3.Distance(transform.position, playerTransform.position)
                : float.MaxValue;

            ActualizarIndicadorFlotante(distToPlayer);

            if (distToPlayer <= greetDistance && currentState != LuluBehaviorState.GreetPlayer)
            {
                TransitionToState(LuluBehaviorState.GreetPlayer);
            }
            else if (distToPlayer > greetDistance * 1.25f && currentState == LuluBehaviorState.GreetPlayer)
            {
                TransitionToState(hasMetPlayerInEncounter ? LuluBehaviorState.GuideOrHide : LuluBehaviorState.Stroll);
            }

            switch (currentState)
            {
                case LuluBehaviorState.Idle:
                    UpdateIdle();
                    break;
                case LuluBehaviorState.Stroll:
                    UpdateStroll();
                    break;
                case LuluBehaviorState.GreetPlayer:
                    UpdateGreetPlayer(distToPlayer);
                    break;
                case LuluBehaviorState.GuideOrHide:
                    UpdateGuideOrHide();
                    break;
            }

            UpdateAnimatorParameters();
        }

        private void TransitionToState(LuluBehaviorState newState)
        {
            currentState = newState;
            switch (currentState)
            {
                case LuluBehaviorState.Idle:
                    stateTimer = UnityEngine.Random.Range(idleWaitMin, idleWaitMax);
                    if (agent != null && agent.enabled && agent.isOnNavMesh)
                    {
                        agent.isStopped = true;
                    }
                    break;

                case LuluBehaviorState.Stroll:
                    if (agent != null && agent.enabled && agent.isOnNavMesh)
                    {
                        agent.speed = strollSpeed;
                        agent.isStopped = false;
                        if (TryPickStrollPoint(homeAnchorPos, strollRadius, out Vector3 target))
                        {
                            currentDestination = target;
                            agent.SetDestination(currentDestination);
                        }
                        stateTimer = 8f;
                    }
                    break;

                case LuluBehaviorState.GreetPlayer:
                    if (agent != null && agent.enabled && agent.isOnNavMesh)
                    {
                        agent.isStopped = true;
                    }
                    break;

                case LuluBehaviorState.GuideOrHide:
                    if (agent != null && agent.enabled && agent.isOnNavMesh)
                    {
                        bool isNight = TimeManager.Instance != null && TimeManager.Instance.CurrentCycleState == TimeManager.CycleState.Night;
                        agent.speed = isNight ? guideSpeed : strollSpeed;
                        agent.isStopped = false;
                        Vector3 anchor = homeAnchorPos;
                        if (CityGeneratorPrototipo3.Instance != null && hasMetPlayerInEncounter)
                        {
                            anchor = Vector3.Lerp(homeAnchorPos, CityGeneratorPrototipo3.Instance.CraftTableSpawnPoint, 0.35f);
                        }
                        if (TryPickStrollPoint(anchor, strollRadius * 1.2f, out Vector3 guideTarget))
                        {
                            currentDestination = guideTarget;
                            agent.SetDestination(currentDestination);
                        }
                        stateTimer = 7f;
                    }
                    break;
            }
        }

        private void UpdateIdle()
        {
            stateTimer -= Time.deltaTime;
            if (stateTimer <= 0f)
            {
                TransitionToState(hasMetPlayerInEncounter ? LuluBehaviorState.GuideOrHide : LuluBehaviorState.Stroll);
            }
        }

        private void UpdateStroll()
        {
            if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;

            stateTimer -= Time.deltaTime;
            if ((!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.25f) || stateTimer <= 0f)
            {
                TransitionToState(LuluBehaviorState.Idle);
            }
        }

        private void UpdateGreetPlayer(float distToPlayer)
        {
            if (playerTransform != null)
            {
                Vector3 lookDir = playerTransform.position - transform.position;
                lookDir.y = 0f;
                if (lookDir.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(lookDir.normalized);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 7f);
                }
            }

            if (distToPlayer <= interactDistance && (FeralInput.Pressed(KeyCode.F) || Input.GetKeyDown(KeyCode.F)))
            {
                InteractuarConLulu();
            }
        }

        private void UpdateGuideOrHide()
        {
            if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;

            stateTimer -= Time.deltaTime;
            if ((!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.3f) || stateTimer <= 0f)
            {
                TransitionToState(LuluBehaviorState.Idle);
            }
        }

        public void InteractuarConLulu()
        {
            if (Prototipo3.UI.FeralGameUI.BlocksGameplay) return;
            if (Time.time - lastInteractTime < 0.4f) return;
            lastInteractTime = Time.time;

            StoryManagerPrototipo3 sm = StoryManagerPrototipo3.Instance ?? FindAnyObjectByType<StoryManagerPrototipo3>();
            if (sm != null)
            {
                hasMetPlayerInEncounter = true;
                sm.InteractuarConLulu(this);
            }
            else
            {
                NarrativeHUDPrototipo3 hud = FindAnyObjectByType<NarrativeHUDPrototipo3>();
                if (hud != null)
                {
                    hud.MostrarDialogo("Lulú", "¡Psst! Hámster, qué alivio verte cuerdo. Mantente en sigilo [C] cuando caiga la noche y recolecta chatarra en los callejones.");
                }
            }
        }

        private void UpdateAnimatorParameters()
        {
            if (animator == null) return;

            float speed = 0f;
            if (agent != null && agent.enabled && agent.isOnNavMesh && !agent.isStopped)
            {
                speed = agent.velocity.magnitude;
            }

            float normalizedVert = speed > 0.12f ? Mathf.Clamp01(speed / strollSpeed) : 0f;
            float stateVal = speed > strollSpeed * 1.15f ? 1f : 0f;

            if (hasSpeedParam)
            {
                animator.SetFloat("Speed", normalizedVert, 0.1f, Time.deltaTime);
            }
            if (hasVertParam)
            {
                animator.SetFloat("Vert", normalizedVert, 0.1f, Time.deltaTime);
            }
            if (hasStateParam)
            {
                animator.SetFloat("State", stateVal, 0.1f, Time.deltaTime);
            }
        }

        private void ActualizarIndicadorFlotante(float distToPlayer)
        {
            if (promptRootObj == null) return;

            bool show = distToPlayer <= greetDistance * 1.8f;
            if (promptRootObj.activeSelf != show)
            {
                promptRootObj.SetActive(show);
            }

            if (show && Camera.main != null)
            {
                Vector3 camForward = Camera.main.transform.forward;
                promptRootObj.transform.rotation = Quaternion.LookRotation(camForward, Camera.main.transform.up);

                if (promptTextMesh != null)
                {
                    if (distToPlayer <= interactDistance)
                    {
                        promptTextMesh.text = "<color=#00FFFF><b>Lulú</b></color>\n<color=#FFD700>[Pulsa F para hablar]</color>";
                    }
                    else
                    {
                        promptTextMesh.text = "<color=#00FFFF><b>Lulú</b></color>";
                    }
                }
            }
        }

        private bool TryPickStrollPoint(Vector3 center, float radius, out Vector3 result)
        {
            float minY = CityGeneratorPrototipo3.Instance != null ? CityGeneratorPrototipo3.Instance.roadHeightY : 0f;
            for (int i = 0; i < 16; i++)
            {
                Vector2 rnd = UnityEngine.Random.insideUnitCircle * radius;
                Vector3 candidate = new Vector3(center.x + rnd.x, minY, center.z + rnd.y);
                if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 4.0f, NavMesh.AllAreas) && hit.position.y >= minY - 0.2f)
                {
                    result = hit.position;
                    return true;
                }
            }

            result = transform.position;
            return false;
        }

        private void EnforceSurfaceHeight()
        {
            float minY = CityGeneratorPrototipo3.Instance != null ? CityGeneratorPrototipo3.Instance.roadHeightY : 0f;
            if (transform.position.y < minY - 0.15f)
            {
                SnapToCitySurface();
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
    }
}
