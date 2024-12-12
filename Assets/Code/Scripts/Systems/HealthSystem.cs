using UnityEngine;
using UnityEngine.UI;

public class HealthSystem : MonoBehaviour
{
    public Slider currentHealthSlider;
    public Slider targetHealthSlider;
    public Gradient currentHealthGradient;
    public Gradient targetHealthGradient;
    public Image currentHealthFill;
    public Image targetHealthFill;
    private float lerpSpeed = 0.5f;
    private float targetHealth;

    public void SetMaxHealth(float maxHealth)
    {
        currentHealthSlider.maxValue = maxHealth;
        targetHealthSlider.maxValue = maxHealth;
        currentHealthSlider.value = maxHealth;
        targetHealthSlider.value = maxHealth;
        targetHealth = maxHealth;

        currentHealthFill.color = currentHealthGradient.Evaluate(1f);
        targetHealthFill.color = targetHealthGradient.Evaluate(1f);
    }

    public void UpdateHealth(float newHealth)
    {
        targetHealth = newHealth;
        targetHealthSlider.value = newHealth;
        targetHealthFill.color = targetHealthGradient.Evaluate(targetHealthSlider.normalizedValue);
    }

    private void Update()
    {
        currentHealthSlider.value = Mathf.Lerp(currentHealthSlider.value, targetHealth, lerpSpeed * Time.deltaTime);
        currentHealthFill.color = currentHealthGradient.Evaluate(currentHealthSlider.normalizedValue);
    }
}