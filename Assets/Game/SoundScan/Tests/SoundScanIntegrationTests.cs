using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SoundThief.SoundScan.Tests
{
    public sealed class SoundScanIntegrationTests
    {
        [UnityTest]
        public IEnumerator ManorScansAudibleEventsButNotMutedOrDistantSounds()
        {
            Time.timeScale = 1;
            yield return SceneManager.LoadSceneAsync("ManorPrototype");
            yield return null;
            var mission = Object.FindFirstObjectByType<MissionDirector>();
            mission.Accept();
            foreach (var guard in Object.FindObjectsByType<GuardAgent>(FindObjectsSortMode.None)) guard.enabled = false;
            var scanner = mission.player.view.GetComponent<SoundScanController>();
            var bridge = mission.player.view.GetComponent<SoundScanSoundBridge>();
            Assert.That(scanner, Is.Not.Null);
            Assert.That(bridge.world, Is.EqualTo(mission.World));
            int count = scanner.ActiveCount;
            var temporary = new GameObject("test muted relation");
            temporary.transform.position = mission.player.transform.position + Vector3.up;
            var relation = temporary.AddComponent<SoundRelation>();
            var charge = relation.Extract(mission.rules);
            relation.MakeNoise(mission.World);
            Assert.That(scanner.ActiveCount, Is.EqualTo(count), "A stolen/muted sound must not emit a scan.");
            charge.Finish();
            relation.MakeNoise(mission.World);
            Assert.That(scanner.ActiveCount, Is.EqualTo(count + 1), "An audible relation sound must emit a scan.");
            mission.World.ReportAudibleSound(mission.player.transform.position + Vector3.right * 1000, 3, 1, temporary);
            Assert.That(scanner.ActiveCount, Is.EqualTo(count + 1), "A sound outside hearing range must not reveal its location.");
            mission.World.ReportAudibleSound(mission.player.transform.position, 1, .12f, temporary);
            Assert.That(scanner.ActiveCount, Is.EqualTo(count + 2), "Quiet footsteps still produce a small scan.");
            mission.World.ReportAudibleSound(mission.player.transform.position, 1, .12f, temporary);
            Assert.That(scanner.ActiveCount, Is.EqualTo(count + 2), "Repeated source events are throttled.");
            var footsteps = mission.player.GetComponent<PlayerFootsteps>();
            Assert.That(footsteps, Is.Not.Null);
            mission.World.ReportAudibleSound(mission.player.transform.position, 1, .12f, footsteps);
            mission.World.ReportAudibleSound(mission.player.transform.position, 1, .12f, footsteps);
            Assert.That(scanner.ActiveCount, Is.EqualTo(count + 4), "Each footstep creates its own scan without the emitter cooldown.");
            var scenePolicy = mission.World.PropagationPolicy as MonoBehaviour;
            if (scenePolicy) mission.World.UnregisterPropagationPolicy(scenePolicy);
            var policy = temporary.AddComponent<ScanTestPropagationPolicy>();
            Assert.That(mission.World.RegisterPropagationPolicy(policy), Is.True);
            scanner.maximumConcurrent = SoundScanController.Capacity;
            int beforeOwnStep = scanner.ActiveCount;
            mission.World.ReportAudibleSound(mission.player.transform.position, 1, .1f, footsteps);
            Assert.That(scanner.ActiveCount, Is.EqualTo(beforeOwnStep + 1), "A room policy must not hide the player's locally audible footstep.");
            mission.World.ReportAudibleSound(temporary.transform.position, 1, .1f, temporary);
            Assert.That(scanner.ActiveCount, Is.EqualTo(beforeOwnStep + 1), "Other sounds still obey the scene hearing policy.");
            Assert.That(mission.World.ScanTransmission(Vector3.zero, Vector3.right, 1), Is.Zero);
            policy.allow = true;
            Assert.That(mission.World.ScanTransmission(Vector3.zero, Vector3.right, 1), Is.EqualTo(1));
            mission.World.UnregisterPropagationPolicy(policy);
            if (scenePolicy) mission.World.RegisterPropagationPolicy(scenePolicy);
            Object.Destroy(temporary);
        }
    }
    public sealed class ScanTestPropagationPolicy : MonoBehaviour, IPositionalSoundPropagationPolicy
    {
        public bool allow;
        public bool AllowsHearing(Vector3 position, int grade, SoundEmitter source, GuardAgent receiver) => allow;
        public float AudioGain(Vector3 position, int grade, SoundEmitter source, Vector3 listener) => 0;
        public bool CanHear(Vector3 origin, Vector3 listener, int grade, SoundRules rules, out float proximity)
        { proximity = allow ? 1 : 0; return allow; }
    }
}

