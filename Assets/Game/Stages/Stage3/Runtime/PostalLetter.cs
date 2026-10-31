using UnityEngine;

namespace SoundThief.PostalDepot
{
    [RequireComponent(typeof(ThiefInteractable))]
    public sealed class PostalLetter : MonoBehaviour, IThiefInteractionHandler
    {
        public string letterId, recipient, sender;
        [TextArea(6, 14)] public string body;
        public bool isObjective;
        public bool WasRead { get; internal set; }
        public bool TryInteract(ThiefPlayer player)
        {
            var reader = player.Mission.GetComponent<PostalLetterReader>();
            if (reader) reader.Read(this, player);
            else player.Mission.Notify("편지 읽기 연결을 확인하세요.");
            return true;
        }
        public string InventoryId => isObjective ? PostalBriefing.LetterId : "postal_letter_" + letterId;
        public string Label => "회수한 편지 #" + letterId.PadLeft(2, '0') + " · " + recipient + " / " + sender;
    }
}
