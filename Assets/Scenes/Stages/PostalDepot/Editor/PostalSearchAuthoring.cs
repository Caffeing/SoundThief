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
            // Preserve the existing guarded B1 approach when the room grows northward.
            Box("B bay back partition",new Vector3(-1,1.8f,21),new Vector3(2,3.6f,.3f),stone);
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
            var recipients=new[]{"김하늘","정민수","김하늘","김하늘"};
            var senders=new[]{"박미정","윤서진","윤서진","윤서진"};
            var bodies=new[]{
                "하늘 씨께.\n\n토요일 고양이 모임에 가져올 간식 목록을 보냅니다.\n참치 맛은 벌써 세 봉지예요. 이번에는 닭고기 맛으로 부탁해요.\n\n우리 집 고양이는 아직도 우편함을 자기 집이라고 생각하네요.\n\n미정 드림",
                "민수야.\n\n우산 가게 영수증을 동봉했어. 지난번 장바구니에 들어간\n노란 우산은 네가 주문한 물건이 맞지?\n가게에서는 다음 비 오는 날에 찾으러 와도 된대.\n\n서진이가",
                "하늘아.\n\n내 노란 우산, 빌려 간 뒤로 왜 소식이 없어?\n비 오는 날마다 네 생각이 나. 좋은 의미는 아니야!\n내일은 꼭 돌려줘. 더는 양말을 말리고 싶지 않거든.\n\n잔뜩 화난 서진이가",
                "하늘아.\n\n토요일 소풍에는 노란 우산을 챙겨 와.\n비가 오면 둘이 쓰고, 맑으면 도시락 그늘로 쓰면 되잖아.\n이번에는 내가 김밥을 쌀게. 당근이 조금 많아도 이해해 줘.\n\n서진이가"
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
            context.definition.briefing="윤서진의 의뢰: 김하늘에게 노란 우산을 돌려주지 않았다고 화내며 쓴 편지를 회수해 주세요. 우산은 집에서 찾았다고 합니다. 분류표로 발송 구역을 확인하고, 편지들을 읽어 목표 한 통을 골라 앞마당으로 돌아오세요.";
            context.definition.objectiveText="분류표를 확인하고, 편지를 읽어 의뢰와 맞는 한 통을 회수하세요.";
            EditorUtility.SetDirty(context.definition);
            var surface=geometry.GetComponent<NavMeshSurface>();var saved=surface.navMeshData;
            surface.BuildNavMesh();var baked=surface.navMeshData;
            if(saved && baked!=saved){surface.RemoveData();EditorUtility.CopySerialized(baked,saved);surface.navMeshData=saved;surface.AddData();UnityEngine.Object.DestroyImmediate(baked);EditorUtility.SetDirty(saved);}
            EditorSceneManager.MarkSceneDirty(context.gameObject.scene);
            var errors=context.Validate();if(errors.Count>0)throw new InvalidOperationException(string.Join("\n",errors));
        }
    }
}
