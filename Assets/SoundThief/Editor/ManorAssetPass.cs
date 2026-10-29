using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace SoundThief.Editor
{
    // Presentation-only pass. Gameplay roots, colliders and the baked navigation remain independent.
    public static class ManorAssetPass
    {
        const string ScenePath = "Assets/Scenes/ManorPrototype.unity";
        const string Generated = "Assets/SoundThief/Generated/";
        const string Dungeon = "Assets/ThirdParty/Dungeon Megapack/Prefabs/";
        const string Slavica = "Assets/ThirdParty/EmaceArt/Slavic World/Prefabs/";
        const string KnightPath = "Assets/ThirdParty/Quaternius/Knight/";
        const string ClockPath = "Assets/ThirdParty/Grab3D/GrandfatherClock/";
        static Material plaster, stone, oak, iron, leather, skin, brass, cloth, border;
        static Transform art;
        static readonly List<string> replacements = new List<string>();

        static Material Material(string name, Color color, float metallic = 0, float smoothness = .25f)
        {
            var path = Generated + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material) { material = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(material, path); }
            material.color = color;
            material.SetFloat("_Metallic", metallic); material.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(material); return material;
        }

        static bool Builtin(Renderer renderer)
        {
            var filter = renderer.GetComponent<MeshFilter>();
            if (!filter || !filter.sharedMesh) return false;
            var path = AssetDatabase.GetAssetPath(filter.sharedMesh);
            return path.Contains("unity default resources");
        }

        internal static Bounds Bounds(GameObject obj)
        {
            var renderers = obj.GetComponentsInChildren<Renderer>(true).Where(r => r.enabled && r.gameObject.activeInHierarchy).ToArray();
            if (renderers.Length == 0) throw new InvalidOperationException("No visible mesh: " + obj.name);
            var result = renderers[0].bounds;
            foreach (var renderer in renderers) result.Encapsulate(renderer.bounds);
            return result;
        }

        static void VisualOnly(GameObject obj)
        {
            foreach (var collider in obj.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(collider);
            foreach (var rigidbody in obj.GetComponentsInChildren<Rigidbody>(true)) Object.DestroyImmediate(rigidbody);
            foreach (var lod in obj.GetComponentsInChildren<LODGroup>(true)) Object.DestroyImmediate(lod);
            foreach (var t in obj.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 0;
            foreach (var renderer in obj.GetComponentsInChildren<Renderer>(true))
                if (System.Text.RegularExpressions.Regex.IsMatch(renderer.name, @"_LOD[1-9]$")) renderer.enabled = false;
        }

        internal static GameObject Fit(string path, string name, Transform parent, Bounds desired, Quaternion rotation, Material material = null)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!source) throw new InvalidOperationException("Missing asset: " + path);
            var wrapper = new GameObject(name);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(source);
            model.transform.SetParent(wrapper.transform, false); model.transform.localRotation = rotation;
            VisualOnly(model);
            var initial = Bounds(wrapper);
            wrapper.transform.localScale = new Vector3(desired.size.x / initial.size.x, desired.size.y / initial.size.y, desired.size.z / initial.size.z);
            wrapper.transform.position += desired.center - Bounds(wrapper).center;
            wrapper.transform.SetParent(parent, true);
            if (material) foreach (var r in wrapper.GetComponentsInChildren<Renderer>(true)) r.sharedMaterials = r.sharedMaterials.Select(_ => material).ToArray();
            return wrapper;
        }

        static void Hide(Renderer renderer)
        {
            replacements.Add(renderer.gameObject.name); renderer.enabled = false;
        }

        static void Wall(Renderer old)
        {
            var b = old.bounds; bool alongX = b.size.x > b.size.z;
            float length = alongX ? b.size.x : b.size.z;
            int pieces = Mathf.Max(1, Mathf.CeilToInt(length / 4));
            float segment = length / pieces;
            for (int i = 0; i < pieces; i++)
            {
                var center = b.center + (alongX ? Vector3.right : Vector3.forward) * (-length / 2 + segment * (i + .5f));
                var size = alongX ? new Vector3(segment, b.size.y, b.size.z) : new Vector3(b.size.x, b.size.y, segment);
                bool exterior = Mathf.Abs(center.x) > 15 || center.z > 25 || center.z < 0;
                string variant = exterior && b.size.y > 3 && i % 3 == 1 ? "석벽 모듈 외형" : "석벽 모듈 외형";
                var rotation = alongX ? Quaternion.Euler(0, center.z < 7 ? 180 : 0, 0) : Quaternion.Euler(0, center.x < 0 ? -90 : 90, 0);
                Fit(Dungeon + "Walls A/" + variant + ".prefab", "Manor masonry · " + old.name + " · " + i, art, new Bounds(center, size), rotation, plaster);
            }
            Hide(old);
        }

        static void Ceiling(Renderer old)
        {
            var b = old.bounds;
            var group = new GameObject("Board ceiling · " + old.name); group.transform.SetParent(art, false);
            int columns = Mathf.CeilToInt(b.size.x / 2), rows = Mathf.CeilToInt(b.size.z / 2);
            float width = b.size.x / columns, depth = b.size.z / rows;
            for (int x = 0; x < columns; x++) for (int z = 0; z < rows; z++)
            {
                var center = new Vector3(b.min.x + (x + .5f) * width, b.min.y + .045f, b.min.z + (z + .5f) * depth);
                Fit(Dungeon + "Furniture/Floor_5_1.prefab", "천장 널판 원본 모듈", group.transform, new Bounds(center, new Vector3(width, .09f, depth)), Quaternion.Euler(180, 0, 0));
            }
            Combine(group, "ManorAssetCeiling-" + old.name.Replace(" ", "")); Hide(old);
        }

        static void Combine(GameObject group, string assetName)
        {
            var filters = group.GetComponentsInChildren<MeshFilter>().Where(f => f.GetComponent<Renderer>().enabled).ToArray();
            var materials = filters.SelectMany(f => f.GetComponent<Renderer>().sharedMaterials).Distinct().ToArray();
            var combined = new List<CombineInstance>(); var temporary = new List<Mesh>();
            foreach (var material in materials)
            {
                var parts = new List<CombineInstance>();
                foreach (var filter in filters)
                {
                    var mats = filter.GetComponent<Renderer>().sharedMaterials;
                    for (int i = 0; i < filter.sharedMesh.subMeshCount; i++) if (i < mats.Length && mats[i] == material)
                        parts.Add(new CombineInstance { mesh = filter.sharedMesh, subMeshIndex = i, transform = group.transform.worldToLocalMatrix * filter.transform.localToWorldMatrix });
                }
                var mesh = new Mesh { indexFormat = IndexFormat.UInt32 }; mesh.CombineMeshes(parts.ToArray(), true, true);
                temporary.Add(mesh); combined.Add(new CombineInstance { mesh = mesh, transform = Matrix4x4.identity });
            }
            var final = new Mesh { name = assetName, indexFormat = IndexFormat.UInt32 }; final.CombineMeshes(combined.ToArray(), false, false);
            AssetDatabase.CreateAsset(final, Generated + assetName + ".asset");
            foreach (var mesh in temporary) Object.DestroyImmediate(mesh);
            foreach (var r in group.GetComponentsInChildren<Renderer>()) r.enabled = false;
            group.AddComponent<MeshFilter>().sharedMesh = final; group.AddComponent<MeshRenderer>().sharedMaterials = materials;
        }

        static Material ClockMaterial(string part)
        {
            var material = Material("Manor clock " + part, Color.white, part == "pendulum" ? .7f : 0, .3f);
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(ClockPath + "Textures/grandfather-clock_" + part + "_basecolor.jpg"));
            var normalPath = ClockPath + "Textures/grandfather-clock_" + part + "_normal.jpg";
            material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath)); material.EnableKeyword("_NORMALMAP");
            return material;
        }

        static void Clock(ThiefInteractable target)
        {
            foreach (var r in target.GetComponentsInChildren<Renderer>()) if (r.enabled) Hide(r);
            var visual = Fit(ClockPath + "grandfather-clock.fbx", "괘종시계 외형", target.transform,
                new Bounds(target.transform.position, new Vector3(.53f, 1.85f, .37f)), Quaternion.Euler(-90, 180, 0));
            foreach (var r in visual.GetComponentsInChildren<Renderer>())
            {
                if (r.name.EndsWith("glass"))
                {
                    var glass = Material("Manor clock glass", new Color(.18f, .22f, .25f, .08f), 0, .65f);
                    glass.SetFloat("_Surface", 1); glass.SetFloat("_SrcBlend", 5); glass.SetFloat("_DstBlend", 10); glass.SetFloat("_ZWrite", 0);
                    glass.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); glass.renderQueue = 3000; r.sharedMaterial = glass;
                }
                else r.sharedMaterial = ClockMaterial(r.name.Contains("pendulum") ? "pendulum" : r.name.Contains("door") ? "door" : "body");
            }
        }

        static GameObject Knight(Transform parent, bool display)
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(KnightPath + "KnightCharacter.fbx");
            var model = (GameObject)PrefabUtility.InstantiatePrefab(source); model.name = display ? "장식 갑옷 외형" : "저택 경비 외형";
            model.transform.SetParent(parent, false); model.transform.localPosition = display ? new Vector3(0, -1, 0) : Vector3.zero;
            model.transform.localRotation = Quaternion.identity; model.transform.localScale = Vector3.one * .335f;
            VisualOnly(model);
            var clips = AssetDatabase.LoadAllAssetsAtPath(KnightPath + "KnightCharacter.fbx").OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__")).ToArray();
            var animator = model.GetComponent<Animator>();
            if (!animator) animator = model.AddComponent<Animator>();
            animator.applyRootMotion = false;
            var idle = clips.First(c => c.name.EndsWith("|Idle")); idle.SampleAnimation(model, 0);
            foreach (var renderer in model.GetComponentsInChildren<Renderer>()) renderer.sharedMaterials = renderer.sharedMaterials.Select(m => m.name.Contains("Boot") ? leather : m.name.Contains("Skin") ? (display ? iron : skin) : iron).ToArray();
            var head = model.GetComponentsInChildren<Transform>().First(t => t.name == "Head");
            var helmetSource = AssetDatabase.LoadAssetAtPath<GameObject>(KnightPath + "Helmet1.fbx");
            var helmet = (GameObject)PrefabUtility.InstantiatePrefab(helmetSource); helmet.name = "Closed steel helmet";
            helmet.transform.SetParent(model.transform, false); helmet.transform.localPosition = new Vector3(0, 4.75f, -.04f); helmet.transform.localScale = Vector3.one * 100;
            helmet.transform.SetParent(head, true); VisualOnly(helmet);
            foreach (var renderer in helmet.GetComponentsInChildren<Renderer>()) renderer.sharedMaterials = renderer.sharedMaterials.Select(m => m.name.Contains("Dark") ? leather : iron).ToArray();
            var shoulderSource = AssetDatabase.LoadAssetAtPath<GameObject>(KnightPath + "ShoulderPads.fbx");
            var shoulders = (GameObject)PrefabUtility.InstantiatePrefab(shoulderSource); shoulders.name = "Steel shoulder plates";
            shoulders.transform.SetParent(model.transform, false); shoulders.transform.localPosition = new Vector3(0, 3.72f, 0); shoulders.transform.localScale = Vector3.one * 100;
            shoulders.transform.SetParent(model.GetComponentsInChildren<Transform>().First(t => t.name == "Neck"), true); VisualOnly(shoulders);
            foreach (var renderer in shoulders.GetComponentsInChildren<Renderer>()) renderer.sharedMaterial = iron;
            if (display)
            {
                foreach (var renderer in model.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    var mesh = new Mesh { name = "Display armour" }; renderer.BakeMesh(mesh);
                    AssetDatabase.CreateAsset(mesh, Generated + "ManorDisplayArmour.asset");
                    var node = new GameObject("Static armour mesh"); node.transform.SetParent(renderer.transform, false);
                    var inheritedScale = renderer.transform.lossyScale;
                    node.transform.localScale = new Vector3(1 / inheritedScale.x, 1 / inheritedScale.y, 1 / inheritedScale.z);
                    node.AddComponent<MeshFilter>().sharedMesh = mesh; node.AddComponent<MeshRenderer>().sharedMaterials = renderer.sharedMaterials;
                    renderer.enabled = false;
                }
                animator.enabled = false;
            }
            else
            {
                const string controllerPath = Generated + "ManorKnight.controller";
                var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
                if (!controller)
                {
                    controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
                    var stateMachine = controller.layers[0].stateMachine;
                    var a = stateMachine.AddState("Idle"); a.motion = idle; stateMachine.defaultState = a;
                    var b = stateMachine.AddState("Walk"); b.motion = clips.First(c => c.name.EndsWith("|Walking"));
                }
                animator.runtimeAnimatorController = controller; animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                foreach (var r in model.GetComponentsInChildren<SkinnedMeshRenderer>()) r.updateWhenOffscreen = true;
                var appearance = parent.GetComponent<GuardAppearance>() ?? parent.gameObject.AddComponent<GuardAppearance>(); appearance.animator = animator;
            }
            return model;
        }

        static void Candle(string name, Vector3 surface)
        {
            Fit(Dungeon + "Other/CandleHolder_1_1.prefab", name + " brass candlestick", art, new Bounds(surface + Vector3.up * .11f, new Vector3(.18f, .22f, .18f)), Quaternion.identity, brass);
            Fit(Dungeon + "Other/Candle_1.prefab", name + " wax candle", art, new Bounds(surface + Vector3.up * .32f, new Vector3(.065f, .22f, .065f)), Quaternion.identity);
            var obj = new GameObject(name + " warm practical"); obj.transform.SetParent(art, false); obj.transform.position = surface + Vector3.up * .55f;
            var light = obj.AddComponent<Light>(); light.type = LightType.Point; light.color = new Color(1, .69f, .36f); light.intensity = .65f; light.range = 4; light.shadows = LightShadows.None;
        }

        public static string Refine()
        {
            if (EditorApplication.isPlaying || SceneManager.GetActiveScene().path != ScenePath) throw new InvalidOperationException("Open the manor in edit mode.");
            art = GameObject.Find("기본 건축 장식").transform;
            var armour = GameObject.Find("02 식당 장식 갑옷").transform;
            var mesh = armour.GetComponentsInChildren<Transform>(true).First(t => t.name == "Static armour mesh");
            var inherited = mesh.parent.lossyScale;
            mesh.localScale = new Vector3(1 / inherited.x, 1 / inherited.y, 1 / inherited.z);
            armour.position = new Vector3(10.2f, 1, 13.5f); armour.rotation = Quaternion.Euler(0, -25, 0);
            var shield = GameObject.Find("장식 방패 외형");
            if (shield) { shield.transform.position = armour.position + armour.rotation * new Vector3(-.5f, -.48f, -.4f); shield.transform.rotation = armour.rotation; }
            var clock = GameObject.Find("01 서재 괘종시계").transform;
            clock.position = new Vector3(-10, 1, 14); clock.rotation = Quaternion.Euler(0, 45, 0);
            var stand = GameObject.Find("가구 받침 - 식당 전시 받침");
            if (stand && stand.GetComponentsInChildren<MeshFilter>(true).Any(f => AssetDatabase.GetAssetPath(f.sharedMesh).Contains("Monument")))
            {
                Object.DestroyImmediate(stand);
                Fit(Slavica + "Prop/Furniture/EA03_Prop_Tabble_01a_PRE.prefab", "가구 받침 - 식당 전시 받침", art,
                    new Bounds(new Vector3(19, .55f, 10), new Vector3(1.4f, 1.1f, 1.4f)), Quaternion.identity);
            }
            var seal = GameObject.Find("가문 인장").transform;
            var sealBase = seal.Find("가문 인장 문양 황동 받침"); var handle = seal.Find("가문 인장 손잡이");
            sealBase.position = seal.position - Vector3.up * .195f; handle.position = seal.position - Vector3.up * .05f;
            var scene = SceneManager.GetActiveScene(); EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
            return "Corrected static armour scale, supported seal and placed clock/armour beside walls away from table and doors.";
        }

        public static string Apply()
        {
            var scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlaying || scene.path != ScenePath || scene.isDirty) throw new InvalidOperationException("Open the saved manor in edit mode first.");
            if (GameObject.Find("기본 건축 장식")) throw new InvalidOperationException("Asset pass already exists; refusing to duplicate it.");
            Directory.CreateDirectory("Logs/AssetAudit");
            const string backup = "Logs/AssetAudit/Manor-before-asset-replacement.unity";
            if (!File.Exists(backup)) File.Copy(ScenePath, backup, false);
            else if (!File.ReadAllBytes(ScenePath).SequenceEqual(File.ReadAllBytes(backup))) throw new InvalidOperationException("Existing backup differs from the source scene; preserve it before another pass.");
            replacements.Clear();
            plaster = Material("Manor warm masonry", new Color(.67f, .60f, .49f));
            stone = Material("Manor carved stone", new Color(.43f, .40f, .35f)); oak = Material("Manor dark structural oak", new Color(.19f, .10f, .052f));
            iron = Material("Manor aged steel", new Color(.30f, .35f, .39f), .65f, .36f); leather = Material("Manor guard leather", new Color(.12f, .058f, .03f));
            skin = Material("Manor guard skin", new Color(.55f, .34f, .23f)); brass = Material("Manor antique brass", new Color(.53f, .34f, .10f), .65f, .3f);
            cloth = Material("Manor woven burgundy", new Color(.22f, .045f, .043f)); border = Material("Manor carpet gold border", new Color(.42f, .27f, .09f));
            art = new GameObject("기본 건축 장식").transform;
            var originals = scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<Renderer>(true)).Where(r => r.enabled && r.gameObject.activeInHierarchy && Builtin(r)).ToArray();
            foreach (var old in originals)
            {
                var n = old.name; var b = old.bounds;
                if (old.GetComponentInParent<ThiefInteractable>()?.kind == InteractionKind.Resonator) continue;
                if (n.Contains("ceiling") && !n.Contains("beam")) { Ceiling(old); continue; }
                if (n.Contains("ceiling beam"))
                {
                    int pieces = 6;
                    for (int i = 0; i < pieces; i++) Fit(Dungeon + "Walls A/Beam_4A.prefab", "구조 참나무 보", art, new Bounds(b.center + Vector3.right * (-b.size.x / 2 + (i + .5f) * b.size.x / pieces), new Vector3(b.size.x / pieces, b.size.y, b.size.z)), Quaternion.Euler(0, 0, 90), oak);
                    Hide(old); continue;
                }
                if (n.Contains("wall") || n.Contains("partition") || n.Contains("outer rear segment")) { Wall(old); continue; }
                if (n.Contains("lintel") || n.Contains("jamb"))
                {
                    Fit(Dungeon + "Walls A/Wall_1_1A.prefab", "Carved doorway · " + n, art, b, Quaternion.identity, stone); Hide(old); continue;
                }
                if (n.StartsWith("Carpet") || n.EndsWith("rug"))
                {
                    var rug = Fit("Assets/ThirdParty/Kenney/Furniture/rugRectangle.fbx", "Woven rug · " + n, art, b, Quaternion.Euler(0, 90, 0));
                    foreach (var r in rug.GetComponentsInChildren<Renderer>()) r.sharedMaterials = r.sharedMaterials.Select(m => m.name.Contains("Darker") ? border : cloth).ToArray();
                    Hide(old); continue;
                }
                if (n.Contains("plinth"))
                {
                    var path = n == "열쇠 받침 충돌체" ? Slavica + "Prop/Furniture/EA03_Prop_Stool_01a_PRE.prefab" : n == "인장 받침 충돌체" ? Dungeon + "Other/Chest_2.prefab" : Dungeon + "Walls A/MonumentA.prefab";
                    Fit(path, "Furnished stand · " + n, art, b, Quaternion.identity, n.Contains("display") ? stone : null); Hide(old); continue;
                }
                if (n == "랜턴 고리")
                {
                    Fit(Dungeon + "Other/Chain_1_1.prefab", "랜턴 고리 사슬", old.transform.parent, b, Quaternion.identity, iron); Hide(old); continue;
                }
                if (n == "가문 인장")
                {
                    Fit(Dungeon + "Other/Coin_1.prefab", "가문 인장 문양 황동 받침", old.transform, new Bounds(b.center + Vector3.down * .12f, new Vector3(.32f, .07f, .32f)), Quaternion.identity, brass);
                    Fit(Dungeon + "Other/Cup_1_1.prefab", "가문 인장 손잡이", old.transform, new Bounds(b.center + Vector3.up * .035f, new Vector3(.13f, .24f, .13f)), Quaternion.Euler(180, 0, 0), brass); Hide(old); continue;
                }
                if (n == "탈출 지점")
                {
                    Fit(Slavica + "Village/Elements/EA03_Prop_House_Door_01b_PRE.prefab", "저택 출구 참나무 문", old.transform, new Bounds(new Vector3(b.center.x, 1.5f, b.center.z), new Vector3(2, 3, .2f)), Quaternion.identity);
                    var collider = old.GetComponent<BoxCollider>(); collider.size = new Vector3(1, 3, 1); collider.center = Vector3.up; Hide(old); continue;
                }
                if (n == "자물쇠") { Hide(old); continue; }
                throw new InvalidOperationException("Unclassified visible placeholder: " + n);
            }
            foreach (var obj in scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<Transform>(true)).Where(t => t.name == "벽 목재 몰딩 통합 외형"))
                foreach (var renderer in obj.GetComponentsInChildren<Renderer>()) renderer.enabled = false;
            var targets = Object.FindObjectsByType<ThiefInteractable>(FindObjectsSortMode.None);
            Clock(targets.First(t => t.name == "01 서재 괘종시계"));
            var armour = targets.First(t => t.name == "02 식당 장식 갑옷");
            foreach (var r in armour.GetComponentsInChildren<Renderer>()) if (r.enabled) Hide(r);
            Knight(armour.transform, true);
            foreach (var guard in Object.FindObjectsByType<GuardAgent>(FindObjectsSortMode.None))
            {
                var previous = guard.transform.Find("경비 외형"); if (previous) previous.gameObject.SetActive(false);
                Knight(guard.transform, false);
            }
            Candle("Study reading", new Vector3(-19.65f, 1.1f, 21.3f));
            Candle("Dining centre", new Vector3(12.5f, 1.3f, 16.2f));
            Candle("Hall storage", new Vector3(-.35f, 1.2f, 21));
            Candle("Seal display", new Vector3(19.45f, 1.1f, 10.2f));
            var unknown = scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<Renderer>(true)).Where(r => r.enabled && r.gameObject.activeInHierarchy && Builtin(r)).Select(r => r.name).ToArray();
            if (unknown.Length != 0) throw new InvalidOperationException("Remaining visible placeholders: " + string.Join(", ", unknown));
            File.WriteAllLines("Logs/AssetAudit/replaced-placeholders.txt", replacements);
            Refine(); EditorSceneManager.MarkSceneDirty(scene); AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(scene);
            return "Replaced " + replacements.Count + " visible placeholders. Imported masonry, ceiling boards, rugs, clock, display armour, seal and three guards; preserved gameplay roots and navigation.";
        }
    }
}
