using System.Collections.Generic;
using UnityEngine;

namespace Shatterline
{
    public sealed class CrouchPrompt : MonoBehaviour
    {
        private bool shown;
        private void OnTriggerEnter(Collider other)
        {
            if (shown || other.GetComponent<Runner>() == null) return;
            shown = true;
            RunGame.Current.Flash("LOW PASSAGE  /  HOLD S AND MOVE WITH A/D", 3f);
        }
    }
}
