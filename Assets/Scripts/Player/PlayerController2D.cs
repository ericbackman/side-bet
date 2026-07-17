using UnityEngine;
using UnityEngine.InputSystem; // new Input System (your project's input package) — reads the keyboard directly, no action-asset wiring needed

namespace SideBet.Playtest
{
    /// <summary>
    /// A tunable 2D platformer movement controller built for FEEL. Every "buttery" knob is
    /// exposed in the Inspector so you can tweak it live while playing. It uses a dynamic
    /// Rigidbody2D for collision but drives velocity itself ("kinematic-style control"),
    /// which is how tight-feeling platformers (Celeste, etc.) work.
    ///
    /// Setup: put this on a GameObject with a Rigidbody2D + a Collider2D (Capsule recommended).
    /// See MOVEMENT-PLAYTEST.md for the full scene setup + a tuning recipe.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    public sealed class PlayerController2D : MonoBehaviour
    {
        [Header("Run")]
        [Tooltip("Top horizontal speed (units/sec).")]
        public float maxSpeed = 9f;
        [Tooltip("Ground acceleration toward top speed. Higher = snappier starts.")]
        public float groundAccel = 90f;
        [Tooltip("Ground deceleration when no input. Higher = less ice-skating.")]
        public float groundDecel = 110f;
        [Tooltip("Air acceleration (how much control you have mid-jump).")]
        public float airAccel = 55f;
        [Tooltip("Air deceleration when no input.")]
        public float airDecel = 35f;

        [Header("Jump (designer-friendly: set height + time, gravity is derived)")]
        [Tooltip("Peak jump height in world units (full hold).")]
        public float jumpHeight = 3.2f;
        [Tooltip("Seconds to reach the apex. Smaller = snappier + heavier.")]
        public float timeToApex = 0.38f;
        [Tooltip("Gravity multiplier while falling. >1 = snappier descent.")]
        public float fallGravityMult = 1.7f;
        [Tooltip("Extra gravity when jump is released early = variable jump height.")]
        public float jumpCutMult = 2.0f;
        [Tooltip("Terminal fall speed.")]
        public float maxFallSpeed = 24f;

        [Header("Assists (the 'butter')")]
        [Tooltip("You can still jump this long after walking off a ledge (sec).")]
        public float coyoteTime = 0.10f;
        [Tooltip("A jump pressed this long before landing still fires (sec).")]
        public float jumpBuffer = 0.12f;
        [Tooltip("Near the apex (|vy| below this), gravity is reduced for a floaty hang. 0 = off.")]
        public float apexHangThreshold = 1.5f;
        [Range(0f, 1f)]
        [Tooltip("Gravity scale during the apex hang.")]
        public float apexHangGravityScale = 0.5f;

        [Header("Ground check")]
        public LayerMask groundLayer = ~0;
        [Tooltip("How far below the collider to probe for ground.")]
        public float groundCheckDist = 0.08f;

        private Rigidbody2D _rb;
        private Collider2D _col;
        private ContactFilter2D _groundFilter;
        private readonly RaycastHit2D[] _hits = new RaycastHit2D[4];

        private float _gravity;       // derived from jumpHeight + timeToApex
        private float _jumpVelocity;  // derived
        private float _moveX;
        private float _coyoteCounter;
        private float _bufferCounter;
        private bool _grounded;
        private bool _jumpHeld;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _col = GetComponent<Collider2D>();
            _rb.gravityScale = 0f;                 // we apply our own gravity
            _rb.freezeRotation = true;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate; // smooth visuals = butter
            _groundFilter.useLayerMask = true;
            _groundFilter.SetLayerMask(groundLayer);
            _groundFilter.useTriggers = false;
            RecalcJump();
        }

        // Recompute whenever you tweak values in the Inspector during play.
        private void OnValidate() => RecalcJump();

        // h = ½·g·t²  ->  g = 2h/t² ;  v = g·t. Tune in feelable units, not raw forces.
        private void RecalcJump()
        {
            if (timeToApex <= 0f) return;
            _gravity = (2f * jumpHeight) / (timeToApex * timeToApex);
            _jumpVelocity = _gravity * timeToApex;
        }

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return; // no keyboard (e.g., headless)

            float right = (kb.dKey.isPressed || kb.rightArrowKey.isPressed) ? 1f : 0f;
            float left = (kb.aKey.isPressed || kb.leftArrowKey.isPressed) ? 1f : 0f;
            _moveX = right - left;

            bool jumpPressed = kb.spaceKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame || kb.upArrowKey.wasPressedThisFrame;
            _jumpHeld = kb.spaceKey.isPressed || kb.wKey.isPressed || kb.upArrowKey.isPressed;

            // Buffer the jump in Update so a press between physics steps isn't lost.
            if (jumpPressed) _bufferCounter = jumpBuffer;
            else _bufferCounter -= Time.deltaTime;
        }

        private void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            _grounded = IsGrounded();
            _coyoteCounter = _grounded ? coyoteTime : _coyoteCounter - dt;

            Vector2 v = _rb.linearVelocity; // Unity 6 API; on older Unity use _rb.velocity

            // ---- horizontal: accelerate toward target, decelerate to stop ----
            float target = _moveX * maxSpeed;
            bool accelerating = Mathf.Abs(target) > 0.01f;
            float rate = _grounded ? (accelerating ? groundAccel : groundDecel)
                                   : (accelerating ? airAccel : airDecel);
            v.x = Mathf.MoveTowards(v.x, target, rate * dt);

            // ---- jump: buffered press + coyote window ----
            if (_bufferCounter > 0f && _coyoteCounter > 0f)
            {
                v.y = _jumpVelocity;
                _bufferCounter = 0f;
                _coyoteCounter = 0f;
            }

            // ---- gravity: asymmetric (heavier falling), variable height, floaty apex ----
            float g = _gravity;
            if (v.y < 0f) g *= fallGravityMult;                 // snappier descent
            else if (v.y > 0f && !_jumpHeld) g *= jumpCutMult;  // released early -> cut the jump short
            if (!_grounded && apexHangThreshold > 0f && Mathf.Abs(v.y) < apexHangThreshold)
                g *= apexHangGravityScale;                       // brief hang at the top
            v.y -= g * dt;
            v.y = Mathf.Max(v.y, -maxFallSpeed);

            // keep planted on the ground without accumulating downward velocity
            if (_grounded && v.y < 0f) v.y = -2f;

            _rb.linearVelocity = v;
        }

        private bool IsGrounded() => _col.Cast(Vector2.down, _groundFilter, _hits, groundCheckDist) > 0;
    }
}
