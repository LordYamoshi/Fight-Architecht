using UnityEngine;

public class FighterAI : MonoBehaviour
{

    public enum AIState
    {
        Aggressive,
        Defensive,
        Balanced,
    }


    [SerializeField] private int fighterID;
    [SerializeField] private MatchStats matchStats;
    [SerializeField] private AIState currentState;
    [SerializeField] private Transform opponent;
    public FighterStats fighterStats;

    private float lastAttackTime;
    private float lastBlockTime;
    private float lastDodgeTime;
    private float stateChangeTime;
    private float stateChangeInterval = 2.0f;
    private float blockDuration = 3.0f; 
    private float blockStartTime;
    private float dodgeDuration = 0.4f;

    public float attackSpeed;
    public float strength;
    public float dodgeRate;
    public float attackCooldown;
    public float currentHealth;
    public float maxHealth;
    public float defense;

    public bool isAttacking;
    public bool isDodging;
    public bool isBlocking;

    [SerializeField] private Animator animator;

    [SerializeField] private MatchManager matchManager;
    [SerializeField] private HealthSystem healthSystem;

    
    
    private void Start()
    {
        if (fighterStats == null) return;
        if (healthSystem != null)
        {
            healthSystem.SetMaxHealth(fighterStats.health);
        }
        
        attackSpeed = fighterStats.attackSpeed;
        strength = fighterStats.strength;
        dodgeRate = fighterStats.dodgeRate;
        maxHealth = fighterStats.health;
        currentHealth = maxHealth;
        defense = fighterStats.defense;
        attackCooldown = fighterStats.attackCooldown;
        stateChangeTime = Time.time;
    }
    

    private void Update()
    {
        if (matchManager == null || !matchManager.isMatchRunning()) return;

        if (isBlocking && Time.time - blockStartTime > blockDuration)
        {
            EndBlock();
        }

        if (isDodging && Time.time - lastDodgeTime > dodgeDuration)
        {
            EndDodge();
        }

        if (Time.time - stateChangeTime >= stateChangeInterval)
        {
            ChangeRandomState();
            stateChangeTime = Time.time;
        }

        ActBasedOnState();
    }
    
    
    private void ChangeRandomState()
    {
        currentState = (AIState)Random.Range(0, 3);
        Debug.Log($"Fighter {fighterID} switched to {currentState} state.");
    }
    
    
    private void ActBasedOnState()
    {
        switch (currentState)
        {
            case AIState.Aggressive:
                if (isInRange()) Attack();
                else MoveTowardsOpponent();
                break;

            case AIState.Defensive:
                if (isInRange())
                {
                    if (Random.value > 0.5f) Attack();
                    else Block();
                }
                else MoveAwayFromOpponent();
                break;

            case AIState.Balanced:
                ActBalanced();
                break;
        }
    }
    
    private void ActBalanced()
    {
        if (isInRange())
        {
            float actionChance = Random.value;
            if (actionChance < 0.5f) Attack(); 
            else if (actionChance < 0.8f) Dodge(); 
            else Block(); 
        }
        else
        {
            if (Random.value < 0.7f) MoveTowardsOpponent(); 
            else Dodge(); 
        }
    }
    
    private void MoveTowardsOpponent()
    {
        if (isInRange()) return;
        transform.rotation = Quaternion.LookRotation(opponent.position - transform.position);
        transform.position += (opponent.position - transform.position).normalized * fighterStats.movementSpeed * Time.deltaTime;
    }

    private void MoveAwayFromOpponent()
    {
        if (!isInRange()) return;
        transform.rotation = Quaternion.LookRotation(transform.position - opponent.position);
        transform.position += (transform.position - opponent.position).normalized * fighterStats.movementSpeed * Time.deltaTime;
    }

    private bool isInRange()
    {
        return Vector3.Distance(transform.position, opponent.position) < 0.5f;
    }

    public void SetHealth(float health)
    {
        currentHealth = health;
        healthSystem.UpdateHealth(currentHealth);
    }
    
    public void Attack()
    {
        
        if (Time.time - lastAttackTime < attackCooldown || isDodging || isBlocking) return;
        isAttacking = true;
        animator.SetTrigger("Attack");
        matchStats.LogHit(fighterID, (int)strength);
        lastAttackTime = Time.time;
        Debug.Log($"Fighter {fighterID} is attacking.");
    }

    public void StartAttack()
    {
        isAttacking = true;
    }
    
    public void EndAttack()
    {
        isAttacking = false;
    }

    public void Block()
    {
        if (isBlocking || Time.time - lastBlockTime < 2.0f) return;
        isBlocking = true;
        animator.SetBool("Block", true);
        lastBlockTime = Time.time;
        blockStartTime = Time.time;
        matchStats.LogDefense(fighterID, true); // Log successful defense
        Debug.Log($"Fighter {fighterID} is blocking.");
    }

    
    public void EndBlock()
    {
        isBlocking = false;
        animator.SetBool("Block", false);
        Debug.Log($"Fighter {fighterID} stopped blocking.");
    }

    public void Dodge()
    {
        if (isDodging || Time.time - lastDodgeTime < 2.0f) return;
        isDodging = true;
        animator.SetTrigger("Dodge");
        lastDodgeTime = Time.time;
        Debug.Log($"Fighter {fighterID} dodged.");
    }

    public void EndDodge()
    {
        isDodging = false;
        Debug.Log($"Fighter {fighterID} finished dodging.");
    }
    
    public void TakeDamage(float damage)
    {
        if (isBlocking)
        {
            damage -= defense;
            if (damage < 0) damage = 0;
        }

        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            matchManager.EndMatch();
            Debug.Log($"Fighter {fighterID} has been defeated.");
        }

        healthSystem.UpdateHealth(currentHealth);
        EndBlock();
    }
}

