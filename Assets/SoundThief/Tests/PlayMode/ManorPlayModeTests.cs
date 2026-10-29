using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SoundThief.Tests
{
    public sealed class ManorPlayModeTests
    {
        MissionDirector mission;
        ThiefPlayer player;
        GuardAgent guard;
        SoundRules rules;
        PlayModeInputScope input;
        ThiefInteractable Door => Find(InteractionKind.Door);
        ThiefInteractable Resonator(int index) => Object.FindObjectsByType<ThiefInteractable>(FindObjectsSortMode.None)
            .Where(i => i.kind == InteractionKind.Resonator).OrderBy(i => i.name).ElementAt(index);
        ThiefInteractable Find(InteractionKind kind) => Object.FindObjectsByType<ThiefInteractable>(FindObjectsSortMode.None).First(i => i.kind == kind);

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            input = new PlayModeInputScope();
            Time.timeScale = 1;
            yield return SceneManager.LoadSceneAsync("ManorPrototype");
            yield return null;
            mission = Object.FindFirstObjectByType<MissionDirector>(); player = mission.player;
            rules = Object.Instantiate(mission.rules); mission.rules = mission.World.rules = rules;
            guard = Object.FindFirstObjectByType<GuardAgent>(); guard.enabled = false;
            player.enabled = false; mission.Accept();
        }
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1; Cursor.lockState = CursorLockMode.None;
            if (rules) Object.Destroy(rules);
            input.Dispose();
            yield return null;
        }
        void Aim(ThiefInteractable target, Vector3 position)
        {
            var controller = player.GetComponent<CharacterController>(); controller.enabled = false;
            player.transform.position = position; controller.enabled = true;
            player.view.transform.localPosition = Vector3.up * 1.65f;
            player.view.transform.LookAt(target.GetComponentInChildren<Renderer>().bounds.center);
            Physics.SyncTransforms();
            typeof(ThiefPlayer).GetMethod("FindTarget", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(player, null);
            Assert.That(player.Target, Is.SameAs(target), "Target must be selected through the real interaction ray.");
        }
        void ExtractDoor()
        {
            player.HasKey = true; Aim(Door, new Vector3(0, 0, 4));
            Assert.That(player.TryExtract(), Is.True);
        }
        [UnityTest]
        public IEnumerator ShortInputEventsStillInteractAndMovementAdvances()
        {
            player.enabled = true;
            Aim(Find(InteractionKind.Key), new Vector3(-1.8f, 0, 2));
            typeof(ThiefPlayer).GetField("pitch", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(player, player.view.transform.localEulerAngles.x);
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.F));
            UnityEngine.InputSystem.InputSystem.Update();
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState());
            UnityEngine.InputSystem.InputSystem.Update();
            yield return null;
            Assert.That(player.HasKey, Is.True, "A complete key press between game frames must not be lost.");
            float start = player.transform.position.z;
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.W));
            UnityEngine.InputSystem.InputSystem.Update();
            yield return new WaitForSeconds(0.15f);
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState());
            UnityEngine.InputSystem.InputSystem.Update();
            Assert.That(player.transform.position.z, Is.GreaterThan(start + 0.1f));
        }
        [UnityTest]
        public IEnumerator SubMillimetreFrameStepsStillMoveThePlayer()
        {
            float previousCaptureDelta = Time.captureDeltaTime;
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            try
            {
                Time.captureDeltaTime = 0.0001f;
                player.enabled = true;
                yield return null;
                yield return null;
                float start = player.transform.position.z;
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,
                    new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.W));
                UnityEngine.InputSystem.InputSystem.Update();
                for (int frame = 0; frame < 40; frame++) yield return null;
                Assert.That(player.transform.position.z - start, Is.GreaterThan(0.01f),
                    "Valid movement must survive frame steps smaller than the controller's legacy 1 mm threshold.");
            }
            finally
            {
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,
                    new UnityEngine.InputSystem.LowLevel.KeyboardState());
                UnityEngine.InputSystem.InputSystem.Update();
                Time.captureDeltaTime = previousCaptureDelta;
            }
        }
        [UnityTest]
        public IEnumerator ExtractionMutesWholeRelationAndBlocksSecondCharge()
        {
            ExtractDoor(); var charge = player.Held;
            Assert.That(Door.relation.IsMuted, Is.True);
            Assert.That(player.CurrentSpeed, Is.EqualTo(rules.carrySpeed));
            Door.Interact(player); Assert.That(Door.IsOpen, Is.True);
            Assert.That(player.Held, Is.SameAs(charge), "Opening the door must not reset the held sound.");
            Aim(Resonator(0), new Vector3(-2, 0, 7.5f));
            Assert.That(player.TryExtract(), Is.False);
            Assert.That(charge.Grade, Is.EqualTo(2));
            yield return null;
        }
        [UnityTest]
        public IEnumerator InjectionAllowsIndependentOutputsAndRestoresOnDecay()
        {
            rules.injectionHold = 0.08f; rules.decayStep = 0.08f;
            ExtractDoor(); var a = Resonator(0); var b = Resonator(1);
            Aim(a, new Vector3(-2, 0, 7.5f)); Assert.That(player.TryInject(), Is.True);
            Assert.That(player.Held, Is.Null); Assert.That(Door.relation.IsMuted, Is.True);
            Aim(b, new Vector3(-5.8f, 0, 16)); Assert.That(player.TryExtract(), Is.True);
            Aim(a, new Vector3(-2, 0, 7.5f)); Assert.That(player.TryInject(), Is.False);
            Assert.That(player.Held, Is.Not.Null, "Rejected injection must retain the charge.");
            Aim(b, new Vector3(-5.8f, 0, 16)); Assert.That(player.TryInject(), Is.True);
            Assert.That(a.emitter.Active && b.emitter.Active, Is.True);
            yield return new WaitForSeconds(0.5f);
            Assert.That(a.emitter.Active || b.emitter.Active, Is.False);
            Assert.That(Door.relation.IsMuted || b.relation.IsMuted, Is.False);
        }
        [UnityTest]
        public IEnumerator ThrowKeepsDeadlineAndExplodesOnceInAir()
        {
            // Retained implementation regression; production rules keep this feature off.
            rules.throwingEnabled = true;
            rules.holdSeconds[1] = 0.15f; ExtractDoor();
            var charge = player.Held;
            var controller = player.GetComponent<CharacterController>(); controller.enabled = false;
            player.transform.position = new Vector3(-3, 3, 10); controller.enabled = true;
            player.view.transform.rotation = Quaternion.identity;
            Assert.That(player.TryThrow(), Is.True);
            Assert.That(player.Held, Is.Null); Assert.That(player.CurrentSpeed, Is.EqualTo(rules.walkSpeed));
            Assert.That(Door.relation.IsMuted, Is.True);
            yield return new WaitForSeconds(0.25f);
            Assert.That(charge.Finished, Is.True); Assert.That(Door.relation.IsMuted, Is.False);
            Assert.That(mission.World.BlastCount, Is.EqualTo(1));
            Assert.That(mission.World.LastBlastPosition.y, Is.GreaterThan(2));
            yield return new WaitForSeconds(0.1f);
            Assert.That(mission.World.BlastCount, Is.EqualTo(1));
        }
        [UnityTest]
        public IEnumerator GroundCollisionDetonatesBeforeDeadline()
        {
            ExtractDoor(); var charge = player.Held;
            var obj = GameObject.CreatePrimitive(PrimitiveType.Sphere); obj.transform.position = new Vector3(-3, 0.3f, 12);
            obj.transform.localScale = Vector3.one * 0.2f; obj.AddComponent<Rigidbody>();
            obj.AddComponent<SoundProjectile>().Init(charge, mission.World);
            yield return new WaitForSeconds(0.5f);
            Assert.That(charge.Remaining, Is.GreaterThan(10));
            Assert.That(charge.Finished, Is.True); Assert.That(mission.World.BlastCount, Is.EqualTo(1));
        }
        [UnityTest]
        public IEnumerator DeferredThrowKeepsHeldSoundAndIsNotAdvertised()
        {
            Assert.That(rules.throwingEnabled, Is.False);
            ExtractDoor(); var held = player.Held; float deadline = held.Deadline;
            Assert.That(player.TryThrow(), Is.False);
            Assert.That(player.Held, Is.SameAs(held)); Assert.That(held.Deadline, Is.EqualTo(deadline));
            player.enabled = true;
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(UnityEngine.InputSystem.Mouse.current,
                new UnityEngine.InputSystem.LowLevel.MouseState().WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Left));
            UnityEngine.InputSystem.InputSystem.Update();
            yield return null;
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(UnityEngine.InputSystem.Mouse.current,new UnityEngine.InputSystem.LowLevel.MouseState());
            UnityEngine.InputSystem.InputSystem.Update();
            Assert.That(player.Held, Is.SameAs(held)); Assert.That(Door.relation.IsMuted, Is.True);
            Assert.That(Object.FindObjectsByType<SoundProjectile>(FindObjectsSortMode.None), Is.Empty);
            Assert.That(Object.FindObjectsByType<TMPro.TextMeshProUGUI>(FindObjectsSortMode.None).Any(t=>t.text.Contains("좌클릭 투척")), Is.False);
        }
        [UnityTest]
        public IEnumerator HeldTimeoutStunsAndRestoresWithoutFailing()
        {
            rules.holdSeconds[1] = 0.05f; ExtractDoor();
            player.enabled = true;
            yield return new WaitForSeconds(0.12f);
            player.enabled = false;
            Assert.That(player.IsStunned, Is.True); Assert.That(player.Held, Is.Null);
            Assert.That(Door.relation.IsMuted, Is.False); Assert.That(mission.Accepted && !mission.Completed, Is.True);
            Aim(Resonator(0), new Vector3(-2, 0, 7.5f));
            Assert.That(player.TryExtract(), Is.False);
            Find(InteractionKind.Treasure).Interact(player);
            Assert.That(player.HasTreasure, Is.False, "Stun blocks ordinary interactions too.");
        }
        [UnityTest]
        public IEnumerator BlastIsBlockedByWallButSoundStillWakesGuard()
        {
            rules.wakeThreshold = 0.1f;
            guard.transform.position = new Vector3(4, 0, 12);
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube); wall.layer = 8;
            wall.transform.position = new Vector3(3, 1.5f, 12); wall.transform.localScale = new Vector3(0.3f, 3, 4);
            Physics.SyncTransforms();
            mission.World.Blast(new Vector3(2, 1, 12), 3, false);
            Assert.That(guard.State, Is.EqualTo(GuardState.InvestigatingSound));
            Object.Destroy(wall); yield return null; Physics.SyncTransforms();
            mission.World.Blast(new Vector3(2, 1, 12), 3, false);
            Assert.That(guard.State, Is.EqualTo(GuardState.Stunned));
        }
        [UnityTest]
        public IEnumerator PanicIgnoresSightSoundAndRestunUntilRecovery()
        {
            rules.guardStun = 0.05f; rules.panicSeconds = 0.2f; guard.enabled = true;
            Assert.That(guard.Stun(), Is.True);
            yield return new WaitForSeconds(0.1f);
            Assert.That(guard.State, Is.EqualTo(GuardState.Panic));
            guard.Hear(player.transform.position, 3, null);
            Assert.That(guard.Stun(), Is.False); Assert.That(guard.State, Is.EqualTo(GuardState.Panic));
            yield return new WaitForSeconds(0.25f);
            Assert.That(guard.Stun(), Is.True);
        }
        [UnityTest]
        public IEnumerator OnlyHigherGradeRetargetsAndSoundEndStopsInvestigation()
        {
            rules.wakeThreshold = 0.1f;
            var original = guard.transform.position + Vector3.left;
            guard.Hear(original, 2, null); guard.Hear(original + Vector3.forward, 1, null);
            Assert.That(guard.Goal, Is.EqualTo(original));
            guard.Hear(original + Vector3.back, 2, null); Assert.That(guard.Goal, Is.EqualTo(original));
            var higher = original + Vector3.left; guard.Hear(higher, 3, null); Assert.That(guard.Goal, Is.EqualTo(higher));
            guard.enabled = true;
            yield return new WaitForSeconds(1.1f);
            Assert.That(guard.State, Is.EqualTo(GuardState.Alert));
        }
        [UnityTest]
        public IEnumerator SightTracksThenUsesLastVisiblePosition()
        {
            rules.wakeThreshold = 0.1f; guard.enabled = true;
            var agent = guard.GetComponent<UnityEngine.AI.NavMeshAgent>(); agent.Warp(new Vector3(0, 0, 12)); guard.transform.rotation = Quaternion.identity;
            var controller = player.GetComponent<CharacterController>(); controller.enabled = false; player.transform.position = new Vector3(0, 0, 15); controller.enabled = true;
            guard.Hear(guard.transform.position, 1, null);
            yield return null; Assert.That(guard.State, Is.EqualTo(GuardState.Chasing));
            var seen = guard.Goal;
            controller.enabled = false; player.transform.position = new Vector3(0, 0, 2); controller.enabled = true;
            yield return null;
            Assert.That(guard.State, Is.EqualTo(GuardState.LastSeen)); Assert.That(guard.Goal, Is.EqualTo(seen));
            yield return new WaitForSeconds(0.1f); Assert.That(guard.Goal, Is.EqualTo(seen));
        }
        [UnityTest]
        public IEnumerator PauseFreezesDeadlineAndContractDoesNot()
        {
            ExtractDoor(); float before = player.Held.Remaining;
            mission.TogglePause(); yield return new WaitForSecondsRealtime(0.1f);
            Assert.That(player.Held.Remaining, Is.EqualTo(before).Within(0.03f));
            mission.TogglePause(); mission.ContractVisible = true;
            yield return new WaitForSeconds(0.1f);
            Assert.That(player.Held.Remaining, Is.LessThan(before - 0.05f));
        }
        [UnityTest]
        public IEnumerator ObjectiveRequiresTheftAndExitThenRestartClearsEverything()
        {
            mission.Complete(); Assert.That(mission.Completed, Is.False);
            ExtractDoor(); Door.Interact(player);
            Find(InteractionKind.Treasure).Interact(player); Assert.That(mission.Completed, Is.False);
            Find(InteractionKind.Exit).Interact(player); Assert.That(mission.Completed, Is.True);
            mission.Restart(); yield return null; yield return null;
            mission = Object.FindFirstObjectByType<MissionDirector>(); player = mission.player;
            Assert.That(mission.Accepted, Is.True, "Stage restarts retain the accepted contract.");
            Assert.That(player.HasKey || player.HasTreasure || Door.IsOpen || Door.relation.IsMuted, Is.False);
            Assert.That(Object.FindObjectsByType<SoundProjectile>(FindObjectsSortMode.None), Is.Empty);
        }
        [UnityTest]
        public IEnumerator GuardContactImmediatelyRestartsWholeContract()
        {
            ExtractDoor(); guard.enabled = true;
            guard.GetComponent<UnityEngine.AI.NavMeshAgent>().Warp(player.transform.position);
            yield return null; yield return null;
            mission = Object.FindFirstObjectByType<MissionDirector>();
            Assert.That(mission.Accepted, Is.True);
            Assert.That(mission.player.HasKey || Door.relation.IsMuted, Is.False);
        }
    }
}
