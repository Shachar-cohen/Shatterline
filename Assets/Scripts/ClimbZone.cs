using System.Collections.Generic;
using UnityEngine;

namespace Shatterline
{
    public sealed class ClimbZone : MonoBehaviour
    {
        public bool Rescue;
        private float lastAnnounced = -10f;
        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<Runner>() == null || Time.time - lastAnnounced < 2f) return;
            lastAnnounced = Time.time;
            RunGame.Current.Flash(Rescue ? "ABYSS LADDER / HOLD R TO CLIMB TO SAFETY" :
                "LADDER / HOLD R TO CLIMB, DOWN ARROW TO DESCEND", 2.5f);
        }
    }
}
