using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SoundThief.PostalDepot
{
    // HMM owns F targeting and permission checks; only this modal owns pause and reading controls.
    [DefaultExecutionOrder(-50)]
    public sealed class PostalLetterReader : MonoBehaviour
    {
        public MissionDirector mission;
        public const string RequestClue = "의뢰인 바질 → 로언\n노란 우산을 돌려주지 않았다고 화내며 쓴 편지입니다.\n우산을 집에서 찾았으니, 발송 전에 그 편지를 회수해 주세요.";
        public PostalLetter OpenedLetter { get; private set; }
        public PostalLetter SelectedLetter { get; private set; }
        public bool IsReading => OpenedLetter;
        InputActionMap actions;
        bool closeRequested, collectRequested;
        bool playerWasEnabled, hudWasEnabled, ownsPause;
        Canvas hudCanvas, readerCanvas;
        GameObject modal;
        TextMeshProUGUI heading, contents, selection;
        UnityEngine.UI.Button takeButton;
        bool uiBuilt;

        void OnEnable()
        {
            actions = new InputActionMap("Postal letters");
            actions.AddAction("Close", InputActionType.Button, "<Keyboard>/escape").performed += _ => closeRequested = true;
            actions.AddAction("Take", InputActionType.Button, "<Keyboard>/enter").performed += _ => collectRequested = true;
            actions.Enable();
        }
        void Start() { BuildUI(); }
        void Update()
        {
            bool close = closeRequested, collect = collectRequested;
            closeRequested = collectRequested = false;
            if (IsReading)
            {
                if (close) Close();
                else if (collect) TakeOpenedLetter();
                return;
            }
        }
        public void Read(PostalLetter letter, ThiefPlayer player)
        {
            if (!letter || !mission || player != mission.player || !mission.CanAct || player.IsStunned || IsReading) return;
            playerWasEnabled = player.enabled;
            player.enabled = false;
            Open(letter);
        }
        void Open(PostalLetter letter)
        {
            BuildUI();
            OpenedLetter = letter;
            letter.WasRead = true;
            hudCanvas = mission.GetComponent<StageContext>().hud.GetComponent<Canvas>();
            hudWasEnabled = hudCanvas && hudCanvas.enabled;
            if (hudCanvas) hudCanvas.enabled = false;
            mission.TogglePause(); ownsPause = mission.Paused;
            heading.text = "받는 사람  " + letter.recipient + "    |    보낸 사람  " + letter.sender;
            contents.text = letter.body;
            selection.text = SelectedLetter ? "현재 " + SelectedLetter.Label : "아직 회수한 편지가 없습니다.";
            takeButton.GetComponentInChildren<TextMeshProUGUI>().text = SelectedLetter == letter ? "이 편지 계속 회수 [Enter]" : "이 편지 회수 [Enter]";
            modal.SetActive(true);
            UnityEngine.EventSystems.EventSystem.current?.SetSelectedGameObject(null);
        }
        public void TakeOpenedLetter()
        {
            if (!OpenedLetter || !OpenedLetter.WasRead) return;
            if (SelectedLetter) mission.player.Inventory.Remove(SelectedLetter.InventoryId);
            SelectedLetter = OpenedLetter;
            mission.player.Inventory.Add(SelectedLetter.InventoryId, SelectedLetter.Label);
            Close();
            mission.Notify("편지 한 통을 회수했습니다. 이름과 내용을 의뢰와 대조하세요. 다른 편지를 회수하면 교체됩니다.");
        }
        public void Close()
        {
            if (!OpenedLetter) return;
            OpenedLetter = null;
            if (modal) modal.SetActive(false);
            if (hudCanvas) hudCanvas.enabled = hudWasEnabled;
            if (ownsPause && mission && mission.Paused) mission.TogglePause();
            ownsPause = false;
            if (mission && mission.player) mission.player.enabled = playerWasEnabled;
            closeRequested = collectRequested = false;
        }
        void OnDisable()
        {
            Close();
            actions?.Dispose(); actions = null;
            closeRequested = collectRequested = false;
        }
        void BuildUI()
        {
            if (uiBuilt) return;
            uiBuilt = true;
            var root = new GameObject("Postal letter reader", typeof(RectTransform), typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
            root.transform.SetParent(transform, false);
            readerCanvas = root.GetComponent<Canvas>(); readerCanvas.renderMode = RenderMode.ScreenSpaceOverlay; readerCanvas.sortingOrder = 100;
            var scaler = root.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600, 900); scaler.matchWidthOrHeight = 0.5f;
            modal = Panel("Reading overlay", root.transform, Vector2.zero, Vector2.one, new Color(0.015f,0.025f,0.035f,0.96f)).gameObject;
            var paper = Panel("Letter paper", modal.transform, new Vector2(0.12f,0.08f),new Vector2(0.88f,0.92f),new Color(0.96f,0.92f,0.8f));
            Label(paper,"의뢰 메모 · 읽는 동안 시간과 경비가 멈춥니다",20,new Vector2(0.045f,0.92f),new Vector2(0.955f,0.97f));
            Label(paper,RequestClue,23,new Vector2(0.045f,0.76f),new Vector2(0.955f,0.91f));
            heading = Label(paper,"",25,new Vector2(0.045f,0.68f),new Vector2(0.955f,0.74f));
            contents = Label(paper,"",27,new Vector2(0.045f,0.27f),new Vector2(0.955f,0.66f));
            selection = Label(paper,"",19,new Vector2(0.045f,0.20f),new Vector2(0.955f,0.25f));
            Label(paper,"읽기만 해서는 회수되지 않습니다. 회수한 편지는 한 통이며, 다른 편지로 교체할 수 있습니다.",18,new Vector2(0.045f,0.14f),new Vector2(0.955f,0.20f));
            takeButton = Button(paper,"이 편지 회수 [Enter]",new Vector2(0.045f,0.045f),new Vector2(0.49f,0.12f),TakeOpenedLetter);
            Button(paper,"닫기 [Esc]",new Vector2(0.53f,0.045f),new Vector2(0.955f,0.12f),Close);
            if (!UnityEngine.EventSystems.EventSystem.current)
            {
                var events = new GameObject("Postal UI input",typeof(UnityEngine.EventSystems.EventSystem),typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
                events.transform.SetParent(transform,false);
                events.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>().AssignDefaultActions();
            }
            modal.SetActive(false);
        }
        static RectTransform Panel(string name, Transform parent, Vector2 min, Vector2 max, Color color)
        {
            var o = new GameObject(name,typeof(RectTransform),typeof(UnityEngine.UI.Image));o.transform.SetParent(parent,false);
            var r = o.GetComponent<RectTransform>();r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;
            var image=o.GetComponent<UnityEngine.UI.Image>();image.color=color;image.raycastTarget=true;return r;
        }
        static TextMeshProUGUI Label(Transform parent,string text,int size,Vector2 min,Vector2 max)
        {
            var o=new GameObject("Letter text",typeof(RectTransform));o.transform.SetParent(parent,false);
            var label=o.AddComponent<TextMeshProUGUI>();label.text=text;label.fontSize=size;label.color=new Color(0.09f,0.13f,0.16f);label.raycastTarget=false;
            label.enableAutoSizing=true;label.fontSizeMin=size*0.85f;label.fontSizeMax=size;label.textWrappingMode=TextWrappingModes.Normal;
            var r=label.rectTransform;r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;return label;
        }
        static UnityEngine.UI.Button Button(Transform parent,string text,Vector2 min,Vector2 max,UnityEngine.Events.UnityAction action)
        {
            var r=Panel(text,parent,min,max,new Color(0.08f,0.26f,0.28f));var b=r.gameObject.AddComponent<UnityEngine.UI.Button>();
            b.targetGraphic=r.GetComponent<UnityEngine.UI.Image>();b.onClick.AddListener(action);
            b.navigation=new UnityEngine.UI.Navigation {mode=UnityEngine.UI.Navigation.Mode.None};
            var label=Label(r,text,23,new Vector2(0.03f,0.1f),new Vector2(0.97f,0.9f));label.color=Color.white;label.alignment=TextAlignmentOptions.Center;return b;
        }
    }
}
