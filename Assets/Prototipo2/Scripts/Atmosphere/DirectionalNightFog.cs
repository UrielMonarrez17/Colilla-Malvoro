using UnityEngine;

namespace Prototipo2
{
    public class DirectionalNightFog : MonoBehaviour
    {
        [Header("Referencias")]
        public Transform playerTransform;

        [Header("Configuración de Niebla Ambiental")]
        public bool controlGlobalFog = true;
        public float dayFogDensity = 0.006f;
        public float nightFogDensity = 0.045f;
        public Color dayFogColor = new Color(0.65f, 0.72f, 0.80f, 1f);
        public Color nightFogColor = new Color(0.04f, 0.05f, 0.09f, 1f);
        public float fogTransitionSpeed = 1.5f;

        [Header("Cono Direccional Frontal (Suspenso Nocturno)")]
        public bool enableForwardSuspenseCone = true;
        public float coneDistance = 6.0f;
        public float coneRadius = 3.5f;
        [Range(0f, 1f)] public float nightParticleAlpha = 0.65f;

        private ParticleSystem forwardFogParticles;
        private ParticleSystem.MainModule particleMain;
        private ParticleSystem.EmissionModule particleEmission;
        private float currentFogDensity;
        private Color currentFogColor;

        private void Start()
        {
            if (playerTransform == null)
            {
                HamsterController hc = FindAnyObjectByType<HamsterController>();
                if (hc != null) playerTransform = hc.transform;
            }

            if (controlGlobalFog)
            {
                RenderSettings.fog = true;
                RenderSettings.fogMode = FogMode.ExponentialSquared;
                currentFogDensity = dayFogDensity;
                currentFogColor = dayFogColor;
                RenderSettings.fogDensity = currentFogDensity;
                RenderSettings.fogColor = currentFogColor;
            }

            if (enableForwardSuspenseCone)
            {
                CrearConoParticulasFrontal();
            }
        }

        private void Update()
        {
            if (playerTransform == null)
            {
                HamsterController hc = FindAnyObjectByType<HamsterController>();
                if (hc != null) playerTransform = hc.transform;
                if (playerTransform == null) return;
            }

            bool isNight = DeterminarSiEsNoche();

            if (controlGlobalFog)
            {
                float targetDensity = isNight ? nightFogDensity : dayFogDensity;
                Color targetColor = isNight ? nightFogColor : dayFogColor;

                currentFogDensity = Mathf.MoveTowards(currentFogDensity, targetDensity, Time.deltaTime * (fogTransitionSpeed * 0.02f));
                currentFogColor = Color.Lerp(currentFogColor, targetColor, Time.deltaTime * fogTransitionSpeed);

                RenderSettings.fogDensity = currentFogDensity;
                RenderSettings.fogColor = currentFogColor;
            }

            if (forwardFogParticles != null)
            {
                ActualizarConoParticulas(isNight);
            }
        }

        private bool DeterminarSiEsNoche()
        {
            if (TimeManager.Instance != null)
            {
                return TimeManager.Instance.CurrentCycleState == TimeManager.CycleState.Night;
            }
            return false;
        }

        private void CrearConoParticulasFrontal()
        {
            GameObject fogConeObj = new GameObject("NightFogSuspenseCone");
            fogConeObj.transform.SetParent(playerTransform != null ? playerTransform : transform, false);
            fogConeObj.transform.localPosition = new Vector3(0, 0.4f, 1.2f);
            fogConeObj.transform.localRotation = Quaternion.identity;

            forwardFogParticles = fogConeObj.AddComponent<ParticleSystem>();
            var renderer = fogConeObj.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;

            // Material suave compatible con URP
            Shader pShader = Shader.Find("Universal Render Pipeline/Particles/Unlit") 
                ?? Shader.Find("Universal Render Pipeline/Unlit") 
                ?? Shader.Find("Particles/Standard Unlit") 
                ?? Shader.Find("Sprites/Default");
            Material fogMat = new Material(pShader);
            renderer.material = fogMat;

            particleMain = forwardFogParticles.main;
            particleMain.startLifetime = 2.0f;
            particleMain.startSpeed = 1.0f;
            particleMain.startSize = 3.5f;
            particleMain.startColor = new Color(0.1f, 0.12f, 0.18f, 0f);
            particleMain.simulationSpace = ParticleSystemSimulationSpace.World;
            particleMain.maxParticles = 60;

            particleEmission = forwardFogParticles.emission;
            particleEmission.rateOverTime = 15f;

            var shape = forwardFogParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 25f;
            shape.radius = 0.5f;

            var colOverLifetime = forwardFogParticles.colorOverLifetime;
            colOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(nightParticleAlpha, 0.4f), new GradientAlphaKey(0f, 1f) }
            );
            colOverLifetime.color = grad;
        }

        private void ActualizarConoParticulas(bool isNight)
        {
            if (forwardFogParticles == null) return;

            if (isNight)
            {
                if (!forwardFogParticles.isPlaying) forwardFogParticles.Play();
                particleEmission.rateOverTime = 25f;
            }
            else
            {
                particleEmission.rateOverTime = 2f;
            }
        }
    }
}
