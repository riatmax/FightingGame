using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    /// <summary>
    ///
    ///     Tracks how much health the player has left
    ///
    /// </summary>



    [Header("--- Health Settings ---")]
    [SerializeField] private int maxHealth = 100;
    private int _currentHealth;

    public event Action<float> HealthChanged; // Sends the health left as a 0-1 percent

    public bool IsDead => _currentHealth <= 0;


    private void Awake()
    {
        _currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        _currentHealth = Mathf.Max(_currentHealth - damage, 0);
        HealthChanged?.Invoke((float)_currentHealth / maxHealth);
    }
}
