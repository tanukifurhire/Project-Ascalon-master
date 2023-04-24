using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameInput : MonoBehaviour
{
    public static GameInput Instance { get; private set; }

    public event EventHandler OnJump;
    public event EventHandler OnBoost;
    public event EventHandler OnTargetPressed;
    public event EventHandler OnTargetReleased;
    public event EventHandler OnHover;
    public event EventHandler OnDisc1Pressed;

    private PlayerInputActions playerInputActions;

    private void Awake()
    {
        Instance = this;
        playerInputActions = new PlayerInputActions();
        playerInputActions.Player.Enable();

        playerInputActions.Player.Jump.performed += Jump_performed;
        playerInputActions.Player.Boost.performed += Boost_performed;
        playerInputActions.Player.Target.started += Target_started;
        playerInputActions.Player.Target.canceled += Target_canceled;
        playerInputActions.Player.Hover.performed += Hover_performed;
        playerInputActions.Player.Disc1.performed += Disc1_performed;
    }

    private void Disc1_performed(InputAction.CallbackContext obj)
    {
        OnDisc1Pressed?.Invoke(this, EventArgs.Empty);
    }

    private void Hover_performed(InputAction.CallbackContext obj)
    {
        OnHover?.Invoke(this, EventArgs.Empty);
    }

    private void Target_canceled(InputAction.CallbackContext obj)
    {
        OnTargetReleased?.Invoke(this, EventArgs.Empty);

        Debug.Log("Target released");
    }

    private void Target_started(InputAction.CallbackContext obj)
    {
        OnTargetPressed?.Invoke(this, EventArgs.Empty);

        Debug.Log("Target pressed");
    }

    private void OnDestroy()
    {
        playerInputActions.Player.Jump.performed -= Jump_performed;
        playerInputActions.Player.Boost.performed -= Boost_performed;
        playerInputActions.Player.Target.started -= Target_started;
        playerInputActions.Player.Target.canceled -= Target_canceled;
        playerInputActions.Player.Hover.performed -= Hover_performed;
        playerInputActions.Player.Disc1.performed -= Disc1_performed;

        playerInputActions.Dispose();
    }

    private void Jump_performed(InputAction.CallbackContext obj)
    {
        OnJump?.Invoke(this, EventArgs.Empty);
    }
    private void Boost_performed(InputAction.CallbackContext obj)
    {
        OnBoost?.Invoke(this, EventArgs.Empty);
    }

    public Vector2 MovementInputNormalized()
    {
        return playerInputActions.Player.Move.ReadValue<Vector2>();
    }

    public Vector2 LookInputNormalized()
    {
        return playerInputActions.Player.Look.ReadValue<Vector2>();
    }
}
