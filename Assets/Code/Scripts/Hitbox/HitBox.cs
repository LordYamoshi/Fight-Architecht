using System;
using UnityEngine;

public class HitBox : MonoBehaviour
{
    [SerializeField] private FighterAI youFighter;
    [SerializeField] private LayerMask opponentLayer;
    
    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject == youFighter.gameObject) return;
    
        FighterAI opponentFighter = other.GetComponent<FighterAI>();
        Debug.Log("HitBox Triggered");
    
        if (opponentFighter == null) return;
        if (!opponentFighter.isAttacking) return;
    
        if ((opponentLayer.value & (1 << other.gameObject.layer)) == 0) return;
    
        float damage = Mathf.Max(youFighter.strength - opponentFighter.defense, 0);
        opponentFighter.TakeDamage(damage);
    
        youFighter.SuccessfulHit(damage);
    
        Debug.Log($"{youFighter.name} hit {opponentFighter.name} for {damage} damage!");
    }
}
