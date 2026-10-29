using System;
using System.IO;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace SoundThief.Editor
{
    public static class ManorCoreLoopPass
    {
        const string Audio = "Assets/ThirdParty/Kenney/ImpactSounds/";
        public static string ConfigureListeningWindow()
        {
            var scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlaying || scene.path != "Assets/Scenes/ManorPrototype.unity") throw new InvalidOperationException("Open manor in edit mode.");
            var context = Object.FindFirstObjectByType<StageContext>();
            const string path = "Assets/SoundThief/Generated/ManorStealthRules.asset";
            var rules = AssetDatabase.LoadAssetAtPath<SoundRules>(path);
            if (!rules) { rules = Object.Instantiate(context.rules); rules.name = "ManorStealthRules"; AssetDatabase.CreateAsset(rules, path); }
            rules.holdSeconds = new[] { 30f, 24f, 12f }; EditorUtility.SetDirty(rules);
            context.rules = rules; context.Bind();
            PrefabUtility.RecordPrefabInstancePropertyModifications(context);
            PrefabUtility.RecordPrefabInstancePropertyModifications(context.mission);
            PrefabUtility.RecordPrefabInstancePropertyModifications(context.mission.World);
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(Audio + "footstep_wood_000.ogg");
            if (!clip) throw new InvalidOperationException("Missing footsteps clip.");
            foreach (var guard in Object.FindObjectsByType<GuardAgent>(FindObjectsSortMode.None))
            {
                var footsteps = guard.GetComponent<GuardFootsteps>() ?? guard.gameObject.AddComponent<GuardFootsteps>();
                footsteps.clip = clip;
            }
            foreach (string name in new[] { "01 서재 괘종시계", "02 식당 장식 갑옷" })
            {
                var item = GameObject.Find(name); item.layer = 8;
                PrefabUtility.RecordPrefabInstancePropertyModifications(item);
            }
            EditorSceneManager.MarkSceneDirty(scene); AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(scene);
            return "Added directional guard footsteps and manor-only 30/24/12s carry limits to allow observation before committing.";
        }
        public static string AddStudyCover()
        {
            var scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlaying || scene.path != "Assets/Scenes/ManorPrototype.unity") throw new InvalidOperationException("Open manor in edit mode.");
            if (GameObject.Find("서재 관찰용 높은 책장")) return "Study cover already present.";
            var parent = GameObject.Find("04 상호작용 사물").transform;
            var shelf = ManorAssetPass.Fit("Assets/ThirdParty/Dungeon Megapack/Prefabs/Furniture/Wardrobe_2.prefab", "서재 관찰용 높은 책장", parent,
                new Bounds(new Vector3(-12.3f, 1.15f, 12.9f), new Vector3(2.8f, 2.3f, .65f)), Quaternion.identity);
            foreach (var f in shelf.GetComponentsInChildren<MeshFilter>().Where(f => f.GetComponent<Renderer>().enabled))
            { f.gameObject.layer = 8; f.gameObject.AddComponent<MeshCollider>().sharedMesh = f.sharedMesh; }
            ManorSetDressing.PopulateCoverShelf(shelf, parent);
            // Tall visual objects must also block eye-level sight and navigation.
            foreach (var item in Object.FindObjectsByType<ThiefInteractable>(FindObjectsSortMode.None))
            {
                if (item.name != "01 서재 괘종시계" && item.name != "02 식당 장식 갑옷") continue;
                var box = item.GetComponent<BoxCollider>();
                if (!box) continue;
                item.gameObject.layer = 8; box.center = Vector3.zero;
                box.size = item.name.Contains("clock") ? new Vector3(.58f, 1.85f, .42f) : new Vector3(.72f, 1.9f, .6f);
                PrefabUtility.RecordPrefabInstancePropertyModifications(item.gameObject);
                PrefabUtility.RecordPrefabInstancePropertyModifications(box);
            }
            var colliders = Object.FindObjectsByType<ThiefInteractable>(FindObjectsSortMode.None).Where(i => i.kind == InteractionKind.Door)
                .SelectMany(d => d.GetComponentsInChildren<Collider>(true)).ToArray();
            var layers = colliders.Select(c => c.gameObject.layer).ToArray();
            try
            {
                foreach (var c in colliders) c.gameObject.layer = 0;
                var surface = Object.FindFirstObjectByType<NavMeshSurface>(); surface.BuildNavMesh();
                AssetDatabase.CreateAsset(surface.navMeshData, "Assets/SoundThief/Generated/ManorStudyCoverNavMesh.asset");
                PrefabUtility.RecordPrefabInstancePropertyModifications(surface);
            }
            finally { for (int i = 0; i < colliders.Length; i++) colliders[i].gameObject.layer = layers[i]; }
            EditorSceneManager.MarkSceneDirty(scene); AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(scene);
            return "Added a tall observation shelf with two side routes; aligned clock/armour cover and rebuilt navigation.";
        }
        public static string PersistConfiguration()
        {
            var scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlaying || scene.path != "Assets/Scenes/ManorPrototype.unity") throw new InvalidOperationException("Open manor in edit mode.");
            var study = Object.FindObjectsByType<GuardAgent>(FindObjectsSortMode.None).First(g => g.name == "서재 순찰 경비");
            study.startsAwake = true; study.patrolWaitSeconds = 2.5f;
            study.patrolPoints = GameObject.Find("04 상호작용 사물").GetComponentsInChildren<Transform>()
                .Where(t => t.name.StartsWith("Study duty point")).OrderBy(t => t.name).ToArray();
            PrefabUtility.RecordPrefabInstancePropertyModifications(study);
            foreach (var item in Object.FindObjectsByType<ThiefInteractable>(FindObjectsSortMode.None))
            {
                if (item.kind == InteractionKind.Door) item.autoCloseSeconds = 8;
                PrefabUtility.RecordPrefabInstancePropertyModifications(item);
                if (item.relation) PrefabUtility.RecordPrefabInstancePropertyModifications(item.relation);
            }
            var surface = Object.FindFirstObjectByType<NavMeshSurface>(); PrefabUtility.RecordPrefabInstancePropertyModifications(surface);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            return "Persisted awake study route and 8s door instance overrides.";
        }
        public static string AddDiningSoundChoice()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop play mode first.");
            var scene = SceneManager.GetActiveScene();
            if (scene.path != "Assets/Scenes/ManorPrototype.unity") throw new InvalidOperationException("Open the manor first.");
            if (GameObject.Find("03 식당 배식 물병")) return "Dining sound choice already present.";
            var source = GameObject.Find("서재 황동 물병");
            var delta = new Vector3(17.2f, .91f, 7.4f) - source.transform.position;
            var target = Object.Instantiate(source, source.transform.parent); target.name = "03 식당 배식 물병";
            target.transform.position += delta;
            target.GetComponent<ThiefInteractable>().displayName = "식당 황동 물병";
            target.GetComponent<SoundRelation>().secondMedium = "식당 황동 물병";
            var stool = GameObject.Find("물병 옆 보조 의자");
            var copy = Object.Instantiate(stool, stool.transform.parent); copy.name = "식당 물병 옆 보조 의자"; copy.transform.position += delta;
            var colliders = Object.FindObjectsByType<ThiefInteractable>(FindObjectsSortMode.None).Where(i => i.kind == InteractionKind.Door)
                .SelectMany(d => d.GetComponentsInChildren<Collider>(true)).ToArray();
            var layers = colliders.Select(c => c.gameObject.layer).ToArray();
            try
            {
                foreach (var c in colliders) c.gameObject.layer = 0;
                var surface = Object.FindFirstObjectByType<NavMeshSurface>(); surface.BuildNavMesh();
                AssetDatabase.CreateAsset(surface.navMeshData, "Assets/SoundThief/Generated/ManorCoreLoopDiningNavMesh.asset");
            }
            finally { for (int i = 0; i < colliders.Length; i++) colliders[i].gameObject.layer = layers[i]; }
            EditorSceneManager.MarkSceneDirty(scene); AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(scene);
            PersistConfiguration();
            return "Added dining pitcher handoff beside the seal, with matching stool and updated navigation.";
        }
        public static string Apply()
        {
            var scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlaying || scene.path != "Assets/Scenes/ManorPrototype.unity" || scene.isDirty)
                throw new InvalidOperationException("Open the saved manor in edit mode.");
            if (GameObject.Find("04 상호작용 사물")) throw new InvalidOperationException("Core-loop pass already applied.");
            Directory.CreateDirectory("Logs/CoreLoop");
            const string backup = "Logs/CoreLoop/Manor-before-core-loop.unity";
            if (!File.Exists(backup)) File.Copy(scene.path, backup);
            AudioClip Clip(string name) => AssetDatabase.LoadAssetAtPath<AudioClip>(Audio + name + ".ogg")
                ?? throw new InvalidOperationException("Missing sound: " + name);
            var wood = Clip("impactWood_heavy_000");
            var tick = Clip("impactMetal_light_000");
            var metal = Clip("impactMetal_heavy_000");
            var world = Object.FindFirstObjectByType<SoundWorld>();
            var items = Object.FindObjectsByType<ThiefInteractable>(FindObjectsSortMode.None);
            foreach (var item in items)
            {
                if (item.kind == InteractionKind.Door)
                {
                    item.autoCloseSeconds = 8;
                    if (item.relation) item.relation.soundClip = wood;
                }
                if (item.kind == InteractionKind.Resonator)
                {
                    bool clock = item.name.Contains("Study");
                    item.relation.soundClip = clock ? tick : metal;
                    if (clock)
                    {
                        var ambient = item.GetComponent<RelationAmbience>() ?? item.gameObject.AddComponent<RelationAmbience>();
                        ambient.world = world; ambient.interval = 1.2f; ambient.volume = .35f;
                    }
                }
                if (item.kind == InteractionKind.Treasure)
                {
                    item.relation = item.GetComponent<SoundRelation>() ?? item.gameObject.AddComponent<SoundRelation>();
                    item.relation.firstMedium = "손"; item.relation.secondMedium = "인장과 금속 받침대";
                    item.relation.grade = 3; item.relation.soundClip = metal;
                }
            }
            int barriers = 0;
            foreach (var collider in Object.FindObjectsByType<Collider>(FindObjectsSortMode.None))
            {
                string n = collider.name.ToLowerInvariant();
                if (collider.gameObject.layer != 8 || collider.bounds.size.y < 2.5f || !(n.Contains("wall") || n.Contains("partition") || n.Contains("outer rear segment"))) continue;
                var barrier = collider.GetComponent<SoundBarrier>() ?? collider.gameObject.AddComponent<SoundBarrier>();
                barrier.rangeMultiplier = .65f; barriers++;
            }
            var root = new GameObject("04 상호작용 사물");
            var study = Object.FindObjectsByType<GuardAgent>(FindObjectsSortMode.None).First(g => g.name == "서재 순찰 경비");
            study.startsAwake = true; study.patrolWaitSeconds = 2.5f;
            var points = new[] { new Vector3(-18, 0, 19), new Vector3(-12.3f, 0, 19.5f), new Vector3(-12.3f, 0, 16) };
            study.patrolPoints = points.Select((p, i) =>
            {
                var point = new GameObject("Study duty point " + i).transform;
                point.SetParent(root.transform); point.position = p; return point;
            }).ToArray();

            var lure = new GameObject("서재 황동 물병"); lure.transform.SetParent(root.transform);
            lure.transform.position = new Vector3(-14.2f, .91f, 7.9f);
            var relation = lure.AddComponent<SoundRelation>(); relation.firstMedium = "손";
            relation.secondMedium = "황동 물병"; relation.grade = 1; relation.soundClip = Clip("impactMetal_light_001");
            var target = lure.AddComponent<ThiefInteractable>(); target.kind = InteractionKind.Resonator;
            target.displayName = "서재 황동 물병"; target.relation = relation; target.emitter = lure.AddComponent<SoundEmitter>();
            const string brassPath = "Assets/SoundThief/Generated/Manor lure brass.mat";
            var brass = AssetDatabase.LoadAssetAtPath<Material>(brassPath);
            if (!brass) { brass = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(brass, brassPath); }
            brass.color = new Color(.5f, .3f, .1f); brass.SetFloat("_Metallic", .6f); brass.SetFloat("_Smoothness", .3f);
            EditorUtility.SetDirty(brass);
            ManorAssetPass.Fit("Assets/ThirdParty/EmaceArt/Slavic World/Prefabs/Items/Crockery/EA03_Items_House_Pot_01a_PRE.prefab",
                "황동 물병 외형", lure.transform, new Bounds(lure.transform.position, new Vector3(.35f, .32f, .34f)), Quaternion.identity, brass);
            var colliderLure = lure.AddComponent<BoxCollider>(); colliderLure.size = new Vector3(.35f, .32f, .34f);
            var stool = ManorAssetPass.Fit("Assets/ThirdParty/EmaceArt/Slavic World/Prefabs/Prop/Furniture/EA03_Prop_Stool_01a_PRE.prefab",
                "물병 옆 보조 의자", root.transform, new Bounds(new Vector3(-14.2f, .375f, 7.9f), new Vector3(.55f, .75f, .55f)), Quaternion.identity);
            foreach (var f in stool.GetComponentsInChildren<MeshFilter>().Where(f => f.GetComponent<Renderer>().enabled))
            { f.gameObject.layer = 8; f.gameObject.AddComponent<MeshCollider>().sharedMesh = f.sharedMesh; }

            // Bake static furniture while leaving authored doors to their runtime carving obstacles.
            var doors = items.Where(i => i.kind == InteractionKind.Door).ToArray();
            var colliders = doors.SelectMany(d => d.GetComponentsInChildren<Collider>(true)).ToArray();
            var layers = colliders.Select(c => c.gameObject.layer).ToArray();
            try
            {
                foreach (var c in colliders) c.gameObject.layer = 0;
                var surface = Object.FindFirstObjectByType<NavMeshSurface>();
                surface.BuildNavMesh();
                AssetDatabase.CreateAsset(surface.navMeshData, "Assets/SoundThief/Generated/ManorCoreLoopNavMesh.asset");
            }
            finally { for (int i = 0; i < colliders.Length; i++) colliders[i].gameObject.layer = layers[i]; }
            EditorSceneManager.MarkSceneDirty(scene); AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(scene);
            return "Applied awake study patrol, two study lure positions, object-specific audio, noisy seal, 8s self-closing doors, " + barriers + " acoustic wall barriers and rebuilt navigation.";
        }
    }
}
