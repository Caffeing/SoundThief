using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using UnityEngine.SceneManagement;
using TMPro;

namespace SoundThief.Editor
{
    public static class ManorSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/ManorPrototype.unity";
        static Material stone, floor, wood, brass, cyan, guardMaterial;
        static Transform root;
        [MenuItem("Sound Thief/Create Manor Prototype")]
        public static void Create()
        {
            if (!File.Exists("Assets/TextMesh Pro/Resources/TMP Settings.asset"))
            {
                TMP_PackageResourceImporter.ImportResources(true, false, false);
                Debug.Log("TMP essentials imported. Run Create Manor Prototype after the import finishes.");
                return;
            }
            // Never replace the user's current or unsaved scene.
            if (File.Exists(ScenePath)) { Debug.Log("Manor prototype already exists: " + ScenePath); return; }
            Directory.CreateDirectory("Assets/Scenes"); Directory.CreateDirectory("Assets/SoundThief/Generated");
            var previous = SceneManager.GetActiveScene();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            root = new GameObject("Manor — Tutorial Contract").transform;
            stone = Material("Midnight stone", new Color(0.09f, 0.13f, 0.21f));
            floor = Material("Slate floor", new Color(0.16f, 0.20f, 0.27f));
            wood = Material("Old oak", new Color(0.24f, 0.13f, 0.085f));
            brass = Material("Warm brass", new Color(0.8f, 0.48f, 0.12f));
            cyan = Material("Resonance", new Color(0.08f, 0.85f, 0.67f));
            guardMaterial = Material("Guard cloak", new Color(0.4f, 0.12f, 0.16f));
            var rules = AssetDatabase.LoadAssetAtPath<SoundRules>("Assets/SoundThief/Generated/PrototypeRules.asset");
            if (!rules) { rules = ScriptableObject.CreateInstance<SoundRules>(); AssetDatabase.CreateAsset(rules, "Assets/SoundThief/Generated/PrototypeRules.asset"); }
            var systems = new GameObject("Contract Systems");
            var mission = systems.AddComponent<MissionDirector>(); var world = systems.AddComponent<SoundWorld>();
            mission.rules = world.rules = rules; mission.World = world; mission.projectileMaterial = cyan;

            // Entry vestibule z 0..6; main hall z 6..22. A side aisle provides a safe return route.
            Box("Foundation", new Vector3(0, -0.25f, 10), new Vector3(18, 0.5f, 26), floor);
            Box("West wall", new Vector3(-9, 2, 10), new Vector3(0.4f, 4, 26), stone);
            Box("East wall", new Vector3(9, 2, 10), new Vector3(0.4f, 4, 26), stone);
            Box("North wall", new Vector3(0, 2, 23), new Vector3(18, 4, 0.4f), stone);
            Box("South wall", new Vector3(0, 2, -3), new Vector3(18, 4, 0.4f), stone);
            Box("Entry partition left", new Vector3(-5, 2, 6), new Vector3(8, 4, 0.4f), stone);
            Box("Entry partition right", new Vector3(5, 2, 6), new Vector3(8, 4, 0.4f), stone);
            Box("Door lintel", new Vector3(0, 3.6f, 6), new Vector3(2, 0.8f, 0.4f), stone);
            for (int i = 0; i < 3; i++)
            {
                float z = 10 + i * 5;
                Box("West pillar " + i, new Vector3(-5, 1.7f, z), new Vector3(0.8f, 3.4f, 0.8f), stone);
                Box("East pillar " + i, new Vector3(5, 1.7f, z), new Vector3(0.8f, 3.4f, 0.8f), stone);
                Box("Carpet " + i, new Vector3(0, 0.015f, z), new Vector3(3, 0.025f, 4), wood, false);
            }
            Box("Cover screen", new Vector3(2.5f, 1.3f, 16), new Vector3(4, 2.6f, 0.5f), wood);
            Box("Seal plinth", new Vector3(0, 0.6f, 21), new Vector3(1.3f, 1.2f, 1.3f), stone);
            Box("Key plinth", new Vector3(-1.8f, 0.5f, 3.5f), new Vector3(0.7f, 1, 0.7f), wood);

            Item("Key", new Vector3(-1.8f, 1.12f, 3.5f), new Vector3(0.35f, 0.12f, 0.15f), brass, InteractionKind.Key, "오래된 열쇠");
            var doorRoot = new GameObject("Door hinge"); doorRoot.transform.position = new Vector3(-1, 0, 6); doorRoot.transform.SetParent(root);
            var leaf = Box("Oak door", new Vector3(0, 1.5f, 6), new Vector3(1.95f, 3, 0.22f), wood);
            leaf.transform.SetParent(doorRoot.transform, true);
            var knob = Box("Lock", new Vector3(0.65f, 1.1f, 5.8f), new Vector3(0.18f, 0.3f, 0.15f), brass, false); knob.transform.SetParent(doorRoot.transform, true);
            var door = doorRoot.AddComponent<ThiefInteractable>(); door.kind = InteractionKind.Door; door.displayName = "저택의 잠긴 문";
            door.relation = doorRoot.AddComponent<SoundRelation>(); door.relation.firstMedium = "열쇠"; door.relation.secondMedium = "자물쇠"; door.relation.grade = 2;
            var obstacle = doorRoot.AddComponent<NavMeshObstacle>(); obstacle.shape = NavMeshObstacleShape.Box; obstacle.center = new Vector3(1, 1.5f, 0); obstacle.size = new Vector3(1.95f, 3, 0.35f); obstacle.carving = true;

            for (int i = 0; i < 2; i++)
            {
                var pos = i == 0 ? new Vector3(-2, 1, 9) : new Vector3(-5.8f, 1, 17.5f);
                Box("Resonator stand", pos - Vector3.up * 0.55f, new Vector3(0.65f, 0.9f, 0.65f), stone);
                var resonator = Item("Resonator " + (i + 1), pos, Vector3.one * 0.6f, cyan, InteractionKind.Resonator, "공명체 " + (i + 1));
                resonator.relation = resonator.gameObject.AddComponent<SoundRelation>();
                resonator.relation.firstMedium = "손"; resonator.relation.secondMedium = "공명체 " + (i + 1); resonator.relation.grade = i == 0 ? 1 : 3;
                resonator.emitter = resonator.gameObject.AddComponent<SoundEmitter>();
                Lamp(pos + Vector3.up, new Color(0.1f, 1, 0.8f), 4, 3);
            }
            Item("Family Seal", new Vector3(0, 1.45f, 21), new Vector3(0.5f, 0.45f, 0.5f), brass, InteractionKind.Treasure, "가문의 인장");
            Item("Exit marker", new Vector3(0, 0.5f, -1.8f), new Vector3(2, 1, 0.2f), cyan, InteractionKind.Exit, "저택 밖으로 탈출");

            // Bake static architecture before adding the door and character agents to the nav data.
            leaf.layer = 0;
            var surface = root.gameObject.AddComponent<NavMeshSurface>(); surface.collectObjects = CollectObjects.Children;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders; surface.layerMask = 1 << 8;
            surface.BuildNavMesh();
            if (surface.navMeshData) AssetDatabase.CreateAsset(surface.navMeshData, "Assets/SoundThief/Generated/ManorNavMesh.asset");
            leaf.layer = 8;

            var playerObject = new GameObject("Thief"); playerObject.transform.position = new Vector3(0, 0.05f, 2);
            var controller = playerObject.AddComponent<CharacterController>(); controller.height = 1.8f; controller.radius = 0.28f; controller.center = Vector3.up * 0.9f;
            var player = playerObject.AddComponent<ThiefPlayer>(); player.Mission = mission; mission.player = world.player = player;
            var cameraObject = new GameObject("First person view", typeof(Camera), typeof(AudioListener)); cameraObject.tag = "MainCamera";
            cameraObject.transform.SetParent(playerObject.transform); cameraObject.transform.localPosition = Vector3.up * 1.65f;
            var camera = cameraObject.GetComponent<Camera>(); camera.fieldOfView = 72; camera.nearClipPlane = 0.05f;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.025f, 0.035f, 0.07f); player.view = camera;
            var hud = new GameObject("HUD").AddComponent<ThiefHud>(); hud.mission = mission;
            var events = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
            var guardObj = new GameObject("Sleeping guard"); guardObj.transform.position = new Vector3(4, 0, 11.5f);
            var guardBody = GameObject.CreatePrimitive(PrimitiveType.Capsule); guardBody.name = "Guard body";
            guardBody.transform.SetParent(guardObj.transform, false); guardBody.transform.localPosition = Vector3.up;
            guardBody.GetComponent<Renderer>().sharedMaterial = guardMaterial;
            Object.DestroyImmediate(guardBody.GetComponent<Collider>());
            var agent = guardObj.AddComponent<NavMeshAgent>(); agent.radius = 0.35f; agent.height = 1.9f; agent.baseOffset = 0;
            var guard = guardObj.AddComponent<GuardAgent>(); guard.world = world;
            var label = new GameObject("Guard state").AddComponent<TextMeshPro>(); label.transform.SetParent(guardObj.transform);
            label.transform.localPosition = Vector3.up * 2.5f; label.fontSize = 3; label.alignment = TextAlignmentOptions.Center;
            label.rectTransform.sizeDelta = new Vector2(3, 1); guard.statusLabel = label;
            var sun = new GameObject("Moonlight").AddComponent<Light>(); sun.type = LightType.Directional; sun.transform.rotation = Quaternion.Euler(55, -35, 0); sun.color = new Color(0.5f, 0.65f, 1); sun.intensity = 1.1f;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat; RenderSettings.ambientLight = new Color(0.3f, 0.36f, 0.48f);
            for (int i = 0; i < 5; i++) { Lamp(new Vector3(-7, 2.8f, 2 + i * 4.5f), new Color(1, 0.65f, 0.3f), 4, 8); Lamp(new Vector3(7, 2.8f, 2 + i * 4.5f), new Color(1, 0.65f, 0.3f), 4, 8); }
            EditorSceneManager.SaveScene(scene, ScenePath);
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(ScenePath, true) };
            foreach (var entry in EditorBuildSettings.scenes) if (entry.path != ScenePath) scenes.Add(entry);
            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
            EditorSceneManager.CloseScene(scene, true);
            if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            Debug.Log("SOUND_THIEF_SCENE_READY: " + ScenePath);
        }
        static Material Material(string name, Color color)
        {
            string path = "Assets/SoundThief/Generated/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material) return material;
            material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name, color = color };
            AssetDatabase.CreateAsset(material, path); return material;
        }
        static GameObject Box(string name, Vector3 position, Vector3 scale, Material material, bool geometry = true)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube); obj.name = name; obj.transform.SetParent(root);
            obj.transform.position = position; obj.transform.localScale = scale; obj.GetComponent<Renderer>().sharedMaterial = material;
            obj.layer = geometry ? 8 : 0;
            if (!geometry) Object.DestroyImmediate(obj.GetComponent<Collider>());
            return obj;
        }
        static ThiefInteractable Item(string name, Vector3 position, Vector3 scale, Material material, InteractionKind kind, string label)
        {
            var obj = Box(name, position, scale, material); obj.layer = 0;
            var item = obj.AddComponent<ThiefInteractable>(); item.kind = kind; item.displayName = label; return item;
        }
        static void Lamp(Vector3 position, Color color, float intensity, float range)
        {
            var obj = new GameObject("Candle light"); obj.transform.SetParent(root); obj.transform.position = position;
            var light = obj.AddComponent<Light>(); light.type = LightType.Point; light.color = color; light.intensity = intensity; light.range = range;
        }
    }
}
