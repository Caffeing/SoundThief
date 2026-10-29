using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace SoundThief.Editor
{
    public static class ManorReferenceArchitecture
    {
        const string Generated = "Assets/SoundThief/Generated/";
        static Material Slate(string name, Color color)
        {
            var path = Generated + name + ".mat"; var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!m) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
            m.color = color; m.SetFloat("_Smoothness", .15f); EditorUtility.SetDirty(m); return m;
        }
        static Mesh Merge(List<CombineInstance> pieces, string name)
        {
            var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt32 }; mesh.CombineMeshes(pieces.ToArray(), true, true); return mesh;
        }
        static Mesh BrickUnit()
        {
            const string sourcePath = "Assets/ThirdParty/EmaceArt/Slavic World/Prefabs/Prop/Carpentry/EA03_Prop_StoneMason_Brick_01a_PRE.prefab";
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
            if (!source) throw new InvalidOperationException("Missing purchased masonry brick.");
            var instance = Object.Instantiate(source);
            var filter = instance.GetComponentsInChildren<MeshFilter>().First(f => f.name.EndsWith("LOD0"));
            var b = filter.GetComponent<Renderer>().bounds;
            var normalized = Matrix4x4.Scale(new Vector3(1 / b.size.x, 1 / b.size.y, 1 / b.size.z)) * Matrix4x4.Translate(-b.center) * filter.transform.localToWorldMatrix;
            var unit = Merge(new List<CombineInstance> { new CombineInstance { mesh = filter.sharedMesh, transform = normalized } }, "Slavica dressed stone module");
            Object.DestroyImmediate(instance); AssetDatabase.CreateAsset(unit, Generated + "ManorDressedStoneModule.asset"); return unit;
        }
        public static string Apply()
        {
            var scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlaying || scene.path != "Assets/Scenes/ManorPrototype.unity" || scene.isDirty) throw new InvalidOperationException("Open the saved manor in edit mode.");
            if (GameObject.Find("석재와 목재 건축")) throw new InvalidOperationException("Reference architecture already exists.");
            const string backup = "Logs/AssetAudit/Manor-before-reference-stonework.unity";
            if (!File.Exists(backup)) File.Copy(scene.path, backup, false);
            var root = new GameObject("석재와 목재 건축");
            var unit = BrickUnit(); var colors = new[] { new Color(.26f, .31f, .42f), new Color(.22f, .27f, .38f), new Color(.30f, .34f, .44f) };
            var materials = colors.Select((c, i) => Slate("Manor blue stone " + i, c)).ToArray();
            var courses = new[] { new List<CombineInstance>(), new List<CombineInstance>(), new List<CombineInstance>() };
            var manor = GameObject.Find("03 저택 구조"); int bricks = 0, walls = 0;
            foreach (var renderer in manor.GetComponentsInChildren<Renderer>(true))
            {
                var filter = renderer.GetComponent<MeshFilter>(); if (!filter || !filter.sharedMesh || !renderer.gameObject.activeInHierarchy) continue;
                if (!AssetDatabase.GetAssetPath(filter.sharedMesh).Contains("unity default resources")) continue;
                string name = renderer.name;
                if (!(name.Contains("wall") || name.Contains("partition") || name.Contains("outer rear segment"))) continue;
                var b = renderer.bounds; if (b.size.y < 3) continue;
                bool alongX = b.size.x > b.size.z; float length = alongX ? b.size.x : b.size.z;
                var along = alongX ? Vector3.right : Vector3.forward; var normal = alongX ? Vector3.forward : Vector3.right;
                float thickness = alongX ? b.size.z : b.size.x;
                int rows = Mathf.RoundToInt(b.size.y / .42f); float rise = b.size.y / rows;
                for (int side = -1; side <= 1; side += 2) for (int row = 0; row < rows; row++)
                {
                    float left = -length / 2; int column = 0;
                    while (left < length / 2 - .001f)
                    {
                        float width = Mathf.Min(column == 0 && row % 2 == 1 ? .48f : .96f, length / 2 - left);
                        if (width < .03f) break;
                        var position = b.center + along * (left + width / 2) + normal * side * (thickness / 2 - .058f);
                        position.y = b.min.y + (row + .5f) * rise;
                        var rotation = Quaternion.Euler(0, alongX ? 0 : 90, 0);
                        var matrix = Matrix4x4.TRS(position, rotation, new Vector3(width - .022f, rise - .022f, .14f));
                        courses[(walls + row * 3 + column * 7) % 3].Add(new CombineInstance { mesh = unit, transform = matrix });
                        left += width; column++; bricks++;
                    }
                }
                walls++;
            }
            var intermediates = courses.Select((p, i) => Merge(p, "Stone tone " + i)).ToArray();
            var combined = new Mesh { name = "Manor staggered stone courses", indexFormat = IndexFormat.UInt32 };
            combined.CombineMeshes(intermediates.Select(m => new CombineInstance { mesh = m, transform = Matrix4x4.identity }).ToArray(), false, false);
            AssetDatabase.CreateAsset(combined, Generated + "ManorStaggeredStoneCourses.asset"); foreach (var mesh in intermediates) Object.DestroyImmediate(mesh);
            root.AddComponent<MeshFilter>().sharedMesh = combined; root.AddComponent<MeshRenderer>().sharedMaterials = materials;
            var grout = AssetDatabase.LoadAssetAtPath<Material>(Generated + "Manor warm masonry.mat"); grout.color = new Color(.075f, .10f, .17f); EditorUtility.SetDirty(grout);

            var floor = GameObject.Find("목재 바닥"); if (floor) floor.GetComponent<Renderer>().enabled = false;
            var floorPieces = new List<CombineInstance>(); var floorMaterial = Slate("Manor slate flags", new Color(.36f, .38f, .43f));
            foreach (var zone in new[] { new Bounds(new Vector3(0, -.05f, 13), new Vector3(32, .08f, 32)), new Bounds(new Vector3(-19, -.05f, 19.5f), new Vector3(6, .08f, 9)), new Bounds(new Vector3(19, -.05f, 10.5f), new Vector3(6, .08f, 9)) })
            {
                int nx = Mathf.CeilToInt(zone.size.x / 2), nz = Mathf.CeilToInt(zone.size.z / 2); float dx = zone.size.x / nx, dz = zone.size.z / nz;
                for (int x = 0; x < nx; x++) for (int z = 0; z < nz; z++)
                {
                    var b = new Bounds(new Vector3(zone.min.x + (x + .5f) * dx, zone.center.y, zone.min.z + (z + .5f) * dz), new Vector3(dx, zone.size.y, dz));
                    var piece = ManorAssetPass.Fit("Assets/ThirdParty/Dungeon Megapack/Prefabs/Furniture/Floor_2_1A.prefab", "석재 바닥 원본 모듈", root.transform, b, Quaternion.identity, floorMaterial);
                    foreach (var f in piece.GetComponentsInChildren<MeshFilter>().Where(f => f.GetComponent<Renderer>().enabled))
                    {
                        for (int sub = 0; sub < f.sharedMesh.subMeshCount; sub++) floorPieces.Add(new CombineInstance { mesh = f.sharedMesh, subMeshIndex = sub, transform = f.transform.localToWorldMatrix });
                        f.GetComponent<Renderer>().enabled = false;
                    }
                }
            }
            var floorMesh = Merge(floorPieces, "저택 석재 바닥 통합 외형"); AssetDatabase.CreateAsset(floorMesh, Generated + "ManorSlateFlagstones.asset");
            var floorObject = new GameObject("저택 석재 바닥 통합 외형"); floorObject.transform.SetParent(root.transform, false); floorObject.AddComponent<MeshFilter>().sharedMesh = floorMesh; floorObject.AddComponent<MeshRenderer>().sharedMaterial = floorMaterial;
            var archMaterial = Slate("Manor arch dressed stone", new Color(.34f, .38f, .47f));
            foreach (float z in new[] { 10.5f, 19.5f })
                ManorAssetPass.Fit("Assets/ThirdParty/Dungeon Megapack/Prefabs/Walls A/Arch_1A.prefab", "Study stone arch " + z, root.transform, new Bounds(new Vector3(-9, 1.95f, z), new Vector3(.62f, 3.9f, 4.4f)), Quaternion.Euler(0, 90, 0), archMaterial);
            foreach (var column in manor.GetComponentsInChildren<Transform>(true).Where(t => t.name.StartsWith("Art EA_Arch_Pillar")))
                foreach (var renderer in column.GetComponentsInChildren<Renderer>(true)) renderer.sharedMaterials = renderer.sharedMaterials.Select(_ => archMaterial).ToArray();
            EditorSceneManager.MarkSceneDirty(scene); AssetDatabase.SaveAssets(); EditorSceneManager.SaveScene(scene);
            File.WriteAllText("Logs/AssetAudit/stonework-result.txt", "Brick modules: " + bricks + "\nWall sections: " + walls + "\nSource: EA03_Prop_StoneMason_Brick_01a_PRE\n");
            return "Built " + bricks + " actual asset brick modules over " + walls + " existing walls, slate flagstone floor and two study arches; gameplay collision unchanged.";
        }
    }
}
