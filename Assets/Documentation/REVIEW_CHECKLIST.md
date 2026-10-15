# Team QA and rubric sign-off

Record reviewer name, date, Unity version, Windows build result and measured gameplay duration in your actual project tracker.

- [ ] MainMenu: Start, instructions, settings, credits and exit tested.
- [ ] First scene generated/imported without errors; scenes in Build Settings.
- [ ] All three acts, fragments, exits and win condition tested with no cheat.
- [ ] Jump buffer, coyote time, double jump, dash and barrier tested.
- [ ] W jumps twice; S + A/D passes each low ceiling and releases automatically after clearing it; Space dashes/breaks walls; R climbs ladders and Down Arrow descends.
- [ ] Collect eight gold coins, choose EMP/Aegis/Repair with mouse, activate each via Shift once; ensure coin remainder and unused power persist after changing acts.
- [ ] Verify EMP affects nearby enemies, mines and barriers; Aegis protects for five seconds; Repair grants 45 HP only when injured. Test a second eight-coin choice after spending a power.
- [ ] Enemy patrol/investigate/attack/retreat AI, projectiles, hit invulnerability, checkpoint and game over tested.
- [ ] Fast scouts use alternative spawn pockets in the later acts; yellow eyes show the warning when a reaction delay exists, and prompt shots remain avoidable through movement and cover.
- [ ] Newly alerted guards have varied reaction delays: some may fire promptly when cooldown allows, while others pause; no off-camera or obstructed shot occurs, and encounters stay fair.
- [ ] A guard fires from across the visible camera when sight is open, but not while offscreen or behind a solid low beam/wall; shots can reach the far edge of the camera.
- [ ] Act II/III ladder-pocket guard: dash or shoot nearby, hide behind a column, and observe searching behavior without text above its head; reach an elevated column to check CLIMB and confirm guards do not pass through walls or cross unsupported gaps.
- [ ] Two nearby guards share a last-seen position and can flank; a wounded guard finds reachable cover or retreats; test that hiding breaks sight and the radio does not track the player through walls. F1 shows AI state counts and the last alert.
- [ ] Guardian in phase two leads shots during a dash but remains dodgeable. Validate easy/difficult Ink guard-fire interval choices and both Editor and Windows build behavior.
- [ ] S shows the distinct bent-knee sprite without scaling the whole body, fits under each raised low beam, and drives a fast downward drop when pressed midair.
- [ ] Contact rotating saws on the elevated and airborne routes, lose 22 HP at most once per damage cooldown, and confirm ordinary lower routes remain traversable.
- [ ] Act I teaches three parkour columns and simpler hazards; Act II has four columns, first-gap moving lift, five timed beams, two presses and two destructible orange ceiling sentries; Act III uses different ground islands, raised core tiers, more saws/presses, three ceiling sentries and final relays. Verify no mandatory jump or moving platform traps the player.
- [ ] Move past guards and confirm there are no ATTACK/INVESTIGATE/SEARCH labels above them during normal play; warning eyes and the F1 developer panel still work.
- [ ] Start new runs and lose lives in Acts II/III: regular guard pockets, ceiling sentry sockets and uncollected fragments change while checkpoint respawn remains safe.
- [ ] Clear all three acts with ordinary controls and no invulnerability; tune guard count or fire interval if sustained unavoidable damage occurs.
- [ ] Mouse aim, right-click volley, charge gained by kills, drone ambush, laser timing and three boss relays tested.
- [ ] Painted background and illustrated character visible in all acts, no unexpected occlusion.
- [ ] Third-act finale: destroy relays around x≈207/213/217, defeat the large boss near x≈222, cross the physical barrier near x≈228, deliver both cores, choose Ink decisions, then reach x≈250 and win; test missing-objective hints and safe return.
- [ ] HP bar loses 15–28 for normal hits; shield Q blocks hits; shockwave E destroys nearby relay; Arc volley right-click costs charge.
- [ ] Bullet trails, muzzle flash, impact particles and original blaster sound render and play at target resolutions.
- [ ] Ink opening, first checkpoint speed/repair choice, second checkpoint's three act-specific directives, gate fallback, and dialogue Continue/Enter verified in Unity.
- [ ] Verify each act's decision effects on rift speed, guard shot intervals, coins, HP, blaster/relay damage; dying retains that act's decision, entering a new act offers its own decision, and collected records change the ending dialogue.
- [ ] Skip the first green checkpoint, collect both fragments, reach the Act I gate, choose an upgrade there, press CONTINUE and enter Act II.
- [ ] Full unassisted playthrough measured at 5–15 minutes; extend level content if shorter.
- [ ] F1 console works in Editor/Development Build and is absent in production.
- [ ] Animations for hero and sentinel are visible and controllers generated.
- [ ] All textures, including CourierCrouch, and five audio assets import; no references missing.
- [ ] Windows standalone build launches cleanly and quits normally.
- [ ] Team contributions, screenshots and a narrated 5–10 minute demo added.
- [ ] Source pushed to assigned GitHub Classroom main/master; Moodle has repository URL.

No item is marked complete merely because source files exist. Mark each after observing the result in Unity.

- [ ] Defuse proximity mines by shooting; approach one to confirm timed warning/explosion, evade a falling rock in Act III, test timed beams in Act II.
- [ ] Measure complete playtime with normal controls and normal win condition; verify 5–15 minutes before grading.

- [ ] Between-act transition card shows for about two seconds, correctly pauses and loads the next act.

- [ ] At low and high display resolutions, only solid platforms have pale tops, neon lips and corner markers; the distant panorama does not resemble traversable terrain.
- [ ] No decorative bright-capped pylons remain. At x≈44–57, jump onto all three solid columns in Act I and all four in Acts II/III, clear the tall gate and land on the far platform; verify no wall contact refills jumps.
- [ ] Cross the mandatory ladder gate at x≈77.5 with R, descend onto the real steps, crouch under x≈101, and cross the x≈151–156 gap below the saw in Act II. In Act III, crouch or fast-drop at the saw near x≈63.
- [ ] Fall fully below the map three times without cheats: each fall consumes one life and respawns at the checkpoint; after the third, the game-over panel appears. Falling into a recoverable pocket between columns allows climbing without a loss.
- [ ] Moving platform markings remain aligned while the collision surface moves.
- [ ] Across all acts and several seeds, collect the first and second cyan signals from each act-specific set of three authored sites; verify none intersects a platform or compulsory hazard.
- [ ] In Acts I–III climb the marked gate ladder at x≈77.5 using R; in Acts II–III also climb the second at x≈128.5. Verify a ground double jump cannot bypass these ladders and both descending routes are safe.
- [ ] Fall into both parkour pockets around x≈44–52; use only the recovery ladders needed to escape, with no trapped landing.
- [ ] Miss an early cyan signal, pass x≈170, and verify it reappears near x≈191 before the exit. A late signal at x≈188.5 remains reachable near the exit; no required pickup should be stranded behind the rift. The green healing pickup must not count.
- [ ] Read the persistent objective HUD at each stage: 0/2, 1/2, cache, 2/2, upgrade and Act III boss requirements.
- [ ] Restart the run multiple times: both required signals, guard spawn points, offset upper platforms and district-specific shortcuts vary while gate ladders, ground and checkpoints remain passable.
- [ ] Lose a life after each checkpoint: surviving and defeated regular guards return at different safe positions, and uncollected cyan signals move; collected signal count stays intact.
- [ ] Break line of sight behind cover: guards investigate the last seen position briefly before patrolling; compare the three acts and confirm no unavoidable guard overlap on checkpoint respawn.

## Tactical squad AI sign-off

- [ ] Two allied guards receive one real sighting and split into SWEEP and OVERWATCH after Nova breaks line of sight; F1 shows the roles and orders.
- [ ] The upper-route sighting prioritizes a nearby flying scout, but no guard shoots through solid cover or while off-camera.
- [ ] Repeated sightings raise alert to at most 3, firing cadence changes by at most 15%, and alert falls after hiding for 12 seconds.
- [ ] Test with and without a carried signal; ensure predicted interception is limited to observed velocity and stops updating after the player hides.
- [ ] Cross a gap or mandatory gate while pursued and confirm ground guards stay on their safe island and only climb functional nearby ladders.
- [ ] Record a short Editor/Development Build demonstration and measure overall game difficulty without cheats.

## Adaptive AI and drone review

- [ ] Cross x≈20, 90, 135 and 203 in each act; F1 records a reasoned encounter choice that changes only upcoming gear/vent behavior.
- [ ] Compare fast, elevated and damaged play. Observe visible timing differences and confirm recovery never makes all hazards disappear or changes mandatory objectives.
- [ ] Drone is invisible and inactive by default; F deploys/recalls it. While deployed, it spends exactly 25 charge to stop one approaching unobstructed hostile shot.
- [ ] Verify eight-second defense cooldown survives recall/redeploy; solid cover or an active player shield causes no charge loss; insufficient energy leaves the projectile intact.
- [ ] Compare 25% slower running, crouch movement, dash and ladder climb while deployed with normal speed after recall. Check drone following, HUD readability, energy interactions with Q/Arc/E, and all three acts in Unity Editor and Windows player.

## Windows build-specific sign-off

- [ ] In Act III, test both terminal uploads: four then five dedicated attackers total, no more than two active at once, 4.5-second spawn spacing, 12 integrity damage per unprotected hit, and 1.8-second SHIELD after a hit. Defeating each dedicated attacker restores 10 integrity; reaching 100% alone does not complete the upload until all have been defeated.

- [ ] Build check accepts only the four ordered scenes and finds StageStandard, compiled Signal.ink, both animation controllers and character/background textures.
- [ ] Start a non-development Windows EXE, open the Ink introduction, deploy Aegis/Q: the thin shield outline surrounds the character without hiding it.
- [ ] At LOW and HIGH grids, OPEN shows translucent, gently pulsing bars with no collision, CLOSE flashes brightly, and LIVE shows prominent bars with active collision. Repeat after changing acts in the Windows EXE.
- [ ] Collect a core, observe the backpack seal, finish and fail a terminal upload, defeat the large Act III boss and cross its barrier. Verify no pink materials or missing textures.
- [ ] Finish a normal run, save a score, return to menu and relaunch the EXE to verify the local leaderboard persists.

## Double-tap and varied guard reaction

- [ ] Quick W press, release within 0.18 s, and second press within the tap window produces exactly one slightly higher air jump.
- [ ] Hold first W longer than 0.18 s, release and press again in midair: no double jump; ordinary held jump still gains its height.
- [ ] A late second press and pressing W repeatedly after the second jump do not create an extra jump; landing replenishes the normal sequence.
- [ ] Observe multiple guard re-acquisitions across seeds: prompt, medium and slow reactions occur without bypassing shot cooldown, walls or camera view.
