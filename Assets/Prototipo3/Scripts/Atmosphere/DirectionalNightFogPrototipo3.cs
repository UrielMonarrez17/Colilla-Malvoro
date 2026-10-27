using UnityEngine;

namespace Prototipo3.Atmosphere
{
    public class DirectionalNightFogPrototipo3 : MonoBehaviour
    {
        [Header("Referencias")]
        public Transform playerTransform;
        public Camera targetCamera;

        [Header("Shader de Niebla Direccional")]
        public Shader fogShader;
        public Color dayFogColor = new Color(0.60f, 0.65f, 0.75f, 0.35f);
        public Color nightFogColor = new Color(0.06f, 0.05f, 0.10f, 0.95f);
        public Color crimsonFogColor = new Color(0.35f, 0.04f, 0.08f, 0.85f);
        [Range(0.001f, 0.1f)] public float fogDensity = 0.022f;
        [Range(0.0f, 3.0f)] public float directionalForwardBoost = 1.6f;
        public float nearDistance = 2.0f;
        public float farDistance = 35.0f;
        public float fogTransitionSpeed = 1.8f;

        [Header("Horizonte Global")]
        public bool controlGlobalFog = true;

        private Material fogMaterial;
        private GameObject fogQuadObj;
        private float currentNightIntensity = 1f;

        private void Awake()
        {
            if (fogShader == null)
            {
                fogShader = Shader.Find("Prototipo3/CityDirectionalFog");
                if (fogShader == null)
                {
                    fogShader = Shader.Find("Prototipo2/CityDirectionalFog");
                }
            }
        }

        private void Start()
        {
            if (playerTransform == null)
            {
                var p = FindFirstObjectByType<Prototipo3.Player.HamsterControllerPrototipo3>();
                if (p != null) playerTransform = p.transform;
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
                RenderSettings.fogDensity = 0.008f;
                RenderSettings.fogColor = nightFogColor;
            }
        }

        private void CrearFrustumFogQuad()
        {
            if (fogShader == null) return;

            fogMaterial = new Material(fogShader);
            fogMaterial.name = "M_DirectionalNightFog_Prototipo3";

            fogQuadObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            fogQuadObj.name = "Prototipo3_FrustumFogQuad";
            Destroy(fogQuadObj.GetComponent<Collider>());

            var meshRenderer = fogQuadObj.GetComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = fogMaterial;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;

            if (targetCamera != null)
            {
                fogQuadObj.transform.SetParent(targetCamera.transform, false);
                fogQuadObj.transform.localPosition = new Vector3(0, 0, targetCamera.nearClipPlane + 0.08f);
                fogQuadObj.transform.localRotation = Quaternion.identity;

                float planeDist = targetCamera.nearClipPlane + 0.08f;
                float height = 2.0f * Mathf.Tan(0.5f * targetCamera.fieldOfView * Mathf.Deg2Rad) * planeDist;
                float width = height * targetCamera.aspect;
                fogQuadObj.transform.localScale = new Vector3(width * 1.05f, height * 1.05f, 1f);
            }
        }

        private void LateUpdate()
        {
            if (fogMaterial == null) return;

            if (playerTransform == null)
            {
                var p = FindFirstObjectByType<Prototipo3.Player.HamsterControllerPrototipo3>();
                if (p != null) playerTransform = p.transform;
            }

            Vector3 playerPos = playerTransform != null ? playerTransform.position : Vector3.zero;
            Vector3 playerFwd = playerTransform != null ? playerTransform.forward : Vector3.forward;

            fogMaterial.SetColor("_FogColor", nightFogColor);
            fogMaterial.SetColor("_CrimsonTint", crimsonFogColor);
            fogMaterial.SetFloat("_FogDensity", fogDensity);
            fogMaterial.SetFloat("_DirectionalIntensity", directionalForwardBoost);
            fogMaterial.SetFloat("_NightIntensity", currentNightIntensity);
            fogMaterial.SetFloat("_NearDistance", nearDistance);
            fogMaterial.SetFloat("_FarDistance", farDistance);

            fogMaterial.SetVector("_PlayerWorldPos", playerPos);
            fogMaterial.SetVector("_PlayerForwardDir", playerFwd);
        }

        private void OnDestroy()
        {
            if (fogQuadObj != null) Destroy(fogQuadObj);
            if (fogMaterial != null) Destroy(fogMaterial);
        }
    }
}
