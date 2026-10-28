using System.Linq;
using TMPro;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace SoundThief.Editor
{
    // Apply in place: preserve the existing scene's object identities and references.
    public static class GuildRearLayout
    {
        [MenuItem("Sound Thief/Apply Rear Practice Layout")]
        public static void Apply()
        {
            var scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlaying || scene.path != GuildSceneBuilder.ScenePath)
                throw new System.InvalidOperationException("Open Guild in Edit Mode before applying the layout.");
            var root = scene.GetRootGameObjects().Single(o => o.name == "Guild — contract and optional practice").transform;
            var mission = scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<MissionDirector>()).Single();
            var practice = mission.practice;
            Material stone = Load("Midnight stone"), wood = Load("Old oak"), brass = Load("Warm brass");

            // All practice props, cover and labels must follow the same room transform.
            foreach (Transform child in root.Cast<Transform>().ToArray())
                if (child.name == "Practice reset" || child.name == "Blast cover wall"
                    || child.name.StartsWith("GRADE ") || child.name == "[F] RESET PRACTICE"
                    || child.name == "THROWING RANGE / WATCH THE GUARD")
                    child.SetParent(practice.practiceRoot, true);
            practice.practiceRoot.position = new Vector3(0, 0, -22);

            Set(root.Find("Foundation"), new Vector3(-2, -0.25f, 2), new Vector3(26, 0.5f, 42));
            Set(root.Find("West wall"), new Vector3(-15, 2, 2), new Vector3(0.4f, 4, 42));
            Set(root.Find("East wall"), new Vector3(11, 2, 2), new Vector3(0.4f, 4, 42));
            Set(root.Find("South wall"), new Vector3(-12.5f, 2, -3), new Vector3(5, 4, 0.4f));
            Box(root, "Rear partition right", new Vector3(2.5f, 2, -3), new Vector3(17, 4, 0.4f), stone);
            Box(root, "Practice doorway lintel", new Vector3(-8, 3.65f, -3), new Vector3(4, 0.7f, 0.4f), stone);
            Box(root, "Rear outer wall", new Vector3(-2, 2, -19), new Vector3(26, 4, 0.4f), stone);
            Box(root, "Contract table", new Vector3(-2, 0.45f, 1), new Vector3(4, 0.9f, 1.5f), wood);
            Box(root, "Right counter", new Vector3(7.7f, 0.55f, 1), new Vector3(5.8f, 1.1f, 1.5f), wood);
            // A floor marker labels the only doorway; it has no blocking collider.
            var threshold = Box(root, "Practice threshold", new Vector3(-8, 0.015f, -3), new Vector3(4, 0.025f, 0.7f), brass);
            var thresholdCollider = threshold.GetComponent<Collider>();
            if (thresholdCollider) Object.DestroyImmediate(thresholdCollider);
            threshold.layer = 0;

            Set(root.Find("Contract board"), new Vector3(-2, 1.5f, 0.8f), new Vector3(2.4f, 1.2f, 0.2f));
            MoveSign(root, "CONTRACT 01", new Vector3(-2, 1.8f, 0.67f), 0, 2.2f);
            MoveSign(root, "THE MANOR SEAL\n[F] ACCEPT", new Vector3(-2, 1.25f, 0.67f), 0, 1.7f);
            Set(root.Find("Departure door"), new Vector3(-2, 1.6f, 22.65f), new Vector3(3.5f, 3.2f, 0.3f));
            MoveSign(root, "TO THE MANOR\n[F] DEPART", new Vector3(-2, 2.1f, 22.45f), 0, 3.5f);
            Set(root.Find("Departure path"), new Vector3(-2, 0.012f, 10), new Vector3(3.6f, 0.02f, 24));
            MoveSign(root, "GUILD / SOUND THIEF", new Vector3(-2, 3.4f, 9), 0, 5);
            MoveSign(root, "NO TRAINING REQUIRED TO DEPART", new Vector3(-2, 3.5f, 18), 0, 2.5f);
            foreach (Transform pillar in root.Cast<Transform>().Where(t => t.name == "Guild pillar"))
                pillar.position = new Vector3(-12, pillar.position.y, pillar.position.z);
            var entranceSign = root.Find("OPTIONAL PRACTICE  <");
            if (entranceSign) entranceSign.name = "REAR PRACTICE";
            MoveSign(root, "REAR PRACTICE", new Vector3(-8, 3.25f, -2.72f), 180, 2.5f);
            var entranceText = root.Find("REAR PRACTICE").GetComponent<TextMeshPro>();
            entranceText.text = "PRACTICE\nAUTO RESET ON RETURN";
            mission.player.transform.position = new Vector3(-2, 0.05f, -1.5f);
            mission.player.transform.rotation = Quaternion.identity;

            var area = root.Find("Practice area");
            if (!area) { area = new GameObject("Practice area").transform; area.SetParent(root, false); }
            area.position = new Vector3(-2, 2, -11); area.gameObject.layer = 2;
            var volume = area.GetComponent<BoxCollider>(); if (!volume) volume = area.gameObject.AddComponent<BoxCollider>();
            volume.isTrigger = true; volume.size = new Vector3(25.6f, 6, 16);
            practice.practiceArea = volume;

            var surface = root.GetComponent<NavMeshSurface>();
            const string navPath = "Assets/SoundThief/Generated/GuildNavMesh.asset";
            var savedNav = AssetDatabase.LoadAssetAtPath<NavMeshData>(navPath);
            surface.BuildNavMesh();
            if (savedNav && surface.navMeshData != savedNav)
            {
                var baked = surface.navMeshData;
                surface.RemoveData(); EditorUtility.CopySerialized(baked, savedNav);
                surface.navMeshData = savedNav; surface.AddData();
                Object.DestroyImmediate(baked); EditorUtility.SetDirty(savedNav);
            }
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("SOUND_THIEF_REAR_PRACTICE_READY: lobby z > -3, practice z <= -3; automatic reset on return.");
        }
        static Material Load(string name) => AssetDatabase.LoadAssetAtPath<Material>("Assets/SoundThief/Generated/" + name + ".mat");
        static void Set(Transform target, Vector3 position, Vector3 scale) { target.position = position; target.localScale = scale; }
        static GameObject Box(Transform root, string name, Vector3 position, Vector3 scale, Material material)
        {
            var existing = root.Find(name);
            var obj = existing ? existing.gameObject : GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = name; obj.transform.SetParent(root, true); Set(obj.transform, position, scale);
            obj.layer = 8; obj.GetComponent<Renderer>().sharedMaterial = material; return obj;
        }
        static void MoveSign(Transform root, string name, Vector3 position, float yaw, float fontSize)
        {
            // Names may contain '/', which Transform.Find treats as a hierarchy path.
            var sign = root.Cast<Transform>().Single(t => t.name == name);
            sign.position = position; sign.rotation = Quaternion.Euler(0, yaw, 0);
            sign.GetComponent<TextMeshPro>().fontSize = fontSize;
        }
    }
}
