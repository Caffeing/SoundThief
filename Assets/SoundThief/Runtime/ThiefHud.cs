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
        GameObject modal, pausePanel, timerTrack, objectivePanel, statusPanel;
        TextMeshProUGUI pauseObjective;
        string lastMessage, lastObjective;
        float messageUntil, objectiveUntil;
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
            var top = Panel("Contract header", transform, new Vector2(0.025f, 0.865f), new Vector2(0.34f, 0.965f));
            objectivePanel = top.gameObject;
            Frame(top);
            title = Text(top, "현재 의뢰", 18, new Vector2(0.045f, 0.62f), new Vector2(0.96f, 0.96f));
            title.color = new Color(0.93f, 0.72f, 0.38f);
            objective = Text(top, "", 22, new Vector2(0.045f, 0.08f), new Vector2(0.96f, 0.62f));
            var bottom = Panel("Sound and items", transform, new Vector2(0.35f, 0.025f), new Vector2(0.65f, 0.135f));
            statusPanel = bottom.gameObject;
            held = Text(bottom, "", 19, new Vector2(0.045f, 0.53f), new Vector2(0.96f, 0.95f));
            var track = Panel("Timer track", bottom, new Vector2(0.045f, 0.46f), new Vector2(0.955f, 0.50f));
            timerTrack = track.gameObject;
            var bar = Panel("Time remaining", track, Vector2.zero, Vector2.one);
            timer = bar.GetComponent<UnityEngine.UI.Image>(); timer.color = new Color(0.15f, 0.95f, 0.78f);
            inventory = Text(bottom, "", 17, new Vector2(0.045f, 0.04f), new Vector2(0.96f, 0.43f));
            var controls = Panel("Controls", transform, new Vector2(0.79f, 0.025f), new Vector2(0.975f, 0.073f));
            Text(controls, "Tab 의뢰 · Esc 정지", 16, new Vector2(0.06f, 0.04f), new Vector2(0.96f, 0.96f)).alignment = TextAlignmentOptions.Center;
            controls.gameObject.SetActive(false);
            var cross = Text(transform, "·", 25, new Vector2(0.48f, 0.48f), new Vector2(0.52f, 0.52f)); cross.alignment = TextAlignmentOptions.Center;
            prompt = Text(transform, "", 21, new Vector2(0.25f, 0.32f), new Vector2(0.75f, 0.45f)); prompt.alignment = TextAlignmentOptions.Center;
            message = Text(transform, "", 18, new Vector2(0.18f, 0.20f), new Vector2(0.82f, 0.27f)); message.alignment = TextAlignmentOptions.Center;
            modal = Panel("Contract panel", transform, new Vector2(0.21f, 0.27f), new Vector2(0.79f, 0.77f)).gameObject;
            Frame(modal.transform);
            contractText = Text(modal.transform, "", 26, new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.92f));
            var veil = Panel("일시정지 배경", transform, Vector2.zero, Vector2.one);
            veil.GetComponent<UnityEngine.UI.Image>().color = new Color(.015f, .02f, .035f, .72f);
            pausePanel = veil.gameObject;
            var pauseCard = Panel("일시정지 패널", veil, new Vector2(.33f, .25f), new Vector2(.67f, .75f));
            pauseCard.GetComponent<UnityEngine.UI.Image>().color = new Color(.045f, .039f, .038f, .97f);
            Frame(pauseCard);
            var pauseTitle = Text(pauseCard, "일시정지", 34, new Vector2(.10f,.73f), new Vector2(.90f,.90f));
            pauseTitle.alignment = TextAlignmentOptions.Center; pauseTitle.color = new Color(.96f,.86f,.67f);
            var pauseLabel = Text(pauseCard, "현재 의뢰", 16, new Vector2(.10f,.57f), new Vector2(.90f,.65f));
            pauseLabel.color = new Color(.83f,.65f,.37f);
            pauseObjective = Text(pauseCard, "", 21, new Vector2(.10f,.39f), new Vector2(.90f,.57f));
            var resume = Text(pauseCard, "[Esc]  계속하기", 22, new Vector2(.10f,.22f), new Vector2(.90f,.34f));
            resume.alignment = TextAlignmentOptions.Center;
            var restart = Text(pauseCard, "[R]  처음부터 다시 시작", 17, new Vector2(.10f,.09f), new Vector2(.90f,.19f));
            restart.alignment = TextAlignmentOptions.Center; restart.color = new Color(.73f,.69f,.62f);
            foreach (var guard in FindObjectsByType<GuardAgent>(FindObjectsSortMode.None))
                if (guard.gameObject.scene == gameObject.scene && guard.statusLabel) guard.statusLabel.font = font;
        }
        void Update()
        {
            if (!mission || !objective) return;
            var player = mission.player;
            if (mission.Objective != lastObjective) { lastObjective = mission.Objective; objectiveUntil = Time.unscaledTime + 5f; }
            objective.text = lastObjective;
            objectivePanel.SetActive(mission.Accepted && Time.unscaledTime < objectiveUntil);
            inventory.text = "아이템  |  " + (player.Inventory.Items.Count == 0 ? "없음" : string.Join(" · ", System.Linq.Enumerable.Select(player.Inventory.Items, item => item.label)));
            if (mission.Message != lastMessage) { lastMessage = mission.Message; messageUntil = Time.unscaledTime + 4f; }
            message.text = Time.unscaledTime < messageUntil ? lastMessage : "";
            var charge = player.Held;
            statusPanel.SetActive(charge != null || player.IsStunned);
            held.text = player.IsStunned ? "기절 — 이동과 능력을 사용할 수 없습니다" : charge == null ? "빈손  /  소리 관계를 찾아보세요"
                : "소리 운반  ·  " + charge.Grade + "등급  ·  " + charge.Remaining.ToString("0.0") + "초";
            timerTrack.SetActive(charge != null);
            timer.rectTransform.anchorMax = new Vector2(charge == null ? 0 : Mathf.Clamp01(charge.Remaining / mission.rules.HoldTime(charge.Grade)), 1);
            timer.color = charge != null && charge.Remaining < 3 ? new Color(1, 0.3f, 0.2f) : new Color(0.15f, 0.95f, 0.78f);
            var target = player.Target;
            prompt.text = !target ? "" : target.displayName + "  [F]";
            if (target && target.relation)
                prompt.text += "\n" + target.relation.Label + " · 등급 " + target.relation.grade + (target.relation.IsMuted ? " · 무음 / 복구 대기" : " · [E] 추출");
            if (target && target.emitter)
                prompt.text += target.emitter.Pending ? "\n주입 대기 · 3초 후 출력" : target.emitter.Active ? "\n주입 출력 · 등급 " + target.emitter.CurrentGrade : charge != null ? "\n[Q] 소리 주입 · 3초 뒤 재생" : "";
            modal.SetActive(!mission.Paused && (!mission.Accepted || mission.Completed || mission.ContractVisible));
            pausePanel.SetActive(mission.Paused);
            pauseObjective.text = mission.Objective;
            contractText.text = mission.Completed ? "의뢰 완료\n\n" + mission.Title + "\n목표물을 회수하고 탈출했습니다.\n\n[R] 의뢰 처음부터 다시 하기"
                : mission.Paused ? "일시정지\n\n소리 보유 시간과 경비 행동이 멈췄습니다.\n\n[Esc] 계속    [R] 전체 재시작"
                : mission.Title + "\n\n" + mission.Briefing + (mission.Accepted ? "\n\n현재 목표: " + mission.Objective + "\n" + inventory.text : "") + "\n\nWASD 이동  ·  마우스 시점  ·  Ctrl 웅크리기\nF 상호작용  ·  E 추출  ·  Q 주입" + (mission.rules.throwingEnabled ? "  ·  좌클릭 투척" : "") + "\nR 재시작  ·  Esc 정지\n\n" + (!mission.Accepted ? "[Enter / F] 의뢰 시작" : "[Tab] 닫기 · 의뢰창을 열어도 시간은 흐릅니다");
        }
        static RectTransform Panel(string name, Transform parent, Vector2 min, Vector2 max)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image)); obj.transform.SetParent(parent, false);
            var rect = obj.GetComponent<RectTransform>(); rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero;
            var image = obj.GetComponent<UnityEngine.UI.Image>(); image.color = new Color(0.035f, 0.032f, 0.045f, 0.80f); image.raycastTarget = false;
            return rect;
        }
        static void Frame(Transform parent)
        {
            var line = Panel("황동 장식", parent, new Vector2(.025f, .94f), new Vector2(.975f, .955f));
            line.GetComponent<UnityEngine.UI.Image>().color = new Color(.7f, .51f, .24f, .8f);
            var edge = Panel("황동 측면", parent, new Vector2(.025f, .10f), new Vector2(.031f, .90f));
            edge.GetComponent<UnityEngine.UI.Image>().color = new Color(.7f, .51f, .24f, .65f);
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
