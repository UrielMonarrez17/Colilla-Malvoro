using UnityEngine;

namespace Prototipo2
{
    public class DirectionalNightFog : MonoBehaviour
    {
        [Header("Referencias")]
        public Transform playerTransform;
        public Camera targetCamera;

        [Header("Shader de Smog Atmosférico")]
        public Shader fogShader;
        public Color dayFogColor = new Color(0.70f, 0.76f, 0.82f, 0.24f);
        public Color nightFogColor = new Color(0.06f, 0.09f, 0.14f, 0.82f);
        [Range(0.001f, 0.1f)] public float fogDensity = 0.022f;
        [Range(0.0f, 3.0f)] public float directionalForwardBoost = 1.25f;
        public float nearDistance = 11.5f;
        public float farDistance = 32.0f;
        [Range(0.01f, 0.25f)] public float noiseScale = 0.065f;
        [Range(0.0f, 2.0f)] public float noiseSpeed = 0.35f;
        [Range(0.1f, 0.95f)] public float maxSmogAlpha = 0.80f;
        public float fogTransitionSpeed = 1.8f;

        [Header("Horizonte Global")]
        [Tooltip("Desactivado por defecto para evitar doble niebla con el shader URP.")]
        public bool controlGlobalFog = false;

        private Material fogMaterial;
        private GameObject fogQuadObj;
        private float currentNightIntensity = 0f;

        private void Awake()
        {
            if (fogShader == null)
            {
                fogShader = Shader.Find("Prototipo2/CityDirectionalFog") ?? Shader.Find("Prototipo3/CityDirectionalFog");
            }
        }

        private void Start()
        {
            ResolvePlayerAndCamera();
            CrearFrustumFogQuad();
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
                targetCamera = Camera.main;
            }
        }

        private void CrearFrustumFogQuad()
        {
            if (targetCamera == null) return;
            if (fogShader == null)
            {
                fogShader = Shader.Find("Prototipo2/CityDirectionalFog") ?? Shader.Find("Prototipo3/CityDirectionalFog");
                if (fogShader == null)
                {
                    Debug.LogWarning("[DirectionalNightFog] Shader 'Prototipo2/CityDirectionalFog' no encontrado.");
                    return;
                }
            }

            if (fogMaterial == null)
            {
                fogMaterial = new Material(fogShader);
                fogMaterial.name = "Mat_CityDirectionalFog_Instance";
            }
            fogMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + 400;

            Transform existing = targetCamera.transform.Find("ScreenDirectionalFogQuad");
            if (existing != null) Destroy(existing.gameObject);

            fogQuadObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            fogQuadObj.name = "ScreenDirectionalFogQuad";
            int ignoreLayer = LayerMask.NameToLayer("Ignore Raycast");
            fogQuadObj.layer = ignoreLayer >= 0 ? ignoreLayer : 2;
            fogQuadObj.transform.SetParent(targetCamera.transform, false);

            Collider col = fogQuadObj.GetComponent<Collider>();
            if (col != null) Destroy(col);

            float distanceInFront = targetCamera.nearClipPlane + 0.15f;
            fogQuadObj.transform.localPosition = new Vector3(0, 0, distanceInFront);
            fogQuadObj.transform.localRotation = Quaternion.identity;
            ActualizarEscalaQuad(distanceInFront);

            MeshRenderer mr = fogQuadObj.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sharedMaterial = fogMaterial;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
                mr.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
                mr.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            }
        }

        private void ActualizarEscalaQuad(float distance)
        {
            if (fogQuadObj == null || targetCamera == null) return;

            float frustumHeight, frustumWidth;
            if (targetCamera.orthographic)
            {
                frustumHeight = targetCamera.orthographicSize * 2.0f;
                frustumWidth = frustumHeight * targetCamera.aspect;
            }
            else
            {
                frustumHeight = 2.0f * distance * Mathf.Tan(targetCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
                frustumWidth = frustumHeight * targetCamera.aspect;
            }

            fogQuadObj.transform.localScale = new Vector3(frustumWidth * 1.15f, frustumHeight * 1.15f, 1.0f);
        }

        private void Update()
        {
            ResolvePlayerAndCamera();
            if (playerTransform == null) return;

            if (fogQuadObj == null && targetCamera != null)
            {
                CrearFrustumFogQuad();
            }

            bool isNight = DeterminarSiEsNoche();
            float targetNight = isNight ? 1.0f : 0.0f;
            currentNightIntensity = Mathf.MoveTowards(currentNightIntensity, targetNight, Time.deltaTime * (fogTransitionSpeed * 0.5f));

            Color currentColor = Color.Lerp(dayFogColor, nightFogColor, currentNightIntensity);
            float effectiveClearRadius = Mathf.Max(8.5f, nearDistance);
            float effectiveFar = Mathf.Max(effectiveClearRadius + 8f, farDistance);

            if (fogMaterial != null)
            {
                fogMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent + 400;
                fogMaterial.SetVector("_PlayerPosition", playerTransform.position);
                fogMaterial.SetVector("_PlayerForward", playerTransform.forward);
                fogMaterial.SetFloat("_NightIntensity", currentNightIntensity);
                fogMaterial.SetColor("_FogColor", currentColor);
                fogMaterial.SetFloat("_FogDensity", fogDensity);
                fogMaterial.SetFloat("_DirectionalIntensity", directionalForwardBoost);
                fogMaterial.SetFloat("_NearDistance", effectiveClearRadius);
                fogMaterial.SetFloat("_FarDistance", effectiveFar);
                fogMaterial.SetFloat("_NoiseScale", noiseScale);
                fogMaterial.SetFloat("_NoiseSpeed", noiseSpeed);
                fogMaterial.SetFloat("_MaxAlpha", maxSmogAlpha);
            }

            if (controlGlobalFog)
            {
                RenderSettings.fog = true;
                float targetGlobalDensity = isNight ? 0.025f : 0.004f;
                RenderSettings.fogDensity = Mathf.MoveTowards(RenderSettings.fogDensity, targetGlobalDensity, Time.deltaTime * 0.01f);
                RenderSettings.fogColor = currentColor;
            }
            else if (RenderSettings.fog)
            {
                RenderSettings.fog = false;
            }
        }

        private bool DeterminarSiEsNoche()
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

            if (TimeManager.Instance != null)
            {
                return TimeManager.Instance.CurrentCycleState == TimeManager.CycleState.Night;
            }
            return false;
        }

        private void OnDestroy()
        {
            if (fogQuadObj != null) Destroy(fogQuadObj);
            if (fogMaterial != null) Destroy(fogMaterial);
        }
    }
}
