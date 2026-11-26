using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Prototipo3
{
    public class DirectionalNightFog : MonoBehaviour
    {
        private const int CurrentProfileVersion = 3;

        [Header("Referencias")]
        public Transform playerTransform;
        public Camera targetCamera;

        [Header("Shader de Smog Volumétrico")]
        public Shader fogShader;
        public Color dayFogColor = new Color(0.62f, 0.59f, 0.50f, 0.30f);
        public Color duskFogColor = new Color(0.78f, 0.45f, 0.25f, 0.40f);
        public Color nightFogColor = new Color(0.08f, 0.12f, 0.18f, 0.54f);
        public Color dayWispsTint = new Color(0.78f, 0.74f, 0.64f, 1.0f);
        public Color duskWispsTint = new Color(0.94f, 0.60f, 0.36f, 1.0f);
        public Color nightWispsTint = new Color(0.16f, 0.24f, 0.32f, 1.0f);

        [Range(0.001f, 0.12f)] public float fogDensity = 0.032f;
        [Range(0.0f, 3.0f)] public float directionalForwardBoost = 1.15f;
        [Tooltip("Radio despejado alrededor de Kilo dentro del smog de partículas (m).")]
        public float nearDistance = 4.5f;
        public float farDistance = 16.5f;
        [Range(0.01f, 0.25f)] public float noiseScale = 0.14f;
        [Range(0.0f, 2.0f)] public float noiseSpeed = 0.35f;
        [Range(0.05f, 0.95f)] public float maxSmogAlpha = 0.65f;
        public float fogTransitionSpeed = 1.8f;

        [Header("Sistema de Partículas Volumétrico")]
        [Range(60, 400)] public int maxCloudParticles = 220;
        [Range(5f, 80f)] public float dayEmissionRate = 26f;
        [Range(5f, 100f)] public float nightEmissionRate = 38f;
        public Vector2 cloudSizeRange = new Vector2(6.5f, 12.5f);
        public Vector2 cloudLifetimeRange = new Vector2(8.0f, 12.5f);
        public Vector3 emitterBoxSize = new Vector3(46f, 2.6f, 46f);
        public Vector3 windDriftVelocity = new Vector3(0.72f, 0.0f, 0.42f);
        [Range(0.2f, 4.0f)] public float softParticlesFade = 1.6f;

        [Header("Estratificación y Faroles Nocturnos")]
        public float streetFogHeight = 3.4f;
        [Range(0.05f, 1.0f)] public float heightFalloff = 0.26f;
        public Color lampWarmColor = new Color(1.0f, 0.78f, 0.40f, 1.0f);
        [Range(0.0f, 0.8f)] public float lampClearStrength = 0.30f;
        [Range(0.0f, 2.0f)] public float lampGlowStrength = 0.85f;

        [Header("Horizonte Global")]
        [Tooltip("Desactivado por defecto para evitar doble niebla con el sistema volumétrico.")]
        public bool controlGlobalFog = false;

        [SerializeField, HideInInspector] private int smogProfileVersion = 0;

        private Material fogMaterial;
        private Texture2D cloudPuffTexture;
        private GameObject fogEmitterObj;
        private ParticleSystem smogParticleSystem;
        private float currentNightIntensity = 0f;
        private float currentIndoorFade = 1f;
        private Color smoothedCoreColor;
        private Color smoothedWispsTint;
        private bool colorsInitialized = false;
        private Vector3 lastEmitterAnchorPos;
        private bool hasInitializedEmitterPos = false;
        private readonly Vector4[] activeLampBuffer = new Vector4[8];

        private void OnValidate()
        {
            UpgradeProfileIfNeeded();
        }

        private void Awake()
        {
            UpgradeProfileIfNeeded();
            ResolveVolumetricShader();
        }

        private void UpgradeProfileIfNeeded()
        {
            if (smogProfileVersion >= CurrentProfileVersion) return;

            smogProfileVersion = CurrentProfileVersion;
            dayFogColor = new Color(0.62f, 0.59f, 0.50f, 0.30f);
            duskFogColor = new Color(0.78f, 0.45f, 0.25f, 0.40f);
            nightFogColor = new Color(0.08f, 0.12f, 0.18f, 0.54f);
            dayWispsTint = new Color(0.78f, 0.74f, 0.64f, 1.0f);
            duskWispsTint = new Color(0.94f, 0.60f, 0.36f, 1.0f);
            nightWispsTint = new Color(0.16f, 0.24f, 0.32f, 1.0f);
            fogDensity = 0.032f;
            directionalForwardBoost = 1.15f;
            nearDistance = 4.5f;
            farDistance = 16.5f;
            noiseScale = 0.14f;
            noiseSpeed = 0.35f;
            maxSmogAlpha = 0.65f;
            maxCloudParticles = 220;
            dayEmissionRate = 26f;
            nightEmissionRate = 38f;
            cloudSizeRange = new Vector2(6.5f, 12.5f);
            cloudLifetimeRange = new Vector2(8.0f, 12.5f);
            emitterBoxSize = new Vector3(46f, 2.6f, 46f);
            windDriftVelocity = new Vector3(0.72f, 0.0f, 0.42f);
            softParticlesFade = 1.6f;
            streetFogHeight = 3.4f;
            heightFalloff = 0.26f;
            lampWarmColor = new Color(1.0f, 0.78f, 0.40f, 1.0f);
            lampClearStrength = 0.30f;
            lampGlowStrength = 0.85f;
        }

        private void ResolveVolumetricShader()
        {
            if (fogShader != null && fogShader.name == "Prototipo3/VolumetricSmogParticle")
            {
                return;
            }

            Material carrier = Resources.Load<Material>("Materials/Mat_VolumetricSmog_Carrier");
            if (carrier != null && carrier.shader != null)
            {
                fogShader = carrier.shader;
                return;
            }

            fogShader = Shader.Find("Prototipo3/VolumetricSmogParticle");
        }

        private void Start()
        {
            ResolvePlayerAndCamera();

            // Limpiar el quad heredado del sistema anterior si existía en la cámara
            if (targetCamera != null)
            {
                Transform legacyQuad = targetCamera.transform.Find("ScreenDirectionalFogQuad");
                if (legacyQuad != null) Destroy(legacyQuad.gameObject);
            }

            CrearSistemaParticulasVolumetrico();

            RenderSettings.fog = controlGlobalFog;
            if (controlGlobalFog)
            {
                RenderSettings.fogMode = FogMode.ExponentialSquared;
                RenderSettings.fogDensity = 0.004f;
                RenderSettings.fogColor = dayFogColor;
            }
        }

        private void ResolvePlayerAndCamera()
        {
            if (playerTransform == null || !playerTransform.gameObject.activeInHierarchy)
            {
                HamsterController hc = FindAnyObjectByType<HamsterController>();
                if (hc != null) playerTransform = hc.transform;
            }
            if (targetCamera == null)
            {
                targetCamera = GetComponent<Camera>() ?? Camera.main;
            }

            if (targetCamera != null)
            {
                UniversalAdditionalCameraData urpCam = targetCamera.GetComponent<UniversalAdditionalCameraData>();
                if (urpCam != null)
                {
                    if (urpCam.requiresDepthOption != CameraOverrideOption.On)
                    {
                        urpCam.requiresDepthOption = CameraOverrideOption.On;
                    }
                    urpCam.requiresDepthTexture = true;
                }
            }
        }

        private void CrearSistemaParticulasVolumetrico()
        {
            ResolveVolumetricShader();
            if (fogShader == null)
            {
                Debug.LogWarning("[DirectionalNightFog] Shader 'Prototipo3/VolumetricSmogParticle' no encontrado.");
                return;
            }

            if (cloudPuffTexture == null)
            {
                cloudPuffTexture = GenerarTexturaNubeVolumetrica(128);
            }

            if (fogMaterial == null)
            {
                fogMaterial = new Material(fogShader);
                fogMaterial.name = "Mat_VolumetricSmogParticle_Instance";
            }
            fogMaterial.SetTexture("_MainTex", cloudPuffTexture);
            fogMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + 200;

            if (fogEmitterObj != null) Destroy(fogEmitterObj);

            fogEmitterObj = new GameObject("VolumetricSmogParticleSystem");
            int ignoreLayer = LayerMask.NameToLayer("Ignore Raycast");
            fogEmitterObj.layer = ignoreLayer >= 0 ? ignoreLayer : 2;

            float groundY = CityGeneratorPrototipo3.Instance != null ? CityGeneratorPrototipo3.Instance.roadHeightY : 0f;
            Vector3 initialAnchor = playerTransform != null ? playerTransform.position : Vector3.zero;
            Vector3 emitterStartPos = new Vector3(initialAnchor.x, groundY + Mathf.Max(1.0f, streetFogHeight * 0.40f), initialAnchor.z);
            fogEmitterObj.transform.position = emitterStartPos;
            lastEmitterAnchorPos = emitterStartPos;
            hasInitializedEmitterPos = playerTransform != null;

            smogParticleSystem = fogEmitterObj.AddComponent<ParticleSystem>();
            smogParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = smogParticleSystem.main;
            main.playOnAwake = false;
            main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = Mathf.Clamp(maxCloudParticles, 40, 500);
            main.startLifetime = new ParticleSystem.MinMaxCurve(Mathf.Max(2f, cloudLifetimeRange.x), Mathf.Max(3f, cloudLifetimeRange.y));
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.18f, 0.55f);
            main.startSize = new ParticleSystem.MinMaxCurve(Mathf.Max(2f, cloudSizeRange.x), Mathf.Max(3f, cloudSizeRange.y));
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = Color.white;
            main.gravityModifier = 0f;

            var emission = smogParticleSystem.emission;
            emission.enabled = true;
            emission.rateOverTime = dayEmissionRate;

            var shape = smogParticleSystem.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = emitterBoxSize;

            var velocity = smogParticleSystem.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.x = new ParticleSystem.MinMaxCurve(windDriftVelocity.x * 0.7f, windDriftVelocity.x * 1.3f);
            velocity.y = new ParticleSystem.MinMaxCurve(-0.04f, 0.05f);
            velocity.z = new ParticleSystem.MinMaxCurve(windDriftVelocity.z * 0.7f, windDriftVelocity.z * 1.3f);

            var colorLife = smogParticleSystem.colorOverLifetime;
            colorLife.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[]
                {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, 0.22f),
                    new GradientAlphaKey(1f, 0.76f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorLife.color = new ParticleSystem.MinMaxGradient(grad);

            var sizeLife = smogParticleSystem.sizeOverLifetime;
            sizeLife.enabled = true;
            sizeLife.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.82f, 1f, 1.18f));

            var rotLife = smogParticleSystem.rotationOverLifetime;
            rotLife.enabled = true;
            rotLife.z = new ParticleSystem.MinMaxCurve(-0.22f, 0.22f);

            var noise = smogParticleSystem.noise;
            noise.enabled = true;
            noise.strength = 0.42f;
            noise.frequency = 0.18f;
            noise.scrollSpeed = 0.14f;
            noise.damping = true;
            noise.octaveCount = 2;

            ParticleSystemRenderer psRenderer = fogEmitterObj.GetComponent<ParticleSystemRenderer>();
            if (psRenderer != null)
            {
                psRenderer.renderMode = ParticleSystemRenderMode.Billboard;
                psRenderer.sortMode = ParticleSystemSortMode.Distance;
                psRenderer.maxParticleSize = 1.5f;
                psRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                psRenderer.receiveShadows = false;
                psRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                psRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
                psRenderer.sharedMaterial = fogMaterial;
            }

            // Precalentar para que las calles tengan nubes volumétricas desde el primer fotograma
            smogParticleSystem.Simulate(10f, true, true);
            smogParticleSystem.Play();
        }

        private static Texture2D GenerarTexturaNubeVolumetrica(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "Tex_VolumetricSmogPuff",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };

            Color[] pixels = new Color[size * size];
            float invHalf = 2f / size;

            for (int y = 0; y < size; y++)
            {
                float ny = y * invHalf - 1f;
                for (int x = 0; x < size; x++)
                {
                    float nx = x * invHalf - 1f;
                    float rSq = nx * nx + ny * ny;

                    if (rSq >= 0.94f)
                    {
                        pixels[y * size + x] = new Color(1f, 1f, 1f, 0f);
                        continue;
                    }

                    // Perfil radial suave (1 - r^2)^2 + cúmulos multi-lóbulo con Perlin noise
                    float radial = 1f - (rSq / 0.94f);
                    float radialSmooth = radial * radial;

                    float p1 = Mathf.PerlinNoise(nx * 2.1f + 4.3f, ny * 2.1f + 7.1f);
                    float p2 = Mathf.PerlinNoise(nx * 4.4f + 13.7f, ny * 4.4f + 2.9f);
                    float p3 = Mathf.PerlinNoise(nx * 8.2f + 21.1f, ny * 8.2f + 19.4f);
                    float fbm = p1 * 0.58f + p2 * 0.28f + p3 * 0.14f;

                    float density = Mathf.Clamp01(radialSmooth * Mathf.Lerp(0.45f, 1.35f, fbm));
                    pixels[y * size + x] = new Color(1f, 1f, 1f, density);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply(false, false);
            return tex;
        }

        private void Update()
        {
            ResolvePlayerAndCamera();
            if (playerTransform == null) return;

            if (smogParticleSystem == null)
            {
                CrearSistemaParticulasVolumetrico();
            }

            EvaluateSolarAtmosphere(out Color targetCoreColor, out Color targetWispTint, out float targetNight);

            if (!colorsInitialized)
            {
                smoothedCoreColor = targetCoreColor;
                smoothedWispsTint = targetWispTint;
                currentNightIntensity = targetNight;
                colorsInitialized = true;
            }
            else
            {
                float step = Time.deltaTime * Mathf.Max(0.4f, fogTransitionSpeed);
                currentNightIntensity = Mathf.MoveTowards(currentNightIntensity, targetNight, Time.deltaTime * (fogTransitionSpeed * 0.5f));
                smoothedCoreColor = Color.Lerp(smoothedCoreColor, targetCoreColor, Mathf.Clamp01(step));
                smoothedWispsTint = Color.Lerp(smoothedWispsTint, targetWispTint, Mathf.Clamp01(step));
            }

            bool insideRefuge = SafeRefugeZone.ActiveZone != null && SafeRefugeZone.IsPointOrPlayerInRefuge(playerTransform.position);
            float targetIndoorFade = insideRefuge ? 0.12f : 1.0f;
            currentIndoorFade = Mathf.MoveTowards(currentIndoorFade, targetIndoorFade, Time.deltaTime * 2.2f);

            float groundY = CityGeneratorPrototipo3.Instance != null ? CityGeneratorPrototipo3.Instance.roadHeightY : 0f;
            Vector3 desiredEmitterPos = new Vector3(
                playerTransform.position.x,
                groundY + Mathf.Max(1.0f, streetFogHeight * 0.40f),
                playerTransform.position.z);

            if (fogEmitterObj != null)
            {
                fogEmitterObj.transform.position = desiredEmitterPos;
                if (hasInitializedEmitterPos &&
                    Vector3.SqrMagnitude(desiredEmitterPos - lastEmitterAnchorPos) > 35f * 35f &&
                    smogParticleSystem != null)
                {
                    smogParticleSystem.Clear();
                    smogParticleSystem.Simulate(8f, true, true);
                    smogParticleSystem.Play();
                }
                lastEmitterAnchorPos = desiredEmitterPos;
                hasInitializedEmitterPos = true;
            }

            if (smogParticleSystem != null)
            {
                var emission = smogParticleSystem.emission;
                emission.rateOverTime = Mathf.Lerp(dayEmissionRate, nightEmissionRate, currentNightIntensity) * currentIndoorFade;
            }

            float effectiveClearRadius = Mathf.Lerp(9.5f, Mathf.Max(3.0f, nearDistance), currentIndoorFade);

            if (fogMaterial != null)
            {
                fogMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + 200;
                fogMaterial.SetVector("_PlayerPosition", playerTransform.position);
                fogMaterial.SetColor("_FogColor", smoothedCoreColor);
                fogMaterial.SetColor("_HorizonTint", smoothedWispsTint);
                fogMaterial.SetFloat("_SoftParticlesFade", softParticlesFade);
                fogMaterial.SetFloat("_ClearRadius", effectiveClearRadius);
                fogMaterial.SetFloat("_NoiseScale", noiseScale);
                fogMaterial.SetFloat("_NoiseSpeed", noiseSpeed);
                fogMaterial.SetFloat("_NightIntensity", currentNightIntensity);
                fogMaterial.SetFloat("_IndoorFade", currentIndoorFade);
                fogMaterial.SetFloat("_MaxAlpha", maxSmogAlpha);
                fogMaterial.SetColor("_LampWarmColor", lampWarmColor);
                fogMaterial.SetFloat("_LampClearStrength", lampClearStrength);
                fogMaterial.SetFloat("_LampGlowStrength", lampGlowStrength);

                int lampCount = 0;
                if (currentNightIntensity > 0.01f && CityStreetLampManagerPrototipo3.Instance != null)
                {
                    lampCount = CityStreetLampManagerPrototipo3.Instance.GetClosestActiveLamps(
                        playerTransform.position,
                        groundY,
                        activeLampBuffer,
                        8);
                }
                else
                {
                    for (int i = 0; i < activeLampBuffer.Length; i++) activeLampBuffer[i] = Vector4.zero;
                }

                fogMaterial.SetInt("_ActiveLampCount", lampCount);
                fogMaterial.SetVectorArray("_ActiveLamps", activeLampBuffer);
            }

            if (controlGlobalFog)
            {
                RenderSettings.fog = true;
                float targetGlobalDensity = currentNightIntensity > 0.5f ? 0.025f : 0.004f;
                RenderSettings.fogDensity = Mathf.MoveTowards(RenderSettings.fogDensity, targetGlobalDensity, Time.deltaTime * 0.01f);
                RenderSettings.fogColor = smoothedCoreColor;
            }
            else if (RenderSettings.fog)
            {
                RenderSettings.fog = false;
            }
        }

        private void EvaluateSolarAtmosphere(out Color coreColor, out Color wispTint, out float nightFactor)
        {
            if (DeterminarSiEsNocheForzadaTutorial())
            {
                coreColor = nightFogColor;
                wispTint = nightWispsTint;
                nightFactor = 1f;
                return;
            }

            TimeManager tm = TimeManager.Instance;
            if (tm == null)
            {
                coreColor = dayFogColor;
                wispTint = dayWispsTint;
                nightFactor = 0f;
                return;
            }

            float hour = tm.CurrentHourFloat;

            // Día completo: 06:30 -> 16:30
            if (hour >= 6.5f && hour < 16.5f)
            {
                coreColor = dayFogColor;
                wispTint = dayWispsTint;
                nightFactor = 0f;
                return;
            }

            // Atardecer cobrizo: 16:30 -> 18:00
            if (hour >= 16.5f && hour < 18.0f)
            {
                if (hour < 17.4f)
                {
                    float t = (hour - 16.5f) / 0.9f;
                    coreColor = Color.Lerp(dayFogColor, duskFogColor, t);
                    wispTint = Color.Lerp(dayWispsTint, duskWispsTint, t);
                    nightFactor = Mathf.Lerp(0f, 0.45f, t);
                }
                else
                {
                    float t = (hour - 17.4f) / 0.6f;
                    coreColor = Color.Lerp(duskFogColor, nightFogColor, t);
                    wispTint = Color.Lerp(duskWispsTint, nightWispsTint, t);
                    nightFactor = Mathf.Lerp(0.45f, 1f, t);
                }
                return;
            }

            // Amanecer cobrizo: 05:30 -> 06:30
            if (hour >= 5.3f && hour < 6.5f)
            {
                if (hour < 5.9f)
                {
                    float t = (hour - 5.3f) / 0.6f;
                    coreColor = Color.Lerp(nightFogColor, duskFogColor, t);
                    wispTint = Color.Lerp(nightWispsTint, duskWispsTint, t);
                    nightFactor = Mathf.Lerp(1f, 0.45f, t);
                }
                else
                {
                    float t = (hour - 5.9f) / 0.6f;
                    coreColor = Color.Lerp(duskFogColor, dayFogColor, t);
                    wispTint = Color.Lerp(duskWispsTint, dayWispsTint, t);
                    nightFactor = Mathf.Lerp(0.45f, 0f, t);
                }
                return;
            }

            // Noche Feral: 18:00 -> 05:30
            coreColor = nightFogColor;
            wispTint = nightWispsTint;
            nightFactor = 1f;
        }

        private static bool DeterminarSiEsNocheForzadaTutorial()
        {
            if (TutorialPipelineManager.Instance != null)
            {
                var st = TutorialPipelineManager.Instance.currentState;
                if (st == TutorialPipelineManager.TutorialState.CinematicaAparicionFeral ||
                    st == TutorialPipelineManager.TutorialState.EscapeDash ||
                    st == TutorialPipelineManager.TutorialState.SigiloEsquina ||
                    st == TutorialPipelineManager.TutorialState.LlegadaRefugioAmanecer)
                {
                    return true;
                }
            }
            return false;
        }

        private void OnDestroy()
        {
            if (fogEmitterObj != null) Destroy(fogEmitterObj);
            if (fogMaterial != null) Destroy(fogMaterial);
            if (cloudPuffTexture != null) Destroy(cloudPuffTexture);
        }
    }
}
