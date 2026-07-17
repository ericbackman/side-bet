using UnityEngine;

namespace SideBet.Playtest
{
    /// <summary>
    /// A smooth, look-ahead 2D camera. Snappy framing is a big part of "buttery" feel — a camera
    /// that hard-locks to the player feels stiff; one that eases and leans in the move direction
    /// feels alive. Put this on the Main Camera and set Target = the player.
    /// </summary>
    public sealed class CameraFollow2D : MonoBehaviour
    {
        public Transform target;
        [Tooltip("Lower = snappier camera, higher = floatier/laggier.")]
        public float smoothTime = 0.15f;
        [Tooltip("How far the camera leads in the direction of motion (units).")]
        public float lookAhead = 1.5f;
        public Vector2 offset = new Vector2(0f, 1f);

        private Rigidbody2D _targetRb;
        private Vector3 _vel;
        private float _look;

        private void Start()
        {
            if (target != null) _targetRb = target.GetComponent<Rigidbody2D>();
        }

        private void LateUpdate()
        {
            if (target == null) return;

            float vx = _targetRb != null ? _targetRb.linearVelocity.x : 0f; // older Unity: .velocity
            float desiredLook = Mathf.Clamp(vx / 8f, -1f, 1f) * lookAhead;
            _look = Mathf.Lerp(_look, desiredLook, Time.deltaTime * 3f);

            Vector3 desired = new Vector3(
                target.position.x + offset.x + _look,
                target.position.y + offset.y,
                transform.position.z);
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref _vel, smoothTime);
        }
    }
}
