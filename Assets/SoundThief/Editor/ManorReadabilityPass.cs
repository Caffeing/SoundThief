using System;
using System.IO;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace SoundThief.Editor
{
    public static class ManorReadabilityPass
    {
        const string Generated = "Assets/SoundThief/Generated/";
        const string Audio = "Assets/ThirdParty/Kenney/ImpactSounds/";
        static Material Material(string name, string shader)
        {
            var result = AssetDatabase.LoadAssetAtPath<Material>(Generated + name + ".mat");
            if (!result)
            {
                var source = Shader.Find(shader); if (!source) throw new InvalidOperationException("Missing shader: " + shader);
                result = new Material(source); AssetDatabase.CreateAsset(result, Generated + name + ".mat");
            }
            return result;
        }
        static Bounds Bounds(GameObject obj)
        {
            var rs = obj.GetComponentsInChildren<Renderer>().Where(r => r.enabled && r.gameObject.activeInHierarchy).ToArray();
            var bounds = rs[0].bounds; foreach (var r in rs) bounds.Encapsulate(r.bounds); return bounds;
        }
        static void Sound(GameObject obj, string label, string medium, int grade, AudioClip clip)
        {
            if (!obj) throw new InvalidOperationException("Missing sound object: " + label);
            var target = obj.GetComponent<ThiefInteractable>() ?? obj.AddComponent<ThiefInteractable>();
            target.kind = InteractionKind.Resonator; target.displayName = label;
            target.relation = obj.GetComponent<SoundRelation>() ?? obj.AddComponent<SoundRelation>();
            target.relation.firstMedium = "손"; target.relation.secondMedium = medium; target.relation.grade = grade; target.relation.soundClip = clip;
            target.emitter = obj.GetComponent<SoundEmitter>() ?? obj.AddComponent<SoundEmitter>();
            if (!obj.GetComponentsInChildren<Collider>().Any())
            {
                var b = Bounds(obj); var collider = obj.AddComponent<BoxCollider>();
                collider.center = obj.transform.InverseTransformPoint(b.center);
                collider.size = obj.transform.InverseTransformVector(b.size);
            }
            PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        }
        public static string Apply()
        {
            var scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlaying || scene.path != "Assets/Scenes/ManorPrototype.unity" || scene.isDirty)
                throw new InvalidOperationException("Open the saved manor in edit mode.");
            Directory.CreateDirectory("Logs/CoreLoop");
            if (!File.Exists("Logs/CoreLoop/Manor-before-readability.unity")) File.Copy(scene.path, "Logs/CoreLoop/Manor-before-readability.unity");
            var wood = AssetDatabase.LoadAssetAtPath<AudioClip>(Audio + "impactWood_heavy_000.ogg");
            var metal = AssetDatabase.LoadAssetAtPath<AudioClip>(Audio + "impactMetal_light_001.ogg");
            Sound(GameObject.Find("현관 여행 상자"), "현관 여행 상자 두드리기", "여행 상자", 1, wood);
            Sound(GameObject.Find("서쪽 복도 보조 탁자"), "복도 탁자 두드리기", "복도 탁자", 2, wood);
            Sound(GameObject.Find("서재 문서함"), "서재 문서함 두드리기", "문서함", 2, wood);
            Sound(GameObject.Find("식당 북쪽 배식장"), "식당 배식장 두드리기", "배식장", 2, wood);
            var tray = GameObject.Find("식당 식기 더미");
            if (!tray)
            {
                var plates = scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<Transform>())
                    .Where(t => t.name == "포갠 식기 접시").ToArray();
                if (plates.Length == 0) throw new InvalidOperationException("Missing serving plates.");
                var b = Bounds(plates[0].gameObject); foreach (var p in plates) b.Encapsulate(Bounds(p.gameObject));
                tray = new GameObject("식당 식기 더미"); tray.transform.position = b.center;
                tray.transform.SetParent(GameObject.Find("04 상호작용 사물").transform, true);
                foreach (var p in plates) p.SetParent(tray.transform, true);
                tray.AddComponent<BoxCollider>().size = Vector3.Max(b.size, new Vector3(.4f, .14f, .4f));
            }
            Sound(tray, "식당 식기 더미 건드리기", "금속 식기", 2, metal);
            foreach (var key in Object.FindObjectsByType<ThiefInteractable>(FindObjectsSortMode.None).Where(i => i.kind == InteractionKind.Key))
            {
                key.relation = key.GetComponent<SoundRelation>() ?? key.gameObject.AddComponent<SoundRelation>();
                key.relation.firstMedium = "손"; key.relation.secondMedium = key.displayName; key.relation.grade = 1; key.relation.soundClip = metal;
                PrefabUtility.RecordPrefabInstancePropertyModifications(key);
            }
            var outline = Material("Manor sound outline", "SoundThief/SoundOutline");
            var vision = Material("Manor guard floor vision", "SoundThief/GuardVision");
            vision.SetColor("_BaseColor", new Color(1, .025f, .02f, .26f)); EditorUtility.SetDirty(vision);
            int count = 0;
            foreach (var item in Object.FindObjectsByType<ThiefInteractable>(FindObjectsSortMode.None).Where(i => i.relation || i.emitter))
            {
                var effect = item.GetComponent<SoundObjectOutline>() ?? item.gameObject.AddComponent<SoundObjectOutline>();
                effect.material = outline; count++;
            }
            foreach (var guard in Object.FindObjectsByType<GuardAgent>(FindObjectsSortMode.None))
            {
                var cone = guard.GetComponent<GuardVisionCone>() ?? guard.gameObject.AddComponent<GuardVisionCone>(); cone.material = vision;
            }
            FitHelmets();
            // Keep the west dining aisle clear now that the armour has real physical cover.
            var armour = GameObject.Find("02 식당 장식 갑옷"); armour.transform.position = new Vector3(10.4f, 1, 9.9f);
            PrefabUtility.RecordPrefabInstancePropertyModifications(armour.transform);
            var doors = Object.FindObjectsByType<ThiefInteractable>(FindObjectsSortMode.None).Where(i => i.kind == InteractionKind.Door).ToArray();
            var colliders = doors.SelectMany(d => d.GetComponentsInChildren<Collider>(true)).ToArray(); var layers = colliders.Select(c => c.gameObject.layer).ToArray();
            try
            {
                foreach (var c in colliders) c.gameObject.layer = 0;
                var surface = Object.FindFirstObjectByType<NavMeshSurface>(); surface.BuildNavMesh();
                const string path = Generated + "ManorReadableStealthNavMesh.asset";
                if (!AssetDatabase.LoadAssetAtPath<NavMeshData>(path)) AssetDatabase.CreateAsset(surface.navMeshData, path);
                else throw new InvalidOperationException("Preserve the previous readability navigation asset before reapplying.");
                PrefabUtility.RecordPrefabInstancePropertyModifications(surface);
            }
            finally { for (int i = 0; i < colliders.Length; i++) colliders[i].gameObject.layer = layers[i]; }
            EditorSceneManager.MarkSceneDirty(scene); AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(scene);
            return "Outlined " + count + " sound objects, added five furniture/plate sound sources plus keys, three occluded vision cones and fitted helmets from actual head geometry.";
        }
        public static void FitHelmets()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Fit the saved guards in edit mode.");
            foreach (var guard in Object.FindObjectsByType<GuardAgent>(FindObjectsSortMode.None))
            {
                var body = guard.transform.Find("경비 외형"); var animator = body.GetComponent<Animator>();
                var idle = animator.runtimeAnimatorController.animationClips.First(c => c.name.ToLowerInvariant().Contains("idle"));
                idle.SampleAnimation(body.gameObject, .2f);
                var head = body.GetComponentsInChildren<SkinnedMeshRenderer>().First(r => r.name == "head");
                var mesh = new Mesh(); head.BakeMesh(mesh, false);
                var points = mesh.vertices.Select(v => head.transform.TransformPoint(v)).ToArray();
                var headBounds = new Bounds(points[0], Vector3.zero); foreach (var p in points) headBounds.Encapsulate(p); Object.DestroyImmediate(mesh);
                var helmet = body.GetComponentsInChildren<Transform>().First(t => t.name == "House guard open helmet");
                var before = Bounds(helmet.gameObject);
                // The helmet's front crown is lower than its mesh bounds: leave clearance there too.
                var desiredSize = new Vector3(headBounds.size.x * 1.3f, headBounds.size.y * .864f, headBounds.size.z * 1.3f);
                helmet.localScale = Vector3.Scale(helmet.localScale, new Vector3(desiredSize.x / before.size.x, desiredSize.y / before.size.y, desiredSize.z / before.size.z));
                var after = Bounds(helmet.gameObject);
                var center = new Vector3(headBounds.center.x, headBounds.max.y + .025f + headBounds.size.y * .144f - after.size.y * .5f, headBounds.center.z);
                helmet.position += center - after.center;
                PrefabUtility.RecordPrefabInstancePropertyModifications(helmet);
            }
        }
    }
}
