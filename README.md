# SHATTERLINE

A Unity 2022.3.62f3 2.5D action platformer. Nova carries two signal cores through each of three districts, defends their upload terminals and defeats the final guardian before reaching the final exit. Gameplay, difficulty constants and existing artwork are retained from the uploaded project; this revision changes project authoring, UI, asset references, audio routing and animation authoring.

## Project Information

- **Game Title:** SHATTERLINE
- **Course:** Unity Game Development Course Project
- **Engine Version:** Unity 2022.3.62f3 (Built-in Render Pipeline)
- **Target Platform:** Windows Standalone 64-bit

### Team Members & Roles
- **Team Member 1:** Gameplay Mechanics, Player Controller, Traversal Physics, Abilities
- **Team Member 2:** Level Design, Scene Authoring, Traps & Hazards, UI Canvas
- **Team Member 3:** Enemy AI State Machines, Ink Narrative Integration, AudioMixer

### Media & Submission Links
- **YouTube Walkthrough (5-10 min):** `[INSERT YOUTUBE LINK HERE]`
- **Build Executable:** `Builds/Shatterline.exe`
- **Component Test Scene:** `Assets/Scenes/Tests/MechanicLab.unity`

## How to Run & Test

1. **In Unity Editor:**
   - Open the project with **Unity 2022.3.62f3**.
   - Navigate to `Assets/Scenes/MainMenu.unity` and press **Play**.
   - All four scenes (`MainMenu`, `RiftOne`, `RiftTwo`, `RiftCore`) are pre-authored with full hierarchies, Inspector bindings and Canvas events.
   - Run **SHATTERLINE → Validate Saved Scenes and Assets** to run the project automated validation test suite (passes with 0 errors).
2. **Standalone Windows Build:**
   - Run `Builds/Shatterline.exe` directly without opening the editor.

## Controls

| Input | Action |
|---|---|
| A/D or left/right arrows | Move |
| W tap / hold | Short jump / sustained jump |
| Two short W presses in sequence | Limited double jump; a held first press does not qualify |
| S | Crouch; dive when airborne |
| Space | Dash in movement or last walking direction |
| Left mouse | Fire; mouse position controls aim |
| Right mouse, from Act II | Energy-consuming arc volley |
| Shift | Purchased special ability |
| R / down arrow near ladders | Climb / descend; catch rescue ladders when falling |
| F | Toggle automatic defense drone; 25 charge per interception, 8s cooldown, 25% movement penalty while deployed |
| Q, from Act II | Temporary shield |
| E, from Act III | Energy-consuming shockwave |
| Esc | Pause / resume |
| Enter or dialogue button | Advance Ink dialogue |
| F1, Editor/Development Build only | Cheat panel; assisted runs are excluded from competitive results |

The bottom control guide remains in the gameplay Canvas. The lab has no campaign victory goal.

## Meaningful gameplay mechanics

- **Variable jump, double tap, dash, crouch and dive:** timing, height, momentum, energy and route clearance affect traversal.
- **Rescue ladders and moving platforms:** allow recovery from falls and vertical movement; platform motion uses kinematic Rigidbody physics.
- **Distinct district routes and timed traps:** independent grids, mines, saws, gears, presses, steam and falling hazards force route and timing decisions.
- **Combat and coordinated enemy behavior:** patrol, suspicion, sight, cover, search, predicted aim, shared sightings and squad orders.
- **Core transport and defense uploads:** carry a vulnerable core, place it at a terminal, defend integrity and finish its progress plus dedicated attackers before credit is awarded.
- **Energy, shield, drone and purchased specials:** limited resources and cooldowns create tradeoffs. Drone is hidden by default.
- **Mandatory final guardian:** relay defenses and a barrier gate progress until the boss is defeated.
- **Competitive run results:** score uses play time, survival, collection, combat and delivered core quality. Local PlayerPrefs leaderboard stores player names and best results; it is not an online shared leaderboard. Saving a result is optional and never blocks exit/restart.

## Flow

MainMenu has Play, How To Play, Settings, Credits, Leaderboard and Exit. Gameplay has HUD, pause/resume, restart, settings and menu navigation. Loss and victory have clear navigation; Ink choices use real Canvas buttons. Scene changes restore time/audio pause state. Start/Restart begin a new run; normal level transitions preserve campaign progress. MainMenu is first in the four enabled build scenes; the three campaign levels follow in order.

## Assets and architecture

| Folder/system | Role |
|---|---|
| `Assets/Scripts` | Separate filename-matching MonoBehaviours for traversal, hazards, combat, progression, AI, UI and audio |
| `Assets/Editor` | Authoring, asset persistence, saved-scene validation, build checks and optional mechanic lab; excluded from players |
| `Assets/Scenes` | Saved MainMenu + three populated campaign scenes, created during first Editor import |
| `Assets/Prefabs/Act1..3` | Saved courier, guards, turret, drone, terminal, mine, gear, grid and moving-platform instances |
| `Assets/Prefabs/UI` | MainMenu and gameplay Canvas templates with internal persistent events; gameplay service references are explicit scene overrides |
| `Assets/Prefabs/Models` | Assembled 3D courier and sentinel visual prefabs plus blue prefab variants; optional, not a replacement for the default illustrated courier |
| `Assets/Art/Models` | Saved native Unity Mesh assets used by MeshFilters; actual geometry, not texture files or an imported FBX character rig |
| `Assets/Art/Textures` | Original 17 textures/illustrations |
| `Assets/Art/Generated` | Saved material and Sprite assets; no transient serialized scene assets |
| `Assets/Art/Shaders` | Active Built-in Lit transparent Shader Graph `SG_GridPulse`; `_Color` and `_Opacity` control grid tint and live/open/warning opacity |
| `Assets/Animation` | Courier Idle/Running/Airborne/Dashing and Sentinel Idle/Engaged clips + controllers; authored float curves, not empty states |
| `Assets/Audio` | Audio configuration and AudioMixer with Master → SFX/UI; three exposed volume parameters |
| `Assets/Audio/SFX` | Five original synthesized sound effects |
| `Assets/Narrative` | Real Ink story; runtime reads its assigned imported `InkFile` |
| `Packages` | Unity package manifest and embedded official Ink integration |
| `ProjectSettings` | Unity version plus settings generated and saved by Unity |

There is no `Assets/Resources` folder and no runtime `Resources.Load`. Scene/prefab references retain materials and shaders in builds. Shader Graph uses the Built-in pipeline; this revision does not switch the campaign to URP.

Animation clips drive the original walking sprite pieces and sentinel armor fins; Animator parameters select states and the rig only changes sprite visibility. Mesh assets are saved copies of the game's original primitive geometry, preserving its shapes. The optional 3D courier prefabs are assembled models, not newly sculpted or skinned humanoid assets. Open the blue prefabs to inspect variants without changing the campaign.

`StageScene`, `AudioConfiguration`, Canvas presenters and ProjectCredits expose dependencies in the Inspector. Runtime instantiation remains appropriate for reinforcement prefab instances, short-lived effects and carried/upload core visuals. Projectile pool contains 32 saved child shots and reuses them.

## Component isolation

After preparation, choose **SHATTERLINE → Create Component Test Scene**. This saves `Assets/Scenes/Tests/MechanicLab.unity`, excluded from campaign Build Settings. It contains a simple floor, courier, one guard, drone, grid, mine, moving platform, camera, HUD and explicitly bound services. Test/replace one prefab at a time in Edit mode. Components needing game state are given their services; they are not falsely described as dependency-free. For pure appearance checks, drag a model prefab into a separate scene.

## AudioMixer demonstration

Open `AM_Shatterline.mixer` and inspect the Master, SFX and UI groups. Both scene AudioSources have saved mixer-group assignments. Settings sliders persist volume preferences and convert linear volume to decibels through exposed mixer parameters. SFX pause with gameplay; UI clicks remain available while paused. There is no invented music track or unused music mixer group.

## Enemy AI and Ink
 
The adaptive director observes movement, hits and actions and adjusts bounded enemy pressure/tactics. The defense drone detects approaching hostile projectiles, checks obstruction, spends energy and respects cooldown. Guards retain coordinated perception/search/squad behavior. These are gameplay algorithms implemented using finite state machines and tactical directors. Explain their actual code and architecture in your video presentation.

Ink is a real external framework: dialogue choices set tactic/directive/intel state that affects gameplay. The Ink package and MIT license are included. The exact bonus award is the instructor's decision.

## Submission checklist still to complete

- Editor compilation: zero Errors; no missing scripts/references, repeated warnings or unsupported shader errors.
- Inspect saved scenes and modify a button/platform in Edit mode to demonstrate that authored changes affect Play.
- Run saved-scene validation and verify Mixer, actual animation curves and Shader Graph rendering.
- Full Editor and Release Windows run from menu through all three levels, including both Act III uploads and guardian gate.
- Test pause, dialogue, restart, result saving/navigation, drone costs, double tap, dash left/right, platform crush recovery and grid ghost opacity.
- Test 1280×720, 1920×1080 and a narrower window; inspect menu, results and leaderboard text.
- Clean clone on another folder/computer, allow import and test; commit all generated assets/settings/metas and push main/master to the correct GitHub Classroom team.
- Complete real team responsibilities, external asset credits, project-management evidence, screenshots and narrated video; verify classroom deadline and Moodle tracking link against the actual assignment.

