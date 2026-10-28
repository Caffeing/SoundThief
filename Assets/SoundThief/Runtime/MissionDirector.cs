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
        public bool Accepted { get; private set; }
        public bool Completed { get; private set; }
        public bool Paused { get; private set; }
        public bool ContractVisible, DoorOpened, Extracted, Injected;
        public string Message { get; private set; } = "저택의 인장을 회수하고 입구로 돌아오세요.";
        public bool CanAct => Accepted && !Completed && !Paused && !restarting;
        bool restarting;
        void Awake() { Time.timeScale = 1; SetCursor(false); }
        public void Accept() { Accepted = true; Notify("의뢰 수락. 왼쪽 받침대에서 열쇠를 얻으세요."); SetCursor(true); }
        public void Notify(string text) => Message = text;
        public void TogglePause()
        {
            if (!Accepted || Completed) return;
            Paused = !Paused; Time.timeScale = Paused ? 0 : 1; SetCursor(!Paused);
        }
        public void Complete()
        {
            if (!player.HasTreasure || !CanAct) return;
            Completed = true; Time.timeScale = 0; SetCursor(false);
            Notify("의뢰 완료 — 인장을 회수하고 무사히 탈출했습니다.");
        }
        public void Caught()
        {
            if (!CanAct) return;
            Notify("경비와 접촉했습니다. 의뢰를 처음부터 다시 시작합니다.");
            Restart();
        }
        public void Restart()
        {
            if (restarting) return;
            restarting = true; Time.timeScale = 1; SetCursor(false);
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
        public string Objective => !Accepted ? "의뢰: 푸른 저택의 인장" : player.HasTreasure ? "입구로 돌아가 F로 탈출"
            : !player.HasKey ? "받침대의 열쇠 획득 [F]" : !Extracted ? "열쇠 ↔ 자물쇠 소리 추출 [E]"
            : !DoorOpened ? "문을 조용히 열기 [F]" : !Injected ? "청록색 공명체에 소리 주입 [Q]" : "저택 안쪽의 가문 인장 획득 [F]";
        static void SetCursor(bool locked) { Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None; Cursor.visible = !locked; }
        void OnDestroy() { Time.timeScale = 1; SetCursor(false); }
    }
}
