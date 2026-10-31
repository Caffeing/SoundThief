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

namespace SoundThief.JinHo.Editor
{
    public static class JinHoStageBuilder
    {
        public const string ValidationPath = "Assets/Game/Stages/Stage2/Scenes/JinHoSystemsValidation.unity";
        public const string ValidationProfilePath = "Assets/Game/Stages/Stage2/Settings/JinHo Validation.asset";
        const string Prefabs = "Assets/Game/Prefabs/";

        [MenuItem("Sound Thief/JinHo/Open Systems Validation")]
        public static void OpenValidation()
        {
            if (EditorApplication.isPlaying || SceneManager.GetActiveScene().isDirty)
                throw new InvalidOperationException("Stop Play Mode and resolve unsaved scene changes first.");
            var profile=AssetDatabase.LoadAssetAtPath<UnityEditor.Build.Profile.BuildProfile>(ValidationProfilePath);
            if (!profile || !File.Exists(ValidationPath)) throw new InvalidOperationException("Validation scene/profile is missing.");
            if (UnityEditor.Build.Profile.BuildProfile.GetActiveBuildProfile()!=profile)
                UnityEditor.Build.Profile.BuildProfile.SetActiveBuildProfile(profile);
            EditorSceneManager.OpenScene(ValidationPath);
        }

        [MenuItem("Sound Thief/JinHo/Use Shared Scene List")]
        public static void UseSharedSceneList()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
            UnityEditor.Build.Profile.BuildProfile.SetActiveBuildProfile(null);
        }

        public static string BuildValidation()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode before building.");
            var previous = SceneManager.GetActiveScene();
            if (previous.isDirty) throw new InvalidOperationException("The current scene has unsaved changes.");
            if (File.Exists(ValidationPath)) throw new InvalidOperationException("Validation scene already exists; refusing to overwrite it.");
            var originalEvents = previous.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<Transform>(true)).First(t=>t.name=="EventSystem").gameObject;
            var originalSun = previous.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<Transform>(true)).First(t=>t.name=="Moonlight").gameObject;
            var ambient = RenderSettings.ambientLight; var ambientMode = RenderSettings.ambientMode;
            Directory.CreateDirectory("Assets/Game/Stages/Stage2/Scenes"); Directory.CreateDirectory("Assets/Game/Stages/Stage2/Navigation");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                var root = new GameObject("JinHo validation geometry").transform;
                var systems = Spawn("StageSystems", Vector3.zero);
                var context = systems.GetComponent<StageContext>();
                context.definition = AssetDatabase.LoadAssetAtPath<StageDefinition>("Assets/Game/Generated/Manor Definition.asset");
                context.rules = AssetDatabase.LoadAssetAtPath<SoundRules>("Assets/Game/Generated/PrototypeRules.asset");
                context.mission.startsAccepted = true;
                var player = Spawn("PlayerRig", new Vector3(-6, 0.05f, 9)).GetComponent<ThiefPlayer>();
                context.player = player;
                context.Bind();
                Object.Instantiate(originalEvents).name = "EventSystem";
                Object.Instantiate(originalSun).name = "Moonlight";
                RenderSettings.ambientLight = ambient; RenderSettings.ambientMode = ambientMode;
                Box(root, "Floor", new Vector3(0,-0.25f,14.5f), new Vector3(18,0.5f,17), "Slate floor");
                Box(root, "West wall", new Vector3(-9,2,14.5f), new Vector3(.4f,4,17));
                Box(root, "East wall", new Vector3(9,2,14.5f), new Vector3(.4f,4,17));
                Box(root, "South wall", new Vector3(0,2,6), new Vector3(18,4,.4f));
                Box(root, "North wall", new Vector3(0,2,23), new Vector3(18,4,.4f));
                Box(root, "Divider south", new Vector3(-3,2,8.5f), new Vector3(.4f,4,5));
                Box(root, "Divider north", new Vector3(-3,2,18), new Vector3(.4f,4,10));
                Box(root, "Door lintel", new Vector3(-3,3.6f,12), new Vector3(.4f,.8f,2));
                Box(root, "Second wall", new Vector3(3,2,14.5f), new Vector3(.4f,4,17));
                var door = Spawn("Door", new Vector3(-3,0,13)).GetComponent<ThiefInteractable>();
                door.name = "Validation Door4"; door.transform.rotation = Quaternion.Euler(0,90,0);
                door.requiresKey = false; door.transform.SetParent(root);
                var device = Spawn("Resonator", new Vector3(-4.3f,1,12)); device.name = "Validation Resonator";
                device.transform.SetParent(root);
                Spawn("Collectible", new Vector3(-7,1.45f,20)).transform.SetParent(root);
                Spawn("Exit", new Vector3(-6,.5f,6.5f)).transform.SetParent(root);
                var leaves = door.GetComponentsInChildren<Collider>();
                foreach (var leaf in leaves) leaf.gameObject.layer = 0;
                var surface = root.gameObject.AddComponent<NavMeshSurface>();
                surface.collectObjects = CollectObjects.Children; surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
                surface.layerMask = 1 << 8; surface.BuildNavMesh();
                if (!surface.navMeshData) throw new InvalidOperationException("NavMesh bake produced no data.");
                AssetDatabase.CreateAsset(surface.navMeshData, "Assets/Game/Stages/Stage2/Navigation/JinHoSystemsValidationNavMesh.asset");
                foreach (var leaf in leaves) leaf.gameObject.layer = 8;
                var g1 = Spawn("Guard", new Vector3(1,0,12)).GetComponent<GuardAgent>(); g1.name = "G1";
                var g2 = Spawn("Guard", new Vector3(1,0,18.5f)).GetComponent<GuardAgent>(); g2.name = "G2";
                context.Bind();
                var propagation = systems.AddComponent<JinHoSoundPropagation>();
                propagation.regions = new[] {
                    Region("Sound room", -9,-3,6,23), Region("Guard hall", -3,3,6,23), Region("Beyond second wall", 3,9,6,23)
                };
                propagation.connections = new[] {
                    new AcousticConnection { name="Door4",regionA=0,regionB=1,point=new Vector3(-3,1,12),door=door },
                    new AcousticConnection { name="Room wall",regionA=0,regionB=1,point=new Vector3(-3,1,18),wall=true },
                    new AcousticConnection { name="Second wall",regionA=1,regionB=2,point=new Vector3(3,1,18),wall=true }
                };
                propagation.traceSignals = true;
                var bindings = systems.AddComponent<JinHoStageBindings>();
                bindings.context=context; bindings.propagation=propagation; bindings.guard1=g1; bindings.guard2=g2; bindings.traceLifecycle=true;
                var errors = context.Validate(); errors.AddRange(propagation.Validate());
                if (errors.Count != 0) throw new InvalidOperationException(string.Join("\n",errors));
                if (!EditorSceneManager.SaveScene(scene,ValidationPath)) throw new InvalidOperationException("Validation scene save failed.");
                return "Created " + ValidationPath + "; original scene preserved; Build Settings untouched.";
            }
            finally
            {
                EditorSceneManager.CloseScene(scene,true);
                SceneManager.SetActiveScene(previous);
            }
        }
        static AcousticRegion Region(string name,float xmin,float xmax,float zmin,float zmax)
            => new AcousticRegion { name=name,min=new Vector2(xmin,zmin),max=new Vector2(xmax,zmax) };
        static GameObject Spawn(string name,Vector3 position)
        {
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs+name+".prefab");
            if (!asset) throw new InvalidOperationException("Missing shared prefab: "+name);
            var obj=(GameObject)PrefabUtility.InstantiatePrefab(asset); obj.transform.position=position; return obj;
        }
        static void Box(Transform parent,string name,Vector3 position,Vector3 size,string material="Midnight stone")
        {
            var box=GameObject.CreatePrimitive(PrimitiveType.Cube); box.name=name; box.layer=8;
            box.transform.SetParent(parent); box.transform.position=position; box.transform.localScale=size;
            box.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>("Assets/Game/Generated/"+material+".mat");
        }
    }
}

