using System.Collections.Generic;
using UnityEngine;

namespace Prototipo3.CameraSystem
{
    public class IsometricCameraFollowPrototipo3 : CameraFollow
    {
        [Header("Ajustes de Seguimiento Isométrico")]
        public Vector3 targetOffset = new Vector3(0, 0.4f, 0);

        [Header("Perspectiva Isométrica")]
        [Range(20f, 60f)] public float pitchAngle = 45f;
        [Range(0f, 360f)] public float yawAngle = 45f;
        public float distance = 20f;
        public float fieldOfView = 32f;

        [Header("Oclusión de Edificios")]
        public bool enableOcclusionFading = true;
        public LayerMask occlusionLayers = ~0;
        public float sphereCastRadius = 0.5f;
        [Range(0.05f, 0.9f)] public float fadedAlpha = 0.20f;
        public float fadeSpeed = 7f;

        private Camera cam;

        private class FadedRendererData
        {
            public Renderer renderer;
            public Material[] originalMaterials;
            public Material[] instancedMaterials;
            public float currentAlpha = 1.0f;
            public float targetAlpha = 1.0f;
            public bool isOccluding = false;
        }

        private readonly Dictionary<Renderer, FadedRendererData> activeFadedObjects = new Dictionary<Renderer, FadedRendererData>();
        private readonly List<Renderer> toRemoveKeys = new List<Renderer>();

        private void Awake()
        {
            cam = GetComponent<Camera>();
            if (cam == null) cam = Camera.main;

            if (cam != null)
            {
                cam.orthographic = false;
                cam.fieldOfView = fieldOfView;
            }
        }

        private void Start()
        {
            if (target == null)
            {
                var hamster = FindFirstObjectByType<Prototipo3.Player.HamsterControllerPrototipo3>();
                if (hamster != null)
                {
                    target = hamster.transform;
                }
            }
        }

        protected override void LateUpdate()
        {
            if (target == null)
            {
                var hamster = FindFirstObjectByType<Prototipo3.Player.HamsterControllerPrototipo3>();
                if (hamster != null) target = hamster.transform;
                else return;
            }

            Vector3 focusPoint = target.position + targetOffset;
            Quaternion isoRotation = Quaternion.Euler(pitchAngle, yawAngle, 0f);
            Vector3 desiredPosition = focusPoint - (isoRotation * Vector3.forward * distance);

            transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, isoRotation, smoothSpeed * Time.deltaTime);

            if (enableOcclusionFading)
            {
                HandleOcclusionFading(focusPoint);
            }
        }

        private void HandleOcclusionFading(Vector3 focusPoint)
        {
            Vector3 camPos = transform.position;
            Vector3 dir = focusPoint - camPos;
            float dist = dir.magnitude;

            if (dist < 0.1f) return;

            foreach (var kvp in activeFadedObjects)
            {
                kvp.Value.isOccluding = false;
            }

            RaycastHit[] hits = Physics.SphereCastAll(camPos, sphereCastRadius, dir.normalized, dist, occlusionLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits.Length; i++)
            {
                Collider col = hits[i].collider;
                if (col == null || col.transform == target || col.transform.IsChildOf(target)) continue;

                Renderer rend = col.GetComponent<Renderer>();
                if (rend == null) rend = col.GetComponentInParent<Renderer>();
                if (rend == null) continue;

                if (!activeFadedObjects.ContainsKey(rend))
                {
                    var data = new FadedRendererData
                    {
                        renderer = rend,
                        originalMaterials = rend.sharedMaterials,
                        instancedMaterials = rend.materials,
                        currentAlpha = 1.0f,
                        targetAlpha = fadedAlpha,
                        isOccluding = true
                    };
                    ConfigureMaterialsForTransparency(data.instancedMaterials);
                    rend.materials = data.instancedMaterials;
                    activeFadedObjects.Add(rend, data);
                }
                else
                {
                    activeFadedObjects[rend].isOccluding = true;
                    activeFadedObjects[rend].targetAlpha = fadedAlpha;
                }
            }

            toRemoveKeys.Clear();
            foreach (var kvp in activeFadedObjects)
            {
                FadedRendererData data = kvp.Value;
                if (data.renderer == null)
                {
                    toRemoveKeys.Add(kvp.Key);
                    continue;
                }

                if (!data.isOccluding)
                {
                    data.targetAlpha = 1.0f;
                }

                data.currentAlpha = Mathf.MoveTowards(data.currentAlpha, data.targetAlpha, fadeSpeed * Time.deltaTime);
                SetAlphaOnMaterials(data.instancedMaterials, data.currentAlpha);

                if (!data.isOccluding && Mathf.Approximately(data.currentAlpha, 1.0f))
                {
                    data.renderer.sharedMaterials = data.originalMaterials;
                    toRemoveKeys.Add(kvp.Key);
                }
            }

            for (int i = 0; i < toRemoveKeys.Count; i++)
            {
                activeFadedObjects.Remove(toRemoveKeys[i]);
            }
        }

        private void ConfigureMaterialsForTransparency(Material[] materials)
        {
            for (int i = 0; i < materials.Length; i++)
            {
                Material m = materials[i];
                if (m == null) continue;

                if (m.HasProperty("_Mode"))
                {
                    m.SetFloat("_Mode", 3);
                    m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    m.SetInt("_ZWrite", 0);
                    m.DisableKeyword("_ALPHATEST_ON");
                    m.EnableKeyword("_ALPHABLEND_ON");
                    m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                    m.renderQueue = 3000;
                }
            }
        }

        private void SetAlphaOnMaterials(Material[] materials, float alpha)
        {
            for (int i = 0; i < materials.Length; i++)
            {
                Material m = materials[i];
                if (m == null) continue;

                if (m.HasProperty("_Color"))
                {
                    Color c = m.color;
                    c.a = alpha;
                    m.color = c;
                }
                else if (m.HasProperty("_BaseColor"))
                {
                    Color c = m.GetColor("_BaseColor");
                    c.a = alpha;
                    m.SetColor("_BaseColor", c);
                }
            }
        }
    }
}
