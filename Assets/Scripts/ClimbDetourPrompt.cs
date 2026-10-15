using System.Collections.Generic;
using UnityEngine;

namespace Shatterline
{
    public sealed class ClimbDetourPrompt : MonoBehaviour
    {
        private bool announced;
        private void OnTriggerEnter(Collider other)
        {
            if (announced || other.GetComponent<Runner>() == null) return;
            announced = true;
            RunGame.Current.Flash("HIGH GATE / USE THE MARKED LADDER: HOLD R, THEN MOVE RIGHT", 3.1f);
        }
    }
}
