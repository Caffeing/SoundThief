using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using System.Reflection;

namespace SoundThief.Tests
{
    public class StealthFeedbackPlayModeTests
    {
        MissionDirector mission;
        ThiefPlayer player;
        GuardAgent guard;
        Keyboard keyboard;
        InputSettings originalInputSettings, testInputSettings;
        GameObject cover;
        [UnitySetUp] public IEnumerator Setup()
        {
            Time.timeScale = 1;
            yield return SceneManager.LoadSceneAsync("ManorPrototype"); yield return null;
            mission = Object.FindFirstObjectByType<MissionDirector>(); player = mission.player;
            player.enabled = false; mission.Accept();
            guard = Object.FindObjectsByType<GuardAgent>(FindObjectsSortMode.None).First(g => g.name == "중앙 홀 경비");
            foreach (var g in Object.FindObjectsByType<GuardAgent>(FindObjectsSortMode.None)) g.enabled = false;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            if (testInputSettings) { InputSystem.settings = originalInputSettings; Object.Destroy(testInputSettings); }
            if (cover) Object.Destroy(cover);
            Time.timeScale = 1; yield return null;
        }
        void Place(Vector3 p, float eye)
        {
            var cc = player.GetComponent<CharacterController>(); cc.enabled = false; player.transform.position = p; cc.enabled = true;
            player.view.transform.localPosition = Vector3.up * eye;
        }
        void SetUpKeyboard()
        {
            originalInputSettings = InputSystem.settings; testInputSettings = Object.Instantiate(originalInputSettings);
            testInputSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            testInputSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings = testInputSettings; keyboard = InputSystem.AddDevice<Keyboard>(); keyboard.MakeCurrent();
        }
        [UnityTest] public IEnumerator FurnitureAndCrouchingHideThePlayer()
        {
            var desk = GameObject.Find("서재 집무 책상"); Assert.That(desk.layer, Is.EqualTo(8)); Assert.That(desk.GetComponent<BoxCollider>(), Is.Not.Null);
            guard.GetComponent<NavMeshAgent>().Warp(new Vector3(0, 0, 12)); guard.transform.rotation = Quaternion.identity;
            Place(new Vector3(0, 0, 17), 1.65f);
            cover = GameObject.CreatePrimitive(PrimitiveType.Cube); cover.layer = 8; cover.transform.position = new Vector3(0, .5f, 16); cover.transform.localScale = new Vector3(3, 1, 1); Physics.SyncTransforms();
            Assert.That(guard.SeesPlayer(), Is.True, "Standing above low furniture remains visible.");
            Place(new Vector3(0, 0, 17), .72f); Assert.That(guard.SeesPlayer(), Is.False, "Crouching behind the furniture blocks sight.");
            cover.transform.position = new Vector3(0, 1.2f, 15); cover.transform.localScale = new Vector3(3, 2.4f, .6f); Physics.SyncTransforms();
            Place(new Vector3(0, 0, 17), 1.65f); Assert.That(guard.SeesPlayer(), Is.False, "Tall bookshelves hide standing players too."); yield return null;
        }
        [UnityTest] public IEnumerator UnmutedEntranceWakesGuardImmediately()
        {
            player.HasKey = true; var door = GameObject.Find("현관문").GetComponent<ThiefInteractable>();
            Assert.That(guard.State, Is.EqualTo(GuardState.Sleeping)); door.Interact(player);
            Assert.That(guard.State, Is.EqualTo(GuardState.InvestigatingSound)); yield return null;
        }
        [UnityTest] public IEnumerator StolenEntranceSoundKeepsGuardAsleep()
        {
            player.HasKey = true; var door = GameObject.Find("현관문").GetComponent<ThiefInteractable>();
            var charge = door.relation.Extract(mission.rules); door.Interact(player);
            Assert.That(guard.State, Is.EqualTo(GuardState.Sleeping)); charge.Finish(); yield return null;
        }
        [UnityTest] public IEnumerator ChaseIsFasterThanPatrolAndSightHasSoftEdges()
        {
            guard.GetComponent<NavMeshAgent>().Warp(new Vector3(0, 0, 12)); guard.transform.rotation = Quaternion.identity;
            Place(new Vector3(0, 0, 19), 1.65f); guard.Hear(player.transform.position, 3, null); guard.enabled = true;
            yield return null; yield return null; yield return null;
            Assert.That(guard.State, Is.EqualTo(GuardState.Chasing));
            float facingDeadline = Time.time + 1.2f;
            while (guard.IsTurning && Time.time < facingDeadline) yield return null;
            yield return null;
            Assert.That(mission.rules.guardChaseSpeed, Is.GreaterThan(mission.rules.guardSpeed));
            Assert.That(guard.GetComponent<NavMeshAgent>().speed, Is.EqualTo(mission.rules.guardChaseSpeed).Within(.1f));
            var cone = guard.GetComponentsInChildren<MeshFilter>().First(f => f.sharedMesh && f.sharedMesh.name == "Occluded guard vision").sharedMesh;
            Assert.That(cone.colors.Count(c => c.a < .01f), Is.GreaterThan(40), "vertices=" + cone.vertexCount + ", colors=" + cone.colors.Length + ", alpha=" + string.Join(",", cone.colors.Take(8).Select(c => c.a.ToString())));
        }
        [UnityTest] public IEnumerator WalkingAndCrouchingPlayDifferentStepVolumes()
        {
            SetUpKeyboard();
            Place(new Vector3(0, 0, 8), 1.65f); player.enabled = true;
            var controller = player.GetComponent<CharacterController>(); bool walking = false, crouching = false;
            player.GetComponent<PlayerFootsteps>().StepPlayed += volume => { walking |= Mathf.Approximately(volume, .42f); crouching |= Mathf.Approximately(volume, .12f); };
            for (int i = 0; i < 20; i++) { controller.Move(new Vector3(.16f, -.05f, 0)); yield return null; }
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.LeftCtrl)); InputSystem.Update();
            for (int i = 0; i < 16; i++) { controller.Move(new Vector3(.1f, -.05f, 0)); yield return null; }
            Assert.That(walking, Is.True, "Walking must play a normal-volume step."); Assert.That(crouching, Is.True, "Crouching must play a quieter step.");
        }
        [UnityTest] public IEnumerator StealingSealSoundKeepsStandingAndAllowsPickupOrCrouch()
        {
            SetUpKeyboard();
            var seal = Object.FindObjectsByType<ThiefInteractable>(FindObjectsSortMode.None).First(i => i.kind == InteractionKind.Treasure);
            Place(new Vector3(19, 0, 8.3f), 1.65f); player.enabled = true;
            float standingHeight = player.GetComponent<CharacterController>().height;
            void Aim() { player.view.transform.LookAt(seal.GetComponentInChildren<Renderer>().bounds.center); Physics.SyncTransforms(); typeof(ThiefPlayer).GetMethod("FindTarget", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(player, null); }
            Aim(); Assert.That(player.Target, Is.EqualTo(seal)); Assert.That(player.TryExtract(), Is.True);
            yield return new WaitForSeconds(.4f);
            Assert.That(player.IsCrouching, Is.False); Assert.That(player.GetComponent<CharacterController>().height, Is.EqualTo(standingHeight).Within(.02f));
            Assert.That(player.CurrentSpeed, Is.EqualTo(mission.rules.carrySpeed));
            Aim(); Assert.That(player.Target, Is.EqualTo(seal), "The muted seal remains targetable from standing height.");
            seal.Interact(player); Assert.That(player.HasTreasure, Is.True);
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.LeftCtrl)); InputSystem.Update(); yield return new WaitForSeconds(.35f);
            Assert.That(player.IsCrouching, Is.True); Assert.That(player.GetComponent<CharacterController>().height, Is.LessThan(1.1f));
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update(); yield return new WaitForSeconds(.35f);
            Assert.That(player.IsCrouching, Is.False); Assert.That(player.Slow, Is.True, "Carrying remains slow when standing up.");
        }
        [UnityTest] public IEnumerator HeldSoundShowsStrainAndBurstsOnlyOnExpiry()
        {
            var presentation = player.GetComponentInChildren<HeldSoundPresentation>(true);
            Assert.That(presentation, Is.Not.Null);
            var relationObject = new GameObject("test sound relation");
            var relation = relationObject.AddComponent<SoundRelation>();
            relation.Extract(mission.rules);
            var heldField = typeof(ThiefPlayer).GetField("<Held>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
            heldField.SetValue(player, new SoundCharge(relation, 1, Time.time + .7f));
            yield return null;
            Assert.That(presentation.hand.gameObject.activeSelf, Is.True);
            Assert.That(presentation.hand.GetComponentInChildren<ParticleSystem>(), Is.Not.Null);
            yield return new WaitForSeconds(.35f);
            Assert.That(presentation.Strain, Is.GreaterThan(.25f));
            int previousBlasts = mission.World.BlastCount;
            player.ExpireHeld();
            Assert.That(mission.World.BlastCount, Is.EqualTo(previousBlasts + 1));
            Assert.That(relation.IsMuted, Is.False);
            yield return null;
            Assert.That(presentation.hand.gameObject.activeSelf, Is.False);
            Assert.That(GameObject.Find("손에서 터진 소리"), Is.Not.Null);
            player.ExpireHeld();
            Assert.That(mission.World.BlastCount, Is.EqualTo(previousBlasts + 1));
            Object.Destroy(relationObject);
        }
        [UnityTest] public IEnumerator NearAimHighlightsKeyWithoutSelectingThroughWalls()
        {
            Place(new Vector3(200, 0, 200), 1.65f);
            player.view.transform.rotation = Quaternion.identity;
            var key = GameObject.CreatePrimitive(PrimitiveType.Cube);
            key.transform.position = player.view.transform.position + new Vector3(.24f, 0, 2);
            key.transform.localScale = Vector3.one * .1f;
            var item = key.AddComponent<ThiefInteractable>(); item.kind = InteractionKind.Key;
            Physics.SyncTransforms();
            var find = typeof(ThiefPlayer).GetMethod("FindTarget", BindingFlags.NonPublic | BindingFlags.Instance);
            find.Invoke(player, null);
            Assert.That(player.HighlightTarget, Is.EqualTo(item));
            Assert.That(player.Target, Is.Null, "Highlighting near aim must not silently change interaction targeting.");
            cover = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cover.transform.position = player.view.transform.position + Vector3.forward;
            cover.transform.localScale = new Vector3(2, 2, .1f);
            Physics.SyncTransforms(); find.Invoke(player, null);
            Assert.That(player.HighlightTarget, Is.Null, "Walls must block the nearby highlight.");
            Object.Destroy(key); yield return null;
        }
    }
}
