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
        [Min(0)] public float brightness = .9f;
        [Min(.02f)] public float contourSpacing = .22f;
        [Range(.01f, .45f)] public float contourThickness = .06f;
        [Range(0, 3)] public float edgeStrength = .8f;
        [Min(.01f)] public float frontWidth = .3f;
        [Header("Propagation")]
        [Min(.1f)] public float speed = 6f;
        [Min(.1f)] public float maximumRadius = 16f;
        [Min(.05f)] public float trailSeconds = 2.2f;
        [Range(1, Capacity)] public int maximumConcurrent = 4;

        struct Wave { public Vector3 origin; public float started, radius, speed, trail; }
        readonly Wave[] waves = new Wave[Capacity];
        int count;
        public int ActiveCount { get { Prune(); return count; } }

        public void TriggerSoundScan(Vector3 position, float loudness)
        {
            float strength = Mathf.Clamp01(loudness);
            if (strength <= 0 || !isActiveAndEnabled) return;
            Prune();
            int limit = Mathf.Clamp(maximumConcurrent, 1, Capacity);
            while (count >= limit) RemoveOldest();
            waves[count++] = new Wave { origin = position, started = Time.time,
                radius = Mathf.Max(.01f, maximumRadius) * strength, speed = Mathf.Max(.01f, speed), trail = Mathf.Max(.01f, trailSeconds) };
        }
        void RemoveOldest() { for (int i = 1; i < count; i++) waves[i - 1] = waves[i]; count--; }
        void Prune()
        {
            for (int i = count - 1; i >= 0; i--)
                if (Time.time - waves[i].started > waves[i].radius / waves[i].speed + waves[i].trail)
                { for (int j = i + 1; j < count; j++) waves[j - 1] = waves[j]; count--; }
        }
        internal int CopyWaves(Vector4[] origins, Vector4[] parameters)
        {
            Prune();
            for (int i = 0; i < count; i++)
            {
                var w = waves[i];
                origins[i] = new Vector4(w.origin.x, w.origin.y, w.origin.z, w.radius);
                parameters[i] = new Vector4(Time.time - w.started, w.speed, w.trail, 0);
            }
            return count;
        }
    }
}
