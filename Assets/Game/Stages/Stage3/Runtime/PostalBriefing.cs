using System.Linq;
using TMPro;
using UnityEngine;

namespace SoundThief.PostalDepot
{
    // Only the postal scene uses this adapter. Shared input, objectives and AI stay in the core.
    [DefaultExecutionOrder(1000)]
    public sealed class PostalBriefing : MonoBehaviour
    {
        public MissionDirector mission;
        public const string ManifestId = "postal_route_note";
        public const string LetterId = "postal_unsent_letter";
        public bool RouteKnown => mission.player.Inventory.Contains(ManifestId);
        bool routeKnown, letterFound;
        TMP_FontAsset localFont;

        void Start()
        {
            // OS font reference only: no redistribution of a system font file.
            var names = Font.GetOSInstalledFontNames();
            string family = new[] { "Apple SD Gothic Neo", "Malgun Gothic", "Noto Sans CJK KR", "Arial Unicode MS" }
                .FirstOrDefault(candidate => names.Contains(candidate));
            if (family != null) localFont = TMP_FontAsset.CreateFontAsset(family, "Regular", 40);
            if (localFont)
                foreach (var root in gameObject.scene.GetRootGameObjects())
                    foreach (var label in root.GetComponentsInChildren<TMP_Text>(true)) label.font = localFont;
            mission.player.Inventory.Changed += InventoryChanged;
            mission.Notify("의뢰: 화나서 보낸 편지를 회수하자. 접수대의 열쇠와 작업실의 분류표부터 확인하세요.");
        }

        void InventoryChanged()
        {
            // Defer until LateUpdate so the core pickup message does not overwrite this clue.
            routeKnown = RouteKnown;
            letterFound = mission.player.Inventory.Contains(LetterId);
            pendingMessage = true;
        }
        bool pendingMessage;
        void LateUpdate()
        {
            if (!pendingMessage) return;
            pendingMessage = false;
            var reader = mission.GetComponent<PostalLetterReader>();
            if (reader && reader.SelectedLetter && routeKnown)
                mission.Notify("편지 한 통을 회수했습니다. 의뢰의 수취인과 사연을 다시 대조하세요. 다른 편지로 교체할 수 있습니다.");
            else if (letterFound)
                mission.Notify("편지를 찾았습니다. 작업실의 분류표도 확인해 주소와 일치하는지 대조하세요.");
            else if (routeKnown)
                mission.Notify("분류표 확인: 개인 편지는 발송 대기실 A~C 선반입니다. F로 내용을 읽고, 의뢰와 맞는 한 통을 회수하세요.");
        }
        void OnDestroy()
        {
            if (mission && mission.player) mission.player.Inventory.Changed -= InventoryChanged;
            if (localFont) Destroy(localFont);
        }
    }
}
