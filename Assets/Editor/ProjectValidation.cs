using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Shatterline;

public static class ProjectValidation
{
    [MenuItem("SHATTERLINE/Validate Saved Scenes and Assets")]
    public static void Validate()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play mode before validating saved scene assets.");
        string[] names = { "MainMenu", "RiftOne", "RiftTwo", "RiftCore" };
        foreach (string name in names)
        {
            string path = "Assets/Scenes/" + name + ".unity";
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null) Fail(name, "scene asset is missing");
            var scene = SceneManager.GetSceneByPath(path);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            try { ValidateScene(scene, name == "MainMenu"); }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }
        Debug.Log("SHATTERLINE validation passed: all saved scenes are populated; scripts, assets, Canvas controls, prefabs and AudioMixer routing are present. Playtesting is still required.");
    }
    private static void Fail(string scene, string reason)
    { throw new InvalidOperationException(scene + ": " + reason + ". Use SHATTERLINE > Prepare Editor Project to repair generated assets."); }
    private static void RequireBindings(UnityEngine.Object component, string scene, params string[] fields)
    {
        var serialized = new SerializedObject(component);
        foreach (string field in fields)
        {
            var property = serialized.FindProperty(field);
            if (property == null || property.objectReferenceValue == null) Fail(scene, component.name + " missing " + field);
        }
    }
    private static T[] Components<T>(Scene scene) where T : Component
    { return scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray(); }
    private static void ValidateScene(Scene scene, bool menu)
    {
        string name = scene.name;
        foreach (var root in scene.GetRootGameObjects())
            foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
                if (behaviour == null) Fail(name, "Missing Script on " + root.name);
        if (Components<AudioListener>(scene).Length != 1) Fail(name, "exactly one AudioListener is required");
        if (Components<EventSystem>(scene).Length != 1) Fail(name, "exactly one EventSystem is required");
        if (Components<Canvas>(scene).Length == 0) Fail(name, "saved Canvas is missing");
        var audio = Components<AudioSystem>(scene);
        if (audio.Length != 1 || !audio[0].IsConfigured) Fail(name, "AudioMixer routing or source bindings are invalid");
        foreach (var source in Components<AudioSource>(scene))
            if (source.outputAudioMixerGroup == null) Fail(name, "AudioSource is not routed to a mixer group");
        float ignored;
        foreach (string parameter in new[] { "MasterVolume", "SfxVolume", "UiVolume" })
            if (!audio[0].Configuration.Mixer.GetFloat(parameter, out ignored)) Fail(name, "missing exposed mixer parameter " + parameter);
        foreach (var button in Components<Button>(scene))
        {
            if (button.onClick.GetPersistentEventCount() == 0) Fail(name, "button has no saved action: " + button.name);
            for (int i = 0; i < button.onClick.GetPersistentEventCount(); i++)
                if (button.onClick.GetPersistentTarget(i) == null) Fail(name, "button target is missing: " + button.name);
        }
        foreach (var renderer in Components<Renderer>(scene))
            foreach (var material in renderer.sharedMaterials)
                if (material == null || material.shader == null) Fail(name, "missing material or shader on " + renderer.name);
        if (menu)
        {
            var controller = Components<MainMenu>(scene).SingleOrDefault();
            if (controller == null || controller.HomePanel == null || controller.InstructionsPanel == null ||
                controller.SettingsPanel == null || controller.CreditsPanel == null || controller.LeaderboardPanel == null)
                Fail(name, "main menu panels are not assigned");
            return;
        }
        var bindings = Components<StageScene>(scene).SingleOrDefault();
        if (bindings == null || bindings.Player == null || bindings.LevelRoot == null || bindings.StageCamera == null ||
            bindings.Projectiles == null || bindings.Guards == null || bindings.Director == null || bindings.Drone == null)
            Fail(name, "StageScene Inspector bindings are missing");
        if (Components<Renderer>(scene).Length < 20) Fail(name, "scene is empty or incomplete before Play");
        if (bindings.SentinelPrefab == null || !AssetDatabase.Contains(bindings.SentinelPrefab)) Fail(name, "sentinel prefab is missing");
        if (bindings.BackpackSealPrefab == null || bindings.UploadCorePrefab == null || bindings.BurstPrefab == null)
            Fail(name, "core or particle prefab is missing");
        if (bindings.Projectiles.Capacity != 32) Fail(name, "saved projectile pool must contain 32 shots");
        if (bindings.Fragments == null || bindings.Fragments.Count(f => f != null && !f.Healing) != 2)
            Fail(name, "the two required core references are missing");
        if (bindings.GuardPlans == null || bindings.GuardPlans.Any(p => p.Active == null)) Fail(name, "guard plan references are missing");
        if (bindings.TurretPlans == null || bindings.TurretPlans.Length > 0 && bindings.TurretPrefab == null)
            Fail(name, "turret plan prefab reference is missing");
        RequireBindings(bindings.Player, name, "body", "game", "animator", "rig", "gun", "standingSprite", "crouchSprite", "shieldVisual", "muzzleSparks", "muzzleTransform");
        var run = Components<RunGame>(scene).SingleOrDefault();
        if (run == null) Fail(name, "RunGame is missing");
        RequireBindings(run, name, "stage", "audioSystem", "narrative");
        if (bindings.Act == 3) RequireBindings(run, name, "guardianBarrier");
        if (Components<GameHud>(scene).Length != 1) Fail(name, "gameplay Canvas presenter is missing");
        foreach (var animator in Components<Animator>(scene))
        {
            if (animator.runtimeAnimatorController == null) Fail(name, "Animator Controller is missing");
            foreach (var clip in animator.runtimeAnimatorController.animationClips)
                if (clip == null || !AssetDatabase.Contains(clip) || AnimationUtility.GetCurveBindings(clip).Length == 0)
                    Fail(name, "Animator has no saved animation curves");
        }
        if (!PrefabUtility.IsPartOfPrefabInstance(bindings.Player)) Fail(name, "player is not a saved prefab instance");
    }
}
