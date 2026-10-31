using UnityEngine;

namespace SoundThief
{
    // A small procedural paper mesh; no texture, shader or imported image is needed.
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class ContractPageCurl : UnityEngine.UI.MaskableGraphic
    {
        float progress;
        public void SetTurn(float value, int sign) { progress = value; SetVerticesDirty(); }
        protected override void OnPopulateMesh(UnityEngine.UI.VertexHelper mesh)
        {
            mesh.Clear();
            var rect = rectTransform.rect;
            const int strips = 32;
            for (int i = 0; i <= strips; i++)
            {
                float u = i / (float)strips;
                // A loose sheet flexes slightly while being lifted; it never rotates around a binding.
                float flex = Mathf.Sin(progress * Mathf.PI);
                float x = rect.xMin + u * rect.width;
                float lift = Mathf.Sin(u * Mathf.PI) * rect.width * 0.018f * flex;
                float shade = 1 - flex * (0.035f + 0.035f * u);
                Color tint = color * new Color(shade, shade, shade, 1);
                mesh.AddVert(new Vector3(x, rect.yMin + lift, 0), tint, Vector2.zero);
                mesh.AddVert(new Vector3(x, rect.yMax + lift, 0), tint, Vector2.one);
                if (i == 0) continue;
                int v = i * 2;
                mesh.AddTriangle(v - 2, v - 1, v); mesh.AddTriangle(v, v - 1, v + 1);
            }
        }
    }
}
