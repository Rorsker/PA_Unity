using UnityEngine;

namespace PlayableAd
{
    [DisallowMultipleComponent]
    public sealed class KnockedEnemyPhysics : MonoBehaviour
    {
        [SerializeField, Min(0.5f), InspectorName("Active Seconds（活动时长）")] private float activeSeconds = 2.25f;
        [SerializeField, Min(0f), InspectorName("Linear Drag（线性阻力）")] private float linearDrag = 0.35f;
        [SerializeField, Min(0f), InspectorName("Angular Drag（角阻力）")] private float angularDrag = 0.8f;
        [SerializeField, Min(1f), InspectorName("Max Linear Speed（最大线速度）")] private float maxLinearSpeed = 10f;

        private Rigidbody body;
        private Collider physicsCollider;
        private float remainingSeconds;
        private bool launched;

        public bool IsLaunched => launched;

        public void Initialize(float lifetime)
        {
            activeSeconds = Mathf.Max(0.5f, lifetime);

            physicsCollider = gameObject.AddComponent<BoxCollider>();
            physicsCollider.enabled = false;

            body = gameObject.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            body.drag = linearDrag;
            body.angularDrag = angularDrag;
            body.maxAngularVelocity = 18f;
        }

        public void Launch(Vector3 velocity, Vector3 angularVelocity)
        {
            if (launched || body == null || physicsCollider == null)
            {
                return;
            }

            launched = true;
            remainingSeconds = activeSeconds;
            physicsCollider.enabled = true;
            body.isKinematic = false;
            body.useGravity = true;
            body.velocity = Vector3.ClampMagnitude(velocity, maxLinearSpeed);
            body.angularVelocity = angularVelocity;
            body.WakeUp();
        }

        private void Update()
        {
            if (!launched)
            {
                return;
            }

            remainingSeconds -= Time.deltaTime;
            if (remainingSeconds <= 0f)
            {
                gameObject.SetActive(false);
            }
        }

        private void OnDisable()
        {
            launched = false;
            if (physicsCollider != null)
            {
                physicsCollider.enabled = false;
            }

            if (body != null)
            {
                if (!body.isKinematic)
                {
                    body.velocity = Vector3.zero;
                    body.angularVelocity = Vector3.zero;
                }
                body.useGravity = false;
                body.isKinematic = true;
            }
        }
    }
}
