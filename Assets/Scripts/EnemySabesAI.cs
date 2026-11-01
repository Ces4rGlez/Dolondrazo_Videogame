using System.Collections;
using UnityEngine;

/// <summary>
/// EnemySabesAI - IA para los estudiantes de la preparatoria SABES (Nivel 3).
/// Arquetipo: Callejero y Elusivo.
/// Habilidad especial: Esquiva táctica (SABES-esquivar-especial).
/// Esquiva los golpes de Fercho deslizándose hacia atrás de forma ágil,
/// volviéndose invulnerable brevemente y preparando un contragolpe.
/// </summary>
public enum SabesState { Idle, Chasing, Dodging, Attacking, Stunned, Dead }

[RequireComponent(typeof(Rigidbody2D), typeof(Health))]
public class EnemySabesAI : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 4.2f;
    [SerializeField] private float stopDistance = 1.30f;

    [Header("Combate SABES")]
    [SerializeField] private float punchRange = 1.85f;
    [SerializeField] private float attackCooldown = 1.2f;
    [SerializeField] private int punchDamage = 16;
    [SerializeField] private float punchKnockback = 4.0f;
    [SerializeField] private LayerMask playerLayer;
    [SerializeField] private AudioClip punchSound;

    [Header("Habilidad Especial: Esquiva")]
    [SerializeField] [Range(0f, 1f)] private float dodgeChance = 0.50f;
    [SerializeField] private float dodgeCooldown = 2.5f;
    [SerializeField] private float dodgeSpeed = 6.0f;
    [SerializeField] private float dodgeDuration = 1.25f;
    [SerializeField] private AudioClip dodgeWhooshSound;

    private SabesState currentState = SabesState.Idle;
    private Transform playerTransform;
    private Health playerHealth;
    private Rigidbody2D rb;
    private Health health;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private float nextAttackTime = 0f;
    private float nextDodgeTime = 0f;
    private bool isDodging = false;
    private int lastHealthRecorded;
    private Vector3 baseScale;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<Health>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        baseScale = transform.localScale;

        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
    }

    private void Start()
    {
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            playerTransform = player.transform;
            playerHealth = player.GetComponent<Health>();
            currentState = SabesState.Chasing;
        }
        else
        {
            GameObject pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj != null)
            {
                playerTransform = pObj.transform;
                playerHealth = pObj.GetComponent<Health>();
                currentState = SabesState.Chasing;
            }
        }

        if (health != null)
        {
            lastHealthRecorded = health.CurrentHealth;
            health.OnHealthChanged += HandleDamageReceived;
            health.OnDeath += HandleDeath;
        }
    }

    private void Update()
    {
        if (currentState == SabesState.Dead || playerTransform == null) return;

        float distance = Vector2.Distance(transform.position, playerTransform.position);

        if (currentState == SabesState.Chasing)
        {
            if (distance <= punchRange && Time.time >= nextAttackTime)
            {
                ExecutePunch();
            }
        }
    }

    private void FixedUpdate()
    {
        if (currentState != SabesState.Chasing || playerTransform == null)
        {
            if (currentState != SabesState.Dodging)
            {
                rb.linearVelocity = Vector2.zero;
            }
            return;
        }

        float distance = Vector2.Distance(transform.position, playerTransform.position);

        if (distance > stopDistance)
        {
            Vector2 dir = (playerTransform.position - transform.position).normalized;
            rb.linearVelocity = dir * moveSpeed;

            if (animator != null) animator.SetBool("IsRunning", true);

            FacePlayer();
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
            if (animator != null) animator.SetBool("IsRunning", false);
            FacePlayer();
        }
    }

    private void FacePlayer()
    {
        if (playerTransform == null) return;
        if (playerTransform.position.x < transform.position.x - 0.05f)
        {
            transform.localScale = new Vector3(-Mathf.Abs(baseScale.x), baseScale.y, 1f);
        }
        else if (playerTransform.position.x > transform.position.x + 0.05f)
        {
            transform.localScale = new Vector3(Mathf.Abs(baseScale.x), baseScale.y, 1f);
        }
    }

    private void ExecutePunch()
    {
        currentState = SabesState.Attacking;
        nextAttackTime = Time.time + attackCooldown;
        rb.linearVelocity = Vector2.zero;

        FacePlayer();

        StartCoroutine(PunchRoutine());
    }

    private IEnumerator PunchRoutine()
    {
        if (animator != null)
        {
            animator.SetBool("IsRunning", false);
            animator.SetTrigger("Punch");
        }

        // Anticipación del puñetazo
        yield return new WaitForSeconds(0.28f);

        if (punchSound != null)
        {
            AudioSource.PlayClipAtPoint(punchSound, transform.position, 1.0f);
        }

        // Conectar golpe si Fercho sigue en rango
        if (playerTransform != null)
        {
            float dist = Vector2.Distance(transform.position, playerTransform.position);
            if (dist <= punchRange + 0.45f)
            {
                if (playerHealth == null) playerHealth = playerTransform.GetComponent<Health>();
                if (playerHealth != null && !playerHealth.IsDead)
                {
                    Vector2 knockDir = (playerTransform.position - transform.position).normalized;
                    playerHealth.TakeDamage(punchDamage, knockDir, punchKnockback);
                    Debug.Log($"[SABES] ¡Golpe certero a Fercho! Daño: {punchDamage}");
                }
            }
        }

        // Recuperación post-golpe
        yield return new WaitForSeconds(0.35f);

        if (currentState != SabesState.Dead)
        {
            currentState = SabesState.Chasing;
        }
    }

    /// <summary>
    /// Mecánica única de Esquiva: se desliza hacia atrás esquivando el golpe de Fercho.
    /// </summary>
    public void TriggerDodge()
    {
        if (currentState == SabesState.Dead || isDodging) return;
        StartCoroutine(DodgeRoutine());
    }

    private IEnumerator DodgeRoutine()
    {
        isDodging = true;
        currentState = SabesState.Dodging;
        nextDodgeTime = Time.time + dodgeCooldown;

        FacePlayer();

        if (animator != null)
        {
            animator.SetBool("IsRunning", false);
            animator.SetTrigger("Dodge");
        }

        if (dodgeWhooshSound != null)
        {
            AudioSource.PlayClipAtPoint(dodgeWhooshSound, transform.position, 0.9f);
        }

        // Impulso hacia atrás (opuesto a Fercho)
        float backwardDir = (playerTransform != null && playerTransform.position.x < transform.position.x) ? 1f : -1f;
        rb.linearVelocity = new Vector2(backwardDir * dodgeSpeed, 0f);

        // Flash blanco/azul de agilidad
        if (spriteRenderer != null) StartCoroutine(FlashAgilityRoutine());

        Debug.Log("[SABES] ¡ESQUIVA TÁCTICA! El estudiante del SABES esquivó el ataque.");

        yield return new WaitForSeconds(dodgeDuration);

        rb.linearVelocity = Vector2.zero;
        isDodging = false;

        if (currentState != SabesState.Dead)
        {
            // Oportunidad de contraataque si Fercho está al alcance
            if (playerTransform != null && Vector2.Distance(transform.position, playerTransform.position) <= punchRange)
            {
                ExecutePunch();
            }
            else
            {
                currentState = SabesState.Chasing;
            }
        }
    }

    private IEnumerator FlashAgilityRoutine()
    {
        if (spriteRenderer == null) yield break;
        Color original = spriteRenderer.color;
        spriteRenderer.color = new Color(0.4f, 0.95f, 1f, 1.0f);  // cyan puro sin transparencia
        yield return new WaitForSeconds(0.15f);
        if (spriteRenderer != null) spriteRenderer.color = original;
    }

    private void HandleDamageReceived(int current, int max)
    {
        if (currentState == SabesState.Dead) return;

        int damageTaken = lastHealthRecorded - current;
        lastHealthRecorded = current;

        // Si estaba en plena esquiva: anula el daño (esquiva perfecta)
        if (isDodging && damageTaken > 0)
        {
            if (health != null)
            {
                health.Heal(damageTaken);
                lastHealthRecorded = health.CurrentHealth;
            }
            return;
        }

        // Si NO está en cooldown y salta la probabilidad: ¡Esquiva en lugar de recibir el combo!
        if (Time.time >= nextDodgeTime && Random.value <= dodgeChance && damageTaken > 0)
        {
            // Revertir el primer golpe y esquivar
            if (health != null)
            {
                health.Heal(damageTaken);
                lastHealthRecorded = health.CurrentHealth;
            }
            TriggerDodge();
            return;
        }

        // Si no esquivó: recibe el impacto normalmente
        currentState = SabesState.Stunned;
        if (animator != null) animator.SetTrigger("Hurt");

        CancelInvoke(nameof(ResumeChase));
        Invoke(nameof(ResumeChase), 0.35f);
    }

    private void ResumeChase()
    {
        if (currentState != SabesState.Dead && !isDodging)
        {
            currentState = SabesState.Chasing;
        }
    }

    private void HandleDeath()
    {
        currentState = SabesState.Dead;
        rb.linearVelocity = Vector2.zero;

        if (animator != null)
        {
            animator.SetBool("IsRunning", false);
            animator.SetTrigger("Die");
        }

        if (TryGetComponent<Collider2D>(out var col)) col.enabled = false;

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.RegisterEnemyDefeated();
        }

        Destroy(gameObject, 2.8f);
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.OnHealthChanged -= HandleDamageReceived;
            health.OnDeath -= HandleDeath;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, punchRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, stopDistance);
    }
}

