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
        UnityEngine.InputSystem.InputSettings originalInputSettings, isolatedInputSettings;
        UnityEngine.InputSystem.Keyboard testKeyboard;
        ThiefInteractable Door => Object.FindObjectsByType<ThiefInteractable>(FindObjectsSortMode.None).First(i => i.kind == InteractionKind.Door && i.name == "현관문");
        ThiefInteractable Resonator(int index) => Object.FindObjectsByType<ThiefInteractable>(FindObjectsSortMode.None)
            .Where(i => i.kind == InteractionKind.Resonator).OrderBy(i => i.name).ElementAt(index);
        ThiefInteractable Find(InteractionKind kind) => Object.FindObjectsByType<ThiefInteractable>(FindObjectsSortMode.None).First(i => i.kind == kind && (kind != InteractionKind.Key || i.ItemId == "key"));

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Time.timeScale = 1;
            yield return SceneManager.LoadSceneAsync("ManorPrototype");
            yield return null;
            mission = Object.FindFirstObjectByType<MissionDirector>(); player = mission.player;
            rules = Object.Instantiate(mission.rules); mission.rules = mission.World.rules = rules;
            var guards = Object.FindObjectsByType<GuardAgent>(FindObjectsSortMode.None);
            guard = guards.First(g => g.name == "중앙 홀 경비");
            foreach (var agent in guards) agent.enabled = false;
            player.enabled = false; mission.Accept();
        }
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1; Cursor.lockState = CursorLockMode.None;
            if (testKeyboard != null) { UnityEngine.InputSystem.InputSystem.RemoveDevice(testKeyboard); testKeyboard = null; }
            if (isolatedInputSettings)
            {
                UnityEngine.InputSystem.InputSystem.settings = originalInputSettings;
                Object.Destroy(isolatedInputSettings);
                isolatedInputSettings = null;
            }
            if (rules) Object.Destroy(rules);
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
        public IEnumerator StudyHasTwoConnectionsAndDiningRequiresStudyKey()
        {
            Assert.That(Resonator(0).displayName, Does.Contain("시계"));
            Assert.That(Resonator(1).displayName, Does.Contain("갑옷"));
            foreach (float z in new[] {10.5f, 19.5f})
            {
                var from = new Vector3(-7, 0.1f, z);
                var to = new Vector3(-11, 0.1f, z);
                Assert.That(Physics.Linecast(from + Vector3.up, to + Vector3.up, 1 << 8), Is.False,
                    "A doorway must allow player passage.");
                Assert.That(UnityEngine.AI.NavMesh.SamplePosition(from, out var start, 1, UnityEngine.AI.NavMesh.AllAreas), Is.True);
                Assert.That(UnityEngine.AI.NavMesh.SamplePosition(to, out var end, 1, UnityEngine.AI.NavMesh.AllAreas), Is.True);
                var path = new UnityEngine.AI.NavMeshPath();
                Assert.That(UnityEngine.AI.NavMesh.CalculatePath(start.position, end.position, UnityEngine.AI.NavMesh.AllAreas, path), Is.True);
                Assert.That(path.status, Is.EqualTo(UnityEngine.AI.NavMeshPathStatus.PathComplete));
                float length = 0;
                for (int i = 1; i < path.corners.Length; i++) length += Vector3.Distance(path.corners[i-1], path.corners[i]);
                Assert.That(length, Is.LessThan(8), "The nearby opening should not require detouring through the other doorway.");
            }
            var items = Object.FindObjectsByType<ThiefInteractable>(FindObjectsSortMode.None);
            var diningDoor = items.First(i => i.name == "식당 앞문");
            var rearDoor = items.First(i => i.name == "식당 뒷문");
            var diningKey = items.First(i => i.kind == InteractionKind.Key && i.ItemId == "dining_key");
            Assert.That(diningKey.transform.position.x, Is.LessThan(-16), "The study key should use the expanded room.");
            Assert.That(Find(InteractionKind.Treasure).transform.position.x, Is.GreaterThan(16), "The seal should use the expanded dining room.");
            foreach (var points in new[] {
                new[] { new Vector3(-11,0,19.5f), new Vector3(-18,0,20) },
                new[] { new Vector3(11,0,10.5f), new Vector3(18,0,10) } })
            {
                Assert.That(UnityEngine.AI.NavMesh.SamplePosition(points[0], out var from, 1, UnityEngine.AI.NavMesh.AllAreas), Is.True);
                Assert.That(UnityEngine.AI.NavMesh.SamplePosition(points[1], out var to, 1, UnityEngine.AI.NavMesh.AllAreas), Is.True);
                var route = new UnityEngine.AI.NavMeshPath();
                Assert.That(UnityEngine.AI.NavMesh.CalculatePath(from.position, to.position, UnityEngine.AI.NavMesh.AllAreas, route), Is.True);
                Assert.That(route.status, Is.EqualTo(UnityEngine.AI.NavMeshPathStatus.PathComplete), "Expanded room must have a walkable route.");
            }
            Assert.That(Physics.Linecast(new Vector3(7,1,19.5f), new Vector3(11,1,19.5f), 1 << 8), Is.True, "The rear entrance cannot bypass the lock.");
            diningDoor.Interact(player); Assert.That(diningDoor.IsOpen, Is.False);
            rearDoor.Interact(player); Assert.That(rearDoor.IsOpen, Is.False, "The second entrance must also require the study key.");
            Assert.That(player.Inventory.Contains("dining_key"), Is.False);
            diningKey.Interact(player); Assert.That(player.Inventory.Contains("dining_key"), Is.True);
            diningDoor.Interact(player); Assert.That(diningDoor.IsOpen, Is.True);
            rearDoor.Interact(player); Assert.That(rearDoor.IsOpen, Is.True);
            yield return null;
        }
        [UnityTest]
        public IEnumerator DiningDoorVisualsRemainAlignedWhenOpened()
        {
            player.Inventory.Add("dining_key", "식당 열쇠");
            foreach (var door in Object.FindObjectsByType<ThiefInteractable>(FindObjectsSortMode.None).Where(i => i.requiredItemId == "dining_key" && i.kind == InteractionKind.Door))
            {
                var visual = door.transform.Find("식당 문 외형");
                Assert.That(visual, Is.Not.Null, "Each dining entrance must have a visible door.");
                var collider = door.transform.Find("식당 문 충돌체").GetComponent<Collider>();
                void CheckAlignment()
                {
                    var renderers = visual.GetComponentsInChildren<Renderer>();
                    var bounds = renderers[0].bounds;
                    foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
                    Assert.That(Vector3.Distance(bounds.center, collider.bounds.center), Is.LessThan(.1f));
                    Assert.That(Vector3.Distance(bounds.size, collider.bounds.size), Is.LessThan(.2f));
                }
                CheckAlignment();
                door.Interact(player); Assert.That(door.IsOpen, Is.True);
                yield return null;
                CheckAlignment();
            }
        }
        [UnityTest]
        public IEnumerator SilentDoorSoundCanBeCarriedOnFootToLureTheStudyGuard()
        {
            yield return GuardedStealthScenario(false);
        }
        [UnityTest]
        public IEnumerator ExploratoryGuardedStealthFullEscape()
        {
            yield return GuardedStealthScenario(true);
        }
        [UnityTest]
        public IEnumerator PitcherLureAlsoOpensRouteToStudyKey()
        {
            yield return GuardedStealthScenario(false, true);
        }
        IEnumerator GuardedStealthScenario(bool tryFullEscape, bool usePitcher = false)
        {
            foreach (var agent in Object.FindObjectsByType<GuardAgent>(FindObjectsSortMode.None)) agent.enabled = true;
            var studyGuard = Object.FindObjectsByType<GuardAgent>(FindObjectsSortMode.None).First(g => g.name == "서재 순찰 경비");
            var lure = usePitcher ? Object.FindObjectsByType<ThiefInteractable>(FindObjectsSortMode.None).First(i => i.name == "서재 황동 물병") : Resonator(0);
            var controller = player.GetComponent<CharacterController>();
            string traceFile = "Logs/CoreLoop/route-" + (tryFullEscape ? "escape" : usePitcher ? "pitcher" : "clock") + ".txt";
            System.IO.Directory.CreateDirectory("Logs/CoreLoop"); System.IO.File.WriteAllText(traceFile, "Guarded real-speed route\n");
            void Trace(string step)
            {
                string actors = string.Join("; ", Object.FindObjectsByType<GuardAgent>(FindObjectsSortMode.None)
                    .Select(g => g.name + " " + g.State + " at " + g.transform.position));
                System.IO.File.AppendAllText(traceFile, Time.time.ToString("F2") + " " + step + " player=" + (player ? player.transform.position.ToString() : "caught") + " | " + actors + "\n");
            }
            IEnumerator WalkTo(Vector3 destination)
            {
                Trace("Walk to " + destination);
                Assert.That(player && mission && mission.player == player, Is.True, "Caught before walking to " + destination + ". See " + traceFile);
                Assert.That(UnityEngine.AI.NavMesh.SamplePosition(player.transform.position, out var start, 2, UnityEngine.AI.NavMesh.AllAreas), Is.True);
                Assert.That(UnityEngine.AI.NavMesh.SamplePosition(destination, out var end, 2, UnityEngine.AI.NavMesh.AllAreas), Is.True);
                var path = new UnityEngine.AI.NavMeshPath();
                Assert.That(UnityEngine.AI.NavMesh.CalculatePath(start.position, end.position, UnityEngine.AI.NavMesh.AllAreas, path), Is.True);
                Assert.That(path.status, Is.EqualTo(UnityEngine.AI.NavMeshPathStatus.PathComplete));
                foreach (var corner in path.corners.Skip(1))
                {
                    float deadline = Time.time + 20;
                    string lastPosition = "unknown";
                    while (true)
                    {
                        Assert.That(player && mission && mission.player == player, Is.True, "The player was caught near " + lastPosition + " while walking to " + destination);
                        if (player.Held != null) Assert.That(player.Held.Remaining, Is.GreaterThan(0), "The carried sound expired while walking to " + destination);
                        if (Vector2.Distance(new Vector2(player.transform.position.x, player.transform.position.z), new Vector2(corner.x, corner.z)) <= .2f || Time.time >= deadline) break;
                        lastPosition = player.transform.position.ToString();
                        Vector3 delta = corner - player.transform.position; delta.y = 0;
                        float speed = player.Held == null ? rules.walkSpeed : rules.carrySpeed;
                        controller.Move(Vector3.ClampMagnitude(delta, speed * Time.deltaTime) + Vector3.down * .05f);
                        yield return null;
                    }
                    Assert.That(Time.time, Is.LessThan(deadline), "The walking route was blocked at " + corner);
                }
            }
            void TargetFromHere(ThiefInteractable target)
            {
                var cameraPosition = player.view.transform.localPosition;
                cameraPosition.y = player.Held == null ? 1.65f : 1.05f;
                player.view.transform.localPosition = cameraPosition;
                player.view.transform.LookAt(target.GetComponentInChildren<Renderer>().bounds.center);
                Physics.SyncTransforms();
                typeof(ThiefPlayer).GetMethod("FindTarget", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(player, null);
                Assert.That(player.Target, Is.SameAs(target), "The target must be selected by the interaction ray.");
            }
            yield return WalkTo(new Vector3(-1.8f, 0, 2)); TargetFromHere(Find(InteractionKind.Key)); player.Target.Interact(player);
            yield return WalkTo(new Vector3(0, 0, 4)); TargetFromHere(Door);
            Assert.That(player.TryExtract(), Is.True); var carried = player.Held;
            Door.Interact(player); Assert.That(Door.IsOpen, Is.True);
            Assert.That(guard.State, Is.EqualTo(GuardState.Sleeping), "A muted door should not wake the hall guard.");
            yield return null; yield return null; yield return null;
            yield return WalkTo(new Vector3(-7, 0, 11.2f));
            float observationUntil = Time.time + 14;
            while (studyGuard && (studyGuard.transform.position.x > -15.5f || studyGuard.transform.position.z < 17.5f) && Time.time < observationUntil)
            {
                Assert.That(player && player.Held != null && player.Held.Remaining > 3, Is.True, "The observation window must fit the real carry deadline.");
                yield return null;
            }
            Trace("Committed after observing the guard away from the south aisle");
            yield return WalkTo(usePitcher ? new Vector3(-13.2f, 0, 8.1f) : new Vector3(-10.8f, 0, 12.4f)); TargetFromHere(lure);
            Assert.That(carried.Remaining, Is.GreaterThan(0), "The sound must survive the real-speed walk.");
            Assert.That(player.TryInject(), Is.True);
            Assert.That(lure.emitter.Pending, Is.True);
            if (usePitcher)
            {
                // Watch from the south side of the shelf; leave by the opposite aisle
                // after the guard commits to investigating the pitcher.
                yield return WalkTo(new Vector3(-11.5f, 0, 10.5f));
                float window = Time.time + 10;
                while (studyGuard && !(studyGuard.State == GuardState.InvestigatingSound && studyGuard.transform.position.z < 12.2f)
                    && Time.time < window) yield return null;
                Assert.That(studyGuard && studyGuard.State == GuardState.InvestigatingSound, Is.True, "The pitcher must draw the guard past the observation shelf.");
            }
            yield return WalkTo(new Vector3(-7.3f, 0, 16));
            float until = Time.time + 6;
            while (!usePitcher && studyGuard && studyGuard.State != GuardState.InvestigatingSound && Time.time < until) yield return null;
            if (!usePitcher) Assert.That(studyGuard && studyGuard.State == GuardState.InvestigatingSound, Is.True, "The delayed sound must actually distract the study guard.");
            Assert.That(player && mission && mission.player == player, Is.True, "The player should be able to retreat from the lure.");
            yield return new WaitForSeconds(3f); // Let the distracted guard move away from the rear entrance.
            var items = Object.FindObjectsByType<ThiefInteractable>(FindObjectsSortMode.None);
            var diningKey = items.First(i => i.kind == InteractionKind.Key && i.ItemId == "dining_key");
            var diningDoor = items.First(i => i.name == "식당 앞문");
            var rearDoor = items.First(i => i.name == "식당 뒷문");
            yield return WalkTo(new Vector3(-7.3f, 0, 19.5f));
            yield return WalkTo(new Vector3(-11, 0, 19.5f));
            yield return WalkTo(new Vector3(-19, 0, 19.5f)); TargetFromHere(diningKey); diningKey.Interact(player);
            Assert.That(player.Inventory.Contains("dining_key"), Is.True);
            Trace("Study key acquired using " + lure.name);
            if (!tryFullEscape) yield break;
            yield return WalkTo(new Vector3(-7.3f, 0, 19.5f));
            yield return WalkTo(new Vector3(7.4f, 0, 10.5f)); TargetFromHere(diningDoor);
            Assert.That(player.TryExtract(), Is.True, "The dining door can be opened silently for the return route.");
            diningDoor.Interact(player); Assert.That(diningDoor.IsOpen, Is.True);
            yield return null; yield return null; yield return null;
            yield return WalkTo(new Vector3(11.4f, 0, 11.5f)); TargetFromHere(Resonator(1));
            Assert.That(player.TryInject(), Is.True, "The dining door sound can be injected into the armour.");
            yield return WalkTo(new Vector3(19, 0, 8.3f)); TargetFromHere(Find(InteractionKind.Treasure));
            Assert.That(player.TryExtract(), Is.True, "The metal seal must be muted before theft."); player.Target.Interact(player);
            TargetFromHere(items.First(i => i.name == "03 식당 배식 물병"));
            Assert.That(player.TryInject(), Is.True, "Place the stolen seal sound away from the escape route.");
            yield return WalkTo(new Vector3(10.8f, 0, 19.5f)); TargetFromHere(rearDoor);
            Assert.That(player.TryExtract(), Is.True, "The rear door can be opened silently with its key.");
            rearDoor.Interact(player); Assert.That(rearDoor.IsOpen, Is.True);
            TargetFromHere(rearDoor);
            Assert.That(player.TryInject(), Is.True, "Leave the rear-door sound behind before taking the hall route.");
            yield return null; yield return null; yield return null;
            yield return WalkTo(new Vector3(7f, 0, 19.5f));
            yield return WalkTo(new Vector3(0, 0, 19.5f));
            yield return WalkTo(new Vector3(-5.5f, 0, 12));
            yield return WalkTo(new Vector3(0, 0, 8.4f)); TargetFromHere(Door);
            if (!Door.IsOpen)
            {
                if (player.Held != null) Assert.That(player.TryInject(), Is.True, "Free the hand before muting the closed entrance.");
                Assert.That(player.TryExtract(), Is.True); Door.Interact(player);
                yield return null; yield return null; yield return null;
            }
            yield return WalkTo(new Vector3(0, 0, 4));
            if (player.Held != null) Assert.That(player.Held.Remaining, Is.GreaterThan(0), "The carried sound must not expire before escape.");
            yield return WalkTo(new Vector3(0, 0, 0)); if (player.Held != null) Assert.That(player.Held.Remaining, Is.GreaterThan(0));
            TargetFromHere(Find(InteractionKind.Exit)); player.Target.Interact(player);
            Assert.That(mission.Completed, Is.True);
        }
        [UnityTest]
        public IEnumerator ManorCanBeTraversedOnFootThroughBothLocks()
        {
            var items = Object.FindObjectsByType<ThiefInteractable>(FindObjectsSortMode.None);
            var diningDoor = items.First(i => i.name == "식당 앞문");
            var diningKey = items.First(i => i.kind == InteractionKind.Key && i.ItemId == "dining_key");
            var controller = player.GetComponent<CharacterController>();
            IEnumerator WalkTo(Vector3 destination)
            {
                Assert.That(UnityEngine.AI.NavMesh.SamplePosition(player.transform.position, out var start, 2, UnityEngine.AI.NavMesh.AllAreas), Is.True);
                Assert.That(UnityEngine.AI.NavMesh.SamplePosition(destination, out var end, 2, UnityEngine.AI.NavMesh.AllAreas), Is.True);
                var path = new UnityEngine.AI.NavMeshPath();
                Assert.That(UnityEngine.AI.NavMesh.CalculatePath(start.position, end.position, UnityEngine.AI.NavMesh.AllAreas, path), Is.True);
                Assert.That(path.status, Is.EqualTo(UnityEngine.AI.NavMeshPathStatus.PathComplete), "A complete walking route is required from " + start.position + " to " + end.position);
                foreach (var corner in path.corners.Skip(1))
                {
                    int attempts = 0;
                    while (Vector3.Distance(new Vector3(player.transform.position.x, 0, player.transform.position.z), new Vector3(corner.x, 0, corner.z)) > .2f && attempts++ < 500)
                    {
                        Vector3 delta = corner - player.transform.position; delta.y = 0;
                        controller.Move(Vector3.ClampMagnitude(delta, .12f) + Vector3.down * .05f);
                        if (attempts % 15 == 0) yield return null;
                    }
                    Assert.That(attempts, Is.LessThan(500), "A collider blocks the walking route to " + corner);
                }
                yield return null;
            }
            void UseFromHere(ThiefInteractable target)
            {
                player.view.transform.LookAt(target.GetComponentInChildren<Renderer>().bounds.center);
                Physics.SyncTransforms();
                typeof(ThiefPlayer).GetMethod("FindTarget", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(player, null);
                Assert.That(player.Target, Is.SameAs(target), "The target must be reachable by the actual interaction ray.");
                target.Interact(player);
            }
            yield return WalkTo(new Vector3(-1.8f, 0, 2)); UseFromHere(Find(InteractionKind.Key));
            yield return WalkTo(new Vector3(0, 0, 4)); UseFromHere(Door); Assert.That(Door.IsOpen, Is.True);
            yield return null; yield return null; yield return null;
            yield return WalkTo(new Vector3(-19, 0, 19.5f)); UseFromHere(diningKey);
            yield return WalkTo(new Vector3(7.4f, 0, 10.5f)); UseFromHere(diningDoor); Assert.That(diningDoor.IsOpen, Is.True);
            yield return null; yield return null; yield return null;
            yield return WalkTo(new Vector3(19, 0, 8.3f)); UseFromHere(Find(InteractionKind.Treasure));
            yield return WalkTo(new Vector3(0, 0, 0)); UseFromHere(Find(InteractionKind.Exit));
            Assert.That(mission.Completed, Is.True);
        }
        [UnityTest]
        public IEnumerator ManorProgressionUsesBothKeysThenSealAndExit()
        {
            var items = Object.FindObjectsByType<ThiefInteractable>(FindObjectsSortMode.None);
            var diningDoor = items.First(i => i.name == "식당 앞문");
            var diningKey = items.First(i => i.kind == InteractionKind.Key && i.ItemId == "dining_key");
            Aim(Find(InteractionKind.Key), new Vector3(-1.8f, 0, 2)); player.Target.Interact(player);
            Assert.That(player.HasKey, Is.True);
            Aim(Door, new Vector3(0, 0, 4)); player.Target.Interact(player); Assert.That(Door.IsOpen, Is.True);
            Aim(diningDoor, new Vector3(7.4f, 0, 10.5f)); player.Target.Interact(player);
            Assert.That(diningDoor.IsOpen, Is.False, "The entrance key must not unlock the dining room.");
            Aim(diningKey, new Vector3(-19, 0, 19.5f)); player.Target.Interact(player);
            Assert.That(player.Inventory.Contains("dining_key"), Is.True);
            Aim(diningDoor, new Vector3(7.4f, 0, 10.5f)); player.Target.Interact(player); Assert.That(diningDoor.IsOpen, Is.True);
            Aim(Find(InteractionKind.Treasure), new Vector3(19, 0, 8.3f)); player.Target.Interact(player);
            Assert.That(player.HasTreasure, Is.True);
            Aim(Find(InteractionKind.Exit), new Vector3(0, 0, 0)); player.Target.Interact(player);
            Assert.That(mission.Completed, Is.True);
            yield return null;
        }
        [UnityTest]
        public IEnumerator ShortInputEventsStillInteractAndMovementAdvances()
        {
            // The Test Runner does not keep the Game View focused. Route the synthetic
            // input to an isolated copy of settings; restore the project settings in TearDown.
            originalInputSettings = UnityEngine.InputSystem.InputSystem.settings;
            isolatedInputSettings = Object.Instantiate(originalInputSettings);
            isolatedInputSettings.backgroundBehavior = UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
            isolatedInputSettings.editorInputBehaviorInPlayMode = UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            UnityEngine.InputSystem.InputSystem.settings = isolatedInputSettings;
            testKeyboard = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>("ThiefInputTest");
            player.enabled = true;
            Aim(Find(InteractionKind.Key), new Vector3(-1.8f, 0, 2));
            typeof(ThiefPlayer).GetField("pitch", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(player, player.view.transform.localEulerAngles.x);
            var keyboard = testKeyboard;
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.F));
            UnityEngine.InputSystem.InputSystem.Update();
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard, new UnityEngine.InputSystem.LowLevel.KeyboardState());
            UnityEngine.InputSystem.InputSystem.Update();
            Assert.That((bool)typeof(ThiefPlayer).GetField("interactRequested", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(player), Is.True,
                "The short input must reach the action callback before the next frame.");
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
        public IEnumerator ExtractionMutesWholeRelationAndBlocksSecondCharge()
        {
            ExtractDoor(); var charge = player.Held;
            Assert.That(Door.relation.IsMuted, Is.True);
            Assert.That(player.CurrentSpeed, Is.EqualTo(rules.carrySpeed));
            Door.Interact(player); Assert.That(Door.IsOpen, Is.True);
            Assert.That(player.Held, Is.SameAs(charge), "Opening the door must not reset the held sound.");
            Aim(Resonator(0), new Vector3(-10.5f, 0, 12));
            Assert.That(player.TryExtract(), Is.False);
            Assert.That(charge.Grade, Is.EqualTo(2));
            yield return null;
        }
        [UnityTest]
        public IEnumerator DoorInjectionWaitsThreeSecondsBeforeGuardHearsIt()
        {
            rules.wakeThreshold = 0.1f;
            Aim(Resonator(0), new Vector3(-10.5f, 0, 12));
            Assert.That(player.TryExtract(), Is.True);
            var source = player.Held.Source;
            Aim(Door, new Vector3(0, 0, 4));
            guard.transform.position = Door.transform.position + Vector3.forward;
            Assert.That(player.TryInject(), Is.True);
            Assert.That(Door.emitter.Pending, Is.True);
            yield return new WaitForSeconds(2.8f);
            Assert.That(guard.State, Is.EqualTo(GuardState.Sleeping));
            Assert.That(source.IsMuted, Is.True);
            yield return new WaitForSeconds(0.4f);
            Assert.That(Door.emitter.Pending, Is.False);
            Assert.That(guard.State, Is.EqualTo(GuardState.InvestigatingSound));
        }
        [UnityTest]
        public IEnumerator InjectionAllowsIndependentOutputsAndRestoresOnDecay()
        {
            rules.injectionHold = 0.08f; rules.decayStep = 0.08f;
            ExtractDoor(); var a = Resonator(0); var b = Resonator(1);
            Aim(a, new Vector3(-10.5f, 0, 12)); Assert.That(player.TryInject(), Is.True);
            Assert.That(player.Held, Is.Null); Assert.That(Door.relation.IsMuted, Is.True);
            Aim(b, new Vector3(12, 0, 12)); Assert.That(player.TryExtract(), Is.True);
            Aim(a, new Vector3(-10.5f, 0, 12)); Assert.That(player.TryInject(), Is.False);
            Assert.That(player.Held, Is.Not.Null, "Rejected injection must retain the charge.");
            Aim(b, new Vector3(12, 0, 12)); Assert.That(player.TryInject(), Is.True);
            Assert.That(a.emitter.Active && b.emitter.Active, Is.True);
            yield return new WaitForSeconds(3.5f);
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
            Aim(Resonator(0), new Vector3(-10.5f, 0, 12));
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
        public IEnumerator GuardAppearanceWalksWhenInvestigatingSound()
        {
            rules.wakeThreshold = 0.1f;
            guard.enabled = true;
            var appearance = guard.GetComponent<GuardAppearance>();
            Assert.That(appearance, Is.Not.Null);
            var start = guard.transform.position;
            guard.Hear(start + Vector3.left * 3, 3, null);
            yield return new WaitForSeconds(.4f);
            Assert.That(guard.State, Is.EqualTo(GuardState.InvestigatingSound));
            Assert.That(Vector3.Distance(guard.transform.position, start), Is.GreaterThan(.05f));
            Assert.That(appearance.animator.GetCurrentAnimatorStateInfo(0).IsName("Walk") || appearance.animator.GetNextAnimatorStateInfo(0).IsName("Walk"), Is.True);
        }
        [UnityTest]
        public IEnumerator OnlyHigherGradeRetargetsAndShortSoundIsInvestigatedUntilArrival()
        {
            rules.wakeThreshold = 0.1f;
            var original = guard.transform.position + Vector3.left;
            guard.Hear(original, 2, null); guard.Hear(original + Vector3.forward, 1, null);
            Assert.That(guard.Goal, Is.EqualTo(original));
            guard.Hear(original + Vector3.back, 2, null); Assert.That(guard.Goal, Is.EqualTo(original));
            var higher = original + Vector3.left; guard.Hear(higher, 3, null); Assert.That(guard.Goal, Is.EqualTo(higher));
            guard.enabled = true;
            float until = Time.time + 6;
            while (guard.State == GuardState.InvestigatingSound && Time.time < until) yield return null;
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
