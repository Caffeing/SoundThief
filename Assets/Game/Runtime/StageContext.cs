using System.Collections.Generic;
using UnityEngine;

namespace SoundThief
{
    // Scene-local composition root. No persistent singleton or cross-scene searches.
    [DefaultExecutionOrder(-100)]
    public sealed class StageContext : MonoBehaviour
    {
        public StageDefinition definition;
        public SoundRules rules;
        public MissionDirector mission;
        public ThiefPlayer player;
        public ThiefHud hud;

        public T[] SceneComponents<T>() where T : Component
        {
            var result = new List<T>();
            foreach (var root in gameObject.scene.GetRootGameObjects()) result.AddRange(root.GetComponentsInChildren<T>(true));
            return result.ToArray();
        }
        public void Bind()
        {
            if (!mission || !player || !hud || !mission.World) return;
            mission.stage = definition; mission.rules = rules; mission.player = player;
            mission.World.rules = rules; mission.World.player = player;
            player.Mission = mission; hud.mission = mission;
            foreach (var guard in SceneComponents<GuardAgent>()) guard.world = mission.World;
        }
        void Awake() { Bind(); }
        public List<string> Validate()
        {
            var errors = new List<string>();
            if (SceneComponents<StageContext>().Length != 1) errors.Add("씬에는 StageContext가 정확히 하나 필요합니다.");
            if (SceneComponents<ThiefPlayer>().Length != 1) errors.Add("플레이어가 정확히 하나 필요합니다.");
            if (SceneComponents<MissionDirector>().Length != 1) errors.Add("MissionDirector가 정확히 하나 필요합니다.");
            if (!definition) errors.Add("StageDefinition을 지정하세요.");
            else
            {
                if (string.IsNullOrWhiteSpace(definition.stageId)) errors.Add("stageId가 비어 있습니다.");
                if (string.IsNullOrWhiteSpace(definition.title)) errors.Add("의뢰 제목이 비어 있습니다.");
                var ids = new HashSet<string>();
                if (definition.requiredItemIds == null) errors.Add("필수 아이템 목록이 없습니다.");
                else foreach (var id in definition.requiredItemIds)
                {
                    if (string.IsNullOrWhiteSpace(id) || !ids.Add(id)) { errors.Add("필수 아이템 ID가 비었거나 중복됩니다."); continue; }
                    bool found = false;
                    foreach (var item in SceneComponents<ThiefInteractable>())
                        if (IsCollectible(item) && item.ItemId == id) found = true;
                    if (!found) errors.Add("목표 아이템이 씬에 없습니다: " + id);
                }
            }
            if (!rules) errors.Add("공통 SoundRules를 지정하세요.");
            else if (rules.holdSeconds == null || rules.holdSeconds.Length < 3 || System.Array.Exists(rules.holdSeconds, time => time <= 0)
                || rules.decayStep <= 0 || rules.pulseInterval <= 0) errors.Add("소리 등급 시간·감쇠·출력 간격은 양수여야 합니다.");
            if (!mission || !mission.World || !mission.projectileMaterial) errors.Add("MissionDirector의 World/투척 재질 참조가 필요합니다.");
            if (!player || !player.view || !player.GetComponent<CharacterController>()) errors.Add("플레이어·카메라·CharacterController가 필요합니다.");
            if (!hud) errors.Add("HUD가 필요합니다.");
            if (SceneComponents<AudioListener>().Length != 1) errors.Add("AudioListener는 정확히 하나 필요합니다. 기본 카메라의 중복 여부를 확인하세요.");
            foreach (var relation in SceneComponents<SoundRelation>())
                if (relation.grade < 1 || relation.grade > 3) errors.Add(relation.name + ": 소리 등급은 1~3이어야 합니다.");
            foreach (var guard in SceneComponents<GuardAgent>())
                if (!UnityEngine.AI.NavMesh.SamplePosition(guard.transform.position, out _, 1, UnityEngine.AI.NavMesh.AllAreas))
                    errors.Add(guard.name + ": 경비 위치에 구운 NavMesh가 없습니다.");
            if ((mission && mission.gameObject.scene != gameObject.scene) || (player && player.gameObject.scene != gameObject.scene)
                || (hud && hud.gameObject.scene != gameObject.scene)) errors.Add("공통 참조는 같은 씬에 있어야 합니다.");
            bool exit = false;
            foreach (var item in SceneComponents<ThiefInteractable>())
            {
                if (item.kind == InteractionKind.Exit) exit = true;
                if (item.GetComponentsInChildren<Collider>().Length == 0) errors.Add(item.name + ": 상호작용 Collider가 없습니다.");
                if (item.kind == InteractionKind.Resonator && (!item.relation || !item.emitter)) errors.Add(item.name + ": 관계와 출력기가 필요합니다.");
                if (item.kind == InteractionKind.Door && item.requiresKey)
                {
                    bool key = false;
                    foreach (var source in SceneComponents<ThiefInteractable>()) if (IsCollectible(source) && source.ItemId == item.requiredItemId) key = true;
                    if (!key) errors.Add(item.name + ": 문에 필요한 아이템이 없습니다: " + item.requiredItemId);
                }
            }
            if (!exit) errors.Add("출구 상호작용이 필요합니다.");
            return errors;
        }
        static bool IsCollectible(ThiefInteractable item) => item.kind == InteractionKind.Key || item.kind == InteractionKind.Treasure || item.kind == InteractionKind.Collectible;
    }
}
