using UnityEngine;

public class FighterStats : MonoBehaviour
{
    [Header("Stat Source")]
    [SerializeField] private FighterStatsSO statsScriptableObject;
    
    [Header("Combat Stats")]
    [SerializeField] private float strength = 5f;
    [SerializeField] private float attackSpeed = 2f;
    [SerializeField] private float attackCooldown = 0.3f;
    [SerializeField] private float dodgeRate = 0.02f;
    
    [Header("Movement Stats")]
    [SerializeField] private float movementSpeed = 3f;
    
    [Header("Defensive Stats")]
    [SerializeField] private float health = 100f;
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float defense = 2f;
    
    public float Strength => strength;
    public float AttackSpeed => attackSpeed;
    public float AttackCooldown => attackCooldown;
    public float DodgeRate => dodgeRate;
    public float MovementSpeed => movementSpeed;
    public float Health => health;
    public float Defense => defense;
    
    private void Awake()
    {
        LoadStats();
    }
    
    private void LoadStats()
    {
        if (statsScriptableObject != null)
        {
            strength = statsScriptableObject.strength;
            attackSpeed = statsScriptableObject.attackSpeed;
            attackCooldown = statsScriptableObject.attackCooldown;
            dodgeRate = statsScriptableObject.dodgeRate;
            movementSpeed = statsScriptableObject.movementSpeed;
            health = statsScriptableObject.health;
            defense = statsScriptableObject.defense;
            
            Debug.Log($"Loaded stats from {statsScriptableObject.name}");
        }
    }
    
    public void UpdateStrength(float newValue)
    {
        strength = newValue;
    }
    
    public void UpdateAttackSpeed(float newValue)
    {
        attackSpeed = newValue;
    }
    
    public void UpdateAttackCooldown(float newValue)
    {
        attackCooldown = newValue;
    }
    
    public void UpdateDodgeRate(float newValue)
    {
        dodgeRate = newValue;
    }
    
    public void UpdateMovementSpeed(float newValue)
    {
        movementSpeed = newValue;
    }
    
    public void UpdateHealth(float newValue)
    {
        health = newValue;
    }

    public void UpdateDefense(float newValue)
    {
        defense = newValue;
    }

}