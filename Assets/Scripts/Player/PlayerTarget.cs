using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerTarget : MonoBehaviour
{
    private void OnTriggerStay(Collider other)
    {
        Transform targetTransform = other.transform;

        if (targetTransform.GetComponent<ITargetable>() != null && targetTransform != Player.LocalInstance.transform)
        {
            Player.LocalInstance.AddPlayerToTargetList(targetTransform);
        }
    }
}
