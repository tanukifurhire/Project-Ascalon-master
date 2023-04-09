using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public interface IAbilityParent
{
    public void SetAbility(Ability ability);

    public void RemoveAbility(int abilityIndex);

    public void RemoveAbility(Ability ability);

    public Ability GetAbilityFromAbilityList(int abilityIndex);

    public bool HasAbility(int abilityIndex);

    public void ClearAbilities();

    public Transform GetFollowTransform();

    public NetworkObject GetNetworkObject();

    public bool CanAddAbility();
}
