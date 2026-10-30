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
    public sealed class UiCompositionTests
    {
        static Rect Box(string name)
        {
            var rt = Resources.FindObjectsOfTypeAll<RectTransform>().FirstOrDefault(r => r.name == name && r.gameObject.scene.IsValid());
            Assert.That(rt, Is.Not.Null, "Missing UI part '" + name + "'.");
            Assert.That(rt.gameObject.activeInHierarchy, Is.True, "'" + name + "' must be visible.");
            var c = new Vector3[4]; rt.GetWorldCorners(c);
            float x0 = c.Min(v => v.x), x1 = c.Max(v => v.x), y0 = c.Min(v => v.y), y1 = c.Max(v => v.y);
            return Rect.MinMaxRect(x0, y0, x1, y1);
        }
        static string Text(string name) => Resources.FindObjectsOfTypeAll<TextMeshProUGUI>().First(t => t.name == name && t.gameObject.scene.IsValid()).text;
        static void InScreen(Rect r, string what) =>
            Assert.That(r.xMin >= -1 && r.yMin >= -1 && r.xMax <= Screen.width + 1 && r.yMax <= Screen.height + 1, Is.True, what + " must stay on screen: " + r);

        [UnityTest]
        public IEnumerator StatusHudHasHierarchyAndTimerSemantics()
        {
            Time.timeScale = 1;
            yield return SceneManager.LoadSceneAsync("ManorPrototype");
            yield return null;
            var mission = Object.FindFirstObjectByType<MissionDirector>(); var player = mission.player;
            player.enabled = false; mission.Accept();
            typeof(ThiefPlayer).GetProperty("Held").GetSetMethod(true).Invoke(player, new object[] { new SoundCharge(null, 2, Time.time + 4f) });
            yield return null;
            var heading = Box("Objective heading"); var objective = Box("Objective text");
            Assert.That(heading.yMin, Is.GreaterThanOrEqualTo(objective.yMax - 1), "Objective heading sits above its text.");
            Assert.That(Text("Objective text"), Is.EqualTo(mission.Objective));
            var status = Box("Sound status"); var seconds = Box("Sound seconds"); var timer = Box("Sound timer"); var items = Box("Item inventory");
            Assert.That(seconds.xMin, Is.GreaterThanOrEqualTo(status.xMax - 1), "Seconds readout sits right of the status label.");
            Assert.That(timer.yMax, Is.LessThanOrEqualTo(Mathf.Min(status.yMin, seconds.yMin) + 1), "Timer track under the readouts.");
            Assert.That(items.yMax, Is.LessThanOrEqualTo(timer.yMin + 1), "Inventory under the timer.");
            foreach (var (r, n) in new[] { (heading, "heading"), (objective, "objective"), (status, "status"), (seconds, "seconds"), (timer, "timer"), (items, "items") }) InScreen(r, n);
            StringAssert.Contains("2등급", Text("Sound status")); StringAssert.EndsWith("초", Text("Sound seconds"));
            var fill = Resources.FindObjectsOfTypeAll<RectTransform>().First(r => r.name == "Time remaining" && r.gameObject.scene.IsValid());
            Assert.That(fill.anchorMax.x, Is.InRange(0.05f, 1f), "Timer fill still tracks remaining hold time.");
        }

        [UnityTest]
        public IEnumerator GuildDeskHasAlignedHierarchyAndKeepsControls()
        {
            Time.timeScale = 1;
            typeof(MissionDirector).GetMethod("ResetSession", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
            yield return SceneManager.LoadSceneAsync("Guild"); yield return null;
            var mission = Object.FindFirstObjectByType<MissionDirector>();
            foreach (var guard in mission.World.Guards) guard.enabled = false;
            Assert.That(mission.contractTable.Open(), Is.True);
            yield return null; yield return null;
            var heading = Box("Desk heading"); var count = Box("Desk page count"); var paper = Box("ActiveContract"); var memo = Box("BriefingSheet");
            var hints = Box("Desk hints"); var note = Box("Desk message");
            float tol = Screen.width * 0.02f;
            Assert.That(Mathf.Abs(heading.xMin - paper.xMin), Is.LessThan(tol), "Heading aligns with the contract's left edge.");
            Assert.That(Mathf.Abs(count.xMax - memo.xMax), Is.LessThan(tol), "Page count aligns with the memo's right edge.");
            Assert.That(heading.yMin, Is.GreaterThanOrEqualTo(paper.yMax - 1), "Heading above the contract.");
            Assert.That(memo.xMin, Is.GreaterThanOrEqualTo(paper.xMax), "Memo beside, not over, the contract.");
            Assert.That(hints.yMax, Is.LessThanOrEqualTo(paper.yMin + 1), "Hints below the contract."); Assert.That(note.yMax, Is.LessThanOrEqualTo(hints.yMin + 1), "Message below hints.");
            Assert.That(paper.width, Is.GreaterThan(memo.width * 1.25f), "The contract dominates the memo.");
            foreach (var (r, n) in new[] { (heading, "heading"), (count, "count"), (paper, "paper"), (memo, "memo"), (hints, "hints"), (note, "message") }) InScreen(r, n);
            var h = Text("Desk hints"); StringAssert.Contains("[F / Enter] 수락", h); StringAssert.Contains("[ESC] 나가기", h); StringAssert.Contains("←", h); StringAssert.Contains("→", h);
        }
    }
}
