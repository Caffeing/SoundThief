using UnityEngine;

namespace SoundThief
{
    // Familiar room ambience is audible, but is not a suspicious guard stimulus.
    // Stealing this relation suppresses it; replaying its charge elsewhere is suspicious.
    [RequireComponent(typeof(SoundRelation))]
    public sealed class RelationAmbience : MonoBehaviour
    {
        public SoundWorld world;
        public float interval = 1.2f, volume = .3f;
        SoundRelation relation;
        float next;
        void Awake() { relation = GetComponent<SoundRelation>(); }
        void Update()
        {
            if (!world || !world.player || !world.player.Mission.CanAct || Time.time < next) return;
            next = Time.time + Mathf.Max(.2f, interval);
            if (!relation.IsMuted) world.PlaySound(transform.position, relation.grade, relation.soundClip, volume);
        }
    }
}
