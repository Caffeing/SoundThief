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
    public static class GuildUpperFloorLayout
    {
        const string RootName = "Guild second floor and facilities";
        const float FloorY = 4.2f;
        static Transform root;
        static Material wood, stone, brass, fabric, linen;
        static Dictionary<string, string> assets;

        [MenuItem("Sound Thief/Apply Guild Second Floor and Facilities")]
        public static void Apply()
        {
            var scene = SceneManager.GetActiveScene();
            if (EditorApplication.isPlaying || scene.path != GuildSceneBuilder.ScenePath)
                throw new InvalidOperationException("Open Guild in Edit Mode first.");
            var art = scene.GetRootGameObjects().SingleOrDefault(g => g.name == "Guild art — Dungeon Megapack");
            if (!art) throw new InvalidOperationException("Apply the medieval guild art first.");
            assets = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Dungeon Megapack", "Assets/ThirdParty/Dungeon Megapack" })
                .Select(AssetDatabase.GUIDToAssetPath).GroupBy(System.IO.Path.GetFileNameWithoutExtension)
                .ToDictionary(g => g.Key, g => g.First());
            // Resolve all required modules before making any scene changes.
            foreach (string n in new[] { "Table_1_2", "Table_2_1", "Chair_2", "Stool_1", "Shelf_Long_2", "Book_6", "Book_3_1", "Potion_1_1", "Chest_1", "Barrel_2", "CandleHolder_1_1", "Sztandar1_3", "Wall_2_1A", "Pilar_1_3A", "Shield_3", "Sword_1" })
                if (!assets.ContainsKey(n)) throw new InvalidOperationException("Missing prefab: " + n);
            wood = Load("Old oak"); stone = Load("Midnight stone"); brass = Load("Warm brass");
            fabric = ColorMaterial("Guild burgundy cloth", new Color(0.32f, 0.06f, 0.08f));
            linen = ColorMaterial("Guild linen", new Color(0.74f, 0.69f, 0.52f));
            var old = scene.GetRootGameObjects().FirstOrDefault(g => g.name == RootName);
            if (old) Object.DestroyImmediate(old);
            root = new GameObject(RootName).transform;

            // Lift only the previous art roof and its beams; leave the original gameplay root intact.
            foreach (Transform t in art.transform)
            {
                if (t.name == "Timber ceiling") t.position = new Vector3(-2, 8.9f, 2);
                if (t.name == "Roof crossbeam") t.position = new Vector3(t.position.x, 8.7f, t.position.z);
            }
            // Relocate the old right-side common-room set clear of the new staircase.
            foreach (Transform t in art.transform)
                if (t.name == "Common room table" || t.name == "Common room seat" || t.name == "Common room table collision"
                    || t.name == "Common room seat collision" || t.name == "Tankard" || t.name == "Common room candle" || t.name == "Adventurer journal")
                    if (t.position.x > 0) t.position += Vector3.left * 14;

            var structure = Group("02 — Mezzanine and accessible stairs");
            // A U-shaped mezzanine leaves the central hall open. The stair opening is a real gap.
            Deck(structure, "West gallery", -14.8f, -9, -2.8f, 22.8f);
            Deck(structure, "North meeting gallery", -9, 4.5f, 17.3f, 22.8f);
            Deck(structure, "East infirmary landing", 4.5f, 10.8f, -2.8f, 4.2f);
            Deck(structure, "East upper landing", 4.5f, 10.8f, 14.28f, 22.8f);
            Deck(structure, "Stair inner gallery", 4.5f, 6.6f, 4.2f, 14.28f);
            Deck(structure, "Stair outer ledge", 9.8f, 10.8f, 4.2f, 14.28f);
            for (int i = 0; i < 24; i++)
            {
                float height = (i + 1) * 0.175f;
                Box(structure, "Stair tread " + (i + 1).ToString("00"),
                    new Vector3(8.2f, height / 2, 4.2f + (i + 0.5f) * 0.42f), new Vector3(3.2f, height, 0.42f), wood, true);
            }
            StairRail(structure, 6.6f); StairRail(structure, 9.8f);
            Rail(structure, new Vector3(-9, FloorY, -2.8f), new Vector3(-9, FloorY, 17.3f));
            Rail(structure, new Vector3(4.5f, FloorY, -2.8f), new Vector3(4.5f, FloorY, 17.3f));
            Rail(structure, new Vector3(-9, FloorY, 17.3f), new Vector3(4.5f, FloorY, 17.3f));
            Rail(structure, new Vector3(-14.8f, FloorY, -2.8f), new Vector3(-9, FloorY, -2.8f));
            Rail(structure, new Vector3(4.5f, FloorY, -2.8f), new Vector3(10.8f, FloorY, -2.8f));
            // The upper walls continue the same stone modules as the ground floor.
            for (int i = 0; i < 7; i++)
            {
                float z = -1 + i * (24f / 6);
                Fit(structure, "Wall_2_1A", "West upper masonry", new Vector3(-15, 6.4f, z), new Vector3(0.4f, 4.8f, 4), 90, true);
                Fit(structure, "Wall_2_1A", "East upper masonry", new Vector3(11, 6.4f, z), new Vector3(0.4f, 4.8f, 4), 90, true);
            }
            for (int i = 0; i < 7; i++)
            {
                float width = 26f / 7;
                float x = -15 + width * (i + 0.5f);
                Fit(structure, "Wall_2_1A", "North upper wall", new Vector3(x, 6.4f, 23), new Vector3(width, 4.8f, 0.4f), 0, true);
                Fit(structure, "Wall_2_1A", "Practice upper separation", new Vector3(x, 6.4f, -3), new Vector3(width, 4.8f, 0.4f), 0, true);
            }
            // A separate lower ceiling closes the original practice room under the tall roof.
            Box(structure, "Practice room ceiling", new Vector3(-2, 4.15f, -11), new Vector3(26, 0.2f, 16), wood, false);
            foreach (float z in new[] { 1.5f, 8f, 15.5f, 21.5f })
            {
                Fit(structure, "Pilar_1_3A", "Gallery support", new Vector3(-9.4f, 2, z), new Vector3(0.5f, 4, 0.5f), 0, true);
                // Keep the stair approach and the right counter free.
                if (z > 15) Fit(structure, "Pilar_1_3A", "Gallery support", new Vector3(4.9f, 2, z), new Vector3(0.5f, 4, 0.5f), 0, true);
            }

            var library = Group("03 — Archive and reading room");
            for (int bay = 0; bay < 3; bay++)
                for (int tier = 0; tier < 3; tier++)
                {
                    float z = 1 + bay * 2.6f, y = FloorY + 0.65f + tier * 0.65f;
                    Fit(library, "Shelf_Long_2", "Archive shelf", new Vector3(-14.2f, y, z), new Vector3(0.65f, 0.13f, 2.3f), 90, true);
                    for (int b = 0; b < 5; b++)
                        Prop(library, b % 2 == 0 ? "Book_6" : "Book_3_1", "Archive volume", new Vector3(-14.2f, y + 0.07f, z - 0.8f + b * 0.4f), b * 13, 0.9f);
                }
            Fit(library, "Table_1_2", "Reading desk", new Vector3(-12.2f, FloorY + 0.44f, 3), new Vector3(1.25f, 0.88f, 2.1f), 90, true);
            Prop(library, "Stool_1", "Reader seat", new Vector3(-11.1f, FloorY, 3), 90, 1, true);
            Prop(library, "Book_6", "Open desk ledger", new Vector3(-12.2f, FloorY + 0.89f, 2.8f));
            Label(library, "ARCHIVE / READING ROOM", new Vector3(-13, 6.7f, -1.8f), 180);

            var dorm = Group("04 — Adventurer rest quarters");
            Bed(dorm, new Vector3(-12.8f, FloorY, 11), false);
            Bed(dorm, new Vector3(-12.8f, FloorY, 14.5f), false);
            Prop(dorm, "Chest_1", "Personal footlocker", new Vector3(-11.1f, FloorY, 11), 90, 1.6f, true);
            Prop(dorm, "Chest_1", "Personal footlocker", new Vector3(-11.1f, FloorY, 14.5f), 90, 1.6f, true);
            Box(dorm, "Rest room screen", new Vector3(-12.6f, FloorY + 1, 8.7f), new Vector3(4.2f, 2, 0.15f), wood, true);
            Label(dorm, "ADVENTURER QUARTERS", new Vector3(-12.5f, 6.7f, 9), 0);

            var meeting = Group("05 — Strategy and meeting room");
            Fit(meeting, "Table_2_1", "Strategy table", new Vector3(-5, FloorY + 0.5f, 20), new Vector3(4.5f, 1, 1.8f), 0, true);
            foreach (float x in new[] { -6.5f, -5, -3.5f })
            {
                Prop(meeting, "Chair_2", "Council chair", new Vector3(x, FloorY, 18.5f), 180, 1, true);
                Prop(meeting, "Chair_2", "Council chair", new Vector3(x, FloorY, 21.7f), 0, 1, true);
            }
            Box(meeting, "Strategy map sheet", new Vector3(-5, FloorY + 1.015f, 20), new Vector3(2.7f, 0.015f, 1.2f), linen, false);
            for (int i = 0; i < 4; i++) Box(meeting, "Map marker", new Vector3(-5.8f + i * 0.5f, FloorY + 1.08f, 19.8f + (i % 2) * 0.35f), new Vector3(0.1f, 0.12f, 0.1f), brass, false);
            Label(meeting, "COUNCIL / STRATEGY", new Vector3(-5, 7.5f, 22.65f), 0);

            var office = Group("06 — Guildmaster office");
            Fit(office, "Table_1_2", "Guildmaster desk", new Vector3(5.9f, FloorY + 0.48f, 21), new Vector3(3.3f, 0.96f, 1.4f), 0, true);
            Prop(office, "Chair_2", "Guildmaster chair", new Vector3(5.9f, FloorY, 22.15f), 0, 1, true);
            Prop(office, "Book_3_1", "Guild accounts", new Vector3(5.3f, FloorY + 0.97f, 21));
            Prop(office, "CandleHolder_1_1", "Office candle", new Vector3(7.1f, FloorY + 0.97f, 21));
            Box(office, "Office partition", new Vector3(1.8f, FloorY + 1.35f, 21), new Vector3(0.18f, 2.7f, 3.5f), wood, true);
            Label(office, "GUILDMASTER", new Vector3(6, 7.3f, 22.65f), 0);

            var infirmary = Group("07 — Infirmary and alchemy bench");
            Bed(infirmary, new Vector3(8.7f, FloorY, -0.8f), true);
            Fit(infirmary, "Table_3_2", "Apothecary bench", new Vector3(6, FloorY + 0.45f, -1.6f), new Vector3(1.6f, 0.9f, 1.1f), 0, true);
            for (int i = 0; i < 4; i++) Prop(infirmary, "Potion_1_1", "Medicinal bottle", new Vector3(5.5f + i * 0.32f, FloorY + 0.91f, -1.6f), i * 20);
            Prop(infirmary, "Stool_1", "Healer stool", new Vector3(6, FloorY, -0.3f), 0, 1, true);
            Box(infirmary, "Infirmary screen", new Vector3(9.4f, FloorY + 1.1f, 1), new Vector3(2.2f, 2.2f, 0.12f), linen, true);
            Label(infirmary, "INFIRMARY / APOTHECARY", new Vector3(7.5f, 6.9f, -2.65f), 180);

            var workshop = Group("08 — Ground floor equipment workshop");
            Fit(workshop, "Table_2_1", "Equipment workbench", new Vector3(7.8f, 0.5f, -1.4f), new Vector3(3.4f, 1, 1), 0, true);
            Prop(workshop, "Sword_1", "Display sword", new Vector3(7.4f, 1.02f, -1.3f), 55, 0.7f);
            Fit(workshop, "Shield_3", "Workshop shield display", new Vector3(9.7f, 1.8f, -2.65f), new Vector3(0.8f, 1.1f, 0.15f));
            Prop(workshop, "Chest_1", "Equipment storage", new Vector3(5.5f, 0, -1.4f), 0, 1.6f, true);
            Label(workshop, "EQUIPMENT / SUPPLIES", new Vector3(7.8f, 2.8f, -2.65f), 180);
            Label(structure, "UPSTAIRS  /  GUILD FACILITIES", new Vector3(8.2f, 2.6f, 3.7f), 0);
            foreach (Vector3 p in new[] { new Vector3(-12, 7.6f, 3), new Vector3(-12, 7.6f, 12), new Vector3(-5, 7.9f, 20), new Vector3(6, 7.8f, 20), new Vector3(7.5f, 7.6f, 0), new Vector3(8, 7.8f, 12) })
                Lamp(structure, p);
            EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
            Debug.Log("Guild second floor, stairs and facilities saved. No Play Mode validation performed.");
        }

        static Transform Group(string n) { var g = new GameObject(n).transform; g.SetParent(root); return g; }
        static Material Load(string n) => AssetDatabase.LoadAssetAtPath<Material>("Assets/SoundThief/Generated/" + n + ".mat");
        static Material ColorMaterial(string name, Color color)
        {
            string path = "Assets/SoundThief/Generated/" + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!m) { m = new Material(Shader.Find("Universal Render Pipeline/Lit")); AssetDatabase.CreateAsset(m, path); }
            m.SetColor("_BaseColor", color); EditorUtility.SetDirty(m); return m;
        }
        static GameObject Box(Transform p, string n, Vector3 position, Vector3 size, Material m, bool solid)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube); g.name = n; g.transform.SetParent(p);
            g.transform.position = position; g.transform.localScale = size;
            g.GetComponent<Renderer>().sharedMaterial = m; g.layer = solid ? 8 : 2;
            if (!solid) Object.DestroyImmediate(g.GetComponent<Collider>()); return g;
        }
        static void Deck(Transform p, string n, float x0, float x1, float z0, float z1)
            => Box(p, n, new Vector3((x0+x1)/2, FloorY-0.12f, (z0+z1)/2), new Vector3(x1-x0, 0.24f, z1-z0), wood, true);
        static Bounds BoundsOf(GameObject g)
        {
            var rs=g.GetComponentsInChildren<Renderer>(); var b=rs[0].bounds;
            foreach(var r in rs.Skip(1)) b.Encapsulate(r.bounds); return b;
        }
        static GameObject Instance(Transform p, string asset, string n, float yaw)
        {
            var g=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(assets[asset]),p);
            g.name=n; g.transform.position=Vector3.zero; g.transform.rotation=Quaternion.Euler(0,yaw,0);
            foreach(var c in g.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(c);
            foreach(var t in g.GetComponentsInChildren<Transform>(true)) t.gameObject.layer=2;
            return g;
        }
        static GameObject Fit(Transform p,string asset,string n,Vector3 center,Vector3 size,float yaw=0,bool solid=false)
        {
            var g=Instance(p,asset,n,yaw); var b=BoundsOf(g);
            var f=new Vector3(size.x/b.size.x,size.y/b.size.y,size.z/b.size.z);
            if(Mathf.Abs(Mathf.Sin(yaw*Mathf.Deg2Rad))>0.99f) f=new Vector3(f.z,f.y,f.x);
            g.transform.localScale=Vector3.Scale(g.transform.localScale,f); g.transform.position+=center-BoundsOf(g).center;
            if(solid) ColliderFor(g); return g;
        }
        static GameObject Prop(Transform p,string asset,string n,Vector3 bottom,float yaw=0,float scale=1,bool solid=false)
        {
            var g=Instance(p,asset,n,yaw);g.transform.localScale*=scale;var b=BoundsOf(g);
            g.transform.position+=bottom-new Vector3(b.center.x,b.min.y,b.center.z);if(solid) ColliderFor(g);return g;
        }
        static void ColliderFor(GameObject g)
        {
            var b=BoundsOf(g);var c=new GameObject(g.name+" collision");c.transform.SetParent(g.transform.parent);
            c.transform.position=b.center;c.layer=8;c.AddComponent<BoxCollider>().size=b.size;
        }
        static void Rail(Transform p,Vector3 a,Vector3 b)
        {
            var d=b-a;float length=d.magnitude;
            var beam=Box(p,"Gallery handrail",(a+b)/2+Vector3.up*1.05f,new Vector3(0.12f,0.12f,length),wood,true);
            beam.transform.rotation=Quaternion.LookRotation(d.normalized);
            // Continuous panel collider prevents slipping through the decorative posts.
            var barrier=new GameObject("Gallery fall barrier");barrier.transform.SetParent(p);barrier.layer=8;
            barrier.transform.position=(a+b)/2+Vector3.up*0.52f;barrier.transform.rotation=beam.transform.rotation;
            barrier.AddComponent<BoxCollider>().size=new Vector3(0.1f,1.04f,length);
            int count=Mathf.CeilToInt(length/1.4f);
            for(int i=0;i<=count;i++)Box(p,"Gallery baluster",Vector3.Lerp(a,b,i/(float)count)+Vector3.up*0.5f,new Vector3(0.1f,1,0.1f),wood,false);
        }
        static void StairRail(Transform p,float x)
        {
            var a=new Vector3(x,0.175f,4.2f);var b=new Vector3(x,FloorY,14.28f);
            var d=b-a;var beam=Box(p,"Stair handrail",(a+b)/2+Vector3.up*1.05f,new Vector3(0.12f,0.12f,d.magnitude),wood,true);
            beam.transform.rotation=Quaternion.LookRotation(d.normalized);
            for(int i=0;i<24;i++)Box(p,"Stair side barrier",new Vector3(x,(i+1)*0.175f+0.5f,4.2f+(i+0.5f)*0.42f),new Vector3(0.12f,1,0.42f),wood,true);
        }
        static void Bed(Transform p,Vector3 origin,bool medical)
        {
            Box(p,"Timber bed frame",origin+new Vector3(0,0.28f,0),new Vector3(1.5f,0.55f,2.4f),wood,true);
            Box(p,medical?"Infirmary mattress":"Rest mattress",origin+new Vector3(0,0.62f,0),new Vector3(1.38f,0.18f,2.25f),linen,false);
            Box(p,"Folded blanket",origin+new Vector3(0,0.73f,0.35f),new Vector3(1.4f,0.04f,1.4f),medical?linen:fabric,false);
            Box(p,"Pillow",origin+new Vector3(0,0.78f,-0.8f),new Vector3(1.05f,0.17f,0.42f),linen,false);
            Box(p,"Bed headboard",origin+new Vector3(0,0.62f,-1.2f),new Vector3(1.55f,1.25f,0.12f),wood,true);
        }
        static void Label(Transform p,string value,Vector3 position,float yaw)
        {
            var sign=new GameObject(value);sign.transform.SetParent(p);sign.transform.position=position;
            sign.transform.rotation=Quaternion.Euler(0,yaw,0);sign.layer=2;
            var text=sign.AddComponent<TMPro.TextMeshPro>();
            var existing=SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<TMPro.TextMeshPro>(true)).FirstOrDefault(t=>t!=text&&t.font);
            if(existing)text.font=existing.font;
            text.text=value;text.fontSize=1.7f;text.alignment=TMPro.TextAlignmentOptions.Center;
            text.color=new Color(1,0.85f,0.57f);text.rectTransform.sizeDelta=new Vector2(5,0.6f);
            Box(p,"Facility sign backing",position+sign.transform.forward*0.07f,new Vector3(4.8f,0.62f,0.08f),wood,false);
        }
        static void Lamp(Transform p,Vector3 position)
        {
            var g=new GameObject("Upper gallery lantern");g.transform.SetParent(p);g.transform.position=position;
            var light=g.AddComponent<Light>();light.type=LightType.Point;light.color=new Color(1,0.76f,0.45f);light.intensity=3;light.range=9;light.shadows=LightShadows.None;
            Box(p,"Lantern housing",position,new Vector3(0.25f,0.38f,0.25f),brass,false);
        }
    }
}
