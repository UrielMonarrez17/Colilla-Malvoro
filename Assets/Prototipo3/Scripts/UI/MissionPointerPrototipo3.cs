using System.Collections.Generic;
using UnityEngine;
using Prototipo3.Narrative;

namespace Prototipo3.UI
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

            if (playerTransform == null)
            {
                var p = FindFirstObjectByType<Prototipo3.Player.HamsterControllerPrototipo3>();
                if (p != null) playerTransform = p.transform;
            }

            ConstruirVisualSiNoExiste();

            if (StoryManagerPrototipo3.Instance != null)
            {
                StoryManagerPrototipo3.Instance.OnActiveNodeChanged += ActualizarObjetivoPorNodo;
                if (StoryManagerPrototipo3.Instance.CurrentActiveNode != null)
                {
                    ActualizarObjetivoPorNodo(StoryManagerPrototipo3.Instance.CurrentActiveNode);
                }
            }
        }

        private void OnDestroy()
        {
            if (StoryManagerPrototipo3.Instance != null)
            {
                StoryManagerPrototipo3.Instance.OnActiveNodeChanged -= ActualizarObjetivoPorNodo;
            }
        }

        public void ActualizarObjetivoPorNodo(StoryNodePrototipo3 node)
        {
            if (node == null)
            {
                hasValidTarget = false;
                if (visualRoot != null) visualRoot.SetActive(false);
                return;
            }

            currentTargetWorldPos = node.worldPosition;
            hasValidTarget = true;
            if (visualRoot != null) visualRoot.SetActive(true);
        }

        public void EstablecerObjetivoManual(Vector3 targetPos)
        {
            currentTargetWorldPos = targetPos;
            hasValidTarget = true;
            if (visualRoot != null) visualRoot.SetActive(true);
        }

        private void LateUpdate()
        {
            if (playerTransform == null)
            {
                var p = FindFirstObjectByType<Prototipo3.Player.HamsterControllerPrototipo3>();
                if (p != null) playerTransform = p.transform;
                else return;
            }

            float bob = Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
            transform.position = playerTransform.position + Vector3.up * (initialYOffset + bob);

            if (!hasValidTarget) return;

            Vector3 direction = currentTargetWorldPos - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.05f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, velocidadRotacion * Time.deltaTime);
            }
        }

        private void ConstruirVisualSiNoExiste()
        {
            if (GetComponentInChildren<MeshRenderer>() != null)
            {
                visualRoot = GetComponentInChildren<MeshRenderer>().gameObject;
                return;
            }

            visualRoot = new GameObject("BananaArrowVisual");
            visualRoot.transform.SetParent(this.transform, false);

            GameObject shaft = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            shaft.name = "ArrowShaft";
            shaft.transform.SetParent(visualRoot.transform, false);
            shaft.transform.localScale = new Vector3(0.045f, 0.12f, 0.045f);
            shaft.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            shaft.transform.localPosition = new Vector3(0f, 0f, -0.05f);
            Destroy(shaft.GetComponent<Collider>());

            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Cube);
            head.name = "ArrowHead";
            head.transform.SetParent(visualRoot.transform, false);
            head.transform.localScale = new Vector3(0.12f, 0.12f, 0.12f);
            head.transform.localRotation = Quaternion.Euler(45f, 45f, 0f);
            head.transform.localPosition = new Vector3(0f, 0f, 0.14f);
            Destroy(head.GetComponent<Collider>());

            if (arrowMaterial == null)
            {
                Shader unlitColorShader = Shader.Find("Universal Render Pipeline/Unlit");
                if (unlitColorShader == null) unlitColorShader = Shader.Find("Unlit/Color");
                if (unlitColorShader != null)
                {
                    arrowMaterial = new Material(unlitColorShader);
                    arrowMaterial.SetColor("_BaseColor", arrowColor);
                    arrowMaterial.color = arrowColor;
                }
            }

            if (arrowMaterial != null)
            {
                shaft.GetComponent<MeshRenderer>().sharedMaterial = arrowMaterial;
                head.GetComponent<MeshRenderer>().sharedMaterial = arrowMaterial;
            }
        }
    }
}
