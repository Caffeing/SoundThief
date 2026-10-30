using UnityEngine;
namespace SoundThief
{
    public sealed class SoundEmitter : MonoBehaviour
    {
        public bool Active => charge != null && !charge.Finished;
        public int CurrentGrade { get; private set; }
        SoundCharge charge;
        SoundWorld world;
        float started, nextPulse;
        public bool Inject(SoundCharge value, SoundWorld soundWorld)
        {
            if (Active || value == null || value.Finished) return false;
            charge = value; world = soundWorld; started = Time.time; nextPulse = 0;
            CurrentGrade = value.Grade;
            return true;
        }
        void Update()
        {
            if (!Active) return;
            CurrentGrade = charge.Grade - Mathf.FloorToInt(Mathf.Max(0, Time.time - started - world.rules.injectionHold) / world.rules.decayStep);
            if (CurrentGrade <= 0) { charge.Finish(); CurrentGrade = 0; return; }
            if (Time.time >= nextPulse) { nextPulse = Time.time + world.rules.pulseInterval; world.Pulse(transform.position, CurrentGrade, this); }
        }
        public void ResetOutput() { charge?.Finish(); charge = null; CurrentGrade = 0; }
        void OnDestroy() { ResetOutput(); }
    }

}

