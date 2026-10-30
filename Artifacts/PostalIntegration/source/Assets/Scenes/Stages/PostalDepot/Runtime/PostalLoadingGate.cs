using UnityEngine;
using UnityEngine.AI;

namespace SoundThief.PostalDepot
{
    // The postal F adapter owns this one-way release without changing shared doors.
    [RequireComponent(typeof(ThiefInteractable))]
    public sealed class PostalLoadingGate : MonoBehaviour
    {
        public bool IsOpen { get; private set; }
        public void Interact(ThiefPlayer player)
        {
            if (!player || !player.Mission.CanAct || player.IsStunned || IsOpen) return;
            if (player.transform.position.z <= transform.position.z + .25f)
            {
                player.Mission.Notify("하역문은 안쪽에서만 열 수 있습니다. 왼쪽 접수실로 들어가세요.");
                return;
            }
            IsOpen = true;
            foreach (var collider in GetComponentsInChildren<Collider>()) collider.enabled = false;
            var obstacle = GetComponent<NavMeshObstacle>();
            if (obstacle) obstacle.enabled = false;
            transform.position += Vector3.up * 4;
            player.Mission.Notify("하역문을 열었습니다. 이제 앞마당으로 바로 돌아갈 수 있습니다.");
        }
    }
}
