using UnityEngine;

namespace Shatterline
{
    public static class RunState
    {
        public static int Lives = 3, Score, Shards, Deaths, Coins, Special;
        public static int CoinsCollected, CoreIntegrity, CombatPoints;
        public static float PlaySeconds;
        public static bool Assisted;
        public static int DirectiveAct, Directive, Intel;
        public static int RunSeed;
        public static float Volume = .7f;
        public static bool Tutorial = true;
        public static int Tactic;
        public static void NewRun()
        {
            Lives = 3; Score = Shards = Deaths = Tactic = Coins = Special = 0;
            CoinsCollected = CoreIntegrity = CombatPoints = 0;
            PlaySeconds = 0f;
            Assisted = false;
            DirectiveAct = Directive = Intel = 0;
            RunSeed = System.Environment.TickCount ^ (int)System.DateTime.UtcNow.Ticks;
            Tutorial = true;
        }
    }

}
