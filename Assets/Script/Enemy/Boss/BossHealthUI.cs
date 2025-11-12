using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BossHealthUI : MonoBehaviour
{
    public BossHealth boss;
    public Slider slider;
    public TextMeshProUGUI hpText;

    private void Start()
    {
        slider.maxValue = boss.maxHealth;
        slider.value = boss.currentHealth;
        hpText.text = $"{boss.currentHealth}/{boss.maxHealth}";

        boss.OnHealthChanged += UpdateUI;
    }

    void UpdateUI(int current, int max)
    {
        slider.value = current;
        hpText.text = $"{current}/{max}";
    }
}
