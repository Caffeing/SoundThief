using System;
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
        public StageDefinition stage;
        public bool startsAccepted;
        public bool Accepted { get; private set; }
        public bool Completed { get; private set; }
        public bool Paused { get; private set; }
        public bool ContractVisible, DoorOpened, Extracted, Injected;
        public string Message { get; private set; } = "목표물을 회수한 뒤 출구로 돌아오세요.";
        public bool CanAct => Accepted && !Completed && !Paused && !restarting;
        public string Title => stage ? stage.title : "의뢰";
        public string Briefing => stage ? stage.briefing : "목표물을 회수한 뒤 출구로 돌아오세요.";
        public event Action Started, Succeeded, Failed;
        bool restarting;
        string guidanceObjective;
        public void SetGuidanceObjective(string value) { guidanceObjective = value; }
        void Awake() { Time.timeScale = 1; SetCursor(false); }
        void Start()
        {
            Message = Briefing;
            if (stage ? stage.startImmediately : startsAccepted) Accept();
        }
        public void Accept()
        {
            if (Accepted || Completed || Paused || restarting) return;
            Accepted = true; Notify(Briefing); SetCursor(true); Started?.Invoke();
        }
        public void Notify(string text) => Message = text;
        public void TogglePause()
        {
            if (!Accepted || Completed) return;
            Paused = !Paused; Time.timeScale = Paused ? 0 : 1; SetCursor(!Paused);
        }
        public bool HasObjectives
        {
            get
            {
                if (!player) return false;
                if (!stage) return player.HasTreasure;
                if (stage.requiredItemIds == null) return false;
                foreach (var id in stage.requiredItemIds)
                    if (!player.Inventory.Contains(id)) return false;
                return true;
            }
        }
        public void Complete()
        {
            if (!HasObjectives || !CanAct) return;
            Completed = true; Time.timeScale = 0; SetCursor(false);
            Notify(Title + " — 목표를 달성하고 탈출했습니다."); Succeeded?.Invoke();
        }
        public void Caught()
        {
            if (!CanAct) return;
            Notify("경비와 접촉했습니다. 의뢰를 처음부터 다시 시작합니다."); Failed?.Invoke(); Restart();
        }
        public void Restart()
        {
            if (restarting) return;
            var scene = gameObject.scene;
            if (string.IsNullOrEmpty(scene.path)) { Notify("재시작하려면 스테이지 씬을 먼저 저장하세요."); return; }
            restarting = true; Time.timeScale = 1; SetCursor(false);
            SceneManager.LoadScene(scene.path);
        }
        public string Objective => !Accepted ? Title : HasObjectives ? (stage ? stage.exitText : "출구에서 F로 탈출")
            : !string.IsNullOrEmpty(guidanceObjective) ? guidanceObjective : stage ? stage.objectiveText : "목표물을 회수하세요.";
        static void SetCursor(bool locked) { Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None; Cursor.visible = !locked; }
        void OnDestroy() { Time.timeScale = 1; SetCursor(false); }
    }
}
