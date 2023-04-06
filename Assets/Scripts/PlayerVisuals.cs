using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerVisuals : NetworkBehaviour
{
    [SerializeField] private Animator playerAnimator;

    private const string MOVE_Y = "MoveY";

    private void Start()
    {
        Player.LocalInstance.OnStateChanged += Player_OnStateChanged;
    }

    private void Player_OnStateChanged(object sender, Player.OnStateChangedEventArgs e)
    {
        if (!IsOwner)
        {
            return;
        }
        playerAnimator.SetBool(e.lastState.ToString(), false);
        playerAnimator.SetBool(e.state.ToString(), true);
    }

    private void Update()
    {
        if (Player.LocalInstance.GetPlayerState() == Player.States.Moving)
        {
            playerAnimator.SetFloat(MOVE_Y, (-GameInput.Instance.MovementInputNormalized().y + 1f) / 2);
        }
    }
}
