using System.Collections.Generic;
using UnityEngine;

namespace Shatterline
{
    public sealed class SteamVent : MonoBehaviour
    {
        public float Phase;
        public float DangerStart = .55f;
        private Collider sensor;
        private Renderer plume;
        private bool active;
        private void Start() { sensor = GetComponent<Collider>(); plume = GetComponent<Renderer>(); }
        private void Update()
        {
            if (sensor == null || RunGame.Current == null) return;
            float cycle = Mathf.Repeat(Time.time * .31f + Phase, 1f);
            active = cycle >= DangerStart && cycle < .82f;
            sensor.enabled = active;
            plume.enabled = cycle >= Mathf.Max(0f, DangerStart - .12f) && cycle < .82f;
            plume.material.color = active ? new Color(.91f, .74f, .32f, .8f) :
                cycle >= DangerStart - .12f ? new Color(1f, .38f, .18f, .4f) :
                new Color(.25f, .41f, .48f, .14f);
        }
        private void OnTriggerEnter(Collider other) { Strike(other); }
        private void OnTriggerStay(Collider other) { Strike(other); }
        private void Strike(Collider other)
        {
            if (active && other.GetComponent<Runner>() != null && RunGame.Current != null &&
                RunGame.Current.CanPlay) RunGame.Current.Hurt(1, "STEAM");
        }
    }
}
