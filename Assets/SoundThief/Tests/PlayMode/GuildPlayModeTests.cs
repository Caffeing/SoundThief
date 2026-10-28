using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SoundThief.Tests
{
    public sealed class GuildPlayModeTests
    {
        MissionDirector mission;
        ThiefPlayer player;
        ThiefInteractable Find(InteractionKind kind) => Object.FindObjectsByType<ThiefInteractable>(FindObjectsSortMode.None).First(i => i.kind == kind);
        ThiefInteractable Resonator(int index) => Object.FindObjectsByType<ThiefInteractable>(FindObjectsSortMode.None)
            .Where(i => i.kind == InteractionKind.Resonator).OrderBy(i => i.name).ElementAt(index);
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Time.timeScale = 1;
            yield return SceneManager.LoadSceneAsync("Guild"); yield return null;
            mission = Object.FindFirstObjectByType<MissionDirector>(); player = mission.player;
            player.enabled = false;
            foreach (var guard in mission.World.Guards) guard.enabled = false;
        }
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (Keyboard.current != null)
            {
                InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState()); InputSystem.Update();
            }
            Time.timeScale = 1; Cursor.lockState = CursorLockMode.None;
            yield return null;
        }
        void Aim(ThiefInteractable target, Vector3 position)
        {
            var controller = player.GetComponent<CharacterController>(); controller.enabled = false;
            player.transform.position = position; controller.enabled = true;
            player.view.transform.LookAt(target.GetComponentInChildren<Renderer>().bounds.center);
            Physics.SyncTransforms();
            typeof(ThiefPlayer).GetMethod("FindTarget", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(player, null);
            Assert.That(player.Target, Is.SameAs(target));
        }
        [UnityTest]
        public IEnumerator UnacceptedPlayerMovesAndIsStoppedByWalls()
        {
            Assert.That(mission.Accepted, Is.False); Assert.That(mission.CanAct, Is.True);
            player.enabled = true; float initial = player.transform.position.z;
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.W)); InputSystem.Update();
            yield return new WaitForSeconds(0.2f);
            Assert.That(player.transform.position.z, Is.GreaterThan(initial + 0.2f));
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState()); InputSystem.Update();
            var controller = player.GetComponent<CharacterController>(); controller.enabled = false;
            player.transform.position = new Vector3(10, 0.05f, 10); controller.enabled = true;
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.D)); InputSystem.Update();
            yield return new WaitForSeconds(0.5f);
            Assert.That(player.transform.position.x, Is.LessThan(10.7f), "East wall must stop the controller.");
            Assert.That(player.transform.position.y, Is.GreaterThan(-0.1f), "The floor must support the player.");
        }
        [UnityTest]
        public IEnumerator BoardAcceptsWithoutLeavingAndExitRejectsUnacceptedContract()
        {
            Find(InteractionKind.GuildExit).Interact(player);
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Guild"));
            Assert.That(mission.Accepted, Is.False);
            Aim(Find(InteractionKind.ContractBoard), new Vector3(2, 0, 3));
            player.Target.Interact(player); yield return null;
            Assert.That(mission.Accepted, Is.True);
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Guild"));
            Assert.That(mission.practice.Extracted || mission.practice.Injected || mission.practice.Thrown, Is.False);
        }
        [UnityTest]
        public IEnumerator ExitDirectlyEntersAcceptedStageWithoutPractice()
        {
            mission.Accept(); Find(InteractionKind.GuildExit).Interact(player);
            yield return null; yield return null;
            var stage = Object.FindFirstObjectByType<MissionDirector>();
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(MissionDirector.StageScene));
            Assert.That(stage.isGuild, Is.False); Assert.That(stage.Accepted && stage.CanAct, Is.True);
            Assert.That(stage.player.Held, Is.Null);
        }
        [UnityTest]
        public IEnumerator PracticeUsesSharedRulesAndResetPreservesContract()
        {
            mission.Accept(); var source = Resonator(0); var target = Resonator(1);
            Aim(source, new Vector3(-7, 0, 5.5f)); Assert.That(player.TryExtract(), Is.True);
            Assert.That(player.CurrentSpeed, Is.EqualTo(mission.rules.carrySpeed));
            Aim(target, new Vector3(-7, 0, 9.5f)); Assert.That(player.TryInject(), Is.True);
            Assert.That(target.emitter.Active, Is.True);
            Aim(target, new Vector3(-7, 0, 9.5f)); Assert.That(player.TryExtract(), Is.True);
            Assert.That(player.TryThrow(), Is.True);
            Assert.That(mission.practice.Extracted && mission.practice.Injected && mission.practice.Thrown, Is.True);
            Assert.That(mission.Extracted || mission.Injected || mission.DoorOpened || player.HasKey || player.HasTreasure, Is.False);
            mission.practice.ResetPractice(); yield return null;
            Assert.That(mission.Accepted, Is.True); Assert.That(mission.CanAct, Is.True);
            Assert.That(player.Held, Is.Null);
            Assert.That(Object.FindObjectsByType<SoundProjectile>(FindObjectsSortMode.None), Is.Empty);
            Assert.That(source.relation.IsMuted || target.relation.IsMuted || target.emitter.Active, Is.False);
            Assert.That(mission.practice.Extracted || mission.practice.Injected || mission.practice.Thrown, Is.False);
            Assert.That(mission.World.Guards[0].State, Is.EqualTo(GuardState.Sleeping));
            Assert.That(mission.World.transform.childCount, Is.EqualTo(0));
            Assert.That(mission.World.BlastCount, Is.EqualTo(0));
        }
        [UnityTest]
        public IEnumerator PracticingDoesNotDepartAndPracticeChargeDoesNotTransfer()
        {
            mission.Accept(); Aim(Resonator(0), new Vector3(-7, 0, 5.5f));
            Assert.That(player.TryExtract(), Is.True); var charge = player.Held;
            yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Guild"));
            Assert.That(mission.Depart(), Is.True);
            yield return null; yield return null;
            var stage = Object.FindFirstObjectByType<MissionDirector>();
            Assert.That(stage.Accepted, Is.True); Assert.That(stage.player.Held, Is.Null);
            Assert.That(stage.Extracted || stage.Injected || stage.DoorOpened, Is.False);
            Assert.That(charge.Finished, Is.True);
            Assert.That(Object.FindObjectsByType<SoundProjectile>(FindObjectsSortMode.None), Is.Empty);
        }
        [UnityTest]
        public IEnumerator GuildPauseFreezesPracticeAndContractPanelKeepsTimeRunning()
        {
            Aim(Resonator(0), new Vector3(-7, 0, 5.5f)); player.TryExtract();
            float remaining = player.Held.Remaining;
            mission.TogglePause(); yield return new WaitForSecondsRealtime(0.1f);
            Assert.That(player.Held.Remaining, Is.EqualTo(remaining).Within(0.02f));
            mission.TogglePause(); mission.ContractVisible = true;
            yield return new WaitForSeconds(0.1f);
            Assert.That(player.Held.Remaining, Is.LessThan(remaining - 0.05f));
        }
        [UnityTest]
        public IEnumerator StageContactRestartsAcceptedContractWithCleanState()
        {
            mission.Accept(); mission.Depart(); yield return null; yield return null;
            var stage = Object.FindFirstObjectByType<MissionDirector>();
            stage.player.HasKey = true; stage.player.HasTreasure = true;
            stage.Caught(); yield return null; yield return null;
            stage = Object.FindFirstObjectByType<MissionDirector>();
            Assert.That(stage.Accepted && stage.CanAct, Is.True);
            Assert.That(stage.player.HasKey || stage.player.HasTreasure || stage.Extracted || stage.Injected || stage.DoorOpened, Is.False);
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(MissionDirector.StageScene));
        }
    }
}
