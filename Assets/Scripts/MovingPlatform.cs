using System.Collections.Generic;
using UnityEngine;

namespace Shatterline
{
    [RequireComponent(typeof(Rigidbody))]
    public sealed class MovingPlatform : MonoBehaviour
    {
        public float Travel = 1.4f;
        private Vector3 start;
        private Rigidbody body;
        private void Awake()
        {
            // Child landing colliders become one kinematic compound body.
            // MovePosition lets the physics solver carry and push the runner.
            body = GetComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        }
        private void Start() { start = transform.position; }
        private void FixedUpdate()
        {
            body.MovePosition(start + Vector3.up * Mathf.Sin(Time.fixedTime * 1.5f + start.x) * Travel);
        }
    }
}
