using UnityEngine;
namespace SoundThief
{
    public sealed class SoundEmitter : MonoBehaviour
    {
        // Stage2 한정 복원용 선택 설정: 기존 HMM 씬의 기본 주입 대기 3초는 유지한다.
        // Stage2의 SoundEmitter 인스턴스에만 0을 지정하면 원래 JinHo처럼 별도 대기 없이 출력을 시작한다.
        // 씬 판정으로 공통 동작을 바꾸지 않으며, 등급 감쇠 시간은 실제 출력 시작 시각부터 그대로 계산한다.
        [Min(0), Tooltip("주입 후 출력까지 대기하는 초. 기본 3초, 원래 JinHo 동작을 복원할 Stage2 인스턴스만 0초.")]
        public float injectionDelaySeconds = 3f;
        public bool Active => charge != null && !charge.Finished;
        public bool Pending => Active && Time.time < started;
        public int CurrentGrade { get; private set; }
        public AudioClip SoundClip => charge != null && charge.Source ? charge.Source.soundClip : null;
        SoundCharge charge;
        SoundWorld world;
        float started, nextPulse;
        public bool Inject(SoundCharge value, SoundWorld soundWorld)
        {
            if (Active || value == null || value.Finished) return false;
            charge = value; world = soundWorld; started = Time.time + Mathf.Max(0, injectionDelaySeconds); nextPulse = started;
            CurrentGrade = value.Grade;
            return true;
        }
        void Update()
        {
            if (!Active) return;
            if (Pending) return;
            CurrentGrade = charge.Grade - Mathf.FloorToInt(Mathf.Max(0, Time.time - started - world.rules.injectionHold) / world.rules.decayStep);
            if (CurrentGrade <= 0) { charge.Finish(); CurrentGrade = 0; return; }
            if (Time.time >= nextPulse) { nextPulse = Time.time + world.rules.pulseInterval; world.Pulse(transform.position, CurrentGrade, this); }
        }
        public void ResetOutput() { charge?.Finish(); charge = null; CurrentGrade = 0; }
        void OnDestroy() { ResetOutput(); }
    }

}

