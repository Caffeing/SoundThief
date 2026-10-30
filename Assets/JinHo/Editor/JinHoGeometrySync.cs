using System;
using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace SoundThief.JinHo.Editor
{
    // Measures the user's current geometry. Never calls the historical layout builder.
    public static class JinHoGeometrySync
    {
        static GameObject root;
        static Transform Find(string name) => root.GetComponentsInChildren<Transform>(true).Single(t=>t.name==name);
        static Bounds Bounds(string name) => Find(name).GetComponent<BoxCollider>().bounds;
        static void Begin()
        {
            var scene=SceneManager.GetActiveScene();
            if(EditorApplication.isPlaying || (scene.path!="Assets/JinHo/Scenes/JinHo.unity" && scene.path!="Assets/Scenes/Stages/Stage2.unity"))
                throw new InvalidOperationException("Use the stopped JinHo scene.");
            root=scene.GetRootGameObjects().Single(g=>g.GetComponent<NavMeshSurface>());
            Physics.SyncTransforms();
        }
        [MenuItem("Sound Thief/JinHo/Sync Systems From Current Walls")]
        public static void RefreshMetadata()
        {
            Begin();
            var b=Object.FindFirstObjectByType<JinHoStageBindings>();var p=b.propagation;
            float left=Find("Plan Left sound outer").position.x,outer=Find("Plan Right outer").position.x;
            float keyWall=Bounds("Plan Key3 vertical partition").min.x;
            float keyEast=Bounds("Plan Key3 vertical partition").max.x;
            float soundWall=Find("Door4").position.x-.2f;
            float soundTop=Bounds("Plan Right sound upper").min.z,soundBottom=Bounds("Plan Right sound lower").max.z+.01f;
            float corner=Find("Plan Key3 horizontal partition").position.z;
            float turn=corner+.45f;
            AcousticRegion R(string n,float x1,float x2,float z1,float z2)=>new AcousticRegion{name=n,min=new Vector2(x1,z1),max=new Vector2(x2,z2)};
            Undo.RecordObject(p,"Sync JinHo acoustic geometry");
            p.regions=new[]{R("시작방",-9,9,-3,6),R("중앙 통로",-1.3f,1.5f,6,23.8f),
                R("왼쪽 공명체 방",1.5f,left,Find("Plan Left sound upper").position.z,15),R("왼쪽 통로",1.5f,left,15,17),R("왼쪽 열쇠방",1.5f,left,17,Find("Plan Key2 lower").position.z),
                R("보물방",-1.3f,Find("Plan Treasure opposite side").position.x,23.8f,Find("Plan Treasure far end").position.z),
                R("오른쪽 통로 상부",keyEast,-1.3f,6,turn),R("오른쪽 열쇠방",outer,keyWall,6,corner),R("오른쪽 중간 통로",outer,keyWall,corner,soundTop),
                R("오른쪽 소리방",soundWall,-1.3f,soundTop,soundBottom),R("오른쪽 바깥 통로",outer,soundWall,soundTop,soundBottom),
                R("오른쪽 하단 통로",outer,-1.3f,soundBottom,23.8f),R("오른쪽 회전부",keyEast,-1.3f,turn,soundTop),
                R("두꺼운 구획벽 내부",keyWall,keyEast,6,corner+.2f),R("구획벽 북쪽 우회",keyWall,keyEast,corner+.2f,soundTop)};
            var edges=new List<AcousticConnection>();
            void A(string n,int a,int c,float x,float z,float y,ThiefInteractable door=null,bool wall=false)=>edges.Add(new AcousticConnection{name=n,regionA=a,regionB=c,point=new Vector3(x,y,z),door=door,wall=wall});
            ThiefInteractable Door(string n)=>Find(n).GetComponent<ThiefInteractable>();
            var d3=Door("Door3");var d4=Door("Door4");
            float bendX=Mathf.Min(soundWall-.3f,Bounds("Plan Right sound upper").min.x-.3f);
            float entryX=(outer+Bounds("Plan Key3 horizontal partition").min.x)/2;
            foreach(float h in new[]{.2f,.6f,1.1f,1.525f})
            {
                A("문1",0,1,0,6,h,Door("Door hinge"));A("왼쪽 연결",1,3,1.5f,16,h);A("공명체 방 입구",2,3,3.7f,15,h);A("열쇠2 방 입구",3,4,3.7f,17,h);
                A("문2",1,5,-.5f,23.8f,h,Door("Door2"));A("문3",1,6,-1.3f,d3.transform.position.z+1,h,d3);A("중앙 하단 열린 길",1,11,-1.3f,22.8f,h);
                A("상부 회전",6,12,keyEast+.5f,turn,h);
                A("칸막이 우회 동쪽",12,14,keyEast,(turn+soundTop)/2,h);A("칸막이 우회 서쪽",14,8,keyWall,(turn+soundTop)/2,h);
                A("구획벽 위 열린 회전",6,14,keyEast,corner+.3f,h);A("열쇠3 열린 입구",7,8,entryX,corner,h);
                foreach(float x in new[]{bendX,(outer+soundWall)/2})
                { A("소리방 상단 우회",8,10,x,soundTop,h);A("소리방 하단 우회",10,11,x,soundBottom,h); }
                foreach(float dz in new[]{-.75f,0,.75f})A("문4",9,10,soundWall,d4.transform.position.z-1+dz,h,d4);
                A("왼쪽 공명체 벽",1,2,1.5f,13.5f,h,null,true);A("오른쪽 공유벽",1,9,-1.3f,20,h,null,true);
                A("소리방 상단 벽",12,9,-3.5f,soundTop,h,null,true);A("소리방 하단 벽",9,11,-3.5f,soundBottom,h,null,true);
                A("두꺼운 벽 동쪽",6,13,keyEast,12,h,null,true);A("두꺼운 벽 서쪽",13,7,keyWall,12,h,null,true);
                A("두꺼운 벽 북쪽",13,14,(keyEast+keyWall)/2,corner+.2f,h,null,true);
                A("열쇠3 가로 벽",7,8,(entryX+keyWall)/2,corner,h,null,true);
            }
            if(Find("단일 관찰 틈 상부"))
            {
                var opening=Find("단일 관찰 틈 입구측 벽끝");
                A("단일 관찰 틈",7,8,-8.85f,corner,opening.position.y);
            }
            var glass=root.GetComponentsInChildren<Transform>(true).SingleOrDefault(t=>t.name=="소리방 밀폐 관찰 유리");
            if(glass)A("밀폐 관찰창 음향 경계",8,9,glass.position.x,soundTop,glass.position.y,null,true);
            p.connections=edges.ToArray();
            var stay=b.GetComponent<JinHoRoomStay>();if(!stay)stay=Undo.AddComponent<JinHoRoomStay>(b.gameObject);
            stay.guard1=b.guard1;stay.guard2=b.guard2;stay.roomEmitter=Find("Right resonator").GetComponent<SoundEmitter>();
            stay.roomMin=new Vector2(Find("Door4").position.x+.11f,Bounds("Plan Right sound upper").max.z);
            stay.roomMax=new Vector2(-1.7f,Bounds("Plan Right sound lower").min.z);stay.staySeconds=3;
            stay.firstEmitter=Find("Left resonator").GetComponent<SoundEmitter>();stay.firstExtraSeconds=2;
            stay.firstRoomMin=new Vector2(1.7f,Bounds("Plan Left sound upper").max.z);
            stay.firstRoomMax=new Vector2(Find("Plan Left sound outer").position.x-.2f,Bounds("Plan Left sound lower A").min.z);
            EditorUtility.SetDirty(stay);PrefabUtility.RecordPrefabInstancePropertyModifications(stay);
            var errors=p.Validate();if(errors.Count>0)throw new InvalidOperationException(string.Join("\n",errors));
            EditorUtility.SetDirty(p);EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }
        public static void Bake()
        {
            Begin();var surface=root.GetComponent<NavMeshSurface>();var stored=surface.navMeshData;
            var colliders=root.GetComponentsInChildren<ThiefInteractable>(true).Where(t=>t.kind==InteractionKind.Door).SelectMany(t=>t.GetComponentsInChildren<Collider>(true)).ToArray();
            var layers=colliders.Select(c=>c.gameObject.layer).ToArray();
            try
            {
                foreach(var c in colliders)c.gameObject.layer=0;surface.BuildNavMesh();var data=surface.navMeshData;surface.RemoveData();
                EditorUtility.CopySerialized(data,stored);surface.navMeshData=stored;surface.AddData();Object.DestroyImmediate(data);
                EditorUtility.SetDirty(stored);AssetDatabase.SaveAssetIfDirty(stored);
            }
            finally {for(int i=0;i<colliders.Length;i++)colliders[i].gameObject.layer=layers[i];}
        }
    }
}
