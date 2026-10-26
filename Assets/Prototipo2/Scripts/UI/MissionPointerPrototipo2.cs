using System.Collections.Generic;
using UnityEngine;
using Prototipo2.Narrative;

namespace Prototipo2
{
    [SelectionBase]
    public class MissionPointerPrototipo2 : MonoBehaviour
    {
        [Header("Referencias")]
        [Tooltip("Transform del hámster al que sigue la flecha.")]
        public Transform playerTransform;

        [Tooltip("Objetivo manual (Transform). Si es nulo, se rastrea la posición de la misión.")]
        public Transform objetivo;

        [Header("Ajustes de Posición y Animación")]
        [Tooltip("Altura de flotación sobre el hámster.")]
        public float hoverHeight = 0.85f;

        [Tooltip("Velocidad de interpolación de giro hacia el objetivo.")]
        public float velocidadRotacion = 12f;

        [Tooltip("Velocidad del balanceo vertical (bobbing).")]
        public float bobSpeed = 3.5f;

        [Tooltip("Amplitud del balanceo vertical.")]
        public float bobAmplitude = 0.08f;

        [Header("Material y Aspecto Visual")]
        public Material arrowMaterial;
        public Color arrowColor = new Color(1f, 0.92f, 0.016f, 1f); // Amarillo Plátano brillante

        // Estado interno
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

        private void Start()
        {
            if (playerTransform == null)
            {
                HamsterController hc = FindAnyObjectByType<HamsterController>();
                if (hc != null) playerTransform = hc.transform;
            }

            ConstruirVisualSiNoExiste();

            // Suscribirse a los cambios de nodo de la historia
            StoryManagerPrototipo2 sm = FindAnyObjectByType<StoryManagerPrototipo2>();
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

            StoryManagerPrototipo2 sm = FindAnyObjectByType<StoryManagerPrototipo2>();
            if (sm != null)
            {
                sm.OnActiveNodeChanged -= ActualizarObjetivoPorNodo;
                sm.OnDemoCompleted -= OcultarFlecha;
            }
        }

        private void LateUpdate()
        {
            if (playerTransform == null)
            {
                HamsterController hc = FindAnyObjectByType<HamsterController>();
                if (hc != null) playerTransform = hc.transform;
                if (playerTransform == null) return;
            }

            // 1. Posicionamiento suave sobre la cabeza del hámster con balanceo senoidal
            float bob = Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
            Vector3 targetPos = playerTransform.position + Vector3.up * (hoverHeight + bob);
            transform.position = targetPos;

            // 2. Determinar la posición del objetivo actual
            ResolverPosicionObjetivo();

            // 3. Rotar hacia el objetivo en el plano XZ (horizontal)
            if (hasValidTarget)
            {
                Vector3 direccion = currentTargetWorldPos - transform.position;
                direccion.y = 0; // Mantener la flecha horizontal sin apuntar al subsuelo

                if (direccion.sqrMagnitude > 0.05f)
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
            // A. Si se asignó un Transform explícito, usarlo
            if (objetivo != null)
            {
                currentTargetWorldPos = objetivo.position;
                hasValidTarget = true;
                return;
            }

            // B. Si hay StoryManager activo, consultar nodo actual
            StoryManagerPrototipo2 sm = StoryManagerPrototipo2.Instance ?? FindAnyObjectByType<StoryManagerPrototipo2>();
            if (sm != null && sm.CurrentActiveNode != null)
            {
                StoryNode node = sm.CurrentActiveNode;

                // Si es nodo de botín (quest), apuntar al contenedor no saqueado más cercano
                if (node.id == "node_quest")
                {
                    Transform lootTarget = EncontrarLootMasCercano();
                    if (lootTarget != null)
                    {
                        currentTargetWorldPos = lootTarget.position;
                        hasValidTarget = true;
                        return;
                    }
                }

                // Para los demás nodos, usar su posición en el mundo
                if (node.worldPosition != Vector3.zero)
                {
                    currentTargetWorldPos = node.worldPosition;
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

            if (node.id == "node_quest")
            {
                Transform loot = EncontrarLootMasCercano();
                if (loot != null)
                {
                    currentTargetWorldPos = loot.position;
                    hasValidTarget = true;
                    return;
                }
            }

            if (node.worldPosition != Vector3.zero)
            {
                currentTargetWorldPos = node.worldPosition;
                hasValidTarget = true;
            }
        }

        private Transform EncontrarLootMasCercano()
        {
            if (playerTransform == null) return null;

            LootContainer[] containers = FindObjectsByType<LootContainer>(FindObjectsSortMode.None);
            Transform mejor = null;
            float menorDist = float.MaxValue;

            Vector3 playerPos = playerTransform.position;
            foreach (var c in containers)
            {
                if (c == null || !c.gameObject.activeInHierarchy) continue;
                if (c.ItemsRestantes <= 0) continue; // Ya fue saqueado

                float d = Vector3.Distance(playerPos, c.transform.position);
                if (d < menorDist)
                {
                    menorDist = d;
                    mejor = c.transform;
                }
            }

            return mejor;
        }

        private void HandleContainerLooted(LootContainer c, List<ItemType> items)
        {
            // Al saquear un contenedor, recalcular de inmediato al siguiente más cercano
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
            // Si ya tiene hijos visuales en la escena o prefab, usarlos
            if (transform.childCount > 0)
            {
                visualRoot = transform.GetChild(0).gameObject;
                return;
            }

            // Material amarillo brillante con soporte para URP Lit / Unlit
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

            // 1. Vástago de la flecha (Cuerpo posterior)
            GameObject shaft = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shaft.name = "ArrowShaft";
            shaft.transform.SetParent(visualRoot.transform, false);
            shaft.transform.localPosition = new Vector3(0, 0, -0.15f);
            shaft.transform.localScale = new Vector3(0.12f, 0.08f, 0.45f);

            Collider colShaft = shaft.GetComponent<Collider>();
            if (colShaft != null) Destroy(colShaft);

            Renderer renShaft = shaft.GetComponent<Renderer>();
            if (renShaft != null) renShaft.material = arrowMaterial;

            // 2. Punta de la flecha (Pirámide/Cuña frontal orientada a +Z)
            GameObject tip = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tip.name = "ArrowTip";
            tip.transform.SetParent(visualRoot.transform, false);
            tip.transform.localPosition = new Vector3(0, 0, 0.22f);
            tip.transform.localRotation = Quaternion.Euler(0, 45, 0); // Rotado a 45 grados para formar la punta de flecha
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
