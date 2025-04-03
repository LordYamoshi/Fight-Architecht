using UnityEngine;

public class FighterCombat : MonoBehaviour
{
 [Header("Component References")]
    [SerializeField] private FighterStats fighterStats;
    [SerializeField] private FighterHealth fighterHealth;
    [SerializeField] private FighterAgent fighterAgent;
    [SerializeField] private Animator animator;
    [SerializeField] private MatchStats matchStats;
    
    [Header("Fighter Info")]
    [SerializeField] private int fighterID;
    
    private bool isAttacking = false;
    private bool isBlocking = false;
    private bool isDodging = false;
    
    private float lastAttackTime = -999f;
    private float lastBlockTime = -999f;
    private float lastDodgeTime = -999f;
    private float blockStartTime = 0f;
    private float blockDuration = 3.0f;
    private float dodgeDuration = 0.4f;
    
    private bool hasRewardedBlock = false;
    private bool hasRewardedDodge = false;
    
    public bool IsAttacking => isAttacking;
    public bool IsBlocking => isBlocking;
    public bool IsDodging => isDodging;
    
    private void Awake()
    {
        if (fighterStats == null) fighterStats = GetComponent<FighterStats>();
        if (fighterHealth == null) fighterHealth = GetComponent<FighterHealth>();
        if (fighterAgent == null) fighterAgent = GetComponent<FighterAgent>();
        if (animator == null) animator = GetComponent<Animator>();
    }
    
    private void Update()
    {
        // End blocking if duration exceeded
        if (isBlocking && Time.time - blockStartTime > blockDuration)
        {
            EndBlock();
        }

        // End dodging if duration exceeded
        if (isDodging && Time.time - lastDodgeTime > dodgeDuration)
        {
            EndDodge();
        }
    }
    
    public void ResetCombatState()
    {
        // Reset action flags
        isAttacking = false;
        isBlocking = false;
        isDodging = false;

        // Reset timers with slight randomization
        lastAttackTime = -Random.Range(0f, fighterStats.AttackCooldown * 0.5f);
        lastBlockTime = -Random.Range(0f, 2.0f);
        lastDodgeTime = -Random.Range(0f, 2.0f);
        
        // Reset reward tracking
        hasRewardedBlock = false;
        hasRewardedDodge = false;
    }
    
    public void Attack()
    {
        // Check if attack is on cooldown or fighter is performing another action
        if (Time.time - lastAttackTime < fighterStats.AttackCooldown || isDodging || isBlocking) return;

        isAttacking = true;
        animator?.SetTrigger("Attack");
        matchStats?.LogHit(fighterID, (int)fighterStats.Strength);
        lastAttackTime = Time.time;
        
        Debug.Log($"Fighter {fighterID} is attacking.");
    }

    public void Block()
    {
        // Check if block is on cooldown
        if (isBlocking || Time.time - lastBlockTime < 2.0f) return;

        isBlocking = true;
        animator?.SetBool("Block", true);
        lastBlockTime = Time.time;
        blockStartTime = Time.time;
        matchStats?.LogDefense(fighterID, true);
        
        hasRewardedBlock = false;
        
        Debug.Log($"Fighter {fighterID} is blocking.");
    }

    public void Dodge()
    {
        if (isDodging || Time.time - lastDodgeTime < 2.0f) return;

        isDodging = true;
        animator?.SetTrigger("Dodge");
        lastDodgeTime = Time.time;
        
        hasRewardedDodge = false;
        
        Debug.Log($"Fighter {fighterID} dodged.");
    }
    
    public void StartAttack()
    {
        isAttacking = true;
    }

    public void EndAttack()
    {
        isAttacking = false;
    }

    public void EndBlock()
    {
        isBlocking = false;
        animator?.SetBool("Block", false);
        Debug.Log($"Fighter {fighterID} stopped blocking.");
    }

    public void EndDodge()
    {
        isDodging = false;
        Debug.Log($"Fighter {fighterID} finished dodging.");
    }
    
    public void ProcessSuccessfulHit(float damageDealt)
    {
        if (fighterAgent != null)
        {
            fighterAgent.OnSuccessfulHit(damageDealt);
        }
    }
    
    public void ProcessBlockedAttack()
    {
        if (!hasRewardedBlock && fighterAgent != null)
        {
            fighterAgent.OnSuccessfulBlock();
            hasRewardedBlock = true;
        }
    }
    
    public void ProcessSuccessfulDodge()
    {
        if (!hasRewardedDodge && fighterAgent != null)
        {
            fighterAgent.OnSuccessfulDodge();
            hasRewardedDodge = true;
        }
    }
    
    public float GetAttackCooldownNormalized()
    {
        return fighterStats.AttackCooldown > 0.001f ? 
            Mathf.Clamp01((Time.time - lastAttackTime) / fighterStats.AttackCooldown) : 1f;
    }
    
    public float GetBlockCooldownNormalized()
    {
        return Mathf.Clamp01((Time.time - lastBlockTime) / 2.0f);
    }
    
    public float GetDodgeCooldownNormalized()
    {
        return Mathf.Clamp01((Time.time - lastDodgeTime) / 2.0f);
    }
}