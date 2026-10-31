using System.Linq;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SoundThief.JinHo.Editor
{
    public sealed class Stage2BuildValidation : IProcessSceneWithReport
    {
        public int callbackOrder => 1;
        public void OnProcessScene(Scene scene, BuildReport report)
        {
            if (report == null || !scene.GetRootGameObjects().Any(r => r.GetComponentInChildren<Stage2MirrorBreak>(true))) return;
            foreach (var name in new[] { "MirrorShardGlass", "MirrorShardBacking" })
            {
                var material = Resources.Load<Material>(name);
                if (!material || !material.shader)
                    throw new BuildFailedException(scene.path + ": Resources/" + name + " 거울 파편 재질 또는 셰이더가 없습니다.");
            }
        }
    }
}
