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
        PostalLetterReader Reader => context.mission.GetComponent<PostalLetterReader>();
        void Interact()
        {
            typeof(ThiefPlayer).GetMethod("FindTarget",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(Player,null);
            Player.Target?.Interact(Player);
        }
        void Read(string name,Vector3 point)
        {
            Aim(Named(name),point);Interact();
            Assert.That(Reader.IsReading,Is.True);
        }
        void TakeTarget()
        {
            Read("Letter B",new Vector3(-1,0.08f,18.8f));Reader.TakeOpenedLetter();
        }
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
            context=Object.FindFirstObjectByType<StageContext>();Mission.Accept();Player.enabled=false;
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
            Move(new Vector3(5,.08f,1.5f));
            Object.FindFirstObjectByType<PostalLoadingGate>().Interact(Player);
            yield return null;yield return null;
            var path=new NavMeshPath();
            Assert.That(NavMesh.CalculatePath(new Vector3(5,0,-3),new Vector3(-1,0,17),NavMesh.AllAreas,path),Is.True);
            Assert.That(path.status,Is.EqualTo(NavMeshPathStatus.PathComplete));
            yield return null;
        }
        [UnityTest] public IEnumerator ExpandedShelvesAndReturnRouteAreConnected()
        {
            Move(new Vector3(5,.08f,1.5f));Object.FindFirstObjectByType<PostalLoadingGate>().Interact(Player);
            yield return null;yield return null;
            var points=new[]{new Vector3(7,0,18),new Vector3(7,0,26),new Vector3(-7,0,27),
                new Vector3(-1,0,27),new Vector3(0,0,18.8f),new Vector3(5,0,16),new Vector3(5,0,-2)};
            var path=new NavMeshPath();var previous=new Vector3(-4,0,-4);
            foreach(var point in points)
            {
                Assert.That(NavMesh.SamplePosition(point,out var hit,.4f,NavMesh.AllAreas),Is.True,"Walkable: "+point);
                Assert.That(NavMesh.CalculatePath(previous,hit.position,NavMesh.AllAreas,path),Is.True);
                Assert.That(path.status,Is.EqualTo(NavMeshPathStatus.PathComplete),"Connected: "+point);
                previous=hit.position;
            }
            Assert.That(GameObject.Find("Floor").transform.localScale.x,Is.EqualTo(24));
            Assert.That(GameObject.Find("Floor").transform.localScale.z,Is.EqualTo(38));
            var guard=GameObject.Find("Dispatch guard").transform.position;
            Assert.That(Vector3.Distance(guard,new Vector3(0,0,18.8f)),Is.GreaterThan(1.5f));
            yield return null;
        }
        [UnityTest] public IEnumerator LoadingGateRejectsOutsideAndOpensThroughPostalInteractionInside()
        {
            var gate=Object.FindFirstObjectByType<PostalLoadingGate>();
            var item=gate.GetComponent<ThiefInteractable>();
            Aim(item,new Vector3(5,.08f,-1.5f));Interact();
            Assert.That(gate.IsOpen,Is.False);Assert.That(Mission.Message,Does.Contain("안쪽"));
            item.Interact(Player);Assert.That(item.IsOpen,Is.False,"Shared door cannot bypass the inside release.");
            Player.GetComponent<CharacterController>().Move(Vector3.forward*3);
            Assert.That(Player.transform.position.z,Is.LessThan(-.3f));
            Aim(item,new Vector3(5,.08f,1.5f));Interact();
            Assert.That(gate.IsOpen,Is.True);Assert.That(item.GetComponent<Collider>().enabled,Is.False);
            Assert.That(item.GetComponent<NavMeshObstacle>().enabled,Is.False);
            Player.GetComponent<CharacterController>().Move(Vector3.back*3);
            Assert.That(Player.transform.position.z,Is.LessThan(-1));
            Player.GetComponent<CharacterController>().Move(Vector3.forward*3);
            Assert.That(Player.transform.position.z,Is.GreaterThan(1));
            Mission.Restart();yield return null;yield return null;
            gate=Object.FindFirstObjectByType<PostalLoadingGate>();
            Assert.That(gate.IsOpen,Is.False);Assert.That(gate.GetComponent<Collider>().enabled,Is.True);
        }
        [UnityTest] public IEnumerator ReceptionCannotBypassSortingDoorThroughServicePartition()
        {
            Move(new Vector3(3.3f,.08f,3));Player.GetComponent<CharacterController>().Move(Vector3.left*3);
            Assert.That(Player.transform.position.x,Is.GreaterThan(2.3f));
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
        [UnityTest] public IEnumerator StandingPlayerCanTargetLowExitAfterMoving()
        {
            Move(new Vector3(-3.74f,0.08f,-4.2f));
            Player.GetComponent<CharacterController>().Move(Vector3.forward*0.27f);
            Player.view.transform.localPosition=new Vector3(0,1.65f,0);
            Physics.SyncTransforms();
            Player.view.transform.LookAt(Named("Yard exit").GetComponentInChildren<Collider>().bounds.center);
            typeof(ThiefPlayer).GetMethod("FindTarget",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(Player,null);
            Assert.That(Player.Target,Is.SameAs(Named("Yard exit")));
            yield return null;
        }
        [UnityTest] public IEnumerator ExternalWallStillBlocksInteraction()
        {
            Move(new Vector3(-4,0.08f,-4));
            Player.view.transform.localPosition=new Vector3(0,1.65f,0);
            var exit=Named("Yard exit");
            var obstacle=GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.transform.position=Vector3.Lerp(Player.view.transform.position,exit.GetComponentInChildren<Collider>().bounds.center,0.5f);
            obstacle.transform.localScale=Vector3.one*0.4f;
            Physics.SyncTransforms();
            Player.view.transform.LookAt(exit.GetComponentInChildren<Collider>().bounds.center);
            typeof(ThiefPlayer).GetMethod("FindTarget",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(Player,null);
            Assert.That(Player.Target,Is.Null,"Ignoring the rig must not allow interaction through walls.");
            Object.Destroy(obstacle);yield return null;
        }
        [UnityTest] public IEnumerator RouteClueAndLetterAreBothRequiredForEscape()
        {
            var exit=Named("Yard exit");exit.Interact(Player);Assert.That(Mission.Completed,Is.False);
            TakeTarget();exit.Interact(Player);Assert.That(Mission.Completed,Is.False);
            Named("Route manifest").Interact(Player);yield return null;
            Assert.That(Object.FindFirstObjectByType<PostalBriefing>().RouteKnown,Is.True);
            Assert.That(Mission.HasObjectives,Is.True);exit.Interact(Player);Assert.That(Mission.Completed,Is.True);
        }
        [UnityTest] public IEnumerator ManifestProvidesTheDestinationClue()
        {
            Named("Route manifest").Interact(Player);yield return null;
            Assert.That(Mission.Message,Does.Contain("A~C 선반"));
            Assert.That(Mission.Completed,Is.False);
        }
        [UnityTest] public IEnumerator LettersHaveDistinctBodiesAndReadingDoesNotCollect()
        {
            var letters=context.SceneComponents<PostalLetter>();
            Assert.That(letters.Length,Is.EqualTo(4));
            Assert.That(letters.Select(l=>l.body).Distinct().Count(),Is.EqualTo(4));
            Assert.That(letters.Count(l=>l.isObjective),Is.EqualTo(1));
            Assert.That(letters.Select(l=>l.sender).Distinct().Count(),Is.EqualTo(4));
            Assert.That(letters.Select(l=>l.recipient).Distinct().Count(),Is.EqualTo(4));
            Assert.That(GameObject.Find("B bay left partition"),Is.Null);
            Assert.That(GameObject.Find("B bay right partition"),Is.Null);
            Assert.That(GameObject.Find("B bay back partition"),Is.Null);
            Assert.That(GameObject.Find("Dispatch shelf B").transform.position.z,Is.EqualTo(GameObject.Find("Dispatch shelf A").transform.position.z));
            Assert.That(Vector3.Distance(Named("Letter B").transform.position,GameObject.Find("Dispatch guard").transform.position),Is.GreaterThan(8));
            Read("Letter 01",new Vector3(7,0.08f,18f));
            Assert.That(Reader.OpenedLetter.body,Does.Contain("타르트"));
            Assert.That(Player.Inventory.Items,Is.Empty);
            Assert.That(Named("Letter 01").Acquired,Is.False);
            Reader.Close();Assert.That(Mission.Paused,Is.False);yield return null;
        }
        [UnityTest] public IEnumerator WrongLetterCanBeReplacedAndCannotCompleteContract()
        {
            Named("Route manifest").Interact(Player);
            Read("Letter 01",new Vector3(7,0.08f,18f));Reader.TakeOpenedLetter();
            Assert.That(Mission.HasObjectives,Is.False);
            Named("Yard exit").Interact(Player);Assert.That(Mission.Completed,Is.False);
            Assert.That(Player.Inventory.Contains("postal_letter_1"),Is.True);
            TakeTarget();
            Assert.That(Player.Inventory.Contains("postal_letter_1"),Is.False);
            Assert.That(Player.Inventory.Contains(PostalBriefing.LetterId),Is.True);
            Assert.That(Player.Inventory.Items.Count,Is.EqualTo(2));
            Named("Yard exit").Interact(Player);Assert.That(Mission.Completed,Is.True);yield return null;
        }
        [UnityTest] public IEnumerator ReadingFreezesChargeAndRestoresPlayerAndHud()
        {
            Named("Staff key").Interact(Player);
            Aim(Named("Sorting room bell door"),new Vector3(-3,0.08f,4.2f));Assert.That(Player.TryExtract(),Is.True);
            Player.enabled=true;
            Read("Letter 01",new Vector3(7,0.08f,18f));
            float remaining=Player.Held.Remaining;
            var position=Player.transform.position;
            Assert.That(Mission.Paused,Is.True);Assert.That(Time.timeScale,Is.Zero);Assert.That(Player.enabled,Is.False);
            Assert.That(context.hud.GetComponent<Canvas>().enabled,Is.False);
            yield return new WaitForSecondsRealtime(0.2f);
            Assert.That(Player.Held.Remaining,Is.EqualTo(remaining).Within(0.001f));
            Assert.That(Player.transform.position,Is.EqualTo(position));
            Reader.Close();Assert.That(Mission.Paused,Is.False);Assert.That(Time.timeScale,Is.EqualTo(1));
            Assert.That(Player.enabled,Is.True);Assert.That(context.hud.GetComponent<Canvas>().enabled,Is.True);
            yield return null;Assert.That(Player.Held.Remaining,Is.LessThan(remaining));
        }
        [UnityTest] public IEnumerator ReadingCannotReachThroughWalls()
        {
            Aim(Named("Letter 01"),new Vector3(7,0.08f,18f));
            var obstacle=GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.transform.position=Vector3.Lerp(Player.view.transform.position,Named("Letter 01").GetComponentInChildren<Collider>().bounds.center,.5f);
            obstacle.transform.localScale=Vector3.one*.4f;Physics.SyncTransforms();
            Interact();Assert.That(Reader.IsReading,Is.False);
            Object.Destroy(obstacle);yield return null;
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
            yield return new WaitForSeconds(receiver.emitter.injectionDelaySeconds+context.rules.injectionHold+2*context.rules.decayStep+0.5f);
            Assert.That(receiver.emitter.Active,Is.False);Assert.That(door.relation.IsMuted,Is.False);
        }
        [UnityTest] public IEnumerator RestartResetsPostalItemsAndDoor()
        {
            Named("Staff key").Interact(Player);Named("Route manifest").Interact(Player);
            TakeTarget();Named("Sorting room bell door").Interact(Player);
            Mission.Restart();yield return null;yield return null;
            context=Object.FindFirstObjectByType<StageContext>();
            Assert.That(SceneManager.GetActiveScene().path,Is.EqualTo(ScenePath));
            Assert.That(Player.Inventory.Items,Is.Empty);Assert.That(Mission.CanAct,Is.True);
            Assert.That(context.SceneComponents<ThiefInteractable>().Any(i=>i.Acquired||i.IsOpen),Is.False);
            Assert.That(Object.FindFirstObjectByType<PostalBriefing>().RouteKnown,Is.False);
            Assert.That(Reader.SelectedLetter,Is.Null);
            Assert.That(context.SceneComponents<PostalLetter>().Any(l=>l.WasRead),Is.False);
            Assert.That(Player.transform.position.z,Is.LessThan(0));
        }
    }
}
