using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SoundThief.Editor
{
    // Check the temporary build scene, including inherited prefab references.
    public sealed class GameplayBuildValidation : IProcessSceneWithReport
    {
        public int callbackOrder => 0;
        public void OnProcessScene(Scene scene, BuildReport report)
        {
            if (report == null) return;
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var player in root.GetComponentsInChildren<ThiefPlayer>(true))
                {
                    if (!player.view || !player.Mission)
                        throw new BuildFailedException(scene.path + ": 플레이어의 카메라 또는 의뢰 연결이 없습니다.");
                    RequireMaterial("SoundConnection", scene.path);
                }
                foreach (var steps in root.GetComponentsInChildren<PlayerFootsteps>(true))
                    if (steps.enabled && !steps.clip)
                        throw new BuildFailedException(scene.path + ": " + steps.name + " 플레이어 발소리 파일이 없습니다.");
            }
        }
        static void RequireMaterial(string name, string scene)
        {
            var material = Resources.Load<Material>(name);
            if (!material || !material.shader)
                throw new BuildFailedException(scene + ": Resources/" + name + " 재질 또는 셰이더가 없습니다.");
        }
    }
}
