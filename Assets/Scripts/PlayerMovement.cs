using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PlayerMovement : MonoBehaviour
{
    private enum State { Grounded, Jumping, Falling, Dashing }
    [SerializeField] private State state = State.Grounded;

    [Header("Movement Settings")]
    [SerializeField, Range(0f, 20f)] private float maxSpeed = 9f;
    [SerializeField, Range(0f, 100f)] private float maxAcceleration = 30f;
    [SerializeField, Range(0f, 100f)] private float maxDecceleration = 10f;
    [SerializeField, Range(0f, 100f)] private float maxTurnSpeed = 80f;
    [SerializeField, Range(0f, 100f)] private float maxAirAcceleration = 30f;
    [SerializeField, Range(0f, 100f)] private float maxAirDeceleration = 10f;
    [SerializeField, Range(0f, 100f)] private float maxAirTurnSpeed = 80f;

    [Header("Jump Settings")]
    [SerializeField, Range(0f, 5.0f)] private float jumpHeight = 2f;
    [SerializeField, Range(0f, 0.3f)] private float coyoteTime = 0.15f;
    [SerializeField, Range(0f, 0.3f)] private float jumpBuffer = 0.15f;

    [Header("Gravity Settings")]
    [SerializeField, Range(0.2f, 1.25f)] private float upwardMultiplier = 1f;
    [SerializeField, Range(1f, 10f)] private float downwardMultiplier = 6f;
    [SerializeField, Range(1f, 10f)] private float jumpCutOff = 3f;
    [SerializeField] private float verticalSpeedLimit = 20f;

    [Header("Dash Settings")]
    [SerializeField] private float dashForce = 20f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 1f;

    private Vector2 inputDirection;
    private Vector3 velocity;
    private Vector3 desiredVelocity;

    private Rigidbody rb;
    private GroundCheck ground;
    private InputAction jumpAction;
    private InputAction dashAction;

    private bool pressingJump;
    private bool jumpQueued;
    private float lastGroundedTime;
    private float lastJumpInputTime;

    private bool dashQueued;
    private bool dashReady;

    // Awake
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        ground = GetComponent<GroundCheck>();

        jumpAction = GetComponent<PlayerInput>().actions["Jump"];
        jumpAction.started += ctx => { pressingJump = true; lastJumpInputTime = Time.time; jumpQueued = true; };
        jumpAction.canceled += ctx => { pressingJump = false; };

        dashAction = GetComponent<PlayerInput>().actions["Dash"];
        dashAction.started += ctx => { dashQueued = true; };

        dashReady = true;
    }

    // OnMove
    public void OnMove(InputValue movementValue)
    {
        inputDirection = movementValue.Get<Vector2>().normalized;
    }

    // Update
    private void Update()
    {
        if (state != State.Dashing)
        {
            if (ground.IsGrounded)
            {
                lastGroundedTime = Time.time;
                if (state != State.Jumping)
                {
                    state = State.Grounded;
                }
            }
            else
            {
                if (state != State.Jumping)
                {
                    state = State.Falling;
                }
            }
        }
    }

    // FixedUpdate
    private void FixedUpdate()
    {
        ApplyGravity();

        UpdateDirectionWithCamera();

        velocity = rb.linearVelocity;
        
        if (state != State.Dashing)
        {
            Move();

            if (jumpQueued)
            {
                bool coyoteAllowed = Time.time - lastGroundedTime <= coyoteTime;
                bool bufferAllowed = Time.time - lastJumpInputTime <= jumpBuffer;

                if ((ground.IsGrounded || coyoteAllowed) && bufferAllowed)
                {
                    if (state != State.Jumping)
                    {
                        Jump();
                    }
                    jumpQueued = false;
                }
            }
        }

        if (dashQueued)
        {
            if (state != State.Dashing && dashReady)
            {
                StartCoroutine(Dash());
            }
            dashQueued = false;
        }
    }

    // ApplyGravity
    private void ApplyGravity()
    {
        Vector3 velocity = rb.linearVelocity;

        switch (state)
        {
            case State.Jumping:
                if (velocity.y <= 0f) state = State.Falling;
                velocity.y += Physics.gravity.y * (pressingJump ? upwardMultiplier : jumpCutOff) * Time.fixedDeltaTime;
                break;

            case State.Falling:
                velocity.y += Physics.gravity.y * downwardMultiplier * Time.fixedDeltaTime;
                break;

            case State.Grounded:
                velocity.y = -2f;
                break;

            case State.Dashing:
                velocity.y = 0f;
                break;
        }

        velocity.y = Mathf.Clamp(velocity.y, -verticalSpeedLimit, verticalSpeedLimit);
        rb.linearVelocity = velocity;
    }

    // UpdateDirectionWithCamera
    private void UpdateDirectionWithCamera()
    {
        if (inputDirection == Vector2.zero)
        {
            desiredVelocity = Vector3.zero;
            return;
        }

        Transform cam = Camera.main.transform;

        Vector3 camForward = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
        Vector3 camRight = Vector3.ProjectOnPlane(cam.right, Vector3.up).normalized;

        Vector3 moveDir = camForward * inputDirection.y + camRight * inputDirection.x;
        desiredVelocity = moveDir * maxSpeed;
    }

    // Move
    private void Move()
    {
        bool onGround = ground.IsGrounded;

        float acceleration = onGround ? maxAcceleration : maxAirAcceleration;
        float deceleration = onGround ? maxDecceleration : maxAirDeceleration;
        float turnSpeed = onGround ? maxTurnSpeed : maxAirTurnSpeed;

        float maxSpeedChange;

        if (inputDirection != Vector2.zero)
        {
            Vector3 moveDir = new Vector3(inputDirection.x, 0f, inputDirection.y);
            Vector3 currentDir = new Vector3(velocity.x, 0f, velocity.z);

            maxSpeedChange = Vector3.Dot(moveDir, currentDir) < 0f ? turnSpeed * Time.fixedDeltaTime : acceleration * Time.fixedDeltaTime;
        }
        else
        {
            maxSpeedChange = deceleration * Time.fixedDeltaTime;
        }

        velocity.x = Mathf.MoveTowards(velocity.x, desiredVelocity.x, maxSpeedChange);
        velocity.z = Mathf.MoveTowards(velocity.z, desiredVelocity.z, maxSpeedChange);

        rb.linearVelocity = velocity;
    }

    // Jump
    private void Jump()
    {
        state = State.Jumping;

        float jumpSpeed = Mathf.Sqrt(-2f * Physics.gravity.y * jumpHeight);

        Vector3 velocity = rb.linearVelocity;
        if (velocity.y > 0f)
        {
            jumpSpeed = Mathf.Max(jumpSpeed - velocity.y, 0f);
        }
        else if (velocity.y < 0f)
        { 
            jumpSpeed += Mathf.Abs(velocity.y); 
        }

        velocity.y += jumpSpeed;
        rb.linearVelocity = velocity;
    }

    // Dash
    private IEnumerator Dash()
    {
        state = State.Dashing;

        dashReady = false;

        rb.linearVelocity = Vector3.zero;

        Vector3 dir = desiredVelocity.normalized;

        if (dir != Vector3.zero)
        {
            rb.AddForce(dir * dashForce, ForceMode.VelocityChange);
        }
        else
        {
            Transform cam = Camera.main.transform;
            Vector3 camForward = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
            rb.AddForce(camForward * dashForce, ForceMode.VelocityChange);
        }

        yield return new WaitForSeconds(dashDuration);

        if (ground.IsGrounded)
            state = State.Grounded;
        else
            state = State.Falling;

        yield return new WaitForSeconds(dashCooldown);

        dashReady = true;
    }
}
