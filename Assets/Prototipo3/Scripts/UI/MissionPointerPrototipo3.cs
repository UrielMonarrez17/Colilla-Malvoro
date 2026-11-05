using System.Collections.Generic;
using UnityEngine;
using Prototipo3.Narrative;

namespace Prototipo3
{
    [SelectionBase]
    public class MissionPointerPrototipo3 : PunteroMision
    {
        [Header("Referencias")]
        [Tooltip("Transform del hámster al que sigue la flecha.")]
        public Transform playerTransform;

        [Header("Ajustes de Posición y Animación")]
        [Tooltip("Altura de flotación sobre el hámster.")]
        public float hoverHeight = 0.85f;

        [Tooltip("Velocidad del balanceo vertical (bobbing).")]
        public float bobSpeed = 3.5f;

        [Tooltip("Amplitud del balanceo vertical.")]
        public float bobAmplitude = 0.08f;

        [Header("Material y Aspecto Visual")]
        public Material arrowMaterial;
        public Color arrowColor = new Color(1f, 0.92f, 0.016f, 1f);

        private Vector3 currentTargetWorldPos;
        private bool hasValidTarget = false;
        private float initialYOffset;
        private GameObject visualRoot;

        public bool HasValidTarget => hasValidTarget;
        public Vector3 CurrentTargetPos => currentTargetWorldPos;

        private void Awake()
        {
            initialYOffset = hoverHeight;
        }

        protected override void Start()
        {
            base.Start();
            flotar = false;
            velocidadRotacion = 12f;

            EnsurePlayerReference();
            ConstruirVisualSiNoExiste();

            StoryManagerPrototipo3 sm = FindAnyObjectByType<StoryManagerPrototipo3>();
            if (sm != null)
            {
                sm.OnActiveNodeChanged += ActualizarObjetivoPorNodo;
                sm.OnDemoCompleted += OcultarFlecha;

                if (sm.CurrentActiveNode != null)
                {
                    ActualizarObjetivoPorNodo(sm.CurrentActiveNode);
                }
            }
        }

        private void OnEnable()
        {
            LootContainer.OnContainerLooted += HandleContainerLooted;
        }

        private void OnDisable()
        {
            LootContainer.OnContainerLooted -= HandleContainerLooted;

            StoryManagerPrototipo3 sm = FindAnyObjectByType<StoryManagerPrototipo3>();
            if (sm != null)
            {
                sm.OnActiveNodeChanged -= ActualizarObjetivoPorNodo;
                sm.OnDemoCompleted -= OcultarFlecha;
            }
        }

        private void EnsurePlayerReference()
        {
            if (playerTransform == null)
            {
                HamsterControllerPrototipo3 hc3 = FindAnyObjectByType<HamsterControllerPrototipo3>();
                if (hc3 != null) { playerTransform = hc3.transform; return; }
                HamsterController hc = FindAnyObjectByType<HamsterController>();
                if (hc != null) playerTransform = hc.transform;
            }
        }

        private void LateUpdate()
        {
            EnsurePlayerReference();
            if (playerTransform == null) return;

            float bob = Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
            Vector3 targetPos = playerTransform.position + Vector3.up * (hoverHeight + bob);
            transform.position = targetPos;

            ResolverPosicionObjetivo();

            if (hasValidTarget)
            {
                Vector3 direccion = currentTargetWorldPos - transform.position;
                direccion.y = 0;

                if (direccion.sqrMagnitude > 0.08f)
                {
                    Quaternion rotacionDeseada = Quaternion.LookRotation(direccion.normalized, Vector3.up);
                    transform.rotation = Quaternion.Slerp(transform.rotation, rotacionDeseada, Time.deltaTime * velocidadRotacion);
                }

                if (visualRoot != null && !visualRoot.activeSelf)
                {
                    visualRoot.SetActive(true);
                }
            }
        }

        private void ResolverPosicionObjetivo()
        {
            if (objetivo != null)
            {
                currentTargetWorldPos = objetivo.position;
                hasValidTarget = true;
                return;
            }

            StoryManagerPrototipo3 sm = StoryManagerPrototipo3.Instance ?? FindAnyObjectByType<StoryManagerPrototipo3>();

            // 1. Resolución contextual prioritaria para el Prólogo (Nodo 0)
            if (sm != null && sm.CurrentActiveNode != null && sm.CurrentActiveNode.id == "node_0_apertura")
            {
                // Paso 1: Hablar con Marco en su posición diurna
                if (!sm.hasSpokenToMarcoDay)
                {
                    if (sm.worldBinder != null && sm.worldBinder.SpawnedMarco != null)
                    {
                        currentTargetWorldPos = sm.worldBinder.SpawnedMarco.transform.position;
                        hasValidTarget = true;
                        return;
                    }

                    Transform marcoT = WorldRegistry.Get("Marco");
                    if (marcoT != null)
                    {
                        currentTargetWorldPos = marcoT.position;
                        hasValidTarget = true;
                        return;
                    }
                }
                // Paso 2: Ya habló con Marco pero falta fabricar en mesa o saquear
                else if (!sm.hasCraftedInitialTool)
                {
                    Transform craftTable = WorldRegistry.Get("CraftTable") ?? WorldRegistry.Get("MesaFabricacion");
                    if (craftTable != null)
                    {
                        currentTargetWorldPos = craftTable.position;
                        hasValidTarget = true;
                        return;
                    }
                }
                // Paso 3: Marco Feral de noche
                else if (sm.hasTriggeredMarcoFeralCutscene || (TimeManager.Instance != null && TimeManager.Instance.CurrentCycleState == TimeManager.CycleState.Night))
                {
                    if (sm.worldBinder != null && sm.worldBinder.SpawnedMarco != null)
                    {
                        currentTargetWorldPos = sm.worldBinder.SpawnedMarco.transform.position;
                        hasValidTarget = true;
                        return;
                    }
                }
            }

            // 2. Acciones compiladas por QuestCompiler
            if (QuestCompiler.Instance != null && QuestCompiler.Instance.CurrentAction != null)
            {
                var act = QuestCompiler.Instance.CurrentAction;
                if (act.boundTarget != null)
                {
                    currentTargetWorldPos = act.boundTarget.position;
                    hasValidTarget = true;
                    return;
                }
                if (act.boundPosition != Vector3.zero)
                {
                    currentTargetWorldPos = act.boundPosition;
                    hasValidTarget = true;
                    return;
                }
            }

            // 3. Nodos activos del grafo narrativo (StoryGraph)
            if (sm != null && sm.CurrentGraph != null)
            {
                List<StoryNode> activeNodes = sm.CurrentGraph.GetActiveNodes();
                if (activeNodes.Count > 0 && playerTransform != null)
                {
                    StoryNode closestNode = null;
                    float bestDist = float.MaxValue;
                    for (int i = 0; i < activeNodes.Count; i++)
                    {
                        StoryNode cand = activeNodes[i];
                        if (cand == null || cand.worldPosition == Vector3.zero) continue;

                        float d = Vector3.Distance(playerTransform.position, cand.worldPosition);
                        // Evitar apuntar a la propia posición del jugador si distancia < 1m
                        if (d > 0.5f && d < bestDist)
                        {
                            bestDist = d;
                            closestNode = cand;
                        }
                    }

                    if (closestNode != null)
                    {
                        currentTargetWorldPos = closestNode.worldPosition;
                        hasValidTarget = true;
                        return;
                    }
                }

                if (sm.CurrentActiveNode != null && sm.CurrentActiveNode.worldPosition != Vector3.zero)
                {
                    currentTargetWorldPos = sm.CurrentActiveNode.worldPosition;
                    hasValidTarget = true;
                    return;
                }
            }
        }

        public void ActualizarObjetivoPorNodo(StoryNode node)
        {
            if (node == null)
            {
                hasValidTarget = false;
                return;
            }

            if (node.worldPosition != Vector3.zero)
            {
                currentTargetWorldPos = node.worldPosition;
                hasValidTarget = true;
            }
        }

        private void HandleContainerLooted(LootContainer c, List<ItemType> items)
        {
            ResolverPosicionObjetivo();
        }

        public void OcultarFlecha()
        {
            hasValidTarget = false;
            if (visualRoot != null) visualRoot.SetActive(false);
        }

        public void EstablecerObjetivoManual(Transform nuevoObjetivo)
        {
            objetivo = nuevoObjetivo;
            if (objetivo != null)
            {
                currentTargetWorldPos = objetivo.position;
                hasValidTarget = true;
                if (visualRoot != null) visualRoot.SetActive(true);
            }
        }

        private void ConstruirVisualSiNoExiste()
        {
            if (transform.childCount > 0)
            {
                visualRoot = transform.GetChild(0).gameObject;
                return;
            }

            if (arrowMaterial == null)
            {
                arrowMaterial = Resources.Load<Material>("AmarilloAmariilloPlatano");
                if (arrowMaterial == null)
                {
                    Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
                    arrowMaterial = new Material(s);
                    arrowMaterial.color = arrowColor;
                    if (arrowMaterial.HasProperty("_BaseColor")) arrowMaterial.SetColor("_BaseColor", arrowColor);
                    if (arrowMaterial.HasProperty("_EmissionColor"))
                    {
                        arrowMaterial.EnableKeyword("_EMISSION");
                        arrowMaterial.SetColor("_EmissionColor", arrowColor * 1.2f);
                    }
                }
            }

            visualRoot = new GameObject("VisualArrowMesh");
            visualRoot.transform.SetParent(transform, false);
            visualRoot.transform.localPosition = Vector3.zero;
            visualRoot.transform.localRotation = Quaternion.identity;

            GameObject shaft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shaft.name = "ArrowShaft";
            shaft.transform.SetParent(visualRoot.transform, false);
            shaft.transform.localPosition = new Vector3(0, 0, -0.15f);
            shaft.transform.localScale = new Vector3(0.12f, 0.08f, 0.45f);

            Collider colShaft = shaft.GetComponent<Collider>();
            if (colShaft != null) Destroy(colShaft);

            Renderer renShaft = shaft.GetComponent<Renderer>();
            if (renShaft != null) renShaft.material = arrowMaterial;

            GameObject tip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tip.name = "ArrowTip";
            tip.transform.SetParent(visualRoot.transform, false);
            tip.transform.localPosition = new Vector3(0, 0, 0.22f);
            tip.transform.localRotation = Quaternion.Euler(0, 45, 0);
            tip.transform.localScale = new Vector3(0.32f, 0.08f, 0.32f);

            Collider colTip = tip.GetComponent<Collider>();
            if (colTip != null) Destroy(colTip);

            Renderer renTip = tip.GetComponent<Renderer>();
            if (renTip != null) renTip.material = arrowMaterial;
        }

        private void OnDrawGizmos()
        {
            if (hasValidTarget)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, currentTargetWorldPos);
                Gizmos.DrawWireSphere(currentTargetWorldPos, 0.5f);
            }
        }
    }
}
