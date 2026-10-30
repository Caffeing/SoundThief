using System;
using UnityEngine;

namespace SoundThief
{
    [Serializable]
    public sealed class GuildContractOffer
    {
        public StageDefinition definition;
        public string title = "준비 중인 의뢰";
        [TextArea] public string description = "새로운 의뢰를 준비하고 있습니다.";
        public string scenePath;
        public bool available;
        public string Title => definition ? definition.title : title;
        public string Description => definition ? definition.briefing : description;
        public bool CanAccept => available && definition && !string.IsNullOrWhiteSpace(scenePath)
            && Application.CanStreamedLevelBeLoaded(scenePath);
    }

    // Owns browsing state only. Accepting a document never changes the scene.
    public sealed class GuildContractTable : MonoBehaviour
    {
        public MissionDirector mission;
        public GuildContractOffer[] offers = Array.Empty<GuildContractOffer>();
        public bool IsOpen { get; private set; }
        public int SelectedIndex { get; private set; }
        public int AcceptedIndex { get; private set; } = -1;
        public int Count => offers == null ? 0 : offers.Length;
        public bool IsTurning { get; private set; }
        public int TurnDirection { get; private set; }
        public float TurnProgress { get; private set; }
        // Left removes the current front sheet; right brings the last sheet to the front.
        public int IncomingIndex => IsTurning ? nextIndex : SelectedIndex;
        public int DisplayIndex => IsTurning && TurnDirection < 0 ? nextIndex : SelectedIndex;
        public GuildContractOffer DisplayedOffer => Count == 0 ? null : offers[DisplayIndex];
        public GuildContractOffer CurrentOffer => Count == 0 ? null : offers[SelectedIndex];
        int nextIndex;
        const float TurnDuration = 0.92f;

        public bool Open()
        {
            if (!mission || !mission.isGuild || !mission.CanAct || !mission.player || mission.player.IsStunned || Count == 0) return false;
            IsOpen = true; CancelTurn();
            mission.ContractVisible = false;
            mission.Notify("← → 의뢰 넘기기 · F / Enter 수락 · ESC 테이블 나가기");
            return true;
        }
        public void Close()
        {
            IsOpen = false; CancelTurn();
            if (mission) mission.Notify(mission.Accepted ? "수락한 의뢰는 유지됩니다. 출구 문 [F]로 출발하세요." : "의뢰 테이블 [F] · 연습장은 자유롭게 이용하세요.");
        }
        public bool Turn(int direction)
        {
            if (!IsOpen || IsTurning || direction == 0 || Count < 2) return false;
            int target = (SelectedIndex - Math.Sign(direction) + Count) % Count;
            nextIndex = target; TurnDirection = Math.Sign(direction); TurnProgress = 0; IsTurning = true;
            return true;
        }
        public bool AcceptCurrent()
        {
            if (!IsOpen || IsTurning || !mission || mission.Paused || mission.player.IsStunned) return false;
            var offer = CurrentOffer;
            if (offer == null || !offer.CanAccept) { mission.Notify("이 문서는 준비 중입니다. 현재 수행 가능한 의뢰만 수락할 수 있습니다."); return false; }
            if (mission.Accepted)
            {
                mission.Notify(AcceptedIndex == SelectedIndex ? "이미 수락한 의뢰입니다. ESC로 닫고 출구로 이동하세요." : "이미 수락한 의뢰가 있습니다. 현재 의뢰를 먼저 수행하세요.");
                return false;
            }
            mission.stage = offer.definition; mission.destinationScenePath = offer.scenePath;
            mission.Accept();
            if (!mission.Accepted) return false;
            AcceptedIndex = SelectedIndex;
            mission.Notify("의뢰 수락 완료 · ESC로 테이블을 닫고 출구 문 [F]로 출발하세요.");
            return true;
        }
        void Update()
        {
            if (IsOpen && (!mission || !mission.player || mission.player.IsStunned)) { Close(); return; }
            if (!IsTurning) return;
            TurnProgress = Mathf.Min(1, TurnProgress + Time.unscaledDeltaTime / TurnDuration);
            if (TurnProgress < 1) return;
            SelectedIndex = nextIndex; IsTurning = false;
        }
        void CancelTurn() { IsTurning = false; TurnProgress = 0; TurnDirection = 0; nextIndex = SelectedIndex; }
        void OnDisable() { IsOpen = false; CancelTurn(); }
    }
}
