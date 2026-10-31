using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SoundThief.Tests
{
    public sealed class FrameworkPlayModeTests
    {
        StageContext context;
        MissionDirector Mission => context.mission;
        ThiefPlayer Player => context.player;
        ThiefInteractable Item(string id) => context.SceneComponents<ThiefInteractable>().First(i=>i.ItemId==id && (i.kind==InteractionKind.Key || i.kind==InteractionKind.Collectible));
        [UnitySetUp] public IEnumerator Setup()
        {
            Time.timeScale=1;yield return SceneManager.LoadSceneAsync("FrameworkExample");yield return null;
            context=Object.FindFirstObjectByType<StageContext>();Player.enabled=false;
            foreach(var guard in context.SceneComponents<GuardAgent>())guard.enabled=false;
        }
        [UnityTearDown] public IEnumerator Cleanup(){Time.timeScale=1;Cursor.lockState=CursorLockMode.None;yield return null;}
        [UnityTest] public IEnumerator SceneBindsSharedActorsAndValidates()
        {
            Assert.That(context.Validate(),Is.Empty);
            Assert.That(Mission.Accepted && Mission.CanAct,Is.True);
            Assert.That(Mission.stage,Is.SameAs(context.definition));
            Assert.That(Player.Mission,Is.SameAs(Mission));Assert.That(context.hud.mission,Is.SameAs(Mission));
            Assert.That(context.SceneComponents<GuardAgent>().All(g=>g.world==Mission.World),Is.True);
            yield return null;
        }
        [UnityTest] public IEnumerator MultipleCustomObjectivesAndCustomKeyControlCompletionAndRestart()
        {
            var door=context.SceneComponents<ThiefInteractable>().First(i=>i.kind==InteractionKind.Door);
            door.Interact(Player);Assert.That(door.IsOpen,Is.False);
            Player.HasKey=true;door.Interact(Player);Assert.That(door.IsOpen,Is.False,"Original manor key cannot unlock a stage-specific door.");
            Item("copper_key").Interact(Player);door.Interact(Player);Assert.That(door.IsOpen,Is.True);
            var exit=context.SceneComponents<ThiefInteractable>().First(i=>i.kind==InteractionKind.Exit);
            exit.Interact(Player);Assert.That(Mission.Completed,Is.False);
            Item("relic_a").Interact(Player);exit.Interact(Player);Assert.That(Mission.Completed,Is.False);
            Item("relic_b").Interact(Player);Assert.That(Mission.HasObjectives,Is.True);
            Assert.That(Mission.Extracted || Mission.Injected,Is.False,"Abilities are optional, not completion gates.");
            exit.Interact(Player);Assert.That(Mission.Completed,Is.True);
            Mission.Restart();yield return null;yield return null;
            context=Object.FindFirstObjectByType<StageContext>();
            Assert.That(SceneManager.GetActiveScene().name,Is.EqualTo("FrameworkExample"));
            Assert.That(Player.Inventory.Items,Is.Empty);Assert.That(Mission.Accepted && !Mission.Completed,Is.True);
            Assert.That(context.SceneComponents<ThiefInteractable>().Any(i=>i.IsOpen || i.Acquired),Is.False);
        }
        [UnityTest] public IEnumerator ValidatorReportsMissingObjectivesReferencesAndDuplicateContexts()
        {
            var original=context.definition;var clone=Object.Instantiate(original);context.definition=clone;
            clone.requiredItemIds=new[]{"missing","missing"};context.hud=null;
            var duplicate=new GameObject("Duplicate context").AddComponent<StageContext>();
            var errors=context.Validate();
            Assert.That(errors.Any(e=>e.Contains("StageContext")),Is.True);
            Assert.That(errors.Any(e=>e.Contains("missing")),Is.True);
            Assert.That(errors.Any(e=>e.Contains("중복")),Is.True);
            Assert.That(errors.Any(e=>e.Contains("HUD")),Is.True);
            Object.Destroy(duplicate.gameObject);Object.Destroy(clone);context.definition=original;yield return null;
        }
        [UnityTest] public IEnumerator DoorRespectsAuthoredRotationAndInventoryRejectsDuplicateIds()
        {
            var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.transform.rotation=Quaternion.Euler(0,35,0);
            var door=obj.AddComponent<ThiefInteractable>();door.kind=InteractionKind.Door;door.requiresKey=false;
            door.Interact(Player);Assert.That(Mathf.DeltaAngle(obj.transform.eulerAngles.y,-65),Is.EqualTo(0).Within(0.1f));
            door.Interact(Player);Assert.That(Mathf.DeltaAngle(obj.transform.eulerAngles.y,35),Is.EqualTo(0).Within(0.1f));
            Assert.That(Player.Inventory.Add("a","A"),Is.True);Assert.That(Player.Inventory.Add("a","Again"),Is.False);
            Assert.That(Player.Inventory.Items.Count,Is.EqualTo(1));Object.Destroy(obj);yield return null;
        }
        [UnityTest] public IEnumerator StageDataCanRequireExternalStartDespiteLegacyDefault()
        {
            var definition=Object.Instantiate(context.definition);definition.startImmediately=false;
            var obj=new GameObject("External-start adapter check");var director=obj.AddComponent<MissionDirector>();
            director.stage=definition;director.startsAccepted=true;director.player=Player;
            yield return null;
            Assert.That(director.Accepted,Is.False,"Stage data takes precedence over legacy defaults.");
            director.Accept();Assert.That(director.CanAct,Is.True);
            Object.Destroy(obj);Object.Destroy(definition);yield return null;
        }
    }
}
