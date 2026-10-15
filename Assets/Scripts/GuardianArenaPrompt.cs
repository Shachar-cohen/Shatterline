using System.Collections.Generic;
using UnityEngine;

namespace Shatterline
{
    public sealed class GuardianArenaPrompt : MonoBehaviour
    {
        private bool announced;
        private void OnTriggerEnter(Collider other)
        {
            if (announced || other.GetComponent<Runner>() == null || RunGame.Current == null ||
                RunGame.Current.BossDown) return;
            announced = true;
            RunGame.Current.Flash("FINAL ARENA  /  DESTROY 3 PURPLE RELAYS, THEN THE GUARDIAN", 4f);
        }
    }
}
