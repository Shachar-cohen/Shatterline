using System.Collections.Generic;
using UnityEngine;

namespace Shatterline
{
    public sealed class MinefieldGrid : MonoBehaviour
    {
        private const float WarningSeconds = .85f;
        private float LiveSeconds { get { return lowerLane ? 3.4f : 4.2f; } }
        private float OpenSeconds { get { return lowerLane ? 4.8f : 3.7f; } }
        [SerializeField] private BoxCollider sensor, wall;
        private Material visual;
        [SerializeField] private Renderer[] gridBars;
        [SerializeField] private Light powerLight;
        private System.Random timerRandom;
        [SerializeField] private float startX, switchAt;
        private bool live = true;
        [SerializeField] private bool lowerLane;
        private bool warned;
        private GUIStyle notice;

        [SerializeField] private int authoredRow;
        [SerializeField] private Material fadeTemplate;
        public void AssignFadeMaterial(Material value) { fadeTemplate = value; }
        private void Start()
        {
            Initialize(transform.position.x - sensor.size.x * .5f, sensor, wall, gridBars, powerLight, lowerLane, authoredRow);
        }
        public void Initialize(float left, BoxCollider trigger, BoxCollider barrier,
            Renderer[] bars, Light lamp, bool lower, int row)
        {
            authoredRow = row;
            startX = left;
            lowerLane = lower;
            sensor = trigger;
            wall = barrier;
            powerLight = lamp;
            // A saved Shader Graph material reference keeps both the shader and its
            // transparent rendering mode in Editor and standalone builds.
            var fade = fadeTemplate;
            if (fade == null)
                throw new System.InvalidOperationException(
                    "StageGridFade material is missing. Reopen the project in Unity to generate it, then rebuild.");
            if (visual != null && Application.isPlaying) Destroy(visual);
            visual = new Material(fade);
            gridBars = bars;
            foreach (Renderer bar in bars) bar.sharedMaterial = visual;
            timerRandom = new System.Random(unchecked(RunState.RunSeed ^
                (Mathf.RoundToInt(left * 7919f) * 31)));
            // Opposite starting states and deliberately separated first changes.
            // Each later phase has its own small seeded timing variation.
            if (row == 0)
            {
                SetLive(true);
                switchAt = Time.time + 2f + (float)timerRandom.NextDouble() * .7f;
            }
            else
            {
                SetLive(false);
                switchAt = Time.time + 4.4f + (float)timerRandom.NextDouble() * .7f;
            }
        }

        private void SetLive(bool state)
        {
            live = state;
            sensor.enabled = state;
            wall.enabled = state;
            switchAt = Time.time + (state ? LiveSeconds : OpenSeconds) +
                (float)(timerRandom.NextDouble() * .7 - .35);
            UpdateVisual();
        }

        private void UpdateVisual()
        {
            if (visual == null) return;
            float remaining = switchAt - Time.time;
            bool warning = !live && remaining < WarningSeconds;
            if (gridBars != null)
                foreach (Renderer bar in gridBars)
                    if (bar != null) bar.enabled = true;
            // OPEN remains a faint, pulsing ghost of the grid; the closing
            // warning flashes brighter before it becomes solid again.
            float alpha = live ? .92f : warning ?
                (Mathf.Sin(Time.time * 18f) > 0f ? .78f : .24f) :
                .18f + .11f * (1f + Mathf.Sin(Time.time * 5f)) * .5f;
            Color tint = warning ? new Color(1f, .76f, .2f) :
                lowerLane ? new Color(1f, .53f, .22f) : new Color(.12f, .85f, 1f);
            tint.a = alpha;
            visual.color = tint;
            visual.SetFloat("_Opacity", alpha);
            if (powerLight != null) powerLight.intensity = live ? .9f : warning ? .35f : .09f;
        }

        private void Update()
        {
            if (RunGame.Current == null || !RunGame.Current.CanPlay) return;
            float x = RunGame.Current.Player.transform.position.x;
            if (!warned && x >= startX - 4f && x <= startX + 1f)
            {
                warned = true;
                RunGame.Current.Flash("LOCAL GRIDS / FOLLOW THE STEPS OR WAIT FOR AN OPEN PHASE", 3f);
            }
            if (Time.time >= switchAt)
            {
                if (live) SetLive(false);
                else
                {
                    // A closing solid wall must never materialize around Nova.
                    Collider runner = RunGame.Current.Player.GetComponent<Collider>();
                    var field = new Bounds(transform.position, sensor.size);
                    if (runner != null && runner.bounds.Intersects(field))
                        switchAt = Time.time + .2f;
                    else SetLive(true);
                }
            }
            UpdateVisual();
        }
        private void OnGUI()
        {
            if (RunGame.Current == null || RunGame.Current.Player == null ||
                Mathf.Abs(RunGame.Current.Player.transform.position.x - transform.position.x) > 8f) return;
            var cameraView = Camera.main;
            if (cameraView == null) return;
            Vector3 screen = cameraView.WorldToScreenPoint(transform.position +
                Vector3.up * (lowerLane ? 1.65f : 2.25f));
            if (screen.z <= 0f || screen.x < 60f || screen.x > Screen.width - 60f) return;
            if (notice == null)
            {
                notice = new GUIStyle(GUI.skin.box) { fontSize = 12, fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter };
                notice.normal.textColor = Color.white;
            }
            float remaining = Mathf.Max(0, switchAt - Time.time);
            string lane = lowerLane ? "LOW" : "HIGH";
            string message = lane + (live ? " LIVE " : remaining < WarningSeconds ? " CLOSE " : " OPEN ") +
                remaining.ToString("0.0") + "s";
            GUI.Box(new Rect(screen.x - 64f,
                Mathf.Clamp(Screen.height - screen.y - 26f, 8f, Screen.height - 28f), 128f, 23f),
                message, notice);
        }
        private void OnTriggerEnter(Collider other) { Strike(other); }
        private void OnTriggerStay(Collider other) { Strike(other); }
        private void Strike(Collider other)
        {
            if (!live || other.GetComponent<Runner>() == null || RunGame.Current == null ||
                !RunGame.Current.CanPlay) return;
            if (!warned)
            {
                warned = true;
                RunGame.Current.Flash(lowerLane ?
                    "LOW GRID LIVE / TAKE STEPS OR WAIT FOR OPEN" :
                    "HIGH GRID LIVE / DROP LOW OR WAIT FOR OPEN", 3f);
            }
            RunGame.Current.Hurt(1, "ELECTRIC GRID");
        }
        private void OnDestroy() { if (Application.isPlaying && visual != null) Destroy(visual); }
    }
}
