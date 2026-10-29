using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace SoundThief.Editor
{
    public static class ManorSetDressing
    {
        static Transform root;
        static int bookVariant;
        static string Asset(string filename)
        {
            var paths = AssetDatabase.GetAllAssetPaths().Where(p => p.EndsWith("/" + filename) && p.Contains("/Prefabs/")).ToArray();
            if (paths.Length != 1) throw new InvalidOperationException("Expected one asset: " + filename);
            return paths[0];
        }
        static GameObject Place(string filename, string name, Vector3 center, Vector3 size, float angle = 0, bool solid = false)
        {
            var obj = ManorAssetPass.Fit(Asset(filename), name, root, new Bounds(center, size), Quaternion.Euler(0, angle, 0));
            if (solid) foreach (var filter in obj.GetComponentsInChildren<MeshFilter>().Where(f => f.GetComponent<Renderer>().enabled))
            {
                filter.gameObject.layer = 8;
                var collider = filter.gameObject.AddComponent<MeshCollider>(); collider.sharedMesh = filter.sharedMesh;
            }
            return obj;
        }
        static void BookStack(Vector3 top, float angle, int count = 2)
        {
            for (int i = 0; i < count; i++)
                Place("EA03_Items_House_Book_01" + (i % 2 == 0 ? "c" : "d") + "_PRE.prefab", "펼친 읽던 책", top + Vector3.up * (.035f + i * .065f), new Vector3(.29f, .06f, .38f), angle + i * 7);
        }
        static void Books(GameObject shelf, bool alongZ)
        {
            var b = ManorAssetPass.Bounds(shelf);
            // Locate the actual upward-facing shelf surfaces instead of assuming matching shelf heights between kits.
            var planes = new Dictionary<int, float>();
            foreach (var f in shelf.GetComponentsInChildren<MeshFilter>().Where(f => f.GetComponent<Renderer>().enabled))
            {
                var vertices = f.sharedMesh.vertices; var triangles = f.sharedMesh.triangles;
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    var a = f.transform.TransformPoint(vertices[triangles[i]]); var c = f.transform.TransformPoint(vertices[triangles[i + 1]]); var d = f.transform.TransformPoint(vertices[triangles[i + 2]]);
                    var cross = Vector3.Cross(c - a, d - a);
                    if (cross.normalized.y < .95f) continue;
                    int y = Mathf.RoundToInt((a.y + c.y + d.y) / 3 * 50);
                    planes[y] = (planes.TryGetValue(y, out var area) ? area : 0) + cross.magnitude * .5f;
                }
            }
            var levels = planes.Where(p => p.Value > .06f && p.Key / 50f > .12f).Select(p => p.Key / 50f).OrderBy(y => y).ToArray();
            var grouped = new List<float>();
            foreach (var y in levels) if (grouped.Count == 0 || y - grouped.Last() > .12f) grouped.Add(y); else grouped[grouped.Count - 1] = y;
            foreach (var y in grouped.Where(y => y < b.max.y - .1f).Take(4))
            {
                int variant = bookVariant++;
                float width = Mathf.Min(.62f, (alongZ ? b.size.z : b.size.x) * .38f);
                var offset = (alongZ ? Vector3.forward : Vector3.right) * (variant % 2 == 0 ? -.22f : .22f);
                var center = new Vector3(b.center.x, y + .165f, b.center.z) + offset;
                string file = "EA_Assortment_Books_01" + new[] { "a", "b", "c", "d" }[variant % 4] + "_PRE.prefab";
                Place(file, "다양한 책 묶음", center, alongZ ? new Vector3(.23f, .30f, width) : new Vector3(width, .30f, .23f), alongZ ? 90 : 0);
            }
        }
        static void Shelf(string filename, string name, Vector3 center, Vector3 size, float angle)
        {
            Books(Place(filename, name, center, size, angle, true), Mathf.Abs(size.z) > Mathf.Abs(size.x));
        }
        internal static void PopulateCoverShelf(GameObject shelf, Transform parent)
        {
            root = parent;
            Books(shelf, false);
        }
        static void Seating(Vector3 center, float angle, string name)
        {
            bool across = Mathf.Abs(Mathf.Sin(angle * Mathf.Deg2Rad)) > .5f;
            Place("EA03_Prop_Town_Bench_01a_PRE.prefab", name + " upholstered bench", center + Vector3.up * .24f, across ? new Vector3(2.1f, .48f, .64f) : new Vector3(.64f, .48f, 2.1f), angle, true);
            Place("EA03_Prop_Pillow_01a_PRE.prefab", name + " cushion", center + Vector3.up * .54f, new Vector3(.42f, .13f, .40f), angle + 8);
        }

        public static string RepairCarvedBookcases()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop play mode first.");
            root = GameObject.Find("생활 가구와 소품").transform;
            foreach (string name in new[] { "서재 조각 책장 남쪽", "서재 조각 책장 북쪽" })
            {
                var old = GameObject.Find(name); var b = ManorAssetPass.Bounds(old);
                foreach (var t in root.Cast<Transform>().Where(t => t.name == "다양한 책 묶음").ToArray())
                {
                    var books = ManorAssetPass.Bounds(t.gameObject);
                    if (Mathf.Abs(books.center.x - b.center.x) < .5f && Mathf.Abs(books.center.z - b.center.z) < .85f) Object.DestroyImmediate(t.gameObject);
                }
                Object.DestroyImmediate(old);
                Shelf("EA03_Prop_Town_BookCase_01a_PRE.prefab", name, b.center, b.size, 0);
            }
            var scene = SceneManager.GetActiveScene(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            return "Rebuilt carved shelving using its true width axis and supported book rows.";
        }
        public static string Apply()
        {
            var scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlaying || scene.path != "Assets/Scenes/ManorPrototype.unity" || scene.isDirty) throw new InvalidOperationException("Open the saved manor in edit mode.");
            if (GameObject.Find("생활 가구와 소품")) throw new InvalidOperationException("Dressing already installed.");
            Directory.CreateDirectory("Logs/AssetAudit");
            const string backup = "Logs/AssetAudit/Manor-before-varied-furnishing.unity";
            if (!File.Exists(backup)) File.Copy(scene.path, backup, false);
            else if (!File.ReadAllBytes(scene.path).SequenceEqual(File.ReadAllBytes(backup))) throw new InvalidOperationException("Preserve the existing furnishing backup before another pass.");
            var manor = GameObject.Find("03 저택 구조").transform;
            root = new GameObject("생활 가구와 소품").transform; root.SetParent(manor, false);
            foreach (var t in scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<Transform>(true)).Where(t => t.name.StartsWith("Woven rug · Carpet "))) t.gameObject.SetActive(false);
            var runner = ManorAssetPass.Fit("Assets/ThirdParty/Kenney/Furniture/rugRectangle.fbx", "중앙 홀 긴 융단", root, new Bounds(new Vector3(0, .025f, 15), new Vector3(2.9f, .03f, 15)), Quaternion.Euler(0, 90, 0));
            foreach (var renderer in runner.GetComponentsInChildren<Renderer>()) renderer.sharedMaterials = renderer.sharedMaterials.Select(m => AssetDatabase.LoadAssetAtPath<Material>("Assets/SoundThief/Generated/" + (m.name.Contains("Darker") ? "Manor carpet gold border.mat" : "Manor woven burgundy.mat"))).ToArray();
            foreach (var t in manor.GetComponentsInChildren<Transform>(true).Where(t => t.name == "책장 외형")) t.gameObject.SetActive(false);
            var previousBooks = GameObject.Find("서재 도서 통합 외형"); if (previousBooks) previousBooks.SetActive(false);
            foreach (string name in new[] { "서재 책장 북쪽", "서재 책장 남쪽", "서재 벽감 책장" })
            {
                var obj = GameObject.Find(name); if (obj) foreach (var collider in obj.GetComponents<Collider>()) collider.enabled = false;
            }
            bookVariant = 0;
            Shelf("EA03_Prop_Town_BookCase_01a_PRE.prefab", "서재 조각 책장 남쪽", new Vector3(-15.35f, 1.23f, 8.9f), new Vector3(.65f, 2.46f, 1.55f), 0);
            Shelf("Wardrobe_2.prefab", "서재 문서 보관장", new Vector3(-15.35f, 1.04f, 11.05f), new Vector3(.66f, 2.08f, 1.75f), 90);
            Shelf("EA03_Prop_House_Shelf_02c_PRE.prefab", "서재 좁은 벽 선반", new Vector3(-12.5f, 1.35f, 23.55f), new Vector3(1.35f, 2.7f, .5f), 0);
            Shelf("Wardrobe_1.prefab", "서재 낮은 자료장", new Vector3(-15.35f, .72f, 17.7f), new Vector3(.7f, 1.44f, 1.8f), 90);
            Shelf("EA03_Prop_Town_BookCase_01a_PRE.prefab", "서재 조각 책장 북쪽", new Vector3(-15.35f, 1.23f, 19.95f), new Vector3(.65f, 2.46f, 1.55f), 0);
            Shelf("Wardrobe_2.prefab", "자료실 벽감 수납장", new Vector3(-21.25f, 1.15f, 17.35f), new Vector3(.72f, 2.3f, 1.8f), 90);
            Shelf("Wardrobe_1.prefab", "자료실 벽감 낮은 수납장", new Vector3(-21.25f, .68f, 19.5f), new Vector3(.74f, 1.36f, 1.75f), 90);
            Shelf("EA03_Prop_House_Shelf_01a_PRE.prefab", "자료실 벽감 개방형 선반", new Vector3(-21.35f, 1.15f, 21.7f), new Vector3(.7f, 2.3f, 2.0f), 90);
            Place("EA03_Prop_Village_Ladder_01a_PRE.prefab", "사다리 외형", new Vector3(-21.2f, 1.35f, 23.1f), new Vector3(.46f, 2.7f, .65f), 90, true);
            BookStack(new Vector3(-21.25f, 1.36f, 19.65f), 82, 3);
            BookStack(new Vector3(-15.35f, 1.44f, 17.85f), 95, 2);
            Place("Chair_2.prefab", "사서 책상 의자", new Vector3(-19, .58f, 22.25f), new Vector3(.65f, 1.16f, .68f), 180, true);
            Place("EA03_Prop_Container_Chest_01b_PRE.prefab", "서재 문서함", new Vector3(-17.3f, .4f, 23.25f), new Vector3(1.5f, .8f, .75f), 0, true);
            BookStack(new Vector3(-17.3f, .80f, 23.25f), 12, 2);

            Seating(new Vector3(-6, 0, 3.4f), 90, "Foyer waiting");
            Place("EA03_Prop_Container_Chest_01a_PRE.prefab", "현관 여행 상자", new Vector3(-8.4f, .42f, 3.45f), new Vector3(1.45f, .84f, .74f), 0, true);
            Seating(new Vector3(5.8f, 0, 3.4f), 90, "Foyer reading");
            Place("EA03_Prop_Tabble_01a_PRE.prefab", "현관 보조 탁자", new Vector3(7.6f, .4f, 3.45f), new Vector3(.72f, .8f, .72f), 0, true);
            BookStack(new Vector3(7.6f, .8f, 3.45f), -18, 2);
            Seating(new Vector3(-8.15f, 0, 14.8f), 0, "West gallery");
            Place("EA03_Prop_Tabble_01a_PRE.prefab", "서쪽 복도 보조 탁자", new Vector3(-8.15f, .40f, 16.55f), new Vector3(.65f, .8f, .65f), 0, true);
            BookStack(new Vector3(-8.15f, .8f, 16.55f), 15, 2);
            Seating(new Vector3(8.15f, 0, 22.4f), 0, "East gallery");
            Place("EA03_Prop_Container_Chest_01b_PRE.prefab", "북쪽 복도 직물 보관함", new Vector3(-5.8f, .45f, 27.6f), new Vector3(2.2f, .9f, .82f), 0, true);
            Place("EA03_Prop_Pillow_01a_PRE.prefab", "북쪽 복도 접힌 천", new Vector3(-5.65f, .99f, 27.6f), new Vector3(.8f, .18f, .5f), 6);
            Seating(new Vector3(4.8f, 0, 27.6f), 90, "North gallery");

            Place("Wardrobe_1.prefab", "식당 북쪽 배식장", new Vector3(12.8f, .65f, 23.35f), new Vector3(3.0f, 1.3f, .72f), 180, true);
            Place("Wardrobe_2.prefab", "식당 벽감 식기 수납장", new Vector3(21.1f, .8f, 12.5f), new Vector3(.7f, 1.6f, 2.1f), -90, true);
            for (int i = 0; i < 3; i++)
            {
                Place("EA03_Items_House_Plate_01a_PRE.prefab", "포갠 식기 접시", new Vector3(12.1f, 1.34f + i * .035f, 23.3f), new Vector3(.32f, .045f, .32f), i * 8);
                Place("EA03_Items_House_Cup_01a_PRE.prefab", "배식 잔", new Vector3(13.2f + i * .22f, 1.39f, 23.3f), new Vector3(.14f, .18f, .14f), 10 + i * 25);
            }
            Place("EA03_Items_House_Pot_01a_PRE.prefab", "배식 도기 항아리", new Vector3(21.1f, 1.77f, 12.9f), new Vector3(.30f, .34f, .30f), 20);
            Place("EA03_Items_House_Bottle_01a_PRE.prefab", "배식 포도주 병", new Vector3(21.1f, 1.79f, 12.25f), new Vector3(.15f, .38f, .15f), -8);
            var surface = manor.GetComponent<NavMeshSurface>();
            var doorLayers = Object.FindObjectsByType<ThiefInteractable>(FindObjectsSortMode.None).Where(t => t.kind == InteractionKind.Door).SelectMany(t => t.GetComponentsInChildren<Collider>(true)).Select(c => c.gameObject).Distinct().ToDictionary(o => o, o => o.layer);
            try
            {
                foreach (var entry in doorLayers) entry.Key.layer = 0;
                surface.RemoveData(); surface.BuildNavMesh();
                if (!surface.navMeshData) throw new InvalidOperationException("Furniture navigation bake failed.");
                AssetDatabase.CreateAsset(surface.navMeshData, "Assets/SoundThief/Generated/ManorDressedNavMesh.asset"); EditorUtility.SetDirty(surface);
            }
            finally { foreach (var entry in doorLayers) entry.Key.layer = entry.Value; }
            EditorSceneManager.MarkSceneDirty(scene); AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(scene);
            return "Replaced eight identical bookcases with five storage silhouettes, shelf-specific books, reading/arrival/serving groups; rebuilt navigation for matching furniture collision.";
        }
    }
}
