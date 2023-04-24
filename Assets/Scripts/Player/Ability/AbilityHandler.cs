using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class AbilityHandler : NetworkBehaviour, IAbilityParent
{
    public static AbilityHandler LocalInstance;

    public event EventHandler OnMeleeAbilityCast;

    public event EventHandler OnMeleeAbilityTrigger;

    private List<Ability> abilities = new List<Ability>();

    private const int MAX_ABILITY_COUNT = 3;

    [SerializeField] private Transform followTransform;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            LocalInstance = this;
        }
    }

    private void Start()
    {
        GameInput.Instance.OnDisc1Pressed += GameInput_OnDisc1Pressed;
    }

    private void GameInput_OnDisc1Pressed(object sender, EventArgs e)
    {
        OnAbilityTrigger(0);
    }

    public void OnAbilityTrigger(int abilityIndex)
    {
        if (abilities.Count < abilityIndex + 1) return;

        abilities[abilityIndex].TriggerAbility();

        RemoveAbility(abilityIndex);
    }

    public void SetAbility(Ability ability)
    {
        if (abilities.Count < MAX_ABILITY_COUNT)
        {
            abilities.Add(ability);
        }
    }

    public void RemoveAbility(int abilityIndex)
    {
        if (abilities.Contains(abilities[abilityIndex]))
        {
            abilities.RemoveAt(abilityIndex);
        }
    }

    public Ability GetAbilityFromAbilityList(int abilityIndex)
    {
        return abilities[abilityIndex];
    }

    public void ClearAbilities()
    {
        abilities = new List<Ability>();
    }

    public bool HasAbility(int abilityIndex)
    {
        return (abilities[abilityIndex] != null);
    }

    public Transform GetFollowTransform()
    {
        return followTransform;
    }

    public NetworkObject GetNetworkObject()
    {
        return Player.LocalInstance.NetworkObject;
    }

    public void RemoveAbility(Ability ability)
    {
        if (abilities.Contains(ability))
        {
            abilities.Remove(ability);
        }
    }

    public bool CanAddAbility()
    {
        return abilities.Count < MAX_ABILITY_COUNT;
    }

    public void OnMeleeAbilityActivated()
    {
        OnMeleeAbilityCast?.Invoke(this, EventArgs.Empty);
    }

    public void OnMeleeAbilityExecuted()
    {
        OnMeleeAbilityTrigger?.Invoke(this, EventArgs.Empty);
    }
}
