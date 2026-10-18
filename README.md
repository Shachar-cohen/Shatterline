# SHATTERLINE

A 2.5D action platformer built with **Unity 2022.3.62f3** (Built-in Render Pipeline). Nova carries two signal cores through each of three districts, defends their terminal upload stations against enemy waves, and silences the Guardian before reaching the final exit gate.

---

## 1. Project Information & Submission Checklist

- **Game Title:** SHATTERLINE
- **Course:** Unity Game Development Course Project
- **Engine Version:** Unity 2022.3.62f3 (Built-in Render Pipeline)
- **Target Platform:** Windows Standalone 64-bit
- **Build Executable (Release):** `Builds/Shatterline.exe`
- **Build Executable (Development):** `Builds/Dev/Shatterline_Dev.exe`
- **Automated Validation:** Unity Editor menu **SHATTERLINE → Validate Saved Scenes and Assets** (0 errors)
- **Component Test Scene:** `Assets/Scenes/Tests/MechanicLab.unity`

### Team Members & Contributions
- **Team Member 1:** Shachar Cohen — Gameplay Mechanics, Player Controller, Traversal Physics, Abilities
- **Team Member 2:** Shachar Levy — Level Design, Scene Authoring, Traps & Hazards, UI Canvas
- **Team Member 3:** Elad Abuganim — Enemy State Machines & Mechanics, Ink Narrative Integration, AudioMixer Routing
- **Team Member 4:** Tamir Ben Ovadia — Art Assets, Visual Effects, Audio Design & Quality Assurance

### Media & Submission Links
- **YouTube Walkthrough (5–10 min, narrated):** `[TODO: REPLACE BEFORE SUBMISSION - INSERT YOUTUBE LINK HERE]`\
  
---

## 2. How to Open, Run, and Test

### In Unity Editor:
1. Open the project root directory with **Unity 2022.3.62f3**.
2. Open `Assets/Scenes/MainMenu.unity` and click **Play**.
3. All four scenes (`MainMenu`, `RiftOne`, `RiftTwo`, `RiftCore`) are pre-authored with hierarchies, Inspector bindings, AudioMixer routing, and Canvas event listeners.
4. Run **SHATTERLINE → Validate Saved Scenes and Assets** to run the automated scene and asset validation suite (verifies 0 missing scripts, AudioMixer routing, button callbacks, and prefabs).

### Standalone Windows Executable:
1. Release build: launch `Builds/Shatterline.exe` directly.
2. Development build: launch `Builds/Dev/Shatterline_Dev.exe` (includes F1 cheat and debug overlay).
3. Both builds support full playthrough, local score persistence, and clean exit.

---

## 3. Game Objective and Win / Loss Conditions

### Objective:
- Transport **two cyan signal cores** per act to designated terminal upload stations.
- Place each core in the terminal and defend the station from dedicated attacker waves until upload reaches 100% and attackers are eliminated.
- In Act III (`RiftCore`), destroy the **three power relays** (around $x \approx 207, 213, 217$), defeat the **Guardian Boss** (near $x \approx 222$), cross the barrier, make the final Ink dialogue choice, and pass the exit gate at $x \ge 248$.

### Win Condition:
- Reaching the exit portal ($x \ge 248$) in Act III with both cores delivered and the Guardian Boss defeated. Triggers the victory sequence and records run performance to the local Top 10 leaderboard.

### Loss Condition:
- Losing all **3 lives** triggers the Game Over screen. Lives are lost when:
  1. Health drops to 0 from hazards, traps, or combat damage.
  2. Falling into the abyss ($y < -3.5\text{m}$) without catching a rescue ladder.
  3. Being overtaken by the collapsing rift ($x < \text{CollapseX}$).

---

## 4. Complete Control Scheme

The controls have been verified directly against the player controller (`Runner.cs`), companion drone (`DroneCompanion.cs`), run loop (`RunGame.cs`), and HUD view (`GameHud.cs`):

| Input | Action | Details |
|---|---|---|
| **A / D** or Left / Right Arrows | Move Left / Right | Horizontal run speed: 7.2 m/s (reduced by 25% while drone is active; 48% speed when crouching). |
| **W (Tap / Hold)** | Variable Jump | Tap for a short hop; hold for sustained height via upward velocity boost. |
| **W (Double Tap)** | Limited Double Jump | Tap W, release within 0.18s, and press again in midair for an additional leap. Holding the first press disqualifies double jump. |
| **S (Grounded)** | Crouch | Reduces collider height from 1.76m to 1.38m; clears low beams and crawl passages. |
| **S (Airborne)** | Fast Drop / Dive | Imparts immediate downward velocity ($-16\text{ m/s}$); clears aerial hazards and lands firmly. |
| **R (in Climb Zone)** | Climb Up | Ascends functional rescue ladders. |
| **Down Arrow (in Climb Zone)** | Climb Down | Descends functional rescue ladders onto lower platforms. |
| **Space** | Rift Dash | Dashes in the direction of movement (locked at activation). 2.1s cooldown, 0.22s duration; breaks weak walls; emits 15m sound radius. |
| **Left Mouse Button** | Blaster Fire | Fires aimed projectile toward cursor. 0.23s cooldown, no energy cost, 11m sound radius. |
| **Right Mouse Button** | Arc Volley *(Act II+)* | Consumes **35 energy**. Fires 3 spread shots (center: 3 dmg, $\pm 13^\circ$: 2 dmg). 15m sound radius. |
| **Q** | Phase Shield *(Act II+)* | Consumes **30 energy**. Grants **2.4s** of damage immunity (< 99 dmg). **8s** recharge cooldown. |
| **E** | Shockwave *(Act III+)* | Consumes **60 energy**. Radial blast (4.2m radius) dealing 3 damage to enemies, 2 to relays, breaking walls and disarming mines. |
| **F** | Toggle Defense Drone | Deploys / recalls companion drone. When active: applies **-25% movement penalty**, automatically intercepts approaching unobstructed shots for **25 energy** with an **8s** recharge cooldown. Hidden/inactive by default. |
| **Shift** | Special Power | Activates purchased one-time power (EMP, Aegis, or Field Repair). |
| **T** | Unstuck Command | Recovers player if trapped by collision geometry for $> 0.75\text{s}$. |
| **Esc** | Pause / Resume | Toggles pause menu, halts game clock (`Time.timeScale = 0`), pauses AudioListener. |
| **Enter** or Click | Advance Dialogue | Advances active Ink story transmission when no choice buttons are pending. |
| **F1** | Cheat Console | *(Unity Editor & Development Build only)* Toggles development cheat tools and live AI debugger. |

---

## 5. Core Delivery and Upload Defense Mechanics

- **Core Carry:** Picking up a cyan core attaches it to Nova's backpack. If spotted by enemy scouts while carrying a core, carrier heat accumulates (+20% per sighting, up to 100%), drawing reinforcements. Breaking line of sight allows heat to dissipate.
- **Terminal Upload:** Approaching a terminal station initiates upload placement. The terminal displays real-time upload progress (0–100%) and station integrity (100 HP).
- **Attacker Waves:** Dedicated enemy squads spawn from both flanks to assault the uploading terminal. Incoming hostile shots inflict 13 HP (Act 1), 16 HP (Act 2), or 12 HP (Act 3) damage to the station.
- **Stabilization & Recovery:** In Act III, terminals gain a 1.8s shield stagger window after being hit, and defeating each dedicated attacker restores +10 station HP.
- **Completion:** The upload completes only when progress reaches 100% **and** all active dedicated attackers are eliminated. Delivering both cores restores the network and unlocks exit gate progression.

---

## 6. Sentinel AI: 10-State Custom Finite State Machine

The enemies in Shatterline use a custom 10-state Finite State Machine implemented in `Sentinel.cs` with sensory perception, line-of-sight checks, and team coordination:

1. **Patrol:** Standard territorial pacing between authored boundaries (`PatrolMin` to `PatrolMax`).
2. **Investigate:** Alerts to auditory cues (dash, blaster fire, arc volley) and inspects the noise origin.
3. **Search:** Systematically sweeps the area around the player's last confirmed position for $5\text{s} + \text{Pressure}$.
4. **Attack:** Direct combat engagement. Performs line-of-sight raycasts and leads aim toward player trajectory.
5. **Flank:** Dispatched along an offset route to encircle the player from the opposite flank.
6. **Sweep:** Coordinated tactical search pattern assigned by `GuardDirector`.
7. **Overwatch:** Anchors to high ground or defensive column to maintain field of fire.
8. **Cover:** Wounded sentinels seek physical shelter behind structural columns.
9. **Retreat:** Severely damaged sentinels fall back within their safe platform zone.
10. **Climb:** Sentinels navigate vertical ladders (`ClimbZone`) to pursue across elevated tiers without passing through walls.

*Guardian Boss:* In Act III, the Guardian executes dedicated Boss Phases (Phase 1 and Phase 2), tracking player dashes with predictive aim while shielded by three external power relays.

---

## 7. GuardDirector Squad Coordination

The `GuardDirector` (`GuardDirector.cs`) orchestrates non-boss Sentinels without giving them omniscient player awareness:
- **Sound Propagation:** Footsteps, dashes (15m), and blaster shots (11m) radiate sound. Concrete columns attenuate sound radius by 45%.
- **Squad Radio:** A guard spotting Nova alerts nearby allies with the observed position, dividing respondents into `SWEEP` and `OVERWATCH` roles.
- **Squad Pressure:** Alert level scales from 0 to 3 based on repeated sightings, tightening search duration and firing cadence by up to 15%. Alert decays after 12 seconds of player concealment.
- **Island Boundaries:** Restricts ground enemies to their assigned platform intervals (`safeIsland.x` to `safeIsland.y`), preventing suicidal falls off floating ledges.

---

## 8. Dynamic Difficulty: AdaptiveDirector

The `AdaptiveDirector` (`AdaptiveDirector.cs`) monitors player traversal at four fixed survey lines ($x = 20, 90, 135, 203\text{m}$) and applies bounded adjustments to upcoming hazards:
- **RECOVERY:** Triggered when the player suffered deaths, took $\ge 2$ damage hits, or has HP $< 38$. Grants $+12$ adaptive health or eases upcoming gear/vent timing.
- **UPPER ROUTE:** Triggered when the player spent over 32% of route time in elevated sections with $\ge 2$ jumps. Accelerates upper gear cycles.
- **FAST ROUTE:** Triggered by high traversal speed ($> 5.8\text{ m/s}$) or frequent dashing ($\ge 2$ dashes). Causes upcoming steam vents to close sooner or moving gears to quicken.
- **STANDARD:** Applied during balanced traversal.

The director never removes mandatory level objectives or bypasses core delivery requirements.

---

## 9. Collapsing Rift Mechanics

- An advancing rift wall (`CollapseX`) continuously moves forward from $-14\text{m}$ at a rate determined by `RiftPace`.
- Contact with the rift instantly defeats the player (`Hurt(99, "THE RIFT CAUGHT YOU")`).
- **Boss Fight Protection:** In Act III, the rift automatically halts at $x \le 194\text{m}$ while the Guardian boss is alive, resuming only after the boss is defeated.
- **Anti-Stranding Recovery:** If an uncollected cyan core is left behind the advancing player or rift, it is safely repositioned to the exit cache at $x \ge 241\text{m}$.

---

## 10. Coin Economy and One-Time Power Selection

- Gold coins are scattered throughout districts and awarded for defeating enemies and completing directives.
- Reaching **8 coins** unlocks a one-time special power selection popup:
  1. **EMP Blast:** 5.4m spherical blast dealing 4 damage to enemies, 3 damage to relays, breaking walls, and disarming mines.
  2. **Aegis Shield:** 5 seconds of complete invulnerability to all damage.
  3. **Field Repair:** Restores 45 HP immediately (cannot be activated if HP is already 100).
- Selected powers are stored in `RunState.Special` and triggered on demand with **Shift**.
- Coin remainder and stored powers persist across act transitions.

---

## 11. External Framework Integration: Ink Narrative Engine

- **Package:** Official Inkle Ink integration (`com.inkle.ink-unity-integration`, MIT License).
- **Integration Architecture:** `Assets/Narrative/Signal.ink` is compiled into story JSON and loaded at runtime by `RunGame.cs`.
- **Gameplay Impact:**
  - `tactic`: Choice between suit overclock (dash speed multiplier 1.28x) or stabilization (+35 HP).
  - `directive`: Three act-specific mission choices altering rift advance pace, enemy fire rates, coin rewards, and weapon damage.
  - `intel`: Narrative variable accumulated across choices, governing ending dialogue variations.
- **Robust Null-Story Fallback:** If the Ink asset is unavailable or fails to compile (`inkStory == null`), the exit gate automatically opens based on core delivery and boss defeat alone, preventing player progression lock.

---

## 12. Enemy AI Architecture: Custom FSM vs. Unity NavMesh

- **Design Decision:** Unity's built-in 3D NavMesh was deliberately rejected in favor of a custom 10-state 2.5D Finite State Machine.
- **Rationale:** Shatterline features moving kinematic platforms, distinct discontinuous rooftop islands, vertical climb zones, and side-scrolling physics. Standard NavMesh agents struggle with 2.5D vertical traversal constraints, falling off floating ledges and mishandling moving platform coordinate spaces. The custom state machine provides deterministic, performant, and safe platform navigation.

---

## 13. ProjectilePool: Garbage-Free Memory Management

- `ProjectilePool.cs` pre-allocates **32 projectile GameObjects** in `PB_ProjectilePool.prefab`.
- Each pooled projectile includes a `TrailRenderer`, `SphereCollider`, and kinematic `Rigidbody`.
- Reused dynamically for both player shots (cyan material) and enemy fire (orange-red material).
- Completely eliminates heap allocation and garbage collection spikes during high-intensity combat.

---

## 14. Developer Console and Cheat System

- **Hotkey:** Press **F1** to toggle the Developer Console.
- **Features:**
  - Real-time display of Run Seed and active game state.
  - Invulnerability toggle (`ON / OFF`).
  - Give 2 fragments (instantly delivers cores).
  - Warp to checkpoint ($x = 95\text{m}$) or exit ($x = 247\text{m}$).
  - Instant boss defeat (Act III).
  - Start next act shortcut.
  - Live AI state counts (`Patrol`, `Combat`, `Climb`, `Sweep`, `Watch`, `Search`) and `AdaptiveDirector` decisions.
- **Integrity & Restrictions:**
  - Wrapped entirely in `#if UNITY_EDITOR || DEVELOPMENT_BUILD`.
  - Excluded from release builds (`Builds/Shatterline.exe`).
  - Activating any cheat immediately sets `RunState.Assisted = true`, permanently disabling local leaderboard score submission.

---

## 15. Verified Unity Systems

- **Audio Mixer:** `AM_Shatterline.mixer` routes through Master → SFX and UI groups. Exposes `MasterVolume`, `SfxVolume`, and `UiVolume` logarithmic decibel parameters bound to Settings sliders.
- **Animator Controllers:** Two controllers (`AC_Courier.controller` and `AC_Sentinel.controller`) with authored animation clips (Idle, Running, Airborne, Dashing, Engaged) driving sprite rigs and armor fins.
- **Scene Flow:** `SceneFlow.cs` orchestrates ordered transitions between `MainMenu`, `RiftOne`, `RiftTwo`, and `RiftCore`, with time scale and cursor restoration.
- **ScriptableObjects:** `ProjectCredits.cs` exposes configurable credits, attributions, and disclosures in `Assets/Documentation/ProjectCredits.asset`.
- **Canvas Scaler:** Scaled with screen size (Reference: $1280 \times 720$, Screen Match Mode: **Expand**), guaranteeing all UI panels remain fully visible and unclipped on 16:9, 16:10, 21:9, and 4:3 displays.

---

## 16. Asset Credits, Attributions, & Disclosures

- **Development:** Designed, programmed, reviewed, and playtested by the team.
- **Artwork & Textures:** Original backdrop and courier artwork, procedural textures, and 5 synthesized sound effects.
- **Ink Integration:** Ink by inkle (MIT License, included in `Packages/com.inkle.ink-unity-integration`).
- **Shader Graph:** `SG_GridPulse` shader graph serialization adapted from Unity Graphics tests (Unity Companion License, included beside the graph in `Assets/Art/Shaders/`).

---

## 17. Build Settings Scene Order

The project Build Settings are configured in the required order:
1. `Assets/Scenes/MainMenu.unity` (Index 0)
2. `Assets/Scenes/RiftOne.unity` (Index 1)
3. `Assets/Scenes/RiftTwo.unity` (Index 2)
4. `Assets/Scenes/RiftCore.unity` (Index 3)
