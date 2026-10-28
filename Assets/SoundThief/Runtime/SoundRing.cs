using UnityEngine;
namespace SoundThief
{
    public sealed class SoundRing : MonoBehaviour
    {
        LineRenderer line;
        float age, radius;
        Color color;
        Material material;
        public void Init(Vector3 position, int grade, bool blast)
        {
            transform.position = position;
            radius = grade * (blast ? 2f : 1.3f);
            color = blast ? new Color(1, 0.25f, 0.2f) : new Color(0.15f, 1, 0.8f);
            line = gameObject.AddComponent<LineRenderer>(); line.positionCount = 49;
            line.widthMultiplier = blast ? 0.08f : 0.025f;
            material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.color = color; line.sharedMaterial = material;
        }
        void Update()
        {
            if (!line) return;
            age += Time.deltaTime;
            for (int i = 0; i < 49; i++)
            {
                float angle = i * Mathf.PI * 2 / 48;
                line.SetPosition(i, transform.position + new Vector3(Mathf.Cos(angle) * radius * age, 0.04f, Mathf.Sin(angle) * radius * age));
            }
            line.widthMultiplier = Mathf.Max(0.002f, (1 - age) * 0.045f);
            if (age >= 1) Destroy(gameObject);
        }
        void OnDestroy() { if (material) Destroy(material); }
    }
}

