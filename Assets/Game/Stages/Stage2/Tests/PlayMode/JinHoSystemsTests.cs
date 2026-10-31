using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SoundThief.JinHo.Tests
{
    public sealed class JinHoSystemsTests
    {
        JinHoStageBindings bindings;
        MissionDirector mission;
        ThiefPlayer player;
        ThiefInteractable door, device;
        InputSettings originalInput, testInput;
        Keyboard keyboard;
        Mouse mouse;
        bool originalBackground;

        [UnitySetUp] public IEnumerator SetUp()
        {
            originalBackground=Application.runInBackground; Application.runInBackground=true;
            originalInput=InputSystem.settings; testInput=Object.Instantiate(originalInput);
            testInput.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            testInput.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            InputSystem.settings=testInput;
            keyboard=InputSystem.AddDevice<Keyboard>(); keyboard.MakeCurrent();
            mouse=InputSystem.AddDevice<Mouse>(); mouse.MakeCurrent();
#if UNITY_EDITOR
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/Game/Stages/Stage2/Scenes/JinHoSystemsValidation.unity",new LoadSceneParameters(LoadSceneMode.Single));
#else
            yield return SceneManager.LoadSceneAsync("JinHoSystemsValidation");
#endif
            yield return null;
            bindings=Object.FindFirstObjectByType<JinHoStageBindings>();
            mission=bindings.context.mission; player=mission.player;
            door=Object.FindObjectsByType<ThiefInteractable>(FindObjectsSortMode.None).Single(i=>i.kind==InteractionKind.Door);
            device=Object.FindObjectsByType<ThiefInteractable>(FindObjectsSortMode.None).Single(i=>i.kind==InteractionKind.Resonator);
            mission.Accept();
            Assert.That(bindings.Ready); Assert.That(bindings.propagation.Validate(),Is.Empty);
        }
        [UnityTearDown] public IEnumerator TearDown()
        {
            InputSystem.RemoveDevice(keyboard); InputSystem.RemoveDevice(mouse); InputSystem.settings=originalInput;
            Object.Destroy(testInput); Time.timeScale=1;
            Application.runInBackground=originalBackground;
            yield return null;
        }
        void PlacePlayer(Vector3 position)
        {
            // Component-test setup only; never evidence of a completed normal stage route.
            var controller=player.GetComponent<CharacterController>(); controller.enabled=false;
            player.transform.position=position; controller.enabled=true;
            Physics.SyncTransforms();
        }
        IEnumerator Aim(ThiefInteractable target)
        {
            var delta=target.GetComponentInChildren<Renderer>().bounds.center-player.view.transform.position;
            player.transform.rotation=Quaternion.LookRotation(new Vector3(delta.x,0,delta.z));
            float desired=-Mathf.Atan2(delta.y,new Vector2(delta.x,delta.z).magnitude)*Mathf.Rad2Deg;
            float current=player.view.transform.localEulerAngles.x; if(current>180) current-=360;
            InputSystem.QueueDeltaStateEvent(mouse.delta,new Vector2(0,(current-desired)/.1f));
            yield return null; yield return null;
            Physics.SyncTransforms();
        }
        IEnumerator Press(Key key)
        {
            InputSystem.QueueStateEvent(keyboard,new KeyboardState(key)); yield return null;
            InputSystem.QueueStateEvent(keyboard,new KeyboardState()); yield return null;
        }
        IEnumerator WaitForWatching(float timeout)
        {
            float until=Time.time+timeout;
            while (Time.time<until && (bindings.guard1.State!=GuardState.Watching || bindings.guard2.State!=GuardState.Watching)) yield return null;
            Assert.That(bindings.guard1.State,Is.EqualTo(GuardState.Watching));
            Assert.That(bindings.guard2.State,Is.EqualTo(GuardState.Watching));
        }

        [UnityTest] public IEnumerator OriginalTypesAndInitialStatesAreBoundOnce()
        {
            Assert.That(mission.World.PropagationPolicy,Is.SameAs(bindings.propagation));
            Assert.That(mission.World.Guards.Count,Is.EqualTo(2));
            Assert.That(bindings.guard1.State,Is.EqualTo(GuardState.Sleeping));
            Assert.That(bindings.guard2.State,Is.EqualTo(GuardState.Watching));
            Assert.That(player.GetType(),Is.EqualTo(typeof(ThiefPlayer)));
            Assert.That(bindings.context.hud.GetType(),Is.EqualTo(typeof(ThiefHud)));
            Assert.That(Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length,Is.EqualTo(1));
            Assert.That(mission.World.RegisterPropagationPolicy(bindings.propagation));
            Assert.That(mission.World.Guards.Count,Is.EqualTo(2));
            Debug.Log("JINHO_TEST initial: G1 Sleeping, G2 Watching, original component types, one listener/world/HUD.");
            yield return null;
        }

        [UnityTest] public IEnumerator RealExtractOpenInjectInputsProduceOnePulseAndRestore()
        {
            PlacePlayer(new Vector3(-4.8f,.05f,10.5f)); yield return Aim(door);
            Assert.That(player.Target,Is.SameAs(door));
            yield return Press(Key.E);
            Assert.That(player.Held,Is.Not.Null); Assert.That(player.Held.Grade,Is.EqualTo(2));
            Assert.That(door.relation.IsMuted);
            yield return Press(Key.F); Assert.That(door.IsOpen);
            Assert.That(bindings.propagation.AudioChecks,Is.Zero,"Extracted door opening must be silent.");
            yield return Aim(device); Assert.That(player.Target,Is.SameAs(device));
            yield return Press(Key.Q);
            Assert.That(player.Held,Is.Null); Assert.That(device.emitter.Active);
            Assert.That(device.emitter.CurrentGrade,Is.EqualTo(2));
            Assert.That(bindings.propagation.AudioChecks,Is.EqualTo(1));
            Assert.That(bindings.propagation.HearingChecks,Is.EqualTo(2));
            Assert.That(mission.World.GetComponentsInChildren<AudioSource>().Length,Is.EqualTo(1));
            Assert.That(mission.World.GetComponentsInChildren<SoundRing>().Length,Is.EqualTo(1));
            PlacePlayer(new Vector3(-7,.05f,20));
            yield return new WaitForSeconds(1.6f);
            Assert.That(bindings.guard1.State,Is.EqualTo(GuardState.InvestigatingSound));
            Assert.That(bindings.guard2.State,Is.EqualTo(GuardState.InvestigatingSound));
            yield return new WaitForSeconds(4.7f);
            Assert.That(device.emitter.CurrentGrade,Is.EqualTo(1));
            yield return new WaitForSeconds(2.1f);
            Assert.That(device.emitter.Active,Is.False); Assert.That(door.relation.IsMuted,Is.False);
            yield return WaitForWatching(15);
            Debug.Log("JINHO_TEST inputs: E/F/Q original path, exactly one initial audio/pulse, grade2→1→end, relation restored, both return Watching.");
        }

        [UnityTest] public IEnumerator ClosedDoorLeavesQuietAudioAndExistingInvestigationSurvivesClosure()
        {
            PlacePlayer(new Vector3(7,.05f,8));
            var charge=door.relation.Extract(mission.rules);
            Assert.That(device.emitter.Inject(charge,mission.World));
            yield return new WaitForSeconds(1.6f);
            Assert.That(bindings.guard1.State,Is.EqualTo(GuardState.Sleeping));
            Assert.That(bindings.guard2.State,Is.EqualTo(GuardState.Watching));
            var audio=mission.World.GetComponentsInChildren<AudioSource>();
            Assert.That(audio.Length,Is.EqualTo(1));
            Assert.That(audio[0].volume,Is.GreaterThan(0).And.LessThanOrEqualTo(.25f));
            door.Interact(player); Assert.That(door.IsOpen);
            yield return new WaitForSeconds(2.2f);
            Assert.That(bindings.guard1.State,Is.EqualTo(GuardState.InvestigatingSound));
            Assert.That(bindings.guard2.State,Is.EqualTo(GuardState.InvestigatingSound));
            door.Interact(player); Assert.That(door.IsOpen,Is.False);
            yield return new WaitForSeconds(1.2f);
            Assert.That(device.emitter.Active);
            Assert.That(bindings.guard1.State,Is.EqualTo(GuardState.InvestigatingSound));
            Assert.That(bindings.guard2.State,Is.EqualTo(GuardState.InvestigatingSound));
            door.Interact(player);
            yield return new WaitForSeconds(4);
            yield return WaitForWatching(15);
            Debug.Log("JINHO_TEST door: quiet nonzero audio, no new reaction closed, open delivery, Active investigation survives closure, awake return.");
        }

        [UnityTest] public IEnumerator RealRInputReloadsValidationSceneAndRestoresInitialState()
        {
            PlacePlayer(new Vector3(-4.8f,.05f,10.5f)); yield return Aim(door);
            yield return Press(Key.E); Assert.That(player.Held,Is.Not.Null);
            var oldMission=mission;
            yield return Press(Key.R); yield return null;
            Assert.That(oldMission==null,Is.True,"R must reload the actual validation scene, without a test-only reset.");
            var reloaded=Object.FindFirstObjectByType<JinHoStageBindings>();
            Assert.That(reloaded.Ready);
            Assert.That(reloaded.context.player.Held,Is.Null);
            Assert.That(reloaded.guard1.State,Is.EqualTo(GuardState.Sleeping));
            Assert.That(reloaded.guard2.State,Is.EqualTo(GuardState.Watching));
            Assert.That(reloaded.context.mission.World.Guards.Count,Is.EqualTo(2));
            Assert.That(Object.FindObjectsByType<ThiefInteractable>(FindObjectsSortMode.None)
                .Single(i=>i.kind==InteractionKind.Door).IsOpen,Is.False);
            Debug.Log("JINHO_TEST restart: real R reload, new policy/world, empty hand, G1 Sleeping/G2 Watching, door closed.");
        }
    }
}
