using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine;

public class Player : NetworkBehaviour, ITargetable
{
    public event EventHandler OnStop;

    public event EventHandler<OnStateChangedEventArgs> OnStateChanged;

    public event EventHandler OnDestroyed;
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
    private States state;
    public States lastState;
    public static Player LocalInstance { get; private set; }

    [field: Header("References")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private GameInput gameInput;
    [SerializeField] private DetectCollision detectCollision;
    [SerializeField] private Transform followTransform;
    [SerializeField] private Image targetSprite;


    [field: Header("Stats")]
    [SerializeField] private float maxSpeed;
    [SerializeField] private float prepToFlyMaxSpeed = 10f;
    [SerializeField] private float flyMaxSpeed = 20f;
    [SerializeField] private float turnSpeed;
    [SerializeField] private float acceleration;
    [SerializeField] private float moveAcceleration = 20f;
    [SerializeField] private float handleReturnSpeed = 2f;
    [SerializeField] private float jumpForce;
    [SerializeField] private float jumpDecelerationForce;
    [SerializeField] private float airborneSpeedMultiplier = .25f;
    [SerializeField] private float gravityForce = 9.81f;
    [SerializeField] private float slopeDownwardsForce = 80f;
    [SerializeField] private float maxTargetingRange = 22.5f;
    [SerializeField] private float maxTargetPixelRadius = 150f;

    [field: Header("Slope Movement")]
    [SerializeField] private float maxSlopeAngle;
    [SerializeField] private RaycastHit slopeHit;

    private Vector3 targetDir;
    private float actualAccel;
    private bool jump;
    private float minVelocity = .1f;
    private float playerHeight = 2.5f;
    private float prepToFlyTimer = .3f;
    private float jumpBufferTimer = .1f;

    private List<Transform> playerTargets;
    private int maxTargets = 10;
    private Transform target;

    private void Awake()
    {
        state = States.Idle;

        playerTargets = new List<Transform>();
    }

    private void Start()
    {
        OnStop += Player_OnStop;
        GameInput.Instance.OnJump += Player_OnJump;
        GameInput.Instance.OnBoost += Player_OnBoost;
        AscalonGameMultiplayer.Instance.OnPlayerDestroyed += AscalonGameMultiplayer_OnPlayerDestroyed;

        Cursor.lockState = CursorLockMode.Locked;
    }

    private void AscalonGameMultiplayer_OnPlayerDestroyed(object sender, EventArgs e)
    {
        playerTargets.Clear();
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            LocalInstance = this;

            CameraManager.Instance.SetPlayerCamera();

            Debug.Log("Player Initialized");
        }
    }

    public override void OnDestroy()
    {
        AscalonGameMultiplayer.Instance.PlayerDestroyed();
        OnDestroyed?.Invoke(this, EventArgs.Empty);
    }

    private void Update()
    {
        if (!IsOwner)
        {
            return;
        }

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
                jumpBufferTimer -= Time.deltaTime;
                
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

                if (jumpBufferTimer <= 0f)
                {
                    float jumpBufferTimerMax = 0.1f;

                    jumpBufferTimer = jumpBufferTimerMax;

                    OnGroundCheck();
                }

                break;
            case States.Falling:
                OnGroundCheck();
                break;
            case States.PrepToFly:

                OnGroundCheck();

                prepToFlyTimer -= Time.deltaTime;

                SetFlightRotationConstraint();

                Quaternion targetRot = Quaternion.LookRotation(Camera.main.transform.forward);

                rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, 1 - Mathf.Exp(-10f * Time.deltaTime)));

                if (prepToFlyTimer <= 0)
                {
                    float prepToFlyTimerMax = .35f;

                    prepToFlyTimer = prepToFlyTimerMax;

                    lastState = state;

                    state = States.Flying;

                    OnStateChanged?.Invoke(this, new OnStateChangedEventArgs
                    {
                        state = state,
                        lastState = lastState
                    });
                }

                break;
            case States.Flying:

                rb.MoveRotation(FlightTargetRotation());

                if (target == null)
                {

                }
                else
                {
                    Vector3 normalDirection = transform.position - target.position;
                    Vector3 xDirection = Vector3.Cross(Vector3.up, normalDirection);
                    Vector3 yDirection = Vector3.Cross(xDirection, normalDirection);

                    Vector3 lookDirection = (xDirection + yDirection).normalized;

                }

                OnGroundCheck();

                break;
        }
        //Debug.Log(state);
        if (state != States.Flying)
        {
            Quaternion targetRot = Quaternion.Euler(0f, Camera.main.transform.eulerAngles.y, 0f);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, 1 - Mathf.Exp(-10f * Time.deltaTime)));
        }


        UpdatePlayerTargetList();

        if (playerTargets.Count > 0)
        {
            target = playerTargets[TargetIndex()];
        }
        else
        {
            target = null;
        }
    }

    private int TargetIndex()
    {
        float[] distances = new float[playerTargets.Count];

        for (int i = 0; i < playerTargets.Count; i++)
        {
            distances[i] = Vector2.Distance(Camera.main.WorldToScreenPoint(playerTargets[i].position), GetScreenCenterSpace());
        }

        float minDistance = Mathf.Min(distances);
        int index = 0;

        for (int i = 0; i < distances.Length; i++)
        {
            if (minDistance == distances[i])
            {
                index = i;
            }
        }

        return index;
    }

    private void ResetVelocity()
    {
        rb.velocity = Vector3.zero;
    }

    private void FixedUpdate()
    {
        if (IsOwner)
        {
            HandleMovement();
        }
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

        if (state == States.PrepToFly)
        {
            if (rb.velocity.magnitude > prepToFlyMaxSpeed)
            {
                rb.velocity = rb.velocity.normalized;
                rb.velocity *= prepToFlyMaxSpeed;
            }
        }

        if (state == States.Flying)
        {
            rb.velocity = FlightTargetRotation() * Vector3.up * flyMaxSpeed;
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
        if (IsOwner)
        {
            ResetVelocity();
        }
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

    private void Player_OnBoost(object sender, EventArgs e)
    {
        if (!detectCollision.CheckGround())
        {
            lastState = state;
            state = States.PrepToFly;
            OnStateChanged?.Invoke(this, new OnStateChangedEventArgs
            {
                state = state,
                lastState = lastState
            });
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

    private Quaternion FlightTargetRotation()
    {
        Quaternion flightTargetRot = Quaternion.LookRotation(-Camera.main.transform.up, Camera.main.transform.forward);

        return flightTargetRot;
    }

    private void SetFlightRotationConstraint()
    {
        rb.constraints = RigidbodyConstraints.FreezeRotationZ;
    }

    private void SetGroundRotationConstraint()
    {
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    private void OnGroundCheck()
    {
        if (detectCollision.CheckGround())
        {
            rb.rotation = Quaternion.Euler(0f, rb.rotation.eulerAngles.y, 0f);

            SetGroundRotationConstraint();

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
    }

    public States GetPlayerState()
    {
        return state;
    }

    public void AddPlayerToTargetList(Transform target)
    {
        if (!playerTargets.Contains(target))
        {
            if (IsEnemyRendererVisible(target))
            {
                if (Vector2.Distance(Camera.main.WorldToScreenPoint(target.position), GetScreenCenterSpace()) <= (Screen.height / 4))
                {
                    playerTargets.Add(target);

                    Debug.Log("Target added to list");

                    AscalonGameMultiplayer.Instance.OnTargetAdd(target);
                }
            }
        }
    }

    public void UpdatePlayerTargetList()
    {

        for (int i = 0; i < playerTargets.Count; i++)
        {
            float distanceToTarget = Vector3.Distance(playerTargets[i].position, transform.position);

            if (distanceToTarget > maxTargetingRange && playerTargets.Contains(playerTargets[i]))
            {
                AscalonGameMultiplayer.Instance.OnTargetRemove(playerTargets[i]);

                playerTargets.Remove(playerTargets[i]);

                Debug.Log("Target removed from list");

                break;
            }

            if (!IsEnemyRendererVisible(playerTargets[i]) || Vector2.Distance(Camera.main.WorldToScreenPoint(playerTargets[i].position), GetScreenCenterSpace()) > (Screen.height / 4))
            {
                if (playerTargets.Contains(playerTargets[i]))
                {
                    AscalonGameMultiplayer.Instance.OnTargetRemove(playerTargets[i]);

                    playerTargets.Remove(playerTargets[i]);

                    Debug.Log("Target removed from list");
                }
            }
        }
    }

    public List<Transform> GetPlayerTargets()
    {
        return playerTargets;
    }

    public Transform GetPlayerTarget()
    {
        return target;
    }

    public Vector2 GetScreenCenterSpace()
    {
        return new Vector2(Screen.width / 2, Screen.height / 2);
    }

    private bool IsEnemyRendererVisible(Transform target)
    {
        return target.GetComponentInChildren<Renderer>().IsVisibleFrom(Camera.main);
    }
}
