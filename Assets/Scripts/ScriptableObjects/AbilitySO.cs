using UnityEngine;

[CreateAssetMenu]
public class AbilitySO : ScriptableObject
{
    public Transform abilityPrefab;

    public string abilityName;

    public int abilityDamage;

    public float abilityCastTime;

    public float abilityTime;

    public AbilityTypes abilityType;

    public enum AbilityTypes
    {
        Ranged,
        Melee,
        Area,
    }
}
