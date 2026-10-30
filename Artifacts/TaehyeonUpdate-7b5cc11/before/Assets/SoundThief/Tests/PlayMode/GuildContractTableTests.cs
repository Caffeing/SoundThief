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
    public sealed class GuildContractTableTests
    {
        MissionDirector mission;
        ThiefPlayer player;
        GuildContractTable table;
        PlayModeInputScope input;
        void Press(Key key)
        {
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(key)); InputSystem.Update();
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState()); InputSystem.Update();
        }
        void AimAt(ThiefInteractable target, Vector3 position)
        {
            var controller = player.GetComponent<CharacterController>(); controller.enabled = false;
            player.transform.position = position; controller.enabled = true;
            player.view.transform.LookAt(target.GetComponent<Renderer>().bounds.center);
            typeof(ThiefPlayer).GetField("pitch", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(player, Mathf.DeltaAngle(0, player.view.transform.localEulerAngles.x));
            Physics.SyncTransforms();
        }
        [UnitySetUp] public IEnumerator SetUp()
        {
            input = new PlayModeInputScope(); Time.timeScale = 1;
            typeof(MissionDirector).GetMethod("ResetSession", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
            yield return SceneManager.LoadSceneAsync("Guild"); yield return null;
            mission = Object.FindFirstObjectByType<MissionDirector>(); player = mission.player; table = mission.contractTable;
            foreach (var guard in mission.World.Guards) guard.enabled = false;
        }
        [UnityTearDown] public IEnumerator TearDown()
        {
            Time.timeScale = 1; Cursor.lockState = CursorLockMode.None; input.Dispose(); yield return null;
        }
        [UnityTest] public IEnumerator RealTableInputLocksMovementAndEscapeOnlyClosesTable()
        {
            AimAt(table.GetComponent<ThiefInteractable>(), new Vector3(-2, 0.05f, -1));
            yield return null;
            Assert.That(player.Target, Is.SameAs(table.GetComponent<ThiefInteractable>()));
            Press(Key.F); yield return null;
            Assert.That(table.IsOpen, Is.True); Assert.That(mission.Accepted, Is.False);
            var position = player.transform.position; var facing = player.transform.rotation; var view = player.view.transform.localRotation;
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.W, Key.E, Key.Q, Key.R, Key.Tab)); InputSystem.Update();
            yield return new WaitForSeconds(0.12f);
            Assert.That(Vector3.Distance(position, player.transform.position), Is.LessThan(0.001f));
            Assert.That(Quaternion.Angle(facing, player.transform.rotation), Is.LessThan(0.01f));
            Assert.That(Quaternion.Angle(view, player.view.transform.localRotation), Is.LessThan(0.01f));
            Assert.That(mission.Paused, Is.False); Assert.That(Time.timeScale, Is.EqualTo(1));
            Assert.That(player.Held, Is.Null);
            Press(Key.Escape); yield return null;
            Assert.That(table.IsOpen || mission.Paused, Is.False);
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(Key.W)); InputSystem.Update();
            yield return new WaitForSeconds(0.1f);
            Assert.That(Vector3.Distance(position, player.transform.position), Is.GreaterThan(0.05f));
        }
        [UnityTest] public IEnumerator ArrowInputTurnsOneDocumentAndCannotAcceptDuringTurnOrPlaceholder()
        {
            Assert.That(table.Open(), Is.True);
            Press(Key.RightArrow); yield return null;
            Assert.That(table.IsTurning, Is.True); Assert.That(table.SelectedIndex, Is.Zero);
            Assert.That(table.Turn(1), Is.False); Assert.That(table.AcceptCurrent(), Is.False);
            yield return new WaitForSecondsRealtime(1.08f);
            Assert.That(table.SelectedIndex, Is.EqualTo(2)); Assert.That(table.IsTurning, Is.False);
            Assert.That(table.AcceptCurrent(), Is.False); Assert.That(mission.Accepted, Is.False);
            Press(Key.LeftArrow); yield return null; yield return new WaitForSecondsRealtime(1.08f);
            Assert.That(table.SelectedIndex, Is.Zero);
            Press(Key.F); yield return null;
            Assert.That(mission.Accepted && table.IsOpen, Is.True);
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Guild"));
            Assert.That(mission.Depart(), Is.False, "The table must be closed before going to the exit.");
            Press(Key.Escape); yield return null;
            Assert.That(mission.Accepted && !table.IsOpen && mission.CanAct, Is.True);
        }
        [UnityTest] public IEnumerator ClosingDuringTurnCancelsAnimationAndNoRemoteTabAcceptance()
        {
            Press(Key.Tab); Press(Key.Enter); yield return null;
            Assert.That(table.IsOpen || mission.Accepted, Is.False);
            table.Open(); table.Turn(1); yield return null;
            Press(Key.Escape); yield return null;
            Assert.That(table.IsOpen || table.IsTurning || mission.Paused, Is.False);
            table.Open(); yield return new WaitForSecondsRealtime(0.65f);
            Assert.That(table.SelectedIndex, Is.Zero);
            Assert.That(table.DisplayIndex, Is.Zero);
        }
        [UnityTest] public IEnumerator DocumentMovesBesidePileAndChangesFrontBackOrder([Values(-1, 1)] int direction)
        {
            table.Open(); table.Turn(direction);
            yield return new WaitForSecondsRealtime(0.22f);
            yield return null;
            Canvas.ForceUpdateCanvases();
            var curl = Object.FindFirstObjectByType<ContractPageCurl>();
            Assert.That(curl, Is.Not.Null);
            Assert.That(curl.GetComponent<CanvasRenderer>(), Is.Not.Null);
            Assert.That(curl.rectTransform.rect.width, Is.GreaterThan(0));
            var mesh = curl.canvasRenderer.GetMesh();
            Assert.That(mesh, Is.Not.Null); Assert.That(mesh.vertexCount, Is.GreaterThan(4));
            Assert.That(mesh.vertices.Select(v => v.y).Distinct().Count(), Is.GreaterThan(4), "Paper must bend, not just show a flat rectangle.");
            var sheet = curl.rectTransform;
            var pile = sheet.parent.Find("ActiveContract");
            var back = sheet.parent.Find("PaperStackBack");
            Assert.That(sheet.anchoredPosition.x, Is.LessThan(-sheet.rect.width * 0.1f));
            Assert.That(sheet.GetSiblingIndex() > pile.GetSiblingIndex(), Is.EqualTo(direction < 0));
            var title = curl.GetComponentsInChildren<TMPro.TextMeshProUGUI>().First(t => t.text == (direction < 0 ? table.CurrentOffer.Title : table.offers[table.IncomingIndex].Title));
            Assert.That(title.alpha, Is.EqualTo(1), "Text must stay printed on the moving sheet.");
            yield return new WaitForSecondsRealtime(0.36f); yield return null;
            Assert.That(sheet.GetSiblingIndex() < back.GetSiblingIndex(), Is.EqualTo(direction < 0));
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(0, sheet.localEulerAngles.y)), Is.LessThan(0.01f), "A loose sheet must not hinge like a book page.");
            table.Close();
        }
        [UnityTest] public IEnumerator LeftCyclesFrontToBackAndRightReversesThePile()
        {
            table.Open();
            for (int i = 1; i <= table.Count; i++)
            {
                Assert.That(table.Turn(-1), Is.True); yield return new WaitForSecondsRealtime(1.04f);
                Assert.That(table.SelectedIndex, Is.EqualTo(i % table.Count));
            }
            Assert.That(table.Turn(1), Is.True); yield return new WaitForSecondsRealtime(1.04f);
            Assert.That(table.SelectedIndex, Is.EqualTo(table.Count - 1));
            table.Close();
            var original = table.offers; table.offers = new[] { original[table.SelectedIndex] };
            // A new table with a single document cannot shuffle a second sheet out of thin air.
            var single = new GameObject("Single document test").AddComponent<GuildContractTable>();
            single.mission = mission; single.offers = new[] { original[0] };
            Assert.That(single.Open(), Is.True);
            Assert.That(single.Turn(-1) || single.Turn(1), Is.False);
            single.Close(); Object.Destroy(single.gameObject); table.offers = original;
        }
        [UnityTest] public IEnumerator PaperAndShadowOrderStaysStableAcrossFrames([Values(-1, 1)] int direction)
        {
            table.Open(); table.Turn(direction); yield return null;
            var view = Object.FindFirstObjectByType<GuildContractView>();
            var root = view.transform.Find("ContractDeskView");
            var render = typeof(GuildContractView).GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
            var progress = typeof(GuildContractTable).GetProperty("TurnProgress");
            var sheet = root.Find("TurningPaper"); var shadow = root.Find("MovingPaperShadow");
            var back = root.Find("PaperStackBack"); var front = root.Find("ActiveContract");
            foreach (float phase in new[] { 0.22f, 0.72f })
            {
                progress.SetValue(table, phase); render.Invoke(view, null);
                var order = root.Cast<Transform>().Select(t => t.GetInstanceID()).ToArray();
                bool behind = direction < 0 ? phase >= 0.5f : phase < 0.5f;
                for (int frame = 0; frame < 8; frame++)
                {
                    progress.SetValue(table, phase + frame * 0.01f); render.Invoke(view, null);
                    CollectionAssert.AreEqual(order, root.Cast<Transform>().Select(t => t.GetInstanceID()).ToArray(), "Stable-depth frames must not reshuffle the Canvas hierarchy.");
                    Assert.That(shadow.GetSiblingIndex(), Is.LessThan(sheet.GetSiblingIndex()), "A shadow must never render over the moving document.");
                    Assert.That(sheet.GetSiblingIndex() < back.GetSiblingIndex(), Is.EqualTo(behind));
                    Assert.That(sheet.GetSiblingIndex() > front.GetSiblingIndex(), Is.EqualTo(!behind));
                    Assert.That(root.Find("BriefingSheet").GetComponent<CanvasGroup>().alpha, Is.EqualTo(1), "The stationary briefing must not pulse while moving a document.");
                }
            }
            progress.SetValue(table, 0.999f); render.Invoke(view, null);
            Assert.That(shadow.GetComponent<UnityEngine.UI.Image>().color.a, Is.LessThan(0.002f), "Settle the shadow before hiding the animated sheet.");
            table.Close(); yield return null;
        }
        [UnityTest] public IEnumerator WorldTimeAndHeldDeadlineStillAdvanceWhileReading()
        {
            var source = Object.FindObjectsByType<ThiefInteractable>(FindObjectsSortMode.None).First(i => i.kind == InteractionKind.Resonator);
            player.enabled = false;
            AimAt(source, source.transform.position - Vector3.forward * 1.5f - Vector3.up * 1.15f);
            typeof(ThiefPlayer).GetMethod("FindTarget", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(player, null);
            var original = mission.rules; var rules = Object.Instantiate(original); rules.holdSeconds = new[] { 0.12f, 0.12f, 0.12f };
            mission.rules = mission.World.rules = rules;
            try
            {
                Assert.That(player.TryExtract(), Is.True); var charge = player.Held;
                Assert.That(table.Open(), Is.True); player.enabled = true;
                float time = Time.time;
                yield return new WaitForSeconds(0.24f);
                Assert.That(Time.time, Is.GreaterThan(time + 0.1f));
                Assert.That(charge.Finished, Is.True); Assert.That(player.Held, Is.Null);
                Assert.That(player.IsStunned, Is.True); Assert.That(table.IsOpen, Is.False);
                Assert.That(mission.Paused, Is.False);
            }
            finally { mission.rules = mission.World.rules = original; Object.Destroy(rules); }
        }
        [UnityTest] public IEnumerator DepartureDoorCannotBeTargetedOrUsedUntilContractAcceptance()
        {
            var door = Object.FindObjectsByType<ThiefInteractable>(FindObjectsSortMode.None).First(i => i.kind == InteractionKind.GuildExit);
            var approach = door.transform.position + Vector3.back * 2; approach.y = .05f;
            AimAt(door, approach); yield return null;
            Assert.That(door.CanInteract(player), Is.False);
            Assert.That(player.Target, Is.Null, "Locked exit must not expose the F interaction prompt.");
            Press(Key.F); door.Interact(player); yield return null; yield return null;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Guild"));
            Assert.That(mission.Accepted, Is.False);
            table.Open(); Assert.That(table.AcceptCurrent(), Is.True); table.Close();
            AimAt(door, approach); yield return null;
            Assert.That(door.CanInteract(player), Is.True);
            Assert.That(player.Target, Is.SameAs(door));
            Press(Key.F); yield return null; yield return null;
            var stage = Object.FindFirstObjectByType<MissionDirector>();
            Assert.That(stage.isGuild, Is.False); Assert.That(stage.Accepted, Is.True);
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo(MissionDirector.StageScene));
        }
        [UnityTest] public IEnumerator SuccessfulTheftAndExitReturnToGuildDoorWithFreshState()
        {
            Assert.That(new GuildContractOffer().nonRepeatable, Is.False, "New offers must allow repeat acceptance by default.");
            Assert.That(table.CurrentOffer.nonRepeatable, Is.True, "Stage 1 must be configured as non-repeatable in the saved Guild scene.");
            table.Open(); Assert.That(table.AcceptCurrent(), Is.True); table.Close();
            mission.Depart(); yield return null; yield return null;
            var stage = Object.FindFirstObjectByType<MissionDirector>(); stage.player.enabled = false;
            foreach (var guard in stage.World.Guards) guard.enabled = false;
            Assert.That(stage.returnToGuildOnSuccess, Is.True);
            Assert.That(stage.World.Guards.Any(g => g.startsAwake && g.patrolPoints.Length > 0), Is.True);
            stage.Complete(); Assert.That(stage.Completed, Is.False); Assert.That(stage.ReturnToGuild(), Is.False);
            var treasure = Object.FindObjectsByType<ThiefInteractable>(FindObjectsSortMode.None).First(i=>i.kind==InteractionKind.Treasure);
            treasure.Interact(stage.player); Assert.That(stage.Completed, Is.False);
            var exit = Object.FindObjectsByType<ThiefInteractable>(FindObjectsSortMode.None).First(i=>i.kind==InteractionKind.Exit);
            exit.Interact(stage.player); Assert.That(stage.Completed, Is.True);
            yield return new WaitForSecondsRealtime(1.4f); yield return null;
            mission = Object.FindFirstObjectByType<MissionDirector>(); player = mission.player; table = mission.contractTable;
            Assert.That(SceneManager.GetActiveScene().name, Is.EqualTo("Guild"));
            Assert.That(mission.ReturnedToGuild, Is.True);
            Assert.That(Vector3.Distance(player.transform.position, mission.guildReturnSpawn.position), Is.LessThan(0.15f));
            Assert.That(player.transform.position.z, Is.GreaterThan(19));
            Assert.That(Vector3.Dot(player.transform.forward, Vector3.back), Is.GreaterThan(0.99f));
            Assert.That(mission.Accepted || table.IsOpen || mission.Paused, Is.False);
            Assert.That(player.Inventory.Items, Is.Empty); Assert.That(player.Held, Is.Null); Assert.That(Time.timeScale, Is.EqualTo(1));
            Assert.That(mission.Depart(), Is.False, "Completed contracts must not remain accepted.");
            Assert.That(table.CurrentOffer.IsCompleted, Is.True);
            Assert.That(table.offers.Where(o => !o.IsAvailable).Any(o => o.IsCompleted), Is.False);
            Assert.That(table.Open(), Is.True); yield return null; yield return null;
            var view = Object.FindFirstObjectByType<GuildContractView>();
            var stamp = (TMPro.TextMeshProUGUI)typeof(GuildContractView).GetField("status", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(view);
            Assert.That(stamp.text, Is.EqualTo("의뢰 완료\n재수락 불가능"));
            Assert.That(stamp.color.g, Is.GreaterThan(stamp.color.r));
            table.Close(); Assert.That(table.Open(), Is.True); yield return null;
            Assert.That(stamp.text, Is.EqualTo("의뢰 완료\n재수락 불가능"), "Reopening the table must preserve completion and its repeat policy.");
            Assert.That(table.CurrentOffer.CanAccept, Is.False);
            Assert.That(table.AcceptCurrent(), Is.False);
            Press(Key.F); yield return null; Press(Key.Enter); yield return null;
            Assert.That(mission.Accepted, Is.False, "F and Enter must not reaccept a completed non-repeatable offer.");
            table.Close(); mission.Accept();
            Assert.That(mission.Accepted, Is.False, "Direct acceptance must respect the same policy.");
            Assert.That(mission.Depart(), Is.False);
            var door = Object.FindObjectsByType<ThiefInteractable>(FindObjectsSortMode.None).First(i => i.kind == InteractionKind.GuildExit);
            Assert.That(door.CanInteract(player), Is.False);
            Assert.That(table.Open(), Is.True);
            table.CurrentOffer.nonRepeatable = false;
            Assert.That(table.CurrentOffer.CanAccept, Is.True, "Turning the inspector option off must allow repeat acceptance.");
            Assert.That(table.AcceptCurrent(), Is.True);
            yield return null;
            Assert.That(stamp.text, Is.EqualTo("완료 · 재수락"));
            Assert.That(table.CurrentOffer.IsCompleted, Is.True);
        }
    }
}
