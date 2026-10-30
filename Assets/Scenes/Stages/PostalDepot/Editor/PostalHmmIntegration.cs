using System;
using System.Collections.Generic;
using System.Linq;
using SoundThief.SoundScan;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace SoundThief.PostalDepot.Editor
{
    // Applies HMM bindings in place; never regenerates the source postal geometry or NavMesh.
    public static class PostalHmmIntegration
    {
        const string ScenePath = PostalSceneBuilder.ScenePath;
        const string GuildPath = "Assets/Scenes/Guild.unity";
        const string RulesPath = PostalSceneBuilder.Folder + "/PostalDepot HMM Rules.asset";

        [MenuItem("Sound Thief/Postal Depot/Connect To HMM")]
        public static void Apply()
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling)
                throw new InvalidOperationException("Use Edit Mode after compilation.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save open scenes before integration.");
            var previous = SceneManager.GetActiveScene();
            bool openedSource, openedPostal, openedGuild;
            var source = Open("Assets/Scenes/ManorPrototype.unity", out openedSource);
            var postal = Open(ScenePath, out openedPostal);
            var guild = Open(GuildPath, out openedGuild);
            try
            {
                SceneManager.SetActiveScene(postal);
                var reference = Components<StageContext>(source).Single();
                var context = Components<StageContext>(postal).Single();
                var rules = AssetDatabase.LoadAssetAtPath<SoundRules>(RulesPath);
                if (!rules)
                {
                    rules = Object.Instantiate(reference.rules); rules.name = "PostalDepot HMM Rules";
                    AssetDatabase.CreateAsset(rules, RulesPath);
                }
                context.rules = rules;
                context.definition.startImmediately = false;
                EditorUtility.SetDirty(context.definition);
                context.mission.startsAccepted = false;
                context.mission.isGuild = false;
                context.mission.returnToGuildOnSuccess = true;
                context.mission.guildScenePath = GuildPath;
                context.Bind();
                Record(context); Record(context.mission); Record(context.mission.World); Record(context.player); Record(context.hud);

                var templateFootsteps = reference.player.GetComponent<PlayerFootsteps>();
                if (!templateFootsteps || !templateFootsteps.clip) throw new InvalidOperationException("HMM player footstep template is missing.");
                Copy(templateFootsteps, Get<PlayerFootsteps>(context.player.gameObject));
                var scan = reference.player.view.GetComponent<SoundScanController>();
                if (!scan) throw new InvalidOperationException("HMM camera scan template is missing.");
                Copy(scan, Get<SoundScanController>(context.player.view.gameObject));
                var bridge = Get<SoundScanSoundBridge>(context.player.view.gameObject);
                bridge.world = context.mission.World; Record(bridge);

                var referenceGuard = reference.SceneComponents<GuardAgent>().First(g => g.GetComponent<GuardAppearance>() && g.GetComponent<GuardAppearance>().animator);
                var appearance = referenceGuard.GetComponent<GuardAppearance>();
                var footstep = referenceGuard.GetComponent<GuardFootsteps>();
                var cone = referenceGuard.GetComponent<GuardVisionCone>();
                foreach (var guard in context.SceneComponents<GuardAgent>())
                {
                    // Keep original guard positions, wake settings and duties; only HMM presentation is bound.
                    var targetAppearance = Get<GuardAppearance>(guard.gameObject);
                    if (!targetAppearance.animator)
                    {
                        var visual = Object.Instantiate(appearance.animator.gameObject, guard.transform);
                        visual.name = "HMM postal guard visual";
                        visual.transform.localPosition = appearance.animator.transform.localPosition;
                        visual.transform.localRotation = appearance.animator.transform.localRotation;
                        visual.transform.localScale = appearance.animator.transform.localScale;
                        visual.SetActive(true);
                        foreach (var collider in visual.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(collider);
                        foreach (var t in visual.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 2;
                        foreach (var renderer in guard.GetComponentsInChildren<Renderer>(true))
                            if (!renderer.transform.IsChildOf(visual.transform) && !renderer.GetComponent<TMPro.TMP_Text>()) renderer.enabled = false;
                        Copy(appearance, targetAppearance);
                        targetAppearance.animator = visual.GetComponent<Animator>();
                    }
                    Record(targetAppearance);
                    if (footstep) Copy(footstep, Get<GuardFootsteps>(guard.gameObject));
                    if (cone) Copy(cone, Get<GuardVisionCone>(guard.gameObject));
                    Record(guard);
                }

                var outlineMaterial = reference.SceneComponents<SoundObjectOutline>().FirstOrDefault(o => o.material)?.material;
                var doorClip = reference.SceneComponents<ThiefInteractable>().Where(i => i.kind == InteractionKind.Door && i.relation && i.relation.soundClip).Select(i => i.relation.soundClip).FirstOrDefault();
                var sourceClip = reference.SceneComponents<ThiefInteractable>().Where(i => i.kind == InteractionKind.Resonator && i.relation && i.relation.soundClip).Select(i => i.relation.soundClip).FirstOrDefault();
                foreach (var item in context.SceneComponents<ThiefInteractable>())
                {
                    if (outlineMaterial && (item.relation || item.emitter || item.kind == InteractionKind.Key || item.kind == InteractionKind.Collectible))
                    {
                        var outline = Get<SoundObjectOutline>(item.gameObject); outline.material = outlineMaterial; Record(outline);
                    }
                    if (item.relation)
                    {
                        if (!item.relation.soundClip) item.relation.soundClip = item.kind == InteractionKind.Door ? doorClip : sourceClip;
                        Record(item.relation);
                    }
                    if (item.kind == InteractionKind.Door && !item.emitter)
                    { item.emitter = Get<SoundEmitter>(item.gameObject); Record(item); }
                    if (item.emitter) { item.emitter.injectionDelaySeconds = 3; Record(item.emitter); }
                    if (item.kind == InteractionKind.Resonator && item.relation)
                    {
                        var ambience = Get<RelationAmbience>(item.gameObject); ambience.world = context.mission.World; Record(ambience);
                    }
                }
                var reader = context.mission.GetComponent<PostalLetterReader>();
                var briefing = context.mission.GetComponent<PostalBriefing>();
                if (!reader || !briefing) throw new InvalidOperationException("Postal letter components are missing.");
                reader.mission = briefing.mission = context.mission; Record(reader); Record(briefing);
                foreach (var gate in context.SceneComponents<PostalLoadingGate>())
                {
                    var item = gate.GetComponent<ThiefInteractable>();
                    item.guardsCanOpen = false; item.autoCloseSeconds = 0; Record(item);
                }
                EditorSceneManager.MarkSceneDirty(postal); EditorSceneManager.SaveScene(postal);

                var table = Components<GuildContractTable>(guild).Single();
                var offers = new List<GuildContractOffer>(table.offers ?? Array.Empty<GuildContractOffer>());
                var offer = new GuildContractOffer { definition = context.definition, scenePath = ScenePath, available = true, nonRepeatable = false };
                int index = offers.FindIndex(o => o != null && o.scenePath == ScenePath);
                if (index < 0) index = offers.FindIndex(o => o == null || (!o.available && !o.definition));
                if (index < 0) offers.Add(offer); else offers[index] = offer;
                table.offers = offers.ToArray(); Record(table);
                EditorSceneManager.MarkSceneDirty(guild); EditorSceneManager.SaveScene(guild);
                var scenes = EditorBuildSettings.scenes.ToList();
                if (!scenes.Any(s => s.path == ScenePath)) scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
                else foreach (var entry in scenes.Where(s => s.path == ScenePath)) entry.enabled = true;
                EditorBuildSettings.scenes = scenes.ToArray(); AssetDatabase.SaveAssets();

                System.IO.Directory.CreateDirectory("Artifacts/PostalIntegration");
                System.IO.File.WriteAllText("Artifacts/PostalIntegration/scene-configuration.json", Newtonsoft.Json.JsonConvert.SerializeObject(new {
                    scene = ScenePath, definition = AssetDatabase.GetAssetPath(context.definition), rules = RulesPath,
                    startImmediately = context.definition.startImmediately, returnToGuild = context.mission.returnToGuildOnSuccess,
                    guards = context.SceneComponents<GuardAgent>().Length, letters = context.SceneComponents<PostalLetter>().Length,
                    playerFootsteps = (bool)context.player.GetComponent<PlayerFootsteps>().clip,
                    scanWorldBound = bridge.world == context.mission.World,
                    offers = table.offers.Select(o => new { o.scenePath, o.nonRepeatable, o.available }).ToArray(),
                    playMode = EditorApplication.isPlaying, navMeshRebuilt = false
                }, Newtonsoft.Json.Formatting.Indented));
            }
            finally
            {
                if (openedSource) EditorSceneManager.CloseScene(source, true);
                if (openedPostal) EditorSceneManager.CloseScene(postal, true);
                if (openedGuild) EditorSceneManager.CloseScene(guild, true);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
        }
        static Scene Open(string path, out bool opened)
        {
            var scene = SceneManager.GetSceneByPath(path); opened = !scene.IsValid() || !scene.isLoaded;
            return opened ? EditorSceneManager.OpenScene(path, OpenSceneMode.Additive) : scene;
        }
        static T[] Components<T>(Scene scene) where T : Component => scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<T>(true)).ToArray();
        static T Get<T>(GameObject g) where T : Component => g.GetComponent<T>() ?? g.AddComponent<T>();
        static void Copy(Object source, Object target) { EditorUtility.CopySerialized(source, target); Record(target); }
        static void Record(Object target) { EditorUtility.SetDirty(target); PrefabUtility.RecordPrefabInstancePropertyModifications(target); }
    }
}
