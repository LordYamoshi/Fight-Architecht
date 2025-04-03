using UnityEngine;

public class FighterHitbox : MonoBehaviour
{
    [Header("Component References")]
    [SerializeField] private FighterCombat ownerFighterCombat;
    [SerializeField] private FighterStats ownerFighterStats;
    
    [Header("Hit Detection")]
    [SerializeField] private LayerMask opponentLayer;
    
    private void Awake()
    {
        if (ownerFighterCombat == null) 
        {
            ownerFighterCombat = GetComponentInParent<FighterCombat>();
        }
        
        if (ownerFighterStats == null && ownerFighterCombat != null)
        {
            ownerFighterStats = ownerFighterCombat.GetComponent<FighterStats>();
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        // Prevent hitting self
        if (ownerFighterCombat == null || other.gameObject == ownerFighterCombat.gameObject) return;
        
        // Check if we're hitting an opponent on the correct layer
        if ((opponentLayer.value & (1 << other.gameObject.layer)) == 0) return;
        
        if (!ownerFighterCombat.IsAttacking) return;
        
        FighterHealth opponentHealth = other.GetComponent<FighterHealth>();
        FighterStats opponentStats = other.GetComponent<FighterStats>();
        
        if (opponentHealth == null || opponentStats == null) return;
        
        // Calculate damage with defense consideration
        float damage = Mathf.Max(ownerFighterStats.Strength - opponentStats.Defense, 0);
        
        opponentHealth.TakeDamage(damage);
        
        ownerFighterCombat.ProcessSuccessfulHit(damage);
        
        Debug.Log($"{ownerFighterCombat.gameObject.name} hit {other.gameObject.name} for {damage} damage!");
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, transform.localScale);
    }
}