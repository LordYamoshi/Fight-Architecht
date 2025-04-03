using UnityEngine;

public class FighterHealth : MonoBehaviour
{
[Header("Component References")]
    [SerializeField] private FighterStats fighterStats;
    [SerializeField] private FighterCombat fighterCombat;
    [SerializeField] private FighterAgent fighterAgent;
    [SerializeField] private HealthSystem healthDisplaySystem;
    [SerializeField] private MatchManager matchManager;
    
    [Header("Fighter Info")]
    [SerializeField] private int fighterID;
    
    private float currentHealth;
    private float maxHealth;
    
    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    
    private void Awake()
    {
        // Get required components if not set
        if (fighterStats == null) fighterStats = GetComponent<FighterStats>();
        if (fighterCombat == null) fighterCombat = GetComponent<FighterCombat>();
        if (fighterAgent == null) fighterAgent = GetComponent<FighterAgent>();
    }
    
    private void Start()
    {
        InitializeHealth();
    }
    
    private void InitializeHealth()
    {
        if (fighterStats == null) return;
        
        maxHealth = fighterStats.Health;
        currentHealth = maxHealth;
        
        if (healthDisplaySystem != null)
        {
            healthDisplaySystem.SetMaxHealth(maxHealth);
            healthDisplaySystem.UpdateHealth(currentHealth);
        }
    }
    
    public void ResetHealth()
    {
        currentHealth = maxHealth * Random.Range(0.95f, 1.0f);
        
        if (healthDisplaySystem != null)
        {
            healthDisplaySystem.UpdateHealth(currentHealth);
        }
    }
    
    public void SetHealth(float health)
    {
        currentHealth = health;
        if (healthDisplaySystem != null)
        {
            healthDisplaySystem.UpdateHealth(currentHealth);
        }
    }
    
    public void TakeDamage(float damage)
    {
        float originalDamage = damage;
        bool wasBlocking = fighterCombat != null && fighterCombat.IsBlocking;
        bool wasDodging = fighterCombat != null && fighterCombat.IsDodging;

        // Apply defense if blocking
        if (wasBlocking)
        {
            damage -= fighterStats.Defense;
            if (damage < 0) damage = 0;
            
            // Notify combat component of successful block
            fighterCombat.ProcessBlockedAttack();
        }
        else if (wasDodging)
        {
            // Notify combat component of successful dodge
            fighterCombat.ProcessSuccessfulDodge();
        }

        // Take damage
        currentHealth -= damage;

        // Notify the agent about damage taken
        if (fighterAgent != null)
        {
            fighterAgent.OnTakeDamage(damage, originalDamage, wasBlocking, wasDodging);
        }

        // Check for defeat
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            OnDefeat();
        }

        // Update health UI
        if (healthDisplaySystem != null)
        {
            healthDisplaySystem.UpdateHealth(currentHealth);
        }
        
        // End block if we were blocking
        if (wasBlocking && fighterCombat != null)
        {
            fighterCombat.EndBlock();
        }
        
        Debug.Log($"Fighter {fighterID} took {damage} damage. Health: {currentHealth}/{maxHealth}");
    }
    
    private void OnDefeat()
    {
        if (fighterAgent != null)
        {
            fighterAgent.CompleteEpisode(false);
        }
        
        if (matchManager != null)
        {
            matchManager.EndMatch();
        }
        
        Debug.Log($"Fighter {fighterID} has been defeated.");
    }
    
    public void HandleVictory()
    {
        if (fighterAgent != null)
        {
            fighterAgent.CompleteEpisode(true);
        }
    }
}
