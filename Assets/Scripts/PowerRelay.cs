using System.Collections.Generic;
using UnityEngine;

namespace Shatterline
{
    public sealed class PowerRelay : MonoBehaviour
    {
        private int hp = 3;
        private bool dead;
        public void TakeDamage(int damage)
        {
            if (dead) return;
            hp -= damage;
            GetComponent<Renderer>().material.color = hp > 0 ? Color.yellow : Color.gray;
            if (hp > 0) return;
            dead = true;
            var collider = GetComponent<Collider>();
            if (collider != null) collider.enabled = false;
            RunGame.Burst(transform.position, Color.magenta);
            RunGame.Current.BreakRelay();
            Destroy(gameObject, .22f);
        }
    }
}
