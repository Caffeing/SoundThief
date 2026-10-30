using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SoundThief.Tests
{
    public class DoorPosturePlayModeTests
    {
        MissionDirector mission;
        ThiefPlayer player;
        GuardAgent guard;
        Keyboard keyboard;
        GameObject ceiling;
        [UnitySetUp] public IEnumerator Setup()
        {
            Time.timeScale = 1; yield return SceneManager.LoadSceneAsync("ManorPrototype"); yield return null;
            mission = Object.FindFirstObjectByType<MissionDirector>(); player = mission.player;
            player.enabled = false; mission.Accept();
            guard = Object.FindObjectsByType<GuardAgent>(FindObjectsSortMode.None).First(g => g.name == "중앙 홀 경비");
            foreach (var g in Object.FindObjectsByType<GuardAgent>(FindObjectsSortMode.None)) g.enabled = false;
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            if (keyboard != null) InputSystem.RemoveDevice(keyboard);
            if (ceiling) Object.Destroy(ceiling);
            Time.timeScale = 1; yield return null;
        }
        void Place(Vector3 p)
        {
            var controller = player.GetComponent<CharacterController>(); controller.enabled = false; player.transform.position = p; controller.enabled = true;
        }
        [UnityTest] public IEnumerator GuardReopensDoorAndCrossesThePortal()
        {
            var door = GameObject.Find("현관문").GetComponent<ThiefInteractable>();
            var charge = door.relation.Extract(mission.rules); player.HasKey = true;
            door.Interact(player); door.Interact(player); Assert.That(door.IsUnlocked, Is.True);
            Place(new Vector3(-6, 0, 2)); guard.GetComponent<NavMeshAgent>().Warp(new Vector3(0, 0, 9));
            guard.enabled = true; guard.Hear(new Vector3(0, 0, 2), 3, null);
            float end = Time.time + 7;
            while (Time.time < end && guard.transform.position.z > 5) yield return null;
            Assert.That(door.IsOpen, Is.True); Assert.That(guard.transform.position.z, Is.LessThan(5), "The guard must resume the original destination after carving updates.");
            charge.Finish();
        }
        [UnityTest] public IEnumerator LockedDoorRemainsLockedAndGuardStepsAreLouder()
        {
            var door = GameObject.Find("식당 앞문").GetComponent<ThiefInteractable>();
            Assert.That(door.OpenForGuard(guard), Is.False); Assert.That(door.IsOpen, Is.False);
            Assert.That(guard.GetComponent<GuardFootsteps>().volume, Is.GreaterThanOrEqualTo(.85f)); yield return null;
        }
        [UnityTest] public IEnumerator CrouchShrinksBodyAndCannotStandThroughFurniture()
        {
            keyboard = InputSystem.AddDevice<Keyboard>(); keyboard.MakeCurrent(); Place(new Vector3(0, 0, 8));
            var controller = player.GetComponent<CharacterController>(); float original = controller.height, fov = player.view.fieldOfView;
            player.enabled = true; InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.LeftCtrl)); InputSystem.Update();
            yield return new WaitForSeconds(.45f);
            Assert.That(player.IsCrouching, Is.True); Assert.That(controller.height, Is.LessThan(1.1f));
            Assert.That(controller.center.y - controller.height * .5f, Is.EqualTo(0).Within(.02f));
            Assert.That(player.view.fieldOfView, Is.LessThan(fov - 2));
            ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube); ceiling.layer = 8; ceiling.transform.position = player.transform.position + Vector3.up * 1.5f; ceiling.transform.localScale = new Vector3(2, .7f, 2); Physics.SyncTransforms();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update(); yield return new WaitForSeconds(.3f);
            Assert.That(player.IsCrouching, Is.True); Assert.That(controller.height, Is.LessThan(1.1f));
            Object.Destroy(ceiling); yield return new WaitForSeconds(.45f);
            Assert.That(player.IsCrouching, Is.False); Assert.That(controller.height, Is.EqualTo(original).Within(.02f));
        }
    }
}
