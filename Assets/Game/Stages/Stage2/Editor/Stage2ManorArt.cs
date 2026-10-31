using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using SoundThief;
namespace SoundThief.JinHo.Editor {
public static class Stage2ManorArt {
[MenuItem("Sound Thief/JinHo/Stage2/Apply Manor Visual Assets")]
public static void MenuApply(){Debug.Log(Apply());}
public static string Apply(){
var target=SceneManager.GetActiveScene();if(target.path!="Assets/Scenes/Stages/Stage2/Stage2.unity"||target.isDirty||EditorApplication.isPlaying)throw new InvalidOperationException("Saved Stage2 in Edit Mode required");
var source=EditorSceneManager.OpenPreviewScene("Assets/Scenes/ManorPrototype.unity");int count=0;
try{
var originals=source.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshRenderer>(true)).ToArray();
var targets=target.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshRenderer>(true)).ToArray();
string[,] maps={{"Midnight stone","Manor warm plaster"},{"Slate floor","Manor warm stone floor"},{"Old oak","ManorTimber"},{"Warm brass","Manor aged steel"}};
foreach(var r in targets){if(r.GetComponentInParent<ThiefPlayer>()||r.GetComponent<TMPro.TMP_Text>())continue;var mats=r.sharedMaterials;bool changed=false;for(int i=0;i<mats.Length;i++)for(int j=0;j<maps.GetLength(0);j++)if(mats[i]&&mats[i].name==maps[j,0]){var found=originals.SelectMany(x=>x.sharedMaterials).FirstOrDefault(m=>m&&m.name==maps[j,1]);if(found){mats[i]=found;changed=true;}break;}if(changed){Undo.RecordObject(r,"Stage2 Manor materials");r.sharedMaterials=mats;PrefabUtility.RecordPrefabInstancePropertyModifications(r);count++;}}
// Copy render data only: collider, AI, audio, item IDs and scene references remain on existing objects.
var sourceGuard=source.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<GuardAgent>(true)).FirstOrDefault(g=>g.GetComponentsInChildren<MeshFilter>(true).Any(m=>AssetDatabase.GetAssetPath(m.sharedMesh).StartsWith("Assets/ThirdParty/")));
if(sourceGuard)foreach(var guard in target.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<GuardAgent>(true))){if(guard.transform.Find("Manor visual assets")||guard.transform.Find("경비 외형"))continue;var root=new GameObject("Manor visual assets");Undo.RegisterCreatedObjectUndo(root,"Stage2 guard visual");root.transform.SetParent(guard.transform,false);foreach(var mf in sourceGuard.GetComponentsInChildren<MeshFilter>(true)){var renderer=mf.GetComponent<MeshRenderer>();if(!renderer||!renderer.enabled)continue;var obj=new GameObject(mf.name);obj.transform.SetParent(root.transform,false);obj.transform.localPosition=sourceGuard.transform.InverseTransformPoint(mf.transform.position);obj.transform.localRotation=Quaternion.Inverse(sourceGuard.transform.rotation)*mf.transform.rotation;obj.transform.localScale=new Vector3(mf.transform.lossyScale.x/sourceGuard.transform.lossyScale.x,mf.transform.lossyScale.y/sourceGuard.transform.lossyScale.y,mf.transform.lossyScale.z/sourceGuard.transform.lossyScale.z);obj.AddComponent<MeshFilter>().sharedMesh=mf.sharedMesh;obj.AddComponent<MeshRenderer>().sharedMaterials=renderer.sharedMaterials;count++;}var old=guard.GetComponentsInChildren<MeshRenderer>(true).FirstOrDefault(r=>r.name=="Guard body"&&!r.transform.IsChildOf(root.transform));if(old){Undo.RecordObject(old,"Hide primitive guard visual");old.enabled=false;PrefabUtility.RecordPrefabInstancePropertyModifications(old);}}
var keyVisual=originals.FirstOrDefault(r=>r.name=="열쇠 외형");
if(keyVisual)foreach(var item in target.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<ThiefInteractable>(true)).Where(i=>i.kind==InteractionKind.Key)){if(item.transform.Find("Manor key visual"))continue;var old=item.GetComponent<MeshRenderer>();if(!old)continue;var obj=new GameObject("Manor key visual");Undo.RegisterCreatedObjectUndo(obj,"Stage2 key visual");obj.transform.SetParent(item.transform,false);var mesh=keyVisual.GetComponent<MeshFilter>().sharedMesh;obj.AddComponent<MeshFilter>().sharedMesh=mesh;obj.AddComponent<MeshRenderer>().sharedMaterials=keyVisual.sharedMaterials;var size=old.bounds.size;float worldSize=Mathf.Max(size.x,size.y,size.z);float meshSize=Mathf.Max(mesh.bounds.size.x,mesh.bounds.size.y,mesh.bounds.size.z);float scale=worldSize/Mathf.Max(.001f,meshSize);obj.transform.localScale=new Vector3(scale/item.transform.lossyScale.x,scale/item.transform.lossyScale.y,scale/item.transform.lossyScale.z);obj.transform.localPosition=-Vector3.Scale(mesh.bounds.center,obj.transform.localScale);Undo.RecordObject(old,"Hide primitive key visual");old.enabled=false;PrefabUtility.RecordPrefabInstancePropertyModifications(old);count++;}
EditorSceneManager.MarkSceneDirty(target);EditorSceneManager.SaveScene(target);
return "Stage2 Manor visual assignments: "+count+". Gameplay colliders/references unchanged; no Play test.";
}finally{EditorSceneManager.ClosePreviewScene(source);}
}
}}
