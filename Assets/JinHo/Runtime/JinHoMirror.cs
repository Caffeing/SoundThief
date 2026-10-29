using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace SoundThief.JinHo
{
    public sealed class JinHoMirror : MonoBehaviour
    {
        public Camera viewer;
        public Renderer surface;
        public int resolution=768;
        public float refreshRate=20;
        Camera reflectionCamera;
        RenderTexture texture;
        float nextRender;
        public int RenderCount {get;private set;}
        public RenderTexture Texture=>texture;
        public Vector3 Normal=>transform.TransformDirection(Vector3.back).normalized;
        public Vector3 ReflectedViewerPosition {get;private set;}
        public Vector3 ReflectionViewport(Vector3 point)=>reflectionCamera?reflectionCamera.WorldToViewportPoint(point):Vector3.back;
        void LateUpdate()
        {
            if(!viewer||!surface||Time.unscaledTime<nextRender)return;
            nextRender=Time.unscaledTime+1/Mathf.Max(1,refreshRate);RenderNow(viewer);
        }
        void Ensure()
        {
            if(!texture)
            {
                texture=new RenderTexture(resolution,resolution,24,RenderTextureFormat.ARGB32){name="JinHo mirror reflection",hideFlags=HideFlags.HideAndDontSave};texture.Create();
                var properties=new MaterialPropertyBlock();surface.GetPropertyBlock(properties);properties.SetTexture("_MirrorTex",texture);surface.SetPropertyBlock(properties);
            }
            if(!reflectionCamera)
            {
                var go=new GameObject("JinHo mirror render camera"){hideFlags=HideFlags.HideAndDontSave};reflectionCamera=go.AddComponent<Camera>();reflectionCamera.enabled=false;
                var data=go.AddComponent<UniversalAdditionalCameraData>();data.renderPostProcessing=false;data.renderShadows=false;data.requiresColorOption=CameraOverrideOption.Off;data.requiresDepthOption=CameraOverrideOption.Off;
            }
        }
        public Matrix4x4 ReflectionMatrix()
        {
            var n=Normal;float d=-Vector3.Dot(n,transform.position);var r=Matrix4x4.identity;
            for(int row=0;row<3;row++)for(int col=0;col<3;col++)r[row,col]-=2*n[row]*n[col];
            r.m03=-2*d*n.x;r.m13=-2*d*n.y;r.m23=-2*d*n.z;return r;
        }
        public void RenderNow(Camera source)
        {
            if(!source||!surface)return;Ensure();var normal=Normal;
            if(Vector3.Dot(normal,source.transform.position-transform.position)<=0)return;
            reflectionCamera.CopyFrom(source);reflectionCamera.enabled=false;reflectionCamera.cameraType=CameraType.Reflection;
            reflectionCamera.cullingMask=source.cullingMask&~(1<<31)&~(1<<5);reflectionCamera.useOcclusionCulling=false;reflectionCamera.targetTexture=null;
            var reflection=ReflectionMatrix();ReflectedViewerPosition=reflection.MultiplyPoint(source.transform.position);
            reflectionCamera.transform.SetPositionAndRotation(ReflectedViewerPosition,Quaternion.LookRotation(reflection.MultiplyVector(source.transform.forward),reflection.MultiplyVector(source.transform.up)));
            reflectionCamera.worldToCameraMatrix=source.worldToCameraMatrix*reflection;
            var point=reflectionCamera.worldToCameraMatrix.MultiplyPoint(transform.position+normal*.02f);
            var cameraNormal=reflectionCamera.worldToCameraMatrix.MultiplyVector(normal).normalized;
            reflectionCamera.projectionMatrix=source.CalculateObliqueMatrix(new Vector4(cameraNormal.x,cameraNormal.y,cameraNormal.z,-Vector3.Dot(point,cameraNormal)));
            var properties=new MaterialPropertyBlock();surface.GetPropertyBlock(properties);
            properties.SetMatrix("_MirrorVP",GL.GetGPUProjectionMatrix(reflectionCamera.projectionMatrix,true)*reflectionCamera.worldToCameraMatrix);surface.SetPropertyBlock(properties);
            bool old=GL.invertCulling;
            try
            {
                GL.invertCulling=!old;
                RenderPipeline.SubmitRenderRequest(reflectionCamera,new UniversalRenderPipeline.SingleCameraRequest{destination=texture});RenderCount++;
            }
            finally{GL.invertCulling=old;}
        }
        void OnDisable()
        {
            if(surface)surface.SetPropertyBlock(null);
            if(texture){texture.Release();if(Application.isPlaying)Destroy(texture);else DestroyImmediate(texture);}
            if(reflectionCamera){if(Application.isPlaying)Destroy(reflectionCamera.gameObject);else DestroyImmediate(reflectionCamera.gameObject);}
            texture=null;reflectionCamera=null;
        }
    }
}
