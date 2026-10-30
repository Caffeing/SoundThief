using System.Collections.Generic;
using UnityEngine;

namespace SoundThief
{
    public sealed class SoundWorld : MonoBehaviour
    {
        public SoundRules rules;
        public ThiefPlayer player;
        public readonly List<GuardAgent> Guards = new List<GuardAgent>();
        public int BlastCount { get; private set; }
        public Vector3 LastBlastPosition { get; private set; }
        // Presentation listeners do not affect guard hearing or sound propagation.
        public event System.Action<Vector3, int, float, Object> AudibleSound;
        public void ReportAudibleSound(Vector3 position, int grade, float volume, Object source)
        {
            AudibleSound?.Invoke(position, grade, volume, source);
        }
        readonly Dictionary<int, AudioClip> tones = new Dictionary<int, AudioClip>();

        void Awake()
        {
            for (int grade = 1; grade <= 3; grade++)
            {
                var samples = new float[11025];
                for (int i = 0; i < samples.Length; i++)
                    samples[i] = Mathf.Sin(i * (180 + grade * 110) * 2 * Mathf.PI / 22050f) * Mathf.Exp(-i / 1800f) * 0.18f;
                var clip = AudioClip.Create("Sound grade " + grade, samples.Length, 1, 22050, false);
                clip.SetData(samples, 0); tones.Add(grade, clip);
            }
        }
        public float HearingMultiplier(Vector3 from, Vector3 to)
        {
            if (from.y < .3f) from += Vector3.up * .9f;
            var delta = to - from;
            var visited = new HashSet<int>(); float multiplier = 1;
            foreach (var hit in Physics.RaycastAll(from, delta.normalized, delta.magnitude, 1 << 8, QueryTriggerInteraction.Ignore))
            {
                var door = hit.collider.GetComponentInParent<ThiefInteractable>();
                if (door && door.kind == InteractionKind.Door)
                {
                    // Only test the source door, and avoid ClosestPoint on non-convex meshes.
                    bool supportsClosest = !(hit.collider is MeshCollider mesh) || mesh.convex;
                    if (supportsClosest ? (hit.collider.ClosestPoint(from) - from).sqrMagnitude < .0004f : hit.distance < .02f) continue;
                    if (!door.IsOpen && visited.Add(door.GetInstanceID())) multiplier *= rules.closedDoorHearingMultiplier;
                    continue;
                }
                var barrier = hit.collider.GetComponentInParent<SoundBarrier>();
                if (barrier && visited.Add(barrier.GetInstanceID())) multiplier *= barrier.rangeMultiplier;
            }
            return multiplier;
        }
        public bool CanHear(Vector3 from, Vector3 listener, int grade, out float proximity)
        {
            float reach = Mathf.Max(.01f, grade * rules.hearingPerGrade * HearingMultiplier(from, listener + Vector3.up * 1.4f));
            float distance = Vector3.Distance(from, listener);
            proximity = Mathf.Clamp01(1 - distance / reach);
            return grade > 0 && distance <= reach;
        }
        public void Pulse(Vector3 position, int grade, SoundEmitter source, AudioClip clip = null)
        {
            ReportAudibleSound(position, grade, 1f, source);
            foreach (var guard in Guards) if (guard) guard.Hear(position, grade, source);
            if (source && source.SoundClip) clip = source.SoundClip;
            if (!clip) tones.TryGetValue(grade, out clip);
            PlaySound(position, grade, clip);
        }
        public void PlaySound(Vector3 position, int grade, AudioClip clip, float volume = 1, bool localPlayer = false)
        {
            if (clip)
            {
                var soundObject = new GameObject("Positional sound"); soundObject.transform.SetParent(transform); soundObject.transform.position = position;
                var feedback = soundObject.AddComponent<AudioSource>();
                float transmission = player && player.view ? HearingMultiplier(position, player.view.transform.position) : 1;
                feedback.spatialBlend = localPlayer ? 0 : 1; feedback.minDistance = .5f; feedback.maxDistance = Mathf.Max(.6f, rules.hearingPerGrade * grade * transmission);
                feedback.rolloffMode = AudioRolloffMode.Linear;
                feedback.volume = localPlayer ? volume : volume * Mathf.Sqrt(transmission);
                feedback.PlayOneShot(clip);
                Destroy(soundObject, clip.length + 0.1f);
            }
        }
        public void Blast(Vector3 position, int grade, bool stunPlayer)
        {
            BlastCount++; LastBlastPosition = position;
            Pulse(position, grade, null);
            foreach (var guard in Guards)
                if (guard && Vector3.Distance(position, guard.transform.position + Vector3.up) <= rules.blastPerGrade * grade
                    && !Blocked(position + Vector3.up * 0.08f, guard.transform.position + Vector3.up))
                    guard.Stun();
            if (stunPlayer) player.Stun(rules.playerStun);
        }
        // Shock and sight use physical occlusion. Hearing attenuates authored barriers separately.
        public static bool Blocked(Vector3 from, Vector3 to) => Physics.Linecast(from, to, 1 << 8, QueryTriggerInteraction.Ignore);
        public void ClearFeedback()
        {
            foreach (var source in GetComponentsInChildren<AudioSource>()) { source.Stop(); Destroy(source.gameObject); }
            foreach (var ring in GetComponentsInChildren<SoundRing>()) Destroy(ring.gameObject);
        }
        public void ResetFeedback()
        {
            ClearFeedback(); BlastCount = 0; LastBlastPosition = Vector3.zero;
        }
        void OnDestroy() { foreach (var clip in tones.Values) if (clip) Destroy(clip); }
    }

}

