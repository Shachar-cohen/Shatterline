using System.Collections.Generic;
using UnityEngine;

namespace Shatterline
{
    public sealed class SawBlade : MonoBehaviour
    {
        [SerializeField] private Transform teeth;
        [SerializeField] private Vector3 initialPosition;
        [SerializeField] private float travel;
        [SerializeField] private float sideTravel;
        private bool warned;
        public void Initialize(Transform wheel, float verticalTravel, float horizontalTravel = 0f)
        {
            teeth = wheel;
            travel = verticalTravel;
            sideTravel = horizontalTravel;
            initialPosition = transform.position;
        }
        private void Start() { initialPosition = transform.position; }
        private void Update()
        {
            if (RunGame.Current == null || !RunGame.Current.CanPlay) return;
            if (teeth != null) teeth.Rotate(0, 0, Time.deltaTime * 275f);
            if (!warned && RunGame.Current.Player != null &&
                Mathf.Abs(RunGame.Current.Player.transform.position.x - initialPosition.x) < 3.4f &&
                Mathf.Abs(RunGame.Current.Player.transform.position.y - initialPosition.y) < 4f)
            {
                warned = true;
                RunGame.Current.Flash("ROTATING SAW / JUMP OVER OR CROUCH UNDER LOW ONES", 2.5f);
            }
        }
        private void FixedUpdate()
        {
            if (RunGame.Current == null || !RunGame.Current.CanPlay) return;
            float phase = Time.time * 2.5f + initialPosition.x;
            transform.position = initialPosition + Vector3.up * (Mathf.Sin(phase) * travel) +
                Vector3.right * (Mathf.Sin(phase * .75f) * sideTravel);
        }
        private void OnTriggerEnter(Collider other) { Cut(other); }
        private void OnTriggerStay(Collider other) { Cut(other); }
        private void Cut(Collider other)
        {
            if (RunGame.Current != null && RunGame.Current.CanPlay &&
                other.GetComponent<Runner>() != null)
                RunGame.Current.Hurt(1, "SAW");
        }
    }
}
