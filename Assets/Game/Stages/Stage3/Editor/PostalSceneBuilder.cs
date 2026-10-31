using System;
using System.IO;
using System.Linq;
using TMPro;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace SoundThief.PostalDepot.Editor
{
    public static class PostalSceneBuilder
    {
        public const string Folder = "Assets/Game/Stages/Stage3";
        public const string ScenePath = "Assets/Scenes/Stages/Stage3/PostalDepot.unity";
        static Material stone, floor, wood, cream, teal, gold, red;

        [MenuItem("Sound Thief/Postal Depot/Create Or Open Graybox")]
        public static void Create()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
            if (File.Exists(ScenePath)) { EditorSceneManager.OpenScene(ScenePath); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var builder = new Layout();
            builder.Build();
            PostalSearchAuthoring.Apply();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Selection.activeGameObject = builder.context.player.gameObject;
            if (SceneView.lastActiveSceneView)
                SceneView.lastActiveSceneView.LookAt(new Vector3(0, 0, 7), Quaternion.Euler(62, 0, 0), 24);
            Debug.Log("POSTAL_DEPOT_READY: " + ScenePath + " (shared build list unchanged)");
        }

        sealed class Layout
        {
            Transform geometry, props;
            public StageContext context;
            Material Material(string name, Color color)
            {
                string path = Folder + "/Materials/" + name + ".mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material) return material;
                material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = color };
                AssetDatabase.CreateAsset(material, path); return material;
            }
            public void Build()
            {
                Directory.CreateDirectory(Folder + "/Settings"); Directory.CreateDirectory(Folder + "/Navigation"); Directory.CreateDirectory("Assets/Scenes/Stages/Stage3"); Directory.CreateDirectory(Folder + "/Materials"); AssetDatabase.Refresh();
                stone = Material("Ink blue", new Color(0.22f, 0.30f, 0.34f));
                floor = Material("Warm floor", new Color(0.48f, 0.44f, 0.36f));
                wood = Material("Parcel cardboard", new Color(0.55f, 0.35f, 0.18f));
                cream = Material("Paper cream", new Color(0.86f, 0.81f, 0.65f));
                teal = Material("Sound teal", new Color(0.12f, 0.70f, 0.62f));
                gold = Material("Route yellow", new Color(0.95f, 0.67f, 0.18f));
                red = Material("Seal red", new Color(0.75f, 0.20f, 0.17f));
                geometry = new GameObject("01 Geometry - replace with modular art").transform;
                props = new GameObject("02 Postal props and signage").transform;
                Box("Floor", new Vector3(0,-0.25f,7), new Vector3(16,0.5f,28), floor);
                Wall("West", -8,7,0.3f,28); Wall("East",8,7,0.3f,28);
                Wall("North",0,21,16,0.3f); Wall("Yard south",0,-7,16,0.3f);
                // Reception entry at x=-4, plus the side loading route at x=5.
                Wall("Front left",-6.5f,0,3,0.3f); Wall("Front center",0.5f,0,7,0.3f); Wall("Front right",7,0,2,0.3f);
                // Sorting door at x=-3, width 2; upper third is a viewing opening.
                Wall("Sorting west",-6,6,4,0.3f); Wall("Sorting east",0,6,4,0.3f);
                Wall("Dispatch west",-6,14,4,0.3f); Wall("Dispatch center",0,14,4,0.3f);
                Wall("Dispatch right",7,14,2,0.3f);
                // Partition with two doorways (z=3 and 11) makes a loop, not a dead end.
                Wall("Service partition south",2,0.8f,0.3f,1.6f);
                Wall("Service partition middle",2,7,0.3f,6);
                Wall("Service partition north",2,13,0.3f,2);
                Box("Reception counter",new Vector3(-5,0.55f,2.6f),new Vector3(4,1.1f,0.9f),wood);
                Box("Sorting worktable",new Vector3(-4.7f,0.55f,10f),new Vector3(3,1.1f,1.2f),wood);
                Box("Sorting divider",new Vector3(-1,0.85f,10.8f),new Vector3(1,1.7f,3),wood);
                Box("Loading pallets",new Vector3(5.6f,0.6f,6.5f),new Vector3(2.8f,1.2f,3),wood);
                for(int i=0;i<3;i++)
                {
                    float x=-5+i*4;
                    Box("Dispatch shelf "+(char)('A'+i),new Vector3(x,0.55f,i==1?20.6f:18.5f),new Vector3(2.6f,1.1f,1.2f),wood);
                    Sign("BAY "+(char)('A'+i),new Vector3(x,2.2f,i==1?20.7f:19.3f),0,0.65f);
                    for(int j=0;j<3;j++) Box("Parcel "+i+"-"+j,new Vector3(x-0.8f+j*0.8f,1.3f,i==1?20.8f:18.7f),new Vector3(0.55f,0.4f,0.5f),cream);
                }
                var systems = Spawn("StageSystems","Postal stage systems",Vector3.zero);
                var player = Spawn("PlayerRig","Postal player",new Vector3(-4,0.05f,-4)).GetComponent<ThiefPlayer>();
                context=systems.GetComponent<StageContext>(); context.player=player;
                var definition=ScriptableObject.CreateInstance<StageDefinition>();
                definition.stageId="gganwoor-postal-depot"; definition.title="아직 보내지 마세요!";
                definition.briefing="홧김에 보낸 편지를 되찾아 주세요. 접수대의 직원 열쇠를 찾고, 분류 작업실의 분류표로 목적지를 확인해 주세요. 편지 앞의 졸고 있는 직원을 소리로 유인한 뒤 회수해 앞마당으로 돌아오세요. 실제 제한 시간은 없습니다.";
                definition.requiredItemIds=new[]{PostalBriefing.ManifestId,PostalBriefing.LetterId};
                definition.objectiveText="분류표를 확인하고 붉은 봉인 편지를 회수하세요.";
                definition.exitText="앞마당의 초록 출구에서 F로 탈출하세요.";
                AssetDatabase.CreateAsset(definition,Folder+"/Settings/PostalDepot Definition.asset");
                context.definition=definition;
                Item("Key","Staff key",new Vector3(-5,1.3f,2.6f),"직원 열쇠","postal_staff_key");
                Item("Collectible","Route manifest",new Vector3(-4.7f,1.28f,9.9f),"분류표 확인 · B구역 / 붉은 봉인",PostalBriefing.ManifestId,new Vector3(0.6f,0.06f,0.4f),gold);
                Item("Collectible","Letter B",new Vector3(-1,1.3f,20.35f),"붉은 봉인 편지",PostalBriefing.LetterId,new Vector3(0.55f,0.12f,0.35f),red);
                var door=Spawn("Door","Sorting room bell door",new Vector3(-4,0,6)).GetComponent<ThiefInteractable>();
                door.requiredItemId="postal_staff_key"; door.displayName="분류실 문 · 문과 종";
                door.relation.firstMedium="출입문";door.relation.secondMedium="종";door.relation.grade=2;
                DoorMaterial(door);
                Resonator("Sorting stamp",new Vector3(-6.5f,1.1f,11.7f),"작업대 · 손 / 날짜 도장",2);
                Resonator("Service receiver",new Vector3(4,1.0f,10.8f),"하역 종 · 주입 대상",1);
                Resonator("Dispatch receiver",new Vector3(-2.7f,1,16.4f),"빈 우편함 · 주입 대상",1);
                Resonator("Dispatch source",new Vector3(-6.5f,1,16.2f),"분류 손잡이 · 소리 원천",1);
                var exit=Spawn("Exit","Yard exit",new Vector3(-4,0.15f,-5.7f)).GetComponent<ThiefInteractable>();
                exit.displayName="분류소 밖으로 탈출";
                foreach(var renderer in exit.GetComponentsInChildren<Renderer>()) {renderer.sharedMaterial=teal;PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);}
                var surface=geometry.gameObject.AddComponent<NavMeshSurface>();
                surface.collectObjects=CollectObjects.Children;surface.useGeometry=NavMeshCollectGeometry.PhysicsColliders;surface.layerMask=1<<8;
                surface.BuildNavMesh();
                AssetDatabase.CreateAsset(surface.navMeshData,Folder+"/Navigation/PostalDepot NavMesh.asset");
                Spawn("Guard","Sorting guard",new Vector3(-7,0,8));
                Spawn("Guard","Dispatch guard",new Vector3(-1,0,18.2f));
                context.Bind(); SoundThief.Editor.StageAuthoring.RecordContext(context);
                foreach(var item in SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<ThiefInteractable>(true)))
                {PrefabUtility.RecordPrefabInstancePropertyModifications(item);if(item.relation)PrefabUtility.RecordPrefabInstancePropertyModifications(item.relation);}
                systems.AddComponent<PostalBriefing>().mission=context.mission;
                Sign("POST OFFICE / AFTER HOURS",new Vector3(-4,3.2f,-0.3f),0,0.5f);
                Sign("01 RECEPTION",new Vector3(-4,2.6f,5.8f),0,0.45f);
                Sign("02 SORTING / ROUTE NOTE",new Vector3(-4.8f,2.1f,9.8f),0,0.4f);
                Sign("03 DISPATCH",new Vector3(-1,3.2f,20.7f),0,0.65f);
                Sign("SERVICE ROUTE >",new Vector3(1,2.4f,3),0,0.35f);
                Sign("EXIT [F]",new Vector3(-4,1,-6.5f),180,0.5f);
                var sun=new GameObject("Warm evening light").AddComponent<Light>();sun.type=LightType.Directional;
                sun.transform.rotation=Quaternion.Euler(48,-35,0);sun.color=new Color(1,0.88f,0.7f);sun.intensity=1.4f;
                RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Flat;RenderSettings.ambientLight=new Color(0.48f,0.49f,0.52f);
                foreach(float z in new[]{3f,10f,17f})
                {var lamp=new GameObject("Work lamp").AddComponent<Light>();lamp.type=LightType.Point;lamp.range=10;lamp.intensity=3;lamp.color=new Color(1,0.8f,0.55f);lamp.transform.position=new Vector3(-3,3.5f,z);}
                var errors=context.Validate();
                if(errors.Count>0) throw new InvalidOperationException(string.Join("\n",errors));
            }
            GameObject Spawn(string prefab,string name,Vector3 position)
            {var obj=SoundThief.Editor.StageAuthoring.Spawn(prefab,position);obj.name=name;return obj;}
            ThiefInteractable Item(string prefab,string name,Vector3 position,string label,string id,Vector3? scale=null,Material material=null)
            {
                var item=Spawn(prefab,name,position).GetComponent<ThiefInteractable>();item.itemId=id;item.displayName=label;
                if(scale.HasValue){item.transform.localScale=scale.Value;PrefabUtility.RecordPrefabInstancePropertyModifications(item.transform);}
                if(material)foreach(var r in item.GetComponentsInChildren<Renderer>()){r.sharedMaterial=material;PrefabUtility.RecordPrefabInstancePropertyModifications(r);}
                return item;
            }
            void Resonator(string name,Vector3 position,string label,int grade)
            {var i=Spawn("Resonator",name,position).GetComponent<ThiefInteractable>();i.displayName=label;i.relation.firstMedium="손";i.relation.secondMedium=name;i.relation.grade=grade;}
            void DoorMaterial(ThiefInteractable item)
            {foreach(var r in item.GetComponentsInChildren<Renderer>()){r.sharedMaterial=wood;PrefabUtility.RecordPrefabInstancePropertyModifications(r);}}
            void Wall(string name,float x,float z,float width,float depth)=>Box(name,new Vector3(x,1.8f,z),new Vector3(width,3.6f,depth),stone);
            GameObject Box(string name,Vector3 position,Vector3 scale,Material material)
            {var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=name;o.transform.SetParent(geometry);o.transform.position=position;o.transform.localScale=scale;o.layer=8;o.GetComponent<Renderer>().sharedMaterial=material;return o;}
            void Sign(string text,Vector3 position,float yaw,float size)
            {var label=new GameObject(text).AddComponent<TextMeshPro>();label.transform.SetParent(props);label.transform.position=position;label.transform.rotation=Quaternion.Euler(0,yaw,0);label.text=text;label.fontSize=size*5;label.alignment=TextAlignmentOptions.Center;label.color=new Color(1,0.91f,0.68f);label.rectTransform.sizeDelta=new Vector2(7,1);}
        }
    }
}
