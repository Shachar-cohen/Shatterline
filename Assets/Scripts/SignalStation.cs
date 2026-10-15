using System.Collections.Generic;
using UnityEngine;

namespace Shatterline
{
    public sealed class SignalStation : MonoBehaviour
    {
        public Transform Beacon;
        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<Runner>() != null && RunGame.Current != null)
                RunGame.Current.BeginSignalUpload(this);
        }
        private void OnTriggerStay(Collider other) { OnTriggerEnter(other); }
    }
}
