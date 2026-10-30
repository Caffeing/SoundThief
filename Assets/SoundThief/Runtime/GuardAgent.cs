using UnityEngine;
using UnityEngine.AI;
using TMPro;

namespace SoundThief
{
    public enum GuardState { Sleeping, InvestigatingSound, Chasing, LastSeen, Alert, Returning, Stunned, Panic, Patrolling }

    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class GuardAgent : MonoBehaviour
    {
        public SoundWorld world;
        public TextMeshPro statusLabel;
        public bool startsAwake;
        public Transform[] patrolPoints = new Transform[0];
        public float patrolWaitSeconds = 2;
        public GuardState State { get; private set; } = GuardState.Sleeping;
        public Vector3 Goal { get; private set; }
        public int ReactingGrade { get; private set; }
        NavMeshAgent agent;
        SoundEmitter listening;
        bool continuousSound;
        Vector3 home;
        Quaternion homeRotation;
        float wakeStimulus, stateUntil, lastStimulus;
        int patrolIndex;
        bool patrolWaiting;
        bool PracticeInactive => world && world.player && world.player.Mission.isGuild
            && world.player.Mission.practice && !world.player.Mission.practice.IsPlayerInside;
        void Start()
        {
            agent = GetComponent<NavMeshAgent>(); agent.speed = world.rules.guardSpeed;
            agent.angularSpeed = 180; agent.stoppingDistance = 0.15f;
            home = transform.position; homeRotation = transform.rotation; Goal = home; world.Guards.Add(this);
            if (startsAwake) { State = GuardState.Patrolling; Goal = home; Stop(); }
        }
        public void Hear(Vector3 position, int grade, SoundEmitter source)
        {
            if (PracticeInactive) return;
            if (State == GuardState.Stunned || State == GuardState.Panic || State == GuardState.Chasing || State == GuardState.LastSeen) return;
            if (!world.CanHear(position, transform.position, grade, out float proximity)) return;
            lastStimulus = Time.time;
            if (State == GuardState.Sleeping)
            {
                wakeStimulus += grade * proximity + 0.1f;
                if (wakeStimulus < world.rules.wakeThreshold) return;
            }
            if (State == GuardState.InvestigatingSound)
            {
                if (source && source == listening) return;
                if (grade <= ReactingGrade) return;
            }
            ReactingGrade = grade; listening = source; continuousSound = source != null;
            Go(GuardState.InvestigatingSound, position);
        }
        bool SeesPlayer()
        {
            Vector3 eye = transform.position + Vector3.up * 1.6f;
            Vector3 delta = world.player.view.transform.position - eye;
            return delta.magnitude <= world.rules.sightRange && Vector3.Angle(transform.forward, delta) <= world.rules.sightAngle * 0.5f
                && !SoundWorld.Blocked(eye, eye + delta);
        }
        void Update()
        {
            if (!agent || !world.player.Mission.CanAct || PracticeInactive) return;
            // Contact ends a contract in every guard state, including sleep/stun/panic.
            if (!world.player.Mission.isGuild && Vector3.Distance(transform.position, world.player.transform.position) < 0.85f)
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
            if (State == GuardState.Patrolling && !patrolWaiting && !agent.hasPath) ResumeDuty();
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
                Stop(); transform.rotation = homeRotation; wakeStimulus = 0; ResumeDuty();
            }
            else if (State == GuardState.Patrolling && Arrived())
            {
                if (!patrolWaiting) { Stop(); patrolWaiting = true; stateUntil = Time.time + patrolWaitSeconds; }
                if (Time.time >= stateUntil) { patrolIndex = (patrolIndex + 1) % Mathf.Max(1, patrolPoints == null ? 0 : patrolPoints.Length); ResumeDuty(); }
            }
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
            if (PracticeInactive) return false;
            if (State == GuardState.Panic || State == GuardState.Stunned) return false;
            Stop(); State = GuardState.Stunned; stateUntil = Time.time + world.rules.guardStun; return true;
        }
        public void ResetPractice()
        {
            if (!world.player.Mission.isGuild) return;
            Stop();
            if (agent && agent.isOnNavMesh) { agent.Warp(home); agent.velocity = Vector3.zero; }
            transform.rotation = homeRotation; State = GuardState.Sleeping;
            Goal = home;
            wakeStimulus = 0; ReactingGrade = 0; listening = null;
            stateUntil = lastStimulus = 0;
            continuousSound = false; patrolIndex = 0; patrolWaiting = false;
            if (statusLabel)
            {
                statusLabel.text = "zzz";
                statusLabel.color = new Color(0.7f, 1, 0.9f);
                statusLabel.transform.rotation = world.player.view.transform.rotation;
            }
        }
        void EnterAlert() { Stop(); State = GuardState.Alert; stateUntil = Time.time + world.rules.alertSeconds; ReactingGrade = 0; listening = null; }
        void Go(GuardState state, Vector3 position)
        {
            State = state; Goal = position;
            if (agent && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                if (NavMesh.SamplePosition(position, out var hit, 8, agent.areaMask))
                {
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
        void Stop() { if (agent && agent.isOnNavMesh) { agent.ResetPath(); agent.isStopped = true; agent.velocity = Vector3.zero; } }
        bool Arrived() => agent.isOnNavMesh && !agent.pathPending && agent.remainingDistance < 0.4f;
        void OnDestroy() { if (world) world.Guards.Remove(this); }
    }
}
