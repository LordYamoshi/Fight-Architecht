using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class CustomizationUI : MonoBehaviour
{
    public Slider healthSlider;
    public Slider strengthSlider;
    public Slider defenseSlider;
    public Slider movementSpeedSlider;
    public Slider attackSpeedSlider;
    public Slider attackCooldownSlider;
    public Slider dodgeRateSlider;

    [SerializeField] private GameObject fighterObject;
    [SerializeField] private FighterStats baseStats;
    [SerializeField] private CustomizationManager customizationManager;
    
    // Component references
    private FighterStats fighterStats;
    private FighterHealth fighterHealth;
    
    private float minMultiplier = 0.25f; // 25% of the base value
    private float maxMultiplier = 5.0f; // 500% of the base value
    
    private void Awake()
    {
        // Get component references
        if (fighterObject != null)
        {
            fighterStats = fighterObject.GetComponent<FighterStats>();
            fighterHealth = fighterObject.GetComponent<FighterHealth>();
        }
    }
   
    private void Start()
    {
        if (customizationManager != null)
        {
            customizationManager.LoadCustomization(); 
        }

        InitializeSliders();

        healthSlider.onValueChanged.AddListener(value => UpdateStat(value, "health"));
        strengthSlider.onValueChanged.AddListener(value => UpdateStat(value, "strength"));
        defenseSlider.onValueChanged.AddListener(value => UpdateStat(value, "defense"));
        movementSpeedSlider.onValueChanged.AddListener(value => UpdateStat(value, "movementSpeed"));
        attackSpeedSlider.onValueChanged.AddListener(value => UpdateStat(value, "attackSpeed"));
        attackCooldownSlider.onValueChanged.AddListener(value => UpdateStat(value, "attackCooldown"));
        dodgeRateSlider.onValueChanged.AddListener(value => UpdateStat(value, "dodgeRate"));
    }

    private void InitializeSliders()
    {
        if (fighterStats == null) return;

        // Use base stats as reference point if provided, otherwise use current stats
        FighterStats referenceStats = baseStats != null ? baseStats : fighterStats;
        
        SetSliderValues(healthSlider, fighterStats.Health, referenceStats.Health);
        SetSliderValues(strengthSlider, fighterStats.Strength, referenceStats.Strength);
        SetSliderValues(defenseSlider, fighterStats.Defense, referenceStats.Defense);
        SetSliderValues(movementSpeedSlider, fighterStats.MovementSpeed, referenceStats.MovementSpeed);
        SetSliderValues(attackSpeedSlider, fighterStats.AttackSpeed, referenceStats.AttackSpeed);
        SetSliderValues(attackCooldownSlider, fighterStats.AttackCooldown, referenceStats.AttackCooldown);
        SetSliderValues(dodgeRateSlider, fighterStats.DodgeRate, referenceStats.DodgeRate);
    }
    
    private void SetSliderValues(Slider slider, float currentValue, float baseValue)
    {
        float adjustedMin = Mathf.Max(baseValue * minMultiplier, 0.01f);
        slider.minValue = adjustedMin;
        slider.maxValue = baseValue * maxMultiplier;
        slider.value = Mathf.Clamp(currentValue, adjustedMin, slider.maxValue);
    
        Debug.Log($"Setting slider values: Min={slider.minValue}, Max={slider.maxValue}, Value={slider.value}");
    }

    private void UpdateStat(float sliderValue, string statType)
    {
        if (fighterStats == null) return;
        
        switch (statType)
        {
            case "health":
                fighterStats.UpdateHealth(sliderValue);
                if (fighterHealth != null)
                {
                    float currentHealth = Mathf.Clamp(fighterHealth.CurrentHealth, 0, sliderValue);
                    fighterHealth.SetHealth(currentHealth);
                }
                break;
            case "strength":
                fighterStats.UpdateStrength(sliderValue);
                break;
            case "defense":
                fighterStats.UpdateDefense(sliderValue);
                break;
            case "movementSpeed":
                fighterStats.UpdateMovementSpeed(sliderValue);
                break;
            case "attackSpeed":
                fighterStats.UpdateAttackSpeed(sliderValue);
                break;
            case "attackCooldown":
                fighterStats.UpdateAttackCooldown(sliderValue);
                break;
            case "dodgeRate":
                fighterStats.UpdateDodgeRate(sliderValue);
                break;
        }
        
        if (customizationManager != null)
        {
            customizationManager.SaveCustomization();
        }
    }
}