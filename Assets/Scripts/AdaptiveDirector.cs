using System.Collections.Generic;
using UnityEngine;

namespace Shatterline
{
    public sealed class AdaptiveDirector : MonoBehaviour
    {
        public static AdaptiveDirector Current { get; private set; }
        private readonly float[] decisions = { 20f, 90f, 135f, 203f };
        [SerializeField] private RunGame game;
        [SerializeField] private Runner player;
        private int next, jumps, dashes, hits, deaths;
        private float observedTime, highTime, startX;
        public string LastDecision { get; private set; }
        public string LastReason { get; private set; }
        public int DecisionsMade { get { return next; } }

        private void Awake() { Current = this; LastDecision = "OBSERVING"; LastReason = "FIRST ROUTE"; }
        private void OnDestroy() { if (Current == this) Current = null; }
        public void Initialize(RunGame run, Runner runner)
        { game = run; player = runner; startX = runner.transform.position.x; }
        public void RecordJump() { jumps++; }
        public void RecordDash() { dashes++; }
        public void RecordHit() { hits++; }
        public void RecordDeath() { deaths++; }

        private void Update()
        {
            if (game == null || player == null || !game.CanPlay) return;
            observedTime += Time.deltaTime;
            if (player.transform.position.y > 3.8f) highTime += Time.deltaTime;
            if (next >= decisions.Length || player.transform.position.x < decisions[next]) return;

            float x = decisions[next];
            float end = next + 1 < decisions.Length ? decisions[next + 1] : StageBuilder.PortalX;
            float speed = (player.transform.position.x - startX) / Mathf.Max(1f, observedTime);
            bool relief = deaths > 0 || hits >= 2 || game.Health < 38;
            bool upper = highTime > observedTime * .32f && jumps >= 2;
            bool rush = speed > 5.8f || dashes >= 2;
            LastDecision = relief ? "RECOVERY" : upper ? "UPPER ROUTE" : rush ? "FAST ROUTE" : "STANDARD";
            LastReason = relief ? "DAMAGE / DEATH" : upper ? "ELEVATED TRAVEL" :
                rush ? "SPEED / DASH" : "MIXED ROUTE";
            bool ventAhead = AdjustUpcomingHazards(x, end, LastDecision);
            if (relief)
            {
                if (game.Health < 100) game.GrantAdaptiveAid(12);
                else game.Flash("ROUTE ASSIST / HAZARDS EASE AHEAD", 2f);
            }
            else if (upper) game.Flash("ROUTE RESPONSE / UPPER GEARS SPEED UP AHEAD", 2f);
            else if (rush) game.Flash(ventAhead ?
                "ROUTE RESPONSE / TIMED VENTS CLOSE SOONER AHEAD" :
                "ROUTE RESPONSE / MOVING GEARS QUICKEN AHEAD", 2f);
            next++;
            jumps = dashes = hits = deaths = 0;
            observedTime = highTime = 0f;
            startX = player.transform.position.x;
        }

        private bool AdjustUpcomingHazards(float start, float end, string choice)
        {
            bool ventAhead = false;
            foreach (GearWheel gear in FindObjectsOfType<GearWheel>())
            {
                float x = gear.transform.position.x;
                if (x <= start + 3f || x >= end) continue;
                gear.Speed = choice == "UPPER ROUTE" ? 2.15f :
                    choice == "FAST ROUTE" ? 1.95f : choice == "RECOVERY" ? 1.35f : 1.7f;
                gear.SweepScale = choice == "UPPER ROUTE" ? 1.18f :
                    choice == "RECOVERY" ? .82f : 1f;
            }
            foreach (SteamVent vent in FindObjectsOfType<SteamVent>())
            {
                float x = vent.transform.position.x;
                if (x <= start + 3f || x >= end) continue;
                ventAhead = true;
                vent.DangerStart = choice == "FAST ROUTE" ? .48f :
                    choice == "RECOVERY" ? .62f : .55f;
            }
            return ventAhead;
        }
    }
}
