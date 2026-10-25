using System.Collections.Generic;
using UnityEngine;

namespace Prototipo2
{
    public class IsometricCameraFollow : MonoBehaviour
    {
        [Header("Seguimiento del Objetivo")]
        public Transform target;
        public Vector3 targetOffset = new Vector3(0, 0.4f, 0);
        public float smoothSpeed = 8f;

        [Header("Perspectiva Isométrica")]
        [Range(20f, 60f)] public float pitchAngle = 45f;
        [Range(0f, 360f)] public float yawAngle = 45f;
        public float distance = 20f;
        public float fieldOfView = 32f;

        [Header("Oclusión de Edificios")]
        public bool enableOcclusionFading = true;
        public LayerMask occlusionLayers = ~0; // Por defecto todo menos jugador
        public float sphereCastRadius = 0.5f;
        [Range(0.05f, 0.9f)] public float fadedAlpha = 0.20f;
        public float fadeSpeed = 7f;

        private Camera cam;

        // Estructura interna para rastrear el fade de renderers
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
                HamsterController hc = FindAnyObjectByType<HamsterController>();
                if (hc != null) target = hc.transform;
            }

            // Excluir capas de Ignore Raycast y UI de la oclusión
            occlusionLayers &= ~LayerMask.GetMask("Ignore Raycast", "UI");

            ActualizarRotacionCamara();
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                HamsterController hc = FindAnyObjectByType<HamsterController>();
                if (hc != null) target = hc.transform;
                if (target == null) return;
            }

            ActualizarPosicionCamara();

            if (enableOcclusionFading)
            {
                ActualizarOclusionEdificios();
            }
        }

        private void ActualizarRotacionCamara()
        {
            transform.rotation = Quaternion.Euler(pitchAngle, yawAngle, 0f);
        }

        private void ActualizarPosicionCamara()
        {
            ActualizarRotacionCamara();

            Vector3 targetCenter = target.position + targetOffset;
            Vector3 cameraDirection = transform.rotation * Vector3.back;
            Vector3 desiredPosition = targetCenter + cameraDirection * distance;

            transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * smoothSpeed);
        }

        private void ActualizarOclusionEdificios()
        {
            Vector3 targetCenter = target.position + targetOffset;
            Vector3 camPos = transform.position;
            Vector3 direction = targetCenter - camPos;
            float maxDist = direction.magnitude;

            if (maxDist <= 0.01f) return;

            // Marcar todos los objetos activos actualmente como no ocluyentes temporalmente
            foreach (var kvp in activeFadedObjects)
            {
                kvp.Value.isOccluding = false;
            }

            RaycastHit[] hits = Physics.SphereCastAll(camPos, sphereCastRadius, direction.normalized, maxDist, occlusionLayers);

            for (int i = 0; i < hits.Length; i++)
            {
                RaycastHit hit = hits[i];
                if (hit.transform == target || hit.transform.IsChildOf(target)) continue;

                // Si impacta con el suelo directamente muy cerca del hamster, ignorar
                if (hit.point.y <= 0.05f && Vector3.Distance(hit.point, targetCenter) < 1.0f) continue;

                Renderer[] renderers = hit.transform.GetComponentsInChildren<Renderer>();
                for (int r = 0; r < renderers.Length; r++)
                {
                    Renderer ren = renderers[r];
                    if (ren == null || !ren.enabled) continue;

                    // Ignorar emisores de partículas
                    if (ren is ParticleSystemRenderer) continue;

                    if (!activeFadedObjects.TryGetValue(ren, out FadedRendererData data))
                    {
                        data = new FadedRendererData
                        {
                            renderer = ren,
                            originalMaterials = ren.sharedMaterials,
                            instancedMaterials = ren.materials, // Clona las instancias para no afectar otros prefabs
                            currentAlpha = 1.0f,
                            targetAlpha = fadedAlpha,
                            isOccluding = true
                        };

                        ConfigurarModoTransparente(data.instancedMaterials);
                        activeFadedObjects.Add(ren, data);
                    }
                    else
                    {
                        data.isOccluding = true;
                        data.targetAlpha = fadedAlpha;
                    }
                }
            }

            // Actualizar alphas y restaurar materiales cuando no ocluyan
            toRemoveKeys.Clear();
            foreach (var kvp in activeFadedObjects)
            {
                Renderer ren = kvp.Key;
                FadedRendererData data = kvp.Value;

                if (ren == null)
                {
                    toRemoveKeys.Add(ren);
                    continue;
                }

                if (!data.isOccluding)
                {
                    data.targetAlpha = 1.0f;
                }

                data.currentAlpha = Mathf.MoveTowards(data.currentAlpha, data.targetAlpha, Time.deltaTime * fadeSpeed);
                AplicarAlpha(data.instancedMaterials, data.currentAlpha);

                // Si ya recuperó su opacidad total y no está ocluyendo, restaurar materiales originales
                if (!data.isOccluding && Mathf.Approximately(data.currentAlpha, 1.0f))
                {
                    ren.sharedMaterials = data.originalMaterials;
                    for (int m = 0; m < data.instancedMaterials.Length; m++)
                    {
                        if (data.instancedMaterials[m] != null)
                        {
                            Destroy(data.instancedMaterials[m]);
                        }
                    }
                    toRemoveKeys.Add(ren);
                }
            }

            for (int i = 0; i < toRemoveKeys.Count; i++)
            {
                activeFadedObjects.Remove(toRemoveKeys[i]);
            }
        }

        private void OnDisable()
        {
            foreach (var kvp in activeFadedObjects)
            {
                if (kvp.Key != null && kvp.Value != null && kvp.Value.originalMaterials != null)
                {
                    kvp.Key.sharedMaterials = kvp.Value.originalMaterials;
                    for (int m = 0; m < kvp.Value.instancedMaterials.Length; m++)
                    {
                        if (kvp.Value.instancedMaterials[m] != null)
                        {
                            Destroy(kvp.Value.instancedMaterials[m]);
                        }
                    }
                }
            }
            activeFadedObjects.Clear();
        }

        private void ConfigurarModoTransparente(Material[] mats)
        {
            if (mats == null) return;
            for (int i = 0; i < mats.Length; i++)
            {
                Material m = mats[i];
                if (m == null) continue;

                // Soporte para shader Standard
                if (m.HasProperty("_Mode"))
                {
                    m.SetFloat("_Mode", 2f); // Fade
                }

                // Soporte para URP Lit
                if (m.HasProperty("_Surface"))
                {
                    m.SetFloat("_Surface", 1f); // Transparent
                    m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                }

                m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                m.SetInt("_ZWrite", 0);
                m.DisableKeyword("_ALPHATEST_ON");
                m.EnableKeyword("_ALPHABLEND_ON");
                m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }
        }

        private void AplicarAlpha(Material[] mats, float alpha)
        {
            if (mats == null) return;
            for (int i = 0; i < mats.Length; i++)
            {
                Material m = mats[i];
                if (m == null) continue;

                if (m.HasProperty("_BaseColor"))
                {
                    Color c = m.GetColor("_BaseColor");
                    c.a = alpha;
                    m.SetColor("_BaseColor", c);
                }

                if (m.HasProperty("_Color"))
                {
                    Color c = m.GetColor("_Color");
                    c.a = alpha;
                    m.SetColor("_Color", c);
                }
            }
        }
    }
}
