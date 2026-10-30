using System;
using System.Linq;
using TMPro;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SoundThief.PostalDepot.Editor
{
    public static class PostalSearchAuthoring
    {
        [MenuItem("Sound Thief/Postal Depot/Apply Letter Search Layout")]
        public static void Apply()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
            var context = UnityEngine.Object.FindFirstObjectByType<StageContext>();
            if (!context || !context.definition || context.definition.stageId != "gganwoor-postal-depot")
                throw new InvalidOperationException("Open the postal stage first.");
            var geometry = GameObject.Find("01 Geometry - replace with modular art").transform;
            var props = GameObject.Find("02 Postal props and signage").transform;
            Material art(string name) => AssetDatabase.LoadAssetAtPath<Material>(PostalSceneBuilder.Folder + "/Art/" + name + ".mat");
            var wood=art("Parcel cardboard");var cream=art("Paper cream");var stone=art("Ink blue");var red=art("Seal red");
            void Place(string name,Vector3 position,Vector3 scale)
            {
                var t=GameObject.Find(name).transform;t.position=position;t.localScale=scale;
            }
            GameObject Box(string name,Vector3 position,Vector3 scale,Material material)
            {
                var o=GameObject.Find(name);
                if(!o){o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=name;o.transform.SetParent(geometry,false);}
                o.layer=8;o.transform.position=position;o.transform.localScale=scale;o.GetComponent<Renderer>().sharedMaterial=material;return o;
            }
            Place("Floor",new Vector3(0,-.25f,9),new Vector3(16,.5f,32));
            Place("West",new Vector3(-8,1.8f,9),new Vector3(.3f,3.6f,32));
            Place("East",new Vector3(8,1.8f,9),new Vector3(.3f,3.6f,32));
            Place("North",new Vector3(0,1.8f,25),new Vector3(16,3.6f,.3f));

            for(int row=0;row<2;row++)for(int column=0;column<3;column++)
            {
                float x=-5+column*4;
                float z=row==0?(column==1?20.6f:18.5f):24;
                string bay=((char)('A'+column)).ToString()+(row+1);
                if(row==1)Box("Dispatch shelf "+bay,new Vector3(x,.55f,z),new Vector3(2.6f,1.1f,1.2f),wood);
                Box("Mail rack "+bay+" upper",new Vector3(x,2.05f,z),new Vector3(2.6f,.12f,1.2f),wood);
                foreach(int side in new[]{-1,1})Box("Mail rack "+bay+" post "+side,new Vector3(x+side*1.24f,1.3f,z+.45f),new Vector3(.12f,1.6f,.12f),wood);
                for(int j=0;j<5;j++)
                {
                    Box("Mail stack "+bay+" "+j,new Vector3(x-1+j*.5f,2.23f,z),new Vector3(.42f,.23f,.65f),cream);
                    if(row==1)Box("Parcel rear "+bay+" "+j,new Vector3(x-1+j*.5f,1.28f,z+.2f),new Vector3(.42f,.3f,.5f),cream);
                }
                var name="Postal bay sign "+bay;var sign=GameObject.Find(name);
                if(!sign){sign=new GameObject(name);sign.transform.SetParent(props,false);sign.AddComponent<TextMeshPro>();}
                sign.transform.position=new Vector3(x,2.55f,z+.3f);
                var label=sign.GetComponent<TextMeshPro>();label.text="BAY "+bay;label.fontSize=3;label.alignment=TextAlignmentOptions.Center;label.color=new Color(1,.9f,.65f);label.rectTransform.sizeDelta=new Vector2(2.5f,.6f);
            }
            foreach(string old in new[]{"BAY A","BAY B","BAY C"})
            {var o=GameObject.Find(old);if(o)o.SetActive(false);}
            var signDispatch=GameObject.Find("03 DISPATCH");if(signDispatch)signDispatch.transform.position=new Vector3(-1,3.2f,24.7f);
            var reader=context.mission.GetComponent<PostalLetterReader>();
            if(!reader)reader=context.mission.gameObject.AddComponent<PostalLetterReader>();reader.mission=context.mission;
            var names=new[]{"Letter 01","Letter 02","Letter B","Letter 04"};
            var locations=new[]{new Vector3(3,1.3f,18.05f),new Vector3(-5,1.3f,23.55f),new Vector3(-1,1.3f,20.35f),new Vector3(-1,1.3f,23.55f)};
            var recipients=new[]{"올리버","셀레스트","로언","루카"};
            var senders=new[]{"피파","바넌 공방","바질","마일로"};
            var bodies=new[]{
                "올리버, 토요일엔 배를 비워 둬!\n\n우리 빵집에서 딸기 타르트 시식회를 열어.\n입장료는 새로 만든 빵 이름 하나야.\n나는 '와삭와삭 구름'에 한 표! 맛있는 이름을 생각해 와.\n\n밀가루투성이 피파가",
                "셀레스트 님께.\n\n주문하신 망원경용 황동 나사 12개를 발송합니다.\n동봉한 규격표와 수량을 확인해 주십시오.\n작은 부품이므로 작업 중에는 쟁반을 사용하시기 바랍니다.\n\n바넌 공방 배송 담당",
                "로언!\n\n빌려 간 내 노란 우산은 대체 언제 돌려줄 거야?\n비만 오면 양말부터 젖는다고!\n내일까지 안 돌려주면 네 장화에 물을 가득 채워 놓겠어.\n이건 농담이 아니야. 아마도!\n\n잔뜩 심통 난 바질",
                "루카, 항구에서 특급 소식이다!\n\n오늘 거대한 바다 괴물과 맞서 싸웠지.\n정확히 말하면 내 점심을 노린 갈매기 한 마리였지만!\n샌드위치는 잃었고 모자는 되찾았다. 무승부로 기록해 줘.\n\n위대한 선장 마일로"
            };
            for(int i=0;i<4;i++)
            {
                var o=GameObject.Find(names[i]);
                if(!o){o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=names[i];o.transform.SetParent(props,false);o.AddComponent<ThiefInteractable>();}
                o.transform.position=locations[i];o.transform.localScale=new Vector3(.65f,.1f,.45f);
                foreach(var renderer in o.GetComponentsInChildren<Renderer>())renderer.sharedMaterial=cream;
                var interact=o.GetComponent<ThiefInteractable>();interact.kind=InteractionKind.Collectible;interact.itemId=i==2?PostalBriefing.LetterId:"postal_letter_"+(i+1);
                interact.displayName="우편물 "+(i+1).ToString("00")+" · 내용 확인";
                var letter=o.GetComponent<PostalLetter>();if(!letter)letter=o.AddComponent<PostalLetter>();
                letter.letterId=(i+1).ToString();letter.recipient=recipients[i];letter.sender=senders[i];letter.body=bodies[i];letter.isObjective=i==2;
                var seal=o.transform.Find("Wax seal");
                if(!seal){var wax=GameObject.CreatePrimitive(PrimitiveType.Sphere);wax.name="Wax seal";wax.transform.SetParent(o.transform,false);UnityEngine.Object.DestroyImmediate(wax.GetComponent<Collider>());seal=wax.transform;}
                seal.localPosition=new Vector3(.22f,.7f,0);seal.localScale=new Vector3(.18f,.7f,.3f);seal.GetComponent<Renderer>().sharedMaterial=red;
                PrefabUtility.RecordPrefabInstancePropertyModifications(o.transform);PrefabUtility.RecordPrefabInstancePropertyModifications(interact);
            }
            var manifest=GameObject.Find("Route manifest").GetComponent<ThiefInteractable>();manifest.displayName="분류표 · 개인 우편 발송 구역";PrefabUtility.RecordPrefabInstancePropertyModifications(manifest);
            context.definition.briefing="바질의 의뢰: 로언에게 노란 우산을 돌려주지 않았다고 화내며 쓴 편지를 회수해 주세요. 우산은 집에서 찾았다고 합니다. 분류표로 발송 구역을 확인하고, 편지들을 읽어 목표 한 통을 골라 앞마당으로 돌아오세요.";
            context.definition.objectiveText="분류표를 확인하고, 편지를 읽어 의뢰와 맞는 한 통을 회수하세요.";
            EditorUtility.SetDirty(context.definition);
            ApplyRoomExpansion(geometry,props,wood,cream,stone);
            var surface=geometry.GetComponent<NavMeshSurface>();var saved=surface.navMeshData;
            surface.BuildNavMesh();var baked=surface.navMeshData;
            if(saved && baked!=saved){surface.RemoveData();EditorUtility.CopySerialized(baked,saved);surface.navMeshData=saved;surface.AddData();UnityEngine.Object.DestroyImmediate(baked);EditorUtility.SetDirty(saved);}
            EditorSceneManager.MarkSceneDirty(context.gameObject.scene);
            var errors=context.Validate();if(errors.Count>0)throw new InvalidOperationException(string.Join("\n",errors));
        }
        // Absolute placements keep repeated authoring runs deterministic.
        static void ApplyRoomExpansion(Transform geometry,Transform props,Material wood,Material cream,Material stone)
        {
            void Move(string name,Vector3 point,Vector3? size=null)
            {
                var o=GameObject.Find(name);if(!o)return;
                o.transform.position=point;if(size.HasValue)o.transform.localScale=size.Value;
                if(PrefabUtility.IsPartOfPrefabInstance(o))PrefabUtility.RecordPrefabInstancePropertyModifications(o.transform);
            }
            void Shift(string name,Vector3 delta)
            {var o=GameObject.Find(name);if(o)Move(name,o.transform.position+delta);}
            void Box(string name,Vector3 point,Vector3 size,Material material)
            {
                var o=GameObject.Find(name);
                if(!o){o=GameObject.CreatePrimitive(PrimitiveType.Cube);o.name=name;o.transform.SetParent(geometry,false);}
                o.layer=8;o.transform.SetPositionAndRotation(point,Quaternion.identity);o.transform.localScale=size;
                o.GetComponent<Renderer>().sharedMaterial=material;
            }
            Move("Floor",new Vector3(0,-.25f,12),new Vector3(24,.5f,38));
            Move("West",new Vector3(-12,1.8f,12),new Vector3(.3f,3.6f,38));
            Move("East",new Vector3(12,1.8f,12),new Vector3(.3f,3.6f,38));
            Move("North",new Vector3(0,1.8f,31),new Vector3(24,3.6f,.3f));
            Move("Yard south",new Vector3(0,1.8f,-7),new Vector3(24,3.6f,.3f));
            Move("Front left",new Vector3(-8.5f,1.8f,0),new Vector3(7,3.6f,.3f));
            Move("Front right",new Vector3(9,1.8f,0),new Vector3(6,3.6f,.3f));
            Move("Sorting west",new Vector3(-8,1.8f,6),new Vector3(8,3.6f,.3f));
            Move("Dispatch west",new Vector3(-8,1.8f,14),new Vector3(8,3.6f,.3f));
            Move("Dispatch right",new Vector3(9,1.8f,14),new Vector3(6,3.6f,.3f));
            Move("Loading pallets",new Vector3(9.5f,.6f,7),new Vector3(2,1.2f,3));
            Move("Sorting divider",new Vector3(-1,.85f,10.8f),new Vector3(1,1.7f,2));
            // Seal the reception-to-service bypass until the sorting door is used.
            Box("Reception service closure",new Vector3(2,1.8f,2.8f),new Vector3(.3f,3.6f,2.4f),stone);
            var gate=GameObject.Find("Inside-release loading gate");
            if(!gate){gate=GameObject.CreatePrimitive(PrimitiveType.Cube);gate.name="Inside-release loading gate";gate.transform.SetParent(props,false);}
            gate.layer=0;gate.transform.position=new Vector3(5,1.8f,0);gate.transform.localScale=new Vector3(2.1f,3.6f,.3f);
            gate.GetComponent<Renderer>().sharedMaterial=stone;
            var interaction=gate.GetComponent<ThiefInteractable>();if(!interaction)interaction=gate.AddComponent<ThiefInteractable>();
            interaction.kind=InteractionKind.Door;interaction.displayName="하역문 · 안쪽에서 F로 개방";
            interaction.requiresKey=true;interaction.requiredItemId="postal_staff_key";
            if(!gate.GetComponent<PostalLoadingGate>())gate.AddComponent<PostalLoadingGate>();
            var obstacle=gate.GetComponent<UnityEngine.AI.NavMeshObstacle>();if(!obstacle)obstacle=gate.AddComponent<UnityEngine.AI.NavMeshObstacle>();
            obstacle.shape=UnityEngine.AI.NavMeshObstacleShape.Box;obstacle.size=Vector3.one;obstacle.carving=true;
            // No objective-specific enclosure or guard placement: all front shelves share a row.
            foreach(var name in new[]{"B bay left partition","B bay right partition","B bay back partition","Dispatch lure screen"})
            {var old=GameObject.Find(name);if(old)UnityEngine.Object.DestroyImmediate(old);}
            Move("Dispatch guard",new Vector3(9,0,25));
            Move("Dispatch source",new Vector3(9,1,19));
            Move("Dispatch receiver",new Vector3(9,1,21.5f));
            for(int row=0;row<2;row++)for(int col=0;col<3;col++)
            {
                string bay=((char)('A'+col)).ToString()+(row+1);
                float x=col==0?-7:col==1?-1:7;
                float z=row==0?20:29;
                var delta=new Vector3(x-(-5+col*4),0,z-(row==0?(col==1?20.6f:18.5f):24));
                if(row==0)
                {
                    Move("Dispatch shelf "+(char)('A'+col),new Vector3(x,.55f,z));
                    for(int j=0;j<3;j++)Move("Parcel "+col+"-"+j,new Vector3(x-.8f+j*.8f,1.3f,z+.2f));
                }
                else Shift("Dispatch shelf "+bay,delta);
                Shift("Mail rack "+bay+" upper",delta);
                foreach(int side in new[]{-1,1})Shift("Mail rack "+bay+" post "+side,delta);
                for(int j=0;j<5;j++){Shift("Mail stack "+bay+" "+j,delta);if(row==1)Shift("Parcel rear "+bay+" "+j,delta);}
                Shift("Postal bay sign "+bay,delta);
            }
            Move("Letter B",new Vector3(-1,1.3f,19.55f));
            Move("Letter 01",new Vector3(7,1.3f,19.55f));
            Move("Letter 02",new Vector3(-7,1.3f,28.55f));
            Move("Letter 04",new Vector3(-1,1.3f,28.55f));
            Move("03 DISPATCH",new Vector3(-1,3.2f,30.7f));
            // Wall-side sorting furniture fills the added space without narrowing the routes.
            for(int i=0;i<3;i++)
            {
                float z=18+i*4;
                Box("West parcel bench "+i,new Vector3(-10.5f,.55f,z),new Vector3(1.2f,1.1f,2),wood);
                Box("West parcel bundle "+i,new Vector3(-10.5f,1.3f,z),new Vector3(.9f,.4f,1.4f),cream);
                Box("East dispatch trolley "+i,new Vector3(10.5f,.5f,z),new Vector3(1.2f,1,2),wood);
                Box("East mail bundle "+i,new Vector3(10.5f,1.2f,z),new Vector3(.9f,.4f,1.4f),cream);
            }
            Box("Sorting west workbench",new Vector3(-10,.55f,10),new Vector3(1.2f,1.1f,3),wood);
            Box("Reception waiting bench",new Vector3(-10,.4f,3),new Vector3(1.2f,.8f,3),wood);
        }
    }
}
