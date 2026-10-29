using UnityEngine;

namespace SoundThief
{
    public sealed class ThiefInteractable : MonoBehaviour
    {
        public InteractionKind kind;
        public string displayName;
        public SoundRelation relation;
        public SoundEmitter emitter;
        public string itemId;
        public bool requiresKey = true;
        public string requiredItemId = "key";
        public float openAngle = -100;
        [Tooltip("0이면 열린 상태를 유지. 양수이면 통과한 뒤 자동으로 닫히는 문.")]
        public float autoCloseSeconds;
        Quaternion closedRotation;
        float closeAt;
        MissionDirector doorMission;
        public string ItemId => string.IsNullOrWhiteSpace(itemId) ? (kind == InteractionKind.Key ? "key" : "objective") : itemId;
        public bool CanUnlock(ThiefPlayer player) => !requiresKey || player.Inventory.Contains(requiredItemId);
        void Awake()
        {
            closedRotation = transform.localRotation;
            if (kind == InteractionKind.Door && !emitter)
                emitter = GetComponent<SoundEmitter>() ?? gameObject.AddComponent<SoundEmitter>();
        }
        public bool IsOpen { get; private set; }
        public bool Acquired { get; private set; }
        void SetDoor(bool open, MissionDirector mission)
        {
            if (relation) relation.MakeNoise(mission.World);
            IsOpen = open;
            transform.localRotation = closedRotation * Quaternion.Euler(0, IsOpen ? openAngle : 0, 0);
            var obstacle = GetComponent<UnityEngine.AI.NavMeshObstacle>();
            if (obstacle) obstacle.enabled = !IsOpen;
            doorMission = mission; closeAt = Time.time + autoCloseSeconds;
        }
        void Update()
        {
            if (kind != InteractionKind.Door || !IsOpen || autoCloseSeconds <= 0 || Time.time < closeAt || !doorMission || !doorMission.CanAct) return;
            // Never swing/carve a door on top of the player or a passing guard.
            if (Vector3.Distance(doorMission.player.transform.position, transform.position) < 2.5f) return;
            foreach (var guard in doorMission.World.Guards)
                if (guard && Vector3.Distance(guard.transform.position, transform.position) < 2.5f) return;
            SetDoor(false, doorMission);
        }
        public void Interact(ThiefPlayer player)
        {
            var mission = player.Mission;
            if (!mission.CanAct || player.IsStunned || Acquired) return;
            switch (kind)
            {
                case InteractionKind.Key:
                case InteractionKind.Treasure:
                case InteractionKind.Collectible:
                    if (relation) relation.MakeNoise(mission.World);
                    player.Inventory.Add(ItemId, displayName); Acquired = true;
                    mission.Notify(displayName + " 획득. " + mission.Objective);
                    gameObject.SetActive(false); break;
                case InteractionKind.Door:
                    if (!CanUnlock(player)) { mission.Notify("이 문에 필요한 아이템이 없습니다."); return; }
                    SetDoor(!IsOpen, mission);
                    if (IsOpen) mission.DoorOpened = true;
                    mission.Notify(relation && relation.IsMuted ? "무음으로 문을 움직였습니다." : "문 소리가 울렸습니다.");
                    break;
                case InteractionKind.Resonator:
                    relation.MakeNoise(mission.World);
                    mission.Notify("E: 이 관계 추출 / 소리를 들고 Q: 이 대상에 주입"); break;
                case InteractionKind.Exit:
                    if (mission.HasObjectives) mission.Complete();
                    else mission.Notify("아직 필요한 목표물을 모두 모으지 못했습니다.");
                    break;
            }
        }
    }
}

