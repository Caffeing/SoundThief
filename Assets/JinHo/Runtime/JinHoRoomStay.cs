using System;
using System.Collections.Generic;
using UnityEngine;

namespace SoundThief.JinHo
{
    [DefaultExecutionOrder(-40)]
    public sealed class JinHoRoomStay : MonoBehaviour, IGuardInvestigationPolicy
    {
        public GuardAgent guard1, guard2;
        public SoundEmitter roomEmitter;
        public Vector2 roomMin,roomMax;
        public float staySeconds=3;
        public bool trace;
        public SoundEmitter firstEmitter;
        public Vector2 firstRoomMin,firstRoomMax;
        public float firstExtraSeconds=2;
        bool firstEntered;
        public bool FirstRoomEntered=>firstEntered;
        float firstOutputEnded=-1;
        sealed class Stay
        {
            public float enteredAt=-1;
            public SoundEmitter suppressed;
            public Renderer body;
            public bool participating,exited;
        }
        readonly Dictionary<GuardAgent,Stay> stays=new Dictionary<GuardAgent,Stay>();
        void OnEnable()
        {
            if(!Application.isPlaying)return;
            if(!guard1||!guard2||!roomEmitter||staySeconds<=0||roomMin.x>=roomMax.x||roomMin.y>=roomMax.y)
                throw new InvalidOperationException("JinHo room stay configuration is incomplete.");
            foreach(var g in new[]{guard1,guard2})
            {
                if(g.gameObject.scene!=gameObject.scene||roomEmitter.gameObject.scene!=gameObject.scene||!g.RegisterInvestigationPolicy(this))
                    throw new InvalidOperationException("Expected one same-scene investigation policy.");
                stays[g]=new Stay{body=g.transform.Find("Guard body").GetComponent<Renderer>()};
            }
        }
        void Update()
        {
            foreach(var pair in stays)
            {
                if(!pair.Key)continue;var state=pair.Value;
                if(state.suppressed&&!state.suppressed.Active)state.suppressed=null;
                if(state.enteredAt>=0&&pair.Key.State==GuardState.Returning&&state.body.bounds.max.x<=roomMin.x-.22f)state.exited=true;
            }
            if(!roomEmitter.Active&&guard1.State==GuardState.Watching&&guard2.State==GuardState.Watching)
                foreach(var state in stays.Values){state.enteredAt=-1;state.participating=false;state.exited=false;}
            if(firstEmitter&&!firstEmitter.Active&&guard1.State==GuardState.Watching){firstEntered=false;firstOutputEnded=-1;}
        }
        public bool ContainsWholeBody(GuardAgent guard)
        {
            if(!stays.TryGetValue(guard,out var state)||!state.body)return false;
            var b=state.body.bounds;
            return b.min.x>=roomMin.x&&b.max.x<=roomMax.x&&b.min.z>=roomMin.y&&b.max.z<=roomMax.y;
        }
        public bool AllowsHearing(GuardAgent guard,SoundEmitter source)
            => !stays.TryGetValue(guard,out var state)||!source||state.suppressed!=source||!source.Active;
        public InvestigationDirective Evaluate(GuardAgent guard,SoundEmitter source)
        {
            if(firstEmitter&&source==firstEmitter&&guard==guard1&&stays.TryGetValue(guard,out var first))
            {
                var body=first.body.bounds;
                firstEntered|=body.min.x>=firstRoomMin.x&&body.max.x<=firstRoomMax.x&&body.min.z>=firstRoomMin.y&&body.max.z<=firstRoomMax.y;
                if(!source.Active)
                {
                    if(firstOutputEnded<0)firstOutputEnded=Time.time;
                    if(Time.time-firstOutputEnded<firstExtraSeconds)return InvestigationDirective.Hold;
                }
                return InvestigationDirective.Original;
            }
            if(source!=roomEmitter||!stays.TryGetValue(guard,out var state))return InvestigationDirective.Original;
            state.participating=true;
            if(state.enteredAt<0)
            {
                if(!ContainsWholeBody(guard))return InvestigationDirective.Original;
                state.enteredAt=Time.time;
                if(trace)Debug.Log($"JINHO_ROOM_ENTER guard={guard.name} t={Time.time:F3}",this);
            }
            if(Time.time-state.enteredAt<staySeconds)return InvestigationDirective.Hold;
            if(guard==guard1&&stays.TryGetValue(guard2,out var second)&&second.participating&&!second.exited
                &&(guard2.State==GuardState.InvestigatingSound||guard2.State==GuardState.Returning))return InvestigationDirective.Hold;
            state.suppressed=source;
            if(trace)Debug.Log($"JINHO_ROOM_LEAVE guard={guard.name} t={Time.time:F3} stay={Time.time-state.enteredAt:F3}",this);
            return InvestigationDirective.ReturnHome;
        }
        void OnDisable()
        {
            foreach(var g in stays.Keys)if(g)g.UnregisterInvestigationPolicy(this);
            stays.Clear();
        }
    }
}
