using UnityEngine;

public class FighterMovement : MonoBehaviour
{
[Header("Component References")]
    [SerializeField] private FighterStats fighterStats;
    [SerializeField] private FighterAgent fighterAgent;
    
    [Header("Movement Configuration")]
    [SerializeField] private Transform opponent;
    [SerializeField] private float arenaMinX = -5f;
    [SerializeField] private float arenaMaxX = 5f;
    [SerializeField] private float boundsCheckBuffer = 0.2f;
    [SerializeField] private float minimumFightingDistance = 0.8f;
    [SerializeField] private float optimalFightingDistance = 1.0f;
    
    [Header("Enhanced Movement")]
    [SerializeField] private bool useSmoothMovement = true;
    [SerializeField] private float movementSmoothTime = 0.15f;
    [SerializeField] private float movementThreshold = 0.03f;
    
    [Header("Early Training Boost")]
    [SerializeField] private bool useEarlyTrainingBoost = true;
    [SerializeField] private int earlyTrainingSteps = 50000;
    [SerializeField] private float earlyMovementBoost = 1.5f;
    
    [Header("Push Force")]
    [SerializeField] private bool enablePushForce = true;
    [SerializeField] private int pushForceSteps = 50000;
    [SerializeField] private float pushForceStrength = 2.0f;
    [SerializeField] private float minPushDistance = 3.0f;
    
    [Header("Fighter Info")]
    [SerializeField] private int fighterID;

    private Vector3 targetPosition;
    private Vector3 currentVelocity = Vector3.zero;
    private bool isMoving = false;
    
    private void Awake()
    {
        if (fighterStats == null) fighterStats = GetComponent<FighterStats>();
        if (fighterAgent == null) fighterAgent = GetComponent<FighterAgent>();
    }
    
    private void Start()
    {
        SetProperFacingDirection();
    }
    
    private void Update()
    {
        MaintainPositionOnPlane();
        
        EnforceArenaBoundaries();

        if (isMoving && useSmoothMovement)
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
        
        if (enablePushForce && opponent != null && 
            Unity.MLAgents.Academy.Instance.StepCount < pushForceSteps)
        {
            ApplyPushForce();
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
            
            if (fighterAgent != null)
            {
                fighterAgent.OnBoundaryHit();
            }
        }
    }
    
    private void ApplyPushForce()
    {
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
    
    public void MoveTowardsOpponent()
    {
        if (opponent == null) return;

        // Calculate current distance and direction
        float xDistance = Mathf.Abs(transform.position.x - opponent.position.x);
        float xDirection = transform.position.x < opponent.position.x ? 1 : -1;
        
        float moveSpeed = fighterStats.MovementSpeed;
        if (useEarlyTrainingBoost && 
            Unity.MLAgents.Academy.Instance.StepCount < earlyTrainingSteps)
        {
            moveSpeed *= earlyMovementBoost;
        }

        // If already at or closer than minimum distance, don't move
        if (xDistance <= minimumFightingDistance)
        {
            Debug.Log($"Fighter {fighterID} already at minimum distance ({xDistance:F2})");
            return;
        }
        
        float moveAmount = moveSpeed * Time.deltaTime;

        float newDistance = xDistance - moveAmount;
        
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

        if (useSmoothMovement)
        {
            if (Vector3.Distance(newPosition, transform.position) > movementThreshold)
            {
                targetPosition = newPosition;
                isMoving = true;
            }
        }
        else
        {
            transform.position = newPosition;
        }

        // Update facing direction
        UpdateFacingDirection();

        float finalDistance = Mathf.Abs(transform.position.x - opponent.position.x);
        Debug.Log($"Fighter {fighterID} moving toward opponent. Distance: {xDistance:F2} → {finalDistance:F2}");
    }

    public void MoveAwayFromOpponent()
    {
        if (opponent == null) return;
        
        float xDistance = Mathf.Abs(transform.position.x - opponent.position.x);

        float maxRetreatDistance = minimumFightingDistance * 2.0f;
        if (xDistance >= maxRetreatDistance)
        {
            Debug.Log($"Fighter {fighterID} already at max retreat distance ({xDistance:F2})");
            return;
        }

        float xDirection = transform.position.x < opponent.position.x ? -1 : 1;
        float moveAmount = fighterStats.MovementSpeed * Time.deltaTime;
        
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
        
        if (useSmoothMovement)
        {
            if (Vector3.Distance(newPosition, transform.position) > movementThreshold)
            {
                targetPosition = newPosition;
                isMoving = true;
            }
        }
        else
        {
            transform.position = newPosition;
        }

        UpdateFacingDirection();
        
        float finalDistance = Mathf.Abs(transform.position.x - opponent.position.x);
        Debug.Log($"Fighter {fighterID} moving away from opponent. Distance: {xDistance:F2} → {finalDistance:F2}");
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
    
    void OnDrawGizmos()
    {
        // Draw minimum fighting distance
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, minimumFightingDistance);
    
        // Draw optimal fighting distance
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, optimalFightingDistance);
    }
}