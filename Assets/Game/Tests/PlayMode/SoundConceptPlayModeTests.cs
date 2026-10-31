using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SoundThief.Tests
{
    public sealed class SoundConceptPlayModeTests
    {
        MissionDirector mission;
        SoundRules rules;
        GuardAgent guard;
        ThiefPlayer player;
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Time.timeScale = 1;
            yield return SceneManager.LoadSceneAsync("ManorPrototype"); yield return null;
            mission = Object.FindFirstObjectByType<MissionDirector>(); player = mission.player;
            rules = Object.Instantiate(mission.rules); mission.rules = mission.World.rules = rules;
            foreach (var g in Object.FindObjectsByType<GuardAgent>(FindObjectsSortMode.None)) g.enabled = false;
            guard = Object.FindObjectsByType<GuardAgent>(FindObjectsSortMode.None).First(g => g.name == "중앙 홀 경비");
            player.enabled = false; mission.Accept(); rules.wakeThreshold = .1f;
        }
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            Time.timeScale = 1; Cursor.lockState = CursorLockMode.None;
            if (rules) Object.Destroy(rules); yield return null;
        }
        void PlacePlayer(Vector3 p)
        {
            var cc = player.GetComponent<CharacterController>(); cc.enabled = false; player.transform.position = p; cc.enabled = true;
        }
        [UnityTest]
        public IEnumerator WallAndClosedDoorReduceReachWithoutChangingGrade()
        {
            var from = new Vector3(0, 10, 0); var listener = new Vector3(0, 10, 8);
            Assert.That(mission.World.CanHear(from, listener, 2, out _), Is.True);
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube); wall.layer = 8;
            wall.transform.position = new Vector3(0, 10.7f, 4); wall.transform.localScale = new Vector3(4, 4, .3f);
            var barrier = wall.AddComponent<SoundBarrier>(); barrier.rangeMultiplier = .5f; Physics.SyncTransforms();
            Assert.That(mission.World.CanHear(from, listener, 2, out _), Is.False);
            // Imported furniture and walls often have non-convex mesh colliders.
            Object.Destroy(wall.GetComponent<BoxCollider>()); yield return null;
            wall.AddComponent<MeshCollider>().sharedMesh = wall.GetComponent<MeshFilter>().sharedMesh;
            Physics.SyncTransforms();
            Assert.That(mission.World.CanHear(from, listener, 2, out _), Is.False);
            LogAssert.NoUnexpectedReceived();
            Object.Destroy(barrier); yield return null;
            var door = wall.AddComponent<ThiefInteractable>(); door.kind = InteractionKind.Door; door.requiresKey = false;
            Assert.That(mission.World.CanHear(from, listener, 2, out _), Is.False);
            door.Interact(player); Physics.SyncTransforms();
            Assert.That(door.IsOpen, Is.True); Assert.That(mission.World.CanHear(from, listener, 2, out _), Is.True);
            Object.Destroy(wall);
        }
        [UnityTest]
        public IEnumerator ShortSoundIsInvestigatedUntilArrival()
        {
            PlacePlayer(new Vector3(0, 0, 0));
            guard.GetComponent<NavMeshAgent>().Warp(new Vector3(0, 0, 12)); guard.enabled = true;
            var target = new Vector3(0, 0, 17); guard.Hear(target, 2, null);
            yield return new WaitForSeconds(1.1f);
            Assert.That(guard.State, Is.EqualTo(GuardState.InvestigatingSound), "A short event must not expire before arrival.");
            float until = Time.time + 8;
            while (guard.State == GuardState.InvestigatingSound && Time.time < until) yield return null;
            Assert.That(guard.State, Is.EqualTo(GuardState.Alert));
            Assert.That(Vector2.Distance(new Vector2(guard.transform.position.x, guard.transform.position.z), new Vector2(target.x, target.z)), Is.LessThan(.65f));
        }
        [UnityTest]
        public IEnumerator ActiveContinuousSoundStopsAtCurrentPositionWhenNoLongerAudible()
        {
            PlacePlayer(Vector3.zero); guard.GetComponent<NavMeshAgent>().Warp(new Vector3(0, 0, 12));
            var source = new GameObject("Continuous sound test"); source.transform.position = new Vector3(0, 1, 20);
            var relation = source.AddComponent<SoundRelation>(); relation.grade = 2;
            var emitter = source.AddComponent<SoundEmitter>(); emitter.Inject(relation.Extract(rules), mission.World);
            guard.enabled = true; yield return new WaitForSeconds(3.2f);
            Assert.That(guard.State, Is.EqualTo(GuardState.InvestigatingSound));
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube); wall.layer = 8;
            wall.transform.position = new Vector3(0, 1.5f, 16); wall.transform.localScale = new Vector3(4, 3, .3f);
            wall.AddComponent<SoundBarrier>().rangeMultiplier = .05f; Physics.SyncTransforms();
            yield return null; yield return null;
            Assert.That(emitter.Active, Is.True); Assert.That(guard.State, Is.EqualTo(GuardState.Alert));
            var stopped = guard.transform.position;
            yield return new WaitForSeconds(.25f);
            Assert.That(Vector3.Distance(stopped, guard.transform.position), Is.LessThan(.08f), "The guard must stop where hearing was lost.");
            Object.Destroy(source); Object.Destroy(wall);
        }
        [UnityTest]
        public IEnumerator InjectedSoundKeepsItsOriginalClipAndThreeSecondDelay()
        {
            var items = Object.FindObjectsByType<ThiefInteractable>(FindObjectsSortMode.None);
            var door = items.First(i => i.name == "현관문"); var clock = items.First(i => i.name == "01 서재 괘종시계");
            Assert.That(door.relation.soundClip, Is.Not.Null); Assert.That(clock.relation.soundClip, Is.Not.SameAs(door.relation.soundClip));
            var charge = door.relation.Extract(rules);
            Assert.That(clock.emitter.Inject(charge, mission.World), Is.True);
            Assert.That(clock.emitter.SoundClip, Is.SameAs(door.relation.soundClip));
            Assert.That(clock.emitter.Pending, Is.True);
            yield return new WaitForSeconds(2.8f); Assert.That(clock.emitter.Pending, Is.True);
            yield return new WaitForSeconds(.4f); Assert.That(clock.emitter.Pending, Is.False); Assert.That(door.relation.IsMuted, Is.True);
        }
        [UnityTest]
        public IEnumerator SelfClosingDoorWaitsForPassageAndRespectsSilence()
        {
            var door = Object.FindObjectsByType<ThiefInteractable>(FindObjectsSortMode.None).First(i => i.name == "현관문");
            Assert.That(door.autoCloseSeconds, Is.GreaterThan(0), "The authored stage must retain its closer setting after scene reload.");
            player.HasKey = true; door.autoCloseSeconds = .15f;
            var charge = door.relation.Extract(rules); PlacePlayer(door.transform.position);
            door.Interact(player); yield return new WaitForSeconds(.25f);
            Assert.That(door.IsOpen, Is.True, "The closer must wait while someone is passing.");
            PlacePlayer(Vector3.zero); yield return null; yield return null;
            Assert.That(door.IsOpen, Is.False); Assert.That(door.relation.IsMuted, Is.True);
            Assert.That(guard.State, Is.EqualTo(GuardState.Sleeping), "A muted door must close silently."); charge.Finish();
        }
        [UnityTest]
        public IEnumerator SealCanBeStolenSilentlyAndStudyOffersTwoLureLocations()
        {
            var items = Object.FindObjectsByType<ThiefInteractable>(FindObjectsSortMode.None);
            var seal = items.First(i => i.kind == InteractionKind.Treasure);
            Assert.That(seal.relation, Is.Not.Null); Assert.That(seal.relation.grade, Is.EqualTo(3));
            var dining = Object.FindObjectsByType<GuardAgent>(FindObjectsSortMode.None).First(g => g.name == "식당 경비");
            var charge = seal.relation.Extract(rules); seal.Interact(player);
            Assert.That(player.HasTreasure, Is.True); Assert.That(dining.State, Is.EqualTo(GuardState.Sleeping)); charge.Finish();
            var study = Object.FindObjectsByType<GuardAgent>(FindObjectsSortMode.None).First(g => g.name == "서재 순찰 경비");
            Assert.That(study.startsAwake && study.patrolPoints.Length >= 2, Is.True);
            Assert.That(items.Count(i => i.name == "01 서재 괘종시계" || i.name == "서재 황동 물병"), Is.EqualTo(2));
            yield return null;
        }
        [UnityTest]
        public IEnumerator LoudSealPickupAlertsTheNearbyGuard()
        {
            var seal = Object.FindObjectsByType<ThiefInteractable>(FindObjectsSortMode.None).First(i => i.kind == InteractionKind.Treasure);
            var dining = Object.FindObjectsByType<GuardAgent>(FindObjectsSortMode.None).First(g => g.name == "식당 경비");
            Assert.That(dining.State, Is.EqualTo(GuardState.Sleeping));
            seal.Interact(player);
            Assert.That(player.HasTreasure, Is.True);
            Assert.That(dining.State, Is.EqualTo(GuardState.InvestigatingSound));
            Assert.That(dining.ReactingGrade, Is.EqualTo(3));
            yield return null;
        }
        [UnityTest]
        public IEnumerator InaccessibleInstantSoundIsCheckedAtReachableDoorEdge()
        {
            PlacePlayer(Vector3.zero); guard.GetComponent<NavMeshAgent>().Warp(new Vector3(7, 0, 11.5f)); guard.enabled = true;
            var source = new Vector3(12, 1, 11.5f); guard.Hear(source, 3, null);
            Assert.That(guard.State, Is.EqualTo(GuardState.InvestigatingSound));
            float until = Time.time + 8;
            while (guard.State == GuardState.InvestigatingSound && Time.time < until) yield return null;
            Assert.That(guard.State, Is.EqualTo(GuardState.Alert));
            Assert.That(guard.transform.position.x, Is.LessThan(9), "The guard must stay on the reachable side of the locked dining door.");
            Assert.That(Vector3.Distance(guard.transform.position, new Vector3(7, 0, 11.5f)), Is.GreaterThan(.2f));
            var stopped = guard.transform.position; yield return new WaitForSeconds(.25f);
            Assert.That(Vector3.Distance(stopped, guard.transform.position), Is.LessThan(.08f));
        }
        [UnityTest]
        public IEnumerator SoundObjectsHaveOutlinesAndVisionIsOccludedByWalls()
        {
            var outlines = Object.FindObjectsByType<SoundObjectOutline>(FindObjectsSortMode.None);
            Assert.That(outlines.Length, Is.GreaterThanOrEqualTo(15));
            Assert.That(outlines.All(o => o.material && o.material.shader.isSupported && o.ShellCount > 0), Is.True);
            Assert.That(outlines.All(o => !o.IsVisible), Is.True, "Objects must stay natural when nothing is targeted.");
            var clock = Object.FindObjectsByType<ThiefInteractable>(FindObjectsSortMode.None).First(i => i.name == "01 서재 괘종시계");
            PlacePlayer(new Vector3(-10.5f, 0, 12));
            player.view.transform.LookAt(clock.transform.position + Vector3.up * .2f); player.SendMessage("FindTarget");
            yield return new WaitForSeconds(.2f);
            Assert.That(player.Target, Is.EqualTo(clock)); Assert.That(clock.GetComponent<SoundObjectOutline>().IsVisible, Is.True);
            player.view.transform.LookAt(player.view.transform.position + Vector3.up); player.SendMessage("FindTarget");
            yield return new WaitForSeconds(.2f);
            Assert.That(outlines.All(o => !o.IsVisible), Is.True);
            var study = Object.FindObjectsByType<GuardAgent>(FindObjectsSortMode.None).First(g => g.name == "서재 순찰 경비");
            study.enabled = true; yield return new WaitForSeconds(.2f);
            var cone = study.GetComponent<GuardVisionCone>(); Assert.That(cone && !cone.IsVisible, Is.True, "Sight must not remain on by default.");
            PlacePlayer(study.transform.position - study.transform.forward * 2);
            player.view.transform.LookAt(study.transform.position + Vector3.up * 1.2f);
            yield return new WaitForSeconds(.65f);
            Assert.That(cone.IsVisible, Is.True, "Deliberate observation briefly reveals sight.");
            Assert.That(guard.GetComponent<GuardVisionCone>().IsVisible, Is.False, "A sleeping guard must not advertise active sight.");
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube); wall.layer = 8;
            wall.transform.position = study.transform.position + study.transform.forward * 2.5f + Vector3.up * 1.5f;
            wall.transform.rotation = study.transform.rotation; wall.transform.localScale = new Vector3(8, 3, .3f); Physics.SyncTransforms();
            yield return new WaitForSeconds(.18f);
            Assert.That(Vector3.Distance(cone.WorldBoundary[cone.segments / 2], study.transform.position), Is.LessThan(3.4f), "The floor cone must end at the wall.");
            yield return new WaitForSeconds(1.5f);
            Assert.That(cone.IsVisible, Is.False, "Holding the gaze must not leave an endless overlay.");
            study.Stun(); yield return null;
            Assert.That(cone.IsVisible, Is.False); Object.Destroy(wall);
        }
        [UnityTest]
        public IEnumerator HelmetsSeatAroundActualHeadGeometry()
        {
            foreach (var g in Object.FindObjectsByType<GuardAgent>(FindObjectsSortMode.None))
            {
                var head = g.GetComponentsInChildren<SkinnedMeshRenderer>().First(r => r.name == "head");
                var helmet = g.GetComponentsInChildren<Transform>().First(t => t.name == "House guard open helmet");
                var baked = new Mesh(); head.BakeMesh(baked, false);
                float top = baked.vertices.Max(v => head.transform.TransformPoint(v).y); Object.Destroy(baked);
                var renderers = helmet.GetComponentsInChildren<Renderer>(); var bounds = renderers[0].bounds;
                foreach (var r in renderers) bounds.Encapsulate(r.bounds);
                Assert.That(bounds.max.y, Is.GreaterThan(top + .04f), g.name + " crown must clear the scalp, including the lower front rim.");
                Assert.That(bounds.max.y, Is.LessThan(top + .09f), g.name + " helmet should sit on the crown instead of floating above it.");
                Assert.That(bounds.min.y, Is.LessThan(top - .1f), g.name + " helmet must cover the upper head.");
            }
            yield return null;
        }
    }
}
