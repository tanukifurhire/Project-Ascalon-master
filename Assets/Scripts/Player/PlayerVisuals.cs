using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

public class PlayerVisuals : NetworkBehaviour
{
    [SerializeField] private NetworkAnimator playerNetworkAnimator;
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
        if (!IsOwner)
        {
            return;
        }
        AbilityHandler_OnMeleeAbilityTriggerServerRpc();
    }

    [ServerRpc (RequireOwnership = false)]
    private void AbilityHandler_OnMeleeAbilityTriggerServerRpc()
    {
        AbilityHandler_OnMeleeAbilityTriggerClientRpc();
    }

    [ClientRpc]
    private void AbilityHandler_OnMeleeAbilityTriggerClientRpc()
    {
        playerNetworkAnimator.SetTrigger("OnMeleeExecute");
    }

    private void AbilityHandler_OnMeleeAbilityCast(object sender, EventArgs e)
    {
        if (!IsOwner)
        {
            return;
        }
        AbilityHandler_OnMeleeCastServerRpc();
    }

    [ServerRpc (RequireOwnership = false)]
    private void AbilityHandler_OnMeleeCastServerRpc()
    {
        AbiltyHandler_OnMeleeCastClientRpc();
    }

    [ClientRpc]
    private void AbiltyHandler_OnMeleeCastClientRpc()
    {
        playerNetworkAnimator.SetTrigger("OnMeleeStart");
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
