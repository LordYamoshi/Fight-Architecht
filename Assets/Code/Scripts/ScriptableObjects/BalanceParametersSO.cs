using UnityEngine;

[CreateAssetMenu(fileName = "BalanceParameters", menuName = "Game/BalanceParameters")]
public class BalanceParametersSO : ScriptableObject
{
    public enum ComparisonType
    {
        GreaterThan,
        LessThan
    }
    
    public float adjustmentPercent = 10f;
    public int priority = 1;
    
    public string statName = "strength";
    public ComparisonType comparisonType = ComparisonType.GreaterThan;
    public float threshold = 1.5f;
    
    
    [TextArea(2, 4)]
    public string playerImbalanceMessage = "Player's stat is too high. Consider reducing by {0}%.";
    [TextArea(2, 4)]
    public string opponentImbalanceMessage = "Opponent's stat is too high. Consider increasing player's by {0}%.";
    public bool CheckImbalance(float playerValue, float opponentValue, out string message)
    {
        if (comparisonType == ComparisonType.GreaterThan)
        {
            if (playerValue > opponentValue * threshold)    
            {
                message = string.Format(playerImbalanceMessage, adjustmentPercent);
                return true;
            }
            else if (opponentValue > playerValue * threshold)
            {
                message = string.Format(opponentImbalanceMessage, adjustmentPercent);
                return true;
            }
        }
        else 
        {
            if (playerValue * threshold < opponentValue)
            {
                message = string.Format(playerImbalanceMessage, adjustmentPercent);
                return true;
            }
            else if (opponentValue * threshold < playerValue)
            {
                message = string.Format(opponentImbalanceMessage, adjustmentPercent);
                return true;
            }
        }

        message = string.Empty;
        return false;
    }
}