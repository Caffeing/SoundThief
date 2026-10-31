using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace SoundThief.JinHo
{
    // Stage2 presentation only. Observes public guard/room state; never emits gameplay sound or alters AI.
    public sealed class Stage2MirrorBreak : MonoBehaviour
    {
        public JinHoMirror mirror;
        public JinHoRoomStay roomStay;
        public GuardAgent guard2;
        public bool Broken {get;private set;}
        readonly List<Mesh> generatedMeshes=new List<Mesh>();
        readonly List<GameObject> fragments=new List<GameObject>();
        Material glass,backing;
        void Update()
        {
            if(Broken||!mirror||!roomStay||!guard2||!roomStay.roomEmitter)return;
            if(gameObject.scene.path!="Assets/Scenes/Stages/Stage2/Stage2.unity")return;
            var delta=guard2.Goal-roomStay.roomEmitter.transform.position;delta.y=0;
            if(guard2.State==GuardState.InvestigatingSound&&delta.sqrMagnitude<2.25f&&roomStay.ContainsWholeBody(guard2))
            {Broken=true;StartCoroutine(Shatter());}
        }
        IEnumerator Shatter()
        {
            var surface=mirror.surface;var filter=surface?surface.GetComponent<MeshFilter>():null;
            if(!filter||!filter.sharedMesh)yield break;
            var bounds=filter.sharedMesh.bounds;var tr=filter.transform;var normal=mirror.Normal;
            var glassTemplate=Resources.Load<Material>("MirrorShardGlass");
            var backingTemplate=Resources.Load<Material>("MirrorShardBacking");
            if(!glassTemplate||!backingTemplate)
            { Debug.LogWarning("Mirror shard materials are missing from Resources.",this);yield break; }
            glass=new Material(glassTemplate);backing=new Material(backingTemplate);
            // Freeze reflection first; keep the existing wall collider and frame intact.
            mirror.enabled=false;surface.sharedMaterial=backing;
            var random=new System.Random(1729);var velocities=new List<Vector3>();var spins=new List<Vector3>();
            const int columns=5,rows=7;
            for(int y=0;y<rows;y++)for(int x=0;x<columns;x++)
            {
                float x0=Mathf.Lerp(bounds.min.x,bounds.max.x,x/(float)columns),x1=Mathf.Lerp(bounds.min.x,bounds.max.x,(x+1f)/columns);
                float y0=Mathf.Lerp(bounds.min.y,bounds.max.y,y/(float)rows),y1=Mathf.Lerp(bounds.min.y,bounds.max.y,(y+1f)/rows);
                var a=new Vector3(x0,y0,bounds.center.z);var b=new Vector3(x1,y0,bounds.center.z);var c=new Vector3(x1,y1,bounds.center.z);var d=new Vector3(x0,y1,bounds.center.z);
                for(int half=0;half<2;half++)
                {
                    var points=half==0?new[]{a,b,c}:new[]{a,c,d};var center=(points[0]+points[1]+points[2])/3;
                    var mesh=new Mesh{name="Stage2 mirror shard"};mesh.vertices=System.Array.ConvertAll(points,p=>tr.TransformPoint(p)-tr.TransformPoint(center));mesh.triangles=new[]{0,1,2,2,1,0};mesh.RecalculateNormals();mesh.RecalculateBounds();generatedMeshes.Add(mesh);
                    var obj=new GameObject("Mirror fragment");obj.layer=2;obj.transform.position=tr.TransformPoint(center)+normal*.025f;obj.AddComponent<MeshFilter>().sharedMesh=mesh;obj.AddComponent<MeshRenderer>().sharedMaterial=glass;fragments.Add(obj);
                    velocities.Add(normal*(.15f+(float)random.NextDouble()*.5f)+Vector3.right*((float)random.NextDouble()-.5f)*.45f+Vector3.up*((float)random.NextDouble()*.3f));spins.Add(new Vector3((float)random.NextDouble(),(float)random.NextDouble(),(float)random.NextDouble())*180);
                }
            }
            float elapsed=0;
            while(elapsed<2.2f)
            {
                float dt=Time.deltaTime;elapsed+=dt;
                for(int i=0;i<fragments.Count;i++)if(fragments[i])
                {
                    velocities[i]+=Physics.gravity*dt;var step=velocities[i]*dt;
                    if(Physics.Raycast(fragments[i].transform.position,step.normalized,out var hit,step.magnitude+.015f,1<<8,QueryTriggerInteraction.Ignore)&&hit.normal.y>.5f){fragments[i].transform.position=hit.point+Vector3.up*.015f;velocities[i]=Vector3.zero;}
                    else fragments[i].transform.position+=step;
                    fragments[i].transform.Rotate(spins[i]*dt,Space.World);fragments[i].transform.localScale=Vector3.one*Mathf.Clamp01((2.2f-elapsed)/.6f);
                }
                yield return null;
            }
            foreach(var obj in fragments)if(obj)Destroy(obj);fragments.Clear();
            Debug.Log("STAGE2_MIRROR_BROKEN: G2 entered right sound room during lure investigation.",this);
        }
        void OnDestroy(){foreach(var obj in fragments)if(obj)Destroy(obj);foreach(var mesh in generatedMeshes)if(mesh)Destroy(mesh);if(glass)Destroy(glass);if(backing)Destroy(backing);}
    }
}
