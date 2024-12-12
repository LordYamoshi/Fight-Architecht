using UnityEngine;

public class FighterAI : MonoBehaviour
{

    public enum AIState
    {
        Aggressive,
        Defensive,
        Balanced,
    }



    [SerializeField] private AIState currentState;
    [SerializeField] private Transform opponent;
    [SerializeField] private FighterStats fighterStats;
    private float lastAttackTime;

    public float attackSpeed { get; private set; }
    public float strength { get; private set; }
    public float dodgeRate { get; private set; }
    public float attackCooldown { get; private set; }
    public float currentHealth {get; private set; }
    public float maxHealth { get; private set; }
    public float defense {get; private set; }

    public bool isAttacking;
    public bool isDodging;
    public bool isBlocking;

    [SerializeField] private Animator animator;
    
    [SerializeField] private MatchManager matchManager;
    [SerializeField] private HealthSystem healthSystem;
    
    private void Start()
    {
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
    }

    
    
    private void Update()
    {
        if (matchManager == null) return;
        if (!matchManager.isMatchRunning()) return;
        
        EvaluateState();
        switch (currentState)
        {
            case AIState.Aggressive:
                MoveTowardsOpponent();
                if (isInRange()) Attack();
                break;
            case AIState.Defensive:
                if (isInRange())
                {
                    if (Random.value > 0.5f) Attack();
                    else Block();
                }
                else
                {
                    MoveAwayFromOpponent();
                }
                break;
            case AIState.Balanced:
                if (Random.value > 0.33f) MoveTowardsOpponent();
                else if (Random.value > 0.5f) Attack();
                else Block();
                break;
        }
    }


    private void EvaluateState()
    { float healthRatio = currentHealth / maxHealth;
        float opponentHealthRatio = opponent.GetComponent<FighterAI>().currentHealth / opponent.GetComponent<FighterAI>().maxHealth;
        float distanceToOpponent = Vector3.Distance(transform.position, opponent.position);


        if (healthRatio < 0.3f)
        {
            if (distanceToOpponent < 1.0f && Random.value > 0.5f)
            {
                ChangeState(AIState.Aggressive);
            }
            else
            {
                ChangeState(AIState.Defensive);
            }
        }
        else if (healthRatio > 0.7f)
        {
            if (opponentHealthRatio < 0.3f)
            {
                ChangeState(AIState.Aggressive);
            }
            else
            {
                ChangeState(AIState.Balanced);
            }
        }
        else
        {
            if (distanceToOpponent < 1.0f)
            {
                ChangeState(AIState.Defensive);
            }
            else
            {
                ChangeState(AIState.Balanced);
            }
        }
    }

    private void MoveTowardsOpponent()
    {
        if (isInRange()) return;
        transform.rotation = Quaternion.LookRotation(opponent.position - transform.position);
        Vector3 direction = (opponent.position - transform.position).normalized;
        transform.position += direction * fighterStats.movementSpeed * Time.deltaTime;
    }
    
    private void MoveAwayFromOpponent()
    {
        if (!isInRange()) return;
        transform.rotation = Quaternion.LookRotation(transform.position - opponent.position);
        Vector3 direction = (transform.position - opponent.position).normalized;
        transform.position += direction * fighterStats.movementSpeed * Time.deltaTime;
    }

    public void SetHealth(float health)
    {
        currentHealth = health;
        healthSystem.UpdateHealth(currentHealth);
    }
    
    private bool isInRange()
    {
        return Vector3.Distance(transform.position, opponent.position) < 0.5f;
    }

    public void Attack()
    {
        if (Time.time - lastAttackTime < attackCooldown || isDodging || isBlocking) return;
        isAttacking = true;
        animator.SetTrigger("Attack");
        lastAttackTime = Time.time;
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
        animator.SetBool("Block", false);
    }
    
    public void Block()
    {
        if (isAttacking) return;
        isBlocking = true;
        animator.SetBool("Block", true);
    }

    public void TakeDamage(float damage)
    {
        if (isBlocking)
        {
            damage -= defense;
            if (damage < 0) damage = 0;
        }

        currentHealth -= damage;
        healthSystem.UpdateHealth(currentHealth);
        EndBlock();
    }
    
    
    public void ChangeState(AIState newState)
    {
        currentState = newState;
    }
}
