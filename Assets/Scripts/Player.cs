using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using Unity.Netcode;
using UnityEngine;

public class Player : NetworkBehaviour
{
    public event EventHandler OnStop;
    public event EventHandler<OnStateChangedEventArgs> OnStateChanged;
    public class OnStateChangedEventArgs : EventArgs
    {
        public States state;
        public States lastState;
    }
    public enum States
    {
        Idle,
        Moving,
        Jumping,
        Falling,
        PrepToFly,
        Flying,
    }
    public States state;
    public States lastState;
    public static Player LocalInstance { get; private set; }

    [field: Header("References")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private GameInput gameInput;
    [SerializeField] private DetectCollision detectCollision;
    [SerializeField] private Transform followTransform;


    [field: Header("Stats")]
    [SerializeField] private float maxSpeed;
    [SerializeField] private float turnSpeed;
    [SerializeField] private float acceleration;
    [SerializeField] private float moveAcceleration = 20f;
    [SerializeField] private float handleReturnSpeed = 2f;
    [SerializeField] private float jumpForce;
    [SerializeField] private float jumpDecelerationForce;
    [SerializeField] private float airborneSpeedMultiplier = .25f;
    [SerializeField] private float gravityForce = 9.81f;
    [SerializeField] private float slopeDownwardsForce = 80f;

    [field: Header("Slope Movement")]
    [SerializeField] private float maxSlopeAngle;
    [SerializeField] private RaycastHit slopeHit;

    private Vector3 targetDir;
    private float actualAccel;
    private bool jump;
    private float minVelocity = .1f;
    private float playerHeight = 2.5f;


    private CinemachineVirtualCamera cinemachineVirtualCamera;

    private void Awake()
    {
        state = States.Idle;
    }

    private void Start()
    {
        OnStop += Player_OnStop;
        GameInput.Instance.OnJump += Player_OnJump;
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            LocalInstance = this;

            CameraManager.Instance.SetPlayerCamera(out cinemachineVirtualCamera);

            Debug.Log("Player Initialized");
        }
    }

    private void Update()
    {
        rb.useGravity = !OnSlope();
        switch (state)
        {
            case States.Idle:
                if (!detectCollision.CheckGround())
                {
                    lastState = state;
                    state = States.Falling;
                    OnStateChanged?.Invoke(this, new OnStateChangedEventArgs
                    {
                        state = state,
                        lastState = lastState,
                    });
                }
                if (ReadMovementInput().magnitude > 0f)
                {
                    lastState = state;
                    state = States.Moving;
                    OnStateChanged?.Invoke(this, new OnStateChangedEventArgs
                    {
                        state = state,
                        lastState = lastState
                    });
                }
                break;
            case States.Moving:
                if (!detectCollision.CheckGround())
                {
                    lastState = state;
                    state = States.Falling;
                    OnStateChanged?.Invoke(this, new OnStateChangedEventArgs
                    {
                        state = state,
                        lastState = lastState
                    });
                }
                if (ReadMovementInput().magnitude <= 0f)
                {
                    if (detectCollision.CheckGround())
                    {
                        lastState = state;
                        state = States.Idle;
                        OnStop?.Invoke(this, EventArgs.Empty);
                        OnStateChanged?.Invoke(this, new OnStateChangedEventArgs
                        {
                            state = state,
                            lastState = lastState
                        });
                    }
                }
                break;
            case States.Jumping:
                if (jump == false)
                {
                    if (IsPlayerStoppedMovingUpwards())
                    {
                        lastState = state;
                        state = States.Falling;
                        OnStateChanged?.Invoke(this, new OnStateChangedEventArgs
                        {
                            state = state,
                            lastState = lastState
                        });
                    }
                }
                break;
            case States.Falling:
                if (detectCollision.CheckGround())
                {
                    if (ReadMovementInput().magnitude > 0f)
                    {
                        lastState = state;
                        state = States.Moving;
                        OnStateChanged?.Invoke(this, new OnStateChangedEventArgs
                        {
                            state = state,
                            lastState = lastState
                        });
                    }
                    else
                    {
                        lastState = state;
                        state = States.Idle;
                        OnStop?.Invoke(this, EventArgs.Empty);
                        OnStateChanged?.Invoke(this, new OnStateChangedEventArgs
                        {
                            state = state,
                            lastState = lastState
                        });
                    }
                }
                break;
            case States.PrepToFly:

                break;
            case States.Flying:
                
                break;
        }
        //Debug.Log(state);
        if (state != States.Flying)
        {
            Quaternion targetRot = rb.rotation;
            targetRot = Quaternion.Euler(0f, Camera.main.transform.eulerAngles.y, 0f);
            rb.MoveRotation(targetRot);
        }
    }

    private void ResetVelocity()
    {
        rb.velocity = Vector3.zero;
    }

    private void FixedUpdate()
    {
        HandleMovement();
    }

    private void HandleMovement()
    {
        if (state == States.Moving)
        {
            float speed = maxSpeed;
            float moveAccel = moveAcceleration;

            DoMove(speed, moveAccel);
            //StickToGround();
        }

        if (state == States.Jumping)
        {
            if (jump && detectCollision.CheckGround())
            {
                rb.AddForce((Vector3.up * jumpForce) + GetPlayerOrientation(), ForceMode.Impulse);
                jump = false;
            }

            if (IsPlayerMovingUpwards())
            {
                DecelerateVertically();
            }

            rb.AddForce((GetPlayerOrientation() * airborneSpeedMultiplier) - GetPlayerHorizontalVelocity(), ForceMode.Acceleration);
        }

        if (state == States.Falling)
        {
            rb.AddForce((GetPlayerOrientation() * airborneSpeedMultiplier) - GetPlayerHorizontalVelocity(), ForceMode.Acceleration);

            if (GetPlayerVerticalVelocity().y > gravityForce)
            {
                return;
            }

            rb.AddForce(0f, -gravityForce - GetPlayerVerticalVelocity().y, 0f, ForceMode.VelocityChange);
        }
    }

    private Vector3 GetPlayerOrientation()
    {
        Vector3 screenMovementForward = transform.forward;
        Vector3 screenMovementRight = transform.right;

        Vector3 horizontalMovement = screenMovementRight * ReadMovementInput().x;
        Vector3 verticalMovement = screenMovementForward * ReadMovementInput().z;

        Vector3 moveDirection = (verticalMovement + horizontalMovement).normalized;

        return moveDirection;
    }

    private void DoMove(float speed, float moveAccel)
    {
        Vector3 targetVelocity = GetPlayerOrientation() * speed;
        actualAccel = Mathf.Lerp(actualAccel, moveAccel, handleReturnSpeed * Time.fixedDeltaTime);
        Vector3 currentVelocity = rb.velocity;
        Vector3 actualMoveDirection = Vector3.Lerp(currentVelocity, targetVelocity, actualAccel * Time.fixedDeltaTime);
        actualMoveDirection.y = rb.velocity.y;
        if (OnSlope())
        {
            Vector3 slopeTargetVelocity = GetSlopeMoveDirection() * speed;
            Vector3 actualSlopeMoveDirection = Vector3.Lerp(currentVelocity, slopeTargetVelocity, actualAccel * Time.fixedDeltaTime);
            rb.velocity = actualSlopeMoveDirection;
            rb.AddForce(Vector3.down * slopeDownwardsForce, ForceMode.Force);
            return;
        }
        rb.velocity = actualMoveDirection;
    }

    private void StickToGround()
    {
        rb.AddForce(-Vector3.up - GetPlayerVerticalVelocity());
    }

    private Vector3 ReadMovementInput()
    {
        return new Vector3(gameInput.MovementInputNormalized().x, 0f, gameInput.MovementInputNormalized().y);
    }
    private void Player_OnStop(object sender, EventArgs e)
    {
        ResetVelocity();
    }
    private void DecelerateVertically()
    {
        Vector3 verticalVelocity = GetPlayerVerticalVelocity();

        rb.AddForce(-verticalVelocity * jumpDecelerationForce, ForceMode.Acceleration);
    }

    private bool IsPlayerStoppedMovingUpwards()
    {
        return GetPlayerVerticalVelocity().y <= minVelocity;
    }

    private bool IsPlayerMovingUpwards()
    {
        return GetPlayerVerticalVelocity().y > minVelocity;
    }

    private Vector3 GetPlayerVerticalVelocity()
    {
        return new Vector3(0f, rb.velocity.y, 0f);
    }
    private Vector3 GetPlayerHorizontalVelocity()
    {
        return new Vector3(rb.velocity.x, 0f, rb.velocity.z);
    }
    private void Player_OnJump(object sender, EventArgs e)
    {
        if (detectCollision.CheckGround())
        {
            lastState = state;
            state = States.Jumping;
            OnStateChanged?.Invoke(this, new OnStateChangedEventArgs
            {
                state = state,
                lastState = lastState
            });
            jump = true;
        }
    }

    private bool OnSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out slopeHit, playerHeight * .5f + .3f))
        {
            float angle = Vector3.Angle(Vector3.up, slopeHit.normal);
            return angle < maxSlopeAngle && angle != 0;
        }
        return false;
    }

    private Vector3 GetSlopeMoveDirection()
    {
        return Vector3.ProjectOnPlane(GetPlayerOrientation(), slopeHit.normal).normalized;
    }

    public Transform GetFollowTransform()
    {
        return followTransform;
    }
}
