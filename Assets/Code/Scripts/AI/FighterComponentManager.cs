using UnityEngine;

public class FighterComponentManager : MonoBehaviour
{
    public FighterStats Stats { get; private set; }
    public FighterHealth Health { get; private set; }
    public FighterCombat Combat { get; private set; }
    public FighterMovement Movement { get; private set; }
    public FighterAgent Agent { get; private set; }
    
    private void Awake()
    {
        Stats = GetComponent<FighterStats>();
        Health = GetComponent<FighterHealth>();
        Combat = GetComponent<FighterCombat>();
        Movement = GetComponent<FighterMovement>();
        Agent = GetComponent<FighterAgent>();
    }

    public T GetComponent<T>() where T : Component
    {
        if (typeof(T) == typeof(FighterStats)) return Stats as T;
        if (typeof(T) == typeof(FighterHealth)) return Health as T;
        if (typeof(T) == typeof(FighterCombat)) return Combat as T;
        if (typeof(T) == typeof(FighterMovement)) return Movement as T;
        if (typeof(T) == typeof(FighterAgent)) return Agent as T;
        
        return base.GetComponent<T>();
    }
}