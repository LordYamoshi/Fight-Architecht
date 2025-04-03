using UnityEngine;
using System.IO;

public class CustomizationManager : MonoBehaviour
{
   [SerializeField] private FighterStats fighterStats;
    [SerializeField] private FighterHealth fighterHealth;

    private const string SaveFileName = "fighter_customization.json";

    public void SaveCustomization()
    {
        if (fighterStats == null) return;

        FighterData fighterData = new FighterData
        {
            health = fighterStats.Health,
            strength = fighterStats.Strength,
            defense = fighterStats.Defense,
            movementSpeed = fighterStats.MovementSpeed,
            attackSpeed = fighterStats.AttackSpeed,
            attackCooldown = fighterStats.AttackCooldown,
            dodgeRate = fighterStats.DodgeRate
        };

        string json = JsonUtility.ToJson(fighterData, true);
        File.WriteAllText(Path.Combine(Application.persistentDataPath, SaveFileName), json);
        Debug.Log("Customization saved.");
    }

    public void LoadCustomization()
    {
        string filePath = Path.Combine(Application.persistentDataPath, SaveFileName);
        if (!File.Exists(filePath))
        {
            Debug.Log("No customization file found.");
            return;
        }

        string json = File.ReadAllText(filePath);
        FighterData fighterData = JsonUtility.FromJson<FighterData>(json);

        ApplyCustomization(fighterData);
        Debug.Log("Customization loaded.");
    }

    private void ApplyCustomization(FighterData fighterData)
    {
        if (fighterStats == null) return;

        // Update FighterStats component with loaded values
        fighterStats.UpdateHealth(fighterData.health);
        fighterStats.UpdateStrength(fighterData.strength);
        fighterStats.UpdateDefense(fighterData.defense);
        fighterStats.UpdateMovementSpeed(fighterData.movementSpeed);
        fighterStats.UpdateAttackSpeed(fighterData.attackSpeed);
        fighterStats.UpdateAttackCooldown(fighterData.attackCooldown);
        fighterStats.UpdateDodgeRate(fighterData.dodgeRate);
        
        // If we have access to the health component, update its current health too
        if (fighterHealth != null)
        {
            // Ensure current health doesn't exceed maximum health
            float currentHealth = Mathf.Min(fighterHealth.CurrentHealth, fighterData.health);
            fighterHealth.SetHealth(currentHealth);
        }
    }

    [System.Serializable]
    public class FighterData
    {
        public float health;
        public float strength;
        public float defense;
        public float movementSpeed;
        public float attackSpeed;
        public float attackCooldown;
        public float dodgeRate;
    }
}