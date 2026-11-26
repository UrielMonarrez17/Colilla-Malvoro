using UnityEngine;

namespace Prototipo3
{
    /// <summary>
    /// Objeto colocado en el suelo por la Trampa 1 (Señuelo Acústico).
    /// Emite ondas visuales/sonoras durante 8 s y atrae a los NavMeshAgent enemigos en 18 m.
    /// </summary>
    public class AcousticDecoyObject : MonoBehaviour
    {
        public float duration = 8f;
        public float attractionRadius = 18f;

        private float elapsed = 0f;
        private float pulseTimer = 0f;
        private Transform ringVisual;
        private Material ringMat;

        private void Start()
        {
            // Cuerpo del emisor si no viene del prefab
            if (transform.childCount == 0)
            {
                GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                body.name = "DecoySpeakerBody";
                body.transform.SetParent(transform, false);
                body.transform.localPosition = new Vector3(0f, 0.25f, 0f);
                body.transform.localScale = new Vector3(0.55f, 0.25f, 0.55f);
                Collider c = body.GetComponent<Collider>();
                if (c != null) Destroy(c);

                Renderer bodyRen = body.GetComponent<Renderer>();
                if (bodyRen != null)
                {
                    Shader lit = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Universal Render Pipeline/Unlit");
                    if (lit != null)
                    {
                        Material m = new Material(lit);
                        Color bodyCol = new Color(0.15f, 0.75f, 1f, 1f);
                        m.color = bodyCol;
                        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", bodyCol);
                        bodyRen.material = m;
                    }
                }
            }

            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "SonicWaveRing";
            ring.transform.SetParent(transform, false);
            ring.transform.localPosition = new Vector3(0f, 0.1f, 0f);
            Collider rc = ring.GetComponent<Collider>();
            if (rc != null) Destroy(rc);
            ringVisual = ring.transform;

            Renderer rRen = ring.GetComponent<Renderer>();
            if (rRen != null)
            {
                Shader sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
                ringMat = new Material(sh);
                Color ringCol = new Color(0.1f, 0.85f, 1f, 0.35f);
                ringMat.color = ringCol;
                if (ringMat.HasProperty("_BaseColor")) ringMat.SetColor("_BaseColor", ringCol);
                if (ringMat.HasProperty("_Surface"))
                {
                    ringMat.SetFloat("_Surface", 1f);
                    ringMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    ringMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    ringMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    ringMat.SetInt("_ZWrite", 0);
                    ringMat.renderQueue = 3600;
                }
                rRen.material = ringMat;
                rRen.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            BroadcastAcousticAttraction();
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            if (elapsed >= duration)
            {
                Destroy(gameObject);
                return;
            }

            // Onda visual expansiva
            if (ringVisual != null)
            {
                float waveT = (elapsed * 1.6f) % 1f;
                float scale = Mathf.Lerp(0.8f, attractionRadius * 0.65f, waveT);
                ringVisual.localScale = new Vector3(scale, 0.03f, scale);
                if (ringMat != null)
                {
                    Color c = new Color(0.1f, 0.88f, 1f, (1f - waveT) * 0.42f);
                    ringMat.color = c;
                    if (ringMat.HasProperty("_BaseColor")) ringMat.SetColor("_BaseColor", c);
                }
            }

            pulseTimer += Time.deltaTime;
            if (pulseTimer >= 0.3f)
            {
                pulseTimer = 0f;
                BroadcastAcousticAttraction();
            }
        }

        private void BroadcastAcousticAttraction()
        {
            float remaining = Mathf.Max(0.5f, duration - elapsed);
            EnemyAIPrototipo3[] enemies = FindObjectsByType<EnemyAIPrototipo3>(FindObjectsSortMode.None);
            for (int i = 0; i < enemies.Length; i++)
            {
                EnemyAIPrototipo3 e = enemies[i];
                if (e == null || !e.isActiveAndEnabled) continue;

                float effectiveRange = attractionRadius * Mathf.Max(1f, e.acousticDecoyVulnerabilityMultiplier);
                float dist = Vector3.Distance(transform.position, e.transform.position);
                if (dist <= effectiveRange)
                {
                    e.AttractToAcousticDecoy(transform.position, remaining);
                }
            }
        }

        private void OnDestroy()
        {
            if (ringMat != null) Destroy(ringMat);
        }
    }
}
