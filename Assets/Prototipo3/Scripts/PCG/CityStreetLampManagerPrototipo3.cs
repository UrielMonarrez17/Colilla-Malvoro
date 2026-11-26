using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Prototipo3
{
    /// <summary>
    /// Gestiona todos los faroles urbanos (Pole -> Pole_light) generados en la ciudad de Prototipo 3:
    /// - Asegura un SpotLight cálido práctico justo debajo del cabezal real (Renderer.bounds) de cada Pole_light.
    /// - Enciende las luminarias automáticamente al caer la tarde/noche y las apaga al amanecer.
    /// - Simula parpadeos eléctricos ocasionales y realistas (balastro de vapor de sodio) sincronizando el cono de luz y el bulbo emisivo.
    /// </summary>
    public class CityStreetLampManagerPrototipo3 : MonoBehaviour
    {
        public static CityStreetLampManagerPrototipo3 Instance { get; private set; }

        [Header("Parámetros de Iluminación Nocturna")]
        public Color lampLightColor = new Color(1.0f, 0.80f, 0.46f, 1f);
        public float baseSpotIntensity = 22f;
        public float spotRange = 15.5f;
        public float outerSpotAngle = 104f;
        public float innerSpotAngle = 52f;
        [Tooltip("Distancia máxima desde el jugador/cámara para mantener activo el componente SpotLight en GPU.")]
        public float activeLightRadius = 56f;

        private struct LampNode
        {
            public Transform poleLightTransform;
            public Renderer bulbRenderer;
            public Light spotLight;
            public Vector3 worldHeadPos;
            public float duskTurnOnHour;     // Pequeño desfase 17.65..18.00 para encendido escalonado realista al atardecer
            public float humPhase;
            public bool isWornBallast;       // ~28% de los postes tienen balastro viejo y parpadean más seguido
            public float nextFlickerTime;
            public float flickerStartTime;
            public float flickerDuration;
            public byte flickerPattern;      // 0: doble parpadeo rápido, 1: chisporroteo, 2: micro-apagón
            public byte lastMatState;        // 0: apagado, 1: tenue, 2: encendido brillante
        }

        private readonly List<LampNode> lamps = new List<LampNode>(256);
        private Material originalDayBulbMat;
        private Material sharedLitBulbMat;
        private Material sharedDimBulbMat;
        private Transform trackedFocus;

        public static void ConfigureCityLamps(Transform cityRoot)
        {
            if (cityRoot == null) return;
            CityStreetLampManagerPrototipo3 mgr = cityRoot.GetComponent<CityStreetLampManagerPrototipo3>();
            if (mgr == null)
            {
                mgr = cityRoot.gameObject.AddComponent<CityStreetLampManagerPrototipo3>();
            }
            mgr.RebuildLampRegistry(cityRoot);
        }

        private void Awake()
        {
            Instance = this;
            EnsureSharedBulbMaterials();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (sharedLitBulbMat != null) Destroy(sharedLitBulbMat);
            if (sharedDimBulbMat != null) Destroy(sharedDimBulbMat);
        }

        private void EnsureSharedBulbMaterials()
        {
            if (sharedLitBulbMat != null && sharedDimBulbMat != null) return;

            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            sharedLitBulbMat = new Material(urpLit) { name = "M_Proto3_StreetLamp_Lit" };
            Color warmBase = new Color(1.0f, 0.92f, 0.72f, 1f);
            Color warmEmis = new Color(1.0f, 0.78f, 0.38f, 1f) * 3.4f;
            sharedLitBulbMat.color = warmBase;
            if (sharedLitBulbMat.HasProperty("_BaseColor")) sharedLitBulbMat.SetColor("_BaseColor", warmBase);
            sharedLitBulbMat.EnableKeyword("_EMISSION");
            if (sharedLitBulbMat.HasProperty("_EmissionColor")) sharedLitBulbMat.SetColor("_EmissionColor", warmEmis);

            sharedDimBulbMat = new Material(urpLit) { name = "M_Proto3_StreetLamp_DimFlicker" };
            Color dimBase = new Color(0.55f, 0.44f, 0.28f, 1f);
            Color dimEmis = new Color(0.85f, 0.52f, 0.18f, 1f) * 0.55f;
            sharedDimBulbMat.color = dimBase;
            if (sharedDimBulbMat.HasProperty("_BaseColor")) sharedDimBulbMat.SetColor("_BaseColor", dimBase);
            sharedDimBulbMat.EnableKeyword("_EMISSION");
            if (sharedDimBulbMat.HasProperty("_EmissionColor")) sharedDimBulbMat.SetColor("_EmissionColor", dimEmis);
        }

        public void RebuildLampRegistry(Transform cityRoot)
        {
            EnsureSharedBulbMaterials();
            lamps.Clear();

            Transform[] allTransforms = cityRoot.GetComponentsInChildren<Transform>(true);
            float now = Time.time;

            for (int i = 0; i < allTransforms.Length; i++)
            {
                Transform tr = allTransforms[i];
                if (tr == null) continue;

                // En los prefabs de calles, la luminaria del poste se llama "Pole_light" o "Pole_light (1)"
                if (!tr.name.StartsWith("Pole_light", StringComparison.OrdinalIgnoreCase)) continue;
                // No procesar el propio objeto hijo "Pole_SpotLight"
                if (tr.GetComponent<Light>() != null && tr.GetComponent<Renderer>() == null) continue;

                Renderer bulbRen = tr.GetComponent<Renderer>();
                if (bulbRen == null) continue;

                if (originalDayBulbMat == null && bulbRen.sharedMaterial != null)
                {
                    originalDayBulbMat = bulbRen.sharedMaterial;
                }

                Light spot = EnsureSpotLightOnPoleLight(tr, bulbRen, lampLightColor, baseSpotIntensity, spotRange, outerSpotAngle, innerSpotAngle, out Vector3 headWorldPos);
                if (spot == null) continue;

                // Hash determinista por posición para variedad natural entre faroles
                int hash = Mathf.Abs((Mathf.RoundToInt(headWorldPos.x * 13f) * 73856093) ^ (Mathf.RoundToInt(headWorldPos.z * 13f) * 19349663));
                float u01 = (hash % 1000) / 1000f;
                bool worn = (hash % 100) < 28; // ~28% parpadean con mayor frecuencia

                LampNode node = new LampNode
                {
                    poleLightTransform = tr,
                    bulbRenderer = bulbRen,
                    spotLight = spot,
                    worldHeadPos = headWorldPos,
                    duskTurnOnHour = Mathf.Lerp(17.68f, 17.98f, u01),
                    humPhase = u01 * Mathf.PI * 2f,
                    isWornBallast = worn,
                    nextFlickerTime = now + (worn ? Mathf.Lerp(2.5f, 14f, u01) : Mathf.Lerp(14f, 42f, u01)),
                    flickerStartTime = -10f,
                    flickerDuration = 0f,
                    flickerPattern = (byte)(hash % 3),
                    lastMatState = 255
                };

                spot.enabled = false;
                lamps.Add(node);
            }

            // Aplicar estado inmediato según la hora actual
            TickLamps(forceImmediateRefresh: true);
        }

        /// <summary>
        /// Crea o actualiza el objeto hijo "Pole_SpotLight" dentro de "Pole_light", posicionándolo exactamente
        /// bajo la malla real del cabezal (Renderer.bounds) e inclinándolo ligeramente hacia el carril de la calle.
        /// Funciona tanto en Editor (creación de Prefabs) como en Runtime.
        /// </summary>
        public static Light EnsureSpotLightOnPoleLight(
            Transform poleLightTr,
            Renderer poleLightRenderer,
            Color lightColor,
            float intensity,
            float range,
            float outerAngle,
            float innerAngle,
            out Vector3 headWorldPos)
        {
            Bounds lb = poleLightRenderer.bounds;
            headWorldPos = new Vector3(lb.center.x, lb.min.y - 0.05f, lb.center.z);

            // Calcular dirección horizontal desde la base del poste hacia el cabezal colgante (apunta hacia el centro de la calle)
            Vector3 outwardDir = Vector3.zero;
            if (poleLightTr.parent != null)
            {
                Transform poleBase = poleLightTr.parent.Find("Pole_base");
                if (poleBase != null && poleBase.TryGetComponent<Renderer>(out Renderer baseRen))
                {
                    Vector3 bCenter = baseRen.bounds.center;
                    outwardDir = new Vector3(lb.center.x - bCenter.x, 0f, lb.center.z - bCenter.z);
                }
                else
                {
                    outwardDir = new Vector3(lb.center.x - poleLightTr.parent.position.x, 0f, lb.center.z - poleLightTr.parent.position.z);
                }
            }

            Vector3 horizOut = outwardDir.sqrMagnitude > 0.001f ? outwardDir.normalized : poleLightTr.forward;
            headWorldPos += horizOut * 0.14f;

            Vector3 aimDir = (Vector3.down * 4.4f + horizOut * 0.95f).normalized;
            Quaternion desiredWorldRot = Quaternion.LookRotation(aimDir, Vector3.up);

            Transform spotTr = poleLightTr.Find("Pole_SpotLight");
            GameObject spotGo;
            if (spotTr == null)
            {
                spotGo = new GameObject("Pole_SpotLight");
                spotGo.transform.SetParent(poleLightTr, true);
            }
            else
            {
                spotGo = spotTr.gameObject;
            }

            spotGo.transform.position = headWorldPos;
            spotGo.transform.rotation = desiredWorldRot;

            Light sl = spotGo.GetComponent<Light>();
            if (sl == null) sl = spotGo.AddComponent<Light>();

            sl.type = LightType.Spot;
            sl.color = lightColor;
            sl.intensity = intensity;
            sl.range = range;
            sl.spotAngle = outerAngle;
            sl.innerSpotAngle = innerAngle;
            sl.shadows = LightShadows.None;
            sl.renderMode = LightRenderMode.ForcePixel;

            return sl;
        }

        private void Update()
        {
            if (lamps.Count == 0) return;
            TickLamps(forceImmediateRefresh: false);
        }

        private void TickLamps(bool forceImmediateRefresh)
        {
            TimeManager tm = TimeManager.Instance;
            float hour = tm != null ? tm.CurrentHourFloat : 12f;
            bool isNightCycle = tm != null && tm.CurrentCycleState == TimeManager.CycleState.Night;

            if (trackedFocus == null || !trackedFocus.gameObject.activeInHierarchy)
            {
                HamsterController hc = FindAnyObjectByType<HamsterControllerPrototipo3>() ?? FindAnyObjectByType<HamsterController>();
                if (hc != null) trackedFocus = hc.transform;
                else if (Camera.main != null) trackedFocus = Camera.main.transform;
            }

            Vector3 focusPos = trackedFocus != null ? trackedFocus.position : Vector3.zero;
            bool hasFocus = trackedFocus != null;
            float maxDistSq = activeLightRadius * activeLightRadius;
            float now = Time.time;

            for (int i = 0; i < lamps.Count; i++)
            {
                LampNode node = lamps[i];
                if (node.spotLight == null || node.bulbRenderer == null) continue;

                // Encendido en la noche (18:00 a 06:00) o durante los últimos instantes del crepúsculo según el umbral de cada poste
                bool shouldBeLit = isNightCycle || hour >= node.duskTurnOnHour || hour < 6.0f;
                // Si aún no hemos hablado con Marco (Fase 0 fuerza mañana), mantener apagados de día
                if (!StoryDirectorPrototipo3.AllowFeralGlobally && hour >= 6.0f && hour < 17.5f)
                {
                    shouldBeLit = false;
                }

                if (!shouldBeLit)
                {
                    if (node.spotLight.enabled) node.spotLight.enabled = false;
                    if (node.lastMatState != 0 || forceImmediateRefresh)
                    {
                        if (originalDayBulbMat != null) node.bulbRenderer.sharedMaterial = originalDayBulbMat;
                        node.lastMatState = 0;
                    }
                    lamps[i] = node;
                    continue;
                }

                // Evaluar si toca iniciar una ráfaga corta de parpadeo eléctrico ocasional
                if (now >= node.nextFlickerTime)
                {
                    node.flickerStartTime = now;
                    node.flickerPattern = (byte)UnityEngine.Random.Range(0, 3);
                    node.flickerDuration = node.flickerPattern == 1
                        ? UnityEngine.Random.Range(0.32f, 0.68f)
                        : UnityEngine.Random.Range(0.18f, 0.36f);

                    node.nextFlickerTime = now + (node.isWornBallast
                        ? UnityEngine.Random.Range(7.5f, 20.0f)
                        : UnityEngine.Random.Range(22.0f, 52.0f));
                }

                float flickerMul = 1f;
                float elapsedFlicker = now - node.flickerStartTime;
                if (elapsedFlicker >= 0f && elapsedFlicker < node.flickerDuration)
                {
                    float norm = elapsedFlicker / Mathf.Max(0.05f, node.flickerDuration);
                    switch (node.flickerPattern)
                    {
                        case 0:
                            // Doble guiño clásico de farol urbano (baja -> vuelve -> micro-corte -> recupera)
                            if (norm < 0.24f) flickerMul = 0.12f;
                            else if (norm < 0.48f) flickerMul = 1.05f;
                            else if (norm < 0.70f) flickerMul = 0.0f;
                            else flickerMul = Mathf.Lerp(0.35f, 1.0f, (norm - 0.70f) / 0.30f);
                            break;

                        case 1:
                            // Chisporroteo irregular de balastro de sodio
                            float n = Mathf.PerlinNoise(now * 34f, node.humPhase * 5.7f);
                            flickerMul = n < 0.34f ? 0.04f : (n < 0.58f ? 0.38f : 0.96f);
                            break;

                        default:
                            // Micro-caída de tensión y reencendido cálido
                            flickerMul = norm < 0.42f ? 0.0f : Mathf.SmoothStep(0.18f, 1.0f, (norm - 0.42f) / 0.58f);
                            break;
                    }
                }
                else
                {
                    // Oscilación sutil de corriente alterna (~2%) cuando está estable
                    flickerMul = 0.985f + 0.025f * Mathf.Sin(now * 6.5f + node.humPhase);
                }

                // Sincronizar material emisivo del cabezal Pole_light con el parpadeo
                byte desiredMatState = flickerMul <= 0.08f ? (byte)0 : (flickerMul < 0.55f ? (byte)1 : (byte)2);
                if (node.lastMatState != desiredMatState || forceImmediateRefresh)
                {
                    if (desiredMatState == 2 && sharedLitBulbMat != null)
                        node.bulbRenderer.sharedMaterial = sharedLitBulbMat;
                    else if (desiredMatState == 1 && sharedDimBulbMat != null)
                        node.bulbRenderer.sharedMaterial = sharedDimBulbMat;
                    else if (originalDayBulbMat != null)
                        node.bulbRenderer.sharedMaterial = originalDayBulbMat;

                    node.lastMatState = desiredMatState;
                }

                // Activar SpotLight práctico dentro del radio visible alrededor del jugador/cámara
                float dx = node.worldHeadPos.x - focusPos.x;
                float dz = node.worldHeadPos.z - focusPos.z;
                bool inRange = !hasFocus || (dx * dx + dz * dz) <= maxDistSq;

                if (inRange && flickerMul > 0.02f)
                {
                    if (!node.spotLight.enabled) node.spotLight.enabled = true;
                    node.spotLight.intensity = baseSpotIntensity * flickerMul;
                }
                else
                {
                    if (node.spotLight.enabled) node.spotLight.enabled = false;
                }

                lamps[i] = node;
            }
        }
    }
}
