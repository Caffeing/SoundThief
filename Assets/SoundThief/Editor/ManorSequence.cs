using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.AI.Navigation;
using UnityEngine.AI;

namespace SoundThief.Editor
{
    public static class ManorSequence
    {
        public static string Apply()
        {
            var scene=SceneManager.GetActiveScene();
            if(scene.path!="Assets/Scenes/ManorPrototype.unity"||scene.isDirty||EditorApplication.isPlaying)throw new InvalidOperationException("Open the saved manor in edit mode first.");
            var root=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<Transform>(true)).First(t=>t.name=="03 저택 구조");
            var area=root.Find("방 구조 충돌체");if(!area||area.Find("식당 앞문"))throw new InvalidOperationException("Rooms missing or sequence already applied.");
            var wood=AssetDatabase.LoadAssetAtPath<Material>("Assets/SoundThief/Generated/Old oak.mat");
            var stone=AssetDatabase.LoadAssetAtPath<Material>("Assets/SoundThief/Generated/Midnight stone.mat");
            var brass=AssetDatabase.LoadAssetAtPath<Material>("Assets/SoundThief/Generated/Warm brass.mat");
            Action<string,Vector3,Vector3,Material> box=(name,pos,size,material)=>{
                var o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=name;o.transform.SetParent(area);o.transform.position=pos;o.transform.localScale=size;o.layer=8;o.GetComponent<Renderer>().sharedMaterial=material;};
            box("Dining rear blocked wall",new Vector3(9,2,19.5f),new Vector3(.4f,4,3),stone);
            box("식당 문 앞쪽 기둥",new Vector3(9,1.5f,9.25f),new Vector3(.45f,3,.5f),stone);
            box("식당 문 뒤쪽 기둥",new Vector3(9,1.5f,11.75f),new Vector3(.45f,3,.5f),stone);
            var hinge=new GameObject("식당 앞문");hinge.transform.SetParent(area);hinge.transform.position=new Vector3(9,0,11.5f);hinge.transform.rotation=Quaternion.Euler(0,90,0);
            var leaf=GameObject.CreatePrimitive(PrimitiveType.Cube);leaf.name="식당 문 충돌체";leaf.transform.SetParent(hinge.transform,false);
            leaf.transform.localPosition=new Vector3(1,1.5f,0);leaf.transform.localScale=new Vector3(1.95f,3,.22f);leaf.layer=8;leaf.GetComponent<Renderer>().sharedMaterial=wood;
            var door=hinge.AddComponent<ThiefInteractable>();door.kind=InteractionKind.Door;door.requiredItemId="dining_key";
            door.displayName="식당의 잠긴 문";
            door.relation=hinge.AddComponent<SoundRelation>();door.relation.firstMedium="식당 열쇠";door.relation.secondMedium="식당 자물쇠";door.relation.grade=2;
            var obstacle=hinge.AddComponent<NavMeshObstacle>();obstacle.shape=NavMeshObstacleShape.Box;obstacle.center=new Vector3(1,1.5f,0);obstacle.size=new Vector3(1.95f,3,.35f);obstacle.carving=true;
            var key=GameObject.CreatePrimitive(PrimitiveType.Cube);key.name="식당 열쇠";key.transform.SetParent(area);key.transform.position=new Vector3(-12.8f,1.22f,20.5f);key.transform.localScale=new Vector3(.4f,.15f,.24f);
            key.GetComponent<Renderer>().sharedMaterial=brass;var keyItem=key.AddComponent<ThiefInteractable>();keyItem.kind=InteractionKind.Key;keyItem.itemId="dining_key";keyItem.displayName="식당 열쇠";
            var seal=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<ThiefInteractable>(true)).First(i=>i.kind==InteractionKind.Treasure);
            seal.transform.position=new Vector3(12.5f,1.58f,16);
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SoundThief/Prefabs/Guard.prefab");
            var context=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<StageContext>(true)).First();
            foreach(var entry in new[]{new {name="서재 순찰 경비",pos=new Vector3(-13,0,18)},new {name="식당 경비",pos=new Vector3(14.5f,0,19)}})
            {
                var guard=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);guard.name=entry.name;guard.transform.position=entry.pos;
                PrefabUtility.RecordPrefabInstancePropertyModifications(guard.transform);
                guard.GetComponent<GuardAgent>().world=context.mission.World;PrefabUtility.RecordPrefabInstancePropertyModifications(guard.GetComponent<GuardAgent>());
            }
            var surface=root.GetComponent<NavMeshSurface>();var old=AssetDatabase.LoadAssetAtPath<NavMeshData>("Assets/SoundThief/Generated/ManorRedesignNavMesh.asset");
            if(!old)throw new InvalidOperationException("Expected redesigned navmesh asset.");
            var main=root.GetComponentsInChildren<Transform>(true).First(t=>t.name=="현관 문짝").gameObject;int oldLayer=main.layer;
            main.layer=0;leaf.layer=0;surface.RemoveData();surface.BuildNavMesh();main.layer=oldLayer;leaf.layer=8;
            var baked=surface.navMeshData;if(!baked)throw new InvalidOperationException("Navigation build failed.");
            surface.RemoveData();EditorUtility.CopySerialized(baked,old);surface.navMeshData=old;surface.AddData();EditorUtility.SetDirty(old);EditorUtility.SetDirty(surface);
            if(baked!=old && !AssetDatabase.Contains(baked))UnityEngine.Object.DestroyImmediate(baked);
            EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();
            return "Study key -> locked dining room -> seal; two guards and updated navmesh.";
        }
    }
}
