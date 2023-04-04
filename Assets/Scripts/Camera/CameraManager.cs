using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    public void SetPlayerCamera(out CinemachineVirtualCamera cinemachineVirtualCamera)
    {
        cinemachineVirtualCamera = GetComponent<CinemachineVirtualCamera>();

        cinemachineVirtualCamera.Follow = Player.LocalInstance.GetFollowTransform();
        cinemachineVirtualCamera.LookAt = Player.LocalInstance.GetFollowTransform();
    }
}
