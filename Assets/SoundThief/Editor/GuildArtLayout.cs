using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace SoundThief.Editor
{
    // Visual dressing is separate from the gameplay geometry and mission references.
    public static class GuildArtLayout
    {
        const string RootName = "Guild art — Dungeon Megapack";
        static Transform art;
        static Dictionary<string, string> prefabs;
        static Material wood, brass;

        [MenuItem("Sound Thief/Apply Medieval Guild Art")]
        public static void Apply()
        {
            var scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlaying || scene.path != GuildSceneBuilder.ScenePath)
                throw new InvalidOperationException("Open Guild in Edit Mode first.");
            prefabs = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Dungeon Megapack", "Assets/ThirdParty/Dungeon Megapack" })
                .Select(AssetDatabase.GUIDToAssetPath).GroupBy(System.IO.Path.GetFileNameWithoutExtension)
                .ToDictionary(g => g.Key, g => g.First());
            var root = scene.GetRootGameObjects().Single(g => g.name == "Guild — contract and optional practice").transform;
            var previous = scene.GetRootGameObjects().FirstOrDefault(g => g.name == RootName);
            if (previous) Object.DestroyImmediate(previous);
            art = new GameObject(RootName).transform;
            wood = Load("Old oak"); brass = Load("Warm brass");

            // Original floor, wall and table colliders stay in place; art has no duplicate mesh colliders.
            Hide(root.Find("Foundation"));
            for (int x = 0; x < 13; x++)
                for (int z = 0; z < 21; z++)
                    Fit("Floor_1_1A", $"Stone paving {x:00}-{z:00}",
                        new Vector3(-14 + x * 2, -0.058f, -18 + z * 2), new Vector3(2, 0.126f, 2));

            foreach (string n in new[] { "West wall", "East wall", "North wall", "South wall", "Rear partition right", "Rear outer wall", "Practice doorway lintel" })
            {
                var t = root.Find(n);
                if (!t) continue;
                Hide(t);
                bool alongZ = t.localScale.z > t.localScale.x;
                float length = alongZ ? t.localScale.z : t.localScale.x;
                int count = Mathf.CeilToInt(length / 4);
                float step = length / count;
                for (int i = 0; i < count; i++)
                {
                    var p = t.position + (alongZ ? Vector3.forward : Vector3.right) * (-length / 2 + step * (i + 0.5f));
                    Fit("Wall_2_1A", n + " masonry " + i, p,
                        alongZ ? new Vector3(0.4f, t.localScale.y, step) : new Vector3(step, t.localScale.y, 0.4f), alongZ ? 90 : 0);
                }
            }
            foreach (Transform t in root.Cast<Transform>().Where(t => t.name == "Guild pillar"))
            {
                Hide(t);
                Fit("Pilar_1_3A", "Carved stone pillar", t.position, new Vector3(0.85f, 3.6f, 0.85f));
            }
            // A timber roof makes the hall read as an interior, without changing walkable geometry.
            Box("Timber ceiling", new Vector3(-2, 4.28f, 2), new Vector3(26, 0.18f, 42), wood);
            for (int z = -18; z <= 22; z += 5)
                Box("Roof crossbeam", new Vector3(-2, 4.08f, z), new Vector3(26, 0.24f, 0.28f), wood);

            var table = root.Find("Contract table");
            Hide(table);
            Fit("Table_1_2", "Guild contract desk", table.position, table.localScale);
            var counter = root.Find("Right counter");
            Hide(counter);
            Fit("Table_2_1", "Quartermaster counter", counter.position, counter.localScale);
            Prop("Book_6", "Contract ledger", new Vector3(-3.65f, 0.91f, 1.1f), 12, 1);
            Prop("CandleHolder_1_1", "Desk candle", new Vector3(-0.3f, 0.91f, 1.35f));
            Prop("Coins_1", "Counter coin pile", new Vector3(6, 1.11f, 1.05f));
            Prop("Book_3_1", "Quartermaster ledger", new Vector3(8.5f, 1.11f, 1.1f), -20);

            // Furnished alcoves sit outside the central desk-to-exit route and rear practice doorway.
            foreach (float z in new[] { 7f, 14f })
            {
                var b = Fit("Table_2_1", "Common room table", new Vector3(6.5f, 0.44f, z), new Vector3(3, 0.88f, 1.4f));
                Solid(b);
                foreach (float x in new[] { 5.5f, 7.5f })
                {
                    var stool = Prop("Stool_1", "Common room seat", new Vector3(x, 0, z - 1.3f));
                    Solid(stool);
                }
                Prop("Cup_1_1", "Tankard", new Vector3(5.8f, 0.89f, z), 20);
                Prop("CandleHolder_1_2", "Common room candle", new Vector3(6.6f, 0.89f, z));
                Prop("Book_6", "Adventurer journal", new Vector3(7.3f, 0.89f, z), -25);
            }
            foreach (float z in new[] { 4.5f, 11f, 18f })
            {
                var cabinet = Fit("Wardrobe_3", "Guild archive cabinet", new Vector3(-13.7f, 1.1f, z), new Vector3(1.3f, 2.2f, 2.2f), 90);
                Solid(cabinet);
            }
            for (int i = 0; i < 5; i++)
            {
                var barrel = Prop("Barrel_2", "Supply barrel", new Vector3(8 + (i % 2) * 1.1f, 0, 18 + (i / 2) * 1.1f), i * 17, 1.25f);
                Solid(barrel);
            }
            Solid(Prop("Chest_1", "Guild supply chest", new Vector3(5, 0, 20.5f), 180, 2.5f));
            Solid(Prop("Chest_2", "Archive chest", new Vector3(-10, 0, 20.5f), 160, 2.3f));

            // Wall-mounted standards and torches repeat the source dungeon's visual rhythm.
            foreach (float z in new[] { 3f, 9f, 15f, 21f })
            {
                Banner(new Vector3(-14.65f, 3.75f, z), 90);
                Banner(new Vector3(10.65f, 3.75f, z), -90);
                Torch(new Vector3(-14.6f, 2.45f, z - 1.4f), 90);
                Torch(new Vector3(10.6f, 2.45f, z - 1.4f), -90);
            }
            foreach (float z in new[] { -6f, -12f, -17f })
            {
                Torch(new Vector3(10.6f, 2.5f, z), -90);
                Torch(new Vector3(-14.6f, 2.5f, z), 90);
            }
            Banner(new Vector3(-5.5f, 3.7f, 22.65f), 180);
            Banner(new Vector3(1.5f, 3.7f, 22.65f), 180);
            // Existing door still owns interaction, mission gating and return spawn.
            var door = root.Find("Departure door");
            if (door)
            {
                var r = door.GetComponent<Renderer>(); if (r) r.sharedMaterial = wood;
                for (int i = 0; i < 6; i++)
                    Box("Departure door timber", new Vector3(-3.48f + i * 0.59f, 1.6f, 22.46f), new Vector3(0.55f, 3.1f, 0.08f), wood);
                foreach (float y in new[] { 0.55f, 2.5f })
                    Box("Door metal strap", new Vector3(-2, y, 22.4f), new Vector3(3.35f, 0.12f, 0.06f), brass);
                Box("Door handle", new Vector3(-1.7f, 1.45f, 22.31f), new Vector3(0.08f, 0.3f, 0.1f), brass);
            }
            // Keep practice objects, occlusion cover and guard movement/collision exactly as authored.
            var practiceSign = root.Find("REAR PRACTICE");
            if (practiceSign) Box("Practice sign backing", practiceSign.position + Vector3.back * 0.1f, new Vector3(4.1f, 0.95f, 0.1f), wood);
            foreach (var light in root.GetComponentsInChildren<Light>())
                if (light.name == "Guild lamp") { light.color = new Color(1, 0.73f, 0.42f); light.intensity = 2; light.range = 9; }
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.3f, 0.32f, 0.38f);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            GuildUpperFloorLayout.Apply();
            Debug.Log("Guild medieval art applied; gameplay references and practice geometry preserved.");
        }

        static Material Load(string name) => AssetDatabase.LoadAssetAtPath<Material>("Assets/SoundThief/Generated/" + name + ".mat");
        static void Hide(Transform t) { if (t && t.TryGetComponent<Renderer>(out var r)) r.enabled = false; }
        static Bounds BoundsOf(GameObject g)
        {
            var renderers = g.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) throw new InvalidOperationException("No mesh: " + g.name);
            var b = renderers[0].bounds;
            foreach (var r in renderers.Skip(1)) b.Encapsulate(r.bounds);
            return b;
        }
        static GameObject Instance(string asset, string name, float yaw)
        {
            if (!prefabs.TryGetValue(asset, out string path)) throw new InvalidOperationException("Missing prefab " + asset);
            var g = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path), art);
            g.name = name; g.transform.position = Vector3.zero; g.transform.rotation = Quaternion.Euler(0, yaw, 0);
            foreach (var c in g.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            foreach (var t in g.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 2;
            return g;
        }
        static GameObject Fit(string asset, string name, Vector3 center, Vector3 size, float yaw = 0)
        {
            var g = Instance(asset, name, yaw); var b = BoundsOf(g);
            var factor = new Vector3(size.x / b.size.x, size.y / b.size.y, size.z / b.size.z);
            // Fit in world axes after rotation (wall and cabinet modules can be quarter-turned).
            if (Mathf.Abs(Mathf.Sin(yaw * Mathf.Deg2Rad)) > 0.99f) factor = new Vector3(factor.z, factor.y, factor.x);
            g.transform.localScale = Vector3.Scale(g.transform.localScale, factor);
            g.transform.position += center - BoundsOf(g).center;
            return g;
        }
        static GameObject Prop(string asset, string name, Vector3 bottom, float yaw = 0, float scale = 1)
        {
            var g = Instance(asset, name, yaw); g.transform.localScale *= scale;
            var b = BoundsOf(g); g.transform.position += bottom - new Vector3(b.center.x, b.min.y, b.center.z);
            return g;
        }
        static void Solid(GameObject g)
        {
            var b = BoundsOf(g);
            // Dedicated simple collider; render children remain on Ignore Raycast for F interactions.
            var c = new GameObject(g.name + " collision"); c.transform.SetParent(art); c.transform.position = b.center;
            c.layer = 8; c.AddComponent<BoxCollider>().size = b.size;
        }
        static void Banner(Vector3 top, float yaw)
        {
            var g = Prop("Sztandar1_3", "Guild wall standard", Vector3.zero, yaw, 1.25f);
            var b = BoundsOf(g); g.transform.position += top - new Vector3(b.center.x, b.max.y, b.center.z);
        }
        static void Torch(Vector3 p, float yaw)
        {
            var g = Instance("Torch", "Wall torch", yaw); g.transform.position = p;
            var light = new GameObject("Warm torch light").AddComponent<Light>(); light.transform.SetParent(g.transform);
            light.transform.position = p + Quaternion.Euler(0, yaw, 0) * new Vector3(0, 0.28f, 0.38f);
            light.type = LightType.Point; light.range = 6.5f; light.intensity = 2.5f;
            light.color = new Color(1, 0.65f, 0.3f); light.shadows = LightShadows.None;
            Prop("Candle_1", "Torch flame cap", light.transform.position, 0, 0.6f);
        }
        static void Box(string name, Vector3 p, Vector3 size, Material material)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube); g.name = name; g.transform.SetParent(art);
            g.transform.position = p; g.transform.localScale = size; g.layer = 2;
            Object.DestroyImmediate(g.GetComponent<Collider>()); g.GetComponent<Renderer>().sharedMaterial = material;
        }
    }
}
