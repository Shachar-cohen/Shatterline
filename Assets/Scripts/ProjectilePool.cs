using System.Collections.Generic;
using UnityEngine;

namespace Shatterline
{
    public sealed class ProjectilePool : MonoBehaviour
    {
        public static ProjectilePool Current;
        [SerializeField] private List<Shot> shots = new List<Shot>();
        [SerializeField] private Material playerMat, enemyMat;
        private void Awake()
        {
            Current = this;
            foreach (var shot in shots)
            {
                if (shot == null) throw new System.InvalidOperationException("Projectile pool has a missing saved shot.");
                shot.Pool = this; shot.gameObject.SetActive(false);
            }
        }

#if UNITY_EDITOR
        public void BuildInEditor()
        {
            playerMat = StageBuilder.Material(new Color(.2f, .92f, 1f));
            enemyMat = StageBuilder.Material(new Color(1f, .3f, .18f));
            for (int i = 0; i < 32; i++)
            {
                var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.name = "Recycled projectile " + i;
                sphere.transform.SetParent(transform, false);
                sphere.transform.localScale = Vector3.one * .2f;
                sphere.GetComponent<Renderer>().sharedMaterial = playerMat;
                var trail = sphere.AddComponent<TrailRenderer>();
                trail.time = .13f;
                trail.startWidth = .15f;
                trail.endWidth = 0;
                trail.minVertexDistance = .045f;
                trail.sharedMaterial = playerMat;
                sphere.GetComponent<SphereCollider>().isTrigger = true;
                var rb = sphere.AddComponent<Rigidbody>(); rb.isKinematic = true; rb.useGravity = false;
                var shot = sphere.AddComponent<Shot>(); shot.Pool = this;
                shots.Add(shot); sphere.SetActive(false);
            }
        }
#endif
        public int Capacity { get { return shots.Count; } }
        public void Fire(Vector3 origin, Vector3 direction, bool hostile, int damage = 1)
        {
            foreach (var shot in shots)
            {
                if (shot.gameObject.activeSelf) continue;
                shot.transform.position = origin;
                shot.GetComponent<Renderer>().sharedMaterial = hostile ? enemyMat : playerMat;
                var streak = shot.GetComponent<TrailRenderer>();
                streak.sharedMaterial = hostile ? enemyMat : playerMat;
                streak.Clear();
                shot.Activate(direction, hostile, damage);
                return;
            }
        }
        private void OnDestroy() { if (Current == this) Current = null; }
    }
}
