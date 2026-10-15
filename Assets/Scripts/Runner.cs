using UnityEngine;

namespace Shatterline
{
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public sealed class Runner : MonoBehaviour
    {
        [SerializeField] private Rigidbody body;
        [SerializeField] private RunGame game;
        [SerializeField] private Animator animator;
        [SerializeField] private Transform rig, gun;
        [SerializeField] private SpriteRenderer characterSprite;
        [SerializeField] private CourierWalkRig walkRig;
        [SerializeField] private Sprite standingSprite, crouchSprite;
        private bool illustrated;
        [SerializeField] private Renderer shieldVisual;
        [SerializeField] private ParticleSystem muzzleSparks;
        [SerializeField] private Transform muzzleTransform;
        private float shieldUntil, shieldReady;
        public bool Shielded { get { return Time.time < shieldUntil; } }
        private int facing = 1, lastMoveDirection = 1, dashDirection = 1, jumpsLeft = 2;
        private float groundTime, jumpQueued, jumpBoostUntil, lastJumpAt, dashUntil, dashReady, fireReady;
        private float firstReleaseAt = -100f;
        private Vector3 lastSafeGround;
        private float blockedFor, underFloorFor;
        private bool firstJumpHeld, doubleTapReady;
        private const float MaxFirstTap = .18f, MaxTapGap = .24f, MaxDoubleSequence = .38f;
        private bool dashing, crouching, diving;
        public bool IsDashing { get { return dashing; } }
        public int Facing { get { return dashing ? dashDirection : facing; } }
        public Transform VisualRig { get { return rig; } }
        public bool IsCrouching { get { return crouching; } }
        private int nearbyClimbZones, nearbyRescueZones;
        public bool CatchingRescueLadder
        {
            get { return nearbyRescueZones > 0 && Input.GetKey(KeyCode.R) && !crouching && !diving; }
        }
        private const float MoveSpeed = 7.6f;
        private const float DroneMovementMultiplier = .75f;
        public int Energy { get; private set; } = 55;
        public void Recharge(int amount) { Energy = Mathf.Min(100, Energy + amount); }
        public bool TrySpendEnergy(int amount)
        {
            if (Energy < amount) return false;
            Energy -= amount; return true;
        }
        public void ActivateSpecialShield(float seconds)
        {
            shieldUntil = Mathf.Max(shieldUntil, Time.time + seconds);
            if (shieldVisual != null) shieldVisual.enabled = true;
        }

        [SerializeField] private Texture2D crouchTexture;
        public void AssignCrouchTexture(Texture2D texture) { crouchTexture = texture; }
        public void InitializeRuntime(RunGame run) { Initialize(run, rig, gun); }

        public void Initialize(RunGame run, Transform model, Transform weapon)
        {
            game = run; rig = model; gun = weapon;
            gameObject.layer = 2; // Ignore Raycast: ground probe and projectiles skip the player's own collider.
            body = GetComponent<Rigidbody>();
            lastSafeGround = transform.position;
            body.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.Continuous;
            GetComponent<CapsuleCollider>().center = Vector3.up * .88f;
            GetComponent<CapsuleCollider>().height = 1.76f;
            GetComponent<CapsuleCollider>().radius = .34f;
            animator = rig.GetComponent<Animator>();
            characterSprite = rig.GetComponentInChildren<SpriteRenderer>();
            walkRig = rig.GetComponent<CourierWalkRig>();
            illustrated = characterSprite != null;
            if (illustrated)
            {
                standingSprite = characterSprite.sprite;
#if UNITY_EDITOR
                var pose = crouchTexture;
                if (pose != null && crouchSprite == null)
                    crouchSprite = Sprite.Create(pose, new Rect(0, 0, pose.width, pose.height),
                        new Vector2(.5f, .05f), 630f);
#endif
                if (crouchSprite == null) throw new System.InvalidOperationException("Runner crouch sprite is not assigned in its prefab.");
            }
            // A thin shield outline stays readable without hiding Nova. It
            // uses the retained stage shader instead of a legacy transparent
            // shader that Unity may strip from Windows player builds.
            if (shieldVisual != null && muzzleSparks != null)
            {
                shieldVisual.enabled = false;
                muzzleSparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                return;
            }
#if UNITY_EDITOR
            var halo = new GameObject("Temporary shield outline");
            halo.transform.SetParent(transform, false);
            var outline = halo.AddComponent<LineRenderer>();
            outline.useWorldSpace = false;
            outline.loop = true;
            outline.positionCount = 32;
            outline.widthMultiplier = .065f;
            outline.sharedMaterial = StageBuilder.Material(new Color(.22f, .85f, 1f));
            for (int i = 0; i < outline.positionCount; i++)
            {
                float angle = i * Mathf.PI * 2f / outline.positionCount;
                outline.SetPosition(i, new Vector3(Mathf.Cos(angle) * .83f,
                    .9f + Mathf.Sin(angle) * 1.16f, -1.1f));
            }
            shieldVisual = outline;
            shieldVisual.enabled = false;
            var muzzle = new GameObject("Reusable blaster muzzle flash");
            muzzle.transform.SetParent(transform, false);
            muzzleTransform = muzzle.transform;
            muzzle.transform.localPosition = new Vector3(.85f, 1.08f, -.7f);
            muzzleSparks = muzzle.AddComponent<ParticleSystem>();
            var muzzleRenderer = muzzle.GetComponent<ParticleSystemRenderer>();
            if (muzzleRenderer != null) muzzleRenderer.sharedMaterial = StageBuilder.Material(new Color(.4f, .96f, 1f));
            var mainFx = muzzleSparks.main;
            mainFx.playOnAwake = false; mainFx.startLifetime = .13f;
            mainFx.startSpeed = 2f; mainFx.startSize = .09f;
            mainFx.startColor = new Color(.4f, .96f, 1f);
            mainFx.maxParticles = 48;
            var fxEmission = muzzleSparks.emission;
            fxEmission.rateOverTime = 0;
            muzzleSparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
#else
            throw new System.InvalidOperationException("Runner prefab has missing shield or muzzle references.");
#endif
        }

        private void Update()
        {
            if (game == null || !game.CanPlay) return;
            float move = Input.GetAxisRaw("Horizontal");
            if (Mathf.Abs(move) > .1f)
                facing = lastMoveDirection = move > 0 ? 1 : -1;
            bool pressedJump = Input.GetKeyDown(KeyCode.W);
            if (firstJumpHeld && Time.time - lastJumpAt > MaxFirstTap)
                doubleTapReady = false;
            // Releasing W during ascent cuts the arc. A held key gets a short
            // upward boost in FixedUpdate; a second press spends the air jump.
            if (Input.GetKeyUp(KeyCode.W))
            {
                if (firstJumpHeld)
                {
                    if (doubleTapReady && Time.time - lastJumpAt <= MaxFirstTap)
                        firstReleaseAt = Time.time;
                    else doubleTapReady = false;
                    firstJumpHeld = false;
                }
                jumpBoostUntil = 0f;
                if (body.velocity.y > 0f && !dashing)
                    body.velocity = new Vector3(body.velocity.x,
                        body.velocity.y * (jumpsLeft == 0 ? .62f : .43f), 0);
            }
            // A downward surface normal is required: touching the side of a
            // tall column or gate must not refill the double jump.
            RaycastHit groundHit;
            bool grounded = Physics.Raycast(transform.position + Vector3.up * .55f,
                Vector3.down, out groundHit, .7f, Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore) && groundHit.normal.y > .6f;
            if (diving && grounded)
                EndDive();
            if (Input.GetKeyDown(KeyCode.S) && !grounded)
            {
                diving = true;
                dashing = false;
                jumpBoostUntil = 0f;
                body.useGravity = true;
                body.velocity = new Vector3(body.velocity.x * .7f, -16f, 0);
                game.Flash("FAST DROP  /  S DRIVES YOU DOWN", 1f);
            }
            // Keep the capsule's feet in place and never stand up inside a low passage.
            bool wantsCrouch = Input.GetKey(KeyCode.S);
            crouching = wantsCrouch || diving || (crouching && !CanStand());
            var capsule = GetComponent<CapsuleCollider>();
            capsule.height = crouching ? 1.38f : 1.76f;
            capsule.center = Vector3.up * (capsule.height * .5f);
            if (crouching) dashing = false;
            // The ray still touches the floor for a frame after takeoff. Do not
            // refill both jumps while the first upward impulse is already active.
            if (grounded)
            {
                groundTime = Time.time;
                if (body.velocity.y <= .1f)
                { jumpsLeft = 2; doubleTapReady = firstJumpHeld = false; }
            }
            if (pressedJump)
            {
                bool validSecond = jumpsLeft == 1 && doubleTapReady && !firstJumpHeld &&
                    firstReleaseAt >= lastJumpAt &&
                    Time.time - firstReleaseAt <= MaxTapGap &&
                    Time.time - lastJumpAt <= MaxDoubleSequence;
                jumpQueued = jumpsLeft == 2 || validSecond ? Time.time + .13f : -1f;
            }
            if (!crouching && jumpQueued >= Time.time && jumpsLeft > 0 &&
                (jumpsLeft == 2 ? Time.time - groundTime < .13f :
                    doubleTapReady && !firstJumpHeld && firstReleaseAt >= lastJumpAt &&
                    Time.time - firstReleaseAt <= MaxTapGap &&
                    Time.time - lastJumpAt <= MaxDoubleSequence))
            {
                bool second = jumpsLeft == 1;
                jumpsLeft--; jumpQueued = -1;
                if (AdaptiveDirector.Current != null) AdaptiveDirector.Current.RecordJump();
                // Only two quick, separate presses produce the air jump.
                float impulse = second ? 7.7f : 9.2f;
                body.velocity = new Vector3(body.velocity.x, impulse, 0);
                jumpBoostUntil = Time.time + (second ? .09f : .24f);
                lastJumpAt = Time.time;
                if (second) doubleTapReady = false;
                else { doubleTapReady = firstJumpHeld = true; firstReleaseAt = -100f; }
                game.Flash(second ? "DOUBLE JUMP" : "LEAP / HOLD W FOR HEIGHT", .65f);
            }
            if (Input.GetKeyDown(KeyCode.Space) && !crouching && Time.time >= dashReady)
            {
                // Lock the dash to the direction of movement at activation.
                // AimDirection can face the mouse even while walking the other way.
                dashDirection = Mathf.Abs(move) > .1f ? (move > 0 ? 1 : -1) : lastMoveDirection;
                facing = dashDirection;
                if (AdaptiveDirector.Current != null) AdaptiveDirector.Current.RecordDash();
                dashReady = Time.time + 2.1f; dashUntil = Time.time + .22f; dashing = true;
                if (GuardDirector.Current != null)
                    GuardDirector.Current.ReportSound(transform.position, 15f, "DASH HEARD");
                game.Sound("dash");
                game.Flash("RIFT DASH  /  BREAK WEAK WALLS", .7f);
            }
            if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
                game.UseSpecial();
            if (Input.GetMouseButton(0) && Time.time >= fireReady)
            {
                fireReady = Time.time + .23f;
                if (GuardDirector.Current != null)
                    GuardDirector.Current.ReportSound(transform.position, 11f, "BLASTER HEARD");
                ProjectilePool.Current.Fire(transform.position + new Vector3(facing * .63f, 1.06f, 0),
                    AimDirection(), false, 1 + game.PlayerShotBonus);
                muzzleTransform.localPosition = new Vector3(facing * .85f, 1.08f, -.7f);
                muzzleSparks.Emit(10);
                game.Shake(.035f);
                game.Sound("blaster");
            }
            if (game.Level >= 2 && Input.GetMouseButtonDown(1) && Energy >= 35)
            {
                Energy -= 35;
                if (GuardDirector.Current != null)
                    GuardDirector.Current.ReportSound(transform.position, 15f, "ARC VOLLEY HEARD");
                Vector3 direction = AimDirection();
                Vector3 origin = transform.position + new Vector3(facing * .65f, 1.06f, 0);
                ProjectilePool.Current.Fire(origin, direction, false, 3);
                ProjectilePool.Current.Fire(origin, Quaternion.Euler(0, 0, 13) * direction, false, 2);
                ProjectilePool.Current.Fire(origin, Quaternion.Euler(0, 0, -13) * direction, false, 2);
                muzzleSparks.Emit(22);
                game.Shake(.14f);
                RunGame.Burst(origin, Color.cyan);
                game.Sound("dash");
                game.Flash("ARC VOLLEY  /  " + Energy + "% CHARGE REMAINS", 1.2f);
            }
            if (game.Level >= 2 && Input.GetKeyDown(KeyCode.Q) && Energy >= 30 && Time.time >= shieldReady)
            {
                Energy -= 30;
                shieldUntil = Mathf.Max(shieldUntil, Time.time + 2.4f);
                shieldReady = Time.time + 8f;
                game.Sound("checkpoint");
                game.Flash("PHASE SHIELD  /  2.4 SECONDS OF PROTECTION", 1.6f);
            }
            if (game.Level >= 3 && Input.GetKeyDown(KeyCode.E) && Energy >= 60)
            {
                Energy -= 60;
                Vector3 center = transform.position + Vector3.up;
                foreach (var target in Physics.OverlapSphere(center, 4.2f))
                {
                    var foe = target.GetComponent<Sentinel>();
                    if (foe != null) foe.TakeDamage(3);
                    var relay = target.GetComponent<PowerRelay>();
                    if (relay != null) relay.TakeDamage(2);
                    var wall = target.GetComponent<Breakable>();
                    if (wall != null) wall.Shatter();
                    var mine = target.GetComponent<ProximityMine>();
                    if (mine != null) mine.Disarm();
                }
                RunGame.Burst(center, new Color(.25f, .89f, 1f));
                game.Shake(.28f);
                game.Sound("dash");
                game.Flash("SHOCKWAVE  /  AREA BURST", 1.4f);
            }
            if (shieldVisual != null) shieldVisual.enabled = Shielded;
            if (dashing && Time.time >= dashUntil) dashing = false;
            if (dashing)
            {
                foreach (Collider other in Physics.OverlapSphere(transform.position +
                    new Vector3(dashDirection * .7f, .9f, 0), .72f))
                {
                    var breakable = other.GetComponent<Breakable>();
                    if (breakable != null) breakable.Shatter();
                    var enemy = other.GetComponent<Sentinel>();
                    if (enemy != null) enemy.TakeDamage(1);
                    var mine = other.GetComponent<ProximityMine>();
                    if (mine != null) mine.Disarm();
                }
            }
            if (rig != null)
            {
                int visualDirection = dashing ? dashDirection : facing;
                rig.localRotation = illustrated ? Quaternion.identity :
                    Quaternion.Euler(0, visualDirection > 0 ? 90 : -90, 0);
                rig.localScale = new Vector3(illustrated ? visualDirection : 1, 1, 1);
                if (characterSprite != null)
                    characterSprite.sprite = crouching && crouchSprite != null ? crouchSprite : standingSprite;
            }
            if (animator != null)
            {
                animator.SetBool("Running", Mathf.Abs(move) > .1f && grounded && !crouching);
                animator.SetBool("Airborne", !grounded);
                animator.SetBool("Dashing", dashing);
            }
            if (walkRig != null)
                walkRig.SetWalking(grounded && !crouching && !dashing && !diving &&
                    Mathf.Abs(body.velocity.x) > .3f);
            if (gun != null)
            {
                Vector3 aim = AimDirection();
                gun.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg);
            }
            TrackSafeGroundAndRecover(grounded, groundHit, move);
        }

        private void TrackSafeGroundAndRecover(bool grounded, RaycastHit groundHit, float move)
        {
            bool ceilingClose = false;
            foreach (Collider obstacle in Physics.OverlapSphere(transform.position + Vector3.up * 1.78f,
                .33f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                // A wall beside the player is not a crushing ceiling.
                if (obstacle.bounds.min.y > transform.position.y + 1.32f)
                { ceilingClose = true; break; }
            }
            if (grounded && groundHit.collider != null &&
                groundHit.collider.GetComponentInParent<MovingPlatform>() == null &&
                !ceilingClose && !crouching &&
                Mathf.Abs(transform.position.x - lastSafeGround.x) > 1.25f)
                lastSafeGround = new Vector3(transform.position.x,
                    groundHit.point.y + .16f, 0);

            // A fast drop that penetrates a floor must never leave the runner
            // trapped beneath a solid island. Do not rescue an ordinary fall
            // through an open gap, where losing a life is intentional.
            bool belowFloor = transform.position.y < -.15f &&
                StageBuilder.InsideGroundIsland(game.Level, transform.position.x, .75f);
            underFloorFor = belowFloor ? underFloorFor + Time.deltaTime : 0f;
            if (belowFloor && (underFloorFor > .08f || transform.position.y < -.55f))
            {
                Vector3 safe = lastSafeGround;
                ResetAt(safe + Vector3.up * .08f);
                lastSafeGround = safe;
                game.Flash("BACK ON SOLID GROUND", 1.2f);
                return;
            }

            // Never let a moving step pin the player beneath the next landing.
            // The last clear, stationary footing is a local, penalty-free escape.
            bool pinned = ceilingClose && !crouching && Mathf.Abs(move) > .5f &&
                Mathf.Abs(body.velocity.x) < .18f && Mathf.Abs(body.velocity.y) < .35f;
            blockedFor = pinned ? blockedFor + Time.deltaTime : 0f;
            if (blockedFor > .75f || Input.GetKeyDown(KeyCode.T))
            {
                Vector3 safe = lastSafeGround;
                ResetAt(safe + Vector3.up * .08f);
                lastSafeGround = safe;
                blockedFor = 0f;
                game.Flash("FREE / RETURNED TO SAFE GROUND", 1.4f);
            }
        }

        private bool CanStand()
        {
            // The player is on Ignore Raycast; the overlap test sees the ceiling, not self.
            return !Physics.CheckCapsule(transform.position + Vector3.up * .34f,
                transform.position + Vector3.up * 1.42f, .32f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        }

        private Vector3 AimDirection()
        {
            var cam = Camera.main;
            if (cam == null) return Vector3.right * facing;
            Vector3 mouse = cam.ScreenToWorldPoint(Input.mousePosition);
            Vector3 direction = new Vector3(mouse.x - transform.position.x,
                mouse.y - transform.position.y - 1.1f, 0);
            if (direction.sqrMagnitude < .08f) return Vector3.right * facing;
            facing = direction.x >= 0 ? 1 : -1;
            return direction.normalized;
        }

        private void FixedUpdate()
        {
            if (game == null || !game.CanPlay) { if (body != null) body.velocity = Vector3.zero; return; }
            if (Input.GetKey(KeyCode.W) && Time.time < jumpBoostUntil &&
                body.velocity.y > 0f && !dashing && !diving)
                body.velocity = new Vector3(body.velocity.x,
                    Mathf.Min(12.8f, body.velocity.y + (jumpsLeft == 1 ? 12f : 5f) * Time.fixedDeltaTime), 0);
            float x = dashing ? dashDirection * 17f * game.DashMultiplier :
                Input.GetAxisRaw("Horizontal") * (crouching ? MoveSpeed * .48f : MoveSpeed);
            float vertical = nearbyClimbZones > 0 && !crouching && !diving ?
                (Input.GetKey(KeyCode.R) ? 1f : Input.GetKey(KeyCode.DownArrow) ? -1f : 0f) : 0f;
            // The drone slows movement only while deployed; recalling it restores base pace.
            bool carryingDrone = DroneCompanion.Current != null && DroneCompanion.Current.Active;
            if (carryingDrone) x *= DroneMovementMultiplier;
            if (Mathf.Abs(vertical) > .1f && !dashing)
            {
                body.useGravity = false;
                body.velocity = new Vector3(x * .35f,
                    vertical * 5.5f * (carryingDrone ? DroneMovementMultiplier : 1f), 0);
                return;
            }
            body.useGravity = !dashing;
            // The initial S press supplies the dive impulse. Reapplying -16
            // every physics tick can drive the capsule through the floor.
            body.velocity = new Vector3(Mathf.MoveTowards(body.velocity.x, x,
                (dashing ? 120f : 63f) * Time.fixedDeltaTime), dashing ? 0 : body.velocity.y, 0);
            transform.position = new Vector3(transform.position.x, transform.position.y, 0);
        }

        public void ResetAt(Vector3 location)
        {
            body.velocity = Vector3.zero;
            body.position = location;
            body.useGravity = true;
            dashing = false; crouching = false; diving = false; jumpsLeft = 2;
            facing = lastMoveDirection = dashDirection = 1;
            jumpBoostUntil = 0; jumpQueued = -1; lastJumpAt = -100;
            firstReleaseAt = -100f; firstJumpHeld = doubleTapReady = false;
            var capsule = GetComponent<CapsuleCollider>();
            capsule.height = 1.76f; capsule.center = Vector3.up * .88f;
            shieldUntil = 0;
            nearbyClimbZones = 0;
            nearbyRescueZones = 0;
            blockedFor = underFloorFor = 0f;
            lastSafeGround = location;
        }
        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<Fragment>() != null) other.GetComponent<Fragment>().Collect();
            if (other.GetComponent<Checkpoint>() != null) other.GetComponent<Checkpoint>().Activate();
            if (other.GetComponent<Hazard>() != null) game.Hurt(1, "SPIKES");
            var ladder = other.GetComponent<ClimbZone>();
            if (ladder != null)
            {
                nearbyClimbZones++;
                if (ladder.Rescue) nearbyRescueZones++;
            }
            var coin = other.GetComponent<PowerCoin>();
            if (coin != null) coin.Collect();
        }
        private void OnTriggerStay(Collider other)
        {
            var signal = other.GetComponent<Fragment>();
            if (signal != null && !signal.Healing) signal.Collect();
        }
        private void OnTriggerExit(Collider other)
        {
            var ladder = other.GetComponent<ClimbZone>();
            if (ladder != null)
            {
                nearbyClimbZones = Mathf.Max(0, nearbyClimbZones - 1);
                if (ladder.Rescue) nearbyRescueZones = Mathf.Max(0, nearbyRescueZones - 1);
            }
        }
        private void OnCollisionEnter(Collision hit)
        {
            if (hit.collider.GetComponent<Hazard>() != null) game.Hurt(1, "SPIKES");
            if (diving)
                foreach (ContactPoint contact in hit.contacts)
                    if (contact.normal.y > .6f) { EndDive(); break; }
        }
        private void OnCollisionStay(Collision hit)
        {
            if (!diving) return;
            foreach (ContactPoint contact in hit.contacts)
                if (contact.normal.y > .6f) { EndDive(); break; }
        }
        private void EndDive()
        {
            diving = false;
            RunGame.Burst(transform.position + Vector3.up * .12f, new Color(.98f, .66f, .3f));
        }
    }
}
