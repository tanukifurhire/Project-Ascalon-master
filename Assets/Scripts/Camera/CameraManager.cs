using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance { get; private set; }

    private CinemachineVirtualCamera cinemachineVirtualCamera;

    private float flightScreenShakeTimer;
    private float flightScreenShakeFrequencyGain = 0.13f;
    private float flightWobbleFrequencyGain = .4f;
    private float flightWobbleAmplitudeGain = .29f;
    [SerializeField] private AnimationCurve flightScreenShakeCurve;
    private CinemachineBasicMultiChannelPerlin cinemachineBasicMultiChannelPerlin;
    private CinemachineTransposer cinemachineTransposer;

    [SerializeField] private NoiseSettings flightScreenShakeNoise;
    [SerializeField] private NoiseSettings flightWobbleNoise;

    private bool isPlayerInitialized = false;

    private void Awake()
    {
        Instance = this;

        cinemachineVirtualCamera = GetComponent<CinemachineVirtualCamera>();

        cinemachineTransposer = cinemachineVirtualCamera.GetCinemachineComponent<CinemachineTransposer>();

        cinemachineBasicMultiChannelPerlin = cinemachineVirtualCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
    }

    public void SetPlayerCamera()
    {
        Player.LocalInstance.OnStateChanged += Player_OnStateChanged;

        cinemachineVirtualCamera.Follow = Player.LocalInstance.GetFollowTransform();
        cinemachineVirtualCamera.LookAt = Player.LocalInstance.transform;

        isPlayerInitialized = true;
    }

    private void Player_OnStateChanged(object sender, Player.OnStateChangedEventArgs e)
    {
        ResetValues();
    }

    private void ResetValues()
    {
    }

    private void Update()
    {
        if (!isPlayerInitialized) return;

        if (Player.LocalInstance.GetPlayerState() == Player.States.Flying)
        {
            cinemachineTransposer.m_XDamping = 4f;

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

            if (cinemachineTransposer != null)
            {
                cinemachineTransposer.m_XDamping = 1f;
            }

            cinemachineBasicMultiChannelPerlin.m_NoiseProfile = null;
        }
    }
}
