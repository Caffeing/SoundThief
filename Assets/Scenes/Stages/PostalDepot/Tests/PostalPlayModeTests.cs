using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SoundThief.PostalDepot.Tests
{
    public sealed class PostalPlayModeTests
    {
        const string ScenePath = "Assets/Scenes/Stages/PostalDepot/PostalDepot.unity";
        StageContext context;
        ThiefPlayer Player => context.player;
        MissionDirector Mission => context.mission;
        ThiefInteractable Named(string name) => context.SceneComponents<ThiefInteractable>().Single(i=>i.name==name);
        void Move(Vector3 point)
        {
            var cc=Player.GetComponent<CharacterController>();cc.enabled=false;
            Player.transform.SetPositionAndRotation(point,Quaternion.identity);cc.enabled=true;Physics.SyncTransforms();
        }
        void Aim(ThiefInteractable item,Vector3 point)
        {
            Move(point);
            Player.view.transform.LookAt(item.GetComponentInChildren<Collider>().bounds.center);
            typeof(ThiefPlayer).GetMethod("FindTarget",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(Player,null);
            Assert.That(Player.Target,Is.SameAs(item),"Target must be visible from the test approach.");
        }
        [UnitySetUp] public IEnumerator Setup()
        {
            Time.timeScale=1;yield return SceneManager.LoadSceneAsync(ScenePath);yield return null;
            context=Object.FindFirstObjectByType<StageContext>();Player.enabled=false;
            foreach(var guard in context.SceneComponents<GuardAgent>()) guard.enabled=false;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {Time.timeScale=1;Cursor.lockState=CursorLockMode.None;yield return null;}

        [UnityTest] public IEnumerator SharedFoundationAndNavMeshAreValid()
        {
            Assert.That(context.Validate(),Is.Empty);
            Assert.That(Mission.CanAct,Is.True);
            Assert.That(context.rules.throwingEnabled,Is.False);
            Assert.That(context.definition.stageId,Is.EqualTo("gganwoor-postal-depot"));
            Assert.That(context.SceneComponents<GuardAgent>().Length,Is.EqualTo(2));
            Assert.That(context.SceneComponents<GuardAgent>().All(g=>g.GetComponent<NavMeshAgent>().isOnNavMesh),Is.True);
            var path=new NavMeshPath();
            Assert.That(NavMesh.CalculatePath(new Vector3(5,0,-3),new Vector3(-1,0,17),NavMesh.AllAreas,path),Is.True);
            Assert.That(path.status,Is.EqualTo(NavMeshPathStatus.PathComplete));
            yield return null;
        }
        [UnityTest] public IEnumerator EntranceIsWalkableAndWallBlocksMovement()
        {
            Move(new Vector3(-4,0.05f,-1));Player.GetComponent<CharacterController>().Move(Vector3.forward*2);
            Assert.That(Player.transform.position.z,Is.GreaterThan(0.5f));
            Move(new Vector3(0,0.05f,-1));Player.GetComponent<CharacterController>().Move(Vector3.forward*2);
            Assert.That(Player.transform.position.z,Is.LessThan(-0.2f));
            yield return null;
        }
        [UnityTest] public IEnumerator RouteClueAndLetterAreBothRequiredForEscape()
        {
            var exit=Named("Yard exit");exit.Interact(Player);Assert.That(Mission.Completed,Is.False);
            Named("Letter B").Interact(Player);exit.Interact(Player);Assert.That(Mission.Completed,Is.False);
            Named("Route manifest").Interact(Player);yield return null;
            Assert.That(Object.FindFirstObjectByType<PostalBriefing>().RouteKnown,Is.True);
            Assert.That(Mission.HasObjectives,Is.True);exit.Interact(Player);Assert.That(Mission.Completed,Is.True);
        }
        [UnityTest] public IEnumerator ManifestProvidesTheDestinationClue()
        {
            Named("Route manifest").Interact(Player);yield return null;
            Assert.That(Mission.Message,Does.Contain("B구역"));
            Assert.That(Mission.Completed,Is.False);
        }
        [UnityTest] public IEnumerator KeyAllowsSilentDoorAndInjectionRestoresRelationship()
        {
            var door=Named("Sorting room bell door");
            Aim(door,new Vector3(-3,0.05f,4.3f));Assert.That(Player.TryExtract(),Is.False);
            Named("Staff key").Interact(Player);Assert.That(Player.TryExtract(),Is.True);
            door.Interact(Player);Assert.That(door.IsOpen,Is.True);Assert.That(door.relation.IsMuted,Is.True);
            Assert.That(context.SceneComponents<GuardAgent>().All(g=>g.State==GuardState.Sleeping),Is.True);
            var receiver=Named("Service receiver");Aim(receiver,new Vector3(4,0.05f,9.2f));
            Assert.That(Player.TryInject(),Is.True);Assert.That(Player.Held,Is.Null);
            Assert.That(receiver.emitter.Active,Is.True);Assert.That(door.relation.IsMuted,Is.True);
            yield return new WaitForSeconds(context.rules.injectionHold+2*context.rules.decayStep+0.5f);
            Assert.That(receiver.emitter.Active,Is.False);Assert.That(door.relation.IsMuted,Is.False);
        }
        [UnityTest] public IEnumerator RestartResetsPostalItemsAndDoor()
        {
            Named("Staff key").Interact(Player);Named("Route manifest").Interact(Player);
            Named("Letter B").Interact(Player);Named("Sorting room bell door").Interact(Player);
            Mission.Restart();yield return null;yield return null;
            context=Object.FindFirstObjectByType<StageContext>();
            Assert.That(SceneManager.GetActiveScene().path,Is.EqualTo(ScenePath));
            Assert.That(Player.Inventory.Items,Is.Empty);Assert.That(Mission.CanAct,Is.True);
            Assert.That(context.SceneComponents<ThiefInteractable>().Any(i=>i.Acquired||i.IsOpen),Is.False);
            Assert.That(Object.FindFirstObjectByType<PostalBriefing>().RouteKnown,Is.False);
            Assert.That(Player.transform.position.z,Is.LessThan(0));
        }
    }
}
