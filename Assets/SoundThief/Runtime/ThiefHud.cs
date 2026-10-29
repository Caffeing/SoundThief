using UnityEngine;
using TMPro;

namespace SoundThief
{
    public sealed class ThiefHud : MonoBehaviour
    {
        public MissionDirector mission;
        TMP_FontAsset font;
        TextMeshProUGUI title, objective, prompt, inventory, held, message, contractText;
        UnityEngine.UI.Image timer;
        GameObject modal;
        void Start()
        {
            // Use the local OS font without copying or redistributing a system font file.
            font = TMP_FontAsset.CreateFontAsset("Malgun Gothic", "Regular", 40);
            if (!font) font = TMP_FontAsset.CreateFontAsset("Arial", "Regular", 40);
            var canvas = gameObject.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900); scaler.matchWidthOrHeight = 0.5f;
            gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            var top = Panel("Contract header", transform, new Vector2(0.025f, 0.80f), new Vector2(0.43f, 0.96f));
            title = Text(top, "SOUND THIEF  /  소리 도둑", 23, new Vector2(0.04f, 0.65f), new Vector2(0.96f, 0.95f));
            title.color = new Color(0.3f, 1, 0.85f);
            objective = Text(top, "", 23, new Vector2(0.04f, 0.14f), new Vector2(0.96f, 0.60f));
            var bottom = Panel("Sound and items", transform, new Vector2(0.025f, 0.03f), new Vector2(0.57f, 0.19f));
            held = Text(bottom, "", 21, new Vector2(0.035f, 0.48f), new Vector2(0.97f, 0.92f));
            var track = Panel("Timer track", bottom, new Vector2(0.035f, 0.39f), new Vector2(0.96f, 0.43f));
            var bar = Panel("Time remaining", track, Vector2.zero, Vector2.one);
            timer = bar.GetComponent<UnityEngine.UI.Image>(); timer.color = new Color(0.15f, 0.95f, 0.78f);
            inventory = Text(bottom, "", 19, new Vector2(0.035f, 0.04f), new Vector2(0.97f, 0.30f));
            var controls = Panel("Controls", transform, new Vector2(0.68f, 0.03f), new Vector2(0.975f, 0.20f));
            Text(controls, "WASD 이동 · 마우스 시점\nF 상호작용 · E 추출 · Q 주입\n" + (mission.rules.throwingEnabled ? "좌클릭 투척 · " : "") + "Ctrl 웅크리기\nTab 의뢰 · Esc 정지 · R 재시작", 18, new Vector2(0.05f, 0.08f), new Vector2(0.96f, 0.93f));
            var cross = Text(transform, "+", 27, new Vector2(0.48f, 0.48f), new Vector2(0.52f, 0.52f)); cross.alignment = TextAlignmentOptions.Center;
            prompt = Text(transform, "", 22, new Vector2(0.22f, 0.30f), new Vector2(0.78f, 0.44f)); prompt.alignment = TextAlignmentOptions.Center;
            message = Text(transform, "", 19, new Vector2(0.16f, 0.21f), new Vector2(0.84f, 0.28f)); message.alignment = TextAlignmentOptions.Center;
            modal = Panel("Contract panel", transform, new Vector2(0.21f, 0.27f), new Vector2(0.79f, 0.77f)).gameObject;
            contractText = Text(modal.transform, "", 26, new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.92f));
            foreach (var guard in FindObjectsByType<GuardAgent>(FindObjectsSortMode.None))
                if (guard.gameObject.scene == gameObject.scene && guard.statusLabel) guard.statusLabel.font = font;
        }
        void Update()
        {
            if (!mission || !objective) return;
            var player = mission.player;
            title.text = mission.isGuild ? "GUILD / 길드 · 선택형 연습장" : "SOUND THIEF / " + mission.Title;
            objective.text = mission.Objective;
            inventory.text = "아이템  |  " + (player.Inventory.Items.Count == 0 ? "없음" : string.Join(" · ", System.Linq.Enumerable.Select(player.Inventory.Items, item => item.label)));
            message.text = mission.isGuild && mission.practice && mission.practice.IsPlayerInside ? mission.Message + "\n" + mission.practice.Guidance : mission.Message;
            var charge = player.Held;
            held.text = player.IsStunned ? "기절 — 이동과 능력을 사용할 수 없습니다" : charge == null ? "빈손  /  소리 관계를 찾아보세요"
                : "운반 · 강제 웅크리기   등급 " + charge.Grade + "   " + charge.Remaining.ToString("0.0") + "초";
            timer.rectTransform.anchorMax = new Vector2(charge == null ? 0 : charge.Remaining / mission.rules.HoldTime(charge.Grade), 1);
            timer.color = charge != null && charge.Remaining < 3 ? new Color(1, 0.3f, 0.2f) : new Color(0.15f, 0.95f, 0.78f);
            var target = player.Target;
            prompt.text = !target ? "" : target.displayName + "  [F]";
            if (target && target.relation)
                prompt.text += "\n" + target.relation.Label + " · 등급 " + target.relation.grade + (target.relation.IsMuted ? " · 무음 / 복구 대기" : " · [E] 추출");
            if (target && target.emitter)
                prompt.text += target.emitter.Active ? "\n주입 출력 · 등급 " + target.emitter.CurrentGrade : charge != null ? "\n[Q] 소리 주입" : "";
            modal.SetActive((!mission.isGuild && !mission.Accepted) || mission.Paused || mission.Completed || mission.ContractVisible);
            contractText.text = mission.Completed ? "의뢰 완료\n\n" + mission.Title + "\n목표물을 회수하고 탈출했습니다.\n\n[R] 의뢰 처음부터 다시 하기"
                : mission.Paused ? "일시정지\n\n소리 보유 시간과 경비 행동이 멈췄습니다.\n\n[Esc] 계속    [R] 전체 재시작"
                : mission.Title + "\n\n" + mission.Briefing + "\n\n" + (mission.isGuild ? (!mission.Accepted ? "게시판 [F] / [Enter] 수락 · [Tab] 닫기\n연습은 선택 · 출구 문 [F]로 출발" : "출구 문 [F]로 출발 · 연습 완료 불필요\n[Tab] 닫기 · 시간은 흐릅니다") : !mission.Accepted ? "[Enter / F] 의뢰 시작" : "[Tab] 닫기 · 의뢰창을 열어도 시간은 흐릅니다");
        }
        static RectTransform Panel(string name, Transform parent, Vector2 min, Vector2 max)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image)); obj.transform.SetParent(parent, false);
            var rect = obj.GetComponent<RectTransform>(); rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero;
            var image = obj.GetComponent<UnityEngine.UI.Image>(); image.color = new Color(0.025f, 0.045f, 0.09f, 0.94f); image.raycastTarget = false;
            return rect;
        }
        TextMeshProUGUI Text(Transform parent, string value, int size, Vector2 min, Vector2 max)
        {
            var obj = new GameObject("Label", typeof(RectTransform)); obj.transform.SetParent(parent, false);
            var text = obj.AddComponent<TextMeshProUGUI>(); text.font = font; text.text = value; text.fontSize = size; text.color = new Color(0.91f, 0.95f, 1);
            text.enableAutoSizing = true; text.fontSizeMin = size * 0.7f; text.fontSizeMax = size; text.raycastTarget = false;
            var rect = text.rectTransform; rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero;
            return text;
        }
        void OnDestroy() { if (font) Destroy(font); }
    }
}
