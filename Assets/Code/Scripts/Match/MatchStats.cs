using UnityEngine;

public class MatchStats : MonoBehaviour
{
    private int playerHitsLanded;
    private int opponentHitsLanded;
    private int playerDamageDealt;
    private int opponentDamageDealt;
    
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

    public string GenerateFeedback()
    {
        return $"Player Hits Landed: {playerHitsLanded}\n" +
               $"Player Damage Dealt: {playerDamageDealt}\n" +
               $"Opponent Hits Landed: {opponentHitsLanded}\n" +
               $"Opponent Damage Dealt: {opponentDamageDealt}";
    }
}
