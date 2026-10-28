using UnityEngine;

namespace SoundThief
{
    [CreateAssetMenu(menuName = "Sound Thief/Sound Rules")]
    public sealed class SoundRules : ScriptableObject
    {
        public float[] holdSeconds = { 24f, 18f, 12f };
        public float walkSpeed = 4f, carrySpeed = 1.5f, interactDistance = 2.8f;
        public float injectionHold = 4f, decayStep = 2f, pulseInterval = 0.7f;
        public float playerStun = 2f, guardStun = 3f, panicSeconds = 4f;
        public float hearingPerGrade = 6f, blastPerGrade = 2f;
        public float sightRange = 10f, sightAngle = 80f, guardSpeed = 2.2f;
        public float wakeThreshold = 2.5f, alertSeconds = 4f;
        public float HoldTime(int grade) => holdSeconds[Mathf.Clamp(grade - 1, 0, holdSeconds.Length - 1)];
    }
}
