using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine;

public class Player : NetworkBehaviour, ITargetable
{
    // Fields

    public static Player LocalInstance { get; private set; }

    #region delegates
    public delegate void HandleMovementDelegate();

    public HandleMovementDelegate handleMovementDelegate;
    #endregion

    #region events
    public event EventHandler OnStop;

    public event EventHandler OnDestroyed;

    public event EventHandler<OnStateChangedEventArgs> OnStateChanged;

    public class OnStateChangedEventArgs : EventArgs
    {
        public States state;
        public States lastState;
    }
    #endregion

    #region StateMachine
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
    #endregion

    #region References
    [field: Header("References")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private GameInput gameInput;
    [SerializeField] private DetectCollision detectCollision;
    [SerializeField] private Transform followTransform;
    [SerializeField] private Image targetSprite;
    #endregion

    #region Non-Transmutable Stats
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
    #endregion

    #region Slope Movement Stats
    [field: Header("Slope Movement")]
    [SerializeField] private float maxSlopeAngle;
    [SerializeField] private RaycastHit slopeHit;
    #endregion

    #region Transmutable Stats
    private float actualAccel;
    private bool jump;
    private bool hover = false;
    private float minVelocity = .1f;
    private float playerHeight = 2.5f;
    private float prepToFlyTimer = .3f;
    private float jumpBufferTimer = .1f;
    private float boostTimer = 2.5f;
    private Vector2 smoothedInput;
    private Vector2 smoothVectorVelocity;
    #endregion

    #region Targeting Variables
    private List<Transform> playerTargets;
    private Transform target;
    private Transform currentTarget;
    private bool isPlayerTryingToTarget;
    #endregion

    #region Ability Handler
    private AbilityHandler abilityHandler;
    #endregion

    // Methods

    #region Unity Default Methods

    private void Awake()
    {
        state = States.Idle;

        playerTargets = new List<Transform>();

        abilityHandler = GetComponentInChildren<AbilityHandler>();
    }

    private void Start()
    {
        OnStop += Player_OnStop;
        GameInput.Instance.OnJump += Player_OnJump;
        GameInput.Instance.OnBoost += Player_OnBoost;
        GameInput.Instance.OnTargetPressed += Player_OnTargetPressed;
        GameInput.Instance.OnTargetReleased += Player_OnTargetReleased;
        GameInput.Instance.OnHover += Player_OnHover;

        LocalInstance.OnStateChanged += Player_OnStateChanged;

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

    public Transform GetActivePlayerTarget()
    {
        return currentTarget;
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
            ChangeState(States.Jumping);
            jump = true;
        }
    }

    private void Player_OnBoost(object sender, EventArgs e)
    {
        if (!detectCollision.CheckGround())
        {
            ChangeState(States.PrepToFly);
        }
    }

    private void Player_OnTargetReleased(object sender, EventArgs e)
    {
        isPlayerTryingToTarget = false;

        currentTarget = null;
    }

    private void Player_OnTargetPressed(object sender, EventArgs e)
    {
        isPlayerTryingToTarget = true;

        if (target != null)
        {
            currentTarget = target;
        }
    }

    private void Player_OnHover(object sender, EventArgs e)
    {
        hover = !hover;
        if (hover)
        {
            ChangeState(States.Hover);
        }
    }

    #endregion

    #region State Methods

    private void HandleState()
    {
        Quaternion targetRot = Quaternion.Euler(0f, Camera.main.transform.eulerAngles.y, 0f);

        switch (state)
        {
            case States.Idle:

                if (!detectCollision.CheckGround())
                {
                    ChangeState(States.Falling);
                }

                if (ReadMovementInput().magnitude > 0f)
                {
                    ChangeState(States.Moving);
                }

                turnSpeed = 2000f;

                rb.rotation = targetRot;

                break;
            case States.Moving:

                if (!detectCollision.CheckGround())
                {
                    ChangeState(States.Falling);
                }

                if (ReadMovementInput().magnitude <= 0f)
                {
                    if (detectCollision.CheckGround())
                    {
                        ChangeState(States.Idle);
                        OnStop?.Invoke(this, EventArgs.Empty);
                    }
                }

                turnSpeed = 2000f;

                rb.rotation = targetRot;

                break;
            case States.Jumping:

                jumpBufferTimer -= Time.deltaTime;

                if (jump == false)
                {
                    if (IsPlayerStoppedMovingUpwards())
                    {
                        ChangeState(States.Falling);
                    }
                }

                if (jumpBufferTimer <= 0f)
                {
                    OnGroundCheck();
                }

                turnSpeed = 2000f;

                rb.rotation = targetRot;

                break;
            case States.Falling:

                OnGroundCheck();

                turnSpeed = 2000f;

                rb.rotation = targetRot;

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

                    ChangeState(States.Flying);
                }

                break;
            case States.Flying:

                turnSpeed = 360f;

                rb.MoveRotation(Quaternion.RotateTowards(transform.rotation, FlightTargetRotation(), turnSpeed * Time.deltaTime));

                OnGroundCheck();

                if (detectCollision.CheckCollisionWithPlayer())
                {
                    ChangeState(States.Hover);
                }

                break;
            case States.Hover:

                OnGroundCheck();

                SetGroundRotationConstraint();

                ResetVerticalVelocity();

                rb.MoveRotation(Quaternion.RotateTowards(transform.rotation, targetRot, turnSpeed * Time.deltaTime));

                rb.useGravity = false;

                break;
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

    private void Player_OnStateChanged(object sender, OnStateChangedEventArgs e)
    {
        if (state == States.Hover)
        {

        }
        else
        {
            hover = false;
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
                ChangeState(States.Moving);
            }
            else
            {
                ChangeState(States.Idle);
                OnStop?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public void ChangeState(States state)
    {
        lastState = LocalInstance.state;
        LocalInstance.state = state;
        OnStateChanged?.Invoke(this, new OnStateChangedEventArgs
        {
            state = state,
            lastState = lastState
        });
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

        if (currentTarget != null)
        {
            FlightTargetHandler.Instance.SetFlightRotation();

            targetRot = Quaternion.LookRotation(-FlightTargetHandler.Instance.transform.up, FlightTargetHandler.Instance.GetFlightDirection());
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

    public AbilityHandler GetAbilityHandler()
    {
        return abilityHandler;
    }

    #endregion

    private void OnDrawGizmos()
    {

    }
}