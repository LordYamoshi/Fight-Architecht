using UnityEngine;
using System.IO;

public class CustomizationManager : MonoBehaviour
{
    [SerializeField] private FighterAI fighterAI;

    private const string SaveFileName = "fighter_customization.json";

    public void SaveCustomization()
    {
        if (fighterAI == null) return;

        FighterData fighterData = new FighterData
        {
            health = fighterAI.maxHealth,
            strength = fighterAI.strength,
            defense = fighterAI.defense,
            movementSpeed = fighterAI.fighterStats.movementSpeed,
            attackSpeed = fighterAI.attackSpeed,
            attackCooldown = fighterAI.attackCooldown,
            dodgeRate = fighterAI.dodgeRate
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
        if (fighterAI == null) return;

        fighterAI.maxHealth = fighterData.health;
        fighterAI.currentHealth = Mathf.Clamp(fighterAI.currentHealth, 0, fighterAI.maxHealth);
        fighterAI.strength = fighterData.strength;
        fighterAI.defense = fighterData.defense;
        fighterAI.fighterStats.movementSpeed = fighterData.movementSpeed;
        fighterAI.attackSpeed = fighterData.attackSpeed;
        fighterAI.attackCooldown = fighterData.attackCooldown;
        fighterAI.dodgeRate = fighterData.dodgeRate;
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