using UnityEngine;

namespace SoundThief
{
    public enum InteractionKind { Key, Door, Resonator, Treasure, Exit, ContractBoard, GuildExit, PracticeReset }

    public sealed class SoundRelation : MonoBehaviour
    {
        public string firstMedium = "손", secondMedium = "금속";
        [Range(1, 3)] public int grade = 2;
        public bool IsMuted { get; private set; }
        public string Label => firstMedium + " ↔ " + secondMedium;
        public SoundCharge Extract(SoundRules rules)
        {
            if (IsMuted) return null;
            IsMuted = true;
            return new SoundCharge(this, grade, Time.time + rules.HoldTime(grade));
        }
        public void Restore() => IsMuted = false;
        public void MakeNoise(SoundWorld world)
        {
            if (!IsMuted) world.Pulse(transform.position, grade, null);
        }
    }

    // A charge owns exactly one suppression lease, even after leaving the player's hand.
    public sealed class SoundCharge
    {
        public readonly SoundRelation Source;
        public readonly int Grade;
        public readonly float Deadline;
        public bool Finished { get; private set; }
        public float Remaining => Mathf.Max(0, Deadline - Time.time);
        public SoundCharge(SoundRelation source, int grade, float deadline)
        { Source = source; Grade = grade; Deadline = deadline; }
        public bool Finish()
        {
            if (Finished) return false;
            Finished = true;
            if (Source) Source.Restore();
            return true;
        }
    }

}

