using System.Collections.Generic;
using UnityEngine;

namespace Shatterline
{
    public sealed class Crusher : MonoBehaviour
    {
        public float Offset;
        private float highY;
        private Renderer surface;
        private bool warned;
        private void Start() { highY = transform.position.y; surface = GetComponent<Renderer>(); }
        private void Update()
        {
            if (RunGame.Current == null || !RunGame.Current.CanPlay) return;
            if (!warned && RunGame.Current.Player != null &&
                Mathf.Abs(RunGame.Current.Player.transform.position.x - transform.position.x) < 4f)
            {
                warned = true;
                RunGame.Current.Flash("TIMED PRESS / WAIT UNTIL IT RISES, THEN CROSS", 2f);
            }
            float cycle = Mathf.Repeat(Time.time * .52f + Offset, 1f);
            float drop = cycle < .55f ? 0f : cycle < .69f ?
                (cycle - .55f) / .14f : cycle < .84f ? 1f :
                1f - (cycle - .84f) / .16f;
            transform.position = new Vector3(transform.position.x,
                highY - Mathf.Clamp01(drop) * 3f, transform.position.z);
            if (surface != null)
                surface.material.color = cycle > .43f && cycle < .89f ?
                    new Color(1f, .27f, .11f) : new Color(.46f, .59f, .63f);
        }
        private void OnTriggerEnter(Collider other) { Strike(other); }
        private void OnTriggerStay(Collider other) { Strike(other); }
        private void Strike(Collider other)
        {
            if (RunGame.Current != null && RunGame.Current.CanPlay &&
                transform.position.y < 2.7f && other.GetComponent<Runner>() != null)
                RunGame.Current.Hurt(1, "PRESS");
        }
    }
}
