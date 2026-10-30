using UnityEngine;
using UnityEngine.AI;

namespace SoundThief
{
    // Optional presentation layer: the guard's sensing and movement stay in GuardAgent.
    public sealed class GuardAppearance : MonoBehaviour
    {
        public Animator animator;
        public bool relaxedArms;
        readonly Transform[] upperArms = new Transform[2], forearms = new Transform[2], hands = new Transform[2];
        GuardAgent guard;
        NavMeshAgent agent;
        int current = -1;
        static readonly int Idle = Animator.StringToHash("Idle");
        static readonly int Walk = Animator.StringToHash("Walk");

        void Awake()
        {
            guard = GetComponent<GuardAgent>();
            agent = GetComponent<NavMeshAgent>();
            if (animator) foreach (var bone in animator.GetComponentsInChildren<Transform>())
            {
                int side = bone.name.EndsWith(".L") ? 0 : 1;
                if (bone.name.EndsWith("upper_arm.L") || bone.name.EndsWith("upper_arm.R")) upperArms[side] = bone;
                else if (bone.name.EndsWith("forearm.L") || bone.name.EndsWith("forearm.R")) forearms[side] = bone;
                else if (bone.name.EndsWith("hand.L") || bone.name.EndsWith("hand.R")) hands[side] = bone;
            }
        }
        void LateUpdate()
        {
            if (!relaxedArms || !animator || !animator.enabled) return;
            float movement = agent && agent.isOnNavMesh ? Mathf.Clamp01(agent.velocity.magnitude / 2) : 0;
            for (int i = 0; i < 2; i++)
            {
                if (!upperArms[i] || !forearms[i] || !hands[i]) continue;
                float side = i == 0 ? -1 : 1;
                float swing = Mathf.Sin(Time.time * 6.5f + i * Mathf.PI) * .23f * movement;
                var upperDirection = transform.TransformDirection(new Vector3(side * .16f, -1, swing));
                upperArms[i].rotation = Quaternion.FromToRotation(forearms[i].position - upperArms[i].position, upperDirection) * upperArms[i].rotation;
                var lowerDirection = transform.TransformDirection(new Vector3(side * .025f, -1, .13f + swing * .5f));
                forearms[i].rotation = Quaternion.FromToRotation(hands[i].position - forearms[i].position, lowerDirection) * forearms[i].rotation;
            }
        }

        void Update()
        {
            if (!animator || !guard || !agent) return;
            bool moving = guard.State != GuardState.Sleeping && guard.State != GuardState.Stunned
                && guard.State != GuardState.Panic && agent.isOnNavMesh && agent.velocity.sqrMagnitude > 0.01f;
            int state = moving ? Walk : Idle;
            if (state == current) return;
            animator.CrossFade(state, 0.15f);
            current = state;
        }
    }
}
