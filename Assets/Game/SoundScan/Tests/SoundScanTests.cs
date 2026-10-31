using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace SoundThief.SoundScan.Tests
{
    public sealed class SoundScanTests
    {
        [TearDown] public void ResetTime() { Time.timeScale = 1; }
        [UnityTest]
        public IEnumerator SoundStrengthMultipleScansAndFadeUseGameTime()
        {
            var go = new GameObject("scan test", typeof(Camera), typeof(SoundScanController));
            var scanner = go.GetComponent<SoundScanController>();
            scanner.maximumRadius = 10; scanner.speed = 100; scanner.trailSeconds = .1f; scanner.maximumConcurrent = 2;
            scanner.TriggerSoundScan(Vector3.zero, 0);
            Assert.That(scanner.ActiveCount, Is.Zero);
            scanner.TriggerSoundScan(new Vector3(1, 2, 3), .25f);
            var waves = (System.Array)typeof(SoundScanController).GetField("waves", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(scanner);
            var radius = waves.GetValue(0).GetType().GetField("radius").GetValue(waves.GetValue(0));
            Assert.That((float)radius, Is.EqualTo(2.5f).Within(.001f));
            scanner.TriggerSoundScan(Vector3.right, 1);
            scanner.TriggerSoundScan(Vector3.up, 2);
            Assert.That(scanner.ActiveCount, Is.EqualTo(2), "Oldest scan is replaced at capacity.");
            Time.timeScale = 0;
            yield return new WaitForSecondsRealtime(.25f);
            Assert.That(scanner.ActiveCount, Is.EqualTo(2), "Pause must freeze scan lifetime.");
            Time.timeScale = 1;
            yield return new WaitForSeconds(.25f);
            Assert.That(scanner.ActiveCount, Is.Zero);
            Object.Destroy(go);
        }
    }
}
