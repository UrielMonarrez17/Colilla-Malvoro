using UnityEngine;

namespace Prototipo3
{
    /// <summary>
    /// Componente de aura visual para el prefab de la Trampa 2 (Bomba de Polen de Helecho).
    /// </summary>
    public class PollenCloudEffect : MonoBehaviour
    {
        public float duration = 15f;
        private float elapsed = 0f;

        private void Update()
        {
            elapsed += Time.deltaTime;
            float bob = Mathf.Sin(elapsed * 3.5f) * 0.06f;
            transform.Rotate(0f, 38f * Time.deltaTime, 0f);
            Vector3 s = Vector3.one * (1f + bob);
            transform.localScale = s;
        }
    }
}
