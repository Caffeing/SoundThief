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
        public void Pulse(Vector3 position, int grade, SoundEmitter source)
        {
            foreach (var guard in Guards) if (guard) guard.Hear(position, grade, source);
            if (tones.TryGetValue(grade, out var clip))
            {
                var soundObject = new GameObject("Positional sound"); soundObject.transform.position = position;
                soundObject.transform.SetParent(transform, true);
                var feedback = soundObject.AddComponent<AudioSource>();
                feedback.spatialBlend = 1; feedback.minDistance = 1; feedback.maxDistance = rules.hearingPerGrade * grade;
                feedback.PlayOneShot(clip);
                Destroy(soundObject, clip.length + 0.1f);
            }
            var ring = new GameObject("Sound pulse").AddComponent<SoundRing>();
            ring.transform.SetParent(transform, true);
            ring.Init(position, grade, false);
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
            var ring = new GameObject("Blast").AddComponent<SoundRing>();
            ring.transform.SetParent(transform, true);
            ring.Init(position, grade, true);
        }
        public void ResetFeedback()
        {
            foreach (Transform effect in transform) Destroy(effect.gameObject);
            BlastCount = 0; LastBlastPosition = Vector3.zero;
        }
        // Only level geometry (layer 8) blocks sight and shock. Hearing deliberately ignores it.
        public static bool Blocked(Vector3 from, Vector3 to) => Physics.Linecast(from, to, 1 << 8, QueryTriggerInteraction.Ignore);
        void OnDestroy() { foreach (var clip in tones.Values) if (clip) Destroy(clip); }
    }

}

