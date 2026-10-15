using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Shatterline;
using AudioConfiguration = Shatterline.AudioConfiguration;
using Object = UnityEngine.Object;

// Only this Edit-mode utility builds the fixed world. Player builds have no map generator.
[InitializeOnLoad]
public static class ShatterlineSetup
{
    private const string SceneRoot = "Assets/Scenes/";
    private const string Marker = "Assets/Documentation/EDITOR_LAYOUT_VERSION.txt";
    private const string Version = "editor-layout-3";
    private static bool preparing;
    static ShatterlineSetup() { EditorApplication.delayCall += Ensure; }
    private static void Ensure()
    {
        if (preparing || EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        { EditorApplication.delayCall += Ensure; return; }
        if (File.Exists(Marker) && File.ReadAllText(Marker).Trim() == Version &&
            new[] { "MainMenu", "RiftOne", "RiftTwo", "RiftCore" }.All(n => File.Exists(SceneRoot + n + ".unity"))) return;
        if (EditorSceneManager.GetActiveScene().isDirty)
        {
            Debug.LogWarning("SHATTERLINE: save your open scene, then choose SHATTERLINE > Prepare Editor Project.");
            return;
        }
        Prepare();
    }
    [MenuItem("SHATTERLINE/Prepare Editor Project")]
    public static void Prepare()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || preparing) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        preparing = true;
        int previousSeed = RunState.RunSeed;
        try
        {
            Directory.CreateDirectory(SceneRoot);
            Directory.CreateDirectory("Assets/Documentation");
            AssetDatabase.Refresh();
            Directory.CreateDirectory("Assets/Prefabs");
            EnsureRuntimeMaterial(); CreateAnimations();
            var audio = AudioAuthoring.EnsureAssets();
            var credits = AssetDatabase.LoadAssetAtPath<ProjectCredits>("Assets/Documentation/ProjectCredits.asset");
            if (credits == null)
            {
                credits = ScriptableObject.CreateInstance<ProjectCredits>();
                AssetDatabase.CreateAsset(credits, "Assets/Documentation/ProjectCredits.asset");
            }
            CreateMenu(audio, credits);
            for (int act = 1; act <= 3; act++) CreateStage(act, audio);
            SetBuildSettings(); AssetDatabase.SaveAssets();
            File.WriteAllText(Marker, Version + "\n"); AssetDatabase.Refresh();
            EditorSceneManager.OpenScene(SceneRoot + "MainMenu.unity");
            Debug.Log("SHATTERLINE: populated scenes, linked prefabs, Canvas controls and AudioMixer saved. Inspect them before Play; commit Assets and ProjectSettings after import.");
        }
        catch (System.Exception error)
        {
            Debug.LogException(error);
            Debug.LogError("SHATTERLINE authoring stopped. Resolve the reported error and run Prepare Editor Project again. Do not build a partial project.");
            throw;
        }
        finally { RunState.RunSeed = previousSeed; preparing = false; }
    }
    [MenuItem("SHATTERLINE/Rebuild scenes and animation")]
    public static void Rebuild() { Prepare(); }
    [MenuItem("SHATTERLINE/Build Windows Standalone")]
    public static void BuildWindowsPlayer()
    {
        string buildDir = "Builds";
        Directory.CreateDirectory(buildDir);
        string exePath = Path.Combine(buildDir, "Shatterline.exe");
        string[] scenes = {
            SceneRoot + "MainMenu.unity",
            SceneRoot + "RiftOne.unity",
            SceneRoot + "RiftTwo.unity",
            SceneRoot + "RiftCore.unity"
        };
        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = scenes,
            locationPathName = exePath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };
        var report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            throw new InvalidOperationException("Build failed with result: " + report.summary.result);
        Debug.Log("SHATTERLINE build succeeded: " + exePath);
    }
    private static void CreateMenu(AudioConfiguration audio, ProjectCredits credits)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var root = new GameObject("SHATTERLINE / Main Menu"); var menu = root.AddComponent<MainMenu>();
        var camera = new GameObject("Main menu camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
        camera.tag = "MainCamera"; camera.transform.position = new Vector3(0, 0, -18);
        camera.orthographic = true; camera.orthographicSize = 7;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.045f, .065f, .11f);
        RenderSettings.ambientLight = new Color(.68f, .68f, .78f);
        for (int i = 0; i < 18; i++)
        {
            var tower = GameObject.CreatePrimitive(PrimitiveType.Cube); tower.name = "Fractured skyline " + i;
            tower.transform.position = new Vector3(-13 + i * 1.7f, -6 + (i * 7 % 5) * .36f, 5 + i % 3);
            tower.transform.localScale = new Vector3(1.5f, 6 + (i * 7 % 5) * 2f, .65f);
            tower.GetComponent<Renderer>().sharedMaterial = StageAuthoring.Material(Color.Lerp(
                new Color(.15f, .22f, .29f), new Color(.30f, .23f, .34f), i / 18f), "stone");
            Object.DestroyImmediate(tower.GetComponent<Collider>());
        }
        var system = AudioAuthoring.AddToScene(audio); CanvasAuthoring.MainMenu(menu, system, credits);
        system.transform.SetParent(root.transform, false);
        Object.FindObjectOfType<Canvas>().transform.SetParent(root.transform, false);
        Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>().transform.SetParent(root.transform, false);
        SceneAssetWriter.PersistAssets(scene);
        SceneAssetWriter.SavePrefab(root, "Assets/Prefabs/UI/PB_MainMenu.prefab");
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene, SceneRoot + "MainMenu.unity");
    }
    private static void CreateStage(int act, AudioConfiguration audio)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var manager = new GameObject("SHATTERLINE / Act " + act); var game = manager.AddComponent<RunGame>();
        game.SetAuthoringLevel(act); RunState.RunSeed = 41987; // Stable preview; run-time sockets reroll with the actual run seed.
        StageAuthoring.Build(act, game);
        var bindings = manager.AddComponent<StageScene>(); bindings.Act = act; StageAuthoring.Capture(bindings);
        bindings.StageCamera.transform.position = new Vector3(bindings.Player.transform.position.x + 3f, 5.5f, -19f);
        bindings.StageCamera.transform.rotation = Quaternion.Euler(5f, 0f, 0f);
        var system = AudioAuthoring.AddToScene(audio);
        game.SetSceneBindings(bindings, system, AssetDatabase.LoadAssetAtPath<Ink.UnityIntegration.InkFile>("Assets/Narrative/Signal.ink"));
        var hud = CanvasAuthoring.Gameplay(game, system);
        foreach (var platform in Object.FindObjectsOfType<MovingPlatform>())
        {
            var body = platform.GetComponent<Rigidbody>(); body.isKinematic = true; body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.Interpolate; body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        }
        BuildEffectPrefabs(bindings);
        SceneAssetWriter.PersistAssets(scene);
        if (act == 1)
        {
            SceneAssetWriter.SaveVisualVariants(bindings.Player.transform.Find("Courier animation rig/Core"), "Courier3D");
            SceneAssetWriter.SaveVisualVariants(bindings.GuardPlans[0].Active.transform.Find("Animated sentinel/Core"), "SentinelModel");
        }
        SceneAssetWriter.SavePrefab(hud.gameObject, "Assets/Prefabs/UI/PB_Gameplay_Act" + act + ".prefab");
        string prefabs = "Assets/Prefabs/Act" + act + "/";
        SceneAssetWriter.SavePrefab(bindings.Player.gameObject, prefabs + "PB_Courier.prefab");
        SceneAssetWriter.SavePrefab(bindings.Projectiles.gameObject, prefabs + "PB_ProjectilePool.prefab");
        SceneAssetWriter.SavePrefab(bindings.Drone.gameObject, prefabs + "PB_Drone.prefab");
        int index = 0;
        foreach (var plan in bindings.GuardPlans)
        {
            var prefab = SceneAssetWriter.SavePrefab(plan.Active.gameObject, prefabs + "PB_Sentinel_" + index++ + ".prefab");
            if (!plan.Boss && bindings.SentinelPrefab == null) bindings.SentinelPrefab = prefab.GetComponent<Sentinel>();
        }
        index = 0;
        foreach (var plan in bindings.TurretPlans)
        {
            var prefab = SceneAssetWriter.SavePrefab(plan.Active.gameObject, prefabs + "PB_Turret_" + index++ + ".prefab");
            if (bindings.TurretPrefab == null) bindings.TurretPrefab = prefab.GetComponent<SentryTurret>();
        }
        index = 0;
        foreach (var station in Object.FindObjectsOfType<SignalStation>())
            SceneAssetWriter.SavePrefab(station.gameObject, prefabs + "PB_Terminal_" + index++ + ".prefab");
        index = 0;
        foreach (var mine in Object.FindObjectsOfType<ProximityMine>())
            SceneAssetWriter.SavePrefab(mine.gameObject, prefabs + "PB_Mine_" + index++ + ".prefab");
        index = 0;
        foreach (var gear in Object.FindObjectsOfType<GearWheel>())
            SceneAssetWriter.SavePrefab(gear.gameObject, prefabs + "PB_Gear_" + index++ + ".prefab");
        index = 0;
        foreach (var grid in Object.FindObjectsOfType<GridPocket>())
            SceneAssetWriter.SavePrefab(grid.gameObject, prefabs + "PB_Grid_" + index++ + ".prefab");
        index = 0;
        foreach (var moving in Object.FindObjectsOfType<MovingPlatform>())
            SceneAssetWriter.SavePrefab(moving.gameObject, prefabs + "PB_MovingPlatform_" + index++ + ".prefab");
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, SceneRoot + SceneFlow.Acts[act - 1] + ".unity");
    }
    private static void BuildEffectPrefabs(StageScene bindings)
    {
        var seal = new GameObject("CORE SEAL / inside courier backpack");
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube); cube.name = "Backpack core seal / small cyan indicator";
        cube.transform.SetParent(seal.transform, false); cube.transform.localScale = new Vector3(.075f, .18f, .025f);
        Object.DestroyImmediate(cube.GetComponent<Collider>());
        var sealMaterial = new Material(bindings.EffectMaterial); sealMaterial.color = new Color(.18f, .83f, 1f);
        sealMaterial.EnableKeyword("_EMISSION"); sealMaterial.SetColor("_EmissionColor", new Color(.1f, .57f, .82f));
        cube.GetComponent<Renderer>().sharedMaterial = sealMaterial;
        var upload = GameObject.CreatePrimitive(PrimitiveType.Sphere); upload.name = "CORE / MOVING FROM BAG TO TERMINAL";
        upload.transform.localScale = Vector3.one * .56f; Object.DestroyImmediate(upload.GetComponent<Collider>());
        var uploadMaterial = new Material(bindings.EffectMaterial); uploadMaterial.color = Color.cyan;
        uploadMaterial.EnableKeyword("_EMISSION"); uploadMaterial.SetColor("_EmissionColor", Color.cyan * 1.5f);
        upload.GetComponent<Renderer>().sharedMaterial = uploadMaterial;
        var particles = new GameObject("Temporary fragment burst").AddComponent<ParticleSystem>();
        var burstRenderer = particles.GetComponent<ParticleSystemRenderer>();
        if (burstRenderer != null)
        {
            var burstMaterial = new Material(bindings.EffectMaterial);
            burstMaterial.color = new Color(.28f, .85f, 1f);
            burstMaterial.EnableKeyword("_EMISSION");
            burstMaterial.SetColor("_EmissionColor", new Color(.28f, .85f, 1f) * 1.5f);
            burstRenderer.sharedMaterial = burstMaterial;
        }
        var main = particles.main; main.playOnAwake = false; main.loop = false;
        main.startLifetime = .38f; main.startSpeed = 3.2f; main.startSize = .13f; main.maxParticles = 36;
        var emission = particles.emission; emission.rateOverTime = 0;
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        SceneAssetWriter.PersistAssets(EditorSceneManager.GetActiveScene());
        bindings.BackpackSealPrefab = PrefabUtility.SaveAsPrefabAsset(seal, "Assets/Prefabs/PB_BackpackSeal.prefab");
        bindings.UploadCorePrefab = PrefabUtility.SaveAsPrefabAsset(upload, "Assets/Prefabs/PB_UploadCore.prefab");
        bindings.BurstPrefab = PrefabUtility.SaveAsPrefabAsset(particles.gameObject, "Assets/Prefabs/PB_FragmentBurst.prefab").GetComponent<ParticleSystem>();
        Object.DestroyImmediate(seal); Object.DestroyImmediate(upload); Object.DestroyImmediate(particles.gameObject);
    }
    private static void SetBuildSettings()
    {
        EditorBuildSettings.scenes = new[] {
            new EditorBuildSettingsScene(SceneRoot + "MainMenu.unity", true),
            new EditorBuildSettingsScene(SceneRoot + "RiftOne.unity", true),
            new EditorBuildSettingsScene(SceneRoot + "RiftTwo.unity", true),
            new EditorBuildSettingsScene(SceneRoot + "RiftCore.unity", true)
        };
    }
    private static void EnsureRuntimeMaterial()
    {
        const string folder = "Assets/Art/Materials";
        const string stagePath = folder + "/StageStandard.mat";
        const string gridPath = folder + "/StageGridFade.mat";
        Directory.CreateDirectory(folder);
        AssetDatabase.Refresh();
        var shader = Shader.Find("Standard");
        if (shader == null)
        {
            Debug.LogError("SHATTERLINE: Unity Standard shader is unavailable in the Editor.");
            return;
        }
        if (!File.Exists(stagePath))
            AssetDatabase.CreateAsset(new Material(shader) { name = "StageStandard" }, stagePath);
        if (!File.Exists(gridPath))
        {
            // Keep a real transparent Shader Graph material asset in Assets. Runtime-only
            // Shader.Find references can disappear from a stripped Windows build.
            var gridShader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Art/Shaders/SG_GridPulse.shadergraph");
            if (gridShader == null) throw new System.InvalidOperationException("Import Shader Graph before authoring scenes.");
            var fade = new Material(gridShader) { name = "StageGridFade" };
            fade.SetFloat("_Opacity", .92f);
            fade.SetOverrideTag("RenderType", "Transparent");
            fade.renderQueue = (int)RenderQueue.Transparent;
            AssetDatabase.CreateAsset(fade, gridPath);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("SHATTERLINE: runtime materials ready for player builds.");
    }
    private static void CreateAnimations()
    {
        const string root = "Assets/Animation/";
        Directory.CreateDirectory(root);
        AssetDatabase.Refresh();
        Controller(root, "Courier", new[] { "Idle", "Running", "Airborne", "Dashing" },
            new[] { 0.04f, .13f, .16f, -.22f });
        Controller(root, "Sentinel", new[] { "Idle", "Engaged" }, new[] { .13f, .29f });
    }
    private static void Controller(string root, string name, string[] states, float[] amplitudes)
    {
        string path = root + name + ".controller";
        if (File.Exists(path)) return;
        var control = AnimatorController.CreateAnimatorControllerAtPath(path);
        var machine = control.layers[0].stateMachine;
        for (int i = 0; i < states.Length; i++)
        {
            string clipPath = root + name + "_" + states[i] + ".anim";
            var clip = new AnimationClip { name = name + " " + states[i], frameRate = 30, wrapMode = WrapMode.Loop };
            float duration = states[i] == "Running" ? .38f : .8f;
            var curve = new AnimationCurve(new Keyframe(0, 0),
                new Keyframe(duration / 2, amplitudes[i]), new Keyframe(duration, 0));
            clip.SetCurve("Core", typeof(Transform), "localPosition.y", curve);
            if (name == "Courier" && states[i] == "Running")
            {
                // Same 19-degree stride as the original rig, now stored in real AnimationClips.
                clip.SetCurve("Core/Near walking leg", typeof(Transform), "localEulerAnglesRaw.z",
                    AnimationCurve.EaseInOut(0, -19, duration / 2, 19));
                var stride = new AnimationCurve(new Keyframe(0, 0), new Keyframe(duration / 4, 19),
                    new Keyframe(duration / 2, 0), new Keyframe(duration * .75f, -19), new Keyframe(duration, 0));
                clip.SetCurve("Core/Near walking leg", typeof(Transform), "localEulerAnglesRaw.z", stride);
                var opposite = new AnimationCurve(new Keyframe(0, 0), new Keyframe(duration / 4, -19),
                    new Keyframe(duration / 2, 0), new Keyframe(duration * .75f, 19), new Keyframe(duration, 0));
                clip.SetCurve("Core/Far walking leg", typeof(Transform), "localEulerAnglesRaw.z", opposite);
            }
            if (name == "Sentinel")
                for (int fin = 0; fin < 4; fin++)
                    clip.SetCurve("Core/Guardian armor fin " + fin, typeof(Transform), "localEulerAnglesRaw.z",
                        new AnimationCurve(new Keyframe(0, 0), new Keyframe(duration / 2, states[i] == "Engaged" ? 12 : 4), new Keyframe(duration, 0)));
            var clipSettings = AnimationUtility.GetAnimationClipSettings(clip);
            clipSettings.loopTime = true; AnimationUtility.SetAnimationClipSettings(clip, clipSettings);
            AssetDatabase.CreateAsset(clip, clipPath);
            var state = machine.AddState(states[i]); state.motion = clip;
            if (i == 0) machine.defaultState = state;
        }
        if (name == "Courier")
        {
            control.AddParameter("Running", AnimatorControllerParameterType.Bool);
            control.AddParameter("Airborne", AnimatorControllerParameterType.Bool);
            control.AddParameter("Dashing", AnimatorControllerParameterType.Bool);
            foreach (var child in machine.states)
            {
                var transition = machine.AddAnyStateTransition(child.state);
                transition.hasExitTime = false; transition.canTransitionToSelf = false; transition.duration = .06f;
                bool dash = child.state.name == "Dashing", air = child.state.name == "Airborne";
                bool run = child.state.name == "Running";
                transition.AddCondition(dash ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, "Dashing");
                if (!dash) transition.AddCondition(air ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, "Airborne");
                if (!dash && !air) transition.AddCondition(run ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, "Running");
            }
        }
        if (name == "Sentinel")
        {
            control.AddParameter("Engaged", AnimatorControllerParameterType.Bool);
            var idle = machine.states[0].state; var engaged = machine.states[1].state;
            var enter = idle.AddTransition(engaged); enter.hasExitTime = false;
            enter.AddCondition(AnimatorConditionMode.If, 0, "Engaged");
            var leave = engaged.AddTransition(idle); leave.hasExitTime = false;
            leave.AddCondition(AnimatorConditionMode.IfNot, 0, "Engaged");
        }
        AssetDatabase.SaveAssets();
    }
}
