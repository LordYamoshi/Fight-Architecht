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

    [SerializeField] private FighterAI fighterAI;
    [SerializeField] private FighterStats baseStats;
    [SerializeField] private CustomizationManager customizationManager;
    
    private float minMultiplier = 0.25f; // 25% of the base value
    private float maxMultiplier = 5.0f; // 500% of the base value
    
   
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
        if (fighterAI == null || fighterAI.fighterStats == null) return;

        FighterStats baseStats = fighterAI.fighterStats;
        
        SetSliderValues(healthSlider, fighterAI.maxHealth, baseStats.health);
        SetSliderValues(strengthSlider, fighterAI.strength, baseStats.strength);
        SetSliderValues(defenseSlider, fighterAI.defense, baseStats.defense);
        SetSliderValues(movementSpeedSlider, fighterAI.fighterStats.movementSpeed, baseStats.movementSpeed);
        SetSliderValues(attackSpeedSlider, fighterAI.attackSpeed, baseStats.attackSpeed);
        SetSliderValues(attackCooldownSlider, fighterAI.attackCooldown, baseStats.attackCooldown);
        SetSliderValues(dodgeRateSlider, fighterAI.dodgeRate, baseStats.dodgeRate);
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
        if (fighterAI == null || fighterAI.fighterStats == null) return;

        FighterStats baseStats = fighterAI.fighterStats;

        switch (statType)
        {
            case "health":
                fighterAI.maxHealth = sliderValue;
                fighterAI.currentHealth = Mathf.Clamp(fighterAI.currentHealth, 0, fighterAI.maxHealth);
                break;
            case "strength":
                fighterAI.strength = sliderValue;
                break;
            case "defense":
                fighterAI.defense = sliderValue;
                break;
            case "movementSpeed":
                fighterAI.fighterStats.movementSpeed = sliderValue;
                break;
            case "attackSpeed":
                fighterAI.attackSpeed = sliderValue;
                break;
            case "attackCooldown":
                fighterAI.attackCooldown = sliderValue;
                break;
            case "dodgeRate":
                fighterAI.dodgeRate = sliderValue;
                break;
        }
        if (customizationManager != null)
        {
            customizationManager.SaveCustomization();
        }    }
}