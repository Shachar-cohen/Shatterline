using System.Collections.Generic;
using UnityEngine;

namespace Shatterline
{
    public sealed class Hazard : MonoBehaviour
    {
        public bool Pulsing;
        private Collider trigger;
        private Renderer body;
        private void Start() { trigger = GetComponent<Collider>(); body = GetComponent<Renderer>(); }
        private void Update()
        {
            if (!Pulsing) return;
            bool dangerous = Mathf.Sin(Time.time * 2.4f + transform.position.x) > -.2f;
            trigger.enabled = dangerous;
            body.material.color = dangerous ? new Color(1f, .27f, .13f) : new Color(.32f, .42f, .47f);
        }
    }
}
