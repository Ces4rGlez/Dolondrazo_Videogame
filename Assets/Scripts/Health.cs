using System;
using System.Collections;
using UnityEngine;

public class Health : MonoBehaviour
{
    [Header("Configuración de Vida")]
    [SerializeField] private int maxHealth = 100;
    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth;

    [Header("Feedback de Daño")]
    [SerializeField] private Color hurtColor = new Color(1f, 0.3f, 0.3f, 1f);
    [SerializeField] private float flashDuration = 0.15f;

    public event Action<int, int> OnHealthChanged;
    public event Action OnDeath;

    private SpriteRenderer spriteRenderer;
    private Color originalColor = Color.white;
    private Rigidbody2D rb;
    private bool isDead = false;

    private void Awake()
    {
        CurrentHealth = maxHealth;
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        if (spriteRenderer != null) originalColor = spriteRenderer.color;
    }

    public void TakeDamage(int damageAmount, Vector2 knockbackDirection, float knockbackForce)
    {
        if (isDead) return;

        CurrentHealth = Mathf.Max(0, CurrentHealth - damageAmount);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

        // Feedback visual: parpadeo rojo
        if (spriteRenderer != null)
        {
            StopAllCoroutines();
            StartCoroutine(FlashRedRoutine());
        }

        // Aplicar retroceso físico (Knockback)
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(knockbackDirection * knockbackForce, ForceMode2D.Impulse);
        }

        Debug.Log($"{gameObject.name} recibió {damageAmount} de daño. Vida restante: {CurrentHealth}/{maxHealth}");

        if (CurrentHealth <= 0)
        {
            Die();
        }
    }

    private IEnumerator FlashRedRoutine()
    {
        spriteRenderer.color = hurtColor;
        yield return new WaitForSeconds(flashDuration);
        spriteRenderer.color = originalColor;
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;
        OnDeath?.Invoke();
    }

    public bool IsDead => isDead;

    public void Heal(int amount)
    {
        if (isDead) return;
        CurrentHealth = Mathf.Min(CurrentHealth + amount, maxHealth);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
        Debug.Log($"{gameObject.name} se curo {amount} HP. Vida: {CurrentHealth}/{maxHealth}");
    }
}


