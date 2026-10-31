#if DEVELOPMENT_BUILD
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace SoundThief {
public sealed class BuildInputSmokeProbe : MonoBehaviour {
int frame; ThiefPlayer player; float before;
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
static void Boot() {
if (!System.Environment.GetCommandLineArgs().Contains("-sound-thief-smoke")) return;
Application.runInBackground = true;
new GameObject("Build input smoke probe").AddComponent<BuildInputSmokeProbe>();
}
void Update() {
frame++;
if (frame == 20) {
player = Object.FindFirstObjectByType<ThiefPlayer>();
Debug.Log("SMOKE material=" + (Resources.Load<Material>("SoundConnection") != null) + " player=" + (player != null));
if (!player || Mouse.current == null) { Debug.LogError("SMOKE no player/mouse"); Application.Quit(1); return; }
before = player.transform.eulerAngles.y;
Cursor.lockState = CursorLockMode.Locked;
InputSystem.onBeforeUpdate += Inject;
}
if (frame == 23) {
InputSystem.onBeforeUpdate -= Inject;
bool moved = player && Mathf.Abs(Mathf.DeltaAngle(before, player.transform.eulerAngles.y)) > .1f;
var agents = Object.FindObjectsByType<UnityEngine.AI.NavMeshAgent>(FindObjectsSortMode.None);
Debug.Log("SMOKE lookMoved=" + moved + " cursor=" + Cursor.lockState + " navmesh=" + agents.Count(a=>a.isOnNavMesh) + "/" + agents.Length);
Application.Quit(moved ? 0 : 1);
}
}
void Inject() { if (Mouse.current != null) InputSystem.QueueDeltaStateEvent(Mouse.current.delta, new Vector2(30, 0)); }
}}
#endif

