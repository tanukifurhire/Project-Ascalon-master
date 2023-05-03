using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance { get; private set; }

    private CinemachineVirtualCamera cinemachineVirtualCamera;

    private Transform playerTarget;
    private float flightScreenShakeTimer;
    private float flightScreenShakeFrequencyGain = 0.13f;
    private float flightWobbleFrequencyGain = .4f;
    private float flightWobbleAmplitudeGain = .29f;
    private float mouseX;
    private float mouseY;

    [SerializeField] private AnimationCurve flightScreenShakeCurve;
    [SerializeField] private CinemachineTargetGroup cinemachineTargetGroup;
    [SerializeField] private NoiseSettings flightScreenShakeNoise;
    [SerializeField] private NoiseSettings flightWobbleNoise;
    [SerializeField] private Vector2 targetLockOffset;
    [SerializeField] private float yOffset;

    private CinemachineBasicMultiChannelPerlin cinemachineBasicMultiChannelPerlin;
    private CinemachineTransposer cinemachineTransposer;
    private CinemachinePOV cinemachinePOV;

    private bool isPlayerInitialized = false;
    private bool isPlayerTryingToTarget;

    private void Awake()
    {
        Instance = this;

        cinemachineVirtualCamera = GetComponent<CinemachineVirtualCamera>();

        cinemachineTransposer = cinemachineVirtualCamera.GetCinemachineComponent<CinemachineTransposer>();

        cinemachinePOV = cinemachineVirtualCamera.GetCinemachineComponent<CinemachinePOV>();

        cinemachineBasicMultiChannelPerlin = cinemachineVirtualCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
    }

    private void GameInput_OnTargetReleased(object sender, EventArgs e)
    {
        cinemachinePOV.m_VerticalRecentering.m_enabled = false;
        cinemachinePOV.m_HorizontalRecentering.m_enabled = false;

        if (Player.LocalInstance.GetPlayerState() == Player.States.Flying || Player.LocalInstance.GetPlayerState() == Player.States.PrepToFly)
        {
            cinemachineTransposer.m_XDamping = 4f;
        }
        else
        {
            cinemachineTransposer.m_XDamping = 1f;
        }

        isPlayerTryingToTarget = false;
    }

    private void GameInput_OnTargetPressed(object sender, EventArgs e)
    {
        if (Player.LocalInstance != null)
        {
            if (Player.LocalInstance.GetPlayerTarget() != null)
            {
                isPlayerTryingToTarget = true;

                cinemachineTransposer.m_XDamping = 0.5f;
            }
        }
    }

    private void Target_OnDestroyed(object sender, EventArgs e)
    {
        
    }

    public void SetPlayerCamera()
    {
        Player.LocalInstance.OnStateChanged += Player_OnStateChanged;
        GameInput.Instance.OnTargetPressed += GameInput_OnTargetPressed;
        GameInput.Instance.OnTargetReleased += GameInput_OnTargetReleased;

        cinemachineTargetGroup.AddMember(Player.LocalInstance.transform, 2f, 2f);

        isPlayerInitialized = true;
    }

    private void Player_OnStateChanged(object sender, Player.OnStateChangedEventArgs e)
    {
        ResetValues();
        if (e.state == Player.States.Flying || e.state == Player.States.PrepToFly)
        {
            if (!isPlayerTryingToTarget)
            {
                cinemachineTransposer.m_XDamping = 4f;
            }
            cinemachinePOV.m_HorizontalAxis.m_SpeedMode = AxisState.SpeedMode.MaxSpeed;
            cinemachinePOV.m_VerticalAxis.m_SpeedMode = AxisState.SpeedMode.MaxSpeed;
            cinemachinePOV.m_HorizontalAxis.m_MaxSpeed = 125f;
            cinemachinePOV.m_VerticalAxis.m_MaxSpeed = 100f;
        }
        else
        {
            if (!isPlayerTryingToTarget)
            {
                cinemachineTransposer.m_XDamping = 1f;
            }

            cinemachinePOV.m_HorizontalAxis.m_SpeedMode = AxisState.SpeedMode.InputValueGain;
            cinemachinePOV.m_VerticalAxis.m_SpeedMode = AxisState.SpeedMode.InputValueGain;
            cinemachinePOV.m_HorizontalAxis.m_MaxSpeed = 1f;
            cinemachinePOV.m_VerticalAxis.m_MaxSpeed = .8f;
        }
    }

    private void ResetValues()
    {

    }

    private void LateUpdate()
    {
        if (!isPlayerInitialized) return;

        if (!isPlayerTryingToTarget)
        {
            mouseX = GameInput.Instance.LookInputNormalized().x;
            mouseY = GameInput.Instance.LookInputNormalized().y;
        }
        else
        {
            Vector3 viewPos = Camera.main.WorldToViewportPoint(Player.LocalInstance.GetActivePlayerTarget().position);

            mouseX = (viewPos.x - 0.5f + (targetLockOffset.x * GameInput.Instance.LookInputNormalized().x)) * 5f;
            mouseY = (viewPos.y - (yOffset + (0.02f * Vector3.Distance(new Vector3(Player.LocalInstance.GetActivePlayerTarget().position.x, 0f, Player.LocalInstance.GetActivePlayerTarget().position.z), new Vector3(Player.LocalInstance.transform.position.x, 0f, Player.LocalInstance.transform.position.z))) + (targetLockOffset.y * GameInput.Instance.LookInputNormalized().y))) * 5f;
        }

        cinemachinePOV.m_HorizontalAxis.m_InputAxisValue = mouseX;
        cinemachinePOV.m_VerticalAxis.m_InputAxisValue = mouseY;

        if (Player.LocalInstance.GetPlayerState() == Player.States.Flying)
        {
            float flightScreenShakeTimerMax = 1f;

            flightScreenShakeTimer += Time.deltaTime;

            if (flightScreenShakeTimer <= flightScreenShakeTimerMax)
            {
                cinemachineBasicMultiChannelPerlin.m_NoiseProfile = flightScreenShakeNoise;
                cinemachineBasicMultiChannelPerlin.m_AmplitudeGain = flightScreenShakeCurve.Evaluate(flightScreenShakeTimer);
                cinemachineBasicMultiChannelPerlin.m_FrequencyGain = flightScreenShakeFrequencyGain;
            }
            else
            {
                cinemachineBasicMultiChannelPerlin.m_NoiseProfile = flightWobbleNoise;
                cinemachineBasicMultiChannelPerlin.m_AmplitudeGain = flightWobbleAmplitudeGain;
                cinemachineBasicMultiChannelPerlin.m_FrequencyGain = flightWobbleFrequencyGain;
            }
        }
        else
        {
            flightScreenShakeTimer = 0f;

            cinemachineBasicMultiChannelPerlin.m_NoiseProfile = null;
        }
    }
}
