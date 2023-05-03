using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class BoxAbility : Ability
{
    [SerializeField] private GameObject abilityHitbox;

    [SerializeField] private GameObject abilityVisual;

    private bool isActive;

    public override void TriggerAbility()
    {
        abilityVisual.SetActive(true);

        isActive = true;

        AbilityHandler.LocalInstance.OnMeleeAbilityActivated();

        abilityCastTimer = abilitySO.abilityCastTime;
    }

    private void Update()
    {
        abilityCastTimer -= Time.deltaTime;

        if (abilityCastTimer <= 0 && isActive == true)
        {
            abilityVisual.SetActive(false);

            abilityHitbox.SetActive(true);

            AbilityHandler.LocalInstance.OnMeleeAbilityExecuted();

            abilityCastTimer = abilitySO.abilityTime;

            isActive = false;
        }
        if (abilityCastTimer <= 0 && isActive == false)
        {
            abilityHitbox.SetActive(false);
        }
    }
}
