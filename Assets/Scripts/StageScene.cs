using System;
using UnityEngine;

namespace Shatterline
{
    [Serializable]
    public sealed class GuardSpawnPlan
    {
        public float Anchor, Height, MinX, MaxX;
        public bool Boss, Flying, Rapid;
        public float[] Sites;
        public Sentinel Active;
        public GuardSpawnPlan Copy() { return (GuardSpawnPlan)MemberwiseClone(); }
    }

    [Serializable]
    public sealed class TurretSpawnPlan
    {
        public float[] Sites;
        public float Height;
        public int Slot;
        public SentryTurret Active;
        public TurretSpawnPlan Copy() { return (TurretSpawnPlan)MemberwiseClone(); }
    }

    // The saved scene is the source of truth. Inspect these bindings before Play.
    public sealed class StageScene : MonoBehaviour
    {
        [Range(1, 3)] public int Act = 1;
        public Transform LevelRoot;
        public Camera StageCamera;
        public Runner Player;
        public ProjectilePool Projectiles;
        public GuardDirector Guards;
        public AdaptiveDirector Director;
        public DroneCompanion Drone;
        public Fragment[] Fragments;
        public RouteVariation[] RouteVariations;
        public GridPocket[] GridPockets;
        public Sentinel SentinelPrefab;
        public SentryTurret TurretPrefab;
        public Material EffectMaterial;
        public GameObject BackpackSealPrefab;
        public GameObject UploadCorePrefab;
        public ParticleSystem BurstPrefab;
        public GuardSpawnPlan[] GuardPlans;
        public TurretSpawnPlan[] TurretPlans;

        public void Initialize(RunGame game)
        {
            if (Player == null || StageCamera == null || LevelRoot == null ||
                Projectiles == null || Guards == null || Director == null || Drone == null ||
                SentinelPrefab == null || EffectMaterial == null || BackpackSealPrefab == null ||
                UploadCorePrefab == null || BurstPrefab == null)
                throw new InvalidOperationException("StageScene has a missing Inspector binding. Re-bake this scene in the Editor.");
            StageBuilder.Initialize(this, game);
            Guards.CacheLadders();
            Player.InitializeRuntime(game);
            Director.Initialize(game, Player);
            Drone.Initialize(game, Player);
        }
    }
}
