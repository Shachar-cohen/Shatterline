using System.Collections.Generic;
using UnityEngine;

namespace Shatterline
{
    public sealed class Fragment : MonoBehaviour
    {
        public bool Healing;
        public int SignalIndex = -1;
        public bool Collected { get { return collected; } }
        public bool Carried { get; private set; }
        public bool Damaged { get; private set; }
        public bool Recovered { get; private set; }
        private float startY;
        private bool collected;
        private float pickupReady;
        private void Start() { startY = transform.position.y; }
        private void Update()
        {
            transform.Rotate(0, Time.deltaTime * 80, 0);
            transform.position = new Vector3(transform.position.x,
                startY + Mathf.Sin(Time.time * 2f + startY) * .22f, transform.position.z);
        }
        public void MoveToCache(Vector3 location)
        {
            if (collected || Carried || Healing || Recovered) return;
            Recovered = true;
            transform.position = location;
            startY = location.y;
        }
        public void PlaceForNewLife(Vector3 location)
        {
            if (collected || Carried || Damaged || Healing) return;
            Recovered = false;
            transform.position = location;
            startY = location.y;
        }
        public void Collect()
        {
            if (collected || Carried || Time.time < pickupReady || RunGame.Current == null) return;
            if (!Healing)
            {
                if (!RunGame.Current.TakeSignal(this)) return;
                Carried = true;
                Damaged = false;
                gameObject.SetActive(false);
                return;
            }
            collected = true;
            RunGame.Current.Pickup(true);
            RunGame.Burst(transform.position, Color.green);
            Destroy(gameObject);
        }
        public void Drop(Vector3 location)
        {
            Carried = false;
            Damaged = true;
            Recovered = false;
            pickupReady = Time.time + .9f;
            transform.position = location;
            startY = location.y;
            gameObject.SetActive(true);
            RunGame.Burst(location, Color.red);
        }
        public void Deliver()
        {
            collected = true;
            Carried = false;
            Destroy(gameObject);
        }
    }
}
