using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine;

public class Player : NetworkBehaviour, ITargetable
{
    public static Player LocalInstance { get; private set; }
    
    public event EventHandler OnStop;

    public event EventHandler OnDestroyed;

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
        Hover,
    }
    private States state;
    public States lastState;

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
    [SerializeField] private float lateralFlightSpeed = 5f;
    [SerializeField] private float turnSpeed = 200f;
    [SerializeField] private float acceleration;
    [SerializeField] private float moveAcceleration = 20f;
    [SerializeField] private float handleReturnSpeed = 2f;
    [SerializeField] private float jumpForce;
    [SerializeField] private float jumpDecelerationForce;
    [SerializeField] private float airborneSpeedMultiplier = .25f;
    [SerializeField] private float gravityForce = 9.81f;
    [SerializeField] private float slopeDownwardsForce = 80f;
    [SerializeField] private float maxTargetingRange = 37.5f;

    [field: Header("Slope Movement")]
    [SerializeField] private float maxSlopeAngle;
    [SerializeField] private RaycastHit slopeHit;

    private float actualAccel;
    private bool jump;
    private float minVelocity = .1f;
    private float playerHeight = 2.5f;
    private float prepToFlyTimer = .3f;
    private float jumpBufferTimer = .1f;
    private float boostTimer = 2.5f;
    private Vector2 smoothedInput;
    private Vector2 smoothVectorVelocity;

    // Targeting Variables
    private List<Transform> playerTargets;
    private Transform target;
    private bool isPlayerTryingToTarget;

    #region Unity Default Methods

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
        GameInput.Instance.OnTargetPressed += Player_OnTargetPressed;
        GameInput.Instance.OnTargetReleased += Player_OnTargetReleased;

        Cursor.lockState = CursorLockMode.Locked;
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
        OnDestroyed?.Invoke(this, EventArgs.Empty);
    }
    private void Player_OnDestroyed(object sender, EventArgs e)
    {
        Player player = sender as Player;

        if (playerTargets.Contains(player.transform))
        {
            playerTargets.Remove(player.transform);
        }
    }

    private void Update()
    {
        if (!IsOwner)
        {
            return;
        }

        rb.useGravity = !OnSlope();

        HandleTargets();
    }
    private void FixedUpdate()
    {
        if (IsOwner)
        {
            HandleState();

            HandleMovement();
        }
    }

    #endregion

    #region Movement Methods

    private void HandleMovement()
    {
        if (state == States.Moving)
        {
            float speed = maxSpeed;
            float moveAccel = moveAcceleration;

            DoMove(speed, moveAccel);
        }

        if (state == States.Jumping)
        {
            if (jump == true)
            {
                jump = false;

                rb.AddForce((Vector3.up * jumpForce) + GetPlayerOrientation(), ForceMode.VelocityChange);
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
            rb.AddForce((FlightTargetRotation() * Vector3.up * flyMaxSpeed) - rb.velocity, ForceMode.VelocityChange);

            Vector3 moveVel = (transform.right * GameInput.Instance.MovementInputNormalized().x);

            rb.velocity += moveVel * lateralFlightSpeed;
        }

        if (state == States.Hover)
        {
            float speed = maxSpeed;
            float moveAccel = moveAcceleration;

            DoMove(speed, moveAccel);
        }
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

    #endregion

    #region Targeting Methods

    private void HandleTargets()
    {
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

    public void AddPlayerToTargetList(Transform target)
    {
        if (!playerTargets.Contains(target))
        {
            if (IsEnemyRendererVisible(target))
            {
                if (Vector2.Distance(Camera.main.WorldToScreenPoint(target.position), GetScreenCenterSpace()) <= (Screen.height / 2.5f))
                {
                    playerTargets.Add(target);

                    target.GetComponent<Player>().OnDestroyed += Player_OnDestroyed;

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

                playerTargets[i].GetComponent<Player>().OnDestroyed -= Player_OnDestroyed;

                playerTargets.Remove(playerTargets[i]);

                Debug.Log("Target removed from list");

                break;
            }

            if (!IsEnemyRendererVisible(playerTargets[i]) || Vector2.Distance(Camera.main.WorldToScreenPoint(playerTargets[i].position), GetScreenCenterSpace()) > (Screen.height / 2.5f))
            {
                if (playerTargets.Contains(playerTargets[i]))
                {
                    AscalonGameMultiplayer.Instance.OnTargetRemove(playerTargets[i]);

                    playerTargets[i].GetComponent<Player>().OnDestroyed -= Player_OnDestroyed;

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

    #endregion

    #region Input Methods

    private Vector3 ReadMovementInput()
    {
        return new Vector3(gameInput.MovementInputNormalized().x, 0f, gameInput.MovementInputNormalized().y);
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

    private void Player_OnTargetReleased(object sender, EventArgs e)
    {
        isPlayerTryingToTarget = false;
    }

    private void Player_OnTargetPressed(object sender, EventArgs e)
    {
        isPlayerTryingToTarget = true;
    }

    #endregion

    #region State Methods

    private void HandleState()
    {
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
                    OnGroundCheck();
                }

                break;
            case States.Falling:
                OnGroundCheck();
                break;
            case States.PrepToFly:

                rb.MoveRotation(Quaternion.RotateTowards(transform.rotation, FlightTargetRotation(), turnSpeed * Time.deltaTime));

                OnGroundCheck();

                prepToFlyTimer -= Time.deltaTime;

                SetFlightRotationConstraint();

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

                turnSpeed = 360f;

                rb.MoveRotation(Quaternion.RotateTowards(transform.rotation, FlightTargetRotation(), turnSpeed * Time.deltaTime));

                OnGroundCheck();

                if (detectCollision.CheckCollisionWithPlayer())
                {
                    lastState = state;

                    state = States.Hover;

                    OnStateChanged?.Invoke(this, new OnStateChangedEventArgs
                    {
                        state = state,
                        lastState = lastState
                    });
                }

                break;
            case States.Hover:

                OnGroundCheck();

                SetGroundRotationConstraint();

                ResetVerticalVelocity();

                Quaternion targetRot = Quaternion.LookRotation(Camera.main.transform.forward);

                rb.MoveRotation(Quaternion.RotateTowards(transform.rotation, targetRot, turnSpeed * Time.deltaTime));

                rb.useGravity = false;

                break;
        }

        if (state != States.Flying && state != States.PrepToFly && state != States.Hover)
        {
            turnSpeed = 2000f;

            Quaternion targetRot = Quaternion.Euler(0f, Camera.main.transform.eulerAngles.y, 0f);

            rb.MoveRotation(Quaternion.RotateTowards(transform.rotation, targetRot, turnSpeed * Time.deltaTime));
        }

        FlightTargetHandler.Instance.SetFlightPosition(transform.position);
    }

    private void Player_OnStop(object sender, EventArgs e)
    {
        if (IsOwner)
        {
            ResetVelocity();
        }
    }

    private void OnGroundCheck()
    {
        float jumpBufferTimerMax = 0.1f;

        jumpBufferTimer = jumpBufferTimerMax;

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

    #endregion

    #region Reusable Methods

    private void ResetVelocity()
    {
        rb.velocity = Vector3.zero;
    }

    private void ResetVerticalVelocity()
    {
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
    }

    private Vector3 GetPlayerOrientation()
    {
        Vector3 screenMovementForward = Vector3.Cross(transform.right, Vector3.up);
        Vector3 screenMovementRight = transform.right;

        Vector3 horizontalMovement = screenMovementRight * ReadMovementInput().x;
        Vector3 verticalMovement = screenMovementForward * ReadMovementInput().z;

        Vector3 moveDirection = (verticalMovement + horizontalMovement).normalized;

        return moveDirection;
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
        var targetRot = Quaternion.LookRotation(-Camera.main.transform.up, Camera.main.transform.forward);

        if (target != null && isPlayerTryingToTarget)
        {
            FlightTargetHandler.Instance.SetFlightRotation();

            targetRot = Quaternion.LookRotation(-FlightTargetHandler.Instance.transform.up, FlightTargetHandler.Instance.GetFlightDirection());

            float dist = Vector3.Distance(target.position, transform.position);

            if (Mathf.Abs(target.position.y - transform.position.y) >= dist / 1.5f)
            {
                targetRot = Quaternion.LookRotation(-Camera.main.transform.up, Camera.main.transform.forward);
            }
        }

        return targetRot;
    }

    

    private float AddCameraRotationToAngle(float angle)
    {
        angle += Camera.main.transform.eulerAngles.y;
        if (angle > 360f)
        {
            angle -= 360f;
        }
        return angle;
    }

    private void SetFlightRotationConstraint()
    {
        rb.constraints = RigidbodyConstraints.FreezeRotationY;
    }

    private void SetGroundRotationConstraint()
    {
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    public Vector2 GetScreenCenterSpace()
    {
        return new Vector2(Screen.width / 2, Screen.height / 2);
    }

    private bool IsEnemyRendererVisible(Transform target)
    {
        return target.GetComponentInChildren<Renderer>().IsVisibleFrom(Camera.main);
    }

    public Quaternion GetRigidbodyRotation()
    {
        return rb.rotation;
    }

    #endregion

    private void OnDrawGizmos()
    {

    }
}