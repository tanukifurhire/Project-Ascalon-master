using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class AscalonGameMultiplayer : NetworkBehaviour
{
    public static AscalonGameMultiplayer Instance { get; private set; }

    public event EventHandler OnPlayerDestroyed;

    public event EventHandler<OnTargetChangedEventArgs> OnTargetAdded;

    public event EventHandler<OnTargetChangedEventArgs> OnTargetRemoved;

    public class OnTargetChangedEventArgs : EventArgs
    {
        public Transform target;
    }

    private void Awake()
    {
        Instance = this;
    }

    public void OnTargetAdd(Transform target)
    {
        OnTargetAdded?.Invoke(this, new OnTargetChangedEventArgs
        {
            target = target
        });
    }

    public void OnTargetRemove(Transform target)
    {
        OnTargetRemoved?.Invoke(this, new OnTargetChangedEventArgs
        {
            target = target
        });
    }

    public void PlayerDestroyed()
    {
        PlayerDestroyedServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void PlayerDestroyedServerRpc()
    {
        PlayerDestroyedClientRpc();
    }

    [ClientRpc]
    private void PlayerDestroyedClientRpc()
    {
        OnPlayerDestroyed?.Invoke(this, EventArgs.Empty);

        Debug.Log("Player Destroyed!");
    }
}
