using UnityEngine;

namespace SoundThief.SoundScan
{
    /// <summary>Attach to a camera using a renderer with SoundScanRendererFeature.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(Camera))]
    public sealed class SoundScanController : MonoBehaviour
    {
        public const int Capacity = 8;
        [Header("Appearance")]
        [ColorUsage(false, true)] public Color scanColor = new Color(.08f, 1f, .85f, 1f);
        [Min(0)] public float brightness = .22f;
        [Min(.02f)] public float contourSpacing = .22f;
        [Range(.01f, .45f)] public float contourThickness = .06f;
        [Range(0, 3)] public float edgeStrength = .8f;
        [Min(.01f)] public float frontWidth = .3f;
        [Header("Propagation")]
        [Min(.1f)] public float speed = 6f;
        [Min(.1f)] public float maximumRadius = 16f;
        [Min(.05f)] public float trailSeconds = 2.2f;
        [Range(1, Capacity)] public int maximumConcurrent = 4;

        const int Directions = 16;
        struct Wave { public Vector3 origin; public float started, radius, speed, trail, strength; public Vector4[] transmission; }
        readonly Wave[] waves = new Wave[Capacity];
        int count;
        public int ActiveCount { get { Prune(); return count; } }

        public void TriggerSoundScan(Vector3 position, float loudness)
        {
            float strength = Mathf.Clamp01(loudness);
            TriggerRadius(position, Mathf.Max(.01f, maximumRadius) * strength, strength, null);
        }
        public void TriggerRadius(Vector3 position, float radius, float strength, System.Func<Vector3, float> transmission)
        {
            if (strength <= 0 || radius <= 0 || !isActiveAndEnabled) return;
            Prune();
            int limit = Mathf.Clamp(maximumConcurrent, 1, Capacity);
            while (count >= limit) RemoveOldest();
            var samples = new Vector4[Directions];
            for (int i = 0; i < Directions; i++)
            {
                float angle = 2 * Mathf.PI * i / Directions;
                Vector3 direction = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                samples[i] = transmission == null ? Vector4.one : new Vector4(
                    transmission(position + direction * (radius * .25f) + Vector3.up * 1.4f),
                    transmission(position + direction * (radius * .5f) + Vector3.up * 1.4f),
                    transmission(position + direction * (radius * .75f) + Vector3.up * 1.4f),
                    transmission(position + direction * radius + Vector3.up * 1.4f));
            }
            waves[count++] = new Wave { origin = position, started = Time.time,
                radius = radius, strength = Mathf.Clamp01(strength), transmission = samples, speed = Mathf.Max(.01f, speed), trail = Mathf.Max(.01f, trailSeconds) };
        }
        void RemoveOldest() { for (int i = 1; i < count; i++) waves[i - 1] = waves[i]; count--; }
        void Prune()
        {
            for (int i = count - 1; i >= 0; i--)
                if (Time.time - waves[i].started > waves[i].radius / waves[i].speed + waves[i].trail)
                { for (int j = i + 1; j < count; j++) waves[j - 1] = waves[j]; count--; }
        }
        internal int CopyWaves(Vector4[] origins, Vector4[] parameters, Vector4[] attenuation)
        {
            Prune();
            for (int i = 0; i < count; i++)
            {
                var w = waves[i];
                origins[i] = new Vector4(w.origin.x, w.origin.y, w.origin.z, w.radius);
                parameters[i] = new Vector4(Time.time - w.started, w.speed, w.trail, w.strength);
                System.Array.Copy(w.transmission, 0, attenuation, i * Directions, Directions);
            }
            return count;
        }
    }
}
