using System.Collections.Generic;
using UnityEngine;

namespace Shatterline
{
    public sealed class SentryTurret : MonoBehaviour
    {
        [SerializeField] private RunGame game;
        [SerializeField] private Renderer eye;
        private float nextShot;
        private int health = 3;
        private bool destroyed;
        public void Configure(RunGame run, Renderer warningEye)
        { game = run; eye = warningEye; nextShot = Time.time + 1.7f; }
        public void InitializeRuntime(RunGame run) { Configure(run, eye); }
        public void Relocate(float x)
        {
            transform.position = new Vector3(x, transform.position.y, 0);
            health = 3; destroyed = false; nextShot = Time.time + 1.8f;
        }
        private void Update()
        {
            if (game == null || !game.CanPlay || destroyed) return;
            var cam = Camera.main;
            if (cam == null || ProjectilePool.Current == null) return;
            Vector3 view = cam.WorldToViewportPoint(transform.position);
            if (view.z < 0 || view.x < .04f || view.x > .96f) return;
            var player = game.Player;
            if (player == null || Mathf.Abs(player.transform.position.x - transform.position.x) > 14f)
                return;
            Vector3 origin = transform.position + Vector3.down * .2f;
            Vector3 destination = player.transform.position + Vector3.up *
                (player.IsCrouching ? .58f : 1f);
            Vector3 path = destination - origin;
            foreach (RaycastHit hit in Physics.RaycastAll(origin, path.normalized, path.magnitude,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                if (hit.collider.GetComponent<SentryTurret>() == null &&
                    hit.collider.GetComponent<Sentinel>() == null &&
                    hit.collider.GetComponent<Runner>() == null) return;
            if (eye != null) eye.material.color = nextShot - Time.time < .45f ?
                Color.yellow : new Color(1f, .25f, .13f);
            if (Time.time < nextShot) return;
            nextShot = Time.time + (game.Level == 2 ? 2.55f : 2.05f);
            ProjectilePool.Current.Fire(origin, path.normalized, true);
            RunGame.Burst(origin, new Color(1f, .42f, .15f));
        }
        public void TakeDamage(int damage)
        {
            if (destroyed) return;
            health -= damage;
            if (eye != null) eye.material.color = Color.white;
            if (health > 0) return;
            destroyed = true;
            Collider body = GetComponent<Collider>();
            if (body != null) body.enabled = false;
            if (game != null) game.DefeatEnemy(125);
            RunGame.Burst(transform.position, new Color(1f, .56f, .12f));
            Destroy(gameObject, .12f);
        }
    }
}
