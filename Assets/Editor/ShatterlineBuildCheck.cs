using Ink.UnityIntegration;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Catch missing assets before producing an EXE that opens but cannot start a run.
public sealed class ShatterlineBuildCheck : IPreprocessBuildWithReport
{
    // Ink's own prebuild callback recompiles the story at order 0.
    public int callbackOrder { get { return 100; } }

    public void OnPreprocessBuild(BuildReport report)
    {
        try { ProjectValidation.Validate(); }
        catch (System.Exception error) { Fail(error.Message); }

        string[] scenes = {
            "Assets/Scenes/MainMenu.unity", "Assets/Scenes/RiftOne.unity",
            "Assets/Scenes/RiftTwo.unity", "Assets/Scenes/RiftCore.unity"
        };
        var configured = EditorBuildSettings.scenes;
        for (int i = 0; i < scenes.Length; i++)
        {
            if (configured.Length <= i || !configured[i].enabled || configured[i].path != scenes[i] ||
                AssetDatabase.LoadAssetAtPath<SceneAsset>(scenes[i]) == null)
                Fail("The four enabled Build Settings scenes must be MainMenu, RiftOne, RiftTwo, RiftCore in that order.");
        }

        var stageMaterial = AssetDatabase.LoadAssetAtPath<Material>(
            "Assets/Art/Materials/StageStandard.mat");
        if (stageMaterial == null || stageMaterial.shader == null)
            Fail("The StageStandard material is missing; runtime geometry would have no shader.");

        var gridMaterial = AssetDatabase.LoadAssetAtPath<Material>(
            "Assets/Art/Materials/StageGridFade.mat");
        if (gridMaterial == null || gridMaterial.shader == null ||
            !gridMaterial.HasProperty("_Opacity") || !gridMaterial.HasProperty("_Color"))
            Fail("The StageGridFade material is missing or not configured for transparent Fade rendering.");

        var story = AssetDatabase.LoadAssetAtPath<InkFile>("Assets/Narrative/Signal.ink");
        if (story == null || !story.isCompiled || story.hasErrors || story.hasUnhandledCompileErrors)
            Fail("The Signal.ink story must be imported and compiled successfully.");

        foreach (string name in new[] { "Courier", "Sentinel" })
            if (AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                "Assets/Animation/" + name + ".controller") == null)
                Fail("The " + name + " animation controller is missing.");

        foreach (string name in new[] { "CourierSprite", "CourierCrouch", "CityBackdrop",
            "stone", "metal", "masonry", "armor", "circuit", "hazard" })
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(
                "Assets/Art/Textures/" + name + ".png") == null)
                Fail("The " + name + " texture is missing.");

        foreach (string name in new[] { "pickup", "damage", "dash", "checkpoint", "blaster" })
            if (AssetDatabase.LoadAssetAtPath<AudioClip>(
                "Assets/Audio/SFX/" + name + ".wav") == null)
                Fail("The " + name + " sound is missing.");
    }

    private static void Fail(string reason)
    {
        throw new BuildFailedException("SHATTERLINE build check: " + reason +
            " Wait for Unity import to finish, then use SHATTERLINE > Prepare Editor Project and build again.");
    }
}
