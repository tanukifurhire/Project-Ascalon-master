using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public enum States
    {
        Idle,
        Moving,
    }
    private States state;
    public static Player Instance { get; private set; }

    [field: Header("References")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private GameInput gameInput;
    [SerializeField] private DetectCollision detectCollision;

    [field: Header("Stats")]
    [SerializeField] private float maxSpeed;
    [SerializeField] private float turnSpeed;
    [SerializeField] private float acceleration;
    [SerializeField] private float moveAcceleration = 20f;
    [SerializeField] private float handleReturnSpeed = 2f;

    private Vector3 targetDir;
    private float actualAccel;

    private bool isGrounded;

    private void Awake()
    {
        Instance = this;
        state = States.Idle;
    }

    private void Start()
    {

    }

    private void Update()
    {
        switch (state)
        {
            case States.Idle:
                if (ReadMovementInput().magnitude > 0f)
                {
                    state = States.Moving;
                }
                break;
            case States.Moving:
                
                if (ReadMovementInput().magnitude <= 0f)
                {
                    if (detectCollision.CheckGround())
                    {
                        state = States.Idle;

                        ResetVelocity();
                    }
                }
                break;
        }
        Debug.Log(state);
    }

    private void ResetVelocity()
    {
        rb.velocity = Vector3.zero;
    }

    private void FixedUpdate()
    {
        float cameraYAngle = Camera.main.transform.localEulerAngles.y;
        float cameraXAngle = Camera.main.transform.localEulerAngles.x;

        Vector3 screenMovementForward = transform.forward;
        Vector3 screenMovementRight = transform.right;

        Vector3 horizontalMovement = screenMovementRight * ReadMovementInput().x;
        Vector3 verticalMovement = screenMovementForward * ReadMovementInput().z;

        Vector3 moveDirection = (verticalMovement + horizontalMovement).normalized;

        if (state == States.Idle || state == States.Moving)
        {
            transform.rotation = Quaternion.Euler(0f, Camera.main.transform.localEulerAngles.y, 0f);
        }

        if (state == States.Moving)
        {
            float speed = maxSpeed;
            float accel = acceleration;
            float moveAccel = moveAcceleration;

            DoMove(speed, moveAccel, moveDirection);
        }
    }

    private void DoMove(float speed, float moveAccel, Vector3 moveDirection)
    {
        Vector3 targetVelocity = moveDirection * speed;
        actualAccel = Mathf.Lerp(actualAccel, moveAccel, handleReturnSpeed * Time.fixedDeltaTime);
        Vector3 currentVelocity = rb.velocity;
        Vector3 actualMoveDirection = Vector3.Lerp(currentVelocity, targetVelocity, actualAccel * Time.fixedDeltaTime);
        actualMoveDirection.y = rb.velocity.y;
        rb.velocity = actualMoveDirection;
    }

    private Vector3 ReadMovementInput()
    {
        return new Vector3(gameInput.MovementInputNormalized().x, 0f, gameInput.MovementInputNormalized().y);
    }
}
