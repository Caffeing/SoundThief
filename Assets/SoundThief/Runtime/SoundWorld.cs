using System.Collections.Generic;
using UnityEngine;

namespace SoundThief
{
    // JinHo 연결부: 같은 씬의 선택적 소리 전달 정책. 청각 전달 허용과 플레이어 재생 음량만 계산한다.
    // 추출·주입·충전·경비 상태는 기존 시스템이 담당하며, 정책 미연결 씬은 기존 처리를 사용한다.
    public interface ISoundPropagationPolicy
    {
        // 기존 Hear를 전달할지 반환한다. 정책 안에서 별도 Hear를 호출하지 않아 중복 반응을 막는다.
        bool AllowsHearing(Vector3 position, int grade, SoundEmitter source, GuardAgent receiver);
        // 기존 위치 음향의 음량 배율을 반환한다. 별도 소리를 재생하지 않는다.
        float AudioGain(Vector3 position, int grade, SoundEmitter source, Vector3 listener);
    }

    public sealed class SoundWorld : MonoBehaviour
    {
        public SoundRules rules;
        public ThiefPlayer player;
        public readonly List<GuardAgent> Guards = new List<GuardAgent>();
        public int BlastCount { get; private set; }
        public Vector3 LastBlastPosition { get; private set; }
        // 인터페이스는 계산에, Unity 소유자 참조는 등록 권한과 컴포넌트 파괴 여부 확인에 사용한다.
        public ISoundPropagationPolicy PropagationPolicy { get; private set; }
        MonoBehaviour policyOwner;
        readonly Dictionary<int, AudioClip> tones = new Dictionary<int, AudioClip>();

        public bool RegisterPropagationPolicy(MonoBehaviour owner)
        {
            // 같은 씬의 정책 하나만 연결한다. 같은 소유자의 재등록은 허용하고 다른 소유자의 중복 등록은 거부한다.
            if (!owner || owner.gameObject.scene != gameObject.scene || !(owner is ISoundPropagationPolicy policy)) return false;
            if (policyOwner && policyOwner != owner) return false;
            policyOwner = owner; PropagationPolicy = policy; return true;
        }
        public void UnregisterPropagationPolicy(MonoBehaviour owner)
        {
            // 등록한 소유자만 자신의 정책을 해제할 수 있다.
            if (policyOwner != owner) return;
            policyOwner = null; PropagationPolicy = null;
        }

        void Awake()
        {
            for (int grade = 1; grade <= 3; grade++)
            {
                var samples = new float[11025];
                for (int i = 0; i < samples.Length; i++)
                    samples[i] = Mathf.Sin(i * (180 + grade * 110) * 2 * Mathf.PI / 22050f) * Mathf.Exp(-i / 1800f) * 0.18f;
                var clip = AudioClip.Create("Sound grade " + grade, samples.Length, 1, 22050, false);
                clip.SetData(samples, 0); tones.Add(grade, clip);
            }
        }
        public void Pulse(Vector3 position, int grade, SoundEmitter source)
        {
            // 소유자가 없거나 파괴됐다면 원래 전달 흐름을 사용한다.
            var policy = policyOwner ? PropagationPolicy : null;
            // 허용된 경비에 한 번만 전달한다. 원본 Hear의 거리·등급·각성 조건도 이어서 적용된다.
            foreach (var guard in Guards)
                if (guard && (policy == null || policy.AllowsHearing(position, grade, source, guard))) guard.Hear(position, grade, source);
            if (tones.TryGetValue(grade, out var clip))
            {
                var soundObject = new GameObject("Positional sound"); soundObject.transform.SetParent(transform); soundObject.transform.position = position;
                var feedback = soundObject.AddComponent<AudioSource>();
                feedback.spatialBlend = 1; feedback.minDistance = 1; feedback.maxDistance = rules.hearingPerGrade * grade;
                // 기존 AudioSource의 음량만 감쇠한다. 원래 3D 거리 감쇠와 소리 고리 표시는 유지한다.
                if (policy != null) feedback.volume = Mathf.Clamp01(policy.AudioGain(position, grade, source, player.view.transform.position));
                feedback.PlayOneShot(clip);
                Destroy(soundObject, clip.length + 0.1f);
            }
            var ring = new GameObject("Sound pulse").AddComponent<SoundRing>();
            ring.transform.SetParent(transform);
            ring.Init(position, grade, false);
        }
        public void Blast(Vector3 position, int grade, bool stunPlayer)
        {
            BlastCount++; LastBlastPosition = position;
            Pulse(position, grade, null);
            foreach (var guard in Guards)
                if (guard && Vector3.Distance(position, guard.transform.position + Vector3.up) <= rules.blastPerGrade * grade
                    && !Blocked(position + Vector3.up * 0.08f, guard.transform.position + Vector3.up))
                    guard.Stun();
            if (stunPlayer) player.Stun(rules.playerStun);
            var ring = new GameObject("Blast").AddComponent<SoundRing>();
            ring.transform.SetParent(transform);
            ring.Init(position, grade, true);
        }
        // 원래 시야·충격 차폐는 레이어8 충돌체를 검사한다. 기본 청각에는 자동 적용하지 않는다.
        // JinHo의 벽·문·통로에 따른 청각 차폐는 위 전달 정책에서 따로 판단한다.
        public static bool Blocked(Vector3 from, Vector3 to) => Physics.Linecast(from, to, 1 << 8, QueryTriggerInteraction.Ignore);
        public void ClearFeedback()
        {
            foreach (var source in GetComponentsInChildren<AudioSource>()) { source.Stop(); Destroy(source.gameObject); }
            foreach (var ring in GetComponentsInChildren<SoundRing>()) Destroy(ring.gameObject);
        }
        public void ResetFeedback()
        {
            ClearFeedback(); BlastCount = 0; LastBlastPosition = Vector3.zero;
        }
        void OnDestroy() { foreach (var clip in tones.Values) if (clip) Destroy(clip); }
    }

}

