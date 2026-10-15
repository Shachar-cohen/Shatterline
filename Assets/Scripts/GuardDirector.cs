using System.Collections.Generic;
using UnityEngine;

namespace Shatterline
{
    // Shared perception and local 2.5D routing. No omniscient knowledge of the player:
    // each guard receives only nearby noise or a nearby ally's actual sighting.
    public sealed class GuardDirector : MonoBehaviour
    {
        public static GuardDirector Current { get; private set; }
        private readonly List<Sentinel> guards = new List<Sentinel>();
        private readonly List<ClimbZone> ladders = new List<ClimbZone>();
        public string LastAlert { get; private set; }
        public int Pressure { get; private set; }
        public int TacticalOrders { get; private set; }
        private float lastSightingAt = -100f, lastEscalationAt = -100f, lastBriefingAt = -100f;

        private void Awake() { Current = this; LastAlert = "NO ALERTS"; }
        private void OnDestroy() { if (Current == this) Current = null; }
        private void Update()
        {
            if (RunGame.Current == null || !RunGame.Current.CanPlay) return;
            // A squad eventually stands down when it has no fresh evidence.
            if (Pressure > 0 && Time.time - lastSightingAt > 12f)
            { Pressure--; lastSightingAt = Time.time; LastAlert = "SQUAD SEARCH COOLING"; }
        }
        public void CacheLadders()
        {
            ladders.Clear();
            ladders.AddRange(FindObjectsOfType<ClimbZone>());
        }
        public void Register(Sentinel guard) { if (!guards.Contains(guard)) guards.Add(guard); }
        public void Unregister(Sentinel guard) { guards.Remove(guard); }

        public void ReportSound(Vector3 source, float radius, string name)
        {
            LastAlert = name;
            foreach (Sentinel guard in guards)
            {
                if (guard == null || guard.Boss) continue;
                float distance = Vector3.Distance(guard.transform.position, source);
                if (distance > radius) continue;
                // Concrete cover muffles, but does not entirely cancel a loud dash.
                bool obstructed = !ClearRoute(guard, source);
                if (obstructed && distance > radius * .55f) continue;
                guard.Hear(source);
            }
        }

        public void ShareSighting(Sentinel caller, Vector3 position, float observedVelocity)
        {
            lastSightingAt = Time.time;
            if (Time.time - lastEscalationAt > 8f)
            { Pressure = Mathf.Min(3, Pressure + 1); lastEscalationAt = Time.time; }
            bool upperRoute = position.y > 3.4f;
            bool carrier = RunGame.Current != null && RunGame.Current.CarryingSignal;
            LastAlert = "SQUAD RADIO: " + (upperRoute ? "UPPER ROUTE " : "GROUND ") +
                Mathf.RoundToInt(position.x) + "m / ALERT " + Pressure;
            var nearby = new List<Sentinel>();
            foreach (Sentinel guard in guards)
                if (guard != null && guard != caller && !guard.Boss &&
                    Vector3.Distance(guard.transform.position, caller.transform.position) < 13f)
                {
                    guard.ReceiveSighting(position);
                    nearby.Add(guard);
                }
            // Send two distinct, bounded orders. The scout favors the upper
            // route, while a second guard holds the last observed approach.
            nearby.Sort((a, b) =>
            {
                float da = Vector3.Distance(a.transform.position, position) -
                    (upperRoute && a.Flying ? 4f : 0f);
                float db = Vector3.Distance(b.transform.position, position) -
                    (upperRoute && b.Flying ? 4f : 0f);
                return da.CompareTo(db);
            });
            float predictedX = position.x + Mathf.Clamp(observedVelocity *
                (carrier ? .38f : .24f), -3.2f, 3.2f);
            if (nearby.Count > 0)
            {
                nearby[0].AssignSquadRole(position, predictedX +
                    (predictedX >= nearby[0].transform.position.x ? 2.5f : -2.5f), true);
                TacticalOrders++;
            }
            if (nearby.Count > 1)
            {
                nearby[1].AssignSquadRole(position, position.x -
                    (predictedX >= position.x ? 1.8f : -1.8f), false);
                TacticalOrders++;
            }
            if (nearby.Count > 1 && Pressure >= 2 && RunGame.Current != null &&
                Time.time - lastBriefingAt > 8f)
            {
                lastBriefingAt = Time.time;
                RunGame.Current.Flash(upperRoute ?
                    "SQUAD SWEEPS THE HIGH ROUTE / BREAK SIGHT TO LOSE THEM" :
                    "SQUAD SPLITS / ONE SWEEPS, ONE HOLDS THE APPROACH", 1.8f);
            }
        }

        // The route is chosen from functional ladder trigger volumes built with the map.
        // Ground units only climb a ladder they can reach from their own safe island.
        public bool TryLadder(Sentinel guard, Vector3 target, out Vector3 waypoint)
        {
            waypoint = Vector3.zero;
            if (guard.Flying || Mathf.Abs(target.y - guard.transform.position.y) < 2f)
                return false;
            float best = float.MaxValue;
            foreach (ClimbZone ladder in ladders)
            {
                if (ladder == null) continue;
                float x = ladder.transform.position.x;
                float dx = Mathf.Abs(x - guard.transform.position.x);
                if (dx > 11f || Mathf.Abs(x - target.x) > 12f) continue;
                if (!guard.WithinSafeIsland(x) || !ClearRoute(guard, new Vector3(x, guard.transform.position.y, 0)))
                    continue;
                float top = ladder.GetComponent<Collider>().bounds.max.y;
                bool ascending = target.y > guard.transform.position.y;
                if (top > 10f) continue; // Nearby columns, not the 12m security bulkhead.
                if (ascending && (top < target.y - .6f || top > target.y + 3.2f ||
                    top < guard.transform.position.y + .8f)) continue;
                if (!ascending && (top < guard.transform.position.y - 1.2f ||
                    top > guard.transform.position.y + 1.2f)) continue;
                float score = dx + Mathf.Abs(x - target.x) * .35f;
                if (score >= best) continue;
                best = score;
                waypoint = dx > .35f ? new Vector3(x, guard.transform.position.y, 0) :
                    new Vector3(x, ascending ? top + .15f : Mathf.Max(2f, target.y), 0);
            }
            return best < float.MaxValue;
        }

        public static bool ClearRoute(Sentinel guard, Vector3 destination)
        {
            Vector3 start = guard.transform.position;
            Vector3 delta = destination - start;
            if (delta.sqrMagnitude < .02f) return true;
            foreach (RaycastHit hit in Physics.RaycastAll(start, delta.normalized,
                delta.magnitude + .08f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                if (hit.collider.GetComponent<Sentinel>() == null &&
                    hit.collider.GetComponent<Runner>() == null) return false;
            return true;
        }

        public static bool SolidFooting(float x, float centerY)
        {
            RaycastHit hit;
            return Physics.Raycast(new Vector3(x, centerY + .3f, 0), Vector3.down,
                out hit, 3.1f, Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore) && hit.normal.y > .65f &&
                hit.point.y < centerY - .55f;
        }
    }
}
