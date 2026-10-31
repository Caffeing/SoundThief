using UnityEngine;
using UnityEngine.AI;

namespace SoundThief
{
    [RequireComponent(typeof(GuardAgent), typeof(NavMeshAgent))]
    public sealed class GuardFootsteps : MonoBehaviour
    {
        public AudioClip clip;
        public float stepDistance = .9f, volume = .9f;
        GuardAgent guard;
        NavMeshAgent agent;
        Vector3 previous;
        float travelled;
        void Awake() { guard = GetComponent<GuardAgent>(); agent = GetComponent<NavMeshAgent>(); previous = transform.position; }
        void Update()
        {
            var position = transform.position; float distance = Vector3.Distance(position, previous); previous = position;
            if (!clip || !guard.world || !guard.world.player.Mission.CanAct || !agent.isOnNavMesh || agent.velocity.sqrMagnitude < .02f || distance > 2) return;
            travelled += distance;
            if (travelled < stepDistance) return;
            travelled = 0;
            // Familiar guard footsteps inform the player without recursively alerting guards.
            guard.world.PlaySound(position + Vector3.up * .1f, 2, clip, volume);
            guard.world.ReportAudibleSound(position + Vector3.up * .1f, 2, volume, this);
        }
    }
}
