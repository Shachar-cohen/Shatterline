using System.Collections.Generic;
using UnityEngine;

namespace Shatterline
{
    public sealed class Checkpoint : MonoBehaviour
    {
        public float Position;
        private bool used;
        public void Activate()
        {
            if (used) return; used = true;
            RunGame.Current.Checkpoint(Position);
            GetComponent<Renderer>().material.color = new Color(.64f, 1f, .72f);
        }
    }
}
