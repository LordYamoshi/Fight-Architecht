using System;
using UnityEngine;

public class HitBox : MonoBehaviour
{
    [SerializeField] private GameObject ownerObject;
    [SerializeField] private LayerMask opponentLayer;
    
    private FighterStats ownerStats;
    private FighterCombat ownerCombat;
    
    private void Awake()
    {
        if (ownerObject != null)
        {
            ownerStats = ownerObject.GetComponent<FighterStats>();
            ownerCombat = ownerObject.GetComponent<FighterCombat>();
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (ownerObject == null || ownerStats == null || ownerCombat == null) return;
        
        // Prevent hitting self
        if (other.gameObject == ownerObject) return;
        
        if ((opponentLayer.value & (1 << other.gameObject.layer)) == 0) return;
        
        FighterCombat opponentCombat = other.GetComponent<FighterCombat>();
        FighterStats opponentStats = other.GetComponent<FighterStats>();
        FighterHealth opponentHealth = other.GetComponent<FighterHealth>();
        
        Debug.Log("HitBox Triggered");
        
        if (opponentCombat == null || opponentStats == null || opponentHealth == null) return;
        
        // Only process if we're attcking
        if (!ownerCombat.IsAttacking) return;
        
        // Calculate damage
        float damage = Mathf.Max(ownerStats.Strength - opponentStats.Defense, 0);
        
        // Apply damage to opponent
        opponentHealth.TakeDamage(damage);
        
        ownerCombat.ProcessSuccessfulHit(damage);
        
        Debug.Log($"{ownerObject.name} hit {other.gameObject.name} for {damage} damage!");
    }
}