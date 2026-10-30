using UnityEngine;
using UnityEngine.InputSystem;

namespace SoundThief
{
    [RequireComponent(typeof(CharacterController), typeof(StageInventory))]
    public sealed class ThiefPlayer : MonoBehaviour
    {
        public MissionDirector Mission;
        public Camera view;
        StageInventory inventory;
        public StageInventory Inventory => inventory ? inventory : (inventory = GetComponent<StageInventory>() ?? gameObject.AddComponent<StageInventory>());
        // Compatibility aliases for the original manor scene. New stages use item IDs.
        public bool HasKey { get => Inventory.Contains("key"); set { if (value) Inventory.Add("key", "열쇠"); else Inventory.Remove("key"); } }
        public bool HasTreasure { get => Inventory.Contains("objective"); set { if (value) Inventory.Add("objective", "목표물"); else Inventory.Remove("objective"); } }
        public SoundCharge Held { get; private set; }
        public ThiefInteractable Target { get; private set; }
        public bool IsStunned => Time.time < stunUntil;
        public bool Slow => Held != null || (Keyboard.current != null && Keyboard.current.leftCtrlKey.isPressed);
        public float CurrentSpeed => Slow ? Mission.rules.carrySpeed : Mission.rules.walkSpeed;
        CharacterController controller;
        float pitch, gravitySpeed, stunUntil;
        float connectionUntil;
        LineRenderer connection;
        Material lineMaterial;
        InputActionMap actions;
        bool interactRequested, extractRequested, injectRequested, throwRequested;
        bool acceptRequested, pauseRequested, contractRequested, restartRequested, previousRequested, nextRequested;

        void OnEnable()
        {
            actions = new InputActionMap("Thief");
            Bind("Interact", "<Keyboard>/f", () => interactRequested = true);
            Bind("Extract", "<Keyboard>/e", () => extractRequested = true);
            Bind("Inject", "<Keyboard>/q", () => injectRequested = true);
            Bind("Throw", "<Mouse>/leftButton", () => throwRequested = true);
            Bind("Accept", "<Keyboard>/enter", () => acceptRequested = true);
            Bind("Pause", "<Keyboard>/escape", () => pauseRequested = true);
            Bind("Contract", "<Keyboard>/tab", () => contractRequested = true);
            Bind("Restart", "<Keyboard>/r", () => restartRequested = true);
            Bind("Previous contract", "<Keyboard>/leftArrow", () => previousRequested = true);
            Bind("Next contract", "<Keyboard>/rightArrow", () => nextRequested = true);
            actions.Enable();
        }
        void Bind(string name, string binding, System.Action request)
        {
            actions.AddAction(name, InputActionType.Button, binding).performed += _ => request();
        }
        void OnDisable() { actions?.Dispose(); actions = null; ClearRequests(); }
        void ClearRequests()
        {
            interactRequested = extractRequested = injectRequested = throwRequested = false;
            acceptRequested = pauseRequested = contractRequested = restartRequested = previousRequested = nextRequested = false;
        }

        void Awake()
        {
            controller = GetComponent<CharacterController>();
            connection = gameObject.AddComponent<LineRenderer>(); connection.positionCount = 2;
            connection.widthMultiplier = 0.018f;
            lineMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            lineMaterial.color = new Color(0.2f, 1, 0.85f); connection.sharedMaterial = lineMaterial;
        }
        void Update()
        {
            var keys = Keyboard.current;
            if (Mission == null) return;
            bool interact = interactRequested, extract = extractRequested, inject = injectRequested, toss = throwRequested;
            bool accept = acceptRequested, pause = pauseRequested, contract = contractRequested, restart = restartRequested;
            bool previous = previousRequested, next = nextRequested;
            ClearRequests();
            // Table browsing locks controls, not world time or a held sound's deadline.
            if (!Mission.Paused && (Mission.Accepted || Mission.isGuild) && Held != null && Held.Remaining <= 0) ExpireHeld();
            if (Mission.TableOpen)
            {
                Target = null; connection.enabled = false;
                if (IsStunned || pause) Mission.contractTable.Close();
                else if (previous != next) Mission.contractTable.Turn(next ? 1 : -1);
                else if (interact || accept) Mission.contractTable.AcceptCurrent();
                return;
            }
            if (restart) { Mission.Restart(); return; }
            if (pause) Mission.TogglePause();
            if (contract && !Mission.isGuild) Mission.ContractVisible = !Mission.ContractVisible;
            if (!Mission.isGuild && !Mission.Accepted)
            {
                Target = null; connection.enabled = false;
                if (accept || interact) Mission.Accept();
                return;
            }
            if (!Mission.CanAct) { connection.enabled = false; return; }
            // Deadline stays in game time; explicit pause is the only time freeze.
            if (IsStunned) { Target = null; connection.enabled = false; return; }
            if (Mouse.current != null && Cursor.lockState == CursorLockMode.Locked)
            {
                var delta = Mouse.current.delta.ReadValue() * 0.1f;
                transform.Rotate(0, delta.x, 0); pitch = Mathf.Clamp(pitch - delta.y, -80, 80);
                view.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
            }
            var movement = keys == null ? Vector2.zero : new Vector2((keys.dKey.isPressed ? 1 : 0) - (keys.aKey.isPressed ? 1 : 0),
                (keys.wKey.isPressed ? 1 : 0) - (keys.sKey.isPressed ? 1 : 0));
            movement = Vector2.ClampMagnitude(movement, 1);
            if (controller.isGrounded && gravitySpeed < 0) gravitySpeed = -2;
            gravitySpeed += Physics.gravity.y * Time.deltaTime;
            controller.Move(((transform.right * movement.x + transform.forward * movement.y) * CurrentSpeed + Vector3.up * gravitySpeed) * Time.deltaTime);
            var local = view.transform.localPosition;
            local.y = Mathf.MoveTowards(local.y, Slow ? 1.05f : 1.65f, Time.deltaTime * 3);
            view.transform.localPosition = local;
            FindTarget();
            if (interact && Target) Target.Interact(this);
            if (extract) TryExtract();
            if (inject) TryInject();
            if (toss) TryThrow();
        }
        void FindTarget()
        {
            Target = null;
            // Looking down at a desk can cross the player's own capsule near its top.
            // Ignore only the player's colliders; the nearest remaining solid still blocks selection.
            float nearest = float.PositiveInfinity;
            foreach (var hit in Physics.RaycastAll(view.transform.position, view.transform.forward, Mission.rules.interactDistance, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.transform.IsChildOf(transform) || hit.distance >= nearest) continue;
                nearest = hit.distance; Target = hit.collider.GetComponentInParent<ThiefInteractable>();
            }
            connection.enabled = Target && Target.relation && Time.time < connectionUntil;
            if (connection.enabled)
            {
                connection.SetPosition(0, view.transform.position + view.transform.right * 0.25f - view.transform.up * 0.18f + view.transform.forward * 0.3f);
                connection.SetPosition(1, Target.transform.position + Vector3.up * (Target.kind == InteractionKind.Door ? 1.1f : 0.2f));
                connection.widthMultiplier = Target.relation.IsMuted ? 0.006f : 0.018f;
            }
        }
        public bool TryExtract()
        {
            if (IsStunned || !Mission.CanAct || Held != null || !Target || !Target.relation) return false;
            if (Target.kind == InteractionKind.Door && !Target.CanUnlock(this)) { Mission.Notify("문에 필요한 아이템을 먼저 찾아야 관계를 연결할 수 있습니다."); return false; }
            Held = Target.relation.Extract(Mission.rules);
            if (Held == null) { Mission.Notify("이 관계의 소리가 아직 돌아오지 않았습니다."); return false; }
            connectionUntil = Time.time + .3f;
            Mission.RecordExtraction();
            Mission.Notify(Target.relation.Label + " 소리를 훔쳤습니다. 이 관계는 무음입니다. Q로 다른 곳에 옮길 수 있습니다.");
            return true;
        }
        public bool TryInject()
        {
            if (IsStunned || !Mission.CanAct || Held == null || !Target || !Target.emitter) return false;
            if (!Target.emitter.Inject(Held, Mission.World)) { Mission.Notify("이미 소리가 출력 중인 대상입니다."); return false; }
            Held = null; connectionUntil = Time.time + .3f; Mission.RecordInjection();
            Mission.Notify("소리를 주입했습니다. 3초 후 출력되며, 출력이 끝나면 원래 관계의 소리가 돌아옵니다.");
            return true;
        }
        public bool TryThrow()
        {
            if (!Mission || !Mission.rules || !Mission.rules.throwingEnabled) return false;
            if (IsStunned || !Mission.CanAct || Held == null) return false;
            var projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectile.name = "Thrown sound"; projectile.transform.localScale = Vector3.one * 0.18f;
            var origin = view.transform.position;
            float distance = Physics.SphereCast(origin, 0.1f, view.transform.forward, out var obstruction, 0.5f, 1 << 8)
                ? Mathf.Max(0, obstruction.distance - 0.02f) : 0.5f;
            projectile.transform.position = origin + view.transform.forward * distance;
            projectile.GetComponent<Renderer>().sharedMaterial = Mission.projectileMaterial;
            var body = projectile.AddComponent<Rigidbody>(); body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.linearVelocity = view.transform.forward * 8 + Vector3.up * 3;
            Physics.IgnoreCollision(projectile.GetComponent<Collider>(), controller);
            projectile.AddComponent<SoundProjectile>().Init(Held, Mission.World);
            Held = null; Mission.RecordThrow();
            Mission.Notify("소리를 던졌습니다. 착지하거나 남은 시간이 끝나면 폭발합니다.");
            return true;
        }
        public void ExpireHeld()
        {
            if (Held == null) return;
            var charge = Held; Held = null;
            if (charge.Finish()) Mission.World.Blast(transform.position + Vector3.up, charge.Grade, true);
            Mission.Notify("보유 시간 초과! 잠시 기절했습니다. 원래 소리는 복구되었습니다.");
        }
        public void Stun(float seconds) { stunUntil = Time.time + seconds; Target = null; if (connection) connection.enabled = false; }
        public void PlaceAt(Transform spawn)
        {
            controller.enabled = false;
            transform.SetPositionAndRotation(spawn.position, spawn.rotation);
            controller.enabled = true;
            pitch = gravitySpeed = 0;
            view.transform.localRotation = Quaternion.identity;
            view.transform.localPosition = Vector3.up * 1.65f;
            Physics.SyncTransforms();
        }
        public void ResetPracticeState()
        {
            Held?.Finish(); Held = null; stunUntil = connectionUntil = 0;
            Inventory.Clear(); Target = null;
            if (connection) connection.enabled = false;
        }
        void OnDestroy() { Held?.Finish(); if (lineMaterial) Destroy(lineMaterial); }
    }
}
