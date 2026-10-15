using System.Collections.Generic;
using UnityEngine;

namespace Shatterline
{
    public sealed class GearWheel : MonoBehaviour
    {
        [SerializeField] private Vector3 origin;
        [SerializeField] private Transform teeth;
        public float Horizontal = 1.4f, Vertical = .5f, Speed = 1.7f, SweepScale = 1f;
        public void Configure(Transform wheel) { teeth = wheel; origin = transform.position; }
        private void Start() { origin = transform.position; }
        private void Update()
        {
            if (RunGame.Current == null || !RunGame.Current.CanPlay) return;
            if (teeth != null) teeth.Rotate(0, 0, Time.deltaTime * 185f);
            float phase = Time.time * Speed + origin.x * .31f;
            transform.position = origin + new Vector3(Mathf.Sin(phase) * Horizontal * SweepScale,
                Mathf.Sin(phase * 1.3f) * Vertical, 0);
        }
        private void OnTriggerEnter(Collider other) { Strike(other); }
        private void OnTriggerStay(Collider other) { Strike(other); }
        private void Strike(Collider other)
        {
            if (other.GetComponent<Runner>() != null && RunGame.Current != null &&
                RunGame.Current.CanPlay) RunGame.Current.Hurt(1, "GEAR");
        }
    }
}
