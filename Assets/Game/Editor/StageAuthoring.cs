using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.AI.Navigation;

namespace SoundThief.Editor
{
    public static class StageAuthoring
    {
        public const string Prefabs = "Assets/Game/Prefabs/";
        public const string Example = "Assets/Scenes/Stages/FrameworkExample.unity";
        public static GameObject Spawn(string name, Vector3 position)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + name + ".prefab");
            if (!asset) throw new InvalidOperationException("Missing shared prefab: " + name);
            var obj = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            Undo.RegisterCreatedObjectUndo(obj, "Place shared stage object"); obj.transform.position = position;
            PrefabUtility.RecordPrefabInstancePropertyModifications(obj.transform); return obj;
        }
        [MenuItem("Sound Thief/Stage/Install Foundation In Current Scene")]
        public static void Install()
        {
            var scene = SceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(scene.path)) throw new InvalidOperationException("Save the scene before installing the foundation.");
            if (scene.GetRootGameObjects().Any(o => o.GetComponentInChildren<StageContext>(true) || o.GetComponentInChildren<ThiefPlayer>(true) || o.GetComponentInChildren<MissionDirector>(true)))
                throw new InvalidOperationException("Scene already contains a foundation/player; refusing to duplicate it.");
            var systems = Spawn("StageSystems", Vector3.zero);
            var player = Spawn("PlayerRig", new Vector3(0,0.05f,0)).GetComponent<ThiefPlayer>();
            var context = systems.GetComponent<StageContext>(); context.player = player;
            var definition = ScriptableObject.CreateInstance<StageDefinition>(); definition.stageId = Path.GetFileNameWithoutExtension(scene.path);
            definition.title = definition.stageId;
            AssetDatabase.CreateAsset(definition, AssetDatabase.GenerateUniqueAssetPath(Path.ChangeExtension(scene.path, null) + " Definition.asset"));
            context.definition = definition; context.Bind();
            RecordContext(context);
            EditorSceneManager.MarkSceneDirty(scene); RegisterScene(scene.path); Selection.activeGameObject = systems;
        }
        [MenuItem("Sound Thief/Stage/Validate Current Scene")]
        public static void ValidateCurrent()
        {
            var contexts = SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<StageContext>(true)).ToArray();
            if (contexts.Length != 1) { Debug.LogError("StageContext must exist exactly once."); return; }
            var errors = contexts[0].Validate();
            foreach (var error in errors) Debug.LogError(error, contexts[0]);
            if (errors.Count == 0) Debug.Log("STAGE_VALID: " + contexts[0].definition.stageId, contexts[0]);
        }
        public static void RegisterScene(string path)
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (!scenes.Any(s=>s.path == path)) scenes.Add(new EditorBuildSettingsScene(path,true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
        public static void RecordContext(StageContext context)
        {
            foreach(var component in new Component[]{context,context.mission,context.mission.World,context.player,context.hud})
            { PrefabUtility.RecordPrefabInstancePropertyModifications(component); EditorUtility.SetDirty(component); }
        }
        static void SavePrefab(GameObject source, string name, Action<GameObject> prepare = null)
        {
            var path = Prefabs + name + ".prefab";
            if (File.Exists(path)) return;
            var copy = UnityEngine.Object.Instantiate(source); copy.name = name; copy.transform.position = Vector3.zero;
            prepare?.Invoke(copy); PrefabUtility.SaveAsPrefabAsset(copy,path); UnityEngine.Object.DestroyImmediate(copy);
        }
        public static void BuildSharedAssets()
        {
            Directory.CreateDirectory(Prefabs); Directory.CreateDirectory("Assets/Scenes/Stages"); AssetDatabase.Refresh();
            var previous = SceneManager.GetActiveScene();
            var manor = EditorSceneManager.OpenScene(ManorSceneBuilder.ScenePath,OpenSceneMode.Additive); SceneManager.SetActiveScene(manor);
            var mission = manor.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<MissionDirector>(true)).First();
            var hud = manor.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<ThiefHud>(true)).First();
            SavePrefab(mission.player.gameObject,"PlayerRig",o=> { o.GetComponent<ThiefPlayer>().Mission=null; if(!o.GetComponent<StageInventory>()) o.AddComponent<StageInventory>(); });
            SavePrefab(mission.gameObject,"StageSystems",o=> {
                var director=o.GetComponent<MissionDirector>(); director.player=null; director.stage=null; director.startsAccepted=false;
                o.GetComponent<SoundWorld>().player=null;
                var ui=UnityEngine.Object.Instantiate(hud.gameObject,o.transform).GetComponent<ThiefHud>(); ui.mission=director;
                var context=o.AddComponent<StageContext>(); context.mission=director; context.hud=ui; context.rules=director.rules;
            });
            foreach(var item in manor.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<ThiefInteractable>(true)))
            {
                string name=item.kind==InteractionKind.Key?"Key":item.kind==InteractionKind.Door?"Door":item.kind==InteractionKind.Treasure?"Collectible":item.kind==InteractionKind.Exit?"Exit":"Resonator";
                SavePrefab(item.gameObject,name,o=> { var i=o.GetComponent<ThiefInteractable>(); if(name=="Collectible") {i.kind=InteractionKind.Collectible;i.itemId="objective";} });
            }
            var guard=manor.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<GuardAgent>(true)).First();
            SavePrefab(guard.gameObject,"Guard",o=>o.GetComponent<GuardAgent>().world=null);
            var contextManor=mission.GetComponent<StageContext>() ?? mission.gameObject.AddComponent<StageContext>();
            const string definitionPath="Assets/Game/Generated/Manor Definition.asset";
            var definition=AssetDatabase.LoadAssetAtPath<StageDefinition>(definitionPath);
            if(!definition) {definition=ScriptableObject.CreateInstance<StageDefinition>();definition.stageId="manor";definition.title="푸른 저택의 인장";definition.briefing="열쇠를 찾아 저택에 잠입하고 가문의 인장을 회수한 뒤 입구로 돌아오세요.";definition.objectiveText="열쇠와 소리를 이용해 가문의 인장을 회수하세요.";AssetDatabase.CreateAsset(definition,definitionPath);}
            contextManor.definition=definition;contextManor.rules=mission.rules;contextManor.mission=mission;contextManor.player=mission.player;contextManor.hud=hud;
            if(!mission.player.GetComponent<StageInventory>())mission.player.gameObject.AddComponent<StageInventory>();
            contextManor.Bind();EditorSceneManager.SaveScene(manor);
            EditorSceneManager.CloseScene(manor,true); if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);
            AssetDatabase.SaveAssets();
        }
        public static void BuildExample()
        {
            if(File.Exists(Example))return;
            var previous=SceneManager.GetActiveScene(); var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
            EditorSceneManager.SaveScene(scene,Example); Install();
            var context=UnityEngine.Object.FindObjectsByType<StageContext>(FindObjectsSortMode.None).First(c=>c.gameObject.scene==scene);
            context.definition.title="공통 기반 검증 — 두 유물";context.definition.requiredItemIds=new[]{"relic_a","relic_b"};context.definition.objectiveText="서로 다른 두 유물을 회수하세요.";
            EditorUtility.SetDirty(context.definition);
            var floor=GameObject.CreatePrimitive(PrimitiveType.Cube);floor.name="Geometry - floor";floor.layer=8;floor.transform.position=new Vector3(0,-0.25f,5);floor.transform.localScale=new Vector3(14,0.5f,20);
            for(int i=0;i<2;i++) {var item=Spawn("Collectible",new Vector3(i==0?-2:2,1,8+i*3)).GetComponent<ThiefInteractable>();item.itemId=i==0?"relic_a":"relic_b";item.displayName=i==0?"붉은 유물":"푸른 유물";PrefabUtility.RecordPrefabInstancePropertyModifications(item);}
            var key=Spawn("Key",new Vector3(-2,1,2)).GetComponent<ThiefInteractable>();key.itemId="copper_key";key.displayName="구리 열쇠";
            PrefabUtility.RecordPrefabInstancePropertyModifications(key);
            var door=Spawn("Door",new Vector3(-1,0,5)).GetComponent<ThiefInteractable>();door.requiredItemId="copper_key";door.displayName="구리 열쇠 문";
            PrefabUtility.RecordPrefabInstancePropertyModifications(door);
            Spawn("Exit",new Vector3(0,0.5f,-3));Spawn("Resonator",new Vector3(-4,1,8));Spawn("Resonator",new Vector3(4,1,8));
            var surface=floor.AddComponent<NavMeshSurface>();surface.useGeometry=UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;surface.layerMask=1<<8;surface.BuildNavMesh();
            if(surface.navMeshData)AssetDatabase.CreateAsset(surface.navMeshData,"Assets/Scenes/Stages/FrameworkExample NavMesh.asset");
            Spawn("Guard",new Vector3(4,0,12));context.Bind();RecordContext(context);
            var light=new GameObject("Example daylight").AddComponent<Light>();light.type=LightType.Directional;light.transform.rotation=Quaternion.Euler(50,-30,0);light.intensity=1.3f;
            RenderSettings.ambientLight=new Color(0.5f,0.5f,0.5f);
            EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();EditorSceneManager.CloseScene(scene,true);if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);
        }
    }
}
