using TMPro;
using UnityEngine;

namespace SoundThief
{
    public sealed class GuildContractView : MonoBehaviour
    {
        GuildContractTable table;
        TMP_FontAsset font;
        GameObject root;
        RectTransform paper, stackBack, movingShadow;
        ContractPageCurl curl;
        TextMeshProUGUI pageNumber, heading, body, objective, status, details, footer, movingTitle, movingBody, movingObjective, movingStatus;
        CanvasGroup note;
        bool? movingBehind;
        UnityEngine.UI.Image shadowImage;
        static readonly Color Ink = new Color(0.19f, 0.14f, 0.09f);
        static readonly Color Paper = new Color(0.94f, 0.89f, 0.75f);

        public void Build(GuildContractTable source, TMP_FontAsset sharedFont)
        {
            table = source; font = sharedFont;
            root = Panel("ContractDeskView", transform, Vector2.zero, Vector2.one, new Color(0.035f, 0.026f, 0.018f, 0.98f)).gameObject;
            Label(root.transform, "길드 의뢰 문서", 30, new Vector2(0.09f, 0.88f), new Vector2(0.6f, 0.95f), Paper);
            pageNumber = Label(root.transform, "", 18, new Vector2(0.70f, 0.9f), new Vector2(0.91f, 0.94f), Paper);
            pageNumber.alignment = TextAlignmentOptions.Right;
            // Offset sheets remain visible beneath the active document.
            stackBack = Panel("PaperStackBack", root.transform, new Vector2(0.352f, 0.188f), new Vector2(0.622f, 0.828f), Paper * new Color(0.72f, 0.7f, 0.64f, 1));
            stackBack.localEulerAngles = new Vector3(0, 0, -2);
            Panel("PaperStackFront", root.transform, new Vector2(0.346f, 0.194f), new Vector2(0.616f, 0.834f), Paper * new Color(0.88f, 0.87f, 0.83f, 1));
            paper = Panel("ActiveContract", root.transform, new Vector2(0.34f, 0.20f), new Vector2(0.61f, 0.84f), Paper);
            var ink = Panel("DocumentContent", paper, Vector2.zero, Vector2.one, Color.clear);
            Label(ink, "의 뢰 서   /   GUILD COMMISSION", 16, new Vector2(0.09f, 0.86f), new Vector2(0.91f, 0.94f), Ink);
            Panel("Rule", ink, new Vector2(0.09f, 0.835f), new Vector2(0.91f, 0.839f), Ink);
            heading = Label(ink, "", 31, new Vector2(0.09f, 0.64f), new Vector2(0.91f, 0.81f), Ink);
            body = Label(ink, "", 21, new Vector2(0.09f, 0.39f), new Vector2(0.91f, 0.61f), Ink);
            objective = Label(ink, "", 19, new Vector2(0.09f, 0.20f), new Vector2(0.91f, 0.37f), Ink);
            status = Label(ink, "", 22, new Vector2(0.09f, 0.07f), new Vector2(0.91f, 0.17f), new Color(0.50f, 0.15f, 0.10f));
            var memo = Panel("BriefingSheet", root.transform, new Vector2(0.66f, 0.40f), new Vector2(0.93f, 0.81f), new Color(0.86f, 0.81f, 0.66f));
            memo.localEulerAngles = new Vector3(0, 0, -3);
            note = memo.gameObject.AddComponent<CanvasGroup>();
            Label(memo, "간략한 설명", 25, new Vector2(0.1f, 0.77f), new Vector2(0.9f, 0.94f), Ink);
            details = Label(memo, "", 21, new Vector2(0.1f, 0.12f), new Vector2(0.9f, 0.74f), Ink);
            footer = Label(root.transform, "", 21, new Vector2(0.12f, 0.06f), new Vector2(0.9f, 0.15f), Paper);
            footer.alignment = TextAlignmentOptions.Center;
            movingShadow = Panel("MovingPaperShadow", root.transform, paper.anchorMin, paper.anchorMax, new Color(0, 0, 0, 0.22f));
            shadowImage = movingShadow.GetComponent<UnityEngine.UI.Image>();
            var moving = new GameObject("TurningPaper", typeof(RectTransform), typeof(CanvasRenderer), typeof(ContractPageCurl));
            moving.transform.SetParent(root.transform, false);
            var rect = moving.GetComponent<RectTransform>(); rect.anchorMin = paper.anchorMin; rect.anchorMax = paper.anchorMax; rect.offsetMin = rect.offsetMax = Vector2.zero;
            curl = moving.GetComponent<ContractPageCurl>(); curl.color = Paper; curl.raycastTarget = false;
            Label(rect, "의 뢰 서   /   GUILD COMMISSION", 16, new Vector2(0.09f, 0.86f), new Vector2(0.91f, 0.94f), Ink);
            Panel("MovingRule", rect, new Vector2(0.09f, 0.835f), new Vector2(0.91f, 0.839f), Ink);
            movingTitle = Label(rect, "", 31, new Vector2(0.09f, 0.64f), new Vector2(0.91f, 0.81f), Ink);
            movingBody = Label(rect, "", 21, new Vector2(0.09f, 0.39f), new Vector2(0.91f, 0.61f), Ink);
            movingObjective = Label(rect, "", 19, new Vector2(0.09f, 0.20f), new Vector2(0.91f, 0.37f), Ink);
            movingStatus = Label(rect, "", 22, new Vector2(0.09f, 0.07f), new Vector2(0.91f, 0.17f), new Color(0.50f, 0.15f, 0.10f));
            root.SetActive(false);
        }
        void LateUpdate()
        {
            if (!root || !table) return;
            root.SetActive(table.IsOpen);
            if (!table.IsOpen) { movingBehind = null; return; }
            FillDocument(table.DisplayedOffer, heading, body, objective, status);
            float p = table.TurnProgress;
            // Description changes as the next physical sheet is laid down, never with an empty flash.
            int descriptionIndex = table.IsTurning && p >= 0.60f ? table.IncomingIndex : table.SelectedIndex;
            var description = table.Count == 0 ? null : table.offers[descriptionIndex];
            details.text = description == null ? "" : description.Description;
            pageNumber.text = (descriptionIndex + 1) + " / " + table.Count + "   DOCUMENTS";
            bool repeatBlocked = table.CurrentOffer != null && table.CurrentOffer.IsRepeatBlocked;
            footer.text = "←  앞 문서를 뒤로     뒤 문서를 앞으로  →     |     "
                + (repeatBlocked ? "재수락 불가능" : "[F / Enter] 수락") + "     [ESC] 나가기\n"
                + (repeatBlocked ? "이미 완료한 의뢰입니다. 다른 의뢰 문서를 확인하세요." : table.mission.Message);
            curl.gameObject.SetActive(table.IsTurning);
            movingShadow.gameObject.SetActive(table.IsTurning);
            note.alpha = 1;
            if (!table.IsTurning) { movingBehind = null; return; }
            bool outgoing = table.TurnDirection < 0;
            var movingOffer = outgoing ? table.CurrentOffer : table.offers[table.IncomingIndex];
            FillDocument(movingOffer, movingTitle, movingBody, movingObjective, movingStatus);
            bool behind = outgoing ? p >= 0.5f : p < 0.5f;
            // Change draw order only while the sheet is entirely beside the pile.
            // Left: front -> side -> behind. Right: behind -> side -> front.
            if (movingBehind != behind)
            {
                // Remove both moving elements from the insertion range first. Otherwise their
                // old indices shift the anchor, alternating shadow/paper order on every frame.
                movingShadow.SetAsLastSibling();
                curl.rectTransform.SetAsLastSibling();
                if (behind)
                {
                    curl.rectTransform.SetSiblingIndex(stackBack.GetSiblingIndex());
                    movingShadow.SetSiblingIndex(curl.rectTransform.GetSiblingIndex());
                }
                else
                {
                    movingShadow.SetSiblingIndex(paper.GetSiblingIndex() + 1);
                    curl.rectTransform.SetSiblingIndex(movingShadow.GetSiblingIndex() + 1);
                }
                movingBehind = behind;
            }
            float side = p < 0.5f ? Ease(Mathf.InverseLerp(0.04f, 0.48f, p)) : 1 - Ease(Mathf.InverseLerp(0.52f, 0.96f, p));
            float lift = Mathf.Sin(p * Mathf.PI);
            var offset = new Vector2(-paper.rect.width * 1.12f * side, 18 * lift + 9 * side);
            var sheet = curl.rectTransform;
            sheet.anchoredPosition = paper.anchoredPosition + offset;
            sheet.localRotation = Quaternion.Euler(0, 0, -5.5f * side + (outgoing ? -0.8f : 0.8f) * lift);
            sheet.localScale = Vector3.one * (1 + 0.018f * lift);
            curl.SetTurn(p, table.TurnDirection);
            movingShadow.anchoredPosition = sheet.anchoredPosition + new Vector2(7 + 5 * lift, -7 - 8 * lift);
            movingShadow.localRotation = sheet.localRotation;
            movingShadow.localScale = sheet.localScale;
            // The shadow settles continuously rather than disappearing at full opacity.
            shadowImage.color = new Color(0, 0, 0, 0.22f * lift);
        }
        static float Ease(float t) => t * t * t * (t * (t * 6 - 15) + 10);
        void FillDocument(GuildContractOffer offer, TextMeshProUGUI title, TextMeshProUGUI text, TextMeshProUGUI goal, TextMeshProUGUI stamp)
        {
            title.text = offer == null ? "의뢰 없음" : offer.Title;
            bool available = offer != null && offer.IsAvailable;
            bool completed = offer != null && offer.IsCompleted;
            text.text = completed ? "완수한 의뢰\n목표물 회수와 탈출을 완료했습니다." : available ? "정식 의뢰\n목표를 수행한 뒤 출구로 복귀하십시오." : "미개방 의뢰\n수행할 수 있는 스테이지가 아직 없습니다.";
            goal.text = available && offer.definition ? "수행 목표\n" + offer.definition.objectiveText : "이 문서는 넘김 확인용 예시입니다.\n수락하거나 출발할 수 없습니다.";
            bool accepted = available && table.mission.Accepted && table.mission.stage == offer.definition;
            stamp.text = completed ? (offer.IsRepeatBlocked ? "의뢰 완료\n재수락 불가능" : accepted ? "완료 · 재수락" : "의뢰 완료") : available ? (accepted ? "수 락 완 료" : "수 락 가 능") : "준 비 중";
            stamp.color = completed ? new Color(0.16f, 0.38f, 0.22f) : new Color(0.50f, 0.15f, 0.10f);
        }
        static RectTransform Panel(string name, Transform parent, Vector2 min, Vector2 max, Color color)
        {
            var obj = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image)); obj.transform.SetParent(parent, false);
            var rect = obj.GetComponent<RectTransform>(); rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero;
            var image = obj.GetComponent<UnityEngine.UI.Image>(); image.color = color; image.raycastTarget = false;
            return rect;
        }
        TextMeshProUGUI Label(Transform parent, string text, int size, Vector2 min, Vector2 max, Color color)
        {
            var obj = new GameObject("DocumentText", typeof(RectTransform)); obj.transform.SetParent(parent, false);
            var label = obj.AddComponent<TextMeshProUGUI>(); label.font = font; label.text = text; label.fontSize = size; label.color = color;
            label.enableAutoSizing = true; label.fontSizeMin = size * 0.78f; label.fontSizeMax = size; label.raycastTarget = false;
            var rect = obj.GetComponent<RectTransform>(); rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = rect.offsetMax = Vector2.zero;
            return label;
        }
    }
}
