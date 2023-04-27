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
        Player.LocalInstance.GetAbilityHandler().OnMeleeAbilityCast += AbilityHandler_OnMeleeAbilityCast;
        Player.LocalInstance.GetAbilityHandler().OnMeleeAbilityTrigger += AbilityHandler_OnMeleeAbilityTrigger;
    }

    private void AbilityHandler_OnMeleeAbilityTrigger(object sender, EventArgs e)
    {
        playerAnimator.SetTrigger("OnMeleeExecute");
    }

    private void AbilityHandler_OnMeleeAbilityCast(object sender, EventArgs e)
    {
        playerAnimator.SetTrigger("OnMeleeStart");
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
