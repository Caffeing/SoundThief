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
            Object.Destroy(temporary);
        }
    }
}
