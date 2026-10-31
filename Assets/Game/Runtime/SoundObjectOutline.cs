using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace SoundThief
{
    [RequireComponent(typeof(ThiefInteractable))]
    public sealed class SoundObjectOutline : MonoBehaviour
    {
        public Material material;
        public int ShellCount => shells.Count;
        public bool IsVisible => shells.Any(s => s.shell && s.shell.enabled);
        readonly List<(MeshRenderer source, MeshRenderer shell)> shells = new List<(MeshRenderer, MeshRenderer)>();
        MaterialPropertyBlock properties;
        ThiefInteractable target;
        ThiefPlayer player;
        float visibility;
        void Awake() { properties = new MaterialPropertyBlock(); }
        void Start()
        {
            target = GetComponent<ThiefInteractable>();
            var context = gameObject.scene.GetRootGameObjects().SelectMany(o => o.GetComponentsInChildren<StageContext>()).FirstOrDefault();
            player = context ? context.player : null;
            if (!material || (!target.relation && !target.emitter && target.kind != InteractionKind.Key && target.kind != InteractionKind.Treasure && target.kind != InteractionKind.Collectible)) return;
            foreach (var filter in GetComponentsInChildren<MeshFilter>(true))
            {
                var source = filter.GetComponent<MeshRenderer>();
                if (!source || !source.enabled || !source.gameObject.activeInHierarchy || !filter.sharedMesh) continue;
                if (source.name.StartsWith("Sound outline") || System.Text.RegularExpressions.Regex.IsMatch(source.name, @"_LOD[1-9]$")) continue;
                if (source.sharedMaterials.Any(m => m && m.name.ToLowerInvariant().Contains("glass"))) continue;
                var obj = new GameObject("사물 조준 테두리"); obj.layer = 2; obj.transform.SetParent(filter.transform, false);
                obj.AddComponent<MeshFilter>().sharedMesh = filter.sharedMesh;
                var renderer = obj.AddComponent<MeshRenderer>();
                renderer.sharedMaterials = Enumerable.Repeat(material, filter.sharedMesh.subMeshCount).ToArray();
                renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
                renderer.lightProbeUsage = LightProbeUsage.Off; renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                shells.Add((source, renderer));
            }
        }
        void LateUpdate()
        {
            if (!target || !player || !player.view) return;
            bool focused = player.Mission.CanAct && !player.IsStunned && player.HighlightTarget == target;
            visibility = Mathf.MoveTowards(visibility, focused ? 1 : 0, Time.deltaTime / .15f);
            // A small warm rim supports close interaction without painting the room in ability colors.
            bool missionItem = target.kind == InteractionKind.Key || target.kind == InteractionKind.Treasure || target.kind == InteractionKind.Collectible;
            var color = missionItem ? new Color(1f, .72f, .22f, visibility * .8f) : new Color(.8f, .84f, .78f, visibility * .5f);
            properties.SetColor("_OutlineColor", color); properties.SetFloat("_Thickness", .005f);
            foreach (var pair in shells)
            {
                if (!pair.shell) continue;
                pair.shell.enabled = visibility > .01f && pair.source && pair.source.enabled && pair.source.gameObject.activeInHierarchy;
                pair.shell.SetPropertyBlock(properties);
            }
        }
        void OnDisable() { visibility = 0; foreach (var pair in shells) if (pair.shell) pair.shell.enabled = false; }
    }
}
