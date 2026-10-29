using System.Collections.Generic;
using System.IO;
using TMPro;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace SoundThief.Editor
{
    public static class GuildSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/Guild.unity";
        static Transform root;
        static Material stone, floor, wood, brass, cyan;

        [MenuItem("Sound Thief/Create Guild and Practice")]
        public static void Create()
        {
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play Mode before creating the guild.");
            if (File.Exists(ScenePath)) { Debug.Log("Guild already exists: " + ScenePath); return; }
            var rules = AssetDatabase.LoadAssetAtPath<SoundRules>("Assets/SoundThief/Generated/PrototypeRules.asset");
            if (!rules) throw new System.InvalidOperationException("Create the Manor prototype first.");
            stone = Load("Midnight stone"); floor = Load("Slate floor"); wood = Load("Old oak");
            brass = Load("Warm brass"); cyan = Load("Resonance");
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                root = new GameObject("Guild — contract and optional practice").transform;
                Box("Foundation", new Vector3(-2, -0.25f, 10), new Vector3(26, 0.5f, 26), floor);
                Box("West wall", new Vector3(-15, 2, 10), new Vector3(0.4f, 4, 26), stone);
                Box("East wall", new Vector3(11, 2, 10), new Vector3(0.4f, 4, 26), stone);
                Box("North wall", new Vector3(-2, 2, 23), new Vector3(26, 4, 0.4f), stone);
                Box("South wall", new Vector3(-2, 2, -3), new Vector3(26, 4, 0.4f), stone);
                Box("Departure path", new Vector3(2, 0.012f, 10), new Vector3(3.6f, 0.02f, 24), wood, false);
                for (int i = 0; i < 4; i++)
                {
                    Box("Guild pillar", new Vector3(-2.5f, 1.8f, 3 + i * 5), new Vector3(0.6f, 3.6f, 0.6f), stone);
                    Lamp(new Vector3(7, 2.7f, 2 + i * 6), new Color(1, 0.7f, 0.4f));
                    Lamp(new Vector3(-11, 2.7f, 2 + i * 6), new Color(0.3f, 0.9f, 1));
                }

                var systems = new GameObject("Guild Systems");
                var mission = systems.AddComponent<MissionDirector>();
                var world = systems.AddComponent<SoundWorld>();
                mission.isGuild = true; mission.rules = world.rules = rules;
                mission.World = world; mission.projectileMaterial = cyan;
                var practice = systems.AddComponent<GuildPractice>();
                mission.practice = practice; practice.mission = mission;
                var practiceRoot = new GameObject("Optional Practice").transform; practiceRoot.SetParent(root);
                practice.practiceRoot = practiceRoot;

                Item("Contract board", new Vector3(2, 1.35f, 5), new Vector3(2.4f, 2.2f, 0.25f), brass,
                    InteractionKind.ContractBoard, "의뢰 게시판 · 푸른 저택의 인장 [수락]");
                Sign("CONTRACT 01", new Vector3(2, 2.1f, 4.82f), 0, 4);
                Sign("THE MANOR SEAL\n[F] ACCEPT", new Vector3(2, 1.25f, 4.82f), 0, 2.5f);
                Item("Departure door", new Vector3(2, 1.6f, 22.65f), new Vector3(3.5f, 3.2f, 0.3f), cyan,
                    InteractionKind.GuildExit, "길드 출구 · 스테이지 1 출발");
                Sign("TO THE MANOR\n[F] DEPART", new Vector3(2, 2.1f, 22.45f), 0, 3.5f);
                Sign("GUILD / SOUND THIEF", new Vector3(2, 3.4f, 9), 0, 5);
                Sign("OPTIONAL PRACTICE  <", new Vector3(-7.6f, 3.2f, 6), 0, 4);
                Sign("NO TRAINING REQUIRED TO DEPART", new Vector3(2, 3.5f, 18), 0, 2.5f);

                for (int i = 0; i < 3; i++)
                {
                    Vector3 pos = new Vector3(-7, 1.15f, 7 + i * 4);
                    Box("Practice stand", pos - Vector3.up * 0.65f, new Vector3(0.9f, 1, 0.9f), stone).transform.SetParent(practiceRoot, true);
                    var item = Item("Practice resonator " + (i + 1), pos, Vector3.one * 0.65f, cyan,
                        InteractionKind.Resonator, "연습 공명체 " + (i + 1));
                    item.transform.SetParent(practiceRoot, true);
                    item.relation = item.gameObject.AddComponent<SoundRelation>(); item.relation.grade = i + 1;
                    item.relation.firstMedium = "손"; item.relation.secondMedium = "연습 공명체 " + (i + 1);
                    item.emitter = item.gameObject.AddComponent<SoundEmitter>();
                    Sign("GRADE " + (i + 1) + " / [E] EXTRACT / [Q] INJECT", pos + new Vector3(0, 0.9f, 0), 0, 2.1f);
                }
                Item("Practice reset", new Vector3(-4.5f, 0.75f, 4), new Vector3(1, 1.5f, 0.6f), brass,
                    InteractionKind.PracticeReset, "연습 초기화 · 의뢰 수락 유지");
                Sign("[F] RESET PRACTICE", new Vector3(-4.5f, 1.9f, 3.65f), 0, 2.3f);
                Box("Blast cover wall", new Vector3(-10, 1.4f, 17), new Vector3(0.4f, 2.8f, 4), stone);
                Sign("THROWING RANGE / WATCH THE GUARD", new Vector3(-8.5f, 3.2f, 19), 0, 2.5f);

                var surface = root.gameObject.AddComponent<NavMeshSurface>();
                surface.collectObjects = CollectObjects.Children; surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
                surface.layerMask = 1 << 8; surface.BuildNavMesh();
                if (surface.navMeshData) AssetDatabase.CreateAsset(surface.navMeshData, "Assets/SoundThief/Generated/GuildNavMesh.asset");

                var playerObject = new GameObject("Thief"); playerObject.transform.position = new Vector3(2, 0.05f, 1);
                var controller = playerObject.AddComponent<CharacterController>();
                controller.height = 1.8f; controller.radius = 0.28f; controller.center = Vector3.up * 0.9f;
                var player = playerObject.AddComponent<ThiefPlayer>(); player.Mission = mission; mission.player = world.player = player;
                var cameraObject = new GameObject("First person view", typeof(Camera), typeof(AudioListener)); cameraObject.tag = "MainCamera";
                cameraObject.transform.SetParent(playerObject.transform, false); cameraObject.transform.localPosition = Vector3.up * 1.65f;
                player.view = cameraObject.GetComponent<Camera>(); player.view.fieldOfView = 72; player.view.nearClipPlane = 0.05f;
                player.view.clearFlags = CameraClearFlags.SolidColor; player.view.backgroundColor = new Color(0.025f, 0.035f, 0.07f);
                var hud = new GameObject("HUD").AddComponent<ThiefHud>(); hud.mission = mission;
                new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));

                var guardObject = new GameObject("Practice guard"); guardObject.transform.SetParent(practiceRoot, false);
                guardObject.transform.position = new Vector3(-12, 0, 17);
                var body = GameObject.CreatePrimitive(PrimitiveType.Capsule); body.transform.SetParent(guardObject.transform, false);
                body.transform.localPosition = Vector3.up; body.GetComponent<Renderer>().sharedMaterial = Load("Guard cloak");
                Object.DestroyImmediate(body.GetComponent<Collider>());
                var agent = guardObject.AddComponent<NavMeshAgent>(); agent.radius = 0.35f; agent.height = 1.9f;
                var guard = guardObject.AddComponent<GuardAgent>(); guard.world = world;
                var label = new GameObject("Guard state").AddComponent<TextMeshPro>();
                label.transform.SetParent(guardObject.transform, false); label.transform.localPosition = Vector3.up * 2.5f;
                label.fontSize = 3; label.alignment = TextAlignmentOptions.Center; label.rectTransform.sizeDelta = new Vector2(3, 1); guard.statusLabel = label;

                var sun = new GameObject("Guild daylight").AddComponent<Light>(); sun.type = LightType.Directional;
                sun.transform.rotation = Quaternion.Euler(55, -35, 0); sun.color = new Color(0.7f, 0.8f, 1); sun.intensity = 1.2f;
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = new Color(0.4f, 0.44f, 0.5f);
                EditorSceneManager.SaveScene(scene, ScenePath);
                GuildRearLayout.Apply();
                var scenes = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(ScenePath, true) };
                foreach (var entry in EditorBuildSettings.scenes) if (entry.path != ScenePath) scenes.Add(entry);
                EditorBuildSettings.scenes = scenes.ToArray(); AssetDatabase.SaveAssets();
                Debug.Log("SOUND_THIEF_GUILD_READY: " + ScenePath);
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
        }
        static Material Load(string name) => AssetDatabase.LoadAssetAtPath<Material>("Assets/SoundThief/Generated/" + name + ".mat");
        static GameObject Box(string name, Vector3 pos, Vector3 size, Material material, bool geometry = true)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube); obj.name = name; obj.transform.SetParent(root);
            obj.transform.position = pos; obj.transform.localScale = size; obj.GetComponent<Renderer>().sharedMaterial = material;
            obj.layer = geometry ? 8 : 0; if (!geometry) Object.DestroyImmediate(obj.GetComponent<Collider>()); return obj;
        }
        static ThiefInteractable Item(string name, Vector3 pos, Vector3 size, Material material, InteractionKind kind, string label)
        {
            var obj = Box(name, pos, size, material); obj.layer = 0;
            var item = obj.AddComponent<ThiefInteractable>(); item.kind = kind; item.displayName = label; return item;
        }
        static void Sign(string value, Vector3 pos, float yaw, float size)
        {
            var text = new GameObject(value).AddComponent<TextMeshPro>(); text.transform.SetParent(root);
            text.transform.position = pos; text.transform.rotation = Quaternion.Euler(0, yaw, 0);
            text.text = value; text.fontSize = size; text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(0.85f, 0.97f, 1); text.rectTransform.sizeDelta = new Vector2(8, 1.5f);
        }
        static void Lamp(Vector3 pos, Color color)
        {
            var light = new GameObject("Guild lamp").AddComponent<Light>(); light.transform.SetParent(root);
            light.transform.position = pos; light.type = LightType.Point; light.color = color; light.range = 9; light.intensity = 5;
        }
    }
}
