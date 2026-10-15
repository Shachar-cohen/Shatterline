#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Shatterline
{
    // Deliberately excluded from production builds by conditional compilation.
    public sealed class DevConsole : MonoBehaviour
    {
        private RunGame run;
        private bool visible, invulnerable;
        private void Start() { run = GetComponent<RunGame>(); }
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1)) visible = !visible;
        }
        public bool Invulnerable { get { return invulnerable; } }
        private void OnGUI()
        {
            if (!visible || run == null) return;
            GUI.Box(new Rect(16, 126, 282, 303), "DEVELOPER TOOLS  [F1]");
            GUI.Label(new Rect(27, 145, 260, 18), "RUN SEED: " + RunState.RunSeed);
            if (GUI.Button(new Rect(27, 163, 260, 34), "Invulnerable: " + (invulnerable ? "ON" : "OFF")))
            { invulnerable = !invulnerable; RunState.Assisted = true; }
            if (GUI.Button(new Rect(27, 202, 260, 34), "Give 2 fragments"))
            { RunState.Assisted = true; if (run.ShardsHere < 2) run.Pickup(); if (run.ShardsHere < 2) run.Pickup(); }
            if (GUI.Button(new Rect(27, 241, 260, 34), "Warp to checkpoint"))
            { RunState.Assisted = true; run.Player.ResetAt(new Vector3(95, 1.3f, 0)); }
            if (GUI.Button(new Rect(27, 280, 260, 34), "Warp to exit"))
            { RunState.Assisted = true; run.Player.ResetAt(new Vector3(StageBuilder.PortalX - 3, 1.3f, 0)); }
            if (GUI.Button(new Rect(27, 319, 260, 34), "Defeat boss (Act III)"))
            { RunState.Assisted = true; run.BossDefeated(); }
            if (GUI.Button(new Rect(27, 358, 260, 34), "Start next act"))
            { RunState.Assisted = true;
                SceneFlow.RestoreClock();
                SceneManager.LoadScene(run.Level == 1 ? "RiftTwo" : run.Level == 2 ? "RiftCore" : "RiftOne"); }
            var ai = GuardDirector.Current;
            if (ai != null)
            {
                int patrol = 0, searching = 0, attacking = 0, climbing = 0, sweep = 0, watch = 0;
                foreach (Sentinel guard in FindObjectsOfType<Sentinel>())
                {
                    if (guard.Boss) continue;
                    string state = guard.AIState;
                    if (state == "PATROL") patrol++;
                    else if (state == "CLIMB") climbing++;
                    else if (state == "SWEEP") sweep++;
                    else if (state == "OVERWATCH") watch++;
                    else if (state == "ATTACK" || state == "FLANK" || state == "COVER" || state == "RETREAT") attacking++;
                    else searching++;
                }
                float x = Screen.width - 305;
                GUI.Box(new Rect(x, 126, 289, 243), "AI DEBUG  /  LIVE STATES");
                GUI.Label(new Rect(x + 12, 153, 272, 22),
                    "Patrol: " + patrol + "   Search: " + searching);
                GUI.Label(new Rect(x + 12, 178, 272, 22),
                    "Combat: " + attacking + "   Climb: " + climbing);
                GUI.Label(new Rect(x + 12, 205, 272, 22),
                    "Sweep: " + sweep + "   Watch: " + watch);
                GUI.Label(new Rect(x + 12, 232, 272, 22),
                    "Alert: " + ai.Pressure + "/3   Orders: " + ai.TacticalOrders);
                GUI.Label(new Rect(x + 12, 259, 272, 48), ai.LastAlert);
                var director = AdaptiveDirector.Current;
                if (director != null)
                {
                    GUI.Label(new Rect(x + 12, 312, 272, 22),
                        "Director: " + director.LastDecision + "  [" + director.DecisionsMade + "/4]");
                    GUI.Label(new Rect(x + 12, 338, 272, 22), director.LastReason);
                }
            }
        }
    }
}
#endif
