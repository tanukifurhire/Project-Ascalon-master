using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Ability : NetworkBehaviour
{
    [SerializeField] private AbilitySO abilitySO;

    private IAbilityParent abilityParent;

    public void SetAbilityParent(IAbilityParent abilityParent)
    {
        if (abilityParent.CanAddAbility())
        {
            SetAbilityParentServerRpc(abilityParent.GetNetworkObject());
        }
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

        IAbilityParent abilityParent = abilityObjectParentNetworkObject.GetComponent<IAbilityParent>();

        if (this.abilityParent != null)
        {
            this.abilityParent.RemoveAbility(this);
        }

        this.abilityParent = abilityParent;

        abilityParent.SetAbility(this);
    }

    public virtual void TriggerAbility()
    {
        
    }

    public AbilitySO GetAbilitySO()
    {
        return abilitySO;
    }
}
