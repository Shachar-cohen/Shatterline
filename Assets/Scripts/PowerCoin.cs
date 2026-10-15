using System.Collections.Generic;
using UnityEngine;

namespace Shatterline
{
    public sealed class PowerCoin : MonoBehaviour
    {
        private float startY;
        private bool collected;
        private void Start() { startY = transform.position.y; }
        private void Update()
        {
            transform.Rotate(0, Time.deltaTime * 105f, 0);
            transform.position = new Vector3(transform.position.x,
                startY + Mathf.Sin(Time.time * 2.7f + transform.position.x) * .08f,
                transform.position.z);
        }
        public void Collect()
        {
            if (collected || RunGame.Current == null) return;
            collected = true;
            RunGame.Current.CollectCoin();
            RunGame.Burst(transform.position, new Color(1f, .73f, .16f));
            Destroy(gameObject);
        }
    }
}
