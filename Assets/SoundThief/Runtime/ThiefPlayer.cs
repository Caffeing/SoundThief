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
        public ThiefInteractable HighlightTarget { get; private set; }
        public bool IsStunned => Time.time < stunUntil;
        public bool IsCrouching { get; private set; }
        bool CrouchRequested => Keyboard.current != null && Keyboard.current.leftCtrlKey.isPressed;
        public bool Slow => Held != null || CrouchRequested || IsCrouching;
        public float CurrentSpeed => Slow ? Mission.rules.carrySpeed : Mission.rules.walkSpeed;
        CharacterController controller;
        float pitch, gravitySpeed, stunUntil;
        float connectionUntil;
        float standingHeight, standingFoot, cameraVelocity, fovVelocity, standingFov, stridePhase;
        Vector3 standingCamera, standingCenter;
        LineRenderer connection;
        Material lineMaterial;
        HeldSoundPresentation heldPresentation;
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
            standingHeight = controller.height; standingCenter = controller.center;
            standingFoot = standingCenter.y - standingHeight * .5f;
            standingCamera = view.transform.localPosition; standingFov = view.fieldOfView;
            connection = gameObject.AddComponent<LineRenderer>(); connection.positionCount = 2;
            connection.widthMultiplier = 0.018f;
            lineMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            lineMaterial.color = new Color(0.2f, 1, 0.85f); connection.sharedMaterial = lineMaterial;
            var presentation = Resources.Load<HeldSoundPresentation>("HeldSoundPresentation");
            if (presentation && view) { heldPresentation = Instantiate(presentation, view.transform); heldPresentation.Initialize(this); }
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
                Target = HighlightTarget = null; connection.enabled = false;
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
                Target = HighlightTarget = null; connection.enabled = false;
                if (accept || interact) Mission.Accept();
                return;
            }
            if (!Mission.CanAct) { Target = HighlightTarget = null; connection.enabled = false; return; }
            // Deadline stays in game time; explicit pause is the only time freeze.
            if (IsStunned) { Target = HighlightTarget = null; connection.enabled = false; return; }
            if (Mouse.current != null && Cursor.lockState == CursorLockMode.Locked)
            {
                var delta = Mouse.current.delta.ReadValue() * 0.1f;
                transform.Rotate(0, delta.x, 0); pitch = Mathf.Clamp(pitch - delta.y, -80, 80);
                view.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
            }
            var movement = keys == null ? Vector2.zero : new Vector2((keys.dKey.isPressed ? 1 : 0) - (keys.aKey.isPressed ? 1 : 0),
                (keys.wKey.isPressed ? 1 : 0) - (keys.sKey.isPressed ? 1 : 0));
            movement = Vector2.ClampMagnitude(movement, 1);
            UpdatePosture();
            if (controller.isGrounded && gravitySpeed < 0) gravitySpeed = -2;
            gravitySpeed += Physics.gravity.y * Time.deltaTime;
            var beforeMove = transform.position;
            controller.Move(((transform.right * movement.x + transform.forward * movement.y) * CurrentSpeed + Vector3.up * gravitySpeed) * Time.deltaTime);
            var travelled = transform.position - beforeMove; travelled.y = 0;
            stridePhase += travelled.magnitude * (IsCrouching ? 5 : 7);
            var local = view.transform.localPosition;
            float bob = travelled.sqrMagnitude > .00001f && controller.isGrounded ? Mathf.Sin(stridePhase) * (IsCrouching ? .008f : .018f) : 0;
            local.y = Mathf.SmoothDamp(local.y, (IsCrouching ? .72f : standingCamera.y) + bob, ref cameraVelocity, .12f);
            local.z = Mathf.Lerp(local.z, standingCamera.z + (IsCrouching ? .07f : 0), 1 - Mathf.Exp(-Time.deltaTime * 12));
            view.transform.localPosition = local;
            view.fieldOfView = Mathf.SmoothDamp(view.fieldOfView, standingFov - (IsCrouching ? 4 : 0), ref fovVelocity, .16f);
            FindTarget();
            if (interact && Target) Target.Interact(this);
            if (extract) TryExtract();
            if (inject) TryInject();
            if (toss) TryThrow();
        }
        void UpdatePosture()
        {
            bool crouch = CrouchRequested;
            if (!crouch && controller.height < standingHeight - .01f)
            {
                float radius = Mathf.Max(.05f, controller.radius - .02f);
                var bottom = transform.position + Vector3.up * (standingFoot + controller.height - radius);
                var top = transform.position + Vector3.up * (standingFoot + standingHeight - radius);
                crouch = Physics.CheckCapsule(bottom, top, radius, 1 << 8, QueryTriggerInteraction.Ignore);
            }
            IsCrouching = crouch;
            controller.height = Mathf.MoveTowards(controller.height, crouch ? Mathf.Max(1, controller.radius * 2) : standingHeight, Time.deltaTime * 7);
            controller.center = new Vector3(standingCenter.x, standingFoot + controller.height * .5f, standingCenter.z);
        }
        void FindTarget()
        {
            Target = HighlightTarget = null;
            // The camera may sit against the player's CharacterController when looking down.
            // Ignore the player's own colliders but still respect the nearest wall or object.
            float nearest = float.PositiveInfinity;
            foreach (var hit in Physics.RaycastAll(view.transform.position, view.transform.forward, Mission.rules.interactDistance, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.transform.IsChildOf(transform) || hit.distance >= nearest) continue;
                nearest = hit.distance; Target = hit.collider.GetComponentInParent<ThiefInteractable>();
            }
            // Keep the nearest collider opaque even when its interaction is locked.
            if (Target && !Target.CanInteract(this)) Target = HighlightTarget = null;
            HighlightTarget = Target;
            if (!HighlightTarget)
            {
                float best = float.PositiveInfinity;
                foreach (var hit in Physics.SphereCastAll(view.transform.position, .22f, view.transform.forward, Mission.rules.interactDistance, ~0, QueryTriggerInteraction.Ignore))
                {
                    if (hit.collider.transform.IsChildOf(transform)) continue;
                    var item = hit.collider.GetComponentInParent<ThiefInteractable>();
                    if (!item || item.Acquired || !item.CanInteract(this) || (!item.relation && !item.emitter && item.kind != InteractionKind.Key && item.kind != InteractionKind.Treasure && item.kind != InteractionKind.Collectible)) continue;
                    var point = hit.collider.ClosestPoint(view.transform.position);
                    var direction = point - view.transform.position;
                    if (direction.sqrMagnitude < .0001f) continue;
                    bool blocked = false;
                    foreach (var obstacle in Physics.RaycastAll(view.transform.position, direction.normalized, direction.magnitude, ~0, QueryTriggerInteraction.Ignore))
                        if (!obstacle.collider.transform.IsChildOf(transform) && obstacle.collider.GetComponentInParent<ThiefInteractable>() != item) { blocked = true; break; }
                    if (blocked) continue;
                    float score = Vector3.Angle(view.transform.forward, direction) * .1f + direction.magnitude;
                    if (score < best) { best = score; HighlightTarget = item; }
                }
            }
            // The softly highlighted object is also the actionable target: small meshes
            // should not require a second, pixel-perfect aim after becoming highlighted.
            if (!Target && HighlightTarget) Target = HighlightTarget;
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
            if (charge.Finish())
            {
                if (heldPresentation) heldPresentation.Burst();
                Mission.World.Blast(transform.position + Vector3.up, charge.Grade, true);
            }
            Mission.Notify("보유 시간 초과! 잠시 기절했습니다. 원래 소리는 복구되었습니다.");
        }
        public void Stun(float seconds) { stunUntil = Time.time + seconds; Target = HighlightTarget = null; if (connection) connection.enabled = false; }
        public void PlaceAt(Transform spawn)
        {
            controller.enabled = false;
            controller.height = standingHeight; controller.center = standingCenter; IsCrouching = false;
            cameraVelocity = fovVelocity = stridePhase = 0;
            transform.SetPositionAndRotation(spawn.position, spawn.rotation);
            controller.enabled = true;
            pitch = gravitySpeed = 0;
            view.transform.localRotation = Quaternion.identity;
            view.transform.localPosition = standingCamera; view.fieldOfView = standingFov;
            Physics.SyncTransforms();
        }
        public void ResetPracticeState()
        {
            Held?.Finish(); Held = null; stunUntil = connectionUntil = 0;
            Inventory.Clear(); Target = HighlightTarget = null;
            if (connection) connection.enabled = false;
        }
        void OnDestroy() { Held?.Finish(); if (lineMaterial) Destroy(lineMaterial); }
    }
}
