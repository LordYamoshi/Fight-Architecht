using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using System.Collections.Generic;

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

    [Header("Arena Settings")] [SerializeField]
    private float arenaMinX = -5f;

    [SerializeField] private float arenaMaxX = 5f;
    [SerializeField] private float boundsCheckBuffer = 0.2f;
    [SerializeField] private float minimumFightingDistance = 0.8f;
    [SerializeField] private float optimalFightingDistance = 1.0f;

    [Header("Reward Settings")] [SerializeField]
    private float successfulHitReward = 1f;

    [SerializeField] private float successfulBlockReward = 0.5f;
    [SerializeField] private float successfulDodgeReward = 0.5f;
    [SerializeField] private float takeDamagePenalty = 0.01f;
    [SerializeField] private float boundaryPenalty = 0.01f;
    [SerializeField] private float victoryReward = 5.0f;
    [SerializeField] private float defeatPenalty = 0.5f;
    [SerializeField] private float smallActionReward = 0.01f;
    [SerializeField] private float proximityReward = 0.05f;

    [Header("Aggression Settings")] [SerializeField]
    private float attackInitiationReward = 0.3f;

    [SerializeField] private float successfulHitMultiplier = 2.0f;
    [SerializeField] private float distanceThreshold = 2.5f;
    [SerializeField] private float farPenaltyPerSecond = 0.05f;
    [SerializeField] private float attackFrequencyCheckInterval = 5f;
    [SerializeField] private float lowAttackRatePenalty = 0.1f;
    [SerializeField] private float highAttackRateReward = 0.15f;

    [Header("Forced Engagement")] [SerializeField]
    private bool enableForcedEngagement = true;

    [SerializeField] private int forcedEngagementSteps = 100000;
    [SerializeField] private float forcedAttackChance = 0.1f;
    [SerializeField] private float forcedApproachChance = 0.3f;

    [Header("Push Force Settings")] [SerializeField]
    private bool enablePushForce = true;

    [SerializeField] private int pushForceSteps = 50000;
    [SerializeField] private float pushForceStrength = 2.0f;
    [SerializeField] private float minPushDistance = 3.0f;

    [Header("Early Training Boosters")] [SerializeField]
    private bool useEarlyTrainingBoost = true;

    [SerializeField] private int earlyTrainingSteps = 50000;
    [SerializeField] private float earlyMovementBoost = 1.5f;

    private Vector3 targetPosition;
    private Vector3 currentVelocity = Vector3.zero;
    private float movementSmoothTime = 0.15f;
    private float movementThreshold = 0.03f;
    private bool isMoving = false;

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

    // Aggression tracking variables
    private float timeTooFarApart = 0f;
    private float lastAttackFrequencyCheck = 0f;
    private int attacksInTimeWindow = 0;

    // Combat style tracking
    private int actionsPerformed = 0;
    private int uniqueActionsThisEpisode = 0;
    private HashSet<string> actionTypes = new HashSet<string>();

    private void Start()
    {
        InitializeStats();
        lastPosition = transform.position;
        lastAttackFrequencyCheck = Time.time;
    }

    private void InitializeStats()
    {
        /*if (fighterStats == null) return;

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

        SetProperFacingDirection();*/
    }

    public override void OnEpisodeBegin()
    {
        // Reset health with slight randomization for training variety
        currentHealth = maxHealth * Random.Range(0.95f, 1.0f);
        if (healthSystem != null)
        {
            healthSystem.UpdateHealth(currentHealth);
        }

        // Reset action flags
        isAttacking = false;
        isBlocking = false;
        isDodging = false;

        // Reset timers with slight randomization
        lastAttackTime = -Random.Range(0f, attackCooldown * 0.5f);
        lastBlockTime = -Random.Range(0f, 2.0f);
        lastDodgeTime = -Random.Range(0f, 2.0f);

        // Reset reward tracking
        hasRewardedBlock = false;
        hasRewardedDodge = false;
        episodeCumulativeReward = 0f;
        inactivityCounter = 0;
        lastPosition = transform.position;

        // Reset aggression tracking
        timeTooFarApart = 0f;
        lastAttackFrequencyCheck = Time.time;
        attacksInTimeWindow = 0;

        // Reset action tracking
        actionsPerformed = 0;
        uniqueActionsThisEpisode = 0;
        actionTypes.Clear();

        // Ensure proper facing direction
        SetProperFacingDirection();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Self state observations
        sensor.AddObservation(Mathf.Clamp01(currentHealth / Mathf.Max(maxHealth, 0.01f)));
        sensor.AddObservation(isAttacking ? 1 : 0);
        sensor.AddObservation(isBlocking ? 1 : 0);
        sensor.AddObservation(isDodging ? 1 : 0);

        // Position and opponent observations
        if (opponent != null)
        {
            // Distance along X-axis 
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

                // Health advantage/disadvantage
                float healthAdvantage = (currentHealth / maxHealth) - (opponentAI.currentHealth / opponentAI.maxHealth);
                sensor.AddObservation(Mathf.Clamp(healthAdvantage, -1f, 1f));
            }
            else
            {
                // Default values if opponent AI not found
                sensor.AddObservation(1.0f);
                sensor.AddObservation(0);
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
            sensor.AddObservation(0);
        }

        // Cooldown timers
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

        // Force engagement during early training
        if (enableForcedEngagement && Academy.Instance.StepCount < forcedEngagementSteps)
        {
            if (opponent != null)
            {
                float distance = Vector3.Distance(transform.position, opponent.position);

                if (distance <= optimalFightingDistance * 1.5f && Random.value < forcedAttackChance)
                {
                    Debug.Log($"Fighter {fighterID} - Attack forced");
                    actionChoice = 1;
                }

                // Force approach when too far apart
                if (distance > optimalFightingDistance * 2f && Random.value < forcedApproachChance)
                {
                    Debug.Log($"Fighter {fighterID} - Approach forced");
                    movementChoice = 1;
                }
            }
        }

        if (Academy.Instance.StepCount < 50000)
        {
            if (opponent != null)
            {
                float xDistance = Mathf.Abs(transform.position.x - opponent.position.x);
                if (xDistance < 0.3f)
                {
                    float contactReward = 0.2f;
                    AddReward(contactReward);
                    Debug.Log($"Contact proximity reward: {contactReward}");
                }
            }
        }

        // Handle movement with small exploration reward
        if (movementChoice != 0)
        {
            float moveReward = smallActionReward;

            // Moving toward opponent when far is good
            if (movementChoice == 1 && !IsInRange())
            {
                moveReward *= 3f;
            }

            AddReward(moveReward);
            episodeCumulativeReward += moveReward;

            if (movementChoice == 1)
                MoveTowardsOpponent();
            else if (movementChoice == 2)
                MoveAwayFromOpponent();
        }

        switch (actionChoice)
        {
            case 1:
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

            case 2:
                FighterAI opponentAI = opponent?.GetComponent<FighterAI>();
                if (opponentAI != null && opponentAI.isAttacking)
                {
                    AddReward(smallActionReward * 3f);
                    episodeCumulativeReward += smallActionReward * 3f;
                }

                Block();
                break;

            case 3:
                Dodge();
                break;
        }

        // Distance-based rewards to encourage proper fighting distance
        if (opponent != null)
        {
            float xDistance = Mathf.Abs(transform.position.x - opponent.position.x);

            if (xDistance <= optimalFightingDistance * 1.5f && xDistance >= minimumFightingDistance * 0.8f)
            {
                AddReward(proximityReward);
                episodeCumulativeReward += proximityReward;
                Debug.Log($"Fighter {fighterID} at good distance ({xDistance:F2}). Reward: {proximityReward}");
            }

            // Significant penalty for being too far away
            if (xDistance > distanceThreshold)
            {
                float penalty = farPenaltyPerSecond * Time.deltaTime;
                AddReward(-penalty);
                episodeCumulativeReward -= penalty;

                if (Academy.Instance.StepCount % 100 == 0)
                {
                    Debug.Log($"Fighter {fighterID} too far away ({xDistance:F2}). Penalty: {-penalty:F3}");
                }
            }
        }

        // Small negative reward per step to encourage efficient fighting
        AddReward(-0.001f);
        episodeCumulativeReward -= 0.001f;

        // Check for inactivity
        if (Vector3.Distance(transform.position, lastPosition) < 0.01f)
        {
            inactivityCounter++;
            if (inactivityCounter > 10)
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

    private (int actionChoice, int movementChoice) ForcedEngagement(ActionBuffers actionBuffers)
    {
        // Get the original actions
        int actionChoice = actionBuffers.DiscreteActions[0];
        int movementChoice = actionBuffers.DiscreteActions[1];

        if (opponent == null) return (actionChoice, movementChoice);

        float distance = Vector3.Distance(transform.position, opponent.position);

        // Force attack at random intervals when in range
        if (distance <= optimalFightingDistance * 1.5f && Random.value < forcedAttackChance)
        {
            Debug.Log($"Fighter {fighterID} - FORCED ATTACK!");
            actionChoice = 1;
        }

        // Force approach when too far apart
        if (distance > optimalFightingDistance * 2f && Random.value < forcedApproachChance)
        {
            Debug.Log($"Fighter {fighterID} - FORCED APPROACH!");
            movementChoice = 1;
        }

        return (actionChoice, movementChoice);
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

        if (isMoving)
        {
            transform.position = Vector3.SmoothDamp(
                transform.position,
                targetPosition,
                ref currentVelocity,
                movementSmoothTime
            );

            // Stop moving when we're very close to target
            if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
            {
                isMoving = false;
            }
        }

        // Track and penalize staying too far apart
        if (opponent != null)
        {
            float distance = Vector3.Distance(transform.position, opponent.position);

            if (distance > distanceThreshold)
            {
                timeTooFarApart += Time.deltaTime;

                // Apply OCCASIONAL penalty rather than continuous
                if (timeTooFarApart > 2.0f && timeTooFarApart % 1.0f < Time.deltaTime)
                {
                    float farPenalty = farPenaltyPerSecond;
                    AddReward(-farPenalty);
                    Debug.Log($"Fighter {fighterID} too far apart. Penalty: {-farPenalty:F3}");
                }
            }
            else
            {
                // Reset when they get close enough
                timeTooFarApart = 0f;
            }
        }

        // Track and reward attack frequency
        if (Time.time - lastAttackFrequencyCheck > attackFrequencyCheckInterval)
        {
            // Reward or penalize based on attack frequency
            float attackRate = attacksInTimeWindow / attackFrequencyCheckInterval;

            if (attackRate < 0.2f)
            {
                // Penalize low attack frequency
                AddReward(-lowAttackRatePenalty);
                episodeCumulativeReward -= lowAttackRatePenalty;
                Debug.Log(
                    $"Fighter {fighterID} not attacking enough ({attacksInTimeWindow} attacks). Penalty: {-lowAttackRatePenalty}");
            }
            else if (attackRate > 0.6f)
            {
                // Reward high attack frequency
                AddReward(highAttackRateReward);
                episodeCumulativeReward += highAttackRateReward;
                Debug.Log(
                    $"Fighter {fighterID} showing good aggression ({attacksInTimeWindow} attacks)! Reward: {highAttackRateReward}");
            }

            // Reset for next interval
            lastAttackFrequencyCheck = Time.time;
            attacksInTimeWindow = 0;
        }

        // Apply push force during early training
        if (enablePushForce && Academy.Instance.StepCount < pushForceSteps)
        {
            ApplyPushForce();
        }

    }

    private void ApplyPushForce()
    {
        if (opponent == null) return;

        float distance = Vector3.Distance(transform.position, opponent.position);

        // Only apply push when fighters are too far apart
        if (distance > minPushDistance)
        {
            // Calculate direction to opponent
            Vector3 direction = (opponent.position - transform.position).normalized;

            // Apply push force
            transform.position += direction * pushForceStrength * Time.deltaTime;

            Debug.Log($"Fighter {fighterID} pushed toward opponent. Distance: {distance:F2}");
        }
    }

    private void MaintainPositionOnPlane()
    {
        // Keep a consistent Z position for 2D plane
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

        transform.rotation = Quaternion.Euler(0, isOnLeftSide ? 90 : -90, 0);
    }

    private void UpdateFacingDirection()
    {
        if (opponent == null) return;

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

    void OnDrawGizmos()
    {
        // Draw minimum fighting distance
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, minimumFightingDistance);

        // Draw optimal fighting distance
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, optimalFightingDistance);
    }

    private void MoveTowardsOpponent()
    {
        /*if (opponent == null) return;

        // Calculate current distance and direction
        float xDistance = Mathf.Abs(transform.position.x - opponent.position.x);
        float xDirection = transform.position.x < opponent.position.x ? 1 : -1;


        // Calculate speed with potential early training boost
        float moveSpeed = fighterStats.movementSpeed;
        if (useEarlyTrainingBoost && Academy.Instance.StepCount < earlyTrainingSteps)
        {
            moveSpeed *= earlyMovementBoost;
        }

        // If already at or closer than minimum distance, don't move
        if (xDistance <= minimumFightingDistance)
        {
            Debug.Log($"Fighter {fighterID} already at minimum distance ({xDistance:F2})");
            return;
        }

        // Calculate the proposed movement amount based on speed
        float moveAmount = moveSpeed * Time.deltaTime;

        // Calculate what the new distance would be after movement
        float newDistance = xDistance - moveAmount;

        // If new distance would be less than minimum, limit the movement amount
        if (newDistance < minimumFightingDistance)
        {
            // Calculate exactly how much we can move while respecting minimum distance
            moveAmount = xDistance - minimumFightingDistance;
            Debug.Log($"Fighter {fighterID} - Limited movement to respect minimum distance");
        }

        // Calculate new position
        Vector3 newPosition = transform.position;
        newPosition.x += xDirection * moveAmount;

        // Apply boundary checks
        if (newPosition.x < arenaMinX + boundsCheckBuffer)
        {
            newPosition.x = arenaMinX + boundsCheckBuffer;
        }
        else if (newPosition.x > arenaMaxX - boundsCheckBuffer)
        {
            newPosition.x = arenaMaxX - boundsCheckBuffer;
        }

        if (Vector3.Distance(newPosition, transform.position) > movementThreshold)
        {
            targetPosition = newPosition;
            isMoving = true;
        }

        // Update facing direction
        UpdateFacingDirection();

        float finalDistance = Mathf.Abs(transform.position.x - opponent.position.x);
        Debug.Log($"Fighter {fighterID} moving toward opponent. Distance: {xDistance:F2} → {finalDistance:F2}");*/
    }

    private void MoveAwayFromOpponent()
    {
        /*if (opponent == null) return;

        float xDistance = Mathf.Abs(transform.position.x - opponent.position.x);

        float maxRetreatDistance = minimumFightingDistance * 2.0f;
        if (xDistance >= maxRetreatDistance)
        {
            Debug.Log($"Fighter {fighterID} already at max retreat distance ({xDistance:F2})");
            return;
        }

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

        Debug.Log($"Fighter {fighterID} moving away from opponent. Distance: {xDistance:F2} → {Mathf.Abs(transform.position.x - opponent.position.x):F2}");*/
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

        TrackActionVariety("attack");

        // Increment attack counter for frequency tracking
        attacksInTimeWindow++;

        // Much higher reward just for attempting attacks
        float attackReward = attackInitiationReward;
        if (IsInRange())
        {
            attackReward *= 1.5f;
        }

        AddReward(attackReward);
        episodeCumulativeReward += attackReward;

        Debug.Log($"Fighter {fighterID} is attacking. Reward: {attackReward}");
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

        TrackActionVariety("block");

        hasRewardedBlock = false;

        Debug.Log($"Fighter {fighterID} is blocking.");
    }

    public void Dodge()
    {
        if (isDodging || Time.time - lastDodgeTime < 2.0f) return;

        isDodging = true;
        animator.SetTrigger("Dodge");
        lastDodgeTime = Time.time;

        TrackActionVariety("dodge");

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

        // Reduced penalty for taking damage 
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
        // Much higher base reward for landing a hit
        float hitReward = successfulHitReward * successfulHitMultiplier;

        // Bonus for high damage hits
        if (damageDealt > 10)
        {
            hitReward *= 1.5f;
        }

        AddReward(hitReward);
        episodeCumulativeReward += hitReward;

        Debug.Log($"Fighter {fighterID} landed hit for {damageDealt} damage. BIG REWARD: {hitReward}");
    }

    private void TrackActionVariety(string actionType)
    {
        actionsPerformed++;
        if (!actionTypes.Contains(actionType))
        {
            actionTypes.Add(actionType);
            uniqueActionsThisEpisode++;
        }
    }

    public void CompleteEpisode(bool wasVictorious)
    {
        /*// Big reward/penalty for victory/defeat
        float finalReward = wasVictorious ? victoryReward : -defeatPenalty;

        // Add health-based bonus for victory
        if (wasVictorious)
        {
            float healthPercentage = currentHealth / maxHealth;
            float healthBonus = healthPercentage * (victoryReward * 0.2f);
            finalReward += healthBonus;
        }

        // Reward for combat style variety
        if (actionsPerformed > 0)
        {
            float varietyRatio = (float)uniqueActionsThisEpisode / Mathf.Min(actionsPerformed, 20);
            float styleReward = varietyRatio * 0.5f;
            finalReward += styleReward;
            Debug.Log($"Combat variety bonus: {styleReward:F2} ({uniqueActionsThisEpisode} unique / {actionsPerformed} total)");
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
    }*/
    }
}