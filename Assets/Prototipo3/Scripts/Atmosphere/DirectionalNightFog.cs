using UnityEngine;

namespace Prototipo3
{
    public class DirectionalNightFog : MonoBehaviour
    {
        [Header("Referencias")]
        public Transform playerTransform;
        public Camera targetCamera;

        [Header("Shader de Niebla Direccional")]
        public Shader fogShader;
        public Color dayFogColor = new Color(0.65f, 0.72f, 0.80f, 0.35f);
        public Color nightFogColor = new Color(0.04f, 0.06f, 0.11f, 0.95f);
        [Range(0.001f, 0.1f)] public float fogDensity = 0.022f;
        [Range(0.0f, 3.0f)] public float directionalForwardBoost = 1.6f;
        public float nearDistance = 2.0f;
        public float farDistance = 35.0f;
        public float fogTransitionSpeed = 1.8f;

        [Header("Horizonte Global")]
        public bool controlGlobalFog = true;

        private Material fogMaterial;
        private GameObject fogQuadObj;
        private float currentNightIntensity = 0f;

        private void Awake()
        {
            if (fogShader == null)
            {
                fogShader = Shader.Find("Prototipo3/CityDirectionalFog") ?? Shader.Find("Prototipo2/CityDirectionalFog");
            }
        }

        private void Start()
        {
            if (playerTransform == null)
            {
                HamsterController hc = FindAnyObjectByType<HamsterController>();
                if (hc != null) playerTransform = hc.transform;
            }

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            CrearFrustumFogQuad();

            if (controlGlobalFog)
            {
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.ExponentialSquared;
                RenderSettings.fogDensity = 0.005f;
                RenderSettings.fogColor = dayFogColor;
            }
        }

        private void CrearFrustumFogQuad()
        {
            if (targetCamera == null) return;
            if (fogShader == null)
            {
                fogShader = Shader.Find("Prototipo3/CityDirectionalFog") ?? Shader.Find("Prototipo2/CityDirectionalFog");
                if (fogShader == null)
                {
                    Debug.LogWarning("[DirectionalNightFog] Shader 'Prototipo3/CityDirectionalFog' no encontrado. Asegúrate de que compila correctamente.");
                    return;
                }
            }

            if (fogMaterial == null)
            {
                fogMaterial = new Material(fogShader);
                fogMaterial.name = "Mat_CityDirectionalFog_Instance";
            }

            // Destruir quad previo si existiera
            Transform existing = targetCamera.transform.Find("ScreenDirectionalFogQuad");
            if (existing != null) Destroy(existing.gameObject);

            // Crear quad acoplado a la cámara para renderizar el shader a pantalla completa
            fogQuadObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            fogQuadObj.name = "ScreenDirectionalFogQuad";
            fogQuadObj.transform.SetParent(targetCamera.transform, false);

            // Eliminar colisionador del quad para que no interfiera con raycasts
            Collider col = fogQuadObj.GetComponent<Collider>();
            if (col != null) Destroy(col);

            // Colocar justo delante del plano cercano de la cámara
            float distanceInFront = targetCamera.nearClipPlane + 0.15f;
            fogQuadObj.transform.localPosition = new Vector3(0, 0, distanceInFront);
            fogQuadObj.transform.localRotation = Quaternion.identity;

            // Escalar para cubrir exactamente el frustum de la cámara
            ActualizarEscalaQuad(distanceInFront);

            MeshRenderer mr = fogQuadObj.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.material = fogMaterial;
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

            // Margen adicional del 15% para garantizar cobertura completa en rotaciones
            fogQuadObj.transform.localScale = new Vector3(frustumWidth * 1.15f, frustumHeight * 1.15f, 1.0f);
        }

        private void Update()
        {
            if (playerTransform == null)
            {
                HamsterController hc = FindAnyObjectByType<HamsterController>();
                if (hc != null) playerTransform = hc.transform;
                if (playerTransform == null) return;
            }

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
                if (fogQuadObj == null && targetCamera != null)
                {
                    CrearFrustumFogQuad();
                }
            }

            bool isNight = DeterminarSiEsNoche();
            float targetNight = isNight ? 1.0f : 0.0f;
            currentNightIntensity = Mathf.MoveTowards(currentNightIntensity, targetNight, Time.deltaTime * (fogTransitionSpeed * 0.5f));

            Color currentColor = Color.Lerp(dayFogColor, nightFogColor, currentNightIntensity);

            if (fogMaterial != null)
            {
                fogMaterial.SetVector("_PlayerPosition", playerTransform.position);
                fogMaterial.SetVector("_PlayerForward", playerTransform.forward);
                fogMaterial.SetFloat("_NightIntensity", currentNightIntensity);
                fogMaterial.SetColor("_FogColor", currentColor);
                fogMaterial.SetFloat("_FogDensity", fogDensity);
                fogMaterial.SetFloat("_DirectionalIntensity", directionalForwardBoost);
                fogMaterial.SetFloat("_NearDistance", nearDistance);
                fogMaterial.SetFloat("_FarDistance", farDistance);
            }

            if (controlGlobalFog)
            {
                float targetGlobalDensity = isNight ? 0.035f : 0.005f;
                RenderSettings.fogDensity = Mathf.MoveTowards(RenderSettings.fogDensity, targetGlobalDensity, Time.deltaTime * 0.01f);
                RenderSettings.fogColor = currentColor;
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
            if (fogQuadObj != null)
            {
                Destroy(fogQuadObj);
            }
            if (fogMaterial != null)
            {
                Destroy(fogMaterial);
            }
        }
    }
}
