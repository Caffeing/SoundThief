using UnityEngine;

namespace SoundThief
{
    public sealed class ThiefInteractable : MonoBehaviour
    {
        public InteractionKind kind;
        public string displayName;
        public SoundRelation relation;
        public SoundEmitter emitter;
        public bool IsOpen { get; private set; }
        public bool Acquired { get; private set; }
        public void Interact(ThiefPlayer player)
        {
            var mission = player.Mission;
            if (!mission.CanAct || player.IsStunned || Acquired) return;
            switch (kind)
            {
                case InteractionKind.Key:
                    player.HasKey = true; Acquired = true;
                    mission.Notify("열쇠를 얻었습니다. 문을 조준하고 E로 관계의 소리를 추출하세요.");
                    gameObject.SetActive(false); break;
                case InteractionKind.Door:
                    if (!player.HasKey) { mission.Notify("문 옆 받침대에서 열쇠를 먼저 챙기세요."); return; }
                    relation.MakeNoise(mission.World);
                    IsOpen = !IsOpen;
                    transform.localRotation = Quaternion.Euler(0, IsOpen ? -100 : 0, 0);
                    var obstacle = GetComponent<UnityEngine.AI.NavMeshObstacle>();
                    if (obstacle) obstacle.enabled = !IsOpen;
                    if (IsOpen) mission.DoorOpened = true;
                    mission.Notify(relation.IsMuted ? "무음으로 문을 열었습니다. 청록색 공명체에 Q로 주입하세요." : "금속음이 울렸습니다. 잠든 경비에게도 소리가 들립니다.");
                    break;
                case InteractionKind.Resonator:
                    relation.MakeNoise(mission.World);
                    mission.Notify("E: 이 관계 추출 / 소리를 들고 Q: 이 대상에 주입"); break;
                case InteractionKind.Treasure:
                    player.HasTreasure = true; Acquired = true;
                    mission.Notify("가문의 인장을 확보했습니다. 입구로 돌아가 밖으로 탈출하세요.");
                    gameObject.SetActive(false); break;
                case InteractionKind.Exit:
                    if (player.HasTreasure) mission.Complete();
                    else mission.Notify("아직 의뢰 물건이 없습니다. 저택 안쪽의 인장을 찾으세요.");
                    break;
            }
        }
    }
}

