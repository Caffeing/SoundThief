using System;
using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;

namespace SoundThief.JinHo.Tests
{
    public sealed class JinHoCurrentRevisionTests
    {
        JinHoStageBindings b;JinHoRoomStay stay;MissionDirector mission;ThiefPlayer player;
        ThiefInteractable left,right,door;Keyboard keys;Mouse mouse;InputSettings oldInput,testInput;bool background,caught;
        [Serializable] sealed class Result
        {
            public string scenario,utc,scope="prepared component scenario with original sound and guard code; not full player route";
            public bool caught,chased,directBlocked,reflectedPathClear;
            public int mirrorFrames,visibleG1,visibleG2;
            public float outputEnd=-1,firstReturn=-1,firstRoomEntry=-1,in1=-1,in2=-1,depart1=-1,depart2=-1,out1=-1,out2=-1;
        }
        [UnitySetUp] public IEnumerator SetUp()
        {
            caught=false;background=Application.runInBackground;Application.runInBackground=true;
            oldInput=InputSystem.settings;testInput=Object.Instantiate(oldInput);testInput.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            testInput.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            InputSystem.settings=testInput;keys=InputSystem.AddDevice<Keyboard>();mouse=InputSystem.AddDevice<Mouse>();keys.MakeCurrent();mouse.MakeCurrent();
#if UNITY_EDITOR
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode("Assets/JinHo/Scenes/JinHo.unity",new LoadSceneParameters(LoadSceneMode.Single));
#endif
            yield return null;b=Object.FindFirstObjectByType<JinHoStageBindings>();stay=Object.FindFirstObjectByType<JinHoRoomStay>();mission=b.context.mission;player=mission.player;mission.Accept();mission.Failed+=()=>caught=true;
            var items=Object.FindObjectsByType<ThiefInteractable>(FindObjectsSortMode.None);left=items.Single(t=>t.name=="Left resonator");right=items.Single(t=>t.name=="Right resonator");door=items.Single(t=>t.name=="Door4");
            Assert.That(b.Ready);Assert.That(b.propagation.Validate(),Is.Empty);Assert.That(stay.staySeconds,Is.EqualTo(3));
        }
        [UnityTearDown] public IEnumerator TearDown()
        {InputSystem.RemoveDevice(keys);InputSystem.RemoveDevice(mouse);InputSystem.settings=oldInput;Object.Destroy(testInput);Application.runInBackground=background;Time.timeScale=1;yield return null;}
        void Place(string name)
        {var cc=player.GetComponent<CharacterController>();cc.enabled=false;player.transform.position=GameObject.Find(name).transform.position+Vector3.up*.05f;cc.enabled=true;Physics.SyncTransforms();}
        void Aim(Vector3 point)
        {
            var d=point-player.view.transform.position;float yaw=Mathf.Atan2(d.x,d.z)*Mathf.Rad2Deg,pitch=-Mathf.Atan2(d.y,new Vector2(d.x,d.z).magnitude)*Mathf.Rad2Deg;
            float current=player.view.transform.localEulerAngles.x;if(current>180)current-=360;
            InputSystem.QueueDeltaStateEvent(mouse.delta,new Vector2(Mathf.DeltaAngle(player.transform.eulerAngles.y,yaw)/.1f,(current-pitch)/.1f));
        }
        void Observe(Result r)
        {r.chased|=b.guard1.State==GuardState.Chasing||b.guard2.State==GuardState.Chasing;r.caught|=caught;}
        static string EvidenceDirectory=>Path.Combine(Path.GetDirectoryName(Application.dataPath),"Temp","JinHoValidation");
        void Save(Result r)
        {Directory.CreateDirectory(EvidenceDirectory);r.utc=DateTime.UtcNow.ToString("o");r.caught|=caught;string json=JsonUtility.ToJson(r);File.AppendAllText(Path.Combine(EvidenceDirectory,"CurrentRevisionPlay.jsonl"),json+"\n");Debug.Log("JINHO_CURRENT "+json);}
        void Capture(string name)
        {
            Directory.CreateDirectory(EvidenceDirectory);
            var rt=RenderTexture.GetTemporary(1280,720,24);var old=RenderTexture.active;Texture2D pixels=null;
            try{RenderPipeline.SubmitRenderRequest(player.view,new UniversalRenderPipeline.SingleCameraRequest{destination=rt});RenderTexture.active=rt;pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);pixels.ReadPixels(new Rect(0,0,1280,720),0,0);pixels.Apply();File.WriteAllBytes(Path.Combine(EvidenceDirectory,name),pixels.EncodeToPNG());}
            finally{RenderTexture.active=old;RenderTexture.ReleaseTemporary(rt);if(pixels)Object.Destroy(pixels);}
        }
        bool Visible(GuardAgent g)
        {
            foreach(float y in new[]{.6f,.9f,1.1f,1.3f,1.5f,1.8f,1.9f,1.95f})foreach(float x in new[]{-.3f,0,.3f})
            {
                var p=g.transform.position+new Vector3(x,y,0);var v=player.view.WorldToViewportPoint(p);
                if(v.z>0&&v.x>0&&v.x<1&&v.y>0&&v.y<1&&!Physics.Linecast(player.view.transform.position,p,1<<8,QueryTriggerInteraction.Ignore))return true;
            }
            return false;
        }
        IEnumerator FirstLure(Result r)
        {
            var d1=Object.FindObjectsByType<ThiefInteractable>(FindObjectsSortMode.None).Single(t=>t.name=="Door hinge");
            Assert.That(left.emitter.Inject(d1.relation.Extract(mission.rules),mission.World));float start=Time.time,end=start+30;bool awakened=false;
            while(Time.time<end&&!caught)
            {
                Observe(r);awakened|=b.guard1.State!=GuardState.Sleeping;
                if(stay.FirstRoomEntered&&r.firstRoomEntry<0)r.firstRoomEntry=Time.time-start;
                if(!left.emitter.Active&&r.outputEnd<0)r.outputEnd=Time.time-start;
                if(b.guard1.State==GuardState.Returning&&r.firstReturn<0)r.firstReturn=Time.time-start;
                if(awakened&&b.guard1.State==GuardState.Watching&&!left.emitter.Active)break;
                yield return null;
            }
            Assert.That(awakened);Assert.That(b.guard1.State,Is.EqualTo(GuardState.Watching));Assert.That(b.guard2.State,Is.EqualTo(GuardState.Watching));
            Assert.That(r.firstRoomEntry,Is.GreaterThanOrEqualTo(0),"The full body must reach the first lure room, including when the original output ended before arrival.");
            Assert.That(r.firstReturn-r.outputEnd,Is.InRange(5.9f,6.2f),"Two extra hold seconds plus original four alert seconds.");
        }
        [UnityTest] public IEnumerator MirrorShowsIdleG2FromThePerpendicularRoute()
        {
            var r=new Result{scenario="mirror"};var mirror=Object.FindFirstObjectByType<JinHoMirror>();Place("거울 관찰점");float until=Time.time+2;
            while(Time.time<until){Aim(mirror.transform.position);Observe(r);yield return null;}
            mirror.RenderNow(player.view);r.mirrorFrames=mirror.RenderCount;
            var target=b.guard2.transform.position+Vector3.up;var virtualTarget=mirror.ReflectionMatrix().MultiplyPoint(target);
            var origin=player.view.transform.position;var ray=virtualTarget-origin;float t=Vector3.Dot(mirror.Normal,mirror.transform.position-origin)/Vector3.Dot(mirror.Normal,ray);var point=origin+ray*t;
            var local=mirror.transform.InverseTransformPoint(point);r.directBlocked=SoundWorld.Blocked(origin,target);
            r.reflectedPathClear=!SoundWorld.Blocked(origin,point+mirror.Normal*.015f)&&!SoundWorld.Blocked(point+mirror.Normal*.015f,target);
            var v=mirror.ReflectionViewport(target);Capture("CurrentMirrorPlay.png");Save(r);
            Assert.That(r.mirrorFrames,Is.GreaterThan(1));Assert.That(r.directBlocked);Assert.That(r.reflectedPathClear);
            Assert.That(Mathf.Abs(local.x),Is.LessThan(.5f));Assert.That(Mathf.Abs(local.y),Is.LessThan(.5f));Assert.That(v.z,Is.GreaterThan(0));
            Assert.That(r.chased,Is.False);Assert.That(r.caught,Is.False);
        }
        [UnityTest] public IEnumerator ThickWallWalkingAndSingleStandingAperture()
        {
            var r=new Result{scenario="thick_wall_and_slot"};Place("서서 보는 새 관찰점");int count=b.propagation.HearingChecks;
            float until=Time.time+1;while(Time.time<until){InputSystem.QueueStateEvent(keys,new KeyboardState(Key.D));Observe(r);yield return null;}
            InputSystem.QueueStateEvent(keys,new KeyboardState());
            Assert.That(b.propagation.HearingChecks,Is.EqualTo(count),"Original walking does not emit guard-hearing events.");
            Assert.That(GameObject.Find("Plan Key3 vertical partition").transform.lossyScale.x,Is.EqualTo(2).Within(.001f));
            Assert.That(GameObject.Find("소리방 밀폐 관찰 유리"),Is.Null);Assert.That(b.propagation.connections.Count(e=>e.name.Contains("관찰 틈")),Is.EqualTo(1));
            Place("서서 보는 새 관찰점");var charge=door.relation.Extract(mission.rules);door.Interact(player);yield return new WaitForSeconds(.2f);right.emitter.Inject(charge,mission.World);
            until=Time.time+16;while(Time.time<until&&!caught){Aim(b.guard2.transform.position+Vector3.up*.9f);Observe(r);if(Visible(b.guard1))r.visibleG1++;if(Visible(b.guard2)){if(r.visibleG2==0)Capture("CurrentStandingSlotPlay.png");r.visibleG2++;}yield return null;}
            Save(r);Assert.That(r.chased,Is.False);Assert.That(r.caught,Is.False);Assert.That(r.visibleG2,Is.GreaterThan(0));Assert.That(player.view.transform.localPosition.y,Is.EqualTo(1.65f).Within(.01f));
        }
        [UnityTest] public IEnumerator FirstLureHasTwoExtraSecondsWithoutLongerOutput()
        {var r=new Result{scenario="first_lure_extra2"};yield return FirstLure(r);Save(r);Assert.That(r.outputEnd,Is.InRange(7.9f,8.2f));Assert.That(r.chased,Is.False);Assert.That(r.caught,Is.False);}
        [UnityTest] public IEnumerator SecondLureKeepsThreeSecondsAndReverseExit()
        {
            var warm=new Result();yield return FirstLure(warm);Place("서서 보는 새 관찰점");var r=new Result{scenario="second_lure_reverse3"};
            var charge=door.relation.Extract(mission.rules);door.Interact(player);yield return new WaitForSeconds(.2f);right.emitter.Inject(charge,mission.World);float start=Time.time,until=start+25;
            while(Time.time<until&&!caught)
            {
                float t=Time.time-start;Aim(b.guard2.transform.position+Vector3.up*.9f);Observe(r);
                if(stay.ContainsWholeBody(b.guard1)&&r.in1<0)r.in1=t;if(stay.ContainsWholeBody(b.guard2)&&r.in2<0)r.in2=t;
                if(r.in1>=0&&b.guard1.State==GuardState.Returning&&r.depart1<0)r.depart1=t;if(r.in2>=0&&b.guard2.State==GuardState.Returning&&r.depart2<0)r.depart2=t;
                if(r.depart1>=0&&b.guard1.transform.Find("Guard body").GetComponent<Renderer>().bounds.max.x<door.transform.position.x-.11f&&r.out1<0)r.out1=t;
                if(r.depart2>=0&&b.guard2.transform.Find("Guard body").GetComponent<Renderer>().bounds.max.x<door.transform.position.x-.11f&&r.out2<0)r.out2=t;
                if(r.out1>=0&&r.out2>=0&&b.guard1.State==GuardState.Watching&&b.guard2.State==GuardState.Watching)break;
                yield return null;
            }
            Save(r);Assert.That(r.in1,Is.GreaterThanOrEqualTo(0));Assert.That(r.in2,Is.GreaterThan(r.in1));Assert.That(r.depart2-r.in2,Is.InRange(2.9f,3.2f));
            Assert.That(r.depart1,Is.GreaterThanOrEqualTo(r.out2));Assert.That(r.out1-r.out2,Is.GreaterThanOrEqualTo(.5f));Assert.That(r.chased,Is.False);Assert.That(r.caught,Is.False);
            Assert.That(b.guard1.State,Is.EqualTo(GuardState.Watching));Assert.That(b.guard2.State,Is.EqualTo(GuardState.Watching));
        }
    }
}
