using System;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{

    [SerializeField] private Transform fighter1;
    [SerializeField] private Transform fighter2;
    [SerializeField] private float cameraSpeed = 5f;
    [SerializeField] private string easingFunction = "EaseIn";

    private void LateUpdate()
    {
        if (fighter1 == null || fighter2 == null) return;
        
        var middlePoint = (fighter1.position + fighter2.position) / 2;
        var targetPosition = new Vector3(middlePoint.x, transform.position.y, transform.position.z);
       
        
        float t = Time.deltaTime * cameraSpeed;
        t = ApplyEasingFunction(t);

        transform.position = Vector3.Lerp(transform.position, targetPosition, t);
    }
    
    private float ApplyEasingFunction(float t)
    {
        switch (easingFunction)
        {
            case "Linear":
                return EasingFunctions.Linear(t);
            case "EaseIn":
                return EasingFunctions.EaseIn(t);
            case "EaseOut":
                return EasingFunctions.EaseOut(t);
            case "EaseInSine":
                return EasingFunctions.EaseInSine(t);
            case "EaseOutSine":
                return EasingFunctions.EaseOutSine(t);
            case "EaseInOutSine":
                return EasingFunctions.EaseInOutSine(t);
            default:
                throw new ArgumentException("Invalid easing function");
        }
    }
}
