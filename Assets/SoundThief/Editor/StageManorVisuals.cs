using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace SoundThief.Editor
{
    // Presentation only. Existing collider geometry, transforms, doors and stage data remain authoritative.
    public static class StageManorVisuals
    {
        const string Visual = "[Manor style] Visual";
        const string RootName = "[Manor style] Architecture details";
        static Transform details;
        static Template wall, windowWall, floor, door, table, bench, cabinet, pedestal, cup, key, bevel, beam;
        static Material masonry, timber, oak, steel, brass, ivory, cloth, amber, slate, ceilingOak;
        struct Template { public Mesh mesh; public Material[] materials; }

        [MenuItem("Sound Thief/Apply Manor Style To Stages 2 And 3")]
        public static void Apply()
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling) throw new InvalidOperationException("Use Edit Mode after compilation.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty) throw new InvalidOperationException("Save open scenes first.");
            var source = EditorSceneManager.OpenPreviewScene("Assets/Scenes/ManorPrototype.unity");
            var previous = SceneManager.GetActiveScene();
            try
            {
                var renderers = source.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<MeshRenderer>(true)).ToArray();
                var materials = renderers.SelectMany(r => r.sharedMaterials).Where(m => m).ToArray();
                Material Mat(string n) => materials.First(m => m.name == n);
                masonry = Mat("Manor warm masonry"); timber = Mat("Manor walnut shelving");
                oak = Mat("Manor dark structural oak"); steel = Mat("Manor aged steel");
                brass = Mat("Manor antique brass"); cloth = Mat("Manor woven burgundy");
                amber = Mat("Manor lantern amber panes"); slate = Mat("Manor slate flags"); ceilingOak = Mat("Manor coffered ceiling oak");
                ivory = AssetDatabase.LoadAssetAtPath<Material>("Assets/SoundThief/Generated/Manor postal parchment.mat");
                if (!ivory)
                {
                    ivory = new Material(Mat("Manor warm plaster")) { name = "Manor postal parchment" };
                    ivory.SetColor("_BaseColor", new Color(.84f,.77f,.61f));
                    AssetDatabase.CreateAsset(ivory,"Assets/SoundThief/Generated/Manor postal parchment.mat");
                }
                Template Find(string suffix)
                {
                    var r = renderers.First(x => x.GetComponent<MeshFilter>() && AssetDatabase.GetAssetPath(x.GetComponent<MeshFilter>().sharedMesh).EndsWith(suffix)
                        && !System.Text.RegularExpressions.Regex.IsMatch(x.name,@"_LOD[1-9]$"));
                    return new Template { mesh = r.GetComponent<MeshFilter>().sharedMesh, materials = r.sharedMaterials };
                }
                wall = Find("Wall_1_1A.fbx"); windowWall = Find("Wall_3_1A.fbx");
                door = Find("EA03_Prop_House_Door_01b.fbx");
                table = Find("EA03_Prop_Town_Tabble_01a.fbx"); bench = Find("EA03_Prop_Town_Bench_01a.fbx");
                cabinet = Find("EA03_Prop_Town_BookCase_01a.fbx"); pedestal = Find("EA_Arch_Pillar_01a.fbx");
                cup = Find("Other/Cup_1_1.fbx"); key = Find("Other/Key_1_1.fbx");
                bevel = new Template { mesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/SoundThief/Generated/ManorDressedStoneModule.asset"), materials = new[]{timber} };
                beam = Find("Beam_4A.fbx");
                floor = new Template { mesh = AssetDatabase.LoadAllAssetsAtPath("Assets/ThirdParty/Dungeon Megapack/3d Models/Furniture/Floor_5_1.fbx").OfType<Mesh>().First(), materials = new[]{slate} };
                if (!bevel.mesh) throw new InvalidOperationException("Missing Manor bevel mesh.");
                foreach (string path in new[]{"Assets/Scenes/Stages/Stage2.unity","Assets/Scenes/Stages/PostalDepot/PostalDepot.unity"})
                {
                    var scene = SceneManager.GetSceneByPath(path); bool opened = !scene.IsValid() || !scene.isLoaded;
                    if (opened) scene = EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
                    try
                    {
                        SceneManager.SetActiveScene(scene);
                        System.IO.Directory.CreateDirectory("Artifacts/StageArtUnification/before");
                        string backup = "Artifacts/StageArtUnification/before/"+System.IO.Path.GetFileName(path);
                        if (!System.IO.File.Exists(backup)) System.IO.File.Copy(path,backup);
                        ApplyScene(scene);
                        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene);
                    }
                    finally { if (opened) EditorSceneManager.CloseScene(scene,true); }
                }
                AssetDatabase.SaveAssets();
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(source);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
            }
        }

        static void ApplyScene(Scene scene)
        {
            // Only our generated presentation is replaced on re-apply.
            var oldRoot = scene.GetRootGameObjects().FirstOrDefault(g => g.name == RootName);
            if (oldRoot) Object.DestroyImmediate(oldRoot);
            foreach (var t in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Where(t=>t.name==Visual).ToArray())
            {
                var original = t.parent.GetComponent<MeshRenderer>();
                if (original) original.enabled = true;
                Object.DestroyImmediate(t.gameObject);
            }
            details = new GameObject(RootName).transform;
            var originalRenderers = scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<MeshRenderer>(true))
                .Where(r=>r.enabled && r.GetComponent<MeshFilter>() && AssetDatabase.GetAssetPath(r.GetComponent<MeshFilter>().sharedMesh)=="Library/unity default resources"
                    && !r.GetComponent<TMPro.TMP_Text>() && !r.GetComponentInParent<ThiefPlayer>() && !r.GetComponentInParent<GuardAgent>()).ToArray();
            var floorBounds = new List<Bounds>(); int replaced=0, walls=0, doors=0;
            var frames = new HashSet<ThiefInteractable>();
            foreach (var r in originalRenderers)
            {
                string name=r.name; string lower=name.ToLowerInvariant();
                // The mirror component owns its renderer, frame and fracture presentation.
                if (name.Contains("거울") || lower.Contains("mirror") || r.GetComponentsInParent<MonoBehaviour>(true).Any(b=>b && b.GetType().Name=="JinHoMirror")) continue;
                if (lower.Contains("wax seal")) { r.sharedMaterial=cloth; Record(r); continue; }
                var item=r.GetComponentInParent<ThiefInteractable>();
                var bounds=r.GetComponent<MeshFilter>().sharedMesh.bounds;
                var size=r.bounds.size;
                var root=new GameObject(Visual).transform; root.SetParent(r.transform,false);root.gameObject.layer=2;
                if (item && item.kind==InteractionKind.Door)
                {
                    if (lower.Contains("lock")) Fit(root,bevel,bounds,0,steel,"Aged metal lock");
                    else
                    {
                        Fit(root,door,bounds,door.mesh.bounds.size.x<door.mesh.bounds.size.z?90:0,null,"Manor timber door");
                        if (frames.Add(item)) { DoorFrame(r.bounds); doors++; }
                    }
                }
                else if (item && item.kind==InteractionKind.Key) Fit(root,key,bounds,0,null,"Manor key");
                else if (item && item.kind==InteractionKind.Resonator) Fit(root,cup,bounds,0,null,"Metal sound vessel");
                else if (item && item.kind==InteractionKind.Collectible)
                {
                    Fit(root,bevel,bounds,0,ivory,"Parchment envelope");
                    // The same fold and paper are used on every candidate; no answer-specific styling.
                    var fold=new Bounds(bounds.center+Vector3.up*bounds.size.y*.505f,new Vector3(bounds.size.x*.83f,bounds.size.y*.015f,bounds.size.z*.5f));
                    Fit(root,bevel,fold,0,ivory,"Envelope folded flap");
                }
                else if (item && item.kind==InteractionKind.Exit) Fit(root,bevel,bounds,0,timber,"Departure sign");
                else if (r.bounds.max.y<=.055f && size.x>.5f && size.z>.5f)
                {
                    TileFloor(root,bounds,r.transform.lossyScale); floorBounds.Add(r.bounds);
                }
                else if (IsWall(name))
                {
                    bool z=size.z>size.x;
                    int count=Mathf.Max(1,Mathf.CeilToInt(Mathf.Max(size.x,size.z)/3.6f));
                    // Thin lintels and the observation slit use their exact existing extents.
                    if (size.y<1.1f || Mathf.Max(size.x,size.z)<.6f)
                        Fit(root,bevel,bounds,0,masonry,"Dressed stone infill");
                    else for(int i=0;i<count;i++)
                    {
                        var cell=bounds; var axis=z?Vector3.forward:Vector3.right;
                        float length=z?bounds.size.z:bounds.size.x;
                        cell.center=bounds.center+axis*(-length/2+length/count*(i+.5f));
                        cell.size=z?new Vector3(bounds.size.x,bounds.size.y,length/count):new Vector3(length/count,bounds.size.y,bounds.size.z);
                        Fit(root,i%4==2?windowWall:wall,cell,z?90:0,masonry,"Manor stone wall module");
                    }
                    walls++;
                }
                else if (lower.Contains("plinth") || lower.EndsWith("stand")) Fit(root,pedestal,bounds,0,masonry,"Carved stone pedestal");
                else if (lower.Contains("worktable") || lower.Contains("counter") || lower.Contains("workbench") || lower.StartsWith("dispatch shelf"))
                    Fit(root,table,bounds,size.z>size.x?90:0,null,"Manor worktable");
                else if (lower.Contains("bench")) Fit(root,bench,bounds,size.z>size.x?90:0,null,"Timber bench");
                else if (lower.Contains("divider")) Fit(root,cabinet,bounds,size.z>size.x?90:0,null,"Mail cabinet divider");
                else if (lower.StartsWith("parcel") || lower.Contains("bundle") || lower.StartsWith("mail stack"))
                {
                    Fit(root,bevel,bounds,0,ivory,"Wrapped parcel");
                    var strap=bounds;strap.center+=Vector3.up*bounds.size.y*.505f;strap.size=new Vector3(bounds.size.x*.08f,bounds.size.y*.015f,bounds.size.z*1.002f);
                    Fit(root,bevel,strap,0,oak,"Parcel binding");
                    strap.size=new Vector3(bounds.size.x*1.002f,bounds.size.y*.015f,bounds.size.z*.08f);Fit(root,bevel,strap,0,oak,"Parcel binding");
                }
                else Fit(root,bevel,bounds,0,lower.Contains("고정대")?brass:timber,"Walnut fitted furniture");
                r.enabled=false; Record(r); replaced++;
            }
            // Roof panels follow the existing floor footprints rather than bridging missing rooms.
            bool postal=scene.path.Contains("PostalDepot");
            float ceiling=postal?3.7f:4.12f;
            foreach(var b in floorBounds)
            {
                float z0=postal?Mathf.Max(0,b.min.z):b.min.z;
                if(b.max.z-z0<.1f)continue;
                var roof=new Bounds(new Vector3(b.center.x,ceiling,(z0+b.max.z)/2),new Vector3(b.size.x,.16f,b.max.z-z0));
                Fit(details,bevel,roof,0,ceilingOak,"Oak ceiling panel");
                int count=Mathf.CeilToInt((b.max.z-z0)/3.6f);
                for(int i=0;i<=count;i++)
                {
                    float z=Mathf.Lerp(z0,b.max.z,i/(float)count);
                    Fit(details,beam,new Bounds(new Vector3(b.center.x,ceiling-.13f,z),new Vector3(b.size.x,.18f,.18f)),0,oak,"Roof timber crossbeam");
                }
            }
            var lamps=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Light>(true)).Where(l=>l.type==LightType.Point&&l.range>2).Take(12).ToArray();
            foreach(var lamp in lamps) Lantern(lamp.transform.position);
            if(postal)
            {
                foreach(var point in new[]{new Vector3(-10.5f,2.8f,21),new Vector3(10.5f,2.8f,28),new Vector3(-6,2.8f,29)})
                {
                    Lantern(point);var light=new GameObject("Manor dispatch lantern light").AddComponent<Light>();light.transform.SetParent(details);light.transform.position=point;
                    light.type=LightType.Point;light.color=new Color(1,.75f,.43f);light.intensity=2;light.range=8;light.shadows=LightShadows.None;
                }
            }
            System.IO.File.WriteAllText("Artifacts/StageArtUnification/"+System.IO.Path.GetFileNameWithoutExtension(scene.path)+"-applied.json",
                Newtonsoft.Json.JsonConvert.SerializeObject(new{scene=scene.path,replaced,walls,doors,originalCollidersPreserved=true,navMeshRebuilt=false,playMode=EditorApplication.isPlaying},Newtonsoft.Json.Formatting.Indented));
        }

        static Material SourceMaterial(string name) => AssetDatabase.LoadAssetAtPath<Material>("Assets/SoundThief/Generated/"+name+".mat");
        static bool IsWall(string name)
        {
            string n=name.ToLowerInvariant();
            return n.Contains("wall")||n.Contains("partition")||n.Contains("lintel")||name.Contains("관찰 틈")||n.StartsWith("plan ")
                ||new[]{"west","east","north","yard south","front left","front center","front right","sorting west","sorting east","dispatch west","dispatch center","dispatch right","reception service closure"}.Contains(n);
        }
        static void TileFloor(Transform root,Bounds bounds,Vector3 scale)
        {
            int nx=Mathf.Max(1,Mathf.CeilToInt(bounds.size.x*Mathf.Abs(scale.x)/2));
            int nz=Mathf.Max(1,Mathf.CeilToInt(bounds.size.z*Mathf.Abs(scale.z)/2));
            float height=.07f/Mathf.Max(.001f,Mathf.Abs(scale.y));
            for(int x=0;x<nx;x++)for(int z=0;z<nz;z++)
            {
                var b=new Bounds(new Vector3(bounds.min.x+bounds.size.x/nx*(x+.5f),bounds.max.y-height/2,bounds.min.z+bounds.size.z/nz*(z+.5f)),new Vector3(bounds.size.x/nx,height,bounds.size.z/nz));
                Fit(root,floor,b,0,slate,"Slate flagstone");
            }
        }
        static GameObject Fit(Transform parent,Template template,Bounds target,float yaw,Material material,string name)
        {
            var g=new GameObject(name);g.transform.SetParent(parent,false);g.layer=2;
            var rot=Quaternion.Euler(0,yaw,0);var b=template.mesh.bounds;var size=b.size;
            bool quarter=Mathf.Abs(Mathf.Sin(yaw*Mathf.Deg2Rad))>.99f;
            if(quarter)size=new Vector3(size.z,size.y,size.x);
            var scale=new Vector3(target.size.x/Mathf.Max(.00001f,size.x),target.size.y/Mathf.Max(.00001f,size.y),target.size.z/Mathf.Max(.00001f,size.z));
            if(quarter)scale=new Vector3(scale.z,scale.y,scale.x);
            g.transform.localRotation=rot;g.transform.localScale=scale;g.transform.localPosition=target.center-rot*Vector3.Scale(b.center,scale);
            g.AddComponent<MeshFilter>().sharedMesh=template.mesh;
            var renderer=g.AddComponent<MeshRenderer>();renderer.sharedMaterials=Enumerable.Range(0,template.mesh.subMeshCount).Select(i=>material?material:template.materials[Mathf.Min(i,template.materials.Length-1)]).ToArray();
            return g;
        }
        static void DoorFrame(Bounds b)
        {
            bool z=b.size.z>b.size.x;float width=z?b.size.z:b.size.x;var axis=z?Vector3.forward:Vector3.right;
            foreach(int side in new[]{-1,1})
            {
                var p=b.center+axis*side*(width/2+.08f);
                Fit(details,bevel,new Bounds(p,z?new Vector3(b.size.x+.12f,b.size.y+.12f,.12f):new Vector3(.12f,b.size.y+.12f,b.size.z+.12f)),0,oak,"Fixed door jamb");
            }
            var top=b.center+Vector3.up*(b.size.y/2+.1f);
            Fit(details,bevel,new Bounds(top,z?new Vector3(b.size.x+.12f,.15f,width+.28f):new Vector3(width+.28f,.15f,b.size.z+.12f)),0,oak,"Fixed door lintel");
        }
        static void Lantern(Vector3 p)
        {
            Fit(details,bevel,new Bounds(p,new Vector3(.22f,.34f,.22f)),0,amber,"Amber lantern panes");
            foreach(float y in new[]{-.2f,.2f})Fit(details,bevel,new Bounds(p+Vector3.up*y,new Vector3(.3f,.06f,.3f)),0,steel,"Lantern cap");
            foreach(float x in new[]{-.12f,.12f})foreach(float z in new[]{-.12f,.12f})
                Fit(details,bevel,new Bounds(p+new Vector3(x,0,z),new Vector3(.035f,.4f,.035f)),0,brass,"Lantern metal frame");
        }
        static void Record(Object o){EditorUtility.SetDirty(o);PrefabUtility.RecordPrefabInstancePropertyModifications(o);}
    }
}
