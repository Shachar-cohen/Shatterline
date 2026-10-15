using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Ink.Runtime;
using Ink.UnityIntegration;

namespace Shatterline
{
    public sealed class RunGame : MonoBehaviour
    {
        public static RunGame Current { get; private set; }
        public int Level { get; private set; }
        public bool Paused { get; private set; }
        public bool Finished { get; private set; }
        public Runner Player { get; private set; }
        public float CollapseX { get; private set; }
        public float LastCheckpoint { get; private set; }
        public int ShardsHere { get; private set; }
        public bool BossDown { get; private set; }
        [SerializeField] private GameObject guardianBarrier;
        public void RegisterGuardianBarrier(GameObject barrier) { guardianBarrier = barrier; }
        public int RelaysLeft { get; private set; }
        private float RiftPace
        {
            get
            {
                float basePace = .67f + (Level - 1) * .12f;
                if (RunState.DirectiveAct != Level) return basePace;
                if (Level == 1 && RunState.Directive == 1 || Level == 3 && RunState.Directive == 2)
                    return basePace * .8f;
                if (Level == 1 && RunState.Directive == 2 || Level == 2 && RunState.Directive == 3 ||
                    Level == 3 && RunState.Directive == 1) return basePace * 1.2f;
                return basePace;
            }
        }
        public float GuardFireIntervalMultiplier
        {
            get { return Level == 2 && RunState.Directive == 1 ? 1.4f :
                (Level == 2 && RunState.Directive == 2 || Level == 3 && RunState.Directive == 3) ? .8f : 1f; }
        }
        public int PlayerShotBonus { get { return Level == 1 && RunState.Directive == 3 ? 1 : 0; } }
        public int RelayShotBonus { get { return Level == 3 && RunState.Directive == 1 ? 1 : 0; } }
        public bool CanPlay { get { return !Paused && !Finished && !failing && !transitioning && !storyOpen && !powerChoiceOpen && Player != null && !starting; } }
        private Camera cameraView;
        [SerializeField] private StageScene stage;
        [SerializeField] private AudioSystem audioSystem;
        [SerializeField] private InkFile narrative;
        private Vector3 cameraVelocity;
        private int hp = 100;
        public int Health { get { return hp; } }
        public int BossHealth { get; private set; } = 16;
        public void UpdateBossHealth(int amount) { BossHealth = Mathf.Max(0, amount); }
        private bool starting = true, failing, transitioning;
        private int dialogueInputFrame = -1;
        // One fall is one lost life, even if the player briefly remains below the map
        // or inside a hazard while the checkpoint respawn settles.
        private bool waitingForRespawnLanding;
        private float levelTime, damageUntil, hintUntil, hitFlashUntil, gateHintUntil, shakeUntil, shakeStrength;
        public void Shake(float strength)
        {
            shakeStrength = Mathf.Max(shakeStrength, strength);
            shakeUntil = Time.unscaledTime + .15f;
        }
        private string hint = "";
        private string startupError;
        private Fragment[] signalFragments;
        private Fragment carriedSignal;
        private GameObject carriedVisual;
        private GameObject uploadVisual;
        private SignalStation uploadStation;
        private readonly List<Sentinel> uploadAttackers = new List<Sentinel>();
        private float uploadProgress, uploadHealth, uploadNextWave, uploadStaggerUntil, uploadRetryAt, uploadAwaySince = -1f;
        private float uploadProtectionUntil;
        private int uploadSpawned;
        private bool uploadPlacing;
        private int coreHealth;
        private float carrierHeat, lastCarrierSighting, nextReinforcement, lostSignalUntil;
        private int reinforcements;
        private int combatPointsThisAct, coinPointsThisAct;
        private int completionBonus, timeBonus, survivalBonus, coreBonus;
        private string leaderboardMessage = "";
        private bool resultSaved;
        public bool CarryingSignal { get { return carriedSignal != null; } }
        private bool cacheHintShown;
        private Story inkStory;
        private string inkLine = "";
        private bool storyOpen, powerChoiceOpen;
        private const int SpecialPrice = 8;
        public bool StoryOpen { get { return storyOpen; } }
        public float DashMultiplier { get { return RunState.Tactic == 1 ? 1.28f : 1f; } }

        public void SetAuthoringLevel(int act) { Level = act; }

        private void Awake()
        {
            Current = this;
            if (RunState.RunSeed == 0)
                RunState.RunSeed = System.Environment.TickCount ^ (int)System.DateTime.UtcNow.Ticks;
            Level = stage != null ? stage.Act : SceneManager.GetActiveScene().name == "RiftOne" ? 1 :
                SceneManager.GetActiveScene().name == "RiftTwo" ? 2 : 3;
            if (RunState.DirectiveAct != Level)
            {
                RunState.DirectiveAct = Level;
                RunState.Directive = 0;
            }
            RelaysLeft = Level == 3 ? 3 : 0;
            Application.targetFrameRate = 75;
            Time.timeScale = 1;
        }

        private void Start()
        {
            try { stage.Initialize(this); }
            catch (System.Exception error)
            {
                Debug.LogException(error);
                startupError = error.GetType().Name + ": " + error.Message;
                return;
            }
            signalFragments = stage.Fragments;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            gameObject.AddComponent<DevConsole>();
#endif
            LastCheckpoint = 0;
            CollapseX = -14f;
            cameraView = stage.StageCamera;
            Player = stage.Player;
            var inkFile = narrative;
            if (inkFile != null && inkFile.isCompiled)
            {
                inkStory = new Story(inkFile.storyJson);
                inkStory.variablesState["act"] = Level;
                inkStory.variablesState["tactic"] = RunState.Tactic;
                inkStory.variablesState["directive"] = RunState.Directive;
                inkStory.variablesState["intel"] = RunState.Intel;
                OpenStory("intro");
            }
            else Debug.LogError("Ink narrative asset failed to import: Assets/Narrative/Signal.ink");
            if (cameraView != null && Player != null)
                cameraView.transform.position = new Vector3(Player.transform.position.x + 3, 5.5f, -19);
            Flash(Level == 1 ? "THE SIGNAL IS FAILING  /  RUN" : Level == 2 ?
                "THE CLOCKWORK QUARTER  /  KEEP MOVING" : "THE CORE  /  END THE COLLAPSE", 4);
            StartCoroutine(Begin());
        }

        private IEnumerator Begin() { yield return new WaitForSecondsRealtime(.85f); starting = false; }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape) && !Finished && !transitioning && !storyOpen && !powerChoiceOpen)
            {
                SetPaused(!Paused);
            }
            if (storyOpen && Input.GetKeyDown(KeyCode.Return) && inkStory != null && inkStory.currentChoices.Count == 0)
                ContinueDialogue();
            if (!storyOpen && !powerChoiceOpen && !Paused && !Finished && !transitioning && !starting &&
                RunState.Special == 0 && RunState.Coins >= SpecialPrice)
            {
                powerChoiceOpen = true;
                Time.timeScale = 0;
                Cursor.visible = true;
            }
            if (!CanPlay) return;
            RunState.PlaySeconds += Time.deltaTime;
            if (waitingForRespawnLanding)
            {
                RaycastHit floor;
                if (Physics.Raycast(Player.transform.position + Vector3.up * .15f,
                    Vector3.down, out floor, .28f, Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore) && floor.normal.y > .6f)
                    waitingForRespawnLanding = false;
                else if (Player.transform.position.y < -3.5f)
                {
                    // Recover a failed checkpoint landing without charging another life.
                    Player.ResetAt(new Vector3(LastCheckpoint + 1f, .2f, 0));
                    return;
                }
            }
            // The rift slows during a mandatory defense, but does not stop.
            levelTime += Time.deltaTime * (uploadStation != null ? .25f : 1f);
            UpdateSignalUpload();
            if (carriedSignal != null)
            {
                // A sighted courier draws increasing attention; breaking line of sight lets heat cool.
                carrierHeat = Mathf.Clamp(carrierHeat + (Time.time - lastCarrierSighting < 5f ?
                    Time.deltaTime * 2.3f : -Time.deltaTime * 2f), 0, 100);
                if (uploadStation == null && carrierHeat >= 25f && Time.time >= nextReinforcement && reinforcements < 3 &&
                    Player.transform.position.x < 181f)
                {
                    nextReinforcement = Time.time + Mathf.Max(9f, 17f - carrierHeat * .07f);
                    reinforcements++;
                    float x = Mathf.Clamp(Player.transform.position.x + 10f, 14f, 185f);
                    var hunter = StageBuilder.SpawnSentinel(this, x, Player.transform.position.y + 3f,
                        false, true, true, x);
                    hunter.ReceiveSighting(Player.transform.position);
                    Flash("SIGNAL ALERT  /  SCOUT REINFORCEMENTS INBOUND", 2.5f);
                }
            }
            else if (lostSignalUntil > 0 && Time.time >= lostSignalUntil)
            {
                lostSignalUntil = 0;
                foreach (var signal in signalFragments)
                    if (signal != null && !signal.Healing && !signal.Collected && !signal.Carried &&
                        !signal.Recovered && signal.transform.position.x < Player.transform.position.x - 8f)
                    {
                        signal.MoveToCache(new Vector3(241f + signal.SignalIndex * 3f, 1.35f, 0));
                        Flash("LOST CORE MOVED TO EXIT CACHE  /  RECOVER IT", 3f);
                    }
            }
            CollapseX = Mathf.Max(CollapseX, -14f + levelTime * RiftPace);
            // Keep the relay approach recoverable during the mandatory final
            // fight. Resume the rift from this position once the boss falls.
            if (Level == 3 && !BossDown && Player.transform.position.x >= 198f)
                CollapseX = Mathf.Min(CollapseX, 194f);
            RestoreMissedSignals();
            if (Player.transform.position.y < -3.5f && !Player.CatchingRescueLadder)
            {
                Hurt(99, "FELL INTO THE ABYSS");
                return;
            }
            if (Player.transform.position.x < CollapseX)
            {
                Hurt(99, "THE RIFT CAUGHT YOU");
                return;
            }
            if (Player.transform.position.x >= StageBuilder.PortalX - 2)
            {
                // The first relay can be skipped by jumping over it. Never strand a player
                // who reached the exit with both fragments: let the gate offer the choice.
                if (ShardsHere >= 2 && RunState.Tactic == 0 && inkStory != null)
                {
                    OpenStory("choice");
                    return;
                }
                if (ShardsHere >= 2 && RunState.Directive == 0 && inkStory != null)
                {
                    OpenMission();
                    return;
                }
                bool ready = ShardsHere >= 2 && RunState.Tactic != 0 &&
                    (inkStory == null || RunState.Directive != 0) && (Level != 3 || BossDown);
                if (ready)
                {
                    if (Level == 3) Win(); else NextLevel();
                }
                else if (Time.time > gateHintUntil)
                {
                    gateHintUntil = Time.time + 2.5f;
                    Flash(ShardsHere < 2 ? "GATE LOCKED: DELIVER SIGNAL CORES (" + ShardsHere + "/2). CHECK EXIT CACHE" :
                        RunState.Tactic == 0 ? "GATE LOCKED: TOUCH THE GREEN RELAY AND PICK AN UPGRADE" :
                        RunState.Directive == 0 ? "GATE LOCKED: CHOOSE THE SECOND RELAY DIRECTIVE" :
                        "GATE LOCKED: DEFEAT THE GUARDIAN", 2.4f);
                }
                // A locked portal returns the player to its approach instead of hiding an impassable wall
                // or allowing a fatal drop past the end of the level.
                if (!ready && Player.transform.position.x > StageBuilder.PortalX + 5)
                    Player.ResetAt(new Vector3(StageBuilder.PortalX - 8, 1.5f, 0));
            }
        }

        // A forward-moving rift can make an earlier missed fragment unreachable.
        // Deliver only the uncollected required signals to the safe exit approach.
        private void RestoreMissedSignals()
        {
            if (Player == null || signalFragments == null || Player.transform.position.x < 170f) return;
            int slot = 0;
            foreach (var fragment in signalFragments)
            {
                if (fragment == null || fragment.Healing || fragment.Collected || fragment.Carried) continue;
                if (fragment.Recovered) { slot++; continue; }
                if (fragment.transform.position.x >= Player.transform.position.x - 19f) continue;
                fragment.MoveToCache(new Vector3(241f + slot * 3f, 1.35f, 0));
                slot++;
                if (!cacheHintShown)
                {
                    cacheHintShown = true;
                    Flash("MISSED CORE AT EXIT CACHE / CARRY IT TO THE CYAN TERMINAL", 4f);
                }
            }
        }

        private string SignalObjective()
        {
            if (uploadStation != null)
                return uploadPlacing ? "PLACING CORE IN TERMINAL" :
                    uploadProgress >= 1f ? "CLEAR THE ATTACKERS TO SECURE THE CORE" :
                    "PROTECT THE UPLOADING CORE";
            if (carriedSignal != null)
                return "DELIVER CORE  /  " + coreHealth + "% INTEGRITY";
            if (ShardsHere >= 2)
            {
                if (RunState.Tactic == 0) return "CHOOSE RELAY UPGRADE";
                if (RunState.Directive == 0) return "CHOOSE MISSION DIRECTIVE";
                if (Level == 3 && RelaysLeft > 0) return "DESTROY RELAYS  " + RelaysLeft + " LEFT";
                if (Level == 3 && !BossDown) return "DEFEAT THE GUARDIAN";
                return "REACH THE EXIT GATE";
            }
            Fragment next = null;
            if (signalFragments != null)
                foreach (var fragment in signalFragments)
                    if (fragment != null && !fragment.Healing && !fragment.Collected && !fragment.Carried &&
                        (next == null || fragment.transform.position.x < next.transform.position.x))
                        next = fragment;
            if (next == null || Player == null) return "FIND CYAN CORE";
            float difference = next.transform.position.x - Player.transform.position.x;
            if (difference < -19f && !next.Recovered)
                return "CORE AT EXIT CACHE  →";
            return "CYAN CORE  " + (difference >= 0 ? "→ " : "← ") +
                Mathf.RoundToInt(Mathf.Abs(difference)) + "m" +
                (next.Recovered ? "  /  EXIT CACHE" : "");
        }

        private void LateUpdate()
        {
            if (cameraView == null || Player == null) return;
            float follow = Mathf.Max(Player.transform.position.x + 3.8f, CollapseX + 9f);
            Vector3 desired = new Vector3(Mathf.Clamp(follow, 2, StageBuilder.PortalX + 7),
                Mathf.Clamp(Player.transform.position.y + 3, 3.7f, 9), -19);
            cameraView.transform.position = Vector3.SmoothDamp(cameraView.transform.position,
                desired, ref cameraVelocity, .14f);
            if (Time.unscaledTime < shakeUntil)
                cameraView.transform.position += new Vector3(Random.Range(-shakeStrength, shakeStrength),
                    Random.Range(-shakeStrength, shakeStrength), 0);
            else shakeStrength = 0;
            cameraView.transform.rotation = Quaternion.Euler(5, 0, 0);
        }

        public void Flash(string message, float duration = 2.2f)
        { hint = message; hintUntil = Time.unscaledTime + duration; }

        public void CollectCoin()
        {
            RunState.Coins++;
            RunState.CoinsCollected++;
            // Currency remains unlimited; only the first 16 coins in each act earn points.
            if (coinPointsThisAct++ < 16) RunState.Score += 25;
            Sound("pickup");
            Flash("POWER COIN  " + Mathf.Min(RunState.Coins, SpecialPrice) + "/" + SpecialPrice +
                (RunState.Special != 0 ? "  /  POWER STORED: SHIFT" : ""), 1.35f);
        }

        public void GrantAdaptiveAid(int amount)
        {
            if (hp >= 100) return;
            int restored = Mathf.Min(amount, 100 - hp);
            hp += restored;
            Flash("ROUTE ASSIST / +" + restored + " HP / HAZARDS EASE AHEAD", 2.1f);
            Sound("checkpoint");
        }

        private void ChooseSpecial(int index)
        {
            if (!powerChoiceOpen || RunState.Coins < SpecialPrice) return;
            RunState.Coins -= SpecialPrice;
            RunState.Special = index;
            powerChoiceOpen = false;
            Time.timeScale = 1;
            Flash("ONE-TIME POWER READY  /  PRESS SHIFT", 3f);
        }

        public void UseSpecial()
        {
            int selected = RunState.Special;
            if (selected == 0)
            {
                Flash("COLLECT " + SpecialPrice + " GOLD COINS TO CHOOSE A SHIFT POWER", 1.6f);
                return;
            }
            if (selected == 3 && hp >= 100)
            {
                Flash("HP FULL  /  SAVE THE REPAIR FOR LATER", 1.6f);
                return;
            }
            RunState.Special = 0;
            if (selected == 1)
            {
                Vector3 center = Player.transform.position + Vector3.up;
                foreach (Collider target in Physics.OverlapSphere(center, 5.4f))
                {
                    var foe = target.GetComponent<Sentinel>();
                    if (foe != null) foe.TakeDamage(4);
                    var relay = target.GetComponent<PowerRelay>();
                    if (relay != null) relay.TakeDamage(3);
                    var wall = target.GetComponent<Breakable>();
                    if (wall != null) wall.Shatter();
                    var mine = target.GetComponent<ProximityMine>();
                    if (mine != null) mine.Disarm();
                }
                Burst(center, new Color(1f, .79f, .2f));
                Shake(.36f);
                Flash("SPECIAL: EMP BLAST  /  NEARBY THREATS CLEARED", 2.5f);
            }
            else if (selected == 2)
            {
                Player.ActivateSpecialShield(5f);
                Burst(Player.transform.position + Vector3.up, Color.cyan);
                Flash("SPECIAL: AEGIS  /  5 SECONDS INVULNERABLE", 2.5f);
            }
            else
            {
                hp = Mathf.Min(100, hp + 45);
                Burst(Player.transform.position + Vector3.up, Color.green);
                Flash("SPECIAL: REPAIR  /  +45 HP  /  " + hp + "/100", 2.5f);
            }
            Sound("checkpoint");
        }

        public void Pickup(bool life = false)
        {
            if (!life) { RunState.Shards++; ShardsHere++; }
            // The six mandatory delivered cores award points; repeatable healing does not.
            if (!life) RunState.Score += 100;
            Sound("pickup");
            if (life) { hp = Mathf.Min(100, hp + 24); Flash("POWER RESTORED  +24 HP"); }
            else
            {
                string revelation = Level == 1 ? "They knew the rift would open." :
                    Level == 2 ? "The guardian is a future version of you." :
                    "The city survives if the core goes dark.";
                Flash("SIGNAL " + ShardsHere + "/2  /  " + revelation, 4f);
                if (ShardsHere == 2 && inkStory != null) OpenStory("revelation");
            }
        }

        public bool TakeSignal(Fragment signal)
        {
            if (!CanPlay || carriedSignal != null || signal == null) return false;
            carriedSignal = signal;
            coreHealth = 100;
            carrierHeat = 0;
            reinforcements = 0;
            lastCarrierSighting = -100f;
            nextReinforcement = Time.time + 7f;
            lostSignalUntil = 0;
            // The illustrated courier already has a detailed real backpack.
            // A tiny lit seal on its cyan strip conveys the carried core
            // without hiding the character beneath a second blocky bag.
            carriedVisual = Instantiate(stage.BackpackSealPrefab, Player.VisualRig, false);
            carriedVisual.transform.localPosition = new Vector3(-.49f, 1.40f, -.94f);
            Burst(Player.transform.position + Vector3.up, Color.cyan);
            Sound("pickup");
            Flash("CORE IN BAG  /  DEFEND A CYAN TERMINAL TO UPLOAD IT", 4f);
            return true;
        }

        public bool IsUploadingAt(SignalStation station)
        { return uploadStation == station && !uploadPlacing && carriedSignal != null; }

        public void BeginSignalUpload(SignalStation station)
        {
            if (station == null || carriedSignal == null || !CanPlay || uploadStation != null ||
                Time.time < uploadRetryAt) return;
            uploadStation = station;
            uploadProgress = 0f;
            uploadHealth = 100f;
            uploadSpawned = 0;
            uploadPlacing = true;
            uploadNextWave = Time.time + (Level == 3 ? 2.6f : 1.4f);
            uploadStaggerUntil = 0f;
            uploadProtectionUntil = 0f;
            uploadAwaySince = -1f;
            StartCoroutine(PlaceSignalInStation(station));
        }

        private IEnumerator PlaceSignalInStation(SignalStation station)
        {
            uploadVisual = Instantiate(stage.UploadCorePrefab);
            uploadVisual.name = "CORE / MOVING FROM BAG TO TERMINAL";
            uploadVisual.transform.localScale = Vector3.one * .56f;
            var visualMaterial = uploadVisual.GetComponent<Renderer>().material;
            visualMaterial.color = Color.cyan;
            visualMaterial.EnableKeyword("_EMISSION");
            visualMaterial.SetColor("_EmissionColor", Color.cyan * 1.5f);
            float elapsed = 0f;
            while (elapsed < .75f && uploadStation == station && carriedSignal != null)
            {
                elapsed += Time.deltaTime;
                Vector3 from = carriedVisual != null ? carriedVisual.transform.position : Player.transform.position + Vector3.up;
                uploadVisual.transform.position = Vector3.Lerp(from, station.Beacon.position,
                    Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / .75f)));
                yield return null;
            }
            if (uploadStation == station && carriedSignal != null)
            {
                if (uploadVisual != null) uploadVisual.transform.position = station.Beacon.position;
                if (carriedVisual != null) carriedVisual.SetActive(false);
                uploadPlacing = false;
                Flash("UPLOAD STARTED  /  PROTECT THE TERMINAL", 2.4f);
            }
        }

        private void UpdateSignalUpload()
        {
            if (uploadStation == null || uploadPlacing || carriedSignal == null) return;
            var beacon = uploadStation.Beacon.GetComponent<Renderer>();
            if (beacon != null)
                beacon.material.color = Time.time < uploadProtectionUntil ? Color.cyan :
                    Color.Lerp(new Color(1f, .23f, .14f), Color.cyan, uploadHealth / 100f);
            float distance = Mathf.Abs(Player.transform.position.x - uploadStation.transform.position.x);
            bool nearby = distance < 7.5f && Mathf.Abs(Player.transform.position.y - uploadStation.transform.position.y) < 5f;
            if (!nearby)
            {
                if (uploadAwaySince < 0f) uploadAwaySince = Time.time;
                if (Time.time - uploadAwaySince > 6f)
                {
                    CancelSignalUpload(true);
                    Flash("UPLOAD INTERRUPTED  /  RETURN TO THE TERMINAL", 3f);
                }
                return;
            }
            uploadAwaySince = -1f;
            // Act III previously stacked five or six rapid flying attackers.
            // Keep a bounded defense encounter, with at most two active reinforcements.
            int limit = Level == 3 ? 4 + (ShardsHere == 0 ? 0 : 1) :
                3 + (ShardsHere == 0 ? 0 : 1) + (Level - 1);
            int activeAttackers = 0;
            foreach (var attacker in uploadAttackers)
                if (attacker != null) activeAttackers++;
            if (Time.time >= uploadNextWave && uploadSpawned < limit &&
                (Level != 3 || activeAttackers < 2))
            {
                SpawnUploadAttacker();
                uploadNextWave = Time.time + (Level == 3 ? 4.5f : Level == 1 ? 3.4f : 2.8f);
            }
            if (Time.time < uploadStaggerUntil) return;
            float duration = 10f + (ShardsHere == 0 ? 0f : 3f) + (Level - 1) * 1.5f;
            uploadProgress = Mathf.Clamp01(uploadProgress + Time.deltaTime / duration);
            if (uploadProgress >= 1f && uploadSpawned >= limit &&
                uploadAttackers.TrueForAll(attacker => attacker == null))
                CompleteSignalUpload();
        }

        private void SpawnUploadAttacker()
        {
            if (uploadStation == null) return;
            // Dedicated flying guards can approach from either bank even when
            // the terminal borders an abyss; the count is bounded per upload.
            float direction = uploadSpawned % 2 == 0 ? 1f : -1f;
            float stationX = uploadStation.transform.position.x;
            float x = stationX + direction * (8.5f + (uploadSpawned % 3) * 1.5f);
            var guard = StageBuilder.SpawnSentinel(this, x, 2.4f + uploadSpawned % 2,
                false, true, Level == 3 ? uploadSpawned % 3 == 2 : Level > 1, x);
            guard.DefenseTarget = uploadStation;
            uploadAttackers.Add(guard);
            uploadSpawned++;
        }

        public bool DamageUploadStation(SignalStation station)
        {
            if (!IsUploadingAt(station) || !CanPlay) return false;
            // The Act III terminal briefly stabilizes after a hit. Consume the
            // projectile during that window so overlapping shots cannot erase it.
            if (Level == 3 && Time.time < uploadProtectionUntil) return true;
            uploadHealth = Mathf.Max(0f, uploadHealth -
                (Level == 1 ? 13f : Level == 3 ? 12f : 16f));
            uploadStaggerUntil = Time.time + .55f;
            if (Level == 3) uploadProtectionUntil = Time.time + 1.8f;
            Shake(.12f);
            if (uploadHealth <= 0f) CancelSignalUpload(true);
            return true;
        }

        public void UploadAttackerDefeated(SignalStation station)
        {
            if (Level == 3 && IsUploadingAt(station))
                uploadHealth = Mathf.Min(100f, uploadHealth + 10f);
        }

        private void CompleteSignalUpload()
        {
            SignalStation station = uploadStation;
            CancelSignalUpload(false);
            DeliverSignal();
            if (station != null) Burst(station.transform.position + Vector3.up, Color.cyan);
        }

        private void CancelSignalUpload(bool failed)
        {
            if (uploadStation == null) return;
            if (uploadStation.Beacon != null)
            {
                var beacon = uploadStation.Beacon.GetComponent<Renderer>();
                if (beacon != null) beacon.material.color = Color.cyan;
            }
            uploadStation = null;
            uploadPlacing = false;
            uploadProgress = 0f;
            uploadProtectionUntil = 0f;
            uploadAwaySince = -1f;
            if (uploadVisual != null) Destroy(uploadVisual);
            uploadVisual = null;
            if (carriedVisual != null) carriedVisual.SetActive(true);
            foreach (var attacker in uploadAttackers)
                if (attacker != null) Destroy(attacker.gameObject);
            uploadAttackers.Clear();
            if (failed)
            {
                uploadRetryAt = Time.time + 2.5f;
                Flash("UPLOAD FAILED  /  CORE RETURNED TO BAG  /  RETRY", 3f);
            }
        }

        public void CarrierSpotted(Vector3 position)
        {
            if (carriedSignal == null || !CanPlay) return;
            bool fresh = Time.time - lastCarrierSighting > 6f;
            lastCarrierSighting = Time.time;
            if (!fresh) return;
            carrierHeat = Mathf.Min(100f, carrierHeat + 20f);
            if (GuardDirector.Current != null)
                GuardDirector.Current.ReportSound(position, 20f, "SQUAD ALERT: SIGNAL CARRIER");
            Flash("CARRIER IDENTIFIED  /  BREAK LINE OF SIGHT TO LOWER HEAT", 2.8f);
        }

        public void DeliverSignal()
        {
            if (carriedSignal == null || !CanPlay) return;
            Fragment delivered = carriedSignal;
            carriedSignal = null;
            if (carriedVisual != null) Destroy(carriedVisual);
            delivered.Deliver();
            RunState.CoreIntegrity += coreHealth;
            carrierHeat = 0;
            lostSignalUntil = 0;
            Pickup();
            Player.Recharge(22);
            Flash("CORE DELIVERED  " + ShardsHere + "/2  /  NETWORK RESTORED", 3.5f);
            Burst(Player.transform.position, Color.green);
        }

        private void DamageCore(int amount)
        {
            if (carriedSignal == null) return;
            coreHealth = Mathf.Max(0, coreHealth - amount);
            if (coreHealth > 0)
            {
                Flash("CORE DAMAGED  /  " + coreHealth + "%  /  FIND CYAN TERMINAL", 2f);
                return;
            }
            DropSignal(false);
        }

        private void DropSignal(bool dying)
        {
            if (carriedSignal == null) return;
            CancelSignalUpload(false);
            Fragment dropped = carriedSignal;
            carriedSignal = null;
            if (carriedVisual != null) Destroy(carriedVisual);
            carrierHeat = 0;
            Vector3 location = dying ? new Vector3(LastCheckpoint + 3f, 2.4f, 0) :
                Player.transform.position + new Vector3(-1.8f, .7f, 0);
            dropped.Drop(location);
            lostSignalUntil = Time.time + 8f;
            Flash(dying ? "CORE LOST  /  RECOVER IT AFTER RESPAWN" :
                "CORE BROKEN  /  PICK IT UP AGAIN OR FIND IT AT EXIT CACHE", 3.5f);
        }

        public void Checkpoint(float x)
        {
            if (x <= LastCheckpoint) return;
            LastCheckpoint = x; Flash("CHECKPOINT LOCKED  /  HISTORY SAVED", 3f);
            Sound("checkpoint");
            hp = Mathf.Min(100, hp + 20);
            if (carriedSignal != null) coreHealth = Mathf.Min(100, coreHealth + 15);
            if (inkStory == null) return;
            if (x < 120 && RunState.Tactic == 0) OpenStory("choice");
            else if (x >= 160 && RunState.Directive == 0) OpenMission();
        }

        private void OpenMission() { OpenStory("mission_" + (Level == 1 ? "one" : Level == 2 ? "two" : "three")); }

        private void OpenStory(string knot)
        {
            inkStory.ChoosePathString(knot);
            storyOpen = true;
            Time.timeScale = 0f;
            inkLine = "";
            AdvanceStory();
        }
        private void AdvanceStory()
        {
            if (inkStory.canContinue)
            {
                inkLine = inkStory.Continue().Trim();
                if (inkLine.Length == 0) AdvanceStory();
            }
            else if (inkStory.currentChoices.Count == 0)
            {
                storyOpen = false;
                if (!Finished && !Paused && !transitioning && !powerChoiceOpen) Time.timeScale = 1f;
                inkLine = "";
            }
        }
        private void ChooseStory(int index)
        {
            int previousTactic = RunState.Tactic;
            int previousDirective = RunState.Directive;
            inkStory.ChooseChoiceIndex(index);
            AdvanceStory();
            RunState.Tactic = (int)inkStory.variablesState["tactic"];
            if (previousTactic == 0 && RunState.Tactic == 2) hp = Mathf.Min(100, hp + 35);
            RunState.Directive = (int)inkStory.variablesState["directive"];
            RunState.Intel = (int)inkStory.variablesState["intel"];
            if (previousDirective == 0 && RunState.Directive != 0)
            {
                if (Level == 1 && RunState.Directive == 2 || Level == 2 && RunState.Directive == 2)
                    RunState.Coins += 3;
                if (Level == 1 && RunState.Directive == 3) hp = Mathf.Max(1, hp - 12);
                if (Level == 2 && RunState.Directive == 3) hp = Mathf.Min(100, hp + 35);
                if (Level == 3 && RunState.Directive == 3) hp = Mathf.Min(100, hp + 40);
                RunState.Score += 150;
                // Keep the advancing front continuous when a new Ink choice changes its speed.
                levelTime = Mathf.Max(0, (CollapseX + 14f) / RiftPace);
            }
        }

        public void DefeatEnemy(int points)
        {
            // Respawnable enemies cannot be farmed for an unlimited leaderboard score.
            int awarded = Mathf.Min(Mathf.Max(0, points), Mathf.Max(0, 1200 - combatPointsThisAct));
            combatPointsThisAct += awarded;
            RunState.CombatPoints += awarded;
            RunState.Score += awarded;
            Sound("pickup"); Flash(awarded > 0 ? "TARGET DOWN  +" + awarded : "TARGET DOWN", 1.2f);
            if (Player != null) Player.Recharge(18);
        }

        public void BreakRelay()
        {
            RelaysLeft = Mathf.Max(0, RelaysLeft - 1);
            RunState.Score += 175;
            Flash(RelaysLeft == 0 ? "GUARDIAN SHIELD DOWN  /  STRIKE NOW" :
                "RELAY DESTROYED  /  " + RelaysLeft + " STILL POWER THE GUARDIAN", 3.5f);
        }

        public void StartAmbush(int act, float location)
        {
            Flash(act == 1 ? "ALERT: SEARCH DRONES HAVE FOUND YOU" :
                act == 2 ? "CLOCKWORK ALARM: DEFEND THE BRIDGE" :
                "CORE COUNTERMEASURES ACTIVATED", 4f);
            StageBuilder.SpawnSentinel(this, location + 8, 3.5f, false, true);
            StageBuilder.SpawnSentinel(this, location + 16, 2.2f, false, act > 1);
        }

        public void BossDefeated()
        {
            levelTime = Mathf.Max(0f, (CollapseX + 14f) / RiftPace);
            BossDown = true;
            if (guardianBarrier != null) guardianBarrier.SetActive(false);
            RunState.Score += 1000;
            Flash("GUARDIAN DEFEATED  /  PATH TO THE FINAL GATE OPEN", 6);
        }

        public void Hurt(int amount, string source = "HIT", float attackX = float.NaN)
        {
            // A fatal fall or rift contact must still cost a life during the
            // short ordinary-hit invulnerability window.
            if (Finished || failing || waitingForRespawnLanding ||
                (amount < 99 && Time.time < damageUntil)) return;
            if (Player != null && Player.Shielded && amount < 99) return;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            var cheats = GetComponent<DevConsole>();
            if (cheats != null && cheats.Invulnerable) return;
#endif
            int actual = amount >= 99 ? hp : source == "MINE" ? 28 :
                source == "FALLING ROCK" ? 26 : source == "PRESS" ? 24 :
                source == "SAW" || source == "ELECTRIC GRID" || source == "GEAR" ? 22 :
                source == "STEAM" ? 18 :
                source == "SPIKES" ? 23 :
                source == "CONTACT" ? 20 : 15;
            hp -= actual; damageUntil = Time.time + .95f;
            if (AdaptiveDirector.Current != null && amount < 99)
                AdaptiveDirector.Current.RecordHit();
            if (carriedSignal != null && uploadStation == null && amount < 99)
            {
                bool rearShot = source == "ENEMY FIRE" && !float.IsNaN(attackX) &&
                    (attackX - Player.transform.position.x) * Player.Facing < 0;
                int coreDamage = source == "MINE" || source == "FALLING ROCK" ||
                    source == "PRESS" || source == "SAW" || source == "SPIKES" ||
                    source == "ELECTRIC GRID" || source == "GEAR" ? 34 :
                    source == "STEAM" ? 23 :
                    rearShot ? 22 : source == "CONTACT" ? 12 : source == "ENEMY FIRE" ? 0 : 16;
                if (coreDamage > 0) DamageCore(coreDamage);
            }
            hitFlashUntil = Time.unscaledTime + .24f;
            Shake(.19f);
            Sound("damage");
            if (hp > 0) { Flash("-" + actual + " HP  /  " + source + "  /  " + hp + "/100", 1.2f); return; }
            StartCoroutine(Respawn(source));
        }

        private IEnumerator Respawn(string cause)
        {
            failing = true;
            if (AdaptiveDirector.Current != null) AdaptiveDirector.Current.RecordDeath();
            DropSignal(true);
            RunState.Lives--; RunState.Deaths++;
            Flash(cause + "  /  REWINDING", 3);
            yield return new WaitForSeconds(1.1f);
            if (RunState.Lives <= 0) { End(false); yield break; }
            hp = 100; damageUntil = Time.time + 2;
            waitingForRespawnLanding = true;
            // Every checkpoint sits above solid ground. Place the feet close to it
            // rather than dropping the player from midair into a second death.
            Player.ResetAt(new Vector3(LastCheckpoint + 1f, .2f, 0));
            CollapseX = Mathf.Min(CollapseX, LastCheckpoint - 12f);
            levelTime = Mathf.Max(0, (CollapseX + 14f) / RiftPace);
            StageBuilder.RerollAfterDeath(Level, RunState.Deaths, Player.transform.position.x,
                signalFragments);
            cacheHintShown = false;
            Flash("REWIND COMPLETE  /  " + RunState.Lives + " LIVES REMAIN", 2.5f);
            failing = false;
        }

        private void NextLevel()
        {
            if (Finished || transitioning) return;
            transitioning = true; RunState.Score += 500;
            StartCoroutine(ChapterTransition());
        }

        private IEnumerator ChapterTransition()
        {
            Time.timeScale = 0;
            yield return new WaitForSecondsRealtime(2.4f);
            Time.timeScale = 1;
            SceneFlow.NextAct(Level);
        }

        private void Win()
        {
            if (inkStory != null) OpenStory("ending");
            completionBonus = 1200;
            timeBonus = Mathf.Max(0, 1800 - Mathf.Max(0, Mathf.RoundToInt(RunState.PlaySeconds) - 300) * 3);
            survivalBonus = RunState.Lives * 250 + (RunState.Deaths == 0 ? 500 : 0);
            coreBonus = RunState.CoreIntegrity * 2;
            RunState.Score += completionBonus + timeBonus + survivalBonus + coreBonus;
            End(true);
        }
        public void Sound(string id) { if (audioSystem != null) audioSystem.Play(id); }
        public void SetSceneBindings(StageScene value, AudioSystem audio, InkFile story)
        { stage = value; audioSystem = audio; narrative = story; }
        public void SetPaused(bool value)
        {
            if (Finished || transitioning || storyOpen || powerChoiceOpen) return;
            Paused = value; Time.timeScale = value ? 0f : 1f;
            AudioListener.pause = value; Cursor.visible = true;
        }

        public static void Burst(Vector3 position, Color color)
        {
            if (StageBuilder.BurstPrefab == null) return;
            var fx = Instantiate(StageBuilder.BurstPrefab, position, Quaternion.identity);
            var main = fx.main; main.startColor = color;
            fx.Emit(22); Destroy(fx.gameObject, 1f);
        }
        private void End(bool victory)
        {
            Finished = true; Time.timeScale = 0;
            won = victory;
            Cursor.visible = true;
        }
        private bool won;

        public int CarrierHeatPercent { get { return Mathf.RoundToInt(carrierHeat); } }
        public void ResumeGame() { SetPaused(false); }
        public void RestartRun() { SceneFlow.StartRun(); }
        public void ReturnToMenu() { SceneFlow.ReturnToMenu(); }
        public void QuitGame() { SceneFlow.Quit(); }
        public void ChooseEmp() { ChooseSpecial(1); }
        public void ChooseAegis() { ChooseSpecial(2); }
        public void ChooseRepair() { ChooseSpecial(3); }
        public void ContinueDialogue()
        {
            if (!storyOpen || inkStory == null || inkStory.currentChoices.Count != 0 || dialogueInputFrame == Time.frameCount) return;
            dialogueInputFrame = Time.frameCount; AdvanceStory();
        }
        public void ChooseDialogue0() { SelectDialogue(0); }
        public void ChooseDialogue1() { SelectDialogue(1); }
        public void ChooseDialogue2() { SelectDialogue(2); }
        private void SelectDialogue(int index)
        { if (storyOpen && inkStory != null && index < inkStory.currentChoices.Count) ChooseStory(index); }
        public void SaveResult(string name)
        {
            if (!Finished || !won || resultSaved || RunState.Assisted) return;
            int rank = Leaderboard.SaveBest(name, RunState.Score, Mathf.RoundToInt(RunState.PlaySeconds), RunState.Deaths);
            if (rank < 0) leaderboardMessage = "Could not save the score.";
            else if (rank == 0) leaderboardMessage = "Enter a name to save.";
            else { resultSaved = true; leaderboardMessage = "Saved! Your best rank: #" + rank; }
        }
        public RunUiState CaptureUi()
        {
            var companion = DroneCompanion.Current;
            string status = "CORES " + ShardsHere + "/2   COINS " + RunState.Coins + "/" + SpecialPrice;
            if (companion != null && companion.Active)
                status += companion.DefenseCooldown > 0f ? "   F DRONE " + companion.DefenseCooldown.ToString("0") + "s" : "   F DRONE";
            else if (RunState.Special != 0) status += "   SHIFT READY";
            string upload = "UPLOAD " + Mathf.RoundToInt(uploadProgress * 100f) + "%   STATION " + Mathf.RoundToInt(uploadHealth) + "%";
            if (uploadStation != null && Player != null && Mathf.Abs(Player.transform.position.x - uploadStation.transform.position.x) >= 7.5f)
                upload += " / RETURN";
            else if (Time.time < uploadProtectionUntil) upload += " / SHIELD " + (uploadProtectionUntil - Time.time).ToString("0.0") + "s";
            string[] choices = new string[storyOpen && inkStory != null ? inkStory.currentChoices.Count : 0];
            for (int i = 0; i < choices.Length; i++) choices[i] = inkStory.currentChoices[i].text;
            int seconds = Mathf.RoundToInt(RunState.PlaySeconds);
            return new RunUiState {
                Act = Level, Health = Mathf.Max(0, hp), Energy = Player != null ? Player.Energy : 0,
                CoreHealth = coreHealth, BossHealth = BossHealth, Carrying = CarryingSignal,
                Uploading = uploadStation != null, ShowBoss = Level == 3 && !BossDown && Player != null && Player.transform.position.x > 201f,
                Paused = Paused, Finished = Finished, Won = won, Transitioning = transitioning,
                PowerChoice = powerChoiceOpen, Story = storyOpen, HitFlash = Time.unscaledTime < hitFlashUntil,
                ShowHint = Time.unscaledTime < hintUntil, Hint = hint, Objective = SignalObjective(), Status = status,
                UploadProgress = uploadProgress, StationHealth = uploadHealth, UploadText = upload,
                Choices = choices, StoryText = inkLine,
                StoryTitle = choices.Length > 0 ? (RunState.Tactic == 0 ? "CHOOSE A RELAY UPGRADE" : "CHOOSE A MISSION DIRECTIVE") : "INTERCEPTED TRANSMISSION",
                TransitionText = "DISTRICT " + Level + " CLEARED\n\n" + (Level == 1 ?
                    "THE CLOCKWORK QUARTER OPENS. ARC FIRE AND SHIELD NOW AVAILABLE." :
                    "THE CORE IS NEXT. SHOCKWAVE UNLOCKED. BREAK THE SHIELD RELAYS."),
                Error = startupError, ResultSaved = resultSaved, Assisted = RunState.Assisted, ResultMessage = leaderboardMessage,
                ResultText = "FINAL SCORE  " + RunState.Score + "\n\nTime  " + seconds / 60 + ":" + (seconds % 60).ToString("00") +
                    "   Deaths  " + RunState.Deaths + "   Cores  " + RunState.Shards + "/6\n" +
                    "Coins collected  " + RunState.CoinsCollected + "   Combat points  " + RunState.CombatPoints + "\n\n" +
                    "Action points  " + (RunState.Score - completionBonus - timeBonus - survivalBonus - coreBonus) + "\n" +
                    "Completion  +" + completionBonus + "   Time  +" + timeBonus + "\n" +
                    "Survival  +" + survivalBonus + "   Core integrity  +" + coreBonus
            };
        }
        private void OnDestroy()
        {
            if (Current == this) Current = null;
            SceneFlow.RestoreClock();
            if (Application.isPlaying) StageBuilder.Release();
        }
    }
}
