using System.Collections.Generic;
using UnityEngine;

namespace Shatterline
{
    public sealed class AmbushZone : MonoBehaviour
    {
        private bool triggered;
        public int Act;
        private void OnTriggerEnter(Collider other)
        {
            if (triggered || other.GetComponent<Runner>() == null) return;
            triggered = true;
            RunGame.Current.StartAmbush(Act, transform.position.x);
        }
    }
}
