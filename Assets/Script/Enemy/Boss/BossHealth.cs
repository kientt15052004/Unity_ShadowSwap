using System;
using UnityEngine;

public class BossHealth : MonoBehaviour
{
    public int MaxHealth = 1000;
    public int CurrentHealth { get; private set; }

    public event Action<int> OnHealthChanged;
    public event Action OnHealthDied;

    public bool IsDead => CurrentHealth <= 0;

    private void Awake()
    {
        CurrentHealth = MaxHealth;
    }

    public void TakeDamage(int amount)
    {
        if (IsDead) return;

        CurrentHealth -= amount;
        CurrentHealth = Mathf.Max(CurrentHealth, 0);

        OnHealthChanged?.Invoke(CurrentHealth);

        if (CurrentHealth == 0)
            OnHealthDied?.Invoke();
    }

    public void Heal(int amount)
    {
        if (IsDead) return;

        CurrentHealth += amount;
        CurrentHealth = Mathf.Min(CurrentHealth, MaxHealth);
        OnHealthChanged?.Invoke(CurrentHealth);
    }
}
