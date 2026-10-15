# SHATTERLINE

A Unity 2022.3.62f3 2.5D action platformer. Nova carries two signal cores through each of three districts, defends their upload terminals and defeats the final guardian before reaching the final exit. Gameplay, difficulty constants and existing artwork are retained from the uploaded project; this revision changes project authoring, UI, asset references, audio routing and animation authoring.

**Team:** Fill in real names and responsibilities before submission. Also edit `Assets/Documentation/ProjectCredits.asset` after import.

**Video:** Add your narrated 5–10 minute YouTube link. **Screenshots:** Add actual screenshots of the menu, three districts, uploads, boss and Editor hierarchy. No video, screenshots, teamwork evidence or completed Unity playtest is claimed by this archive.

## Open this revision

1. Extract into a NEW folder. Keep the working original as a backup. In Unity Hub, add the folder containing `Assets`, `Packages`, `ProjectSettings`.
2. Open with **Unity 2022.3.62f3**, with Windows Build Support installed. First import needs access to Unity's package registry for uGUI and Shader Graph 14.0.12; Ink is embedded locally.
3. Wait for package import, compilation and Ink compilation. On first import the Editor-only authoring utility creates and SAVES complete scenes, prefabs, meshes, animation clips/controllers, material assets, AudioMixer and configuration assets. This happens in Edit mode, before Play. If automatic preparation waits for a dirty scene, save it and choose **SHATTERLINE → Prepare Editor Project**.
4. Inspect `Assets/Scenes/MainMenu.unity` BEFORE pressing Play. The Canvas, buttons, panels, EventSystem, camera and audio sources must already exist in the hierarchy. Select a button: its OnClick has a persistent target and method. Move/resize/rename a UI element and save: the saved change is used by the game.
5. Inspect `RiftOne`, `RiftTwo`, `RiftCore`: world geometry, player, guards, hazards and linked prefab instances must already exist. `StageScene` holds explicit Inspector bindings. The runtime initializes existing objects and chooses variations; it does not generate the campaign geometry.
6. Run **SHATTERLINE → Validate Saved Scenes and Assets**. Resolve every error. Test Play from MainMenu, then make a NEW Windows build and complete it.
7. Commit the actual generated `.unity`, `.prefab`, `.anim`, `.controller`, `.mixer`, `.mat`, `.asset`, `.meta` files, `Packages` and the generated `ProjectSettings`. Do a clean clone test. An archive containing only authoring source is not a substitute for committing these generated assets.

**Verification Status:** Unity 2022.3.62f3 compilation, scene authoring, `ProjectValidation.Validate()` (all scenes passing with zero errors), and standalone Windows 64-bit build (`Builds/Shatterline.exe`) have been executed and verified. Before submission, conduct a full gameplay playthrough from the built executable or Editor, record your gameplay video, take screenshots, and fill in the team members list below.

`Prepare Editor Project` is a regeneration tool and overwrites generated campaign scenes. After editing scenes manually, save and commit them; use validation for routine checks instead of regenerating them.

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

Do not manufacture historical tasks/team roles or claim guaranteed 100% compliance. A previous 12-minute playthrough is useful context but does not validate this refactor.
