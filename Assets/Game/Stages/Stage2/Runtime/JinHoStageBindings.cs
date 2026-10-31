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
        // Stage2 인스턴스만 true. HMM의 다른 씬은 기본 false라 기존 3초 주입 지연을 유지한다.
        public bool immediateInjection;
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
        void Start()
        {
            if(!immediateInjection)return;
            // 모든 Awake 이후 한 번만 설정하므로 문이 Awake에서 추가한 SoundEmitter도 포함한다.
            // 실제 Inject/출력/감쇠는 공통 컴포넌트가 담당하며 가짜 소리나 매 프레임 상태 변경은 없다.
            foreach(var emitter in context.SceneComponents<SoundEmitter>())emitter.injectionDelaySeconds=0;
        }
    }
}
