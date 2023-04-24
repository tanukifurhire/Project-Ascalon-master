using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Ability : NetworkBehaviour
{
    [SerializeField] protected AbilitySO abilitySO;

    [SerializeField] private FollowTransform followTransform;

    protected IAbilityParent abilityParent;

    protected float abilityCastTimer;

    public void SetAbilityParent(IAbilityParent abilityParent)
    {
        SetAbilityParentServerRpc(abilityParent.GetNetworkObject());
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetAbilityParentServerRpc(NetworkObjectReference abilityNetworkObjectReference)
    {
        SetAbilityParentClientRpc(abilityNetworkObjectReference);
    }

    [ClientRpc]
    private void SetAbilityParentClientRpc(NetworkObjectReference abilityNetworkObjectReference)
    {
        abilityNetworkObjectReference.TryGet(out NetworkObject abilityObjectParentNetworkObject);

        IAbilityParent abilityParent = abilityObjectParentNetworkObject.GetComponentInChildren<IAbilityParent>();

        if (this.abilityParent != null)
        {
            this.abilityParent.RemoveAbility(this);
        }

        this.abilityParent = abilityParent;

        abilityParent.SetAbility(this);

        Debug.Log(abilityParent);

        followTransform.SetTargetTransform(abilityParent.GetFollowTransform());
    }

    public virtual void TriggerAbility()
    {
        
    }

    public AbilitySO GetAbilitySO()
    {
        return abilitySO;
    }
}
