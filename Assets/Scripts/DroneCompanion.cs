using System.Collections.Generic;
using UnityEngine;

namespace Shatterline
{
    public sealed class DroneCompanion : MonoBehaviour
    {
        public static DroneCompanion Current { get; private set; }
        [SerializeField] private RunGame game;
        [SerializeField] private Runner player;
        [SerializeField] private GameObject visual;
        private float defendReady;
        public bool Active { get; private set; }
        public float DefenseCooldown { get { return Mathf.Max(0, defendReady - Time.time); } }

        private void Awake() { Current = this; }
        private void OnDestroy() { if (Current == this) Current = null; }
        public void Initialize(RunGame run, Runner runner)
        {
            game = run; player = runner;
            transform.position = runner.transform.position + new Vector3(-.65f, 2.1f, -.8f);
            Active = false;
            if (visual != null) { visual.SetActive(false); return; }
#if UNITY_EDITOR
            visual = new GameObject("Drone visuals / hidden until F");
            visual.transform.SetParent(transform, false);
            var body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            body.name = "Courier companion / illuminated core";
            body.transform.SetParent(visual.transform, false);
            body.transform.localScale = Vector3.one * .53f;
            DestroyImmediate(body.GetComponent<Collider>());
            body.GetComponent<Renderer>().sharedMaterial = StageBuilder.Material(new Color(1f, .68f, .22f));
            var eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eye.name = "Drone forward eye";
            eye.transform.SetParent(visual.transform, false);
            eye.transform.localPosition = new Vector3(.19f, .04f, -.27f);
            eye.transform.localScale = Vector3.one * .19f;
            DestroyImmediate(eye.GetComponent<Collider>());
            eye.GetComponent<Renderer>().sharedMaterial = StageBuilder.Material(Color.white);
            visual.SetActive(false);
#else
            throw new System.InvalidOperationException("Drone prefab visual is not assigned.");
#endif
        }
        private void Update()
        {
            if (game == null || player == null || !game.CanPlay) return;
            if (Input.GetKeyDown(KeyCode.F))
            {
                Active = !Active;
                visual.SetActive(Active);
                game.Flash(Active ? "DRONE ON / -25% SPEED / BLOCK COSTS 25 CHARGE" :
                    "DRONE OFF / MOVEMENT SPEED RESTORED", 2f);
            }
            if (!Active) return;
            Vector3 destination = player.transform.position +
                new Vector3(-.65f, 2f + Mathf.Sin(Time.time * 3f) * .18f, -.8f);
            transform.position = Vector3.Lerp(transform.position, destination,
                Mathf.Min(1f, Time.deltaTime * 7f));
        }
        public bool TryIntercept(Vector3 shotPosition, Vector3 velocity)
        {
            if (!Active || game == null || !game.CanPlay || player == null || player.Shielded ||
                Time.time < defendReady || player.Energy < 25) return false;
            Vector3 target = player.transform.position + Vector3.up * .9f;
            Vector3 approach = target - shotPosition;
            if (approach.sqrMagnitude > 4.8f || Vector3.Dot(velocity, approach) <= 0) return false;
            RaycastHit wall;
            if (Physics.Raycast(shotPosition, approach.normalized, out wall,
                approach.magnitude, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                return false;
            if (!player.TrySpendEnergy(25)) return false;
            defendReady = Time.time + 8f;
            RunGame.Burst(shotPosition, new Color(.2f, .88f, 1f));
            game.Flash("DRONE INTERCEPT / -25 CHARGE / 8s RECHARGE", 1.5f);
            return true;
        }
    }
}
