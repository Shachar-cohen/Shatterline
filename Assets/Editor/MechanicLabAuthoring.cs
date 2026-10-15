using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Shatterline;

// A small saved test scene, deliberately excluded from the four campaign build scenes.
public static class MechanicLabAuthoring
{
    [MenuItem("SHATTERLINE/Create Component Test Scene")]
    public static void Create()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var manager = new GameObject("Test services / explicit dependencies");
        var game = manager.AddComponent<RunGame>(); game.SetAuthoringLevel(1);
        var stage = manager.AddComponent<StageScene>(); stage.Act = 1;
        stage.LevelRoot = new GameObject("Test floor and prefab instances").transform;
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube); floor.name = "Test ground";
        floor.transform.SetParent(stage.LevelRoot); floor.transform.position = new Vector3(18, -.5f, 0);
        floor.transform.localScale = new Vector3(48, 1, 5);
        stage.EffectMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/StageStandard.mat");
        floor.GetComponent<Renderer>().sharedMaterial = stage.EffectMaterial;
        stage.Player = Instance("PB_Courier").GetComponent<Runner>();
        stage.Player.transform.position = new Vector3(1, 1.25f, 0);
        stage.Projectiles = Instance("PB_ProjectilePool").GetComponent<ProjectilePool>();
        stage.Drone = Instance("PB_Drone").GetComponent<DroneCompanion>();
        stage.Guards = manager.AddComponent<GuardDirector>(); stage.Director = manager.AddComponent<AdaptiveDirector>();
        var guard = Instance("PB_Sentinel_0").GetComponent<Sentinel>();
        stage.SentinelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Act1/PB_Sentinel_0.prefab").GetComponent<Sentinel>();
        stage.GuardPlans = new[] { new GuardSpawnPlan { Anchor = 14, Height = 1.25f, Sites = new[] { 14f }, Active = guard } };
        stage.TurretPlans = new TurretSpawnPlan[0]; stage.Fragments = new Fragment[0]; stage.RouteVariations = new RouteVariation[0];
        var grid = Instance("PB_Grid_0").GetComponent<GridPocket>(); grid.Center = 27; grid.Row = 0;
        stage.GridPockets = new[] { grid };
        var moving = Instance("PB_MovingPlatform_0"); moving.transform.position = new Vector3(8, 3, 0);
        var mine = Instance("PB_Mine_0"); mine.transform.position = new Vector3(20, .22f, 0);
        stage.BackpackSealPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PB_BackpackSeal.prefab");
        stage.UploadCorePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PB_UploadCore.prefab");
        stage.BurstPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/PB_FragmentBurst.prefab").GetComponent<ParticleSystem>();
        var cameraObject = new GameObject("Lab camera", typeof(Camera), typeof(AudioListener));
        stage.StageCamera = cameraObject.GetComponent<Camera>(); cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(4, 5.5f, -19); cameraObject.transform.rotation = Quaternion.Euler(5, 0, 0);
        stage.StageCamera.orthographic = true; stage.StageCamera.orthographicSize = 6;
        RenderSettings.ambientLight = new Color(.68f, .68f, .78f);
        var audio = AudioAuthoring.AddToScene(AudioAuthoring.EnsureAssets());
        game.SetSceneBindings(stage, audio, AssetDatabase.LoadAssetAtPath<Ink.UnityIntegration.InkFile>("Assets/Narrative/Signal.ink"));
        CanvasAuthoring.Gameplay(game, audio); SceneAssetWriter.PersistAssets(scene);
        Directory.CreateDirectory("Assets/Scenes/Tests"); AssetDatabase.Refresh();
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Tests/MechanicLab.unity");
        Debug.Log("MechanicLab saved outside campaign Build Settings. Test movement, dash, drone, one guard, grid and platform; remove/replace individual prefab instances in Edit mode. This scene has no campaign completion goal.");
    }
    private static GameObject Instance(string name)
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Act1/" + name + ".prefab") ??
                    AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Act2/" + name + ".prefab") ??
                    AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Act3/" + name + ".prefab") ??
                    AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/" + name + ".prefab");
        if (asset == null) throw new InvalidOperationException("Prepare Editor Project first; missing " + name);
        return (GameObject)PrefabUtility.InstantiatePrefab(asset);
    }
}
