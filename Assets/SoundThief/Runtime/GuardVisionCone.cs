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
        float nextUpdate;
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
            WorldBoundary = new Vector3[segments + 1]; vertices = new Vector3[segments + 2];
            var colors = new Color[vertices.Length]; var triangles = new int[segments * 3];
            colors[0] = new Color(1, 1, 1, .35f);
            for (int i = 1; i < colors.Length; i++) colors[i] = Color.white;
            mesh.vertices = vertices; mesh.colors = colors;
            for (int i = 0; i < segments; i++) { triangles[i * 3] = 0; triangles[i * 3 + 1] = i + 1; triangles[i * 3 + 2] = i + 2; }
            mesh.triangles = triangles;
        }
        float FloorHeight(Vector3 point)
        {
            float y = transform.position.y; float nearest = 1.2f;
            foreach (var hit in Physics.RaycastAll(point + Vector3.up * 2, Vector3.down, 4, geometryMask, QueryTriggerInteraction.Ignore))
            {
                float delta = Mathf.Abs(hit.point.y - transform.position.y);
                if (hit.normal.y > .6f && delta < nearest) { nearest = delta; y = hit.point.y; }
            }
            return y + .065f;
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
            if (!view.enabled || Time.time < nextUpdate) return;
            nextUpdate = Time.time + .08f;
            var origin = transform.position; var eye = origin + Vector3.up * 1.6f; var rules = guard.world.rules;
            vertices[0] = visual.InverseTransformPoint(new Vector3(origin.x, FloorHeight(origin), origin.z));
            for (int i = 0; i <= segments; i++)
            {
                float angle = -rules.sightAngle * .5f + rules.sightAngle * i / segments;
                var direction = Quaternion.AngleAxis(angle, Vector3.up) * transform.forward;
                float distance = rules.sightRange;
                if (Physics.Raycast(eye, direction, out var hit, distance, geometryMask, QueryTriggerInteraction.Ignore)) distance = Mathf.Max(0, hit.distance - .045f);
                var point = origin + direction * distance; point.y = FloorHeight(point);
                WorldBoundary[i] = point; vertices[i + 1] = visual.InverseTransformPoint(point);
            }
            mesh.vertices = vertices; mesh.RecalculateBounds();
        }
        void OnDestroy() { if (mesh) Destroy(mesh); }
    }
}
