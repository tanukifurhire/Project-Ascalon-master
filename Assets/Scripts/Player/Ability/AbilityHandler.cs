using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class AbilityHandler : NetworkBehaviour, IAbilityParent
{
    private List<Ability> abilities = new List<Ability>();

    private const int MAX_ABILITY_COUNT = 3;

    [SerializeField] private Transform followTransform;

    public void OnAbilityTrigger(int abilityIndex)
    {
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
        return NetworkObject;
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
}
