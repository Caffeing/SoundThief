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
    public static class ManorRedesign
    {
        const string ScenePath="Assets/Scenes/ManorPrototype.unity";
        const string BackupPath="Assets/Scenes/ManorPrototype-before-redesign.unity";
        const string NavPath="Assets/SoundThief/Generated/ManorRedesignNavMesh.asset";
        static Transform root;
        static Material wall,floor,wood,brass;
        static GameObject Box(string name,Vector3 p,Vector3 s,Material material,bool solid=true,Transform parent=null)
        {
            var go=GameObject.CreatePrimitive(PrimitiveType.Cube);go.name=name;go.transform.SetParent(parent?parent:root);
            go.transform.position=p;go.transform.localScale=s;go.layer=solid?8:0;go.GetComponent<Renderer>().sharedMaterial=material;
            if(!solid)UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());return go;
        }
        static GameObject Find(string name)=>root.GetComponentsInChildren<Transform>(true).Select(t=>t.gameObject).First(o=>o.name==name);
        static void Shape(Transform parent,string name,PrimitiveType type,Vector3 local,Vector3 size,Material mat)
        {
            var go=GameObject.CreatePrimitive(type);go.name=name;go.transform.SetParent(parent,false);go.transform.localPosition=local;
            go.transform.localScale=size;go.GetComponent<Renderer>().sharedMaterial=mat;UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
        }
        public static string Build()
        {
            if(UnityEditor.EditorApplication.isPlaying)throw new InvalidOperationException("Stop play mode before redesign.");
            var scene=SceneManager.GetSceneByPath(ScenePath);var previous=SceneManager.GetActiveScene();bool opened=!scene.IsValid()||!scene.isLoaded;
            if(!opened && scene.isDirty)throw new InvalidOperationException("Manor has unsaved changes; preserve them before redesign.");
            if(File.Exists(BackupPath)||File.Exists(NavPath))throw new InvalidOperationException("Backup or navmesh already exists; refusing overwrite.");
            if(!AssetDatabase.CopyAsset(ScenePath,BackupPath))throw new InvalidOperationException("Could not back up original scene.");
            if(opened)scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            root=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).First(t=>t.name=="03 저택 구조");
            wall=AssetDatabase.LoadAssetAtPath<Material>("Assets/SoundThief/Generated/Midnight stone.mat");
            floor=AssetDatabase.LoadAssetAtPath<Material>("Assets/SoundThief/Generated/Slate floor.mat");
            wood=AssetDatabase.LoadAssetAtPath<Material>("Assets/SoundThief/Generated/Old oak.mat");
            brass=AssetDatabase.LoadAssetAtPath<Material>("Assets/SoundThief/Generated/Warm brass.mat");
            var baseFloor=Find("바닥 충돌 기반");baseFloor.transform.position=new Vector3(0,-0.25f,13);baseFloor.transform.localScale=new Vector3(32,0.5f,32);
            var west=Find("서쪽 벽");west.transform.position=new Vector3(-16,2,13);west.transform.localScale=new Vector3(.4f,4,32);
            var east=Find("동쪽 벽");east.transform.position=new Vector3(16,2,13);east.transform.localScale=new Vector3(.4f,4,32);
            var north=Find("북쪽 벽");north.transform.position=new Vector3(0,2,29);north.transform.localScale=new Vector3(32,4,.4f);
            var south=Find("남쪽 벽");south.transform.position=new Vector3(0,2,-3);south.transform.localScale=new Vector3(32,4,.4f);
            var area=new GameObject("방 구조 충돌체");area.transform.SetParent(root,false);
            foreach(float side in new[]{-1f,1f})
            {
                float x=side*9;
                Box("방 앞쪽 칸막이",new Vector3(x,2,7.5f),new Vector3(.4f,4,3),wall,true,area.transform);
                Box("방 중앙 칸막이",new Vector3(x,2,15),new Vector3(.4f,4,6),wall,true,area.transform);
                Box("방 뒤쪽 칸막이",new Vector3(x,2,22.5f),new Vector3(.4f,4,3),wall,true,area.transform);
                Box("방 앞쪽 벽",new Vector3(side*12.5f,2,6),new Vector3(7,4,.4f),wall,true,area.transform);
                Box("방 뒤쪽 벽",new Vector3(side*12.5f,2,24),new Vector3(7,4,.4f),wall,true,area.transform);
                Box("앞쪽 문 위 인방",new Vector3(x,3.65f,10.5f),new Vector3(.5f,.7f,3),wall,true,area.transform);
                Box("뒤쪽 문 위 인방",new Vector3(x,3.65f,19.5f),new Vector3(.5f,.7f,3),wall,true,area.transform);
            }
            Box("서재 책장 북쪽",new Vector3(-15,1.3f,17),new Vector3(.6f,2.6f,5),wood,true,area.transform);
            Box("서재 책장 남쪽",new Vector3(-15,1.3f,10),new Vector3(.6f,2.6f,4),wood,true,area.transform);
            Box("서재 책상",new Vector3(-12.8f,.55f,20.5f),new Vector3(2.1f,1.1f,1.2f),wood,true,area.transform);
            Box("서재 융단",new Vector3(-12,.025f,15),new Vector3(5,.04f,9),wood,false,area.transform);
            Box("식당 탁자",new Vector3(12.5f,.65f,16),new Vector3(2,1.3f,4.6f),wood,true,area.transform);
            for(int i=0;i<3;i++)Box("식당 의자",new Vector3(14.5f,.45f,14.5f+i*1.5f),new Vector3(.7f,.9f,.7f),wood,true,area.transform);
            Box("식당 융단",new Vector3(12.5f,.02f,16),new Vector3(5,.03f,9),wood,false,area.transform);
            var resonators=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<ThiefInteractable>(true)).Where(i=>i.kind==InteractionKind.Resonator).OrderBy(i=>i.name).ToArray();
            if(resonators.Length!=2)throw new InvalidOperationException("Expected exactly two existing targets; backup retained.");
            var clock=resonators[0];clock.name="01 서재 괘종시계";clock.displayName="서재의 태엽 시계";clock.relation.secondMedium="태엽 시계";
            clock.transform.position=new Vector3(-12,1,14);clock.transform.localScale=Vector3.one;
            clock.GetComponent<Renderer>().enabled=false;clock.GetComponent<BoxCollider>().size=new Vector3(.85f,1.6f,.6f);
            Shape(clock.transform,"시계 몸체",PrimitiveType.Cube,new Vector3(0,0,0),new Vector3(.8f,1.4f,.5f),wood);
            Shape(clock.transform,"시계 문자판",PrimitiveType.Sphere,new Vector3(0,.25f,-.27f),new Vector3(.5f,.5f,.08f),brass);
            Shape(clock.transform,"시계 분침 바늘",PrimitiveType.Cube,new Vector3(0,.33f,-.33f),new Vector3(.025f,.2f,.025f),wall);
            Shape(clock.transform,"시계 시침 바늘",PrimitiveType.Cube,new Vector3(.07f,.25f,-.33f),new Vector3(.14f,.025f,.025f),wall);
            var armor=resonators[1];armor.name="02 식당 장식 갑옷";armor.displayName="식당의 갑옷 장식";armor.relation.secondMedium="갑옷 장식";
            armor.transform.position=new Vector3(12,1,13.5f);armor.transform.localScale=Vector3.one;
            armor.GetComponent<Renderer>().enabled=false;armor.GetComponent<BoxCollider>().size=new Vector3(.9f,1.7f,.6f);
            Shape(armor.transform,"갑옷 몸통",PrimitiveType.Cube,new Vector3(0,0,0),new Vector3(.75f,.85f,.45f),wall);
            Shape(armor.transform,"갑옷 투구",PrimitiveType.Sphere,new Vector3(0,.65f,0),new Vector3(.5f,.5f,.5f),brass);
            Shape(armor.transform,"갑옷 왼쪽 다리",PrimitiveType.Cube,new Vector3(-.18f,-.65f,0),new Vector3(.23f,.65f,.3f),wall);
            Shape(armor.transform,"갑옷 오른쪽 다리",PrimitiveType.Cube,new Vector3(.18f,-.65f,0),new Vector3(.23f,.65f,.3f),wall);
            Shape(armor.transform,"갑옷 왼쪽 팔",PrimitiveType.Cube,new Vector3(-.49f,-.06f,0),new Vector3(.22f,.85f,.3f),wall);
            Shape(armor.transform,"갑옷 오른쪽 팔",PrimitiveType.Cube,new Vector3(.49f,-.06f,0),new Vector3(.22f,.85f,.3f),wall);
            Shape(armor.transform,"투구 얼굴가림",PrimitiveType.Cube,new Vector3(0,.64f,-.24f),new Vector3(.32f,.09f,.04f),wall);
            Shape(armor.transform,"갑옷 받침대",PrimitiveType.Cube,new Vector3(0,-.96f,0),new Vector3(.9f,.08f,.65f),wood);
            foreach(var stand in root.GetComponentsInChildren<Transform>(true).Where(t=>t.name=="소리 사물 받침"))stand.gameObject.SetActive(false);
            foreach(var lamp in root.GetComponentsInChildren<Light>(true))
                if(lamp.color.g>.9f && lamp.color.r<.2f) lamp.color=new Color(1f,.65f,.3f);
            var surface=root.GetComponent<NavMeshSurface>();var leaf=Find("현관 문짝");int layer=leaf.layer;leaf.layer=0;
            surface.RemoveData();surface.BuildNavMesh();leaf.layer=layer;
            if(!surface.navMeshData)throw new InvalidOperationException("NavMesh bake failed; original backup retained.");
            AssetDatabase.CreateAsset(surface.navMeshData,NavPath);EditorUtility.SetDirty(surface);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            if(opened){EditorSceneManager.CloseScene(scene,true);if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);}
            return "Stage 1 rooms created; original scene backed up at "+BackupPath;
        }
    }
}
