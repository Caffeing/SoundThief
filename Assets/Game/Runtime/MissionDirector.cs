using System;
using System.Collections;
using System.Collections.Generic;
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
        public bool isGuild;
        public GuildPractice practice;
        public GuildContractTable contractTable;
        public Transform guildReturnSpawn;
        public bool returnToGuildOnSuccess;
        public string guildScenePath = "Assets/Scenes/Guild.unity";
        static string pendingGuildReturn;
        // Completion history survives scene transitions within this play session.
        static readonly HashSet<string> completedContracts = new HashSet<string>(StringComparer.Ordinal);
        public static bool WasContractCompleted(string scenePath) => !string.IsNullOrEmpty(scenePath) && completedContracts.Contains(scenePath);
        public bool ReturnedToGuild { get; private set; }
        public bool TableOpen => contractTable && contractTable.IsOpen;
        public const string StageScene = "ManorPrototype";
        public string destinationScenePath = "Assets/Scenes/ManorPrototype.unity";
        static string pendingAcceptedScene;
        bool acceptedOnEntry;
        public bool Accepted { get; private set; }
        public bool Completed { get; private set; }
        public bool Paused { get; private set; }
        public bool ContractVisible, DoorOpened, Extracted, Injected;
        public string Message { get; private set; } = "목표물을 회수한 뒤 출구로 돌아오세요.";
        public bool CanAct => (isGuild || Accepted) && !Completed && !Paused && !restarting && !TableOpen;
        public string Title => stage ? stage.title : isGuild ? "푸른 저택의 인장" : "의뢰";
        public string Briefing => stage ? stage.briefing : "목표물을 회수한 뒤 출구로 돌아오세요.";
        public event Action Started, Succeeded, Failed;
        bool restarting;
        string guidanceObjective;
        public void SetGuidanceObjective(string value) { guidanceObjective = value; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetSession() { pendingAcceptedScene = pendingGuildReturn = null; completedContracts.Clear(); }
        void Awake()
        {
            Time.timeScale = 1;
            acceptedOnEntry = !isGuild && pendingAcceptedScene == gameObject.scene.path;
            if (acceptedOnEntry) pendingAcceptedScene = null;
            SetCursor(isGuild || acceptedOnEntry);
        }
        void Start()
        {
            if (isGuild)
            {
                ReturnedToGuild = pendingGuildReturn == gameObject.scene.path;
                if (ReturnedToGuild)
                {
                    pendingGuildReturn = null;
                    if (guildReturnSpawn && player) player.PlaceAt(guildReturnSpawn);
                }
                Notify(ReturnedToGuild ? "의뢰 성공 · 길드로 돌아왔습니다. 의뢰 테이블 [F]에서 다음 의뢰를 확인하세요." : "길드 · 의뢰 테이블 [F] / 뒤쪽 연습장은 선택 / 출구 문 [F] 출발");
                SetCursor(true);
            }
            else
            {
                Message = Briefing;
                if (acceptedOnEntry || (stage ? stage.startImmediately : startsAccepted)) Accept();
            }
        }
        public void Accept()
        {
            if (Accepted || Completed || Paused || restarting || (player && player.IsStunned)) return;
            if (isGuild && contractTable && contractTable.IsRepeatBlocked(destinationScenePath))
            { Notify("이미 완료한 의뢰입니다. 이 의뢰는 재수락 불가능합니다."); return; }
            Accepted = true; ContractVisible = false;
            Notify(isGuild ? "의뢰를 수락했습니다. 연습 없이도 출구 문에 F를 눌러 출발할 수 있습니다." : Briefing);
            SetCursor(true); Started?.Invoke();
        }
        public bool Depart()
        {
            if (!isGuild || !CanAct || !player || player.IsStunned) return false;
            if (contractTable && contractTable.IsRepeatBlocked(destinationScenePath))
            { Notify("이미 완료한 의뢰입니다. 이 의뢰는 재수락 불가능합니다."); return false; }
            if (!Accepted) { Notify("의뢰 테이블에서 의뢰를 먼저 수락하세요. 연습 완료는 필요하지 않습니다."); return false; }
            if (string.IsNullOrEmpty(destinationScenePath) || !Application.CanStreamedLevelBeLoaded(destinationScenePath))
            { Notify("출발 씬을 Build Settings에 등록하세요: " + destinationScenePath); return false; }
            // Acceptance crosses scenes; inventory, charges, and learning records do not.
            pendingAcceptedScene = destinationScenePath;
            restarting = true; Time.timeScale = 1; SetCursor(false);
            SceneManager.LoadScene(destinationScenePath); return true;
        }
        public void Notify(string text) => Message = text;
        public void TogglePause()
        {
            if (TableOpen) { contractTable.Close(); return; }
            if ((!isGuild && !Accepted) || Completed || restarting) return;
            Paused = !Paused; Time.timeScale = Paused ? 0 : 1; SetCursor(!Paused);
        }
        public bool HasObjectives
        {
            get
            {
                if (isGuild || !player) return false;
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
            Completed = true;
            if (!string.IsNullOrEmpty(gameObject.scene.path)) completedContracts.Add(gameObject.scene.path);
            Time.timeScale = 0; SetCursor(false);
            Notify(Title + " — 목표를 달성하고 탈출했습니다."); Succeeded?.Invoke();
            if (returnToGuildOnSuccess) StartCoroutine(ReturnAfterSuccess());
        }
        IEnumerator ReturnAfterSuccess()
        {
            yield return new WaitForSecondsRealtime(0.8f);
            ReturnToGuild();
        }
        public bool ReturnToGuild()
        {
            if (isGuild || !Completed || restarting) return false;
            if (string.IsNullOrEmpty(guildScenePath) || !Application.CanStreamedLevelBeLoaded(guildScenePath))
            { Notify("복귀할 길드 씬을 Build Settings에 등록하세요: " + guildScenePath); return false; }
            pendingGuildReturn = guildScenePath;
            pendingAcceptedScene = null;
            restarting = true; Time.timeScale = 1; SetCursor(false);
            SceneManager.LoadScene(guildScenePath); return true;
        }
        public void Caught()
        {
            if (isGuild || !CanAct) return;
            Notify("경비와 접촉했습니다. 의뢰를 처음부터 다시 시작합니다."); Failed?.Invoke(); Restart();
        }
        public void Restart()
        {
            if (restarting) return;
            if (isGuild)
            {
                Paused = false; Time.timeScale = 1; SetCursor(true);
                contractTable?.Close(); practice?.ResetPractice(); return;
            }
            var scene = gameObject.scene;
            if (string.IsNullOrEmpty(scene.path)) { Notify("재시작하려면 스테이지 씬을 먼저 저장하세요."); return; }
            if (acceptedOnEntry) pendingAcceptedScene = scene.path;
            restarting = true; Time.timeScale = 1; SetCursor(false);
            SceneManager.LoadScene(scene.path);
        }
        public void RecordExtraction() { if (isGuild) practice?.RecordExtraction(); else Extracted = true; }
        public void RecordInjection() { if (isGuild) practice?.RecordInjection(); else Injected = true; }
        public void RecordThrow() { if (isGuild) practice?.RecordThrow(); }
        public string Objective => isGuild
            ? (Accepted ? "출구 문 [F] 출발 · 뒤쪽 연습장은 선택" : "의뢰 테이블에서 문서 보기 [F] · 연습장 자유 이용")
            : !Accepted ? Title : HasObjectives ? (stage ? stage.exitText : "출구에서 F로 탈출")
            : !string.IsNullOrEmpty(guidanceObjective) ? guidanceObjective : stage ? stage.objectiveText : "목표물을 회수하세요.";
        static void SetCursor(bool locked) { Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None; Cursor.visible = !locked; }
        void OnDestroy() { Time.timeScale = 1; SetCursor(false); }
    }
}
