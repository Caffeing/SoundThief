using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace SoundThief.JinHo.Editor
{
    // Stage2 전용 설정 도구. 다른 씬을 열거나 공유 규칙/프리팹을 덮어쓰지 않는다.
    public static class Stage2OriginalLureSetup
    {
        public const string ScenePath="Assets/Scenes/Stages/Stage2.unity";
        public const string RulesPath="Assets/JinHo/Settings/Stage2 Original Lure Rules.asset";
        [MenuItem("Sound Thief/JinHo/Stage2/Restore Original Lure Settings")]
        public static void ApplyFromMenu()=>Apply();
        public static string Apply()
        {
            var scene=SceneManager.GetActiveScene();
            if(EditorApplication.isPlaying||scene.isDirty||scene.path!=ScenePath)
                throw new InvalidOperationException("현재 Stage2를 Play 정지/저장 완료 상태로 연 뒤 실행하세요. 다른 씬이나 미저장 변경을 자동 처리하지 않습니다.");
            var bindings=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<JinHoStageBindings>(true)).Single();
            var context=bindings.context;var stay=bindings.GetComponent<JinHoRoomStay>();
            if(!context||!context.mission||!context.mission.World||!stay||!bindings.guard1||!bindings.guard2)
                throw new InvalidOperationException("Stage2의 기존 유인 참조가 완전하지 않습니다.");
            var rules=AssetDatabase.LoadAssetAtPath<SoundRules>(RulesPath);
            if(!rules)
            {
                var original=AssetDatabase.LoadAssetAtPath<SoundRules>("Assets/JinHo/Settings/JinHo Rules.asset");
                if(!original)throw new InvalidOperationException("원래 JinHo 규칙 자산이 없습니다.");
                rules=Object.Instantiate(original);rules.name="Stage2 Original Lure Rules";
                // 원래 JinHo는 조사/추격 모두 같은 경비 속도를 사용했다.
                rules.guardChaseSpeed=rules.guardSpeed;
                AssetDatabase.CreateAsset(rules,RulesPath);
            }
            Undo.RecordObjects(new Object[]{context,context.mission,context.mission.World,bindings,stay},"Restore Stage2 original lure settings");
            context.rules=rules;context.mission.rules=rules;context.mission.World.rules=rules;
            bindings.immediateInjection=true;stay.keepAcceptedLureWhileActive=true;
            stay.firstExtraSeconds=2;stay.staySeconds=3;
            foreach(var emitter in context.SceneComponents<SoundEmitter>())
            {
                Undo.RecordObject(emitter,"Stage2 immediate injection");emitter.injectionDelaySeconds=0;
                EditorUtility.SetDirty(emitter);PrefabUtility.RecordPrefabInstancePropertyModifications(emitter);
            }
            foreach(var guard in new[]{bindings.guard1,bindings.guard2})
            {
                Undo.RecordObject(guard,"Stage2 original guard duty");
                guard.startsAwake=guard==bindings.guard2;guard.stayAwakeAfterFirstWake=true;
                guard.instantWakeGrade=0;guard.patrolPoints=Array.Empty<Transform>();
                EditorUtility.SetDirty(guard);PrefabUtility.RecordPrefabInstancePropertyModifications(guard);
            }
            foreach(var item in new Object[]{context,context.mission,context.mission.World,bindings,stay})
            {EditorUtility.SetDirty(item);PrefabUtility.RecordPrefabInstancePropertyModifications(item);}
            EditorSceneManager.MarkSceneDirty(scene);AssetDatabase.SaveAssetIfDirty(rules);
            return "Stage2 전용 원래 유인 규칙 적용. 다른 씬/공유 규칙/프리팹 변경 없음. 씬 저장은 별도입니다.";
        }
    }
}
