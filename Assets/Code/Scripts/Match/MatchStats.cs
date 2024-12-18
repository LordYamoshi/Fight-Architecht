using TMPro;
using UnityEngine.UI;
using UnityEngine;

public class MatchStats : MonoBehaviour
{
    private int playerHitsLanded;
    private int opponentHitsLanded;
    private int playerDamageDealt;
    private int opponentDamageDealt;
    private int playerSuccessfulDefenses;
    private int opponentSuccessfulDefenses;
    
    
    public void LogHit(int fighterID, int damage)
    {
        if (fighterID == 1)
        {
            playerHitsLanded++;
            playerDamageDealt += damage;
        }
        else
        {
            opponentHitsLanded++;
            opponentDamageDealt += damage;
        }
    }

    public void LogDefense(int fighterID, bool wasSuccessful)
    {
        if (wasSuccessful)
        {
            if (fighterID == 1)
                playerSuccessfulDefenses++;
            else
                opponentSuccessfulDefenses++;
        }
    }

    public int GetPlayerHitsLanded() => playerHitsLanded;
    public int GetOpponentHitsLanded() => opponentHitsLanded;
    public int GetPlayerDamageDealt() => playerDamageDealt;
    public int GetOpponentDamageDealt() => opponentDamageDealt;
    public int GetPlayerSuccessfulDefenses() => playerSuccessfulDefenses;
    public int GetOpponentSuccessfulDefenses() => opponentSuccessfulDefenses;

    public string GenerateFeedback()
    {
        return $"Match Summary:\n" +
               $"Player Hits Landed: {playerHitsLanded}\n" +
               $"Player Damage Dealt: {playerDamageDealt}\n" +
               $"Player Successful Defenses: {playerSuccessfulDefenses}\n" +
               $"Opponent Hits Landed: {opponentHitsLanded}\n" +
               $"Opponent Damage Dealt: {opponentDamageDealt}\n" +
               $"Opponent Successful Defenses: {opponentSuccessfulDefenses}";
    }


    public void ResetStats()
    {
        playerHitsLanded = 0;
        opponentHitsLanded = 0;
        playerDamageDealt = 0;
        opponentDamageDealt = 0;
        playerSuccessfulDefenses = 0;
        opponentSuccessfulDefenses = 0;
        
    }
}
    