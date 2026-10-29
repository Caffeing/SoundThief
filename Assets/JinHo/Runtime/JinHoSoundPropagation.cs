using System;
using System.Collections.Generic;
using UnityEngine;

namespace SoundThief.JinHo
{
    [Serializable]
    public sealed class AcousticRegion
    {
        public string name;
        public Vector2 min, max;
        public bool Contains(Vector3 point) => point.x >= min.x - 0.001f && point.x <= max.x + 0.001f
            && point.z >= min.y - 0.001f && point.z <= max.y + 0.001f;
    }

    [Serializable]
    public sealed class AcousticConnection
    {
        public string name;
        public int regionA, regionB;
        public Vector3 point;
        public bool wall;
        public ThiefInteractable door;
        public bool Closed => wall || (door && !door.IsOpen);
    }

    public struct AcousticPath
    {
        public bool valid;
        public float distance, cost;
        public int closedBoundaries;
    }

    // Convex room/portal graph. Each connection is a measured crossing point, not a teleport.
    public sealed class JinHoSoundPropagation : MonoBehaviour, ISoundPropagationPolicy
    {
        public AcousticRegion[] regions = Array.Empty<AcousticRegion>();
        public AcousticConnection[] connections = Array.Empty<AcousticConnection>();
        public float closedBoundaryCost = 12;
        public float closedBoundaryGain = 0.25f;
        [Tooltip("Extra path length allowed around open corners. Direct hearing radius and closed-boundary costs remain unchanged. Zero preserves the original policy.")]
        public float openRouteExtraDistance;
        public bool traceSignals;
        public int HearingChecks { get; private set; }
        public int AudioChecks { get; private set; }

        public List<string> Validate()
        {
            var errors = new List<string>();
            if (regions == null || regions.Length == 0) errors.Add("Acoustic regions are missing.");
            if (closedBoundaryCost <= 0 || closedBoundaryGain <= 0 || closedBoundaryGain >= 1)
                errors.Add("Boundary cost must be positive and gain must be between zero and one.");
            if (float.IsNaN(openRouteExtraDistance) || float.IsInfinity(openRouteExtraDistance) || openRouteExtraDistance < 0)
                errors.Add("Open-route distance allowance must be finite and nonnegative.");
            if (regions != null) for (int i = 0; i < regions.Length; i++)
            {
                if (regions[i] == null || regions[i].min.x >= regions[i].max.x || regions[i].min.y >= regions[i].max.y)
                    errors.Add("Invalid acoustic region " + i);
                if (regions[i] == null) continue;
                for (int j = i + 1; j < regions.Length; j++)
                    if (regions[j] != null && Mathf.Min(regions[i].max.x, regions[j].max.x) > Mathf.Max(regions[i].min.x, regions[j].min.x) + 0.001f
                        && Mathf.Min(regions[i].max.y, regions[j].max.y) > Mathf.Max(regions[i].min.y, regions[j].min.y) + 0.001f)
                        errors.Add("Acoustic regions overlap in their interior: " + i + "/" + j);
            }
            if (connections == null) errors.Add("Acoustic connections are missing.");
            else foreach (var edge in connections)
            {
                if (edge == null || regions == null || edge.regionA < 0 || edge.regionB < 0
                    || edge.regionA >= regions.Length || edge.regionB >= regions.Length || edge.regionA == edge.regionB)
                { errors.Add("Invalid acoustic connection indices."); continue; }
                if (regions[edge.regionA] == null || regions[edge.regionB] == null) continue;
                if (!regions[edge.regionA].Contains(edge.point) || !regions[edge.regionB].Contains(edge.point))
                    errors.Add(edge.name + ": crossing must lie on both regions.");
                if (edge.door && (edge.wall || edge.door.kind != InteractionKind.Door || edge.door.gameObject.scene != gameObject.scene))
                    errors.Add(edge.name + ": expected a same-scene door, without a permanent wall.");
            }
            return errors;
        }

        // Dijkstra over the two sides of each crossing. In-room edges use actual 3D length.
        public AcousticPath Evaluate(Vector3 origin, Vector3 receiver)
        {
            int count = connections.Length * 2;
            var costs = new float[count]; var lengths = new float[count];
            var boundaries = new int[count]; var visited = new bool[count];
            for (int i = 0; i < count; i++) costs[i] = float.PositiveInfinity;
            var best = new AcousticPath { cost = float.PositiveInfinity };
            for (int region = 0; region < regions.Length; region++)
            {
                if (!regions[region].Contains(origin)) continue;
                if (regions[region].Contains(receiver))
                    Consider(ref best, Vector3.Distance(origin, receiver), 0);
                for (int i = 0; i < count; i++) if (Region(i) == region)
                {
                    float distance = Vector3.Distance(origin, Point(i));
                    if (distance < costs[i]) { costs[i] = lengths[i] = distance; boundaries[i] = 0; }
                }
            }
            for (int step = 0; step < count; step++)
            {
                int current = -1;
                for (int i = 0; i < count; i++)
                    if (!visited[i] && (current < 0 || costs[i] < costs[current])) current = i;
                if (current < 0 || float.IsPositiveInfinity(costs[current]) || costs[current] > best.cost) break;
                visited[current] = true;
                int room = Region(current);
                if (regions[room].Contains(receiver))
                    Consider(ref best, lengths[current] + Vector3.Distance(Point(current), receiver), boundaries[current]);
                for (int next = 0; next < count; next++)
                {
                    if (visited[next] || next == current) continue;
                    bool crossing = next == (current ^ 1);
                    if (!crossing && Region(next) != room) continue;
                    float distance = crossing ? 0 : Vector3.Distance(Point(current), Point(next));
                    int closed = boundaries[current] + (crossing && connections[current / 2].Closed ? 1 : 0);
                    float length = lengths[current] + distance, cost = length + closed * closedBoundaryCost;
                    if (cost < costs[next] || (Mathf.Approximately(cost, costs[next]) && closed < boundaries[next]))
                    { costs[next] = cost; lengths[next] = length; boundaries[next] = closed; }
                }
            }
            return best;
        }
        int Region(int node) => node % 2 == 0 ? connections[node / 2].regionA : connections[node / 2].regionB;
        Vector3 Point(int node) => connections[node / 2].point;
        void Consider(ref AcousticPath best, float distance, int closed)
        {
            float cost = distance + closed * closedBoundaryCost;
            if (!best.valid || cost < best.cost || (Mathf.Approximately(cost, best.cost) && closed < best.closedBoundaries))
                best = new AcousticPath { valid = true, distance = distance, closedBoundaries = closed, cost = cost };
        }
        public bool AllowsHearing(Vector3 position, int grade, SoundEmitter source, GuardAgent receiver)
        {
            HearingChecks++;
            var path = Evaluate(position, receiver.transform.position);
            float radius = grade * receiver.world.rules.hearingPerGrade;
            float budget = radius + (path.closedBoundaries == 0 ? openRouteExtraDistance : 0);
            bool allowed = path.valid && Vector3.Distance(position, receiver.transform.position) <= radius
                && path.cost <= budget;
            if (traceSignals) Debug.Log($"JINHO_HEAR t={Time.time:F3} guard={receiver.name} grade={grade} D={path.distance:F3} N={path.closedBoundaries} K={path.cost:F3} budget={budget:F3} allow={allowed}", this);
            return allowed;
        }
        public float AudioGain(Vector3 position, int grade, SoundEmitter source, Vector3 listener)
        {
            AudioChecks++;
            var path = Evaluate(position, listener);
            if (!path.valid) throw new InvalidOperationException("Sound source/listener is outside the connected acoustic graph.");
            float direct = Vector3.Distance(position, listener);
            float gain = Mathf.Pow(closedBoundaryGain, path.closedBoundaries)
                * Mathf.Min(1, Mathf.Max(1, direct) / Mathf.Max(1, path.distance));
            if (traceSignals) Debug.Log($"JINHO_AUDIO t={Time.time:F3} grade={grade} D={path.distance:F3} N={path.closedBoundaries} gain={gain:F4}", this);
            return gain;
        }
    }
}
