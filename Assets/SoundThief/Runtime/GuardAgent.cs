using UnityEngine;
using UnityEngine.AI;
using TMPro;
using System.Linq;

namespace SoundThief
{
    public enum GuardState { Sleeping, InvestigatingSound, Chasing, LastSeen, Alert, Returning, Stunned, Panic, Patrolling }

    [DefaultExecutionOrder(-10), RequireComponent(typeof(NavMeshAgent))]
    public sealed class GuardAgent : MonoBehaviour
    {
        public SoundWorld world;
        public TextMeshPro statusLabel;
        public bool startsAwake;
        public Transform[] patrolPoints = new Transform[0];
        public float patrolWaitSeconds = 2;
        [Range(0, 3)] public int instantWakeGrade;
        public GuardState State { get; private set; } = GuardState.Sleeping;
        public Vector3 Goal { get; private set; }
        public int ReactingGrade { get; private set; }
        public bool IsTurning { get; private set; }
        NavMeshAgent agent;
        SoundEmitter listening;
        bool continuousSound;
        Vector3 home;
        Quaternion homeRotation;
        float wakeStimulus, stateUntil, lastStimulus;
        int patrolIndex;
        bool patrolWaiting;
        ThiefInteractable[] doors;
        Vector3 requestedGoal;
        bool openingDoor, waitingForDoor;
        float doorRepathAt;
        float turnVelocity, nextPathAt;
        bool restoringFacing;
        void Start()
        {
            agent = GetComponent<NavMeshAgent>(); agent.speed = world.rules.guardSpeed;
            agent.updateRotation = false; agent.stoppingDistance = 0.15f;
            home = transform.position; homeRotation = transform.rotation; world.Guards.Add(this);
            doors = gameObject.scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<ThiefInteractable>()).Where(i => i.kind == InteractionKind.Door).ToArray();
            if (startsAwake) { State = GuardState.Patrolling; Goal = home; Stop(); }
        }
        public void Hear(Vector3 position, int grade, SoundEmitter source)
        {
            if (openingDoor) return;
            if (State == GuardState.Stunned || State == GuardState.Panic || State == GuardState.Chasing || State == GuardState.LastSeen) return;
            if (!world.CanHear(position, transform.position, grade, out float proximity)) return;
            lastStimulus = Time.time;
            if (State == GuardState.Sleeping)
            {
                wakeStimulus += grade * proximity + 0.1f;
                if ((instantWakeGrade == 0 || grade < instantWakeGrade) && wakeStimulus < world.rules.wakeThreshold) return;
            }
            if (State == GuardState.InvestigatingSound)
            {
                if (source && source == listening) return;
                if (grade <= ReactingGrade) return;
            }
            ReactingGrade = grade; listening = source; continuousSound = source != null;
            Go(GuardState.InvestigatingSound, position);
        }
        public bool SeesPlayer()
        {
            Vector3 eye = transform.position + Vector3.up * 1.6f;
            Vector3 delta = world.player.view.transform.position - eye;
            return delta.magnitude <= world.rules.sightRange && Vector3.Angle(transform.forward, delta) <= world.rules.sightAngle * 0.5f
                && !SoundWorld.Blocked(eye, eye + delta);
        }
        void Update()
        {
            if (!agent || !world.player.Mission.CanAct) return;
            // Contact ends a contract in every guard state, including sleep/stun/panic.
            if (Vector3.Distance(transform.position, world.player.transform.position) < 0.85f)
            { world.player.Mission.Caught(); return; }
            if (statusLabel)
            {
                statusLabel.text = State == GuardState.Sleeping ? "zzz" : State == GuardState.Stunned ? "×" : State == GuardState.Chasing ? "!!"
                    : State == GuardState.InvestigatingSound || State == GuardState.LastSeen ? "!" : State == GuardState.Returning ? "..."
                    : State == GuardState.Panic ? "?\n패닉 · 면역" : State == GuardState.Patrolling ? "..." : "?";
                statusLabel.transform.rotation = world.player.view.transform.rotation;
                statusLabel.color = State == GuardState.Chasing ? new Color(1, 0.3f, 0.25f) : new Color(0.7f, 1, 0.9f);
            }
            if (State == GuardState.Stunned)
            {
                if (Time.time >= stateUntil) { State = GuardState.Panic; stateUntil = Time.time + world.rules.panicSeconds; }
                return;
            }
            if (State == GuardState.Panic)
            {
                if (Time.time >= stateUntil) EnterAlert();
                return;
            }
            if (State == GuardState.Sleeping) { wakeStimulus = Mathf.Max(0, wakeStimulus - Time.deltaTime * 0.12f); return; }
            if (waitingForDoor)
            {
                if (Time.time >= doorRepathAt) { waitingForDoor = false; Go(State, requestedGoal); }
                return;
            }
            if (State == GuardState.Patrolling && !patrolWaiting && !agent.hasPath) ResumeDuty();
            if (State != GuardState.Alert && TryOpenRouteDoor()) return;
            if (SeesPlayer()) { Go(GuardState.Chasing, world.player.transform.position); return; }
            if (State == GuardState.Chasing) { Go(GuardState.LastSeen, Goal); return; }
            if (State == GuardState.InvestigatingSound)
            {
                if (continuousSound && (!listening || !listening.Active || listening.Pending || !world.CanHear(listening.transform.position, transform.position, listening.CurrentGrade, out _))) EnterAlert();
                else if (Arrived()) { if (continuousSound) Stop(); else EnterAlert(); }
            }
            else if (State == GuardState.LastSeen && Arrived()) EnterAlert();
            else if (State == GuardState.Alert && Time.time - lastStimulus >= world.rules.alertSeconds && Time.time >= stateUntil) Go(GuardState.Returning, home);
            else if (State == GuardState.Returning && Arrived())
            {
                Stop(); restoringFacing = true;
                if (Quaternion.Angle(transform.rotation, homeRotation) > 3) return;
                restoringFacing = false; wakeStimulus = 0; ResumeDuty();
            }
            else if (State == GuardState.Patrolling && Arrived())
            {
                if (!patrolWaiting) { Stop(); patrolWaiting = true; stateUntil = Time.time + patrolWaitSeconds; }
                if (Time.time >= stateUntil) { patrolIndex = (patrolIndex + 1) % Mathf.Max(1, patrolPoints == null ? 0 : patrolPoints.Length); ResumeDuty(); }
            }
        }
        void LateUpdate()
        {
            IsTurning = false;
            if (!agent || !agent.isOnNavMesh || !world || !world.player.Mission.CanAct) return;
            if (State == GuardState.Sleeping || State == GuardState.Stunned || State == GuardState.Panic) return;
            Vector3 direction = restoringFacing ? homeRotation * Vector3.forward
                : agent.hasPath && !agent.pathPending ? agent.steeringTarget - transform.position : Vector3.zero;
            direction.y = 0;
            float speed = State == GuardState.Chasing ? world.rules.guardChaseSpeed : world.rules.guardSpeed;
            if (direction.sqrMagnitude < .0025f) { turnVelocity = 0; agent.speed = speed; return; }
            float angle = Vector3.Angle(transform.forward, direction);
            float yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            float current = transform.eulerAngles.y;
            float dt = Mathf.Min(Time.deltaTime, .05f);
            float next = Mathf.SmoothDampAngle(current, yaw, ref turnVelocity, .16f,
                State == GuardState.Chasing ? 300 : 180, dt);
            transform.rotation = Quaternion.Euler(0, next, 0);
            IsTurning = angle > 8;
            // Face the route first: don't slide backwards while performing a large turn.
            float alignment = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(25, 100, angle));
            agent.speed = speed * alignment;
        }
        void ResumeDuty()
        {
            patrolWaiting = false;
            if (!startsAwake) { State = GuardState.Sleeping; return; }
            if (patrolPoints != null && patrolPoints.Length > 0 && patrolPoints[patrolIndex % patrolPoints.Length])
                Go(GuardState.Patrolling, patrolPoints[patrolIndex % patrolPoints.Length].position);
            else { State = GuardState.Patrolling; Goal = home; Stop(); }
        }
        public bool Stun()
        {
            if (State == GuardState.Panic || State == GuardState.Stunned) return false;
            waitingForDoor = false; restoringFacing = false; Stop(); State = GuardState.Stunned; stateUntil = Time.time + world.rules.guardStun; return true;
        }
        void EnterAlert() { waitingForDoor = false; restoringFacing = false; Stop(); State = GuardState.Alert; stateUntil = Time.time + world.rules.alertSeconds; ReactingGrade = 0; listening = null; }
        void Go(GuardState state, Vector3 position)
        {
            restoringFacing = false;
            State = state; Goal = position; requestedGoal = position;
            if (agent && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                if (NavMesh.SamplePosition(position, out var hit, 8, agent.areaMask))
                {
                    if (agent.hasPath && !agent.isStopped && Time.time < nextPathAt && (agent.destination - hit.position).sqrMagnitude < .36f) return;
                    nextPathAt = Time.time + .15f;
                    var path = new NavMeshPath();
                    if (agent.CalculatePath(hit.position, path) && path.corners.Length > 0)
                    {
                        // A locked door may yield a partial route: investigate its reachable end.
                        if (path.status != NavMeshPathStatus.PathComplete) Goal = path.corners[path.corners.Length - 1];
                        agent.SetPath(path);
                    }
                    else { Goal = transform.position; Stop(); }
                }
                else { Goal = transform.position; Stop(); }
            }
        }
        bool TryOpenRouteDoor()
        {
            if (doors == null) return false;
            foreach (var door in doors)
            {
                if (!door || door.IsOpen || !door.guardsCanOpen || !door.IsUnlocked) continue;
                var leaf = door.GetComponentInChildren<Collider>(); if (!leaf) continue;
                var center = leaf.bounds.center; center.y = transform.position.y;
                var normal = door.transform.forward;
                if (Vector3.Dot(transform.position - center, normal) * Vector3.Dot(requestedGoal - center, normal) >= 0) continue;
                var near = leaf.bounds.ClosestPoint(transform.position + Vector3.up); near.y = transform.position.y;
                if (Vector3.Distance(transform.position, near) > 1.6f) continue;
                bool opened; openingDoor = true;
                try { opened = door.OpenForGuard(this); } finally { openingDoor = false; }
                if (!opened) continue;
                Stop(); waitingForDoor = true; doorRepathAt = Time.time + .25f; return true;
            }
            return false;
        }
        void Stop() { if (agent && agent.isOnNavMesh) { agent.ResetPath(); agent.isStopped = true; agent.velocity = Vector3.zero; } }
        bool Arrived() => agent.isOnNavMesh && !agent.pathPending && agent.remainingDistance < 0.4f;
        void OnDestroy() { if (world) world.Guards.Remove(this); }
    }
}
