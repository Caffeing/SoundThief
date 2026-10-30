using UnityEngine;
using TMPro;

namespace SoundThief
{
    public sealed class ThiefHud : MonoBehaviour
    {
        public MissionDirector mission;
        TMP_FontAsset font;
        TextMeshProUGUI seconds, title, objective, prompt, inventory, held, message, mHeading, mBriefing, mGoal, mControls, mFooter;
        CanvasGroup pauseFade;
        UnityEngine.UI.Image timer;
        GameObject interactionPanel, modal, pausePanel, timerTrack, objectivePanel, statusPanel;
        GameObject[] playPanels;
        TextMeshProUGUI pauseObjective;
        string lastMessage, lastObjective;
        float messageUntil, objectiveUntil;
        static readonly Color Ivory = new Color(0.93f, 0.9f, 0.82f), Brass = new Color(0.72f, 0.57f, 0.33f), Paper = new Color(0.96f, 0.9f, 0.76f), Dim = new Color(0.72f, 0.67f, 0.58f);
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
            var top = Panel("Contract header", transform, new Vector2(0.025f, 0.855f), new Vector2(0.32f, 0.965f));
            objectivePanel = top.gameObject;
            Edge(top);
            title = Text(top, "현재 의뢰", 15, new Vector2(0.07f, 0.64f), new Vector2(0.94f, 0.90f));
            title.name = "Objective heading"; title.color = Brass; title.alignment = TextAlignmentOptions.BottomLeft;
            objective = Text(top, "", 21, new Vector2(0.07f, 0.10f), new Vector2(0.94f, 0.60f));
            objective.name = "Objective text"; objective.alignment = TextAlignmentOptions.TopLeft;
            var bottom = Panel("Sound and items", transform, new Vector2(0.36f, 0.025f), new Vector2(0.64f, 0.125f));
            statusPanel = bottom.gameObject;
            Edge(bottom);
            held = Text(bottom, "", 19, new Vector2(0.06f, 0.58f), new Vector2(0.72f, 0.93f)); held.name = "Sound status";
            seconds = Text(bottom, "", 24, new Vector2(0.72f, 0.58f), new Vector2(0.95f, 0.93f)); seconds.name = "Sound seconds";
            seconds.alignment = TextAlignmentOptions.Right; seconds.color = Paper;
            var track = Panel("Sound timer", bottom, new Vector2(0.06f, 0.46f), new Vector2(0.95f, 0.52f));
            track.GetComponent<UnityEngine.UI.Image>().color = new Color(1, .95f, .8f, .12f);
            timerTrack = track.gameObject;
            var bar = Panel("Time remaining", track, Vector2.zero, Vector2.one);
            timer = bar.GetComponent<UnityEngine.UI.Image>(); timer.color = Brass;
            inventory = Text(bottom, "", 16, new Vector2(0.06f, 0.06f), new Vector2(0.95f, 0.40f)); inventory.name = "Item inventory"; inventory.color = Dim;
            var controls = Panel("Controls", transform, new Vector2(0.79f, 0.025f), new Vector2(0.975f, 0.073f));
            Text(controls, "Tab 의뢰 · Esc 정지", 16, new Vector2(0.06f, 0.04f), new Vector2(0.96f, 0.96f)).alignment = TextAlignmentOptions.Center;
            controls.gameObject.SetActive(false);
            var cross = Text(transform, "·", 25, new Vector2(0.48f, 0.48f), new Vector2(0.52f, 0.52f)); cross.alignment = TextAlignmentOptions.Center;
            // Compact dark backing keeps the F/E/Q prompt readable against bright scenes; shown only while a target is aimed.
            var promptBack = Panel("Interaction panel", transform, new Vector2(0.36f, 0.30f), new Vector2(0.64f, 0.41f));
            promptBack.GetComponent<UnityEngine.UI.Image>().color = new Color(0.03f, 0.028f, 0.027f, 0.88f);
            interactionPanel = promptBack.gameObject; interactionPanel.SetActive(false);
            var rule = Panel("Brass edge", promptBack, new Vector2(0f, 0f), new Vector2(0.012f, 1f));
            rule.GetComponent<UnityEngine.UI.Image>().color = new Color(Brass.r, Brass.g, Brass.b, 0.8f);
            prompt = Text(promptBack, "", 21, new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.92f)); prompt.alignment = TextAlignmentOptions.Center;
            message = Text(transform, "", 18, new Vector2(0.18f, 0.20f), new Vector2(0.82f, 0.27f)); message.alignment = TextAlignmentOptions.Center;
            modal = Panel("Contract panel", transform, new Vector2(0.20f, 0.20f), new Vector2(0.80f, 0.80f)).gameObject;
            modal.GetComponent<UnityEngine.UI.Image>().color = new Color(.05f, .04f, .033f, .94f);
            Frame(modal.transform);
            mHeading = Text(modal.transform, "", 32, new Vector2(0.08f, 0.80f), new Vector2(0.92f, 0.92f)); mHeading.name = "Contract heading"; mHeading.color = Paper;
            Rule(modal.transform, .79f);
            mBriefing = Text(modal.transform, "", 21, new Vector2(0.08f, 0.54f), new Vector2(0.92f, 0.77f)); mBriefing.name = "Contract briefing";
            mGoal = Text(modal.transform, "", 19, new Vector2(0.08f, 0.38f), new Vector2(0.92f, 0.52f)); mGoal.name = "Contract objective"; mGoal.color = Brass;
            Rule(modal.transform, .365f);
            mControls = Text(modal.transform, "", 17, new Vector2(0.08f, 0.19f), new Vector2(0.92f, 0.35f)); mControls.name = "Contract controls"; mControls.color = Dim;
            mFooter = Text(modal.transform, "", 20, new Vector2(0.08f, 0.05f), new Vector2(0.92f, 0.16f)); mFooter.name = "Contract footer";
            mFooter.alignment = TextAlignmentOptions.Center; mFooter.color = Paper;
            var veil = Panel("일시정지 배경", transform, Vector2.zero, Vector2.one);
            veil.GetComponent<UnityEngine.UI.Image>().color = new Color(.015f, .012f, .01f, .62f);
            pausePanel = veil.gameObject; pauseFade = pausePanel.AddComponent<CanvasGroup>();
            var pauseCard = Panel("일시정지 패널", veil, new Vector2(.32f, .22f), new Vector2(.68f, .78f));
            pauseCard.GetComponent<UnityEngine.UI.Image>().color = new Color(.05f, .04f, .033f, .96f);
            Frame(pauseCard);
            var pauseTitle = Text(pauseCard, "일시정지", 34, new Vector2(.10f,.77f), new Vector2(.90f,.90f)); pauseTitle.name = "Pause heading";
            pauseTitle.alignment = TextAlignmentOptions.Center; pauseTitle.color = Paper;
            Rule(pauseCard, .75f);
            var pauseLabel = Text(pauseCard, "현재 의뢰", 16, new Vector2(.10f,.65f), new Vector2(.90f,.72f)); pauseLabel.color = Brass;
            pauseObjective = Text(pauseCard, "", 21, new Vector2(.10f,.45f), new Vector2(.90f,.65f)); pauseObjective.name = "Pause objective";
            Rule(pauseCard, .41f);
            ActionRow(pauseCard, "Pause resume", "[Esc]  계속하기", 22, .22f, .36f, Ivory);
            ActionRow(pauseCard, "Pause restart", "[R]  처음부터 다시 시작", 18, .07f, .19f, Dim);
            playPanels = new[] { top.gameObject, bottom.gameObject, cross.gameObject, message.gameObject };
            if (mission.isGuild && mission.contractTable) gameObject.AddComponent<GuildContractView>().Build(mission.contractTable, font);
            foreach (var guard in FindObjectsByType<GuardAgent>(FindObjectsSortMode.None))
                if (guard.gameObject.scene == gameObject.scene && guard.statusLabel) guard.statusLabel.font = font;
        }
        void Update()
        {
            if (!mission || !objective) return;
            var player = mission.player;
            foreach (var panel in playPanels) panel.SetActive(!mission.TableOpen);
            if (mission.TableOpen) { interactionPanel.SetActive(false); modal.SetActive(false); pausePanel.SetActive(false); return; }
            title.text = mission.isGuild ? "길드 · 의뢰와 연습장" : "현재 의뢰";
            if (mission.Objective != lastObjective) { lastObjective = mission.Objective; objectiveUntil = Time.unscaledTime + 5f; }
            objective.text = lastObjective;
            objectivePanel.SetActive(mission.isGuild || (mission.Accepted && Time.unscaledTime < objectiveUntil));
            inventory.text = "아이템  |  " + (player.Inventory.Items.Count == 0 ? "없음" : string.Join(" · ", System.Linq.Enumerable.Select(player.Inventory.Items, item => item.label)));
            bool practicing = mission.isGuild && mission.practice && mission.practice.IsPlayerInside;
            string currentMessage = practicing ? mission.Message + "\n" + mission.practice.Guidance : mission.Message;
            if (currentMessage != lastMessage) { lastMessage = currentMessage; messageUntil = Time.unscaledTime + 4f; }
            message.text = practicing || Time.unscaledTime < messageUntil ? lastMessage : "";
            var charge = player.Held;
            statusPanel.SetActive(charge != null || player.IsStunned);
            held.text = player.IsStunned ? "기절 — 이동과 능력을 사용할 수 없습니다" : charge == null ? "빈손  /  소리 관계를 찾아보세요"
                : "소리 운반  ·  " + charge.Grade + "등급";
            seconds.text = charge == null ? "" : charge.Remaining.ToString("0.0") + "초";
            timerTrack.SetActive(charge != null);
            timer.rectTransform.anchorMax = new Vector2(charge == null ? 0 : Mathf.Clamp01(charge.Remaining / mission.rules.HoldTime(charge.Grade)), 1);
            timer.color = charge != null && charge.Remaining < 3 ? new Color(0.85f, 0.2f, 0.14f) : Brass;
            var target = player.Target;
            prompt.text = !target ? "" : target.displayName + "  [F]";
            if (target && target.relation)
                prompt.text += "\n" + target.relation.Label + " · 등급 " + target.relation.grade + (target.relation.IsMuted ? " · 무음 / 복구 대기" : " · [E] 추출");
            if (target && target.emitter)
                prompt.text += target.emitter.Pending ? "\n주입 대기 · 3초 후 출력" : target.emitter.Active ? "\n주입 출력 · 등급 " + target.emitter.CurrentGrade : charge != null ? "\n[Q] 소리 주입 · 3초 뒤 재생" : "";
            interactionPanel.SetActive(target && prompt.text.Length > 0);
            modal.SetActive(!mission.Paused && ((!mission.isGuild && !mission.Accepted) || mission.Completed || mission.ContractVisible));
            if (mission.Paused && !pausePanel.activeSelf) pauseFade.alpha = 0;
            pausePanel.SetActive(mission.Paused);
            pauseFade.alpha = Mathf.MoveTowards(pauseFade.alpha, 1, Time.unscaledDeltaTime / 0.15f);
            pauseObjective.text = mission.Objective;
            bool done = mission.Completed;
            mHeading.text = done ? "의뢰 완료" : mission.Title;
            mBriefing.text = done ? mission.Title + "\n목표물을 회수하고 탈출했습니다." : mission.Briefing;
            mGoal.text = done ? "" : mission.Accepted ? "현재 목표: " + mission.Objective + "\n" + inventory.text : "";
            mControls.text = done ? "" : "WASD 이동  ·  마우스 시점  ·  Ctrl 웅크리기\nF 상호작용  ·  E 추출  ·  Q 주입" + (mission.rules.throwingEnabled ? "  ·  좌클릭 투척" : "") + "\nR 재시작  ·  Esc 정지";
            mFooter.text = done ? (mission.returnToGuildOnSuccess ? "길드 출입문으로 복귀합니다…" : "[R] 의뢰 처음부터 다시 하기")
                : mission.isGuild ? "의뢰 테이블 [F] · 문서 ← → · ESC 닫기 / 수락 후 출구 [F]" : !mission.Accepted ? "[Enter / F] 의뢰 시작" : "[Tab] 닫기 · 의뢰창을 열어도 시간은 흐릅니다";
        }
        static void Edge(Transform parent)
        {
            var edge = Panel("황동 측면", parent, Vector2.zero, new Vector2(.012f, 1));
            edge.GetComponent<UnityEngine.UI.Image>().color = new Color(Brass.r, Brass.g, Brass.b, .8f);
        }
        static void Rule(Transform parent, float y)
        {
            var line = Panel("Rule", parent, new Vector2(.08f, y), new Vector2(.92f, y + .004f));
            line.GetComponent<UnityEngine.UI.Image>().color = new Color(Brass.r, Brass.g, Brass.b, .45f);
        }
        void ActionRow(Transform parent, string name, string value, int size, float y0, float y1, Color color)
        {
            var row = Panel(name + " row", parent, new Vector2(.10f, y0), new Vector2(.90f, y1));
            row.GetComponent<UnityEngine.UI.Image>().color = new Color(1, .95f, .8f, .05f);
            var label = Text(row, value, size, new Vector2(.04f, 0), new Vector2(.96f, 1)); label.name = name; label.color = color;
            label.alignment = TextAlignmentOptions.Center;
        }
        static RectTransform Panel(string name, Transform parent, Vector2 min, Vector2 max)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image)); obj.transform.SetParent(parent, false);
            var rect = obj.GetComponent<RectTransform>(); rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero;
            var image = obj.GetComponent<UnityEngine.UI.Image>(); image.color = new Color(0.04f, 0.037f, 0.036f, 0.86f); image.raycastTarget = false;
            return rect;
        }
        static void Frame(Transform parent)
        {
            var line = Panel("황동 장식", parent, new Vector2(.025f, .94f), new Vector2(.975f, .955f));
            line.GetComponent<UnityEngine.UI.Image>().color = new Color(Brass.r, Brass.g, Brass.b, .8f);
            var edge = Panel("황동 측면", parent, new Vector2(.025f, .10f), new Vector2(.031f, .90f));
            edge.GetComponent<UnityEngine.UI.Image>().color = new Color(Brass.r, Brass.g, Brass.b, .55f);
        }
        TextMeshProUGUI Text(Transform parent, string value, int size, Vector2 min, Vector2 max)
        {
            var obj = new GameObject("Label", typeof(RectTransform)); obj.transform.SetParent(parent, false);
            var text = obj.AddComponent<TextMeshProUGUI>(); text.font = font; text.text = value; text.fontSize = size; text.color = Ivory;
            text.enableAutoSizing = true; text.fontSizeMin = size * 0.7f; text.fontSizeMax = size; text.raycastTarget = false;
            var rect = text.rectTransform; rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero;
            return text;
        }
        void OnDestroy() { if (font) Destroy(font); }
    }
}
