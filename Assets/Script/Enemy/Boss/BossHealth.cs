using UnityEngine;
using System;

public class BossHealth : MonoBehaviour
{
    public int maxHealth = 300;
    public int currentHealth;

    public Action<int, int> OnHealthChanged; // (current, max)
    public Action OnDeath;

    private void Start()
    {
        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void TakeDamage(int dmg)
    {
        currentHealth -= dmg;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0)
            Die();
    }

    void Die()
    {
        OnDeath?.Invoke();
        Debug.Log("Boss Dead");
        Destroy(gameObject, 1.5f); // hoặc animation chết
    }
}
