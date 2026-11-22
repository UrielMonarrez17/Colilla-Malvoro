using UnityEngine;
using UnityEngine.UI;

namespace Prototipo2.UI
{
    // Editable Canvas graphic: deterministic ragged dry-brush edges, no texture dependency.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class MenuBrushGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect r = rectTransform.rect;
            const int steps = 80;
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                float edge = Mathf.Pow(Mathf.Abs(t * 2 - 1), 18) * Mathf.Min(10, r.height * .25f);
                float noise = Mathf.PerlinNoise(i * 2.71f, 4.2f) * Mathf.Min(5, r.height * .12f);
                float x = Mathf.Lerp(r.xMin, r.xMax, t);
                vh.AddVert(new Vector3(x, r.yMin + noise + edge), color, Vector2.zero);
                vh.AddVert(new Vector3(x, r.yMax - noise - edge), color, Vector2.one);
                if (i == steps) continue;
                int v = i * 2;
                vh.AddTriangle(v, v + 1, v + 2);
                vh.AddTriangle(v + 1, v + 3, v + 2);
            }
        }
    }
}
