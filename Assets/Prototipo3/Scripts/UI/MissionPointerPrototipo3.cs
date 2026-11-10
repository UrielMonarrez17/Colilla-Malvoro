using System;
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
            else
            {
                if (visualRoot != null && visualRoot.activeSelf)
                {
                    visualRoot.SetActive(false);
                }
            }
        }

        private void ResolverPosicionObjetivo()
        {
            // Prioridad 1: Objetivo manual explícito
            if (objetivo != null)
            {
                currentTargetWorldPos = objetivo.position;
                hasValidTarget = true;
                return;
            }

            // Prioridad 2: Historia Clásica Lineal (StoryManagerPrototipo3)
            StoryManagerPrototipo3 sm = StoryManagerPrototipo3.Instance ?? FindAnyObjectByType<StoryManagerPrototipo3>();
            CityGeneratorPrototipo3 cityGen = CityGeneratorPrototipo3.Instance ?? FindAnyObjectByType<CityGeneratorPrototipo3>();

            if (sm != null && sm.CurrentActiveNode != null)
            {
                StoryNode activeNode = sm.CurrentActiveNode;
                Vector3 targetPos = Vector3.zero;
                bool targetFound = false;

                switch (activeNode.id)
                {
                    case "node_0_apertura":
                        // 1. Si no ha hablado con Marco de día: apuntar a Marco
                        if (!sm.hasSpokenToMarcoDay)
                        {
                            Transform marcoT = WorldRegistry.Get("Marco");
                            if (marcoT != null)
                            {
                                targetPos = marcoT.position;
                                targetFound = true;
                            }
                            else if (cityGen != null)
                            {
                                targetPos = cityGen.GetLandmarkPosition("Marco");
                                targetFound = (targetPos != Vector3.zero);
                            }
                        }
                        // 2. Si ya habló pero no ha fabricado su primera trampa
                        else if (!sm.hasCraftedInitialTool)
                        {
                            bool canCraft = (TrapSystemPrototipo3.Instance != null || true) &&
                                            (TrapSystemPrototipo3.CanCraftTrap(TipoTrampa.Madera) ||
                                             TrapSystemPrototipo3.CanCraftTrap(TipoTrampa.Puas) ||
                                             TrapSystemPrototipo3.CanCraftTrap(TipoTrampa.Red));

                            if (!canCraft)
                            {
                                // Si no puede fabricar aún: apuntar al contenedor sin saquear más cercano
                                LootContainer closestContainer = null;
                                float bestContainerDist = float.MaxValue;
                                Vector3 hamsterPos = playerTransform != null ? playerTransform.position : transform.position;

                                LootContainer[] containers = FindObjectsByType<LootContainer>(FindObjectsSortMode.None);
                                for (int i = 0; i < containers.Length; i++)
                                {
                                    LootContainer lc = containers[i];
                                    if (lc == null || lc.YaFueSaqueado || lc.isLooted) continue;

                                    float d = Vector3.Distance(hamsterPos, lc.transform.position);
                                    if (d < bestContainerDist)
                                    {
                                        bestContainerDist = d;
                                        closestContainer = lc;
                                    }
                                }

                                if (closestContainer != null)
                                {
                                    targetPos = closestContainer.transform.position;
                                    targetFound = true;
                                }
                            }

                            // Si puede fabricar (o no hay contenedores): apuntar a la Mesa de Fabricación
                            if (!targetFound)
                            {
                                Transform craftTable = WorldRegistry.Get("CraftTable") ?? WorldRegistry.Get("MesaFabricacion");
                                if (craftTable != null)
                                {
                                    targetPos = craftTable.position;
                                    targetFound = true;
                                }
                                else if (cityGen != null && cityGen.CraftTableSpawnPoint != Vector3.zero)
                                {
                                    targetPos = cityGen.CraftTableSpawnPoint;
                                    targetFound = true;
                                }
                            }
                        }
                        // 3. Ya fabricó la trampa: apuntar al refugio de la Base
                        else
                        {
                            Transform baseT = WorldRegistry.Get("Base");
                            if (baseT != null)
                            {
                                targetPos = baseT.position;
                                targetFound = true;
                            }
                            else if (cityGen != null && cityGen.PlayerSpawnPoint != Vector3.zero)
                            {
                                targetPos = cityGen.PlayerSpawnPoint;
                                targetFound = true;
                            }
                        }
                        break;

                    case "node_a1_escuela":
                        // Apuntar a Toby en su taller de robótica (o Tony / posición del nodo)
                        {
                            Transform toby = WorldRegistry.Get("Toby") ?? WorldRegistry.Get("Tony");
                            if (toby != null)
                            {
                                targetPos = toby.position;
                                targetFound = true;
                            }
                            else if (activeNode.worldPosition != Vector3.zero)
                            {
                                targetPos = activeNode.worldPosition;
                                targetFound = true;
                            }
                        }
                        break;

                    case "node_a2_toby":
                        // Apuntar a la Oficina del Rector Morales
                        {
                            Transform ofi = WorldRegistry.Get("Oficina_Morales");
                            if (ofi != null)
                            {
                                targetPos = ofi.position;
                                targetFound = true;
                            }
                            else if (activeNode.worldPosition != Vector3.zero)
                            {
                                targetPos = activeNode.worldPosition;
                                targetFound = true;
                            }
                        }
                        break;

                    case "node_a3_apagon":
                        // Apuntar al Laboratorio de Química de la escuela
                        {
                            Transform lab = WorldRegistry.Get("Laboratorio_Quimica");
                            if (lab != null)
                            {
                                targetPos = lab.position;
                                targetFound = true;
                            }
                            else if (activeNode.worldPosition != Vector3.zero)
                            {
                                targetPos = activeNode.worldPosition;
                                targetFound = true;
                            }
                        }
                        break;

                    case "node_b1_bosque":
                        // Si no ha hablado con Moka -> Moka; si ya habló -> Sultán
                        {
                            Transform mokaOrSultan = !sm.hasSpokenToMoka ? WorldRegistry.Get("Moka") : WorldRegistry.Get("Sultan");
                            if (mokaOrSultan != null)
                            {
                                targetPos = mokaOrSultan.position;
                                targetFound = true;
                            }
                            else if (activeNode.worldPosition != Vector3.zero)
                            {
                                targetPos = activeNode.worldPosition;
                                targetFound = true;
                            }
                        }
                        break;

                    case "node_b2_postes":
                        // Revisar Poste_Reflector_1, Poste_Reflector_2, Poste_Reflector_3; apuntar al primer !mech.isCompleted
                        {
                            string[] postes = { "Poste_Reflector_1", "Poste_Reflector_2", "Poste_Reflector_3" };
                            for (int i = 0; i < postes.Length; i++)
                            {
                                Transform pTrans = WorldRegistry.Get(postes[i]);
                                if (pTrans != null)
                                {
                                    StoryMechanismPrototipo3 mech = pTrans.GetComponent<StoryMechanismPrototipo3>() ?? pTrans.GetComponentInChildren<StoryMechanismPrototipo3>();
                                    if (mech != null && !mech.isCompleted)
                                    {
                                        targetPos = pTrans.position;
                                        targetFound = true;
                                        break;
                                    }
                                }
                            }
                            if (!targetFound && activeNode.worldPosition != Vector3.zero)
                            {
                                targetPos = activeNode.worldPosition;
                                targetFound = true;
                            }
                        }
                        break;

                    case "node_b3_sultan":
                        // Apuntar al Interruptor de la Subestación
                        {
                            Transform inter = WorldRegistry.Get("Interruptor_Subestacion");
                            if (inter != null)
                            {
                                targetPos = inter.position;
                                targetFound = true;
                            }
                            else if (activeNode.worldPosition != Vector3.zero)
                            {
                                targetPos = activeNode.worldPosition;
                                targetFound = true;
                            }
                        }
                        break;

                    case "node_c1_hospital":
                        // Si !hasSpokenToSalgado -> Salgado; else if !hasVisitedSofiaRoom304 -> Sofia; else -> Clara
                        {
                            Transform hospTarget = null;
                            if (!sm.hasSpokenToSalgado) hospTarget = WorldRegistry.Get("Salgado");
                            else if (!sm.hasVisitedSofiaRoom304) hospTarget = WorldRegistry.Get("Sofia");
                            else hospTarget = WorldRegistry.Get("Clara");

                            if (hospTarget != null)
                            {
                                targetPos = hospTarget.position;
                                targetFound = true;
                            }
                            else if (activeNode.worldPosition != Vector3.zero)
                            {
                                targetPos = activeNode.worldPosition;
                                targetFound = true;
                            }
                        }
                        break;

                    case "node_c2_clara":
                        // Apuntar a la Válvula de la Cámara Hiperbárica
                        {
                            Transform val = WorldRegistry.Get("Valvula_Hiperbarica");
                            if (val != null)
                            {
                                targetPos = val.position;
                                targetFound = true;
                            }
                            else if (activeNode.worldPosition != Vector3.zero)
                            {
                                targetPos = activeNode.worldPosition;
                                targetFound = true;
                            }
                        }
                        break;

                    case "node_c3_oxigeno":
                        // Si !HasKeyItem("Cilindro de Oxígeno Puro") -> Cilindro_Oxigeno; else -> Salgado
                        {
                            Transform oxiTarget = !sm.HasKeyItem("Cilindro de Oxígeno Puro")
                                ? WorldRegistry.Get("Cilindro_Oxigeno")
                                : WorldRegistry.Get("Salgado");

                            if (oxiTarget != null)
                            {
                                targetPos = oxiTarget.position;
                                targetFound = true;
                            }
                            else if (activeNode.worldPosition != Vector3.zero)
                            {
                                targetPos = activeNode.worldPosition;
                                targetFound = true;
                            }
                        }
                        break;

                    case "node_d1_torre":
                        // Si !hasSpokenToVane -> Vane; else -> Celda_Dra_Elena
                        {
                            Transform torreTarget = !sm.hasSpokenToVane
                                ? WorldRegistry.Get("Vane")
                                : WorldRegistry.Get("Celda_Dra_Elena");

                            if (torreTarget != null)
                            {
                                targetPos = torreTarget.position;
                                targetFound = true;
                            }
                            else if (activeNode.worldPosition != Vector3.zero)
                            {
                                targetPos = activeNode.worldPosition;
                                targetFound = true;
                            }
                        }
                        break;

                    case "node_d2_kross":
                        // Revisar Condensador_UV_1, 2, 3; apuntar al primer !mech.isCompleted
                        {
                            string[] condensadores = { "Condensador_UV_1", "Condensador_UV_2", "Condensador_UV_3" };
                            for (int i = 0; i < condensadores.Length; i++)
                            {
                                Transform cTrans = WorldRegistry.Get(condensadores[i]);
                                if (cTrans != null)
                                {
                                    StoryMechanismPrototipo3 mech = cTrans.GetComponent<StoryMechanismPrototipo3>() ?? cTrans.GetComponentInChildren<StoryMechanismPrototipo3>();
                                    if (mech != null && !mech.isCompleted)
                                    {
                                        targetPos = cTrans.position;
                                        targetFound = true;
                                        break;
                                    }
                                }
                            }
                            if (!targetFound && activeNode.worldPosition != Vector3.zero)
                            {
                                targetPos = activeNode.worldPosition;
                                targetFound = true;
                            }
                        }
                        break;

                    case "node_d3_final":
                        // Apuntar al Inyector Central de la chimenea
                        {
                            Transform iny = WorldRegistry.Get("Inyector_Central");
                            if (iny != null)
                            {
                                targetPos = iny.position;
                                targetFound = true;
                            }
                            else if (activeNode.worldPosition != Vector3.zero)
                            {
                                targetPos = activeNode.worldPosition;
                                targetFound = true;
                            }
                        }
                        break;

                    default:
                        if (activeNode.worldPosition != Vector3.zero)
                        {
                            targetPos = activeNode.worldPosition;
                            targetFound = true;
                        }
                        break;
                }

                if (targetFound && targetPos != Vector3.zero)
                {
                    currentTargetWorldPos = targetPos;
                    hasValidTarget = true;
                    return;
                }
                else if (activeNode.worldPosition != Vector3.zero)
                {
                    currentTargetWorldPos = activeNode.worldPosition;
                    hasValidTarget = true;
                    return;
                }
            }

            // Prioridad 3: Acciones compiladas por QuestCompiler (solo si corresponden al nodo actual)
            if (QuestCompiler.Instance != null && QuestCompiler.Instance.CurrentAction != null)
            {
                if (sm == null || sm.CurrentActiveNode == null || string.Equals(QuestCompiler.Instance.ActiveStoryNodeID, sm.CurrentActiveNode.id, StringComparison.OrdinalIgnoreCase))
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
            }

            // Prioridad 4: Nodos activos del grafo narrativo (StoryGraph)
            if (sm != null && sm.CurrentGraph != null)
            {
                List<StoryNode> activeNodes = sm.CurrentGraph.GetActiveNodes();
                if (activeNodes != null && activeNodes.Count > 0 && playerTransform != null)
                {
                    StoryNode closestNode = null;
                    float bestDist = float.MaxValue;
                    for (int i = 0; i < activeNodes.Count; i++)
                    {
                        StoryNode cand = activeNodes[i];
                        if (cand == null || cand.worldPosition == Vector3.zero) continue;

                        float d = Vector3.Distance(playerTransform.position, cand.worldPosition);
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

            hasValidTarget = false;
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
