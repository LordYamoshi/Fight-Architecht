using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using System.Collections.Generic;

public class FighterAgent : Agent
{
    [Header("Components")]
    [SerializeField] private FighterStats fighterStats;
    [SerializeField] private FighterCombat fighterCombat;
    [SerializeField] private FighterMovement fighterMovement;
    [SerializeField] private FighterHealth fighterHealth;
    
    [Header("References")]
    [SerializeField] private int fighterID;
    [SerializeField] private Transform opponent;
    [SerializeField] private TrainingManager trainingManager;
    [SerializeField] private MatchManager matchManager;
    [SerializeField] private MatchStats matchStats;
    
    [Header("Arena Settings")]
    [SerializeField] private float arenaMinX = -5f;
    [SerializeField] private float arenaMaxX = 5f;
    [SerializeField] private float minimumFightingDistance = 0.8f;
    [SerializeField] private float optimalFightingDistance = 1.0f;
    
    [Header("Reward Settings")]
    [SerializeField] private float successfulHitReward = 1f;
    [SerializeField] private float successfulBlockReward = 0.5f;
    [SerializeField] private float successfulDodgeReward = 0.5f;
    [SerializeField] private float takeDamagePenalty = 0.01f;
    [SerializeField] private float boundaryPenalty = 0.01f;
    [SerializeField] private float victoryReward = 5.0f;
    [SerializeField] private float defeatPenalty = 0.5f;
    [SerializeField] private float smallActionReward = 0.01f;
    [SerializeField] private float proximityReward = 0.05f;
    [SerializeField] private float attackInitiationReward = 0.3f;
    [SerializeField] private float successfulHitMultiplier = 2.0f;
    
    [Header("Aggression Settings")]
    [SerializeField] private float distanceThreshold = 2.5f;
    [SerializeField] private float farPenaltyPerSecond = 0.05f;
    [SerializeField] private float attackFrequencyCheckInterval = 5f;
    [SerializeField] private float lowAttackRatePenalty = 0.1f;
    [SerializeField] private float highAttackRateReward = 0.15f;
    
    [Header("Forced Engagement")]
    [SerializeField] private bool enableForcedEngagement = true;
    [SerializeField] private int forcedEngagementSteps = 100000;
    [SerializeField] private float forcedAttackChance = 0.1f;
    [SerializeField] private float forcedApproachChance = 0.3f;

    private float episodeCumulativeReward = 0f;
    private int inactivityCounter = 0;
    private Vector3 lastPosition;
    private float timeTooFarApart = 0f;
    private float lastAttackFrequencyCheck = 0f;
    private int attacksInTimeWindow = 0;
    
    private int actionsPerformed = 0;
    private int uniqueActionsThisEpisode = 0;
    private HashSet<string> actionTypes = new HashSet<string>();
    
    private FighterAgent opponentAgent;
    private FighterHealth opponentHealth;
    private FighterCombat opponentCombat;
    
    private void Awake()
    {
        if (fighterStats == null) fighterStats = GetComponent<FighterStats>();
        if (fighterCombat == null) fighterCombat = GetComponent<FighterCombat>();
        if (fighterMovement == null) fighterMovement = GetComponent<FighterMovement>();
        if (fighterHealth == null) fighterHealth = GetComponent<FighterHealth>();
    }
    
    private void Start()
    {
        lastPosition = transform.position;
        lastAttackFrequencyCheck = Time.time;
        
        if (opponent != null)
        {
            opponentAgent = opponent.GetComponent<FighterAgent>();
            opponentHealth = opponent.GetComponent<FighterHealth>();
            opponentCombat = opponent.GetComponent<FighterCombat>();
        }
    }

    private void Update()
    {
        if (matchManager == null || !matchManager.isMatchRunning()) return;

        // Track and penalize staying too far apart
        if (opponent != null)
        {
            
            float sqrDistance = (transform.position - opponent.position).sqrMagnitude;
            float sqrThreshold = distanceThreshold * distanceThreshold;

            if (sqrDistance > sqrThreshold)
            {
                timeTooFarApart += Time.deltaTime;

                float timeSinceLastPenalty = timeTooFarApart - Mathf.Floor(timeTooFarApart);
                if (timeTooFarApart > 2.0f && timeSinceLastPenalty < Time.deltaTime)
                {
                    float farPenalty = farPenaltyPerSecond;
                    AddReward(-farPenalty);
                    Debug.Log($"Fighter {fighterID} too far apart. Penalty: {-farPenalty:F3}");
                }
            }
            else
            {
                timeTooFarApart = 0f;
            }
        }

        // Track and reward attack frequency
        if (Time.time - lastAttackFrequencyCheck > attackFrequencyCheckInterval)
        {
            float attackRate = attacksInTimeWindow / attackFrequencyCheckInterval;

            if (attackRate < 0.2f)
            {
                AddReward(-lowAttackRatePenalty);
                episodeCumulativeReward -= lowAttackRatePenalty;
                Debug.Log($"Fighter {fighterID} not attacking enough ({attacksInTimeWindow} attacks)");
            }
            else if (attackRate > 0.6f)
            {
                AddReward(highAttackRateReward);
                episodeCumulativeReward += highAttackRateReward;
                
                Debug.Log($"Fighter {fighterID} showing good aggression ({attacksInTimeWindow} attacks)!");
            }

            lastAttackFrequencyCheck = Time.time;
            attacksInTimeWindow = 0;
        }

        // Check for inactivity
        float posDiffSqr = (transform.position - lastPosition).sqrMagnitude;
        if (posDiffSqr < 0.0001f)
        {
            inactivityCounter++;
            if (inactivityCounter > 10)
            {
                float inactivityPenalty = 0.02f;
                AddReward(-inactivityPenalty);
                episodeCumulativeReward -= inactivityPenalty;
                Debug.Log($"Fighter {fighterID} inactive");
            }
        }
        else
        {
            inactivityCounter = 0;
            lastPosition = transform.position;
        }
    }
    
    public override void OnEpisodeBegin()
    {
        // Reset reward tracking
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
        
        // Reset fighter components
        if (fighterHealth != null) fighterHealth.ResetHealth();
        if (fighterCombat != null) fighterCombat.ResetCombatState();
    }
    
    public override void CollectObservations(VectorSensor sensor)
    {
        // Self state observations
        sensor.AddObservation(fighterHealth != null ? 
            Mathf.Clamp01(fighterHealth.CurrentHealth / Mathf.Max(fighterHealth.MaxHealth, 0.01f)) : 1.0f);
        
        sensor.AddObservation(fighterCombat != null ? (fighterCombat.IsAttacking ? 1 : 0) : 0);
        sensor.AddObservation(fighterCombat != null ? (fighterCombat.IsBlocking ? 1 : 0) : 0);
        sensor.AddObservation(fighterCombat != null ? (fighterCombat.IsDodging ? 1 : 0) : 0);

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
            if (opponentAgent != null && opponentHealth != null && opponentCombat != null)
            {
                sensor.AddObservation(Mathf.Clamp01(opponentHealth.CurrentHealth / Mathf.Max(opponentHealth.MaxHealth, 0.01f)));
                sensor.AddObservation(opponentCombat.IsAttacking ? 1 : 0);
                sensor.AddObservation(opponentCombat.IsBlocking ? 1 : 0);
                sensor.AddObservation(opponentCombat.IsDodging ? 1 : 0);
                
                // Health advantage/disadvantage
                float healthAdvantage = (fighterHealth.CurrentHealth / fighterHealth.MaxHealth) - 
                                        (opponentHealth.CurrentHealth / opponentHealth.MaxHealth);
                sensor.AddObservation(Mathf.Clamp(healthAdvantage, -1f, 1f));
            }
            else
            {
                // Default values if opponent components not found
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

        // Cooldown timers from combat component
        if (fighterCombat != null)
        {
            sensor.AddObservation(fighterCombat.GetAttackCooldownNormalized());
            sensor.AddObservation(fighterCombat.GetBlockCooldownNormalized());
            sensor.AddObservation(fighterCombat.GetDodgeCooldownNormalized());
        }
        else
        {
            sensor.AddObservation(1f);
            sensor.AddObservation(1f);
            sensor.AddObservation(1f);
        }

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

            if (movementChoice == 1 && fighterMovement != null)
                fighterMovement.MoveTowardsOpponent();
            else if (movementChoice == 2 && fighterMovement != null)
                fighterMovement.MoveAwayFromOpponent();
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

                if (fighterCombat != null)
                {
                    fighterCombat.Attack();
                    attacksInTimeWindow++;
                    TrackActionVariety("attack");
                }
                break;

            case 2:
                if (opponentCombat != null && opponentCombat.IsAttacking)
                {
                    AddReward(smallActionReward * 3f);
                    episodeCumulativeReward += smallActionReward * 3f;
                }

                if (fighterCombat != null)
                {
                    fighterCombat.Block();
                    TrackActionVariety("block");
                }
                break;

            case 3: 
                if (fighterCombat != null)
                {
                    fighterCombat.Dodge();
                    TrackActionVariety("dodge");
                }
                break;
        }
        
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

    private bool IsInRange()
    {
        if (opponent == null) return false;

        float xDistance = Mathf.Abs(transform.position.x - opponent.position.x);

        // Return true if within optimal fighting distance
        return xDistance <= optimalFightingDistance;
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

    public void OnSuccessfulBlock()
    {
        AddReward(successfulBlockReward);
        episodeCumulativeReward += successfulBlockReward;
        Debug.Log($"Fighter {fighterID} successful block. Reward: {successfulBlockReward}");
    }

    public void OnSuccessfulDodge()
    {
        AddReward(successfulDodgeReward);
        episodeCumulativeReward += successfulDodgeReward;
        Debug.Log($"Fighter {fighterID} successful dodge. Reward: {successfulDodgeReward}");
    }

    public void OnSuccessfulHit(float damageDealt)
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

    public void OnTakeDamage(float damage, float originalDamage, bool wasBlocking, bool wasDodging)
    {
        // Reduced penalty for taking damage 
        float damagePenalty = takeDamagePenalty * (damage / fighterHealth.MaxHealth);

        // Less penalty if blocking
        if (wasBlocking)
        {
            damagePenalty *= 0.5f;
        }

        AddReward(-damagePenalty);
        episodeCumulativeReward -= damagePenalty;

        Debug.Log($"Fighter {fighterID} took {damage} damage. Penalty: {-damagePenalty}");
    }

    public void OnBoundaryHit()
    {
        AddReward(-boundaryPenalty);
        episodeCumulativeReward -= boundaryPenalty;
        Debug.Log($"Fighter {fighterID} hit boundary. Penalty: {-boundaryPenalty}");
    }

    public void CompleteEpisode(bool wasVictorious)
    {
        // Big reward/penalty for victory/defeat
        float finalReward = wasVictorious ? victoryReward : -defeatPenalty;

        // Add health-based bonus for victory 
        if (wasVictorious && fighterHealth != null)
        {
            float healthPercentage = fighterHealth.CurrentHealth / fighterHealth.MaxHealth;
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
    }
}
