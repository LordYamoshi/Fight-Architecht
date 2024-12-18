using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FeedBackSystem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI feedbackText;
    private MatchStats matchStats;
    
    [SerializeField] private FighterAI player;
    [SerializeField] private FighterAI opponent;

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
        string suggestions = "";
        FighterStats playerStats = player.fighterStats;
        FighterStats opponentStats = opponent.fighterStats;
        
     
        if (playerStats.movementSpeed > opponentStats.movementSpeed * 1.5f)
        {
            suggestions += "Player’s speed is significantly higher than Opponent’s. Consider reducing speed by 10% to balance the match.";
        }

        if (playerStats.strength > opponentStats.strength * 1.5f)
        {
            suggestions += "Player’s strength is significantly higher than Opponent’s. Consider reducing strength by 10% to ensure fair play.";
        }

        if (opponentStats.defense > playerStats.defense * 1.5f)
        {
            suggestions += "Opponent’s defense is significantly higher than Player’s. Consider reducing defense by 10% to create openings.";
        }

        if (playerStats.dodgeRate > opponentStats.dodgeRate * 2f)
        {
            suggestions += "Player’s dodge rate is excessively high. Consider reducing dodge rate by 15% to give Opponent a chance.";
        }

        if (opponentStats.attackSpeed > playerStats.attackSpeed * 1.5f)
        {
            suggestions += "Opponent’s attack speed is significantly higher than Player’s. Consider increasing Player’s attack speed by 10%.";
        }

        if (playerStats.health > opponentStats.health * 1.5f)
        {
            suggestions += "Player’s health is disproportionately higher than Opponent’s. Consider reducing health by 15%.";
        }

        if (playerStats.attackCooldown < opponentStats.attackCooldown * 0.5f)
        {
            suggestions += "Player’s attack cooldown is much shorter. Consider increasing cooldown by 10% to level the playing field.";
        }

        if (opponentStats.strength > playerStats.strength * 2f)
        {
            suggestions += "Opponent’s strength is overwhelming. Player should prioritize defensive actions or reduce Opponent’s strength by 10%.";
        }

        if (playerStats.defense > opponentStats.defense * 2f)
        {
            suggestions += "Player’s defense is excessively high. Opponent should focus on breaking through with combos or Player could reduce defense by 10%.";
        }

        if (opponentStats.movementSpeed > playerStats.movementSpeed * 2f)
        {
            suggestions += "Opponent’s movement speed is too high. Consider reducing Opponent’s speed by 15% or increasing Player’s speed by 10%.";
        }

        if (playerStats.health < opponentStats.health * 0.5f)
        {
            suggestions += "Player’s health is critically low compared to Opponent. Consider increasing Player’s health by 20%.";
        }

        if (playerStats.strength < opponentStats.strength * 0.5f)
        {
            suggestions += "Player’s strength is too low. Consider increasing Player’s offensive capabilities by 15%.";
        }

        if (playerStats.attackSpeed > opponentStats.attackSpeed * 2f)
        {
            suggestions += "Player’s attack speed is significantly higher. Consider reducing Player’s speed by 10% to balance exchanges.";
        }

        if (opponentStats.dodgeRate > playerStats.dodgeRate * 2f)
        {
            suggestions += "Opponent’s dodge rate is excessively high. Player should vary attack patterns or reduce Opponent’s dodge rate by 15%.";
        }

        if (playerStats.attackCooldown > opponentStats.attackCooldown * 1.5f)
        {
            suggestions += "Player’s attack cooldown is significantly longer. Consider reducing cooldown time by 10%.";
        }

        if (playerStats.movementSpeed < opponentStats.movementSpeed * 0.5f)
        {
            suggestions += "Player’s movement speed is too low. Consider increasing mobility by 20% to match Opponent.";
        }

        if (playerStats.defense < opponentStats.defense * 0.5f)
        {
            suggestions += "Player’s defense is significantly lower. Consider improving defensive stats by 15%.";
        }

        if (playerStats.health > 200 && opponentStats.health < 100)
        {
            suggestions += "Player has a substantial health advantage. Opponent should focus on dealing consistent damage or Player could reduce health by 20%.";
        }

        if (opponentStats.health > 200 && playerStats.health < 100)
        {
            suggestions += "Opponent has a substantial health advantage. Player should prioritize evasion and defense or Opponent could reduce health by 15%.";
        }


        return string.IsNullOrEmpty(suggestions) ? "Stats are well balanced." : suggestions;
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
