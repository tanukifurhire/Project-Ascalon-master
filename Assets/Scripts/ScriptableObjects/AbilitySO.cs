using UnityEngine;

[CreateAssetMenu]
public class AbilitySO : ScriptableObject
{
    public string abilityName;

    public int abilityDamage;

    public AbilityTypes abilityType;

    public enum AbilityTypes
    {
        Ranged,
        Melee,
        Area,
    }
}
