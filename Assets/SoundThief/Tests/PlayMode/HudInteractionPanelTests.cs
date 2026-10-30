using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SoundThief.Tests
{
    public sealed class HudInteractionPanelTests
    {
        [UnityTest]
        public IEnumerator InteractionPanelBacksPromptOnlyWhileTargeted()
        {
            Time.timeScale = 1;
            yield return SceneManager.LoadSceneAsync("ManorPrototype");
            yield return null;
            var mission = Object.FindFirstObjectByType<MissionDirector>(); var player = mission.player;
            player.enabled = false; mission.Accept();
            yield return null;
            var panel = Resources.FindObjectsOfTypeAll<RectTransform>().FirstOrDefault(r => r.name == "Interaction panel" && r.gameObject.scene.IsValid());
            Assert.That(panel, Is.Not.Null, "HUD must create a named 'Interaction panel'.");
            Assert.That(panel.gameObject.activeInHierarchy, Is.False, "Hidden with no target.");

            var key = Object.FindObjectsByType<ThiefInteractable>(FindObjectsSortMode.None).First(i => i.kind == InteractionKind.Key && i.ItemId == "key");
            var controller = player.GetComponent<CharacterController>(); controller.enabled = false;
            player.transform.position = new Vector3(-1.8f, 0, 2); controller.enabled = true;
            player.view.transform.localPosition = Vector3.up * 1.65f;
            player.view.transform.LookAt(key.GetComponentInChildren<Renderer>().bounds.center);
            Physics.SyncTransforms();
            var find = typeof(ThiefPlayer).GetMethod("FindTarget", BindingFlags.NonPublic | BindingFlags.Instance);
            find.Invoke(player, null);
            Assert.That(player.Target, Is.SameAs(key), "Target must come from the real interaction ray.");
            yield return null;
            Assert.That(panel.gameObject.activeInHierarchy, Is.True, "Visible while targeting.");
            var text = panel.GetComponentInChildren<TextMeshProUGUI>(true).text;
            StringAssert.Contains(key.displayName, text); StringAssert.Contains("[F]", text);

            player.view.transform.rotation = Quaternion.LookRotation(Vector3.up);
            Physics.SyncTransforms();
            find.Invoke(player, null);
            Assert.That(player.Target, Is.Null);
            yield return null;
            Assert.That(panel.gameObject.activeInHierarchy, Is.False, "Hidden again once the target is cleared.");
        }

        static T Named<T>(string name) where T : Component =>
            Resources.FindObjectsOfTypeAll<T>().FirstOrDefault(c => c.name == name && c.gameObject.scene.IsValid());

        [UnityTest]
        public IEnumerator PauseScreenIsComposedOfNamedPartsAndFreezesTime()
        {
            Time.timeScale = 1;
            yield return SceneManager.LoadSceneAsync("ManorPrototype");
            yield return null;
            var mission = Object.FindFirstObjectByType<MissionDirector>();
            mission.player.enabled = false; mission.Accept();
            yield return null;
            var heading = Named<TextMeshProUGUI>("Pause heading"); var objective = Named<TextMeshProUGUI>("Pause objective");
            var resume = Named<TextMeshProUGUI>("Pause resume"); var restart = Named<TextMeshProUGUI>("Pause restart");
            Assert.That(heading && objective && resume && restart, Is.True, "Pause UI needs separate heading, objective, resume and restart parts.");
            Assert.That(heading.gameObject.activeInHierarchy, Is.False, "Hidden while not paused.");

            mission.TogglePause();
            yield return null;
            Assert.That(Time.timeScale, Is.EqualTo(0f));
            Assert.That(heading.gameObject.activeInHierarchy && objective.gameObject.activeInHierarchy && resume.gameObject.activeInHierarchy && restart.gameObject.activeInHierarchy, Is.True);
            Assert.That(objective.text, Is.EqualTo(mission.Objective));
            StringAssert.Contains("Esc", resume.text); StringAssert.Contains("R", restart.text);
            var a = new Vector3[4]; var b = new Vector3[4];
            resume.rectTransform.GetWorldCorners(a); restart.rectTransform.GetWorldCorners(b);
            Assert.That(b[1].y, Is.LessThanOrEqualTo(a[0].y), "Action rows must not overlap.");

            mission.TogglePause();
            yield return null;
            Assert.That(Time.timeScale, Is.EqualTo(1f));
            Assert.That(heading.gameObject.activeInHierarchy, Is.False, "Hidden again after resume.");
        }
    }
}
