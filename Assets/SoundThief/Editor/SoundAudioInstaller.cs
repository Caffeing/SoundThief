using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SoundThief.Editor
{
    public static class SoundAudioInstaller
    {
        public const string LibraryPath = "Assets/SoundThief/Audio/SoundAudioLibrary.asset";
        const string Kenney = "Assets/ThirdParty/Kenney/ImpactSounds/";
        const string Complete = "Assets/ThirdParty/CompleteKenney/ImpactSounds/Audio/";
        static readonly string[] Scenes =
        {
            "Assets/Scenes/ManorPrototype.unity",
            "Assets/Scenes/Guild.unity",
            "Assets/Scenes/Stages/Stage2.unity",
            "Assets/Scenes/Stages/FrameworkExample.unity",
            "Assets/Scenes/Stages/StageTemplate.unity"
        };

        public static void InstallBatch() { Run(true); }
        public static void ValidateBatch() { Run(false); }

        static void Run(bool install)
        {
            var errors = new List<string>();
            var library = GetLibrary(install, errors);

            if (install && library)
            {
                foreach (string scenePath in Scenes)
                {
                    WireScene(scenePath, library, errors);
                }
            }

            if (!install)
            {
                foreach (string scenePath in Scenes)
                {
                    ValidateScene(scenePath, library, errors);
                }
            }

            foreach (string error in errors)
            {
                Debug.LogError("AUDIO_ERROR " + error);
            }

            Debug.Log(errors.Count == 0 ? "AUDIO_VALIDATE: PASS" : "AUDIO_VALIDATE: FAIL (" + errors.Count + " errors)");
            EditorApplication.Exit(errors.Count == 0 ? 0 : 1);
        }

        static SoundAudioLibrary GetLibrary(bool install, List<string> errors)
        {
            var library = AssetDatabase.LoadAssetAtPath<SoundAudioLibrary>(LibraryPath);

            if (!install)
            {
                if (!library) errors.Add("MISSING_LIBRARY");
                return library;
            }

            if (!library)
            {
                Directory.CreateDirectory("Assets/SoundThief/Audio");
                library = ScriptableObject.CreateInstance<SoundAudioLibrary>();
                AssetDatabase.CreateAsset(library, LibraryPath);
            }

            library.doorOpen = LoadClip(Kenney + "impactWood_heavy_000.ogg", errors);
            library.doorClose = LoadClip(Kenney + "impactWood_heavy_000.ogg", errors);
            library.keyPickup = LoadClip(Kenney + "impactMetal_light_000.ogg", errors);
            library.treasurePickup = LoadClip(Kenney + "impactMetal_heavy_000.ogg", errors);
            library.collectiblePickup = LoadClip(Kenney + "impactMetal_light_001.ogg", errors);
            library.resonator = library.collectiblePickup;
            library.guardFootstep = LoadClip(Kenney + "footstep_wood_000.ogg", errors);
            library.playerFootstep = LoadClip(Complete + "footstep_concrete_000.ogg", errors);
            EditorUtility.SetDirty(library);
            AssetDatabase.SaveAssets();
            return errors.Count == 0 ? library : null;
        }

        static AudioClip LoadClip(string path, List<string> errors)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (!clip) errors.Add("MISSING_CLIP " + path);
            return clip;
        }

        static void WireScene(string path, SoundAudioLibrary library, List<string> errors)
        {
            if (!File.Exists(path)) return;
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            bool dirty = false;

            foreach (var world in Object.FindObjectsByType<SoundWorld>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                world.audio = library;
                EditorUtility.SetDirty(world);
                dirty = true;
            }

            foreach (var player in Object.FindObjectsByType<ThiefPlayer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var footsteps = player.GetComponent<PlayerFootsteps>() ?? player.gameObject.AddComponent<PlayerFootsteps>();
                footsteps.clip = library.playerFootstep;
                EditorUtility.SetDirty(footsteps);
                dirty = true;
            }

            foreach (var guard in Object.FindObjectsByType<GuardAgent>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var footsteps = guard.GetComponent<GuardFootsteps>() ?? guard.gameObject.AddComponent<GuardFootsteps>();
                footsteps.clip = library.guardFootstep;
                EditorUtility.SetDirty(footsteps);
                dirty = true;
            }

            foreach (var relation in Object.FindObjectsByType<SoundRelation>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var item = relation.GetComponent<ThiefInteractable>();
                relation.soundClip = library.RelationClip(item);
                EditorUtility.SetDirty(relation);
                dirty = true;
            }

            if (dirty)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        }

        static void ValidateScene(string path, SoundAudioLibrary library, List<string> errors)
        {
            if (!File.Exists(path)) return;
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var worlds = Object.FindObjectsByType<SoundWorld>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var players = Object.FindObjectsByType<PlayerFootsteps>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var guards = Object.FindObjectsByType<GuardFootsteps>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var relations = Object.FindObjectsByType<SoundRelation>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (var world in worlds) if (world.audio != library) errors.Add(path + " SoundWorld.audio missing");
            foreach (var player in players) if (!player.clip) errors.Add(path + " PlayerFootsteps.clip missing");
            foreach (var guard in guards) if (!guard.clip) errors.Add(path + " GuardFootsteps.clip missing");
            foreach (var relation in relations) if (!relation.soundClip) errors.Add(path + " SoundRelation.soundClip missing");

            Debug.Log("AUDIO_REPORT scene=" + path + " worlds=" + worlds.Length + " players=" + players.Length + " guards=" + guards.Length + " relations=" + relations.Length);
        }
    }
}
