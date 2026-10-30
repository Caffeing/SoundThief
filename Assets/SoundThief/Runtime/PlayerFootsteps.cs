using UnityEngine;

namespace SoundThief
{
    [RequireComponent(typeof(ThiefPlayer), typeof(CharacterController))]
    public sealed class PlayerFootsteps : MonoBehaviour
    {
        public AudioClip clip;
        public float walkingVolume = .42f, crouchingVolume = .12f;
        public float carryingVolume = .28f;
        public float walkingStepDistance = 1.5f, crouchingStepDistance = .85f;
        public event System.Action<float> StepPlayed;
        ThiefPlayer player;
        CharacterController body;
        Vector3 previous;
        float distance;
        void Awake() { player = GetComponent<ThiefPlayer>(); body = GetComponent<CharacterController>(); previous = transform.position; }
        void LateUpdate()
        {
            var delta = transform.position - previous; previous = transform.position; delta.y = 0;
            if (!clip || !player.enabled || !player.Mission || !player.Mission.CanAct || player.IsStunned || !body.isGrounded || delta.magnitude > 2)
            { distance = 0; return; }
            distance += delta.magnitude;
            if (distance < (player.Slow ? crouchingStepDistance : walkingStepDistance)) return;
            distance = 0;
            float volume = player.IsCrouching ? crouchingVolume : player.Held != null ? carryingVolume : walkingVolume;
            player.Mission.World.PlaySound(transform.position + Vector3.up * .1f, 1, clip, volume);
            player.Mission.World.ReportAudibleSound(transform.position + Vector3.up * .1f, 1, volume, this);
            StepPlayed?.Invoke(volume);
        }
    }
}
