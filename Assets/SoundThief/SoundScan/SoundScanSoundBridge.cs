using System.Collections.Generic;
using UnityEngine;

namespace SoundThief.SoundScan
{
    [DisallowMultipleComponent, RequireComponent(typeof(SoundScanController))]
    public sealed class SoundScanSoundBridge : MonoBehaviour
    {
        public SoundWorld world;
        [Min(0)] public float repeatedSourceInterval = 1.2f;
        readonly Dictionary<int, float> lastSourceTime = new Dictionary<int, float>();
        SoundScanController scanner;
        void OnEnable()
        {
            scanner = GetComponent<SoundScanController>();
            if (world) world.AudibleSound += OnSound;
        }
        void OnDisable()
        {
            if (world) world.AudibleSound -= OnSound;
            lastSourceTime.Clear();
        }
        void OnSound(Vector3 position, int grade, float volume, Object source)
        {
            if (!world || !world.player || !world.player.view || !world.player.Mission || !world.player.Mission.CanAct || !scanner) return;
            float scaledVolume = Mathf.Clamp01(volume);
            if (grade <= 0 || scaledVolume <= 0) return;
            if (!world.CanHear(position, world.player.transform.position, grade, out float proximity)) return;
            float transmission = world.HearingMultiplier(position, world.player.view.transform.position);
            float loudness = Mathf.Clamp01(scaledVolume * Mathf.Sqrt(transmission) * Mathf.Lerp(.5f, 1f, proximity));
            if (loudness < .035f) return;
            int key = source ? source.GetInstanceID() : Vector3Int.FloorToInt(position).GetHashCode();
            bool footstep = source is PlayerFootsteps || source is GuardFootsteps;
            if (!footstep && lastSourceTime.TryGetValue(key, out float last) && Time.time - last < repeatedSourceInterval) return;
            lastSourceTime[key] = Time.time;
            float radius = grade * world.rules.hearingPerGrade;
            if (source is PlayerFootsteps footsteps)
                radius = 2f * world.rules.hearingPerGrade / 3f * (footsteps.GetComponent<ThiefPlayer>().IsCrouching ? .5f : 1f);
            scanner.TriggerRadius(position, radius, loudness, point => world.HearingMultiplier(position, point));
        }
    }
}
