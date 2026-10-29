using System.Linq;
namespace SoundThief.Editor
{
    public static class ManorExpandRooms
    {
        public static object Apply()
        {
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(scene.path!="Assets/Scenes/ManorPrototype.unity"||scene.isDirty||UnityEditor.EditorApplication.isPlaying)throw new System.Exception("Unexpected scene state; changes not applied.");
var root=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<UnityEngine.Transform>(true)).First(t=>t.name=="03 저택 구조");
var area=root.Find("방 구조 충돌체");if(!area||area.Find("서재 벽감 바깥 벽"))throw new System.Exception("Room layout missing or already expanded.");
var wall=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/SoundThief/Generated/Midnight stone.mat");
var wood=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/SoundThief/Generated/Old oak.mat");
var brass=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>("Assets/SoundThief/Generated/Warm brass.mat");
System.Func<string,UnityEngine.GameObject> find=name=>root.GetComponentsInChildren<UnityEngine.Transform>(true).First(t=>t.name==name).gameObject;
System.Action<string,UnityEngine.Vector3,UnityEngine.Vector3,UnityEngine.Material> box=(name,pos,size,mat)=>{var o=UnityEngine.GameObject.CreatePrimitive(UnityEngine.PrimitiveType.Cube);o.name=name;o.transform.SetParent(area);o.transform.position=pos;o.transform.localScale=size;o.layer=8;o.GetComponent<UnityEngine.Renderer>().sharedMaterial=mat;};
var ground=find("바닥 충돌 기반");ground.transform.localScale=new UnityEngine.Vector3(44,.5f,32);
foreach(var name in new[]{"북쪽 벽","남쪽 벽"}){var o=find(name);o.transform.localScale=new UnityEngine.Vector3(44,4,.4f);}
var west=find("서쪽 벽");west.transform.position=new UnityEngine.Vector3(-16,2,6);west.transform.localScale=new UnityEngine.Vector3(.4f,4,18);
box("서쪽 바깥 뒤쪽 벽",new UnityEngine.Vector3(-16,2,26.5f),new UnityEngine.Vector3(.4f,4,5),wall);
box("서재 벽감 바깥 벽",new UnityEngine.Vector3(-22,2,19.5f),new UnityEngine.Vector3(.4f,4,9),wall);
box("서재 벽감 남쪽 벽",new UnityEngine.Vector3(-19,2,15),new UnityEngine.Vector3(6,4,.4f),wall);
box("서재 벽감 북쪽 벽",new UnityEngine.Vector3(-19,2,24),new UnityEngine.Vector3(6,4,.4f),wall);
var east=find("동쪽 벽");east.transform.position=new UnityEngine.Vector3(16,2,1.5f);east.transform.localScale=new UnityEngine.Vector3(.4f,4,9);
box("동쪽 바깥 뒤쪽 벽",new UnityEngine.Vector3(16,2,22),new UnityEngine.Vector3(.4f,4,14),wall);
box("식당 벽감 바깥 벽",new UnityEngine.Vector3(22,2,10.5f),new UnityEngine.Vector3(.4f,4,9),wall);
box("식당 벽감 남쪽 벽",new UnityEngine.Vector3(19,2,6),new UnityEngine.Vector3(6,4,.4f),wall);
box("식당 벽감 북쪽 벽",new UnityEngine.Vector3(19,2,15),new UnityEngine.Vector3(6,4,.4f),wall);
box("서재 벽감 책장",new UnityEngine.Vector3(-21,1.3f,19),new UnityEngine.Vector3(.6f,2.6f,5),wood);
var desk=find("서재 책상");desk.transform.position=new UnityEngine.Vector3(-19,.55f,21);
var key=find("식당 열쇠");key.transform.position=new UnityEngine.Vector3(-19,1.22f,21);
box("식당 전시 받침",new UnityEngine.Vector3(19,.55f,10),new UnityEngine.Vector3(1.4f,1.1f,1.4f),wood);
var seal=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<SoundThief.ThiefInteractable>(true)).First(i=>i.kind==SoundThief.InteractionKind.Treasure);seal.transform.position=new UnityEngine.Vector3(19,1.35f,10);
var guards=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<SoundThief.GuardAgent>(true)).ToArray();
foreach(var guard in guards){if(guard.name=="서재 순찰 경비")guard.transform.position=new UnityEngine.Vector3(-18,0,18);if(guard.name=="식당 경비")guard.transform.position=new UnityEngine.Vector3(18,0,12.5f);UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(guard.transform);}
foreach(var position in new[]{new UnityEngine.Vector3(-19,2.8f,19),new UnityEngine.Vector3(19,2.8f,10)}){var l=new UnityEngine.GameObject("벽감 초 조명").AddComponent<UnityEngine.Light>();l.transform.SetParent(area);l.transform.position=position;l.type=UnityEngine.LightType.Point;l.color=new UnityEngine.Color(1,.65f,.3f);l.intensity=4;l.range=7;}
var surface=root.GetComponent<Unity.AI.Navigation.NavMeshSurface>();var old=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.AI.NavMeshData>("Assets/SoundThief/Generated/ManorRedesignNavMesh.asset");
var main=find("현관 문짝");var dining=find("식당 문 충돌체");int a=main.layer,b=dining.layer;main.layer=0;dining.layer=0;surface.RemoveData();surface.BuildNavMesh();main.layer=a;dining.layer=b;
var baked=surface.navMeshData;if(!baked||!old)throw new System.Exception("NavMesh build failed.");surface.RemoveData();UnityEditor.EditorUtility.CopySerialized(baked,old);surface.navMeshData=old;surface.AddData();UnityEditor.EditorUtility.SetDirty(old);UnityEditor.EditorUtility.SetDirty(surface);if(baked!=old&&!UnityEditor.AssetDatabase.Contains(baked))UnityEngine.Object.DestroyImmediate(baked);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);UnityEditor.AssetDatabase.SaveAssets();
return new {studyKey=key.transform.position.ToString(),seal=seal.transform.position.ToString(),size=ground.transform.localScale.ToString()};

        }
    }
}
