using System.Collections.Generic;
using UnityEngine;

namespace Shatterline
{
    public sealed class Shot : MonoBehaviour
    {
        public ProjectilePool Pool;
        private Vector3 velocity;
        private float expiry;
        private bool hostile;
        private int damage;
        public void Activate(Vector3 direction, bool enemy, int strength)
        {
            damage = strength;
            hostile = enemy; velocity = direction.normalized * (enemy ? 9.5f : 24f);
            // A visible guard can shoot across the whole side camera, not just
            // the older seven-unit attack radius. Give its projectile time to cross.
            expiry = Time.time + (enemy ? 3.2f : 2.1f); gameObject.SetActive(true);
        }
        private void Update()
        {
            if (RunGame.Current == null || !RunGame.Current.CanPlay) return;
            if (hostile && DroneCompanion.Current != null &&
                DroneCompanion.Current.TryIntercept(transform.position, velocity))
            { gameObject.SetActive(false); return; }
            transform.position += velocity * Time.deltaTime;
            if (Time.time > expiry) gameObject.SetActive(false);
        }
        private void OnTriggerEnter(Collider other)
        {
            if (!gameObject.activeSelf) return;
            if (hostile)
            {
                var station = other.GetComponent<SignalStation>();
                if (station != null && RunGame.Current != null && RunGame.Current.DamageUploadStation(station))
                {
                    RunGame.Burst(transform.position, Color.red);
                    gameObject.SetActive(false);
                    return;
                }
                if (other.GetComponent<Runner>() != null)
                {
                    RunGame.Current.Hurt(1, "ENEMY FIRE", transform.position.x);
                    RunGame.Burst(transform.position, Color.red);
                }
                else if (other.isTrigger || other.GetComponent<Sentinel>() != null ||
                         other.GetComponent<Shot>() != null) return;
                // Solid cover now stops hostile shots; the player's own body and
                // enemy colliders do not consume them before they leave the muzzle.
            }
            else
            {
                if (other.GetComponent<Runner>() != null) return;
                var target = other.GetComponent<Sentinel>();
                if (target != null) target.TakeDamage(damage);
                var turret = other.GetComponent<SentryTurret>();
                if (turret != null)
                {
                    turret.TakeDamage(damage);
                    RunGame.Burst(transform.position, new Color(1f, .67f, .2f));
                    gameObject.SetActive(false);
                    return;
                }
                var relay = other.GetComponent<PowerRelay>();
                if (relay != null) relay.TakeDamage(damage + RunGame.Current.RelayShotBonus);
                var wall = other.GetComponent<Breakable>();
                if (wall != null) wall.Shatter();
                var mine = other.GetComponent<ProximityMine>();
                if (mine != null)
                {
                    mine.Disarm();
                    gameObject.SetActive(false);
                    return;
                }
                if (other.GetComponent<Shot>() != null || other.isTrigger) return;
                RunGame.Burst(transform.position, Color.cyan);
            }
            gameObject.SetActive(false);
        }
    }
}
