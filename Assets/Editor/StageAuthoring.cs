using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Shatterline
{
    public static class StageAuthoring
    {
        public const float PortalX = 250f;
        private static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();
        private static Transform root;
        private static Color stone, trim, glow;
        private static System.Random layoutRandom;
        private static int firstSignalSlot, secondSignalSlot;
        private static readonly int[] lastFirstSlot = { -1, -1, -1 };
        private static readonly int[] lastSecondSlot = { -1, -1, -1 };
        private static bool recordingGuards;
        private sealed class GuardPlan
        {
            public float Anchor, Height, MinX, MaxX;
            public bool Boss, Flying, Rapid;
            public float[] Sites;
            public Sentinel Active;
        }
        private static readonly List<GuardPlan> guardPlans = new List<GuardPlan>();
        private sealed class TurretPlan
        {
            public float[] Sites;
            public float Height;
            public int Slot;
            public SentryTurret Active;
        }
        private static readonly List<TurretPlan> turretPlans = new List<TurretPlan>();
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
        public static Material Material(Color tint, string texture = null)
        {
            string key = tint.ToString() + ":" + texture;
            Material value;
            if (cache.TryGetValue(key, out value) && value != null) return value;
            // A serialized material reference keeps its shader in standalone builds. Shader.Find
            // alone can succeed in the Editor yet return null in a stripped player.
            var template = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/StageStandard.mat");
            Shader shader = template != null ? template.shader : Shader.Find("Standard");
            if (shader == null)
                throw new System.InvalidOperationException(
                    "StageStandard material/shader is missing. Reopen the project in Unity " +
                    "to generate Assets/Art/Materials/StageStandard.mat, then rebuild.");
            value = template != null ? new Material(template) : new Material(shader);
            value.color = tint;
            if (texture != null)
            {
                var bitmap = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Textures/" + texture + ".png");
                if (bitmap != null) value.mainTexture = bitmap;
            }
            value.SetFloat("_Glossiness", .23f);
            cache[key] = value;
            return value;
        }
        private static GameObject Shape(PrimitiveType type, string title, Vector3 position,
            Vector3 scale, Color tint, string texture = null, Transform parent = null, bool solid = true)
        {
            GameObject obj = GameObject.CreatePrimitive(type);
            obj.name = title; obj.transform.SetParent(parent == null ? root : parent, false);
            obj.transform.localPosition = position; obj.transform.localScale = scale;
            obj.GetComponent<Renderer>().sharedMaterial = Material(tint, texture);
            if (!solid) Object.DestroyImmediate(obj.GetComponent<Collider>());
            return obj;
        }
        private static void Glow(Vector3 location, Color color, float range, float power)
        {
            var go = new GameObject("Rift light"); go.transform.SetParent(root, false);
            go.transform.localPosition = location;
            var lamp = go.AddComponent<Light>(); lamp.type = LightType.Point;
            lamp.color = color; lamp.range = range; lamp.intensity = power;
        }
        public static void Build(int sector, RunGame game)
        {
            layoutRandom = new System.Random(unchecked(RunState.RunSeed ^ (sector * 19391)));
            firstSignalSlot = layoutRandom.Next(3);
            secondSignalSlot = layoutRandom.Next(3);
            if (firstSignalSlot == lastFirstSlot[sector - 1])
                firstSignalSlot = (firstSignalSlot + 1 + layoutRandom.Next(2)) % 3;
            if (secondSignalSlot == lastSecondSlot[sector - 1])
                secondSignalSlot = (secondSignalSlot + 1 + layoutRandom.Next(2)) % 3;
            lastFirstSlot[sector - 1] = firstSignalSlot;
            lastSecondSlot[sector - 1] = secondSignalSlot;
            cache.Clear();
            guardPlans.Clear();
            turretPlans.Clear();
            root = new GameObject("ACT " + sector + " / THE BREAKING CITY").transform;
            stone = sector == 1 ? new Color(.34f, .49f, .57f) : sector == 2 ?
                new Color(.52f, .36f, .44f) : new Color(.26f, .29f, .44f);
            trim = sector == 1 ? new Color(.2f, .76f, .88f) : sector == 2 ?
                new Color(1f, .63f, .3f) : new Color(.73f, .36f, .95f);
            glow = sector == 1 ? new Color(.28f, .83f, 1f) : sector == 2 ?
                new Color(1f, .52f, .27f) : new Color(.82f, .4f, 1f);
            Camera camera = Object.FindObjectOfType<Camera>();
            if (camera == null)
            {
                var holder = new GameObject("Side camera"); camera = holder.AddComponent<Camera>();
                camera.tag = "MainCamera"; holder.AddComponent<AudioListener>();
            }
            camera.orthographic = true; camera.orthographicSize = 7.4f;
            camera.backgroundColor = sector == 1 ? new Color(.06f, .12f, .19f) :
                sector == 2 ? new Color(.20f, .10f, .15f) : new Color(.08f, .05f, .15f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            RenderSettings.ambientLight = new Color(.62f, .65f, .78f);
            RenderSettings.fog = true; RenderSettings.fogColor = camera.backgroundColor;
            RenderSettings.fogMode = FogMode.Linear; RenderSettings.fogStartDistance = 22;
            RenderSettings.fogEndDistance = 61;
            var sun = new GameObject("Sun / cold backlight").AddComponent<Light>();
            sun.type = LightType.Directional; sun.color = new Color(1f, .94f, .84f);
            sun.intensity = .95f; sun.transform.rotation = Quaternion.Euler(35, -28, 0);
            BuildBackground(sector);
            var pool = new GameObject("Bounded projectile pool").AddComponent<ProjectilePool>();
            pool.BuildInEditor();
            Platforms(sector);
            var ai = new GameObject("Guard AI / sound, radio and safe routes");
            ai.transform.SetParent(root, false);
            ai.AddComponent<GuardDirector>().CacheLadders();
            Player(game);
            recordingGuards = true;
            Details(sector, game);
            recordingGuards = false;
            var runner = Object.FindObjectOfType<Runner>();
            var director = new GameObject("Adaptive encounter director / observed play style");
            director.transform.SetParent(root, false);
            director.AddComponent<AdaptiveDirector>().Initialize(game, runner);
            var drone = new GameObject("Companion drone / scan or intercept");
            drone.transform.SetParent(root, false);
            drone.AddComponent<DroneCompanion>().Initialize(game, runner);
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
            recordingGuards = false;
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
            foreach (TurretPlan plan in turretPlans)
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
        private static void BuildBackground(int sector)
        {
            var artwork = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Textures/CityBackdrop.png");
            if (artwork == null) return;
            var panorama = Sprite.Create(artwork, new Rect(0, 0, artwork.width, artwork.height),
                new Vector2(.5f, .5f), artwork.width / 80f);
            for (int i = 0; i < 4; i++)
            {
                var sky = new GameObject("Distant noninteractive panorama " + i);
                sky.transform.SetParent(root, false);
                sky.transform.localPosition = new Vector3(36 + i * 80, 3, 24);
                var display = sky.AddComponent<SpriteRenderer>();
                display.sprite = panorama;
                display.color = sector == 1 ? new Color(.8f, .89f, 1f) :
                    sector == 2 ? new Color(1f, .79f, .65f) : new Color(.76f, .7f, 1f);
                display.sortingOrder = -100;
            }
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
        private static void Platforms(int sector)
        {
            float[,] sections = GroundSections(sector);
            Color walkway = sector == 1 ? new Color(.72f, .76f, .75f) :
                sector == 2 ? new Color(.76f, .62f, .45f) : new Color(.56f, .56f, .73f);
            for (int i = 0; i < sections.GetLength(0); i++)
            {
                float begin = sections[i, 0], end = sections[i, 1];
                float middle = (begin + end) / 2, width = end - begin;
                Shape(PrimitiveType.Cube, "Walkable bridge", new Vector3(middle, -.6f, 0),
                    new Vector3(width, 1.2f, 4), stone * .58f, "stone");
                Shape(PrimitiveType.Cube, "Walkable top / light stone", new Vector3(middle, .055f, 0),
                    new Vector3(width, .105f, 4.03f), walkway,
                    sector == 2 ? "metal" : sector == 3 ? "masonry" : "stone");
                Shape(PrimitiveType.Cube, "Front face / deep shadow", new Vector3(middle, -.57f, -2.06f),
                    new Vector3(width, 1.13f, .12f), stone * .33f, "masonry", null, false);
                Shape(PrimitiveType.Cube, "Playable floor / luminous edge", new Vector3(middle, .10f, -2.12f),
                    new Vector3(width, .105f, .11f), trim, null, null, false);
                foreach (float cap in new[] { begin + .13f, end - .13f })
                    Shape(PrimitiveType.Cube, "Floor boundary marker", new Vector3(cap, -.06f, -2.14f),
                        new Vector3(.17f, .43f, .12f), glow, null, null, false);
                Shape(PrimitiveType.Cube, "Understructure", new Vector3(middle, -2.1f, .3f),
                    new Vector3(width - .25f, 1.8f, 2.1f), stone * .57f, "metal");
                for (int p = 0; p < Mathf.FloorToInt(width / 2f); p++)
                {
                    float x = begin + 1 + p * 2;
                    Shape(PrimitiveType.Cube, "Bridge rivet", new Vector3(x, .13f, -1.9f),
                        new Vector3(.07f, .07f, .07f), new Color(.87f, .8f, .65f), null, null, false);
                }
            }
            // Every fatal gap has a reachable escape on either bank. The rails
            // hang below the walking surface, so a missed jump can still be
            // saved by steering into one of them and holding R in time.
            for (int i = 0; i < sections.GetLength(0) - 1; i++)
            {
                AbyssEdgeLadder(sections[i, 1], 1);
                AbyssEdgeLadder(sections[i + 1, 0], -1);
            }
            // Each district has an authored silhouette. The small offsets below
            // vary each new run without moving the mandatory ladder approaches.
            float[] platforms = sector == 1 ?
                new[] { 11f, 27f, 0f, 59f, 84f, 93f, 109f, 125f, 140f, 156f, 172f, 189f } :
                sector == 2 ?
                new[] { 0f, 0f, 0f, 64f, 90f, 104f, 117f, 0f, 141f, 159f, 176f, 192f } :
                new[] { 0f, 0f, 0f, 68f, 94f, 102f, 116f, 0f, 141f, 154f, 177f, 194f };
            for (int i = 0; i < platforms.Length; i++)
            {
                if (platforms[i] == 0f) continue;
                float x = platforms[i] +
                    (i == 4 ? 0f : Roll(layoutRandom, -.65f, .65f));
                float elevation = (sector == 1 ? 2.8f : sector == 2 ? 3.5f : 4.1f) +
                    (i % 3) * (sector == 1 ? .85f : sector == 2 ? 1.0f : 1.25f) +
                    (i == 0 ? Roll(layoutRandom, 0f, .35f) : Roll(layoutRandom, -.35f, .35f));
                var platformRoot = new GameObject("PLAYABLE elevated platform " + i);
                platformRoot.transform.SetParent(root, false);
                platformRoot.transform.localPosition = new Vector3(x, elevation, 0);
                var variation = platformRoot.AddComponent<RouteVariation>();
                variation.Anchor = new Vector3(platforms[i],
                    (sector == 1 ? 2.8f : sector == 2 ? 3.5f : 4.1f) +
                    (i % 3) * (sector == 1 ? .85f : sector == 2 ? 1f : 1.25f), 0);
                variation.HorizontalRange = i == 4 ? Vector2.zero : new Vector2(-.65f, .65f);
                variation.VerticalRange = i == 0 ? new Vector2(0, .35f) : new Vector2(-.35f, .35f);
                Shape(PrimitiveType.Cube, "Solid landing surface", Vector3.zero,
                    new Vector3(5.3f, .38f, 3), stone * .67f, sector == 3 ? "masonry" : "metal",
                    platformRoot.transform);
                Shape(PrimitiveType.Cube, "Light top / visual landing plane", new Vector3(0, .235f, 0),
                    new Vector3(5.35f, .09f, 3.05f), walkway, "metal",
                    platformRoot.transform);
                Shape(PrimitiveType.Cube, "Bright front lip", new Vector3(0, .19f, -1.57f),
                    new Vector3(5.48f, .16f, .11f), trim, null, platformRoot.transform, false);
                foreach (float side in new[] { -2.57f, 2.57f })
                    Shape(PrimitiveType.Cube, "Landing corner", new Vector3(side, .09f, -1.6f),
                        new Vector3(.15f, .35f, .12f), glow, null, platformRoot.transform, false);
                if (sector == 2 && i % 3 == 1)
                    platformRoot.AddComponent<MovingPlatform>().Travel = .85f;
                if (sector == 3 && i == 9)
                    platformRoot.AddComponent<MovingPlatform>().Travel = .55f;
            }
            if (sector == 2)
            {
                // Act II begins with moving machinery rather than Act I's fixed steps.
                MovingStep(10f, 3.35f, 3.8f, 1f);
                MovingStep(27.8f, 3.2f, 4.4f, .95f);
            }
            if (sector == 3)
            {
                // A broad opening shelf above the first rift carries the early
                // signal, replacing the repeated sequence of small steps.
                Shape(PrimitiveType.Cube, "PLAYABLE core opening shelf",
                    new Vector3(11.5f, 4.15f, 0), new Vector3(9.5f, .42f, 3f),
                    stone * .7f, "masonry");
                Shape(PrimitiveType.Cube, "Core shelf / bright walking edge",
                    new Vector3(11.5f, 4.41f, -1.56f), new Vector3(9.55f, .1f, .12f),
                    trim, null, null, false);
                Shape(PrimitiveType.Cube, "PLAYABLE core landing staircase",
                    new Vector3(27.6f, 2.45f, 0), new Vector3(3.9f, .36f, 3f),
                    stone * .69f, "masonry");
            }
            ExtendedRoute(sector);
            // Each district has its own additional, genuinely collidable route.
            float[] detours = sector == 1 ? new[] { 18.5f, 136f } :
                sector == 2 ? new[] { 36.5f, 108.5f } : new[] { 112f, 193f };
            foreach (float anchor in detours)
            {
                float x = anchor + Roll(layoutRandom, -.65f, .65f);
                float y = sector == 1 ? 4f : sector == 2 ? 4.5f : 5.1f;
                var group = new GameObject("District shortcut variation"); group.transform.SetParent(root, false);
                group.transform.localPosition = new Vector3(x, 0, 0);
                var variation = group.AddComponent<RouteVariation>(); variation.Anchor = new Vector3(anchor, 0, 0);
                variation.HorizontalRange = new Vector2(-.65f, .65f);
                var floor = Shape(PrimitiveType.Cube, "PLAYABLE district-specific shortcut",
                    new Vector3(x, y, 0), new Vector3(3.6f, .4f, 3f), stone * .68f, "metal");
                var top = Shape(PrimitiveType.Cube, "District shortcut / solid bright top",
                    new Vector3(x, y + .24f, 0), new Vector3(3.65f, .08f, 3.05f), trim);
                floor.transform.SetParent(group.transform, true); top.transform.SetParent(group.transform, true);
            }
            foreach (float x in new[] { 72f, 123f, 163f })
            {
                // The first two towers are now genuine ladder-only crossings.
                // Leaving the old jumpable ledges here would let players skip R.
                if (x == 72f || (sector >= 2 && x == 123f)) continue;
                float height = sector == 3 ? 6.7f : 5.4f;
                BuildServiceColumn(x, height);
                // Jump from the solid column to this solid raised walkway.
                Shape(PrimitiveType.Cube, "PLAYABLE high route ledge", new Vector3(x + 5.5f, height - .48f, 0),
                    new Vector3(7f, .32f, 3), stone * .65f, "masonry");
                Shape(PrimitiveType.Cube, "High ledge / bright walking edge", new Vector3(x + 5.5f, height - .25f, -1.56f),
                    new Vector3(7f, .13f, .15f), trim, null, null, false);
            }
            foreach (float gate in sector == 1 ? new[] { 77.5f } : new[] { 77.5f, 128.5f })
            {
                // No preceding jumpable tower reaches this height. Climb the
                // marked ladder with R and step right over the solid top.
                Shape(PrimitiveType.Cube, "SOLID climb-over security bulkhead",
                    new Vector3(gate, 6.2f, 0), new Vector3(1.18f, 12.4f, 3.4f),
                    stone * .74f, "metal");
                Shape(PrimitiveType.Cube, "High gate / visible amber cap",
                    new Vector3(gate, 12.38f, -1.68f), new Vector3(1.55f, .14f, .24f),
                    new Color(1f, .72f, .19f));
                SideClimb(gate, .59f, 12.4f, -1);
                var sign = new GameObject("Mandatory ladder / floating HOLD R sign");
                sign.transform.SetParent(root, false);
                sign.transform.localPosition = new Vector3(gate - 1.62f, 2.25f, -1.82f);
                var label = sign.AddComponent<TextMesh>();
                label.text = "HOLD R";
                label.fontSize = 54;
                label.characterSize = .16f;
                label.anchor = TextAnchor.MiddleCenter;
                label.alignment = TextAlignment.Center;
                label.color = new Color(.24f, 1f, 1f);
                Shape(PrimitiveType.Cube, "PLAYABLE descent step beyond ladder gate",
                    new Vector3(gate + 3.35f, 9.1f, 0), new Vector3(5.25f, .36f, 3),
                    stone * .68f, "metal");
                Shape(PrimitiveType.Cube, "Descent step / solid illuminated top",
                    new Vector3(gate + 3.35f, 9.31f, 0), new Vector3(5.3f, .07f, 3.05f), trim);
                Shape(PrimitiveType.Cube, "PLAYABLE lower descent step",
                    new Vector3(gate + 7.5f, 5f, 0), new Vector3(5.3f, .36f, 3),
                    stone * .68f, "metal");
                Shape(PrimitiveType.Cube, "Lower descent / solid illuminated top",
                    new Vector3(gate + 7.5f, 5.21f, 0), new Vector3(5.35f, .07f, 3.05f), trim);
                var guide = Shape(PrimitiveType.Cube, "Ladder detour prompt",
                    new Vector3(gate - 8f, 1.1f, 0), new Vector3(.55f, 2.2f, 2.3f),
                    Color.clear, null, null, false);
                guide.AddComponent<BoxCollider>().isTrigger = true;
                guide.AddComponent<ClimbDetourPrompt>();
                guide.GetComponent<Renderer>().enabled = false;
            }
            BuildColumnCrossing(sector);
            if (sector == 2)
            {
                // A moving step across the first clockwork trench changes
                // the route without trapping a player who misses the jump.
                var step = new GameObject("PLAYABLE clockwork trench lift");
                step.transform.SetParent(root, false);
                step.transform.localPosition = new Vector3(37.5f, 2.45f, 0);
                Shape(PrimitiveType.Cube, "SOLID moving lift base", Vector3.zero,
                    new Vector3(3.7f, .36f, 3f), stone, "metal", step.transform);
                Shape(PrimitiveType.Cube, "SOLID lift / bright walking face",
                    new Vector3(0, .22f, 0), new Vector3(3.75f, .08f, 3.05f), trim,
                    "metal", step.transform);
                step.AddComponent<MovingPlatform>().Travel = .55f;
            }
            if (sector == 3)
            {
                // The core's first rift is crossed via a fixed raised landing;
                // the last rift uses the moving x154 platform built above.
                Shape(PrimitiveType.Cube, "PLAYABLE core rift landing",
                    new Vector3(39f, 3.1f, 0), new Vector3(3.7f, .36f, 3f), stone, "masonry");
                Shape(PrimitiveType.Cube, "SOLID core landing / purple top",
                    new Vector3(39f, 3.32f, 0), new Vector3(3.76f, .08f, 3.05f), trim, "metal");
                foreach (float x in new[] { 120f, 134.5f })
                {
                    Shape(PrimitiveType.Cube, "PLAYABLE core tier",
                        new Vector3(x, 4.3f, 0), new Vector3(3.7f, .4f, 3f), stone, "masonry");
                    Shape(PrimitiveType.Cube, "SOLID core tier / luminous edge",
                        new Vector3(x, 4.54f, 0), new Vector3(3.74f, .08f, 3.05f), trim, "metal");
                }
            }
            Shape(PrimitiveType.Cylinder, "SOLID glowing exit pad on the ground",
                new Vector3(PortalX, .08f, 0), new Vector3(2.4f, .08f, 2.4f),
                trim, "metal");
            Glow(new Vector3(PortalX, 1.1f, -1.1f), glow, 5f, 1.6f);
        }

        private static void AbyssEdgeLadder(float bankEdge, int towardGap)
        {
            float x = bankEdge + towardGap * .34f;
            const float bottom = -2.75f, top = .75f;
            for (int side = -1; side <= 1; side += 2)
                Shape(PrimitiveType.Cube, "Abyss escape ladder / rail",
                    new Vector3(x + side * .23f, (bottom + top) * .5f, -1.58f),
                    new Vector3(.08f, top - bottom, .12f), trim, "metal", null, false);
            for (float y = bottom + .22f; y < top; y += .48f)
                Shape(PrimitiveType.Cube, "Abyss escape ladder / rung / hold R",
                    new Vector3(x, y, -1.65f), new Vector3(.52f, .075f, .13f),
                    new Color(.72f, .97f, 1f), "metal", null, false);
            var trigger = new GameObject("FUNCTIONAL abyss rescue ladder / hold R");
            trigger.transform.SetParent(root, false);
            trigger.transform.localPosition = new Vector3(x, -.95f, 0);
            var zone = trigger.AddComponent<BoxCollider>();
            zone.size = new Vector3(1.05f, 4.4f, 2.6f);
            zone.isTrigger = true;
            trigger.AddComponent<ClimbZone>().Rescue = true;
        }
        private static void BuildServiceColumn(float x, float highRouteHeight)
        {
            // The old glowing cylinder was a trigger and looked like a fake
            // pillar. A real solid column now supports jumps onto the ledge.
            float routeTop = highRouteHeight - .32f;
            float top = Mathf.Max(3.2f, routeTop - 3.35f);
            Shape(PrimitiveType.Cube, "PLAYABLE solid service column",
                new Vector3(x, top * .5f, 0), new Vector3(1.1f, top, 2.55f),
                stone * .64f, "metal");
            Shape(PrimitiveType.Cube, "PLAYABLE service column cap / real landing",
                new Vector3(x, top - .06f, 0), new Vector3(1.26f, .12f, 2.65f),
                trim, "metal");
        }

        private static void SideClimb(float x, float halfWidth, float top, int side)
        {
            float ladderX = x + side * (halfWidth + .48f);
            // Both the ladder and the top it reaches are functional. Keeping
            // the trigger outside the solid column lets the player climb again
            // after a missed jump without clipping into the wall.
            foreach (float rail in new[] { ladderX - .26f, ladderX + .26f })
                Shape(PrimitiveType.Cube, "Recovery ladder / continuous side rail",
                    new Vector3(rail, top * .5f, -1.55f), new Vector3(.07f, top, .09f),
                    stone * .86f, "metal", null, false);
            for (float y = .5f; y < top - .15f; y += .62f)
                Shape(PrimitiveType.Cube, "Recovery ladder / closely spaced rung / R to climb",
                    new Vector3(ladderX, y, -1.62f), new Vector3(.55f, .06f, .1f),
                    trim * .72f, "metal", null, false);
            var trigger = new GameObject("FUNCTIONAL recovery ladder / R climb, DOWN descend");
            trigger.transform.SetParent(root, false);
            trigger.transform.localPosition = new Vector3(ladderX, (top + 1.15f) * .5f, 0);
            var sensor = trigger.AddComponent<BoxCollider>();
            sensor.size = new Vector3(.8f, top + 1.15f, 2.4f);
            sensor.isTrigger = true;
            trigger.AddComponent<ClimbZone>();
        }
        private static void BuildColumnCrossing(int sector)
        {
            // Each district has a different way across the same fixed story gate.
            if (sector == 1)
            {
                ParkourColumn(44f, 3.1f);
                ParkourColumn(48.1f, 5.65f);
                ParkourColumn(52.2f, 8.35f);
            }
            else if (sector == 2)
            {
                MovingStep(44f, 3.15f, 2.7f, .65f);
                MovingStep(48.2f, 5.35f, 2.6f, .55f);
                MovingStep(52.2f, 7.55f, 2.65f, .55f);
                // The former fourth lift overlapped the third at the peak of
                // their cycles and squeezed the runner below the gate.
            }
            else
            {
                MovingStep(45f, 5.3f, 3.7f, 2f);
                Shape(PrimitiveType.Cube, "PLAYABLE core high bridge",
                    new Vector3(51.3f, 9.2f, 0), new Vector3(4.6f, .45f, 3f),
                    stone * .72f, "masonry");
                Shape(PrimitiveType.Cube, "Core bridge / bright lip",
                    new Vector3(51.3f, 9.48f, -1.58f), new Vector3(4.7f, .1f, .12f),
                    trim, null, null, false);
                SideClimb(51.3f, 2.3f, 9.45f, -1);
            }
            Shape(PrimitiveType.Cube, "SOLID parkour gate / cross from the columns",
                new Vector3(56.8f, 5.5f, 0), new Vector3(1.35f, 11f, 3.4f),
                stone * .69f, "metal");
            Shape(PrimitiveType.Cube, "Gate top / real landing surface",
                new Vector3(56.8f, 10.96f, 0), new Vector3(1.43f, .08f, 3.45f),
                new Color(.96f, .67f, .22f), "metal");
            var guide = Shape(PrimitiveType.Cube, "Approach to real climbable columns",
                new Vector3(42.1f, 3.5f, 0), new Vector3(1.25f, 7f, 2.6f),
                Color.clear, null, null, false);
            guide.AddComponent<BoxCollider>().isTrigger = true;
            guide.AddComponent<ColumnCoursePrompt>();
            guide.GetComponent<Renderer>().enabled = false;
        }

        private static void MovingStep(float x, float y, float width, float travel)
        {
            var lift = new GameObject("PLAYABLE clockwork elevator");
            lift.transform.SetParent(root, false);
            lift.transform.localPosition = new Vector3(x, y, 0);
            Shape(PrimitiveType.Cube, "Moving landing / solid",
                Vector3.zero, new Vector3(width, .4f, 3f), stone * .7f,
                "metal", lift.transform);
            Shape(PrimitiveType.Cube, "Moving landing / luminous edge",
                new Vector3(0, .24f, -1.56f), new Vector3(width + .08f, .09f, .12f),
                trim, null, lift.transform, false);
            lift.AddComponent<MovingPlatform>().Travel = travel;
        }

        // A distinct late district follows the former exit. Both the lower
        // islands and the upper crossings are real collision surfaces.
        private static void ExtendedRoute(int sector)
        {
            float[] upper = sector == 1 ? new[] { 205f, 214f, 224f, 235f, 244f } :
                sector == 2 ? new[] { 206f, 216f, 226f, 236f, 245f } :
                new[] { 204f, 213f, 223f, 234f, 244f };
            for (int i = 0; i < upper.Length; i++)
            {
                float x = upper[i];
                float y = sector == 1 ? (i % 2 == 0 ? 3.55f : 4.35f) :
                    sector == 2 ? (i % 2 == 0 ? 4.2f : 5.15f) :
                    (i % 2 == 0 ? 5.0f : 5.75f);
                if (sector == 2 && (i == 1 || i == 3) || sector == 3 && i == 2)
                {
                    MovingStep(x, y, 4.8f, .55f);
                    continue;
                }
                Shape(PrimitiveType.Cube, "PLAYABLE late elevated route",
                    new Vector3(x, y, 0), new Vector3(5f, .38f, 3f),
                    stone * .7f, "metal");
                Shape(PrimitiveType.Cube, "Late elevated route / bright edge",
                    new Vector3(x, y + .22f, -1.56f),
                    new Vector3(5.08f, .1f, .12f), trim, null, null, false);
            }
            // The ground gap is a second choice rather than an impassable
            // obstacle; the bridge above offers a landing and a safer wait.
            if (sector == 3)
                Shape(PrimitiveType.Cube, "PLAYABLE core rift rest",
                    new Vector3(233f, 2.6f, 0), new Vector3(3.6f, .36f, 3f),
                    stone * .72f, "masonry");
        }

        private static void ParkourColumn(float x, float top, bool rightRecovery = true)
        {
            const float width = 1.9f;
            Shape(PrimitiveType.Cube, "PLAYABLE solid column / jump onto marked top",
                new Vector3(x, top * .5f, 0), new Vector3(width, top, 2.55f),
                stone * .58f, "masonry");
            // Its luminous cap is also solid, with the upper face exactly at top.
            Shape(PrimitiveType.Cube, "PLAYABLE illuminated column landing",
                new Vector3(x, top - .055f, 0), new Vector3(width + .16f, .11f, 2.65f),
                trim, "metal");
            Shape(PrimitiveType.Cube, "Column front face / clearly solid",
                new Vector3(x, top * .5f, -1.32f), new Vector3(width - .12f, top - .12f, .1f),
                stone * .37f, "masonry", null, false);
            foreach (float edge in new[] { x - .76f, x + .76f })
                Shape(PrimitiveType.Cube, "Vertical column edge / NOT a step",
                    new Vector3(edge, top * .5f, -1.4f), new Vector3(.07f, top - .15f, .08f),
                    trim * .65f, null, null, false);
            // Only the recovery routes in the narrow landing pockets need
            // ladders. The first column is reached by an ordinary jump.
            if (x > 45f) SideClimb(x, width * .5f, top, -1);
            if (x > 50f && rightRecovery) SideClimb(x, width * .5f, top, 1);
        }

        private static void Player(RunGame game)
        {
            var player = new GameObject("NOVA / the courier"); player.transform.SetParent(root, false);
            player.transform.localPosition = new Vector3(1, 1.25f, 0);
            player.AddComponent<CapsuleCollider>(); player.AddComponent<Rigidbody>();
            var visual = new GameObject("Courier animation rig"); visual.transform.SetParent(player.transform, false);
            Transform rig = visual.transform;
            var core = new GameObject("Core").transform; core.SetParent(rig, false);
            Shape(PrimitiveType.Capsule, "Armored torso", new Vector3(0, .88f, 0),
                new Vector3(.75f, .77f, .57f), new Color(.21f, .23f, .36f), "armor", core, false);
            Shape(PrimitiveType.Sphere, "Helmet", new Vector3(0, 1.67f, 0),
                new Vector3(.7f, .67f, .72f), new Color(.9f, .85f, .72f), null, core, false);
            Shape(PrimitiveType.Cube, "Visor", new Vector3(0, 1.7f, -.37f),
                new Vector3(.6f, .23f, .1f), new Color(.1f, .79f, .93f), null, core, false);
            Shape(PrimitiveType.Cube, "Signal backpack", new Vector3(0, 1.05f, .42f),
                new Vector3(.72f, .78f, .26f), new Color(.94f, .31f, .14f), "metal", core, false);
            for (int side = -1; side < 2; side += 2)
            {
                Shape(PrimitiveType.Capsule, "Arm", new Vector3(side * .52f, 1.06f, 0),
                    new Vector3(.21f, .62f, .25f), new Color(.24f, .27f, .37f), null, core, false);
                Shape(PrimitiveType.Capsule, "Boot", new Vector3(side * .22f, .32f, 0),
                    new Vector3(.29f, .53f, .35f), new Color(.13f, .16f, .21f), null, core, false);
            }
            var rifle = Shape(PrimitiveType.Cube, "Signal blaster", new Vector3(.3f, 1.08f, -.55f),
                new Vector3(.82f, .19f, .18f), new Color(.9f, .33f, .16f), "metal", core, false);
            Shape(PrimitiveType.Cylinder, "Blaster muzzle", new Vector3(.5f, 1.07f, -.6f),
                new Vector3(.13f, .41f, .13f), new Color(.14f, .19f, .25f), null, core, false)
                .transform.localRotation = Quaternion.Euler(0, 0, 90);
            var portrait = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Textures/CourierSprite.png");
            if (portrait != null)
            {
                foreach (var primitive in visual.GetComponentsInChildren<Renderer>())
                    primitive.enabled = false;
                var spriteObject = new GameObject("Illustrated courier and rifle");
                spriteObject.transform.SetParent(core, false);
                spriteObject.transform.localPosition = new Vector3(0, 0, -.85f);
                var display = spriteObject.AddComponent<SpriteRenderer>();
                display.sprite = Sprite.Create(portrait,
                    new Rect(0, 0, portrait.width, portrait.height), new Vector2(.5f, .06f), 590);
                display.sortingOrder = 8;
                var walkRig = visual.AddComponent<CourierWalkRig>();
                walkRig.Initialize(core, display, portrait);
            }
            var animator = visual.AddComponent<Animator>();
            animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animation/Courier.controller");
            var mover = player.AddComponent<Runner>(); mover.AssignCrouchTexture(AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Art/Textures/CourierCrouch.png")); mover.Initialize(game, rig, rifle.transform);
        }
        private static void Details(int sector, RunGame game)
        {
            // Avoid bright decorative horizontal caps in the play lane: only
            // real collision surfaces advertise themselves as steps.
            LowPassagesAndCoins(sector);
            BuildSaws(sector);
            LateDistrictHazards(sector, game);
            // Pick two reachable sites, one before and one after the midpoint.
            var first = SignalSite(sector, 0, firstSignalSlot);
            var second = SignalSite(sector, 1, secondSignalSlot);
            FragmentAt(game, first.x, first.y, false, 0);
            FragmentAt(game, second.x, second.y, false, 1);
            if (sector != 3) FragmentAt(game, 98, 2, true);
            foreach (float stationX in new[] { 96f, 161f, 194f, 246f })
            {
                var station = Shape(PrimitiveType.Cylinder, "CYAN SIGNAL / DELIVERY TERMINAL",
                    new Vector3(stationX, 1.4f, -.95f), new Vector3(1.3f, 1.5f, 1.3f),
                    new Color(.13f, .85f, 1f), "circuit");
                station.GetComponent<Collider>().isTrigger = true;
                var delivery = station.AddComponent<SignalStation>();
                var beacon = Shape(PrimitiveType.Sphere, "Delivery beacon", new Vector3(0, 1.25f, -.2f),
                    new Vector3(.48f, .48f, .48f), Color.cyan, null, station.transform, false);
                delivery.Beacon = beacon.transform;
                beacon.GetComponent<Renderer>().material.EnableKeyword("_EMISSION");
                beacon.GetComponent<Renderer>().material.SetColor("_EmissionColor", Color.cyan * 1.5f);
            }
            // The second checkpoint stands BEFORE the solid x163 service
            // column; neither its trigger nor its respawn overlaps that column.
            foreach (float checkpointX in new[] { 96f, 161f, 203f })
            {
                var checkpoint = Shape(PrimitiveType.Cylinder, "Chrono checkpoint / " + checkpointX,
                    new Vector3(checkpointX, 1f, -.35f),
                    new Vector3(1.35f, 1.2f, 1.35f), new Color(.2f, .92f, .76f), "metal");
                checkpoint.GetComponent<Collider>().isTrigger = true;
                checkpoint.AddComponent<Checkpoint>().Position = checkpointX;
            }
            float[] spikes = sector == 1 ? new[] { 30f, 122f, 185f } :
                sector == 2 ? new[] { 32f, 122f, 185f } :
                new[] { 33f, 92f, 166f, 187f };
            foreach (float x in spikes)
            {
                var trap = Shape(PrimitiveType.Cube, "Hazard spikes", new Vector3(x, .27f, -.7f),
                    new Vector3(2.1f, .52f, 2), new Color(1f, .31f, .12f), "hazard");
                trap.GetComponent<Collider>().isTrigger = true;
                trap.AddComponent<Hazard>().Pulsing = sector == 2;
            }
            // x=50 blocked retreat between the climbing columns after a fall.
            // The remaining walls are away from mandatory recovery routes.
            foreach (float x in sector == 1 ? new[] { 128f, 187f } : new[] { 187f })
            {
                var wall = Shape(PrimitiveType.Cube, "Fracture barrier", new Vector3(x, 1.65f, 0),
                    new Vector3(.8f, 3.2f, 3.3f), new Color(.72f, .42f, .30f), "hazard");
                wall.AddComponent<Breakable>();
            }
            foreach (float x in new[] { 67f, 141f })
            {
                var ambush = Shape(PrimitiveType.Cube, "One-time district ambush / " + x,
                    new Vector3(x, 2.2f, 0), new Vector3(.4f, 4.4f, 3), Color.clear, null, null, false);
                ambush.AddComponent<BoxCollider>().isTrigger = true;
                ambush.AddComponent<AmbushZone>().Act = sector;
                ambush.GetComponent<Renderer>().enabled = false;
            }
            // Stable encounter pockets, varied on each new run. Mine offsets and
            // which lane closes first change, while both ground islands stay traversable.
            float[] standalone = sector == 1 ? new[] { 14f, 139f } : sector == 2 ?
                new[] { 91f, 138f, 170f } : new[] { 73f, 105f, 150f, 170f, 191f };
            foreach (float x in standalone) PlaceMine(x);
            bool earlyLow = layoutRandom.Next(2) == 0;
            if (sector == 1)
            {
                BuildRoutePocket(27.2f, earlyLow, 0, new[] { 23.2f, 27.8f });
                BuildRoutePocket(119f, !earlyLow, 1, new[] { 114.8f, 119f });
            }
            else if (sector == 2)
            {
                BuildRoutePocket(31.2f, earlyLow, 0, new[] { 24.8f, 27f, 30.2f });
                BuildRoutePocket(121.7f, !earlyLow, 1, new[] { 118f, 123f });
            }
            else
            {
                BuildRoutePocket(33.5f, earlyLow, 0, new[] { 23.7f, 27f, 30.6f });
                BuildRoutePocket(120.5f, !earlyLow, 1, new[] { 117.2f, 121.5f });
            }
            if (sector == 2)
            {
                foreach (float x in new[] { 12f, 63f, 102f, 172f, 191f })
                {
                    var laser = Shape(PrimitiveType.Cube, "Timed clockwork beam", new Vector3(x, 1.35f, 0),
                        new Vector3(.18f, 2.7f, 2.6f), new Color(.98f, .16f, .14f), "hazard");
                    laser.GetComponent<Collider>().isTrigger = true;
                    laser.AddComponent<Hazard>().Pulsing = true;
                }
                foreach (float x in new[] { 25f, 147f }) MakeCrusher(x, .65f);
                MakeGear(30.5f, 6f, 1.1f, .55f);
                MakeGear(50f, 9.9f, 1f, .65f);
                MakeGear(105f, 6.1f, 1.7f, .45f);
                MakeGear(145f, 6.3f, 1.2f, .55f);
                MakeSteam(70f, .12f);
                MakeSteam(149f, .56f);
                PlaceTurret(game, new[] { 110f, 114f }, 6.3f);
                PlaceTurret(game, new[] { 144f, 149f }, 6.1f);
            }
            if (sector == 3)
            {
                foreach (float x in new[] { 68f, 139f, 185f }) MakeCrusher(x, 1.0f);
                MakeGear(15f, 6.3f, 1.2f, .3f);
                MakeGear(51f, 11.2f, 1f, .4f);
                MakeGear(105f, 7f, 1.6f, .5f);
                MakeGear(147f, 7.1f, 1.3f, .4f);
                MakeSteam(106.5f, .24f);
                MakeSteam(150f, .71f);
                PlaceTurret(game, new[] { 62f, 69f }, 7.2f);
                PlaceTurret(game, new[] { 106f, 113f }, 7f);
                PlaceTurret(game, new[] { 144f, 149f }, 6.8f);
                // All three relays are on the approach after the last story
                // checkpoint, before the mandatory final guardian arena.
                foreach (float x in new[] { 207f, 213f, 217f })
                {
                    var relay = Shape(PrimitiveType.Cylinder, "Guardian shield relay", new Vector3(x, 3.1f, 0),
                        new Vector3(.57f, 1.4f, .57f), new Color(.8f, .31f, 1f), "circuit");
                    relay.AddComponent<PowerRelay>();
                    Glow(new Vector3(x, 3.2f, -1), glow, 4f, 1.25f);
                }
                Shape(PrimitiveType.Cube, "Suspended middle relay / support from upper platform",
                    new Vector3(213f, 5.05f, 0), new Vector3(.18f, 1.2f, .18f),
                    trim, "metal", null, false);
                var barrier = new GameObject("LOCKED guardian arena exit / disappears after boss defeat");
                barrier.transform.SetParent(root, false);
                Shape(PrimitiveType.Cube, "SOLID boss barrier / blocks high and low routes",
                    new Vector3(228.25f, 5.25f, 0), new Vector3(.8f, 19.5f, 4.1f),
                    new Color(.32f, .14f, .52f), "circuit", barrier.transform);
                Shape(PrimitiveType.Cube, "Boss barrier / luminous front edge",
                    new Vector3(228.25f, 5.25f, -2.09f), new Vector3(.85f, 19.5f, .12f),
                    new Color(.91f, .38f, 1f), null, barrier.transform, false);
                game.RegisterGuardianBarrier(barrier);
                var warning = Shape(PrimitiveType.Cube, "Final guardian arena approach prompt",
                    new Vector3(205f, 1.25f, 0), new Vector3(.6f, 2.5f, 2.6f),
                    Color.clear, null, null, false);
                warning.AddComponent<BoxCollider>().isTrigger = true;
                warning.AddComponent<GuardianArenaPrompt>();
                warning.GetComponent<Renderer>().enabled = false;
                foreach (float x in new[] { 57f, 107f, 143f })
                {
                    var rock = Shape(PrimitiveType.Sphere, "Crumbling mountain boulder", new Vector3(x, 9f, 0),
                        new Vector3(1.5f, 1.5f, 1.5f), stone * .68f, "stone");
                    rock.GetComponent<Collider>().isTrigger = true;
                    rock.AddComponent<FallingDebris>();
                }
            }
            if (sector == 1)
            {
                MakeGear(32f, 5.4f, 1.15f, .4f);
                MakeGear(103f, 5.7f, 1.2f, .7f);
                MakeGear(146f, 6f, 1f, .5f);
                MakeSteam(115f, .18f);
                MakeSteam(168f, .53f);
                // Patrols cover the low minefield, the first signal route and the late approach.
                SpawnVariant(game, new[] { 10.5f, 13f }, 2f);
                SpawnVariant(game, new[] { 27.8f, 31.2f }, 2f);
                SpawnVariant(game, new[] { 29f, 31f }, 3.5f, true);
                SpawnVariant(game, new[] { 65f, 69f }, 2f);
                SpawnVariant(game, new[] { 89f, 104.5f }, 2f, false, true);
                SpawnVariant(game, new[] { 114f, 118f }, 2f);
                SpawnVariant(game, new[] { 137f, 147f }, 2f);
                SpawnVariant(game, new[] { 169f, 187f }, 2f);
            }
            else if (sector == 2)
            {
                // The clockwork works has moving patrols, airborne scouts,
                // a climber at the column course, and ceiling sentries.
                SpawnVariant(game, new[] { 8f, 12f }, 5f, true, true);
                SpawnVariant(game, new[] { 27.8f, 31.2f }, 2f);
                SpawnVariant(game, new[] { 32f, 33f }, 4.4f, true);
                SpawnVariant(game, new[] { 46f, 46.3f }, 2f, false, true);
                SpawnVariant(game, new[] { 57f, 70f }, 3.3f, true);
                SpawnVariant(game, new[] { 89f, 104f }, 2f, false, true);
                SpawnVariant(game, new[] { 118f, 124f }, 2f);
                SpawnVariant(game, new[] { 139f, 148f }, 2f);
                SpawnVariant(game, new[] { 142f, 146f }, 3.6f, true);
                SpawnVariant(game, new[] { 154f, 174f }, 3.4f, true);
                SpawnVariant(game, new[] { 169f, 187f }, 2f);
            }
            else
            {
                // The core adds overlapping ground/air patrols and sentry
                // turrets, culminating in the existing three-relay guardian.
                SpawnVariant(game, new[] { 8f, 12f }, 6.2f, true, true);
                SpawnVariant(game, new[] { 24f, 28f }, 2f);
                SpawnVariant(game, new[] { 31f, 34f }, 5.2f, true);
                SpawnVariant(game, new[] { 46f, 46.3f }, 2f, false, true);
                SpawnVariant(game, new[] { 54f, 70f }, 3.4f, true);
                SpawnVariant(game, new[] { 90f, 104f }, 2f, false, true);
                SpawnVariant(game, new[] { 117f, 121f }, 2f);
                SpawnVariant(game, new[] { 118f, 124f }, 2f, false, true);
                SpawnVariant(game, new[] { 140f, 148f }, 3.4f, true);
                SpawnVariant(game, new[] { 169f, 188f }, 2f);
                SentinelAt(game, 222f, 3.7f, true);
            }
        }

        private static void LowPassagesAndCoins(int sector)
        {
            // Passages change position and count between acts. Each is on solid
            // ground and has real overhead collision and a marked lower route.
            float[] vents = sector == 1 ? new[] { 6.5f, 25f, 119f } :
                sector == 2 ? new[] { 8.5f, 68f, 142f } :
                new[] { 9f, 31f, 118f, 145f };
            foreach (float x in vents)
            {
                Shape(PrimitiveType.Cube, "SOLID low ventilation beam / crouch with S",
                    new Vector3(x, 1.97f, 0), new Vector3(3.7f, .65f, 2.9f),
                    new Color(.29f, .41f, .49f), "metal");
                Shape(PrimitiveType.Cube, "Vent hazard stripe / overhead boundary",
                    new Vector3(x, 1.61f, -1.52f), new Vector3(3.85f, .11f, .12f),
                    new Color(1f, .69f, .12f), null, null, false);
                foreach (float edge in new[] { x - 1.87f, x + 1.87f })
                    Shape(PrimitiveType.Cube, "Amber passage marker", new Vector3(edge, 1.87f, -1.53f),
                        new Vector3(.12f, .55f, .1f), new Color(1f, .78f, .18f), null, null, false);
                var reminder = Shape(PrimitiveType.Cube, "Approach / hold S to crawl",
                    new Vector3(x - 3.05f, 1f, 0), new Vector3(.48f, 2f, 2.2f),
                    Color.clear, null, null, false);
                reminder.AddComponent<BoxCollider>().isTrigger = true;
                reminder.AddComponent<CrouchPrompt>();
                reminder.GetComponent<Renderer>().enabled = false;
                foreach (float offset in new[] { -.72f, .72f })
                    MakeCoin(x + offset, .62f);
            }
            float[] coins = sector == 1 ?
                new[] { 12.5f, 28f, 44f, 53f, 69f, 79f, 94f, 103f, 139f, 146f, 160f, 170f, 190f } :
                sector == 2 ?
                new[] { 12f, 29f, 46f, 55f, 63f, 85f, 100f, 104f, 120f, 148f, 160f, 173f, 190f } :
                new[] { 13f, 28f, 45f, 53f, 66f, 85f, 99f, 117f, 139f, 143f, 161f, 176f, 191f };
            foreach (float x in coins)
                MakeCoin(x, 1.18f);
        }

        private static ProximityMine PlaceMine(float x)
        {
            var mine = Shape(PrimitiveType.Sphere, "Armed proximity mine",
                new Vector3(x, .55f, 0), new Vector3(.86f, .62f, .86f),
                new Color(.93f, .21f, .12f), "hazard");
            mine.GetComponent<Collider>().isTrigger = true;
            return mine.AddComponent<ProximityMine>();
        }

        private static void BuildRoutePocket(float center, bool lower, int pocket,
            float[] mineSockets)
        {
            // One short blocker per ground island, with a wide, open run-up.
            // Opposite lanes are chosen across the two distant pockets in each act.
            float x = center + Roll(layoutRandom, -.3f, .3f);
            var container = new GameObject("Timed grid pocket / " + pocket); container.transform.SetParent(root, false);
            container.transform.localPosition = new Vector3(x, 0, 0);
            var low = LocalGrid(x, true, pocket); var high = LocalGrid(x, false, pocket);
            low.transform.SetParent(container.transform, true); high.transform.SetParent(container.transform, true);
            low.gameObject.SetActive(lower); high.gameObject.SetActive(!lower);
            var variation = container.AddComponent<GridPocket>(); variation.Lower = low; variation.Upper = high;
            variation.Row = pocket; variation.Center = center;
            foreach (float socket in mineSockets)
            {
                var mine = PlaceMine(socket + Roll(layoutRandom, -.25f, .25f));
                var offset = mine.gameObject.AddComponent<RouteVariation>(); offset.Anchor = new Vector3(socket, .55f, 0);
                offset.HorizontalRange = new Vector2(-.25f, .25f);
            }
        }

        private static MinefieldGrid LocalGrid(float x, bool lower, int row)
        {
            float bottom = lower ? .12f : 2.4f;
            float top = lower ? 2.62f : 6.15f;
            float height = top - bottom;
            float width = lower ? 2.35f : 2.65f;
            float y = (bottom + top) * .5f;
            var field = new GameObject(lower ? "TIMED GRID / LOW ROUTE" : "TIMED GRID / UPPER ROUTE");
            field.transform.SetParent(root, false);
            field.transform.localPosition = new Vector3(x, y, 0);
            var trigger = field.AddComponent<BoxCollider>();
            trigger.size = new Vector3(width, height, 2.9f);
            trigger.isTrigger = true;
            var barrier = field.AddComponent<BoxCollider>();
            barrier.size = trigger.size;
            var bars = new List<Renderer>();
            Color electric = lower ? new Color(1f, .53f, .22f) : new Color(.12f, .85f, 1f);
            foreach (float lineY in new[] { bottom, y, top })
                bars.Add(Shape(PrimitiveType.Cube, "Timed grid / horizontal warning",
                    new Vector3(x, lineY, -1.62f), new Vector3(width, .1f, .12f),
                    electric, null, null, false).GetComponent<Renderer>());
            foreach (float lineX in new[] { x - width * .5f, x, x + width * .5f })
                bars.Add(Shape(PrimitiveType.Cube, "Timed grid / vertical warning",
                    new Vector3(lineX, y, -1.63f), new Vector3(.1f, height, .12f),
                    electric, null, null, false).GetComponent<Renderer>());
            var lamp = new GameObject("Local grid warning lamp");
            lamp.transform.SetParent(root, false);
            lamp.transform.localPosition = new Vector3(x, y, -1.2f);
            var light = lamp.AddComponent<Light>();
            light.type = LightType.Point; light.color = electric; light.range = 4.2f;
            foreach (var bar in bars) bar.transform.SetParent(field.transform, true);
            lamp.transform.SetParent(field.transform, true);
            var grid = field.AddComponent<MinefieldGrid>();
            grid.AssignFadeMaterial(UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/StageGridFade.mat"));
            grid.Initialize(x - width * .5f,
                trigger, barrier, bars.ToArray(), light, lower, row);
            return grid;
        }

        private static void BuildSaws(int sector)
        {
            // Mandatory normal jumps AND landings clear the upper wheels.
            // The x=101 wheel always clears a crouch. The original early saw
            // was removed because it intercepted the required jump over a gap.
            if (sector == 1)
            {
                MakeSaw(101f, 2.75f, .26f);
            }
            else if (sector == 2)
            {
                MakeSaw(101f, 2.75f, .26f);
                MakeSaw(111f, 6.2f, 0f, .65f);
                MakeSaw(155.9f, 9f, .25f);
            }
            else
            {
                MakeSaw(63f, 6.5f, .25f);
                MakeSaw(101f, 2.75f, .26f);
                MakeSaw(139.8f, 10.05f, .32f);
                MakeSaw(169f, 12.1f, .25f);
                MakeSaw(147f, 6.1f, 0f, 1.1f);
            }
        }

        private static void MakeSaw(float x, float y, float travel, float horizontalTravel = 0f)
        {
            var saw = new GameObject("ROTATING SAW / time jump or choose another route");
            saw.transform.SetParent(root, false);
            saw.transform.localPosition = new Vector3(x, y, 0);
            saw.AddComponent<Rigidbody>().isKinematic = true;
            var sensor = saw.AddComponent<SphereCollider>();
            sensor.radius = .86f; sensor.isTrigger = true;
            var pivot = new GameObject("Spinning teeth").transform;
            pivot.SetParent(saw.transform, false);
            var disc = Shape(PrimitiveType.Cylinder, "Steel cutting wheel",
                new Vector3(0, 0, -.95f), new Vector3(.72f, .075f, .72f),
                new Color(.69f, .79f, .83f), "metal", pivot, false);
            disc.transform.localRotation = Quaternion.Euler(90, 0, 0);
            for (int i = 0; i < 12; i++)
            {
                float theta = i * Mathf.PI / 6f;
                var tooth = Shape(PrimitiveType.Cube, "Visible cutting tooth",
                    new Vector3(Mathf.Cos(theta) * .79f, Mathf.Sin(theta) * .79f, -.97f),
                    new Vector3(.24f, .23f, .12f), new Color(1f, .43f, .19f), null, pivot, false);
                tooth.transform.localRotation = Quaternion.Euler(0, 0, i * 30f);
            }
            saw.AddComponent<SawBlade>().Initialize(pivot, travel, horizontalTravel);
        }

        private static void MakeGear(float x, float y, float horizontal, float vertical)
        {
            var gear = new GameObject("MOVING GEAR / upper route hazard");
            gear.transform.SetParent(root, false);
            gear.transform.localPosition = new Vector3(x, y, 0);
            var body = gear.AddComponent<Rigidbody>();
            body.isKinematic = true; body.useGravity = false;
            var sensor = gear.AddComponent<SphereCollider>();
            sensor.radius = 1.05f; sensor.isTrigger = true;
            var wheel = new GameObject("Rotating gear wheel").transform;
            wheel.SetParent(gear.transform, false);
            Shape(PrimitiveType.Cylinder, "Steel gear disc", new Vector3(0, 0, -.86f),
                new Vector3(1.8f, .18f, 1.8f), stone * .75f, "metal", wheel, false)
                .transform.localRotation = Quaternion.Euler(90, 0, 0);
            Shape(PrimitiveType.Sphere, "Gear warning hub", new Vector3(0, 0, -1.05f),
                new Vector3(.46f, .46f, .2f), new Color(1f, .38f, .1f), null, wheel, false);
            for (int i = 0; i < 12; i++)
            {
                float angle = i * Mathf.PI / 6f;
                var tooth = Shape(PrimitiveType.Cube, "Visible gear tooth",
                    new Vector3(Mathf.Cos(angle) * .93f, Mathf.Sin(angle) * .93f, -.88f),
                    new Vector3(.36f, .32f, .23f), new Color(.92f, .55f, .26f),
                    "metal", wheel, false);
                tooth.transform.localRotation = Quaternion.Euler(0, 0, i * 30f);
            }
            var hazard = gear.AddComponent<GearWheel>();
            hazard.Horizontal = horizontal; hazard.Vertical = vertical;
            hazard.Configure(wheel);
        }

        private static void MakeSteam(float x, float phase)
        {
            Shape(PrimitiveType.Cylinder, "SOLID steam vent nozzle",
                new Vector3(x, .17f, 0), new Vector3(1.05f, .16f, 1.05f),
                stone * .74f, "metal");
            var jet = Shape(PrimitiveType.Cube, "TIMED steam plume / wait for clear",
                new Vector3(x, 1.72f, 0), new Vector3(.7f, 3.25f, 2.6f),
                new Color(1f, .65f, .2f), null);
            jet.GetComponent<Collider>().isTrigger = true;
            jet.AddComponent<SteamVent>().Phase = phase;
        }

        private static void LateDistrictHazards(int sector, RunGame game)
        {
            // A lower timed route and an exposed upper route through different
            // machinery. Neither lane has an uninterrupted safe sprint.
            if (sector == 1)
            {
                MakeSteam(221f, .41f);
                PlaceMine(227f);
                MakeSteam(248f, .11f);
                MakeGear(214f, 6.15f, .7f, .35f);
                MakeGear(235f, 6.5f, 1.1f, .4f);
                SpawnVariant(game, new[] { 221f, 226f }, 2f);
                SpawnVariant(game, new[] { 240f, 246f }, 4.4f, true);
            }
            else if (sector == 2)
            {
                MakeCrusher(224f, .29f);
                MakeSteam(243f, .73f);
                PlaceMine(228f);
                MakeGear(215f, 7.0f, 1f, .4f);
                MakeGear(235f, 7.1f, 1.2f, .5f);
                SpawnVariant(game, new[] { 222f, 229f }, 2f);
                SpawnVariant(game, new[] { 242f, 247f }, 5f, true, true);
            }
            else
            {
                MakeSteam(223f, .37f);
                MakeCrusher(226f, .47f);
                PlaceMine(247f);
                MakeGear(212f, 7.4f, .95f, .45f);
                MakeGear(234f, 8f, 1f, .5f);
                SpawnVariant(game, new[] { 220f, 226f }, 4.8f, true);
                SpawnVariant(game, new[] { 240f, 246f }, 2f, false, true);
            }
            MakeCoin(sector == 2 ? 228f : 225f, 5.7f);
            MakeCoin(245f, 1.2f);
        }

        private static void MakeCrusher(float x, float phase)
        {
            Shape(PrimitiveType.Cube, "Press warning / red floor marking",
                new Vector3(x, .15f, -2.12f), new Vector3(1.9f, .05f, .13f),
                new Color(.96f, .19f, .08f), null, null, false);
            var press = Shape(PrimitiveType.Cube, "TIMED hydraulic press / watch its cycle",
                new Vector3(x, 4.35f, 0), new Vector3(1.65f, 1.1f, 2.8f),
                new Color(.46f, .59f, .63f), "metal");
            press.GetComponent<Collider>().isTrigger = true;
            press.AddComponent<Crusher>().Offset = phase;
        }

        private static void PlaceTurret(RunGame game, float[] sites, float height)
        {
            int slot = layoutRandom.Next(sites.Length);
            turretPlans.Add(new TurretPlan { Sites = sites, Height = height, Slot = slot,
                Active = CreateTurret(game, sites[slot], height) });
        }

        private static SentryTurret CreateTurret(RunGame game, float x, float y)
        {
            var turret = new GameObject("CEILING SENTRY / destructible orange drone");
            turret.transform.SetParent(root, false);
            turret.transform.localPosition = new Vector3(x, y, 0);
            var detector = turret.AddComponent<SphereCollider>();
            detector.radius = .67f; detector.isTrigger = true;
            var shell = Shape(PrimitiveType.Cube, "Angular sentry armor",
                new Vector3(0, 0, -.7f), new Vector3(.95f, .95f, .55f),
                new Color(.8f, .42f, .19f), "metal", turret.transform, false);
            shell.transform.localRotation = Quaternion.Euler(0, 0, 45f);
            var optic = Shape(PrimitiveType.Sphere, "Sentry eye / warns before firing",
                new Vector3(0, 0, -1.05f), new Vector3(.32f, .32f, .19f),
                new Color(1f, .25f, .13f), null, turret.transform, false);
            Shape(PrimitiveType.Cube, "Fixed sentry barrels",
                new Vector3(0, -.46f, -.7f), new Vector3(.34f, .45f, .37f),
                new Color(.19f, .22f, .27f), "metal", turret.transform, false);
            var brain = turret.AddComponent<SentryTurret>();
            brain.Configure(game, optic.GetComponent<Renderer>());
            return brain;
        }

        private static void MakeCoin(float x, float y)
        {
            var pickup = new GameObject("GOLD coin / choose a one-time Shift power");
            pickup.transform.SetParent(root, false);
            pickup.transform.localPosition = new Vector3(x, y, 0);
            var sensor = pickup.AddComponent<SphereCollider>();
            sensor.isTrigger = true; sensor.radius = .45f;
            pickup.AddComponent<PowerCoin>();
            var rim = Shape(PrimitiveType.Cylinder, "Gold rim", new Vector3(0, 0, -.78f),
                new Vector3(.52f, .055f, .52f), new Color(1f, .7f, .12f), null,
                pickup.transform, false);
            rim.transform.localRotation = Quaternion.Euler(90, 0, 0);
            var face = Shape(PrimitiveType.Cylinder, "Bright coin face", new Vector3(0, 0, -.86f),
                new Vector3(.37f, .035f, .37f), new Color(1f, .91f, .34f), null,
                pickup.transform, false);
            face.transform.localRotation = Quaternion.Euler(90, 0, 0);
        }
        private static void FragmentAt(RunGame game, float x, float y, bool healing = false,
            int signalIndex = -1)
        {
            var shard = Shape(PrimitiveType.Sphere, healing ? "Green health pickup" : "CYAN SIGNAL / REQUIRED FOR GATE",
                new Vector3(x, y, 0), new Vector3(.95f, 1.18f, .95f),
                healing ? new Color(.32f, 1f, .55f) : new Color(.10f, .94f, 1f));
            shard.GetComponent<Collider>().isTrigger = true;
            var pickup = shard.AddComponent<Fragment>();
            pickup.Healing = healing; pickup.SignalIndex = signalIndex;
            if (!healing)
            {
                var halo = Shape(PrimitiveType.Sphere, "Bright cyan signal halo / collectible",
                    new Vector3(0, 0, -.62f), new Vector3(1.42f, 1.53f, .12f),
                    new Color(.13f, .78f, .91f), "circuit", shard.transform, false);
                halo.GetComponent<Renderer>().enabled = true;
                var lamp = shard.AddComponent<Light>(); lamp.type = LightType.Point;
                lamp.color = Color.cyan; lamp.range = 4.3f; lamp.intensity = 1.45f;
            }
        }
        private static void SentinelAt(RunGame game, float x, float y, bool boss)
        { SpawnSentinel(game, x, y, boss, false); }
        private static void SpawnVariant(RunGame game, float[] sites, float height,
            bool flying = false, bool rapid = false)
        {
            float site = sites[layoutRandom.Next(sites.Length)];
            Sentinel guard = SpawnSentinel(game, site, height, false, flying, rapid);
            if (recordingGuards)
            {
                var plan = guardPlans.Find(p => p.Active == guard);
                if (plan != null) plan.Sites = sites;
            }
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
        public static Sentinel SpawnSentinel(RunGame game, float x, float y, bool boss, bool flying,
            bool rapid = false, float forcedX = -999f)
        {
            Vector2 bounds = GuardRange(game.Level, x, boss, flying);
            float actualX = forcedX < -500f ? Roll(layoutRandom, bounds.x, bounds.y) : forcedX;
            var enemy = new GameObject(boss ? "CHRONO GUARDIAN / boss" : rapid ?
                "Rapid-fire scout / AI" : "Rift sentinel / AI");
            enemy.transform.SetParent(root, false);
            enemy.transform.localPosition = new Vector3(actualX, y, 0);
            var body = enemy.AddComponent<SphereCollider>(); body.radius = boss ? 2.2f : .7f;
            var model = new GameObject("Animated sentinel"); model.transform.SetParent(enemy.transform, false);
            var core = new GameObject("Core").transform; core.SetParent(model.transform, false);
            Shape(PrimitiveType.Sphere, "Armored shell", Vector3.zero,
                Vector3.one * (boss ? 4.2f : 1.05f), stone * .55f, "armor", core, false);
            Shape(PrimitiveType.Sphere, "Crimson eye", new Vector3(0, boss ? .32f : .12f, boss ? -2.05f : -.7f),
                Vector3.one * (boss ? 1.3f : .4f), new Color(1f, .22f, .13f), null, core, false);
            for (int i = 0; i < 4; i++)
                Shape(PrimitiveType.Cube, "Guardian armor fin " + i, new Vector3(Mathf.Sin(i * Mathf.PI / 2) * (boss ? 1.75f : .7f),
                    Mathf.Cos(i * Mathf.PI / 2) * (boss ? 1.75f : .7f), 0),
                    new Vector3(boss ? .62f : .3f, boss ? .7f : .34f, boss ? 1.25f : .5f), stone, "metal", core, false);
            if (boss)
            {
                var aura = model.AddComponent<Light>(); aura.type = LightType.Point;
                aura.color = new Color(.84f, .35f, 1f); aura.range = 9f; aura.intensity = 2.1f;
            }
            var animator = model.AddComponent<Animator>();
            animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/Animation/Sentinel.controller");
            var brain = enemy.AddComponent<Sentinel>();
            brain.Configure(game, boss, rapid); brain.Flying = flying;
            brain.SetPatrolRange(bounds.x, bounds.y);
            if (recordingGuards)
                guardPlans.Add(new GuardPlan { Anchor = x, Height = y, MinX = bounds.x,
                    MaxX = bounds.y, Boss = boss, Flying = flying, Rapid = rapid, Active = brain });
            return brain;
        }
        public static void Capture(StageScene bindings)
        {
            bindings.LevelRoot = root;
            bindings.StageCamera = Camera.main;
            bindings.Player = Object.FindObjectOfType<Runner>();
            bindings.Projectiles = Object.FindObjectOfType<ProjectilePool>();
            bindings.Guards = Object.FindObjectOfType<GuardDirector>();
            bindings.Director = Object.FindObjectOfType<AdaptiveDirector>();
            bindings.Drone = Object.FindObjectOfType<DroneCompanion>();
            bindings.Fragments = Object.FindObjectsOfType<Fragment>();
            bindings.RouteVariations = Object.FindObjectsOfType<RouteVariation>();
            bindings.GridPockets = Object.FindObjectsOfType<GridPocket>();
            bindings.EffectMaterial = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/StageStandard.mat");
            bindings.GuardPlans = guardPlans.ConvertAll(p => new GuardSpawnPlan {
                Anchor = p.Anchor, Height = p.Height, MinX = p.MinX, MaxX = p.MaxX,
                Boss = p.Boss, Flying = p.Flying, Rapid = p.Rapid, Sites = p.Sites, Active = p.Active
            }).ToArray();
            bindings.TurretPlans = turretPlans.ConvertAll(p => new TurretSpawnPlan {
                Sites = p.Sites, Height = p.Height, Slot = p.Slot, Active = p.Active
            }).ToArray();
        }
    }
}
