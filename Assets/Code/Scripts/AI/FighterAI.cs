using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class FighterAI : Agent
{
    [SerializeField] private int fighterID;
    [SerializeField] private Transform opponent;
    [SerializeField] private TrainingManager trainingManager;
    [SerializeField] private MatchManager matchManager;
    [SerializeField] private HealthSystem healthSystem;
    [SerializeField] private Animator animator;
    [SerializeField] private MatchStats matchStats;

    public FighterStats fighterStats;
    public float attackSpeed;
    public float strength;
    public float dodgeRate;
    public float attackCooldown;
    public float currentHealth;
    public float maxHealth;
    public float defense;

    [Header("Arena Settings")]
    [SerializeField] private float arenaMinX = -5f;
    [SerializeField] private float arenaMaxX = 5f;
    [SerializeField] private float boundsCheckBuffer = 0.2f;
    [SerializeField] private float minimumFightingDistance = 1.0f; 
    [SerializeField] private float optimalFightingDistance = 1.2f;

    [Header("Reward Settings")]
    [SerializeField] private float successfulHitReward = 0.5f;
    [SerializeField] private float successfulBlockReward = 0.2f;
    [SerializeField] private float successfulDodgeReward = 0.3f;
    [SerializeField] private float takeDamagePenalty = 0.1f;
    [SerializeField] private float boundaryPenalty = 0.05f;
    [SerializeField] private float victoryReward = 5.0f;
    [SerializeField] private float defeatPenalty = 0.5f;
    [SerializeField] private float smallActionReward = 0.01f;
    [SerializeField] private float proximityReward = 0.05f;
    [SerializeField] private float distancePenalty = 0.03f; 

    [SerializeField] private float attackInitiationReward = 0.15f;  
    [SerializeField] private float successfulHitMultiplier = 2.0f;
    [SerializeField] private float damageReceivedPenaltyScale = 0.05f;
    [SerializeField] private float distanceThreshold = 2.5f; 
    [SerializeField] private float farPenaltyPerSecond = 0.5f;
    
    [Header("Early Training Boosters")]
    [SerializeField] private bool useEarlyTrainingBoost = true;
    [SerializeField] private int earlyTrainingSteps = 50000;
    [SerializeField] private float earlyMovementBoost = 1.5f;

    public bool isAttacking;
    public bool isDodging;
    public bool isBlocking;

    private float lastAttackTime;
    private float lastBlockTime;
    private float lastDodgeTime;
    private float blockStartTime;
    private float blockDuration = 3.0f;
    private float dodgeDuration = 0.4f;

    private bool hasRewardedBlock = false;
    private bool hasRewardedDodge = false;
    private float episodeCumulativeReward = 0f;
    private int inactivityCounter = 0;
    private Vector3 lastPosition;

    private void Start()
    {
        InitializeStats();
        lastPosition = transform.position;
    }

    private void InitializeStats()
    {
        if (fighterStats == null) return;

        attackSpeed = fighterStats.attackSpeed;
        strength = fighterStats.strength;
        dodgeRate = fighterStats.dodgeRate;
        maxHealth = fighterStats.health;
        currentHealth = maxHealth;
        defense = fighterStats.defense;
        attackCooldown = fighterStats.attackCooldown;

        if (healthSystem != null)
        {
            healthSystem.SetMaxHealth(maxHealth);
        }

        SetProperFacingDirection();
    }

    public override void OnEpisodeBegin()
    {
        // Reset health and state
        currentHealth = maxHealth;
        if (healthSystem != null)
        {
            healthSystem.UpdateHealth(currentHealth);
        }

        isAttacking = false;
        isBlocking = false;
        isDodging = false;

        // Reset timers
        lastAttackTime = 0f;
        lastBlockTime = 0f;
        lastDodgeTime = 0f;

        // Reset reward tracking
        hasRewardedBlock = false;
        hasRewardedDodge = false;
        episodeCumulativeReward = 0f;
        inactivityCounter = 0;
        lastPosition = transform.position;

        // Ensure proper facing direction
        SetProperFacingDirection();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Self state observations (safe from NaN)
        sensor.AddObservation(Mathf.Clamp01(currentHealth / Mathf.Max(maxHealth, 0.01f)));
        sensor.AddObservation(isAttacking ? 1 : 0);
        sensor.AddObservation(isBlocking ? 1 : 0);
        sensor.AddObservation(isDodging ? 1 : 0);

        // Position and opponent observations
        if (opponent != null)
        {
            // Distance along X-axis (normalized by arena width)
            float xDistance = opponent.position.x - transform.position.x;
            float arenaWidth = arenaMaxX - arenaMinX;
            float normalizedXDistance = Mathf.Clamp(xDistance / arenaWidth, -1f, 1f);
            sensor.AddObservation(normalizedXDistance);

            // Absolute distance
            float distance = Mathf.Abs(xDistance);
            float normalizedDistance = Mathf.Clamp01(distance / arenaWidth);
            sensor.AddObservation(normalizedDistance);

            // Is in attack range
            sensor.AddObservation(IsInRange() ? 1 : 0);

            // Am I on the left or right of opponent
            sensor.AddObservation(xDistance > 0 ? 1 : -1);

            // Opponent state
            FighterAI opponentAI = opponent.GetComponent<FighterAI>();
            if (opponentAI != null)
            {
                sensor.AddObservation(Mathf.Clamp01(opponentAI.currentHealth / Mathf.Max(opponentAI.maxHealth, 0.01f)));
                sensor.AddObservation(opponentAI.isAttacking ? 1 : 0);
                sensor.AddObservation(opponentAI.isBlocking ? 1 : 0);
                sensor.AddObservation(opponentAI.isDodging ? 1 : 0);
            }
            else
            {
                // Default values if opponent AI not found
                sensor.AddObservation(1.0f);
                sensor.AddObservation(0);
                sensor.AddObservation(0);
                sensor.AddObservation(0);
            }
        }
        else
        {
            // Default values if no opponent
            sensor.AddObservation(0f);
            sensor.AddObservation(1f);
            sensor.AddObservation(0);
            sensor.AddObservation(0);
            sensor.AddObservation(1.0f);
            sensor.AddObservation(0);
            sensor.AddObservation(0);
            sensor.AddObservation(0);
        }

        // Cooldown timers (normalized)
        float attackCooldownNormalized =
            attackCooldown > 0.001f ? Mathf.Clamp01((Time.time - lastAttackTime) / attackCooldown) : 1f;
        sensor.AddObservation(attackCooldownNormalized);

        float blockCooldownNormalized = Mathf.Clamp01((Time.time - lastBlockTime) / 2.0f);
        sensor.AddObservation(blockCooldownNormalized);

        float dodgeCooldownNormalized = Mathf.Clamp01((Time.time - lastDodgeTime) / 2.0f);
        sensor.AddObservation(dodgeCooldownNormalized);

        // Arena boundaries awareness
        float distanceToLeftBoundary = (transform.position.x - arenaMinX) / (arenaMaxX - arenaMinX);
        sensor.AddObservation(Mathf.Clamp01(distanceToLeftBoundary));

        float distanceToRightBoundary = (arenaMaxX - transform.position.x) / (arenaMaxX - arenaMinX);
        sensor.AddObservation(Mathf.Clamp01(distanceToRightBoundary));
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        if (matchManager == null || !matchManager.isMatchRunning()) return;

        // Get discrete actions
        int actionChoice = actionBuffers.DiscreteActions[0];
        int movementChoice = actionBuffers.DiscreteActions[1];

        // Handle movement with small exploration reward
        if (movementChoice != 0)
        {
            float moveReward = smallActionReward;

            // Moving toward opponent when far is good
            if (movementChoice == 1 && !IsInRange())
            {
                moveReward *= 3f; // Stronger reward for approaching when not in range
            }

            AddReward(moveReward);
            episodeCumulativeReward += moveReward;

            if (movementChoice == 1)
                MoveTowardsOpponent();
            else if (movementChoice == 2)
                MoveAwayFromOpponent();
        }

        // Handle actions
        switch (actionChoice)
        {
            case 1: // Attack
                if (IsInRange())
                {
                    AddReward(smallActionReward * 2f);
                    episodeCumulativeReward += smallActionReward * 2f;
                }
                else
                {
                    AddReward(smallActionReward * 0.5f);
                    episodeCumulativeReward += smallActionReward * 0.5f;
                }

                Attack();
                break;

            case 2: // Block
                FighterAI opponentAI = opponent?.GetComponent<FighterAI>();
                if (opponentAI != null && opponentAI.isAttacking)
                {
                    AddReward(smallActionReward * 3f);
                    episodeCumulativeReward += smallActionReward * 3f;
                }

                Block();
                break;

            case 3: // Dodge
                Dodge();
                break;
        }

        // Distance-based rewards to encourage proper fighting distance
        if (opponent != null)
        {
            float xDistance = Mathf.Abs(transform.position.x - opponent.position.x);
            
            // Strong reward for being at optimal fighting distance
            if (xDistance <= optimalFightingDistance * 1.5f && xDistance >= minimumFightingDistance)
            {
                AddReward(proximityReward);
                episodeCumulativeReward += proximityReward;
                Debug.Log($"Fighter {fighterID} at good distance ({xDistance:F2}). Reward: {proximityReward}");
            }
            
            // Significant penalty for being too far away
            if (xDistance > optimalFightingDistance * 3f)
            {
                float penalty = distancePenalty * (xDistance / optimalFightingDistance);
                AddReward(-penalty);
                episodeCumulativeReward -= penalty;
                Debug.Log($"Fighter {fighterID} too far away ({xDistance:F2}). Penalty: {-penalty:F3}");
            }
        }

        // Small negative reward per step to encourage efficient fighting
        AddReward(-0.001f);
        episodeCumulativeReward -= 0.001f;

        // Check for inactivity
        if (Vector3.Distance(transform.position, lastPosition) < 0.01f)
        {
            inactivityCounter++;
            if (inactivityCounter > 10) // Penalize after 10 frames of no movement
            {
                float inactivityPenalty = 0.02f;
                AddReward(-inactivityPenalty);
                episodeCumulativeReward -= inactivityPenalty;
                Debug.Log($"Fighter {fighterID} inactive. Penalty: {-inactivityPenalty}");
            }
        }
        else
        {
            inactivityCounter = 0;
            lastPosition = transform.position;
        }
    }

    private void Update()
    {
        if (matchManager == null || !matchManager.isMatchRunning()) return;

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

        MaintainPositionOnPlane();

        // Ensure fighter stays within arena bounds
        EnforceArenaBoundaries();
    }

    private void MaintainPositionOnPlane()
    {
        float targetZ = 0f;

        if (Mathf.Abs(transform.localPosition.z - targetZ) > 0.1f)
        {
            Vector3 position = transform.localPosition;
            position.z = Mathf.Lerp(position.z, targetZ, Time.deltaTime * 5f);
            transform.localPosition = position;
        }
    }

    private void EnforceArenaBoundaries()
    {
        Vector3 position = transform.position;
        bool wasOutOfBounds = false;

        // Check X boundaries
        if (position.x < arenaMinX)
        {
            position.x = arenaMinX;
            wasOutOfBounds = true;
        }
        else if (position.x > arenaMaxX)
        {
            position.x = arenaMaxX;
            wasOutOfBounds = true;
        }

        // Apply position correction if needed
        if (wasOutOfBounds)
        {
            transform.position = position;
            AddReward(-boundaryPenalty);
            episodeCumulativeReward -= boundaryPenalty;
            Debug.Log($"Fighter {fighterID} hit boundary. Penalty: {-boundaryPenalty}");
        }
    }
    
    private void SetProperFacingDirection()
    {
        if (opponent == null) return;

        // Determine if this fighter is on the left or right side
        bool isOnLeftSide = transform.position.x < opponent.position.x;

        // Set rotation to face right if on left side, left if on right side
        transform.rotation = Quaternion.Euler(0, isOnLeftSide ? 90 : -90, 0);
    }

    private void UpdateFacingDirection()
    {
        if (opponent == null) return;

        // Determine if this fighter is on the left or right side
        bool isOnLeftSide = transform.position.x < opponent.position.x;

        // Only change direction if the fighter has crossed sides
        if ((isOnLeftSide && transform.eulerAngles.y != 90) ||
            (!isOnLeftSide && transform.eulerAngles.y != 270))
        {
            // Update rotation to face opponent
            Vector3 newRotation = new Vector3(0, isOnLeftSide ? 90 : 270, 0);
            transform.rotation = Quaternion.Euler(newRotation);
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;

        // Main actions
        if (Input.GetKey(KeyCode.Space))
            discreteActionsOut[0] = 1;
        else if (Input.GetKey(KeyCode.B))
            discreteActionsOut[0] = 2;
        else if (Input.GetKey(KeyCode.V))
            discreteActionsOut[0] = 3; 
        else
            discreteActionsOut[0] = 0; 

        // Movement
        if (Input.GetKey(KeyCode.W))
            discreteActionsOut[1] = 1; 
        else if (Input.GetKey(KeyCode.S))
            discreteActionsOut[1] = 2;
        else
            discreteActionsOut[1] = 0; 
    }

    private void MoveTowardsOpponent()
    {
        if (opponent == null) return;
        
        // Calculate distance along X-axis
        float xDistance = Mathf.Abs(transform.position.x - opponent.position.x);
        
        // Get movement speed with potential early training boost
        float moveSpeed = fighterStats.movementSpeed;
        if (useEarlyTrainingBoost && Academy.Instance.StepCount < earlyTrainingSteps)
        {
            moveSpeed *= earlyMovementBoost;
        }
        
        // Only move if we're further than the minimum distance
        if (xDistance <= minimumFightingDistance)
        {
            Debug.Log($"Fighter {fighterID} already at fighting distance ({xDistance:F2})");
            return;
        }
        
        // Determine direction along X-axis
        float xDirection = transform.position.x < opponent.position.x ? 1 : -1;
        
        // Calculate new position
        Vector3 newPosition = transform.position;
        newPosition.x += xDirection * moveSpeed * Time.deltaTime;
        
        // Apply position with boundary check
        if (newPosition.x < arenaMinX + boundsCheckBuffer)
        {
            newPosition.x = arenaMinX + boundsCheckBuffer;
        }
        else if (newPosition.x > arenaMaxX - boundsCheckBuffer)
        {
            newPosition.x = arenaMaxX - boundsCheckBuffer;
        }
        
        float newDistance = Mathf.Abs(newPosition.x - opponent.position.x);
        if (newDistance < minimumFightingDistance)
        {
            Debug.Log($"Movement would be too close ({newDistance:F2}), adjusting");
            newPosition.x = opponent.position.x - (minimumFightingDistance * xDirection);
        }
        
        transform.position = newPosition;
        
        // Update facing direction
        UpdateFacingDirection();
        
        Debug.Log($"Fighter {fighterID} moving toward opponent. Distance: {xDistance:F2} → {Mathf.Abs(transform.position.x - opponent.position.x):F2}");
    }

    private void MoveAwayFromOpponent()
    {
        if (opponent == null) return;
        
        // Calculate distance along X-axis
        float xDistance = Mathf.Abs(transform.position.x - opponent.position.x);
        
        // Only retreat to a reasonable distance
        float maxRetreatDistance = minimumFightingDistance * 2.0f;
        if (xDistance >= maxRetreatDistance)
        {
            Debug.Log($"Fighter {fighterID} already at max retreat distance ({xDistance:F2})");
            return;
        }
        
        // Determine direction 
        float xDirection = transform.position.x < opponent.position.x ? -1 : 1;
        
        // Calculate new position
        Vector3 newPosition = transform.position;
        newPosition.x += xDirection * fighterStats.movementSpeed * Time.deltaTime;
        
        // Apply position with boundary check
        if (newPosition.x < arenaMinX + boundsCheckBuffer)
        {
            newPosition.x = arenaMinX + boundsCheckBuffer;
        }
        else if (newPosition.x > arenaMaxX - boundsCheckBuffer)
        {
            newPosition.x = arenaMaxX - boundsCheckBuffer;
        }
        
        transform.position = newPosition;
        
        UpdateFacingDirection();
        
        Debug.Log($"Fighter {fighterID} moving away from opponent. Distance: {xDistance:F2} → {Mathf.Abs(transform.position.x - opponent.position.x):F2}");
    }

    private bool IsInRange()
    {
        if (opponent == null) return false;

        float xDistance = Mathf.Abs(transform.position.x - opponent.position.x);

        // Return true if within optimal fighting distance
        return xDistance <= optimalFightingDistance;
    }

    public void Attack()
    {
        // Check if attack is on cooldown or fighter is performing another action
        if (Time.time - lastAttackTime < attackCooldown || isDodging || isBlocking) return;

        isAttacking = true;
        animator.SetTrigger("Attack");
        matchStats.LogHit(fighterID, (int)strength);
        lastAttackTime = Time.time;

        // Give small reward for attempting attack when in range
        if (IsInRange())
        {
            AddReward(0.5f);
            episodeCumulativeReward += 0.5f;
        }

        Debug.Log($"Fighter {fighterID} is attacking.");
    }

    public void Block()
    {
        // Check if block is on cooldown
        if (isBlocking || Time.time - lastBlockTime < 2.0f) return;

        isBlocking = true;
        animator.SetBool("Block", true);
        lastBlockTime = Time.time;
        blockStartTime = Time.time;
        matchStats.LogDefense(fighterID, true);
        
        hasRewardedBlock = false;
        
        Debug.Log($"Fighter {fighterID} is blocking.");
    }

    public void Dodge()
    {
        // Check if dodge is on cooldown
        if (isDodging || Time.time - lastDodgeTime < 2.0f) return;

        isDodging = true;
        animator.SetTrigger("Dodge");
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
        animator.SetBool("Block", false);
        Debug.Log($"Fighter {fighterID} stopped blocking.");
    }

    public void EndDodge()
    {
        isDodging = false;
        Debug.Log($"Fighter {fighterID} finished dodging.");
    }

    public void SetHealth(float health)
    {
        currentHealth = health;
        healthSystem.UpdateHealth(currentHealth);
    }

    public void TakeDamage(float damage)
    {
        float originalDamage = damage;

        // Apply defense if blocking
        if (isBlocking)
        {
            damage -= defense;
            if (damage < 0) damage = 0;

            // Reward for successful block
            if (!hasRewardedBlock)
            {
                AddReward(successfulBlockReward);
                episodeCumulativeReward += successfulBlockReward;
                hasRewardedBlock = true;
                Debug.Log($"Fighter {fighterID} successful block. Reward: {successfulBlockReward}");
            }
        }
        else if (isDodging)
        {
            // Reward for successful dodge
            if (!hasRewardedDodge)
            {
                AddReward(successfulDodgeReward);
                episodeCumulativeReward += successfulDodgeReward;
                hasRewardedDodge = true;
                Debug.Log($"Fighter {fighterID} successful dodge. Reward: {successfulDodgeReward}");
            }
        }

        // Take damage
        currentHealth -= damage;

        // Penalty for taking damage 
        float damagePenalty = takeDamagePenalty * (damage / maxHealth);

        // Less penalty if blocking
        if (isBlocking)
        {
            damagePenalty *= 0.5f;
        }

        AddReward(-damagePenalty);
        episodeCumulativeReward -= damagePenalty;

        Debug.Log($"Fighter {fighterID} took {damage} damage. Penalty: {-damagePenalty}");

        // Check for defeat
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            CompleteEpisode(false);

            if (matchManager != null)
                matchManager.EndMatch();

            Debug.Log($"Fighter {fighterID} has been defeated.");
        }

        healthSystem.UpdateHealth(currentHealth);
        EndBlock();
    }

    public void SuccessfulHit(float damageDealt)
    {
        // Base reward for landing a hit
        float hitReward = successfulHitReward;

        // Bonus for high damage hits
        if (damageDealt > 10)
        {
            hitReward *= 1.5f;
        }

        AddReward(hitReward);
        episodeCumulativeReward += hitReward;

        Debug.Log($"Fighter {fighterID} landed hit for {damageDealt} damage. Reward: {hitReward}");
    }

    public void CompleteEpisode(bool wasVictorious)
    {
        // Big reward/penalty for victory/defeat
        float finalReward = wasVictorious ? victoryReward : -defeatPenalty;

        // Add health-based bonus for victory (rewarding dominance)
        if (wasVictorious)
        {
            float healthPercentage = currentHealth / maxHealth;
            float healthBonus = healthPercentage * (victoryReward * 0.2f);
            finalReward += healthBonus;
        }

        AddReward(finalReward);
        episodeCumulativeReward += finalReward;

        Debug.Log($"Fighter {fighterID} episode complete. Victory: {wasVictorious}. " +
                  $"Final reward: {finalReward}. Total episode reward: {episodeCumulativeReward}");

        // Notify training manager
        if (trainingManager != null)
            trainingManager.NotifyEpisodeComplete(this);

        // End episode
        EndEpisode();
    }
}