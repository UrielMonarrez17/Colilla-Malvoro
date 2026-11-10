using UnityEngine;

namespace Prototipo3
{
    /// <summary>
    /// Nube temporal de gas anestésico dejada por Clara Valdés (Cisne Negro - "La Dama de Gas").
    /// Ralentiza a Kilo si camina por el suelo dentro de su radio.
    /// </summary>
    public class AnestheticGasCloud : MonoBehaviour
    {
        [Header("Configuración de Nube de Gas")]
        public float duration = 7.0f;
        public float radius = 2.6f;
        public float slowMultiplier = 0.52f;
        public Color gasColor = new Color(0.55f, 0.25f, 0.85f, 0.38f);

        private float elapsed = 0f;
        private Renderer cloudRenderer;
        private Material cloudMaterial;

        private void Start()
        {
            cloudRenderer = GetComponentInChildren<Renderer>();
            if (cloudRenderer == null)
            {
                GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                visual.name = "GasVisual";
                visual.transform.SetParent(transform, false);
                visual.transform.localPosition = new Vector3(0f, 0.15f, 0f);
                visual.transform.localScale = new Vector3(radius * 2f, 0.15f, radius * 2f);
                Collider col = visual.GetComponent<Collider>();
                if (col != null) Destroy(col);
                cloudRenderer = visual.GetComponent<Renderer>();
            }

            if (cloudRenderer != null)
            {
                Shader sh = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard");
                cloudMaterial = new Material(sh);
                cloudMaterial.color = gasColor;
                cloudRenderer.material = cloudMaterial;
                cloudRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                cloudRenderer.receiveShadows = false;
            }

            SphereCollider trigger = GetComponent<SphereCollider>();
            if (trigger == null)
            {
                trigger = gameObject.AddComponent<SphereCollider>();
            }
            trigger.isTrigger = true;
            trigger.radius = radius;
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            if (elapsed >= duration)
            {
                Destroy(gameObject);
                return;
            }

            // Efecto visual de pulsación y desvanecimiento
            float lifeNormalized = 1f - (elapsed / duration);
            float pulse = 1f + Mathf.Sin(Time.time * 4f) * 0.08f;
            transform.localScale = Vector3.one * pulse;

            if (cloudMaterial != null)
            {
                Color c = gasColor;
                c.a = gasColor.a * Mathf.Clamp01(lifeNormalized * 1.4f);
                cloudMaterial.color = c;
            }

            // Verificar distancia al jugador en el suelo
            HamsterControllerPrototipo3 player = FindAnyObjectByType<HamsterControllerPrototipo3>();
            if (player != null && !player.IsDead)
            {
                Vector3 delta = player.transform.position - transform.position;
                if (Mathf.Abs(delta.y) <= 1.5f)
                {
                    delta.y = 0f;
                    if (delta.sqrMagnitude <= radius * radius)
                    {
                        player.ApplyExternalSlow(slowMultiplier, 0.35f);
                    }
                }
            }
        }

        private void OnDestroy()
        {
            if (cloudMaterial != null)
            {
                Destroy(cloudMaterial);
            }
        }
    }
}
