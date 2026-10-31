using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SoundThief.Tests
{
    public class GuardTurningPlayModeTests
    {
        MissionDirector mission;
        GuardAgent guard;
        [UnitySetUp] public IEnumerator Setup()
        {
            Time.timeScale = 1; yield return SceneManager.LoadSceneAsync("ManorPrototype"); yield return null;
            mission = Object.FindFirstObjectByType<MissionDirector>(); mission.player.enabled = false; mission.Accept();
            var cc = mission.player.GetComponent<CharacterController>(); cc.enabled = false; mission.player.transform.position = new Vector3(-6, 0, 2); cc.enabled = true;
            guard = Object.FindObjectsByType<GuardAgent>(FindObjectsSortMode.None).First(g => g.name == "중앙 홀 경비");
            foreach (var g in Object.FindObjectsByType<GuardAgent>(FindObjectsSortMode.None)) g.enabled = false;
        }
        [UnityTest] public IEnumerator ReverseDirectionTurnsWithoutSnappingOrSlidingBackwards()
        {
            var agent = guard.GetComponent<NavMeshAgent>(); agent.Warp(new Vector3(0, 0, 12)); guard.transform.rotation = Quaternion.identity;
            guard.enabled = true; guard.Hear(new Vector3(0, 0, 17), 2, null); yield return new WaitForSeconds(.5f);
            guard.Hear(new Vector3(0, 0, 10), 3, null);
            float backwardTravel = 0; float end = Time.time + 3;
            while (Time.time < end)
            {
                var rotation = guard.transform.rotation; var position = guard.transform.position;
                bool facingAway = Vector3.Angle(guard.transform.forward, new Vector3(0, 0, 10) - position) > 100;
                yield return null;
                Assert.That(Quaternion.Angle(rotation, guard.transform.rotation), Is.LessThan(25), "One frame must not snap the body around.");
                if (facingAway) backwardTravel += Vector3.Distance(position, guard.transform.position);
            }
            Assert.That(backwardTravel, Is.LessThan(.65f), "Brake rather than slide backwards while turning.");
            Assert.That(guard.transform.forward.z, Is.LessThan(-.7f));
        }
        [UnityTest] public IEnumerator ReturningHomeRestoresFacingGradually()
        {
            var home = guard.transform.position; var facing = guard.transform.rotation;
            guard.transform.rotation = facing * Quaternion.Euler(0, 180, 0); guard.enabled = true;
            typeof(GuardAgent).GetMethod("Go", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(guard, new object[] { GuardState.Returning, home });
            var before = guard.transform.rotation; yield return null;
            Assert.That(Quaternion.Angle(before, guard.transform.rotation), Is.LessThan(25));
            float end = Time.time + 4;
            while (guard.State == GuardState.Returning && Time.time < end) yield return null;
            Assert.That(Quaternion.Angle(facing, guard.transform.rotation), Is.LessThan(4));
            Assert.That(guard.State, Is.EqualTo(GuardState.Sleeping));
        }
    }
}
