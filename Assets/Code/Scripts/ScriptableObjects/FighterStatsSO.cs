using UnityEngine;

[CreateAssetMenu(fileName = "FighterStats", menuName = "Data/FighterStats")]
public class FighterStatsSO : ScriptableObject
{
    public float strength;
    public float attackSpeed;
    public float attackCooldown;
    public float dodgeRate;
    public float movementSpeed;
    public float health;
    public float defense;
}
