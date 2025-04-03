using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FeedBackSystem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI feedbackText;
    private MatchStats matchStats;
    
    [SerializeField] private GameObject playerObject;
    [SerializeField] private GameObject opponentObject;
    
    private FighterStats playerStats;
    private FighterHealth playerHealth;
    private FighterStats opponentStats;
    private FighterHealth opponentHealth;

    [SerializeField] private List<BalanceParametersSO> balanceParameters = new List<BalanceParametersSO>();
    
    private void Start()
    {
        matchStats = FindObjectOfType<MatchStats>();
        
        // Get component references
        if (playerObject != null)
        {
            playerStats = playerObject.GetComponent<FighterStats>();
            playerHealth = playerObject.GetComponent<FighterHealth>();
        }
        
        if (opponentObject != null)
        {
            opponentStats = opponentObject.GetComponent<FighterStats>();
            opponentHealth = opponentObject.GetComponent<FighterHealth>();
        }
    }

    public void DisplayFeedback()
    {
        if (matchStats == null) return;
        string feedback = matchStats.GenerateFeedback();

        // Highlight imbalances
        string imbalances = HighlightImbalances();

        // Combine match feedback with suggestions
        feedbackText.text = feedback + "\n\nSuggestions:\n" + imbalances;
    }

    private string HighlightImbalances()
    {
        if (playerStats == null || opponentStats == null)
        {
            return "Unable to analyze balance: missing fighter components.";
        }
        
        Dictionary<string, int> imbalances = new Dictionary<string, int>();
    
        //Loops through all balance parameters and checks for imbalances
        foreach (var balanceParameter in balanceParameters)
        {
            float playerValue = GetStatValue(playerStats, playerHealth, balanceParameter.statName);
            float opponentValue = GetStatValue(opponentStats, opponentHealth, balanceParameter.statName);
        
            string message;
            if (balanceParameter.CheckImbalance(playerValue, opponentValue, out message))
            {
                imbalances.Add(message, balanceParameter.priority);
            }
        }

        // Get highest priority message if any
        if (imbalances.Count > 0)
        {
            var highestPriority = imbalances.OrderByDescending(i => i.Value).First();
            return highestPriority.Key;
        }

        return "Stats are well balanced.";
    }
    
    private float GetStatValue(FighterStats stats, FighterHealth health, string statName)
    {
        if (stats == null) return 0f;
        
        switch (statName.ToLower())
        {
            case "movementspeed":
                return stats.MovementSpeed;
            case "strength":
                return stats.Strength;
            case "defense":
                return stats.Defense;
            case "dodgerate":
                return stats.DodgeRate;
            case "attackspeed":
                return stats.AttackSpeed;
            case "health":
            case "maxhealth":
                return stats.Health;
            case "currenthealth":
                return health != null ? health.CurrentHealth : 0f;
            case "attackcooldown":
                return stats.AttackCooldown;
            default:
                Debug.LogWarning($"Unknown stat name: {statName}");
                return 0f;
        }
    }

    public void DisplayCurrentStats(GameObject fighter)
    {
        if (fighter == null) return;
        
        FighterStats stats = fighter.GetComponent<FighterStats>();
        FighterHealth health = fighter.GetComponent<FighterHealth>();
        
        if (stats == null) return;

        string currentStats = $"\nHealth: {(health != null ? health.CurrentHealth : 0)}/{stats.Health}" +
                              $"\nStrength: {stats.Strength}" +
                              $"\nDefense: {stats.Defense}" +
                              $"\nAttack Speed: {stats.AttackSpeed}" +
                              $"\nMovement Speed: {stats.MovementSpeed}" +
                              $"\nAttack Cooldown: {stats.AttackCooldown}" +
                              $"\nDodge Rate: {stats.DodgeRate}";

        feedbackText.text = currentStats;
    }
    
    public void ClearFeedback()
    {
        if (feedbackText != null)
        {
            feedbackText.text = string.Empty;
        }
    }
}