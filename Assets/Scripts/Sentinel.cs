using System.Collections.Generic;
using UnityEngine;

namespace Shatterline
{
    public sealed class Sentinel : MonoBehaviour
    {
        public bool Boss;
        public bool Flying;
        public bool Rapid;
        public SignalStation DefenseTarget;
        public int Health = 2;
        [SerializeField] private float patrolMin, patrolMax;
        public float PatrolMin { get { return patrolMin; } }
        public float PatrolMax { get { return patrolMax; } }
        public float HomeX { get { return origin; } }
        private float origin, lastShot, lastHit, lastSeenAt, lastSeenX, lastSeenY, originalHeight;
        private float heardAt, searchUntil, lastRadioAt, flankUntil, lastWarnAt;
        private float tacticalUntil, tacticalTargetX;
        private bool sweepOrder;
        private System.Random reactionRandom;
        private float climbExitUntil, lastLadderX = -999f;
        private float climbTop, climbX;
        private Vector2 safeIsland;
        [SerializeField] private RunGame game;
        [SerializeField] private Transform model;
        [SerializeField] private Animator animator;
        [SerializeField] private Renderer warningEye;
        private int phase;
        private bool dying;
        private enum Alert { Patrol, Investigate, Search, Attack, Flank, Sweep, Overwatch, Cover, Retreat, Climb }
        private Alert alert;
        public string AIState { get { return Boss ? "BOSS PHASE " + (phase + 1) : alert.ToString().ToUpperInvariant(); } }
        public bool WithinSafeIsland(float x)
        { return Flying || x >= safeIsland.x && x <= safeIsland.y; }
        public void Hear(Vector3 position)
        {
            if (Boss || dying || Time.time - lastSeenAt < 1f) return;
            lastSeenX = position.x; lastSeenY = position.y;
            heardAt = Time.time;
            searchUntil = Time.time + 5f;
        }
        public void ReceiveSighting(Vector3 position)
        {
            if (Boss || dying || Time.time - lastSeenAt < 1f) return;
            lastSeenX = position.x; lastSeenY = position.y;
            heardAt = Time.time;
            searchUntil = Time.time + 5f + (GuardDirector.Current != null ? GuardDirector.Current.Pressure : 0);
            flankUntil = Time.time + 2.6f;
        }
        public void AssignSquadRole(Vector3 lastObserved, float targetX, bool sweep)
        {
            if (Boss || dying) return;
            lastSeenX = lastObserved.x; lastSeenY = lastObserved.y;
            sweepOrder = sweep;
            tacticalUntil = Time.time + (sweep ? 4.4f : 5.2f);
            float low = Flying ? origin - 11f : Mathf.Max(safeIsland.x, origin - 11f);
            float high = Flying ? origin + 11f : Mathf.Min(safeIsland.y, origin + 11f);
            tacticalTargetX = Mathf.Clamp(targetX, low, high);
            searchUntil = Mathf.Max(searchUntil, tacticalUntil);
            if (!sweep) flankUntil = 0;
        }
        public void Configure(RunGame run, bool boss, bool rapid = false)
        {
            game = run; Boss = boss; Rapid = rapid;
            Health = boss ? 16 : rapid ? Mathf.Max(2, run.Level) : 1 + run.Level;
            if (boss) game.UpdateBossHealth(Health);
            origin = transform.position.x; model = transform.GetChild(0);
            animator = model.GetComponent<Animator>();
            reactionRandom = new System.Random(unchecked(RunState.RunSeed ^
                (Mathf.RoundToInt(origin * 419f) * 7919) ^ (run.Level << 16)));
            originalHeight = transform.position.y;
            lastSeenAt = heardAt = lastRadioAt = -100f;
            safeIsland = StageBuilder.SafeGuardIsland(run.Level, origin);
            if (GuardDirector.Current != null) GuardDirector.Current.Register(this);
            warningEye = model.Find("Core/Crimson eye").GetComponent<Renderer>();
            // Unity instantiates a unique material in Play mode; only this sentinel flashes.
            if (Application.isPlaying) warningEye.material.color = new Color(1f, .22f, .13f);
            else if (warningEye.sharedMaterial != null) warningEye.sharedMaterial.color = new Color(1f, .22f, .13f);
            lastShot = Time.time + (boss ? 1.1f : (float)reactionRandom.NextDouble() * .7f);
        }
        public void Relocate(float x)
        {
            origin = x;
            transform.position = new Vector3(x, originalHeight, 0);
            lastSeenAt = heardAt = lastRadioAt = -100f;
            searchUntil = flankUntil = 0;
            tacticalUntil = 0;
            climbExitUntil = 0;
            lastLadderX = -999f;
            climbTop = 0;
            safeIsland = StageBuilder.SafeGuardIsland(game.Level, x);
            alert = Alert.Patrol;
            lastShot = Time.time + (float)reactionRandom.NextDouble() * .55f;
        }
        public void SetPatrolRange(float minX, float maxX)
        { patrolMin = minX; patrolMax = maxX; }
        private void Update()
        {
            if (game == null || !game.CanPlay || dying) return;
            var player = game.Player;
            float delta = player.transform.position.x - transform.position.x;
            if (Boss)
            {
                // The guardian waits at the final arena instead of firing at
                // the nearby story checkpoint and upload terminal.
                if (player.transform.position.x < 205f) return;
                if (phase == 0 && Health <= 11 || phase == 1 && Health <= 5)
                {
                    phase++; game.Flash("GUARDIAN PHASE " + (phase + 1), 2.2f);
                }
                transform.position = new Vector3(origin + Mathf.Sin(Time.time * (1.4f + phase * .6f)) * 1.6f,
                    3.7f + Mathf.Sin(Time.time * 2f) * .65f, 0);
            }
            else
            {
                bool onScreen = WithinCameraView();
                bool visible = onScreen && HasLineOfSight(player);
                if (visible)
                {
                    if (game.CarryingSignal) game.CarrierSpotted(player.transform.position);
                    // An ally hears the actual last-seen position, not the player's
                    // continually updated transform behind a wall.
                    bool firstSighting = Time.time - lastSeenAt > 2.5f;
                    lastSeenAt = Time.time;
                    lastSeenX = player.transform.position.x;
                    lastSeenY = player.transform.position.y;
                    searchUntil = Time.time + 5f;
                    if (GuardDirector.Current != null &&
                        (firstSighting || Time.time - lastRadioAt > 4f))
                    {
                        lastRadioAt = Time.time;
                        var velocity = player.GetComponent<Rigidbody>();
                        GuardDirector.Current.ShareSighting(this, player.transform.position,
                            velocity != null ? velocity.velocity.x : 0f);
                    }
                }
                bool wounded = Health == 1;
                Alert previous = alert;
                Vector3 target = transform.position;
                if (visible && wounded && Mathf.Abs(delta) < 6f)
                {
                    Vector3 cover;
                    if (TryFindCover(player, out cover)) { alert = Alert.Cover; target = cover; }
                    else { alert = Alert.Retreat; target.x -= Mathf.Sign(delta) * 3.5f; }
                }
                else if (visible && Time.time < flankUntil &&
                    (Time.time >= tacticalUntil || sweepOrder) && Mathf.Abs(delta) > 3f)
                {
                    alert = Alert.Flank;
                    target.x = player.transform.position.x +
                        (origin < player.transform.position.x ? 2.5f : -2.5f);
                    target.y = player.transform.position.y;
                }
                else if (visible)
                { alert = Alert.Attack; target = player.transform.position; }
                else if (Time.time < searchUntil)
                {
                    float distance = Mathf.Abs(lastSeenX - transform.position.x);
                    if (Time.time < tacticalUntil)
                    {
                        alert = sweepOrder ? Alert.Sweep : Alert.Overwatch;
                        target.x = tacticalTargetX;
                    }
                    else
                    {
                        alert = distance > 1.2f ? Alert.Investigate : Alert.Search;
                        target.x = distance > 1.2f ? lastSeenX :
                            lastSeenX + Mathf.Sin(Time.time * 2.1f + origin) * 1.5f;
                    }
                    target.y = lastSeenY;
                }
                else
                {
                    alert = Alert.Patrol;
                    target.x = origin + Mathf.Sin(Time.time * .9f + origin) * 2.3f;
                    target.y = originalHeight;
                }
                if ((alert == Alert.Attack || alert == Alert.Flank) &&
                    previous != Alert.Attack && previous != Alert.Flank)
                    lastShot = Mathf.Max(lastShot, Time.time + ReactionDelay());
                if (DefenseTarget != null && game.IsUploadingAt(DefenseTarget))
                {
                    alert = Alert.Attack;
                    target = DefenseTarget.transform.position;
                }
                MoveTo(target);
            }
            if (animator != null) animator.SetBool("Engaged", Boss || alert == Alert.Attack || alert == Alert.Flank);
            if (model != null) model.Rotate(0, Time.deltaTime * 80, 0);
            if (warningEye != null && !Boss)
                warningEye.material.color = (alert == Alert.Attack || alert == Alert.Flank) &&
                    lastShot - Time.time < .48f ? Color.yellow :
                    alert == Alert.Sweep ? Color.magenta :
                    alert == Alert.Overwatch ? new Color(.18f, .65f, 1f) :
                    alert == Alert.Search || alert == Alert.Investigate ? Color.cyan :
                    alert == Alert.Cover || alert == Alert.Retreat ? new Color(1f, .55f, .14f) :
                    Rapid ? new Color(1f, .45f, .13f) : new Color(1f, .22f, .13f);
            bool attackingStation = DefenseTarget != null && game.IsUploadingAt(DefenseTarget) &&
                Mathf.Abs(DefenseTarget.transform.position.x - transform.position.x) < 19f && WithinCameraView();
            if ((Boss ? Mathf.Abs(delta) < 22 : attackingStation ||
                (alert == Alert.Attack || alert == Alert.Flank) && WithinCameraView() && HasLineOfSight(player)) &&
                Time.time > lastShot)
            {
                lastShot = Time.time + (Boss ? 1.2f - phase * .22f : Rapid ?
                    1.38f - (game.Level - 1) * .14f : 1.78f - (game.Level - 1) * .18f) *
                    game.GuardFireIntervalMultiplier * (GuardDirector.Current != null && !Boss ?
                        1f - GuardDirector.Current.Pressure * .05f : 1f);
                Vector3 originShot = transform.position + Vector3.up * (Boss ? .4f : .2f);
                Vector3 predicted = attackingStation ? DefenseTarget.transform.position : player.transform.position;
                if (Boss && phase > 0 && player.IsDashing)
                {
                    var motion = player.GetComponent<Rigidbody>();
                    if (motion != null)
                        predicted.x += Mathf.Clamp(motion.velocity.x * .16f, -2f, 2f);
                    if (Time.time - lastWarnAt > 2f)
                    { lastWarnAt = Time.time; game.Flash("GUARDIAN TRACKING YOUR DASH", 1.4f); }
                }
                Vector3 aim = (predicted + (attackingStation ? Vector3.zero : Vector3.up *
                    (player.IsCrouching ? .55f : 1f)) - originShot).normalized;
                ProjectilePool.Current.Fire(originShot, aim, true);
                if (Boss && phase > 0)
                    ProjectilePool.Current.Fire(originShot, (aim + Vector3.up * .32f).normalized, true);
            }
            if (Vector3.Distance(player.transform.position, transform.position) < (Boss ? 3.1f : .95f))
                game.Hurt(1, "CONTACT");
        }
        private bool WithinCameraView()
        {
            var cam = Camera.main;
            if (cam == null) return false;
            Vector3 point = cam.WorldToViewportPoint(transform.position);
            return point.z > 0 && point.x > .03f && point.x < .97f &&
                point.y > .06f && point.y < .94f;
        }
        private float ReactionDelay()
        {
            // Per sighting, not per level: a minority fires at the next update,
            // others hesitate. Existing shot cooldown still prevents a barrage.
            int roll = reactionRandom.Next(100);
            return roll < 25 ? 0f : roll < 70 ?
                .12f + (float)reactionRandom.NextDouble() * .28f :
                .45f + (float)reactionRandom.NextDouble() * .4f;
        }
        private bool HasLineOfSight(Runner player)
        {
            Vector3 start = transform.position + Vector3.up * .2f;
            Vector3 target = player.transform.position + Vector3.up *
                (player.IsCrouching ? .55f : 1f);
            Vector3 path = target - start;
            // Ignore other guards in the squad. Low beams and breakable walls
            // still block the sight ray, so crouching and cover remain useful.
            foreach (RaycastHit hit in Physics.RaycastAll(start, path.normalized, path.magnitude,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                if (hit.collider.GetComponent<Sentinel>() == null &&
                    hit.collider.GetComponent<Runner>() == null) return false;
            return true;
        }

        private void MoveTo(Vector3 target)
        {
            Vector3 here = transform.position;
            float pace = alert == Alert.Patrol ? 1.1f : Rapid ? 2.65f : 2.2f;
            if (Flying)
            {
                Vector3 destination = new Vector3(Mathf.Clamp(target.x, origin - 11f, origin + 11f),
                    Mathf.Clamp(target.y + .6f, 1.7f, 8.7f), 0);
                Vector3 next = Vector3.MoveTowards(here, destination, pace * Time.deltaTime);
                if (GuardDirector.ClearRoute(this, next)) transform.position = next;
                return;
            }

            Vector3 ladderWaypoint = Vector3.zero;
            bool climbing;
            if (climbTop > 0 && here.y < climbTop - .1f)
            {
                climbing = true;
                ladderWaypoint = Mathf.Abs(here.x - climbX) > .35f ?
                    new Vector3(climbX, here.y, 0) : new Vector3(climbX, climbTop, 0);
            }
            else
            {
                climbTop = 0;
                climbing = GuardDirector.Current != null &&
                    GuardDirector.Current.TryLadder(this, target, out ladderWaypoint);
                if (climbing && ladderWaypoint.y > here.y + .25f)
                { climbTop = ladderWaypoint.y; climbX = ladderWaypoint.x; }
            }
            if (climbing) { target = ladderWaypoint; alert = Alert.Climb; }
            else target.y = here.y; // Ground patrols do not levitate onto arbitrary platforms.
            float x = Mathf.Clamp(target.x, Mathf.Max(safeIsland.x, origin - 11f),
                Mathf.Min(safeIsland.y, origin + 11f));
            Vector3 destinationGround = new Vector3(x, target.y, 0);
            Vector3 step = Vector3.MoveTowards(here, destinationGround, pace * Time.deltaTime);
            if (climbing && Mathf.Abs(step.y - destinationGround.y) < .1f)
            { lastLadderX = step.x; climbExitUntil = Time.time + 1.4f; }
            bool leavingLadder = Time.time < climbExitUntil &&
                Mathf.Abs(step.x - lastLadderX) < 3f;
            if (step.x != here.x && !climbing &&
                !leavingLadder &&
                (!GuardDirector.SolidFooting(step.x, here.y) ||
                 !GuardDirector.SolidFooting((step.x + here.x) * .5f, here.y))) return;
            if (GuardDirector.ClearRoute(this, step)) transform.position = step;
        }

        private bool TryFindCover(Runner player, out Vector3 cover)
        {
            cover = transform.position;
            if (Flying || GuardDirector.Current == null) return false;
            // Sample reachable points on the same solid island. Only a real
            // blocking collider makes a candidate count as cover.
            float away = transform.position.x < player.transform.position.x ? -1f : 1f;
            foreach (float side in new[] { away, -away })
                for (float distance = 2f; distance <= 5f; distance += 1f)
                {
                    Vector3 candidate = new Vector3(transform.position.x + side * distance,
                        transform.position.y, 0);
                    if (!WithinSafeIsland(candidate.x) || !GuardDirector.SolidFooting(candidate.x, candidate.y) ||
                        !GuardDirector.ClearRoute(this, candidate)) continue;
                    Vector3 from = candidate + Vector3.up * .15f;
                    Vector3 to = player.transform.position + Vector3.up;
                    Vector3 ray = to - from;
                    foreach (RaycastHit hit in Physics.RaycastAll(from, ray.normalized, ray.magnitude,
                        Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                        if (hit.collider.GetComponent<Sentinel>() == null &&
                            hit.collider.GetComponent<Runner>() == null)
                        { cover = candidate; return true; }
                }
            return false;
        }

        private void OnDestroy()
        { if (GuardDirector.Current != null) GuardDirector.Current.Unregister(this); }

        public void TakeDamage(int damage)
        {
            if (Boss && game.RelaysLeft > 0)
            {
                game.Flash("GUARDIAN SHIELD ACTIVE  /  BREAK THE THREE RELAYS", 1.8f);
                return;
            }
            if (dying || Time.time - lastHit < .18f) return;
            lastHit = Time.time; Health -= damage;
            if (Boss) game.UpdateBossHealth(Health);
            if (Health > 0) return;
            dying = true;
            RunGame.Burst(transform.position, Boss ? new Color(.81f, .32f, 1f) : new Color(1f, .36f, .18f));
            if (!Boss && DefenseTarget != null) game.UploadAttackerDefeated(DefenseTarget);
            if (Boss) game.BossDefeated(); else game.DefeatEnemy(150);
            var body = GetComponent<Collider>(); if (body != null) body.enabled = false;
            Destroy(gameObject, .2f);
        }
    }
}
