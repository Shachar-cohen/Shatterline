using System.Collections.Generic;
using UnityEngine;

namespace Shatterline
{
    public sealed class FallingDebris : MonoBehaviour
    {
        private bool released;
        private float startingY;
        private void Start() { startingY = transform.position.y; }
        private void Update()
        {
            if (RunGame.Current == null || !RunGame.Current.CanPlay) return;
            var player = RunGame.Current.Player;
            if (!released && Mathf.Abs(player.transform.position.x - transform.position.x) < 2.8f)
            {
                released = true;
                RunGame.Current.Flash("FALLING DEBRIS  /  MOVE!", 1.1f);
            }
            if (!released) return;
            transform.position += Vector3.down * (2.8f + (startingY - transform.position.y) * 1.9f) * Time.deltaTime;
            if (transform.position.y < -3f) Destroy(gameObject);
        }
        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<Runner>() == null) return;
            RunGame.Current.Hurt(1, "FALLING ROCK");
            RunGame.Burst(transform.position, new Color(.9f, .45f, .2f));
            Destroy(gameObject);
        }
    }
}
