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

        Dictionary<string, int> imbalances = new Dictionary<string, int>();

        if (playerStats.movementSpeed > opponentStats.movementSpeed * 1.5f)
        {
            imbalances.Add(
                "Player’s speed is significantly higher than Opponent’s. Consider reducing speed by 10% to balance the match.",
                1);
        }
        else if (opponentStats.movementSpeed > playerStats.movementSpeed * 1.5f)
        {
            imbalances.Add(
                "Opponent’s speed is significantly higher than Player’s. Consider increasing Player’s speed by 10% to balance the match.",
                1);
        }

        if (player.strength > opponentStats.strength * 1.5f)
        {
            imbalances.Add(
                "Player’s strength is significantly higher than Opponent’s. Consider reducing strength by 10% to ensure fair play.",
                2);
        }
        else if (opponentStats.strength > player.strength * 1.5f)
        {
            imbalances.Add(
                "Opponent’s strength is significantly higher than Player’s. Consider increasing Player’s strength by 10% to ensure fair play.",
                2);
        }

        if (player.defense > opponentStats.defense * 1.5f)
        {
            imbalances.Add(
                "Player’s defense is significantly higher than Opponent’s. Consider reducing defense by 10% to create openings.",
                3);
        }
        else if (opponentStats.defense > player.defense * 1.5f)
        {
            imbalances.Add(
                "Opponent’s defense is significantly higher than Player’s. Consider increasing Player’s defense by 10% to create openings.",
                3);
        }

        if (player.dodgeRate > opponentStats.dodgeRate * 2f)
        {
            imbalances.Add(
                "Player’s dodge rate is excessively high. Consider reducing dodge rate by 15% to give Opponent a chance.",
                4);
        }
        else if (opponentStats.dodgeRate > player.dodgeRate * 2f)
        {
            imbalances.Add(
                "Opponent’s dodge rate is excessively high. Consider increasing Player’s dodge rate by 15% to give Player a chance.",
                4);
        }

        if (player.attackSpeed > opponentStats.attackSpeed * 1.5f)
        {
            imbalances.Add(
                "Player’s attack speed is significantly higher. Consider reducing Player’s speed by 10% to balance exchanges.",
                5);
        }
        else if (opponentStats.attackSpeed > player.attackSpeed * 1.5f)
        {
            imbalances.Add(
                "Opponent’s attack speed is significantly higher. Consider increasing Player’s attack speed by 10% to balance exchanges.",
                5);
        }

        if (player.maxHealth > opponentStats.health * 1.5f)
        {
            imbalances.Add(
                "Player’s health is disproportionately higher than Opponent’s. Consider reducing health by 15%.", 6);
        }
        else if (opponentStats.health > player.maxHealth * 1.5f)
        {
            imbalances.Add(
                "Opponent’s health is disproportionately higher than Player’s. Consider increasing Player’s health by 15%.",
                6);
        }

        if (player.attackCooldown < opponentStats.attackCooldown * 0.5f)
        {
            imbalances.Add(
                "Player’s attack cooldown is much shorter. Consider increasing cooldown by 10% to level the playing field.",
                7);
        }
        else if (opponentStats.attackCooldown < player.attackCooldown * 0.5f)
        {
            imbalances.Add(
                "Opponent’s attack cooldown is much shorter. Consider increasing Player’s cooldown by 10% to level the playing field.",
                7);
        }

        if (imbalances.Count > 0)
        {
            var highestPriority = imbalances.OrderByDescending(i => i.Value).First();
            suggestions = highestPriority.Key;
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
