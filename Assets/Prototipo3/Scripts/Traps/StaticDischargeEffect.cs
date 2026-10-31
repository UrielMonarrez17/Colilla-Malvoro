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
                Shader sh = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard");
                pulseMat = new Material(sh);
                pulseMat.color = new Color(0.3f, 0.75f, 1f, 0.55f);
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
                pulseMat.color = new Color(0.35f, 0.85f, 1f, (1f - t) * 0.55f);
            }
        }

        private void OnDestroy()
        {
            if (pulseMat != null) Destroy(pulseMat);
        }
    }
}
