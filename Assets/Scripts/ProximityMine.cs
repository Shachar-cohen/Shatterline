using System.Collections.Generic;
using UnityEngine;

namespace Shatterline
{
    public sealed class ProximityMine : MonoBehaviour
    {
        private float armedAt;
        private bool exploding;
        private Renderer shell;
        private void Awake()
        {
            shell = GetComponent<Renderer>();
        }
        private void Update()
        {
            if (RunGame.Current == null || !RunGame.Current.CanPlay || exploding) return;
            var player = RunGame.Current.Player;
            float distance = Vector3.Distance(transform.position, player.transform.position + Vector3.up * .35f);
            if (distance < 2.5f && armedAt <= 0)
            {
                armedAt = Time.time + .8f;
                RunGame.Current.Flash("MINE ARMED  /  SHOOT IT OR TIME YOUR DASH", .85f);
            }
            if (armedAt > 0)
            {
                shell.material.color = Mathf.Sin(Time.time * 25) > 0 ? Color.red : Color.yellow;
                if (Time.time >= armedAt) Explode();
            }
        }
        private void Explode()
        {
            if (exploding) return;
            exploding = true;
            RunGame.Burst(transform.position, new Color(1f, .42f, .1f));
            RunGame.Current.Shake(.26f);
            if (Vector3.Distance(RunGame.Current.Player.transform.position,
                    transform.position) < 2.25f)
                RunGame.Current.Hurt(1, "MINE");
            Destroy(gameObject);
        }
        public void Disarm()
        {
            if (exploding) return;
            exploding = true;
            RunGame.Current.DefeatEnemy(75);
            RunGame.Burst(transform.position, Color.cyan);
            Destroy(gameObject);
        }
    }
}
