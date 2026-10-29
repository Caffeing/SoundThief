using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace SoundThief.Editor
{
    public static class ManorVisualPolish
    {
        const string Generated = "Assets/SoundThief/Generated/";
        public static string Apply()
        {
            var scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlaying || scene.path != "Assets/Scenes/ManorPrototype.unity")
                throw new InvalidOperationException("Open the manor in edit mode.");
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(.35f, .40f, .52f);
            RenderSettings.ambientEquatorColor = new Color(.23f, .26f, .34f);
            RenderSettings.ambientGroundColor = new Color(.12f, .13f, .18f);
            RenderSettings.ambientIntensity = 1;
            DynamicGI.UpdateEnvironment();
            foreach (var ceiling in scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<MeshRenderer>(true))
                .Where(r => r.name.StartsWith("Asset timber ceiling") && !r.name.EndsWith("joint backing")))
            {
                string name = ceiling.name + " joint backing";
                var backing = GameObject.Find(name);
                if (!backing)
                {
                    backing = new GameObject(name); backing.transform.SetParent(ceiling.transform.parent, true);
                    backing.AddComponent<MeshFilter>().sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(Generated + "ManorDressedStoneModule.asset");
                    backing.AddComponent<MeshRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(Generated + "Manor coffered ceiling oak.mat");
                }
                var bounds = ceiling.bounds;
                backing.transform.position = new Vector3(bounds.center.x, 4.18f, bounds.center.z);
                backing.transform.localScale = new Vector3(bounds.size.x + 10, .1f, bounds.size.z + 10);
            }
            string woodPath = Generated + "Manor walnut shelving.mat";
            var wood = AssetDatabase.LoadAssetAtPath<Material>(woodPath);
            if (!wood) { wood = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(wood, woodPath); }
            wood.color = new Color(.34f, .20f, .10f); wood.SetFloat("_Smoothness", .15f); EditorUtility.SetDirty(wood);
            foreach (string name in new[] { "서재 조각 책장 남쪽", "서재 조각 책장 북쪽" })
            {
                var bookcase = GameObject.Find(name);
                foreach (var r in bookcase.GetComponentsInChildren<MeshRenderer>()) r.sharedMaterials = r.sharedMaterials.Select(_ => wood).ToArray();
            }
            string path = Generated + "Manor lantern amber panes.mat";
            var glass = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!glass) { glass = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(glass, path); }
            glass.color = new Color(1, .69f, .20f);
            glass.SetColor("_EmissionColor", new Color(4, 2.2f, .42f));
            glass.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            glass.EnableKeyword("_EMISSION");
            EditorUtility.SetDirty(glass);
            var renderers = scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<MeshRenderer>(true))
                .Where(r => r.gameObject.activeInHierarchy && r.name.EndsWith("Lantern_01a_LOD0")).ToArray();
            int panes = 0;
            foreach (var renderer in renderers)
            {
                var filter = renderer.GetComponent<MeshFilter>();
                // Use the original imported mesh even when this scene was already polished.
                var original = AssetDatabase.LoadAllAssetsAtPath("Assets/EmaceArt/Slavic World/Meshes/EA03_Items_House_Lantern_01a.fbx")
                    .OfType<Mesh>().First(m => m.name.EndsWith("LOD0"));
                string meshPath = Generated + "ManorLanternLitPanes.asset";
                var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                if (!mesh)
                {
                    var vertices = original.vertices; var body = new System.Collections.Generic.List<int>(); var windows = new System.Collections.Generic.List<int>();
                    var triangles = original.triangles; var bounds = original.bounds;
                    for (int i = 0; i < triangles.Length; i += 3)
                    {
                        var a = vertices[triangles[i]]; var b = vertices[triangles[i + 1]]; var c = vertices[triangles[i + 2]];
                        var cross = Vector3.Cross(b - a, c - a); float y = ((a.y + b.y + c.y) / 3 - bounds.min.y) / bounds.size.y;
                        bool pane = Mathf.Abs(cross.normalized.y) < .35f && y > .16f && y < .69f
                            && cross.magnitude * .5f > bounds.size.x * bounds.size.y * .045f;
                        var list = pane ? windows : body; list.Add(triangles[i]); list.Add(triangles[i + 1]); list.Add(triangles[i + 2]);
                    }
                    if (windows.Count == 0) throw new InvalidOperationException("No lantern panes found.");
                    mesh = Object.Instantiate(original); mesh.name = "Manor lantern with lit panes"; mesh.subMeshCount = 2;
                    mesh.SetTriangles(body, 0); mesh.SetTriangles(windows, 1); AssetDatabase.CreateAsset(mesh, meshPath);
                }
                filter.sharedMesh = mesh;
                renderer.sharedMaterials = new[] { renderer.sharedMaterials[0], glass };
                renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.enabled = true;
                var lod = renderer.GetComponentInParent<LODGroup>();
                if (lod)
                {
                    foreach (var r in lod.GetComponentsInChildren<Renderer>(true))
                        if (r.name.EndsWith("LOD1") || r.name.EndsWith("LOD2")) r.enabled = false;
                    Object.DestroyImmediate(lod);
                }
                panes += mesh.GetTriangles(1).Length / 3;
            }
            EditorSceneManager.MarkSceneDirty(scene); AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(scene);
            return "Raised cool fill and lit purchased lantern panes: " + renderers.Length + " fixtures, " + panes + " glass triangles; collision unchanged.";
        }
    }
}
