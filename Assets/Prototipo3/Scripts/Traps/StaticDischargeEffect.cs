using UnityEngine;

namespace Prototipo3
{
    /// <summary>
    /// Efecto visual de pulso eléctrico de la Trampa 3 (Descargador Estático).
    /// </summary>
    public class StaticDischargeEffect : MonoBehaviour
    {
        public float radius = 8f;
        public float visualLifetime = 0.65f;
        private float timer = 0f;
        private Material pulseMat;

        private void Start()
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "ElectricArcSphere";
            sphere.transform.SetParent(transform, false);
            sphere.transform.localPosition = Vector3.up * 0.4f;
            Collider c = sphere.GetComponent<Collider>();
            if (c != null) Destroy(c);

            Renderer ren = sphere.GetComponent<Renderer>();
            if (ren != null)
            {
                Shader sh = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default");
                pulseMat = new Material(sh);
                Color pulseCol = new Color(0.3f, 0.75f, 1f, 0.55f);
                pulseMat.color = pulseCol;
                if (pulseMat.HasProperty("_BaseColor")) pulseMat.SetColor("_BaseColor", pulseCol);
                if (pulseMat.HasProperty("_Surface"))
                {
                    pulseMat.SetFloat("_Surface", 1f);
                    pulseMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    pulseMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                    pulseMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                    pulseMat.SetInt("_ZWrite", 0);
                    pulseMat.renderQueue = 3600;
                }
                ren.material = pulseMat;
                ren.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        private void Update()
        {
            timer += Time.deltaTime;
            if (timer >= visualLifetime)
            {
                Destroy(gameObject);
                return;
            }

            float t = timer / visualLifetime;
            float d = Mathf.Lerp(1f, radius * 2f, t);
            transform.localScale = new Vector3(d, 1.2f, d);
            if (pulseMat != null)
            {
                Color c = new Color(0.35f, 0.85f, 1f, (1f - t) * 0.55f);
                pulseMat.color = c;
                if (pulseMat.HasProperty("_BaseColor")) pulseMat.SetColor("_BaseColor", c);
            }
        }

        private void OnDestroy()
        {
            if (pulseMat != null) Destroy(pulseMat);
        }
    }
}
