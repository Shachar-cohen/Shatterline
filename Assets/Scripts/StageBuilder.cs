using System.Collections.Generic;
using UnityEngine;

namespace Shatterline
{
    // Runtime variation uses saved spawn sockets and prefab assets. Geometry is authored in Editor.
    public static class StageBuilder
    {
        public const float PortalX = 250f;
        private static StageScene scene;
        private static Transform root;
        private static System.Random layoutRandom;
        private static int firstSignalSlot, secondSignalSlot;
        private static readonly int[] lastFirstSlot = { -1, -1, -1 };
        private static readonly int[] lastSecondSlot = { -1, -1, -1 };
        private static readonly List<GuardSpawnPlan> guardPlans = new List<GuardSpawnPlan>();
        private static readonly List<TurretSpawnPlan> turretPlans = new List<TurretSpawnPlan>();
        private static readonly Dictionary<string, Material> effects = new Dictionary<string, Material>();

        public static void Initialize(StageScene bindings, RunGame game)
        {
            Release();
            scene = bindings; root = bindings.LevelRoot;
            int sector = bindings.Act;
            layoutRandom = new System.Random(unchecked(RunState.RunSeed ^ (sector * 19391)));
            firstSignalSlot = layoutRandom.Next(3); secondSignalSlot = layoutRandom.Next(3);
            if (firstSignalSlot == lastFirstSlot[sector - 1]) firstSignalSlot = (firstSignalSlot + 1 + layoutRandom.Next(2)) % 3;
            if (secondSignalSlot == lastSecondSlot[sector - 1]) secondSignalSlot = (secondSignalSlot + 1 + layoutRandom.Next(2)) % 3;
            lastFirstSlot[sector - 1] = firstSignalSlot; lastSecondSlot[sector - 1] = secondSignalSlot;
            foreach (var variation in bindings.RouteVariations) variation.Apply(layoutRandom);
            bool firstLow = layoutRandom.Next(2) == 0;
            foreach (var pocket in bindings.GridPockets) pocket.Apply(firstLow, layoutRandom);
            Physics.SyncTransforms();
            foreach (var fragment in bindings.Fragments)
                if (fragment != null && !fragment.Healing)
                    fragment.PlaceForNewLife(SignalSite(sector, fragment.SignalIndex,
                        fragment.SignalIndex == 0 ? firstSignalSlot : secondSignalSlot));
            foreach (var authored in bindings.GuardPlans)
            {
                var plan = authored.Copy();
                if (plan.Sites != null && plan.Sites.Length > 0) plan.Anchor = plan.Sites[layoutRandom.Next(plan.Sites.Length)];
                Vector2 bounds = GuardRange(sector, plan.Anchor, plan.Boss, plan.Flying);
                plan.MinX = bounds.x; plan.MaxX = bounds.y;
                if (plan.Active != null)
                {
                    plan.Active.transform.position = new Vector3(Roll(layoutRandom, bounds.x, bounds.y), plan.Height, 0);
                    plan.Active.Configure(game, plan.Boss, plan.Rapid);
                    plan.Active.Flying = plan.Flying;
                    plan.Active.SetPatrolRange(bounds.x, bounds.y);
                }
                guardPlans.Add(plan);
            }
            foreach (var authored in bindings.TurretPlans)
            {
                var plan = authored.Copy();
                plan.Slot = layoutRandom.Next(plan.Sites.Length);
                if (plan.Active != null)
                {
                    plan.Active.Relocate(plan.Sites[plan.Slot]);
                    plan.Active.InitializeRuntime(game);
                }
                turretPlans.Add(plan);
            }
        }

        public static Material Material(Color tint, string texture = null)
        {
            string key = tint.ToString() + ":" + texture;
            Material value;
            if (effects.TryGetValue(key, out value)) return value;
            Material template = scene != null ? scene.EffectMaterial : null;
#if UNITY_EDITOR
            if (template == null && !Application.isPlaying) template = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/StageStandard.mat");
#endif
            if (template == null) throw new System.InvalidOperationException("Stage effect material is not assigned.");
            value = new Material(template); value.color = tint;
            if (Application.isPlaying) effects.Add(key, value); return value;
        }

        public static Sentinel SpawnSentinel(RunGame game, float x, float y, bool boss, bool flying,
            bool rapid = false, float forcedX = -999f)
        {
            if (scene == null || scene.SentinelPrefab == null)
                throw new System.InvalidOperationException("Sentinel prefab is missing from StageScene.");
            Vector2 bounds = GuardRange(game.Level, x, boss, flying);
            float actualX = forcedX < -500f ? Roll(layoutRandom, bounds.x, bounds.y) : forcedX;
            Sentinel brain = Object.Instantiate(scene.SentinelPrefab, new Vector3(actualX, y, 0), Quaternion.identity, root);
            brain.name = rapid ? "Rapid-fire scout / AI" : "Rift sentinel / AI";
            brain.Configure(game, boss, rapid); brain.Flying = flying;
            brain.SetPatrolRange(bounds.x, bounds.y);
            return brain;
        }

        private static SentryTurret CreateTurret(RunGame game, float x, float y)
        {
            if (scene == null || scene.TurretPrefab == null)
                throw new System.InvalidOperationException("Turret prefab is missing from StageScene.");
            var turret = Object.Instantiate(scene.TurretPrefab, new Vector3(x, y, 0), Quaternion.identity, root);
            turret.InitializeRuntime(game); return turret;
        }

        public static ParticleSystem BurstPrefab { get { return scene != null ? scene.BurstPrefab : null; } }

        public static void Release()
        {
            foreach (var material in effects.Values) if (material != null) Object.Destroy(material);
            effects.Clear(); guardPlans.Clear(); turretPlans.Clear(); scene = null; root = null;
        }
        private static float Roll(System.Random rng, float min, float max)
        { return min + (float)rng.NextDouble() * (max - min); }

        private static Vector3 SignalSite(int sector, int index, int slot)
        {
            if (sector == 2)
            {
                if (index == 0) return slot == 0 ? new Vector3(9f, 4.5f, 0) :
                    slot == 1 ? new Vector3(47f, 6.3f, 0) : new Vector3(68f, 4f, 0);
                return slot == 0 ? new Vector3(139f, 3.2f, 0) :
                    slot == 1 ? new Vector3(145f, 3.3f, 0) : new Vector3(190f, 3.3f, 0);
            }
            if (sector == 3)
            {
                if (index == 0) return slot == 0 ? new Vector3(12f, 5.6f, 0) :
                    slot == 1 ? new Vector3(45.8f, 6f, 0) : new Vector3(66f, 5.5f, 0);
                return slot == 0 ? new Vector3(142.5f, 3.2f, 0) :
                    slot == 1 ? new Vector3(147.5f, 4.2f, 0) : new Vector3(189f, 3.3f, 0);
            }
            if (index == 0)
                return slot == 0 ? new Vector3(11.7f, 5f, 0) :
                    slot == 1 ? new Vector3(45.8f, 5.4f, 0) : new Vector3(68.3f, 3.1f, 0);
            return slot == 0 ? new Vector3(142.5f, 3.2f, 0) :
                slot == 1 ? new Vector3(147.5f, 4.2f, 0) : new Vector3(188.5f, 3.3f, 0);
        }

        private static float[,] GroundSections(int sector)
        {
            return sector == 1 ? new float[,] {
                { -3, 16 }, { 21, 34 }, { 39, 58 }, { 64, 82 }, { 87, 107 },
                { 112, 128 }, { 134, 150 }, { 155, 174 }, { 180, 209 },
                { 217, 231 }, { 237, 261 }
            } : sector == 2 ? new float[,] {
                { -3, 17 }, { 23, 37 }, { 41, 56 }, { 62, 82 }, { 88, 108 },
                { 116, 131 }, { 137, 151 }, { 156, 175 }, { 182, 209 },
                { 218, 233 }, { 239, 262 }
            } : new float[,] {
                { -3, 14 }, { 22, 38 }, { 43, 59 }, { 65, 83 }, { 90, 109 },
                { 116, 131 }, { 138, 152 }, { 158, 178 }, { 184, 209 },
                { 216, 229 }, { 237, 261 }
            };
        }

        public static bool InsideGroundIsland(int sector, float x, float edgeMargin)
        {
            float[,] sections = GroundSections(sector);
            for (int i = 0; i < sections.GetLength(0); i++)
                if (x >= sections[i, 0] + edgeMargin && x <= sections[i, 1] - edgeMargin)
                    return true;
            return false;
        }

        public static Vector2 SafeGuardIsland(int sector, float x)
        {
            float[,] sections = GroundSections(sector);
            for (int i = 0; i < sections.GetLength(0); i++)
                if (x >= sections[i, 0] && x <= sections[i, 1])
                    return new Vector2(sections[i, 0] + 1.1f, sections[i, 1] - 1.1f);
            // Flying guards do not use this range; a stranded scripted guard stays put.
            return new Vector2(x - .5f, x + .5f);
        }

        private static Vector2 GuardRange(int sector, float anchor, bool boss, bool flying)
        {
            if (boss) return new Vector2(221.5f, 222.5f);
            if (flying) return new Vector2(anchor - 3.2f, anchor + 3.2f);
            if (anchor > 45.5f && anchor < 47f)
                return new Vector2(45.72f, 46.4f); // Clear pocket beside the functional parkour ladder.
            // These anchors formerly intersected the parkour pillars or the
            // second mandatory gate. Place the guard on playable open ground.
            if (anchor > 46f && anchor < 52f) return new Vector2(53.95f, 55.3f);
            if (sector >= 2 && anchor > 124f && anchor < 130f)
                return new Vector2(122f, 125f);
            if (anchor >= 28f && anchor <= 30f)
                return new Vector2(27.7f, 31.5f);
            if (anchor >= 103f && anchor <= 106f)
                return new Vector2(103.5f, 106.2f);
            float[,] sections = GroundSections(sector);
            for (int i = 0; i < sections.GetLength(0); i++)
            {
                float begin = sections[i, 0], end = sections[i, 1];
                if (anchor < begin || anchor > end) continue;
                float min = Mathf.Max(anchor - 4f, begin + 2f);
                float max = Mathf.Min(anchor + 4f, end - 2f);
                if (anchor >= 9f && anchor <= 15f) min = Mathf.Max(min, 9.2f);
                if (min < max) return new Vector2(min, max);
            }
            return new Vector2(anchor - 1f, anchor + 1f);
        }

        public static void RerollAfterDeath(int sector, int deathCount, float respawnX,
            Fragment[] signals)
        {
            var rng = new System.Random(unchecked(RunState.RunSeed ^ (sector * 19391) ^
                (deathCount * 7919)));
            firstSignalSlot = (firstSignalSlot + 1 + rng.Next(2)) % 3;
            secondSignalSlot = (secondSignalSlot + 1 + rng.Next(2)) % 3;
            lastFirstSlot[sector - 1] = firstSignalSlot;
            lastSecondSlot[sector - 1] = secondSignalSlot;
            if (signals != null)
                foreach (var fragment in signals)
                    if (fragment != null && !fragment.Healing && !fragment.Collected)
                    {
                        int slot = fragment.SignalIndex == 0 ? firstSignalSlot : secondSignalSlot;
                        fragment.PlaceForNewLife(SignalSite(sector, fragment.SignalIndex, slot));
                    }
            
            foreach (var plan in guardPlans)
            {
                if (plan.Boss && RunGame.Current.BossDown) continue;
                if (plan.Sites != null && plan.Sites.Length > 1)
                {
                    // Change the actual encounter pocket after a death, not only
                    // a tiny offset inside the same predefined patrol range.
                    int next = rng.Next(plan.Sites.Length);
                    if (Mathf.Abs(plan.Sites[next] - plan.Anchor) < 3f)
                        next = (next + 1) % plan.Sites.Length;
                    plan.Anchor = plan.Sites[next];
                    Vector2 chosen = GuardRange(sector, plan.Anchor, plan.Boss, plan.Flying);
                    plan.MinX = chosen.x;
                    plan.MaxX = chosen.y;
                }
                float newX = Roll(rng, plan.MinX, plan.MaxX);
                if (plan.Active != null && Mathf.Abs(newX - plan.Active.HomeX) < .55f)
                    newX = Mathf.Abs(plan.MinX - plan.Active.HomeX) >
                        Mathf.Abs(plan.MaxX - plan.Active.HomeX) ? plan.MinX : plan.MaxX;
                // Checkpoint safety takes precedence over position variety.
                if (Mathf.Abs(newX - respawnX) < 6f)
                    newX = Mathf.Abs(plan.MinX - respawnX) >
                        Mathf.Abs(plan.MaxX - respawnX) ? plan.MinX : plan.MaxX;
                if (plan.Active == null)
                    plan.Active = SpawnSentinel(RunGame.Current, plan.Anchor, plan.Height,
                        plan.Boss, plan.Flying, plan.Rapid, newX);
                else plan.Active.Relocate(newX);
                if (plan.Active != null) plan.Active.SetPatrolRange(plan.MinX, plan.MaxX);
            }
            foreach (TurretSpawnPlan plan in turretPlans)
            {
                // Flip the sentry's authored socket after each death. Checkpoint
                // islands and the final gate remain outside these sockets.
                plan.Slot = (plan.Slot + 1) % plan.Sites.Length;
                float x = plan.Sites[plan.Slot];
                if (plan.Active == null) plan.Active = CreateTurret(RunGame.Current, x, plan.Height);
                else plan.Active.Relocate(x);
            }
            // Surviving one-time ambush units also move after a rewind.
            foreach (var other in Object.FindObjectsOfType<Sentinel>())
            {
                if (guardPlans.Exists(plan => plan.Active == other)) continue;
                float newX = Roll(rng, other.PatrolMin, other.PatrolMax);
                if (Mathf.Abs(newX - other.HomeX) < .55f)
                    newX = Mathf.Abs(other.PatrolMin - other.HomeX) >
                        Mathf.Abs(other.PatrolMax - other.HomeX) ? other.PatrolMin : other.PatrolMax;
                if (Mathf.Abs(newX - respawnX) < 6f)
                    newX = Mathf.Abs(other.PatrolMin - respawnX) >
                        Mathf.Abs(other.PatrolMax - respawnX) ? other.PatrolMin : other.PatrolMax;
                other.Relocate(newX);
            }
        }
    }
}
