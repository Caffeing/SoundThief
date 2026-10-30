using System;using System.Linq;using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine.SceneManagement;using SoundThief;
namespace SoundThief.JinHo.Editor {public static class Stage2ItemGuardArt {
[MenuItem("Sound Thief/JinHo/Stage2/Apply Resonator Crest And Exact Guard Visual")]
public static void MenuApply(){Debug.Log(Apply());}
public static string Apply(){var scene=SceneManager.GetActiveScene();if(scene.path!="Assets/Scenes/Stages/Stage2.unity"||scene.isDirty||EditorApplication.isPlaying)throw new Exception("Saved Stage2 in Edit Mode required");
var preview=EditorSceneManager.OpenPreviewScene("Assets/Scenes/ManorPrototype.unity");try{
var src=preview.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).First(t=>t.name=="경비 외형");var originalAppearance=src.GetComponentInParent<GuardAppearance>();
var guards=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<GuardAgent>(true)).ToArray();
foreach(var guard in guards){var old=guard.transform.Find("Manor visual assets");if(old)Undo.DestroyObjectImmediate(old.gameObject);var existing=guard.transform.Find("경비 외형");if(existing)Undo.DestroyObjectImmediate(existing.gameObject);
var visual=UnityEngine.Object.Instantiate(src.gameObject,guard.transform,false);visual.name="경비 외형";Undo.RegisterCreatedObjectUndo(visual,"Exact Manor guard visual");visual.SetActive(true);
var appearance=guard.GetComponent<GuardAppearance>();if(!appearance)appearance=Undo.AddComponent<GuardAppearance>(guard.gameObject);Undo.RecordObject(appearance,"Bind Stage2 guard presentation");appearance.animator=visual.GetComponentInChildren<Animator>(true);appearance.relaxedArms=originalAppearance&&originalAppearance.relaxedArms;PrefabUtility.RecordPrefabInstancePropertyModifications(appearance);
foreach(var r in guard.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.name=="Guard body")){Undo.RecordObject(r,"Hide primitive guard");r.enabled=false;PrefabUtility.RecordPrefabInstancePropertyModifications(r);}
}
var items=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<ThiefInteractable>(true)).Where(i=>i.kind==InteractionKind.Resonator||i.kind==InteractionKind.Treasure).ToArray();
foreach(var item in items){string childName=item.kind==InteractionKind.Resonator?"공명체 외형 - 금속 잔":"가문의 휘장 외형 - 방패";if(item.transform.Find(childName))continue;
string path=item.kind==InteractionKind.Resonator?"Assets/ThirdParty/Dungeon Megapack/Prefabs/Other/Cup_1_1.prefab":"Assets/ThirdParty/Dungeon Megapack/Prefabs/Other/Shield_1_1.prefab";
var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!asset)throw new Exception(path);var obj=(GameObject)PrefabUtility.InstantiatePrefab(asset,item.transform);obj.name=childName;Undo.RegisterCreatedObjectUndo(obj,"Stage2 item visual");
// Visual assets keep the existing item's sole collider, sound and interaction path.
foreach(var collider in obj.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(collider);
var renderers=obj.GetComponentsInChildren<Renderer>(true);var bounds=renderers[0].bounds;foreach(var r in renderers.Skip(1))bounds.Encapsulate(r.bounds);
float max=Mathf.Max(bounds.size.x,bounds.size.y,bounds.size.z);float desired=item.kind==InteractionKind.Resonator?.8f:.65f;obj.transform.localScale*=desired/Mathf.Max(.001f,max);
bounds=renderers[0].bounds;foreach(var r in renderers.Skip(1))bounds.Encapsulate(r.bounds);obj.transform.position+=item.transform.position-bounds.center;
var oldRenderer=item.GetComponent<MeshRenderer>();if(oldRenderer){Undo.RecordObject(oldRenderer,"Hide primitive item");oldRenderer.enabled=false;PrefabUtility.RecordPrefabInstancePropertyModifications(oldRenderer);}
}
EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);return "Exact guard visual hierarchies="+guards.Length+", resonator/crest visuals="+items.Length+"; Stage2 gameplay unchanged; no Play tests.";
}finally{EditorSceneManager.ClosePreviewScene(preview);}}}}
