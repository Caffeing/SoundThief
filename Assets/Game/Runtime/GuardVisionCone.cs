using UnityEngine;
using UnityEngine.Rendering;

namespace SoundThief
{
    [RequireComponent(typeof(GuardAgent))]
    public sealed class GuardVisionCone : MonoBehaviour
    {
        public Material material;
        [Range(12, 96)] public int segments = 48;
        public LayerMask geometryMask = 1 << 8;
        public float observationDistance = 10;
        public float observationDelay = .4f;
        public float previewSeconds = 1.4f;
        public bool IsVisible => view && view.enabled;
        public Vector3[] WorldBoundary { get; private set; }
        GuardAgent guard;
        Mesh mesh;
        MeshRenderer view;
        Transform visual;

        Vector3[] vertices;
        MaterialPropertyBlock properties;
        float gazeSeconds, awaySeconds, previewUntil, visibility;
        bool observed;
        void Awake()
        {
            guard = GetComponent<GuardAgent>();
            properties = new MaterialPropertyBlock();
            var obj = new GameObject("경비 관찰 시야"); obj.layer = 2; obj.transform.SetParent(transform, false); visual = obj.transform;
            mesh = new Mesh { name = "Occluded guard vision" }; obj.AddComponent<MeshFilter>().sharedMesh = mesh;
            view = obj.AddComponent<MeshRenderer>(); view.sharedMaterial = material;
            view.shadowCastingMode = ShadowCastingMode.Off; view.receiveShadows = false; view.enabled = false;
            WorldBoundary = new Vector3[segments + 1]; vertices = new Vector3[1 + 3 * (segments + 1)];
            var colors = new Color[vertices.Length]; var triangles = new System.Collections.Generic.List<int>();
            colors[0] = new Color(1, 1, 1, .12f);
            for (int ring = 0; ring < 3; ring++) for (int i = 0; i <= segments; i++)
            {
                int index = 1 + ring * (segments + 1) + i;
                float edge = Mathf.SmoothStep(0, 1, Mathf.Min(i, segments - i) / 4f);
                colors[index] = new Color(1, 1, 1, (ring == 0 ? .7f : ring == 1 ? .35f : 0) * edge);
                if (i == segments) continue;
                if (ring == 0) { triangles.Add(0); triangles.Add(index); triangles.Add(index + 1); }
                else { int prior = index - segments - 1; triangles.Add(prior); triangles.Add(index); triangles.Add(index + 1); triangles.Add(prior); triangles.Add(index + 1); triangles.Add(prior + 1); }
            }
            mesh.vertices = vertices; mesh.colors = colors; mesh.triangles = triangles.ToArray();
        }
        float FloorHeight(Vector3 point)
        {
            float y = transform.position.y; float nearest = .25f;
            foreach (var hit in Physics.RaycastAll(point + Vector3.up * 2, Vector3.down, 4, geometryMask, QueryTriggerInteraction.Ignore))
            {
                float delta = Mathf.Abs(hit.point.y - transform.position.y);
                if (hit.normal.y > .6f && delta < nearest) { nearest = delta; y = hit.point.y; }
            }
            return y + .04f;
        }
        void LateUpdate()
        {
            var player = guard && guard.world ? guard.world.player : null;
            bool active = material && guard && guard.enabled && player && player.Mission.CanAct && !player.IsStunned
                && guard.State != GuardState.Sleeping && guard.State != GuardState.Stunned && guard.State != GuardState.Panic;
            bool looking = false;
            if (active && player.view)
            {
                var aim = transform.position + Vector3.up * 1.2f - player.view.transform.position;
                looking = aim.sqrMagnitude <= observationDistance * observationDistance && Vector3.Angle(player.view.transform.forward, aim) < 12
                    && !Physics.Linecast(player.view.transform.position, transform.position + Vector3.up * 1.2f, geometryMask, QueryTriggerInteraction.Ignore);
            }
            if (looking)
            {
                awaySeconds = 0; gazeSeconds += Time.deltaTime;
                if (!observed && gazeSeconds >= observationDelay) { observed = true; previewUntil = Time.time + previewSeconds; }
            }
            else
            {
                gazeSeconds = 0; awaySeconds += Time.deltaTime;
                if (awaySeconds >= .25f) observed = false;
                previewUntil = 0;
            }
            visibility = Mathf.MoveTowards(visibility, active && looking && Time.time < previewUntil ? 1 : 0, Time.deltaTime / .2f);
            view.enabled = active && visibility > .01f;
            properties.SetColor("_BaseColor", new Color(.8f, .12f, .07f, .18f * visibility)); view.SetPropertyBlock(properties);
            if (!view.enabled) return;

            var origin = transform.position; var eye = origin + Vector3.up * 1.6f; var rules = guard.world.rules;
            vertices[0] = visual.InverseTransformPoint(new Vector3(origin.x, FloorHeight(origin), origin.z));
            for (int i = 0; i <= segments; i++)
            {
                float angle = -rules.sightAngle * .5f + rules.sightAngle * i / segments;
                var direction = Quaternion.AngleAxis(angle, Vector3.up) * transform.forward;
                float distance = rules.sightRange;
                float targetHeight = player.view.transform.position.y - player.transform.position.y;
                var ray = direction * distance + Vector3.up * (targetHeight - 1.6f);
                if (Physics.Raycast(eye, ray.normalized, out var hit, ray.magnitude, geometryMask, QueryTriggerInteraction.Ignore)) distance = Mathf.Max(0, hit.distance * rules.sightRange / ray.magnitude - .045f);
                var point = origin + direction * distance; point.y = FloorHeight(point);
                WorldBoundary[i] = point;
                for (int ring = 0; ring < 3; ring++)
                {
                    float fraction = ring == 0 ? .65f : ring == 1 ? .9f : 1;
                    var p = origin + direction * distance * fraction; p.y = FloorHeight(p);
                    vertices[1 + ring * (segments + 1) + i] = visual.InverseTransformPoint(p);
                }
            }
            mesh.vertices = vertices; mesh.RecalculateBounds();
        }
        void OnDestroy() { if (mesh) Destroy(mesh); }
    }
}
