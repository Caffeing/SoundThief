using UnityEngine;
using UnityEngine.AI;
using TMPro;

namespace SoundThief
{
    public enum GuardState { Sleeping, InvestigatingSound, Chasing, LastSeen, Alert, Returning, Stunned, Panic }

    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class GuardAgent : MonoBehaviour
    {
        public SoundWorld world;
        public TextMeshPro statusLabel;
        public GuardState State { get; private set; } = GuardState.Sleeping;
        public Vector3 Goal { get; private set; }
        public int ReactingGrade { get; private set; }
        NavMeshAgent agent;
        SoundEmitter listening;
        Vector3 home;
        Quaternion homeRotation;
        float wakeStimulus, stateUntil, soundUntil, lastStimulus;
        void Start()
        {
            agent = GetComponent<NavMeshAgent>(); agent.speed = world.rules.guardSpeed;
            agent.angularSpeed = 180; agent.stoppingDistance = 0.15f;
            home = transform.position; homeRotation = transform.rotation; world.Guards.Add(this);
        }
        public void Hear(Vector3 position, int grade, SoundEmitter source)
        {
            if (State == GuardState.Stunned || State == GuardState.Panic || State == GuardState.Chasing || State == GuardState.LastSeen) return;
            float distance = Vector3.Distance(transform.position, position);
            if (distance > grade * world.rules.hearingPerGrade) return;
            lastStimulus = Time.time;
            if (State == GuardState.Sleeping)
            {
                wakeStimulus += grade * Mathf.Clamp01(1 - distance / (grade * world.rules.hearingPerGrade)) + 0.1f;
                if (wakeStimulus < world.rules.wakeThreshold) return;
            }
            if (State == GuardState.InvestigatingSound)
            {
                if (source && source == listening) { soundUntil = Time.time + 0.9f; return; }
                if (grade <= ReactingGrade) return;
            }
            ReactingGrade = grade; listening = source; soundUntil = Time.time + 0.9f;
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
            if (!agent || !world.player.Mission.CanAct) return;
            // Contact ends a contract in every guard state, including sleep/stun/panic.
            if (!world.player.Mission.isGuild && Vector3.Distance(transform.position, world.player.transform.position) < 0.85f)
            { world.player.Mission.Caught(); return; }
            if (statusLabel)
            {
                statusLabel.text = State == GuardState.Sleeping ? "zzz" : State == GuardState.Stunned ? "×" : State == GuardState.Chasing ? "!!"
                    : State == GuardState.InvestigatingSound || State == GuardState.LastSeen ? "!" : State == GuardState.Returning ? "..."
                    : State == GuardState.Panic ? "?\n패닉 · 면역" : "?";
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
            if (SeesPlayer()) { Go(GuardState.Chasing, world.player.transform.position); return; }
            if (State == GuardState.Chasing) { Go(GuardState.LastSeen, Goal); return; }
            if (State == GuardState.InvestigatingSound)
            {
                if ((listening && !listening.Active) || (!listening && Time.time > soundUntil)) EnterAlert();
                else if (Arrived()) Stop();
            }
            else if (State == GuardState.LastSeen && Arrived()) EnterAlert();
            else if (State == GuardState.Alert && Time.time - lastStimulus >= world.rules.alertSeconds && Time.time >= stateUntil) Go(GuardState.Returning, home);
            else if (State == GuardState.Returning && Arrived())
            {
                Stop(); transform.rotation = homeRotation; State = GuardState.Sleeping; wakeStimulus = 0;
            }
        }
        public bool Stun()
        {
            if (State == GuardState.Panic || State == GuardState.Stunned) return false;
            Stop(); State = GuardState.Stunned; stateUntil = Time.time + world.rules.guardStun; return true;
        }
        public void ResetPractice()
        {
            if (!world.player.Mission.isGuild) return;
            Stop(); if (agent && agent.isOnNavMesh) agent.Warp(home);
            transform.rotation = homeRotation; State = GuardState.Sleeping;
            wakeStimulus = 0; ReactingGrade = 0; listening = null;
            stateUntil = soundUntil = lastStimulus = 0;
        }
        void EnterAlert() { Stop(); State = GuardState.Alert; stateUntil = Time.time + world.rules.alertSeconds; ReactingGrade = 0; listening = null; }
        void Go(GuardState state, Vector3 position)
        {
            State = state; Goal = position;
            if (agent && agent.isOnNavMesh)
            {
                agent.isStopped = false;
                if (NavMesh.SamplePosition(position, out var hit, 3, NavMesh.AllAreas)) agent.SetDestination(hit.position);
            }
        }
        void Stop() { if (agent && agent.isOnNavMesh) { agent.isStopped = true; agent.ResetPath(); } }
        bool Arrived() => agent.isOnNavMesh && !agent.pathPending && agent.remainingDistance < 0.4f;
        void OnDestroy() { if (world) world.Guards.Remove(this); }
    }
}
