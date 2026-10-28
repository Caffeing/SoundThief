using UnityEngine;

namespace SoundThief
{
    // Learning records are deliberately independent of contract and ability ownership.
    public sealed class GuildPractice : MonoBehaviour
    {
        public MissionDirector mission;
        public Transform practiceRoot;
        public bool Extracted { get; private set; }
        public bool Injected { get; private set; }
        public bool Thrown { get; private set; }
        public string Guidance => !Extracted ? "선택 연습 · 공명체 조준 → E 추출 · 운반 중 느린 이동과 타이머 확인"
            : !Injected ? "선택 연습 · 다른 공명체에 Q 주입 · 등급이 0이 되면 원래 관계 복구"
            : !Thrown ? "선택 연습 · 다시 추출한 뒤 좌클릭 투척 · 벽은 소리만 통과, 기절은 차단"
            : "기본 연습 완료 · 반복 연습 또는 출구로 출발 · 초기화 받침대 [F]";
        public void RecordExtraction() => Extracted = true;
        public void RecordInjection() => Injected = true;
        public void RecordThrow() => Thrown = true;
        public void ResetPractice()
        {
            if (!mission.isGuild || !mission.CanAct || mission.player.IsStunned) return;
            mission.player.ResetPracticeState();
            foreach (var projectile in FindObjectsByType<SoundProjectile>(FindObjectsSortMode.None))
                if (projectile.World == mission.World) projectile.Cancel();
            foreach (var emitter in practiceRoot.GetComponentsInChildren<SoundEmitter>(true)) emitter.ResetOutput();
            foreach (var relation in practiceRoot.GetComponentsInChildren<SoundRelation>(true)) relation.Restore();
            foreach (var guard in practiceRoot.GetComponentsInChildren<GuardAgent>(true)) guard.ResetPractice();
            mission.World.ResetFeedback();
            Extracted = Injected = Thrown = false;
            mission.Notify("연습장을 초기화했습니다. 수락한 의뢰는 유지됩니다.");
        }
    }
}
