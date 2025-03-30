using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FeedBackSystem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI feedbackText;
    private MatchStats matchStats;
    
    [SerializeField] private FighterAI player;
    [SerializeField] private FighterAI opponent;

    [SerializeField] private List<BalanceParameters> balanceParameters = new List<BalanceParameters>();
    
    private void Start()
    {
        matchStats = FindObjectOfType<MatchStats>();
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
        Dictionary<string, int> imbalances = new Dictionary<string, int>();
    
        //Loops through all balance parameters and checks for imbalances
        foreach (var balanceParameter in balanceParameters)
        {
            float playerValue = GetStatValue(player, balanceParameter.statName);
            float opponentValue = GetStatValue(opponent, balanceParameter.statName);
        
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
    
    private float GetStatValue(FighterAI fighter, string statName)
    {
        switch (statName.ToLower())
        {
            case "movementspeed":
                return fighter.fighterStats.movementSpeed;
            case "strength":
                return fighter.strength;
            case "defense":
                return fighter.defense;
            case "dodgerate":
                return fighter.dodgeRate;
            case "attackspeed":
                return fighter.attackSpeed;
            case "health":
            case "maxhealth":
                return fighter.maxHealth;
            case "attackcooldown":
                return fighter.fighterStats.attackCooldown;
            default:
                Debug.Log($"Unknown stat name: {statName}");
                return 0f;
        }
    }

    public void DisplayCurrentStats(FighterAI fighter)
    {
        if (fighter == null) return;

        string currentStats = $"\nHealth: {fighter.currentHealth}/{fighter.maxHealth}" +
                              $"\nStrength: {fighter.strength}" +
                              $"\nDefense: {fighter.defense}" +
                              $"\nAttack Speed: {fighter.attackSpeed}" +
                              $"\nMovement Speed: {fighter.fighterStats.movementSpeed}" +
                              $"\nAttack Cooldown: {fighter.fighterStats.attackCooldown}" +
                              $"\nDodge Rate: {fighter.dodgeRate}";

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
