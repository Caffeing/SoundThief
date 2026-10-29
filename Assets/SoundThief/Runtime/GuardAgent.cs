using UnityEngine;
using UnityEngine.AI;
using TMPro;

namespace SoundThief
{
    public enum GuardState { Sleeping, InvestigatingSound, Chasing, LastSeen, Alert, Returning, Stunned, Panic, Watching }
    // JinHo 연결부: Original=원래 조사, Hold=기존 목표로 이동/도착 후 대기, ReturnHome=원래 복귀.
    public enum InvestigationDirective { Original, Hold, ReturnHome }
    // 씬 전용 정책은 체류·퇴장 시점을 판단하고, 실제 이동과 상태 전환은 기존 GuardAgent가 수행한다.
    public interface IGuardInvestigationPolicy
    {
        // 퇴장 중 같은 출력에 즉시 재유인되는 것을 정책에서 막을 수 있다. 새 소리는 만들지 않는다.
        bool AllowsHearing(GuardAgent guard, SoundEmitter source);
        InvestigationDirective Evaluate(GuardAgent guard, SoundEmitter source);
    }

    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class GuardAgent : MonoBehaviour
    {
        public SoundWorld world;
        public TextMeshPro statusLabel;
        // JinHo G2용 선택 설정: 시작부터 Watching. 기본 false이면 기존 Sleeping 시작을 유지한다.
        public bool startsAwake;
        // JinHo G1용 선택 설정: 한 번 깨어난 뒤 원위치에 복귀해도 Watching을 유지한다.
        public bool stayAwakeAfterFirstWake;
        public GuardState State { get; private set; } = GuardState.Sleeping;
        public Vector3 Goal { get; private set; }
        public int ReactingGrade { get; private set; }
        NavMeshAgent agent;
        SoundEmitter listening;
        Vector3 home;
        Quaternion homeRotation;
        float wakeStimulus, stateUntil, soundUntil, lastStimulus;
        // 시작 각성·실제 소리 반응·기절의 각성 이력. 연습 초기화에서는 해제한다.
        bool hasAwakened;
        // Unity 소유자 참조로 컴포넌트 생존 여부를 확인하며 정책 하나만 연결한다.
        MonoBehaviour investigationOwner;
        IGuardInvestigationPolicy investigationPolicy;
        public bool RegisterInvestigationPolicy(MonoBehaviour owner)
        {
            // 다른 씬/다른 소유자의 정책을 거부해 중복 대기 처리와 씬 간 간섭을 막는다.
            if (!owner || owner.gameObject.scene != gameObject.scene || !(owner is IGuardInvestigationPolicy policy)) return false;
            if (investigationOwner && investigationOwner != owner) return false;
            investigationOwner = owner; investigationPolicy = policy; return true;
        }
        public void UnregisterInvestigationPolicy(MonoBehaviour owner)
        {
            // 등록한 소유자만 자신의 정책을 해제한다.
            if (investigationOwner != owner) return;
            investigationOwner = null; investigationPolicy = null;
        }
        bool PracticeInactive => world && world.player && world.player.Mission.isGuild
            && world.player.Mission.practice && !world.player.Mission.practice.IsPlayerInside;
        void Start()
        {
            agent = GetComponent<NavMeshAgent>(); agent.speed = world.rules.guardSpeed;
            agent.angularSpeed = 180; agent.stoppingDistance = 0.15f;
            home = transform.position; homeRotation = transform.rotation; Goal = home; world.Guards.Add(this);
            // 기존 시작 좌표/방향 저장은 유지하며 선택 설정이 켜진 경비의 시작 상태만 바꾼다.
            hasAwakened = startsAwake;
            if (startsAwake) State = GuardState.Watching;
        }
        public void Hear(Vector3 position, int grade, SoundEmitter source)
        {
            if (PracticeInactive) return;
            // 정책 연결 시 재유인 허용을 먼저 확인한다. 허용돼도 아래 원본 청각 조건은 모두 거친다.
            if (investigationOwner && !investigationPolicy.AllowsHearing(this, source)) return;
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
            // 원본 청각·각성 조건을 통과한 실제 소리 반응을 기록한다.
            hasAwakened = true;
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
            if (!agent || !world.player.Mission.CanAct || PracticeInactive) return;
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
                // 시야 발견·기절·패닉 처리가 우선이며, 소리 조사 상태에서만 체류 정책을 평가한다.
                // 미연결 시 Original로 원래 출력 종료/경계 흐름을 그대로 실행한다.
                var directive = investigationOwner ? investigationPolicy.Evaluate(this, listening) : InvestigationDirective.Original;
                if (directive == InvestigationDirective.ReturnHome)
                {
                    // 소리 연결을 정리하고 Start에서 저장한 원래 좌표로 기존 이동 기능을 통해 복귀한다.
                    ReactingGrade = 0; listening = null; Go(GuardState.Returning, home); return;
                }
                // 이동 중에는 기존 목표로 계속 가고 도착하면 멈춘다. 원본 경계 전환만 이번 프레임 보류한다.
                if (directive == InvestigationDirective.Hold) { if (Arrived()) Stop(); return; }
                if ((listening && !listening.Active) || (!listening && Time.time > soundUntil)) EnterAlert();
                else if (Arrived()) Stop();
            }
            else if (State == GuardState.LastSeen && Arrived()) EnterAlert();
            else if (State == GuardState.Alert && Time.time - lastStimulus >= world.rules.alertSeconds && Time.time >= stateUntil) Go(GuardState.Returning, home);
            else if (State == GuardState.Returning && Arrived())
            {
                Stop(); transform.rotation = homeRotation;
                // 선택 설정/각성 이력에 따라 복귀 후 상태를 정한다. 두 설정이 false인 기존 씬은 다시 잔다.
                State = startsAwake || (stayAwakeAfterFirstWake && hasAwakened) ? GuardState.Watching : GuardState.Sleeping;
                wakeStimulus = 0;
            }
        }
        public bool Stun()
        {
            if (PracticeInactive) return false;
            if (State == GuardState.Panic || State == GuardState.Stunned) return false;
            // 기절도 각성 이력에 포함하지만 원래 기절·패닉 시간과 전환은 유지한다.
            Stop(); hasAwakened = true; State = GuardState.Stunned; stateUntil = Time.time + world.rules.guardStun; return true;
        }
        public void ResetPractice()
        {
            if (!world.player.Mission.isGuild) return;
            Stop();
            if (agent && agent.isOnNavMesh) { agent.Warp(home); agent.velocity = Vector3.zero; }
            transform.rotation = homeRotation; State = GuardState.Sleeping;
            // 기존 연습 초기화 때 각성 이력도 비워 다음 연습에 남기지 않는다.
            hasAwakened = false;
            Goal = home;
            wakeStimulus = 0; ReactingGrade = 0; listening = null;
            stateUntil = soundUntil = lastStimulus = 0;
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
                if (NavMesh.SamplePosition(position, out var hit, 3, NavMesh.AllAreas)) agent.SetDestination(hit.position);
            }
        }
        void Stop() { if (agent && agent.isOnNavMesh) { agent.isStopped = true; agent.ResetPath(); } }
        bool Arrived() => agent.isOnNavMesh && !agent.pathPending && agent.remainingDistance < 0.4f;
        void OnDestroy() { if (world) world.Guards.Remove(this); }
    }
}
