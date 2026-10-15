using System.Collections.Generic;
using UnityEngine;

namespace Shatterline
{
    public sealed class Breakable : MonoBehaviour
    {
        private bool broken;
        public void Shatter()
        {
            if (broken) return; broken = true;
            var box = GetComponent<Collider>(); if (box != null) box.enabled = false;
            var renderer = GetComponent<Renderer>();
            // Standard is opaque in the player build: alpha zero would still
            // draw the broken obstacle until its delayed destruction.
            if (renderer != null) renderer.enabled = false;
            RunGame.Current.DefeatEnemy(50);
            RunGame.Burst(transform.position, new Color(1f, .47f, .21f));
            Destroy(gameObject, .15f);
        }
    }
}
