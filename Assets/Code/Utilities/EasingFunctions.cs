using UnityEngine;

public static class EasingFunctions
{
    public static float Linear(float t)
    {
        return t;
    }

    public static float EaseIn(float t)
    {
        return t * t;
    }

    public static float EaseOut(float t)
    {
        return t * (2 - t);
    }
    
  
    public static float EaseInSine(float t)
    {
        return 1 - Mathf.Cos(t * Mathf.PI / 2);
    }
    
    public static float EaseOutSine(float t)
    {
        return Mathf.Sin(t * Mathf.PI / 2);
    }
    
    public static float EaseInOutSine(float t)
    {
        return 0.5f * (1 - Mathf.Cos(Mathf.PI * t));
    }
}