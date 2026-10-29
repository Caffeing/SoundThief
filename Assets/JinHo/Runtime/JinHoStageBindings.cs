using System;
using UnityEngine;

namespace SoundThief.JinHo
{
    [DefaultExecutionOrder(-50)]
    public sealed class JinHoStageBindings : MonoBehaviour
    {
        public StageContext context;
        public JinHoSoundPropagation propagation;
        public GuardAgent guard1, guard2;
        public bool traceLifecycle;
        SoundWorld world;
        public bool Ready { get; private set; }

        void OnEnable()
        {
            if (!Application.isPlaying) return;
            if (!context || !context.mission || !context.mission.World || !propagation || !guard1 || !guard2)
                throw new InvalidOperationException("JinHo bindings are incomplete.");
            if (context.gameObject.scene != gameObject.scene || propagation.gameObject.scene != gameObject.scene
                || guard1.gameObject.scene != gameObject.scene || guard2.gameObject.scene != gameObject.scene)
                throw new InvalidOperationException("JinHo bindings must stay within one scene.");
            var errors = propagation.Validate();
            if (errors.Count != 0) throw new InvalidOperationException(string.Join("\n", errors));
            world = context.mission.World;
            if (!world.RegisterPropagationPolicy(propagation)) throw new InvalidOperationException("A different sound policy is already registered.");
            guard1.startsAwake = false; guard1.stayAwakeAfterFirstWake = true;
            guard2.startsAwake = true; guard2.stayAwakeAfterFirstWake = true;
            Ready = true;
            if (traceLifecycle) Debug.Log("JINHO_BOUND: original World/Guard/Player/HUD/Mission types; one propagation policy.", this);
        }
        void OnDisable()
        {
            if (world && propagation) world.UnregisterPropagationPolicy(propagation);
            Ready = false;
        }
    }
}
