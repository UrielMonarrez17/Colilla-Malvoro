using System.Collections.Generic;
using UnityEngine;

namespace Prototipo3
{
    public class IsometricCameraFollow : CameraFollow
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
        public LayerMask occlusionLayers = ~0; // Por defecto todo menos jugador
        public float sphereCastRadius = 0.5f;
        [Tooltip("Margen anticipado en bordes visuales antes de cubrir a Kilo (m)")]
        [Range(0.0f, 0.45f)] public float occlusionEdgeMargin = 0.28f;
        [Range(0.05f, 0.9f)] public float fadedAlpha = 0.20f;
        public float fadeSpeed = 7f;
        [SerializeField] private Shader occlusionFadeShader;

        private Camera cam;
        private Shader cachedOcclusionShader;
        private Transform cutsceneOverrideTarget;
        private float cutsceneDistanceOverride = -1f;
        private readonly RaycastHit[] rayHitBuffer = new RaycastHit[32];
        private readonly Collider[] camOverlapBuffer = new Collider[8];
        private readonly HashSet<Transform> processedHitRoots = new HashSet<Transform>();

        public void SetCutsceneOverrideTarget(Transform overrideTarget, float customDistance = 14f)
        {
            cutsceneOverrideTarget = overrideTarget;
            cutsceneDistanceOverride = customDistance;
        }

        public void ClearCutsceneOverrideTarget()
        {
            cutsceneOverrideTarget = null;
            cutsceneDistanceOverride = -1f;
        }

        // Estructura interna para rastrear el fade de renderers
        private class FadedRendererData
        {
            public Renderer renderer;
            public Material[] originalMaterials;
            public Material[] instancedMaterials;
            public float[] originalBaseAlphas;
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

            ResolveOcclusionShader();
            ActualizarRotacionCamara();
            offset = transform.rotation * (Vector3.back * distance);
        }

        private void OnValidate()
        {
            if (cam == null) cam = GetComponent<Camera>();
            if (cam != null)
            {
                cam.orthographic = false;
                cam.fieldOfView = fieldOfView;
            }

            ActualizarRotacionCamara();
            offset = transform.rotation * (Vector3.back * distance);
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
            offset = transform.rotation * (Vector3.back * distance);
        }

        protected override void LateUpdate()
        {
            if (target == null)
            {
                HamsterController hc = FindAnyObjectByType<HamsterController>();
                if (hc != null) target = hc.transform;
                if (target == null && cutsceneOverrideTarget == null) return;
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

            Transform activeTarget = cutsceneOverrideTarget != null ? cutsceneOverrideTarget : target;
            if (activeTarget == null) return;

            float activeDist = cutsceneDistanceOverride > 0f ? cutsceneDistanceOverride : distance;
            offset = transform.rotation * (Vector3.back * activeDist);
            Vector3 targetCenter = activeTarget.position + targetOffset;
            Vector3 desiredPosition = targetCenter + offset;

            float followSpd = cutsceneOverrideTarget != null ? Mathf.Max(3.5f, smoothSpeed * 0.65f) : smoothSpeed;
            transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.unscaledDeltaTime * followSpd);
        }

        private void ActualizarOclusionEdificios()
        {
            Transform activeTarget = cutsceneOverrideTarget != null ? cutsceneOverrideTarget : target;
            if (activeTarget == null) return;

            Vector3 basePos = activeTarget.position;
            Vector3 bodyCenter = basePos + new Vector3(0f, 0.32f, 0f);
            Vector3 camPos = transform.position;
            Vector3 direction = bodyCenter - camPos;
            float maxDist = direction.magnitude;

            if (maxDist <= 0.01f) return;

            // Marcar todos los objetos activos actualmente como no ocluyentes temporalmente
            foreach (var kvp in activeFadedObjects)
            {
                kvp.Value.isOccluding = false;
                kvp.Value.targetAlpha = 1.0f;
            }

            // Cuando Kilo está dentro de la casita del Refugio Seguro, transparentar automáticamente sus muros frontales/laterales
            // (sin tocar jamás el suelo, alfombras ni la mesa de crafteo) para que el interior y el hámster se vean nítidos!
            if (SafeRefugeZone.ActiveZone != null && SafeRefugeZone.IsPointOrPlayerInRefuge(activeTarget.position))
            {
                Transform refugeRoot = SafeRefugeZone.ActiveZone.transform;
                Transform frontFacade = refugeRoot.Find("FrontFacadeWalls");
                if (frontFacade != null) FadeHierarchyWallsOnly(frontFacade, 0.14f);

                Transform interiorWalls = refugeRoot.Find("InteriorModel/Layout/Walls");
                if (interiorWalls != null) FadeHierarchyWallsOnly(interiorWalls, 0.16f);
            }

            // Abanico de 5 rayos cámara -> silueta frontal de Kilo (con margen lateral/frontal de ~0.28m, nunca hacia atrás)
            // para que tocar una pared siendo visible no active la transparencia.
            processedHitRoots.Clear();
            Vector3 bodyHead = basePos + new Vector3(0f, 0.56f, 0f);
            Vector3 camRight = transform.right;
            Vector3 camForwardXZ = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
            if (camForwardXZ.sqrMagnitude > 0.001f) camForwardXZ.Normalize();
            else camForwardXZ = Vector3.forward;

            float edge = Mathf.Clamp(occlusionEdgeMargin, 0f, 0.45f);
            Ray centerRay = new Ray(camPos, direction / maxDist);
            float centerMaxRayDist = Mathf.Max(0.05f, maxDist - 0.10f);

            // Si la cámara queda dentro del BoxCollider de un rascacielos alto, detectarlo igualmente
            int overlapCount = Physics.OverlapSphereNonAlloc(camPos, 0.15f, camOverlapBuffer, occlusionLayers, QueryTriggerInteraction.Ignore);
            for (int o = 0; o < overlapCount; o++)
            {
                ProcessOcclusionHitCollider(camOverlapBuffer[o], activeTarget, basePos, camPos, centerRay, centerMaxRayDist, 0f, camPos.y);
            }

            for (int s = 0; s < 5; s++)
            {
                Vector3 samplePoint = s switch
                {
                    0 => bodyCenter,
                    1 => bodyHead,
                    2 => bodyCenter - camRight * edge,
                    3 => bodyCenter + camRight * edge,
                    _ => bodyCenter - camForwardXZ * edge
                };

                Vector3 toSample = samplePoint - camPos;
                float sampleDist = toSample.magnitude;
                if (sampleDist <= 0.25f) continue;

                float maxRayDist = sampleDist - 0.10f;
                if (maxRayDist <= 0.05f) continue;

                Ray ray = new Ray(camPos, toSample / sampleDist);
                int hitCount = Physics.RaycastNonAlloc(ray, rayHitBuffer, maxRayDist, occlusionLayers, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < hitCount; i++)
                {
                    RaycastHit hit = rayHitBuffer[i];
                    ProcessOcclusionHitCollider(hit.collider, activeTarget, basePos, camPos, ray, maxRayDist, hit.distance, hit.point.y);
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
                    DestroyInstancedMaterials(data);
                    toRemoveKeys.Add(ren);
                    continue;
                }

                if (!data.isOccluding)
                {
                    data.targetAlpha = 1.0f;
                }

                data.currentAlpha = Mathf.MoveTowards(data.currentAlpha, data.targetAlpha, Time.unscaledDeltaTime * fadeSpeed);
                AplicarAlpha(data);

                // Si ya recuperó su opacidad total y no está ocluyendo, restaurar materiales originales
                if (!data.isOccluding && Mathf.Approximately(data.currentAlpha, 1.0f))
                {
                    if (data.originalMaterials != null)
                    {
                        ren.sharedMaterials = data.originalMaterials;
                    }
                    DestroyInstancedMaterials(data);
                    toRemoveKeys.Add(ren);
                }
            }

            for (int i = 0; i < toRemoveKeys.Count; i++)
            {
                activeFadedObjects.Remove(toRemoveKeys[i]);
            }
        }

        private void ProcessOcclusionHitCollider(
            Collider col,
            Transform activeTarget,
            Vector3 basePos,
            Vector3 camPos,
            Ray ray,
            float maxRayDist,
            float hitDistance,
            float hitPointY)
        {
            if (col == null || col.isTrigger) return;
            Transform hitTransform = col.transform;
            if (hitTransform == activeTarget || hitTransform.IsChildOf(activeTarget)) return;
            if (processedHitRoots.Contains(hitTransform)) return;

            // Ignorar siempre cualquier piso, calle, cancha o losa debajo de la cintura del jugador para que jamás desaparezca el suelo.
            if ((hitDistance > 0.001f && hitPointY <= basePos.y + 0.25f) || col.bounds.max.y <= basePos.y + 0.35f) return;
            string hitName = hitTransform.name;
            if (hitName.StartsWith("Pole", System.StringComparison.OrdinalIgnoreCase)) return;
            if (HasGroundSurfaceName(hitName)) return;

            // Ignorar objetos interactivos, jugadores, enemigos, conos de visión, NPCs, mesas de crafteo, contenedores, etc.
            if (hitTransform.CompareTag("Interactable") || hitTransform.CompareTag("Player") || hitTransform.CompareTag("Enemy") || hitTransform.CompareTag("Respawn")) return;
            if (hitName.Contains("VisionCone") || hitName.Contains("DetectionRadius")) return;
            if (hitTransform.GetComponentInParent<CraftingTable>() != null) return;
            if (hitTransform.GetComponentInParent<LootContainer>() != null) return;
            if (hitTransform.GetComponentInParent<HamsterController>() != null) return;
            if (hitTransform.GetComponentInParent<EnemyAI>() != null) return;
            if (hitTransform.GetComponentInParent<LuluNPC>() != null) return;

            Renderer[] renderers = hitTransform.GetComponentsInChildren<Renderer>();
            if (!DoesRayOccludeBuildingRenderers(ray, maxRayDist, camPos, renderers)) return;

            processedHitRoots.Add(hitTransform);
            for (int r = 0; r < renderers.Length; r++)
            {
                Renderer ren = renderers[r];
                if (ShouldIgnoreRendererForOcclusion(ren)) continue;
                RegistrarRendererParaFade(ren, fadedAlpha);
            }
        }

        private static bool DoesRayOccludeBuildingRenderers(Ray ray, float maxRayDist, Vector3 camPos, Renderer[] renderers)
        {
            if (renderers == null || renderers.Length == 0) return false;

            for (int r = 0; r < renderers.Length; r++)
            {
                Renderer ren = renderers[r];
                if (ShouldIgnoreRendererForOcclusion(ren)) continue;

                Bounds expBounds = ren.bounds;
                expBounds.Expand(0.06f);
                if (expBounds.Contains(camPos)) return true;
                if (expBounds.IntersectRay(ray, out float enterDist) && enterDist <= maxRayDist) return true;
            }

            return false;
        }

        private void FadeHierarchyWallsOnly(Transform wallGroup, float alphaTarget)
        {
            Renderer[] rens = wallGroup.GetComponentsInChildren<Renderer>();
            for (int r = 0; r < rens.Length; r++)
            {
                Renderer ren = rens[r];
                if (ShouldIgnoreRendererForOcclusion(ren)) continue;
                RegistrarRendererParaFade(ren, alphaTarget);
            }
        }

        private void RegistrarRendererParaFade(Renderer ren, float targetAlpha)
        {
            if (!activeFadedObjects.TryGetValue(ren, out FadedRendererData data))
            {
                Material[] orig = ren.sharedMaterials;
                if (orig == null || orig.Length == 0) return;
                for (int m = 0; m < orig.Length; m++)
                {
                    if (orig[m] == null) return;
                }

                Material[] inst = new Material[orig.Length];
                float[] baseAlphas = new float[orig.Length];
                for (int m = 0; m < orig.Length; m++)
                {
                    inst[m] = CrearInstanciaMaterialOclusion(orig[m], out baseAlphas[m], out bool usedDedicatedShader);
                    if (!usedDedicatedShader)
                    {
                        ConfigurarModoTransparenteMaterial(inst[m]);
                    }
                }
                ren.sharedMaterials = inst;

                data = new FadedRendererData
                {
                    renderer = ren,
                    originalMaterials = orig,
                    instancedMaterials = inst,
                    originalBaseAlphas = baseAlphas,
                    currentAlpha = 1.0f,
                    targetAlpha = targetAlpha,
                    isOccluding = true
                };

                activeFadedObjects.Add(ren, data);
            }
            else
            {
                data.isOccluding = true;
                data.targetAlpha = Mathf.Min(data.targetAlpha, targetAlpha);
            }
        }

        private Shader ResolveOcclusionShader()
        {
            if (occlusionFadeShader != null) return occlusionFadeShader;
            if (cachedOcclusionShader != null) return cachedOcclusionShader;

            Material carrier = Resources.Load<Material>("Materials/Mat_BuildingOcclusionFade_Carrier");
            if (carrier != null && carrier.shader != null)
            {
                cachedOcclusionShader = carrier.shader;
                return cachedOcclusionShader;
            }

            cachedOcclusionShader = Shader.Find("Prototipo3/BuildingOcclusionFade");
            return cachedOcclusionShader;
        }

        private Material CrearInstanciaMaterialOclusion(Material orig, out float baseAlpha, out bool usedDedicatedShader)
        {
            Color baseColor = orig.HasProperty("_BaseColor")
                ? orig.GetColor("_BaseColor")
                : (orig.HasProperty("_Color") ? orig.GetColor("_Color") : Color.white);

            bool wasTransparent = orig.HasProperty("_Surface") && orig.GetFloat("_Surface") >= 0.5f;
            baseAlpha = wasTransparent ? Mathf.Clamp01(baseColor.a) : 1.0f;

            Shader fadeShader = ResolveOcclusionShader();
            if (fadeShader != null)
            {
                Material mat = new Material(fadeShader)
                {
                    name = orig.name + "_OcclusionFade",
                    renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent
                };

                Texture mainTex = null;
                Vector2 texScale = Vector2.one;
                Vector2 texOffset = Vector2.zero;
                if (orig.HasProperty("_BaseMap"))
                {
                    mainTex = orig.GetTexture("_BaseMap");
                    texScale = orig.GetTextureScale("_BaseMap");
                    texOffset = orig.GetTextureOffset("_BaseMap");
                }
                else if (orig.HasProperty("_MainTex"))
                {
                    mainTex = orig.GetTexture("_MainTex");
                    texScale = orig.GetTextureScale("_MainTex");
                    texOffset = orig.GetTextureOffset("_MainTex");
                }

                if (mainTex != null)
                {
                    mat.SetTexture("_BaseMap", mainTex);
                }
                mat.SetTextureScale("_BaseMap", texScale);
                mat.SetTextureOffset("_BaseMap", texOffset);

                baseColor.a = baseAlpha;
                mat.SetColor("_BaseColor", baseColor);

                float cull = orig.HasProperty("_Cull") ? orig.GetFloat("_Cull") : 2f;
                mat.SetFloat("_Cull", cull);

                Texture bump = orig.HasProperty("_BumpMap") ? orig.GetTexture("_BumpMap") : null;
                if (bump != null)
                {
                    mat.SetTexture("_BumpMap", bump);
                    mat.EnableKeyword("_NORMALMAP");
                }
                else
                {
                    mat.DisableKeyword("_NORMALMAP");
                }

                Color emisCol = orig.HasProperty("_EmissionColor") ? orig.GetColor("_EmissionColor") : Color.black;
                Texture emisMap = orig.HasProperty("_EmissionMap") ? orig.GetTexture("_EmissionMap") : null;
                bool hasEmission = orig.IsKeywordEnabled("_EMISSION") || emisCol.maxColorComponent > 0.001f || emisMap != null;
                if (hasEmission)
                {
                    if (emisMap != null) mat.SetTexture("_EmissionMap", emisMap);
                    mat.SetColor("_EmissionColor", emisCol);
                    mat.EnableKeyword("_EMISSION");
                }
                else
                {
                    mat.SetColor("_EmissionColor", Color.black);
                    mat.DisableKeyword("_EMISSION");
                }

                float smoothness = orig.HasProperty("_Smoothness")
                    ? orig.GetFloat("_Smoothness")
                    : (orig.HasProperty("_Glossiness") ? orig.GetFloat("_Glossiness") : 0.25f);
                Color specCol = orig.HasProperty("_SpecColor") ? orig.GetColor("_SpecColor") : new Color(0.18f, 0.18f, 0.18f, 1f);
                specCol.a = Mathf.Clamp01(smoothness);
                mat.SetColor("_SpecColor", specCol);
                mat.SetFloat("_Smoothness", Mathf.Clamp01(smoothness));
                mat.EnableKeyword("_SPECULAR_COLOR");

                usedDedicatedShader = true;
                return mat;
            }

            usedDedicatedShader = false;
            return new Material(orig);
        }

        private static bool HasGroundSurfaceName(string objectName)
        {
            if (string.IsNullOrEmpty(objectName)) return false;
            return objectName.IndexOf("Floor", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                   objectName.IndexOf("Ground", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                   objectName.IndexOf("Road", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                   objectName.IndexOf("Pavement", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                   objectName.IndexOf("Court", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                   objectName.IndexOf("Duela", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                   objectName.IndexOf("Foundation", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool ShouldIgnoreRendererForOcclusion(Renderer ren)
        {
            if (ren == null || !ren.enabled) return true;
            if (ren is ParticleSystemRenderer || ren is SpriteRenderer || ren is BillboardRenderer || ren is LineRenderer) return true;
            string rName = ren.name;
            if (rName.Contains("VisionCone") || rName.Contains("DetectionRadius") || HasGroundSurfaceName(rName)) return true;
            if (ren.GetComponent<TMPro.TMP_Text>() != null || ren.GetComponent<TextMesh>() != null || ren.GetComponentInParent<Canvas>() != null) return true;
            if (ren.CompareTag("Interactable") || ren.CompareTag("Player") || ren.CompareTag("Enemy")) return true;
            if (ren.GetComponentInParent<CraftingTable>() != null || ren.GetComponentInParent<LootContainer>() != null) return true;
            if (ren.GetComponentInParent<EnemyAI>() != null || ren.GetComponentInParent<PuertaTrampa>() != null || ren.GetComponentInParent<LuluNPC>() != null) return true;
            return false;
        }

        private static void DestroyInstancedMaterials(FadedRendererData data)
        {
            if (data == null || data.instancedMaterials == null) return;
            for (int m = 0; m < data.instancedMaterials.Length; m++)
            {
                Material instMat = data.instancedMaterials[m];
                if (instMat == null) continue;

                bool isOriginal = false;
                if (data.originalMaterials != null)
                {
                    for (int j = 0; j < data.originalMaterials.Length; j++)
                    {
                        if (data.originalMaterials[j] == instMat)
                        {
                            isOriginal = true;
                            break;
                        }
                    }
                }
                if (!isOriginal)
                {
                    Destroy(instMat);
                }
            }
        }

        private void OnDisable()
        {
            foreach (var kvp in activeFadedObjects)
            {
                if (kvp.Key != null && kvp.Value != null && kvp.Value.originalMaterials != null)
                {
                    kvp.Key.sharedMaterials = kvp.Value.originalMaterials;
                }
                DestroyInstancedMaterials(kvp.Value);
            }
            activeFadedObjects.Clear();
        }

        private static void ConfigurarModoTransparenteMaterial(Material m)
        {
            if (m == null) return;

            // Respaldo si el shader dedicado de oclusión no está disponible
            if (m.HasProperty("_Surface"))
            {
                m.SetFloat("_Surface", 1f); // Transparent
                if (m.HasProperty("_Blend")) m.SetFloat("_Blend", 0f);
                m.SetOverrideTag("RenderType", "Transparent");
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                if (m.HasProperty("_SrcBlendAlpha")) m.SetInt("_SrcBlendAlpha", (int)UnityEngine.Rendering.BlendMode.One);
                if (m.HasProperty("_DstBlendAlpha")) m.SetInt("_DstBlendAlpha", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                m.SetInt("_ZWrite", 0);
                m.DisableKeyword("_ALPHATEST_ON");
                m.EnableKeyword("_ALPHABLEND_ON");
                m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }
            else if (m.HasProperty("_Mode"))
            {
                m.SetFloat("_Mode", 2f); // Fade legacy
                m.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                m.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                m.SetInt("_ZWrite", 0);
                m.DisableKeyword("_ALPHATEST_ON");
                m.EnableKeyword("_ALPHABLEND_ON");
                m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }
        }

        private static void AplicarAlpha(FadedRendererData data)
        {
            if (data == null || data.instancedMaterials == null) return;
            for (int i = 0; i < data.instancedMaterials.Length; i++)
            {
                Material m = data.instancedMaterials[i];
                if (m == null) continue;

                float baseAlpha = (data.originalBaseAlphas != null && i < data.originalBaseAlphas.Length)
                    ? data.originalBaseAlphas[i]
                    : 1.0f;
                float finalAlpha = Mathf.Clamp01(baseAlpha * data.currentAlpha);

                if (m.HasProperty("_BaseColor"))
                {
                    Color c = m.GetColor("_BaseColor");
                    c.a = finalAlpha;
                    m.SetColor("_BaseColor", c);
                }

                if (m.HasProperty("_Color"))
                {
                    Color c = m.GetColor("_Color");
                    c.a = finalAlpha;
                    m.SetColor("_Color", c);
                }
            }
        }
    }
}
