using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoxAbilityHolder : MonoBehaviour
{
    [SerializeField] private Ability ability;

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform == Player.LocalInstance.transform)
        {
            ability.SetAbilityParent(Player.LocalInstance.GetAbilityHandler());
        }
    }
}
