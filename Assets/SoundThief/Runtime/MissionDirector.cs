using UnityEngine;
using UnityEngine.SceneManagement;

namespace SoundThief
{
    public sealed class MissionDirector : MonoBehaviour
    {
        public SoundRules rules;
        public SoundWorld World;
        public ThiefPlayer player;
        public Material projectileMaterial;
        public bool isGuild;
        public GuildPractice practice;
        public const string StageScene = "ManorPrototype";
        static bool enterAccepted;
        bool acceptedOnEntry;
        public bool Accepted { get; private set; }
        public bool Completed { get; private set; }
        public bool Paused { get; private set; }
        public bool ContractVisible, DoorOpened, Extracted, Injected;
        public string Message { get; private set; } = "저택의 인장을 회수하고 입구로 돌아오세요.";
        public bool CanAct => (isGuild || Accepted) && !Completed && !Paused && !restarting;
        bool restarting;
        void Awake()
        {
            Time.timeScale = 1;
            acceptedOnEntry = !isGuild && enterAccepted;
            enterAccepted = false;
            Accepted = acceptedOnEntry;
            SetCursor(isGuild || Accepted);
            if (isGuild) Notify("길드에 오신 것을 환영합니다. 테이블 의뢰 게시판 [F] · 뒤쪽 연습장은 자유롭게 이용하세요.");
        }
        public void Accept()
        {
            if (Accepted || Completed || Paused || restarting || (player && player.IsStunned)) return;
            Accepted = true; ContractVisible = false;
            Notify(isGuild ? "의뢰를 수락했습니다. 연습은 선택사항입니다. 출구 문에서 F로 출발하세요." : "의뢰 수락. 인장을 회수하고 입구로 돌아오세요.");
            SetCursor(true);
        }
        public bool Depart()
        {
            if (!isGuild || !CanAct || player.IsStunned) return false;
            if (!Accepted) { Notify("게시판에서 의뢰를 먼저 수락하세요. 연습 완료는 필요하지 않습니다."); return false; }
            restarting = true; enterAccepted = true;
            SceneManager.LoadScene(StageScene);
            return true;
        }
        public void Notify(string text) => Message = text;
        public void TogglePause()
        {
            if ((!isGuild && !Accepted) || Completed || restarting) return;
            Paused = !Paused; Time.timeScale = Paused ? 0 : 1; SetCursor(!Paused);
        }
        public void Complete()
        {
            if (isGuild || !player.HasTreasure || !CanAct) return;
            Completed = true; Time.timeScale = 0; SetCursor(false);
            Notify("의뢰 완료 — 인장을 회수하고 무사히 탈출했습니다.");
        }
        public void Caught()
        {
            if (isGuild || !CanAct) return;
            Notify("경비와 접촉했습니다. 의뢰를 처음부터 다시 시작합니다.");
            Restart();
        }
        public void Restart()
        {
            if (restarting) return;
            enterAccepted = !isGuild && acceptedOnEntry;
            restarting = true; Time.timeScale = 1; SetCursor(false);
            SceneManager.LoadScene(gameObject.scene.name);
        }
        public void RecordExtraction() { if (isGuild) practice?.RecordExtraction(); else Extracted = true; }
        public void RecordInjection() { if (isGuild) practice?.RecordInjection(); else Injected = true; }
        public void RecordThrow() { if (isGuild) practice?.RecordThrow(); }
        public string Objective => isGuild ? (!Accepted ? "테이블 의뢰 게시판에서 수락 [F]" : "출구 문에서 출발 [F] · 뒤쪽 연습은 선택")
            : !Accepted ? "의뢰: 푸른 저택의 인장" : player.HasTreasure ? "입구로 돌아가 F로 탈출"
            : !player.HasKey ? "받침대의 열쇠 획득 [F]" : !Extracted ? "열쇠 ↔ 자물쇠 소리 추출 [E]"
            : !DoorOpened ? "문을 조용히 열기 [F]" : !Injected ? "청록색 공명체에 소리 주입 [Q]" : "저택 안쪽의 가문 인장 획득 [F]";
        static void SetCursor(bool locked) { Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None; Cursor.visible = !locked; }
        void OnDestroy() { Time.timeScale = 1; SetCursor(false); }
    }
}
