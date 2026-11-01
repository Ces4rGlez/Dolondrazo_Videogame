using System.Collections;
using UnityEngine;

/// <summary>
/// EnemyRebsamenAI - IA para el estudiante de la preparatoria Rebsamen.
/// Arquetipo: Agil y Defensivo ("Fresa").
/// A diferencia del CECyTE, se mueve mas rapido, ataca con patadas frontales de largo alcance,
/// y puede entrar en postura de guardia (bloqueo) donde mitiga dano y contraataca.
/// </summary>
public enum RebsamenState { Idle, Chasing, Defending, Attacking, Stunned, Dead }

[RequireComponent(typeof(Rigidbody2D), typeof(Health))]
public class EnemyRebsamenAI : MonoBehaviour
{
    [Header("Movimiento Agil")]
    [SerializeField] private float moveSpeed = 4.8f;
    [SerializeField] private float stopDistance = 1.25f;

    [Header("Combate Rebsamen")]
    [SerializeField] private float kickRange = 1.65f;
    [SerializeField] private float punchRange = 1.1f;
    [SerializeField] private float attackCooldown = 1.3f;
    [SerializeField] private int kickDamage = 14;
    [SerializeField] private int punchDamage = 8;
    [SerializeField] private float kickKnockback = 4.5f;
    [SerializeField] private float punchKnockback = 2.0f;
    [SerializeField] private LayerMask playerLayer;

    [Header("Mecanica de Guardia / Bloqueo")]
    [SerializeField] private float defenseDuration = 1.2f;
    [SerializeField] [Range(0f, 1f)] private float blockChanceOnHit = 0.45f;
    [SerializeField] private float defenseCooldown = 3.5f;
    [SerializeField] private AudioClip blockSound;

    private RebsamenState currentState = RebsamenState.Idle;
    private Transform playerTransform;
    private Health playerHealth;
    private Rigidbody2D rb;
    private Health health;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private float nextAttackTime = 0f;
    private float nextDefenseTime = 0f;
    private bool isDefending = false;
    private Coroutine defenseRoutine;
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
        // Localizar a Fercho
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            playerTransform = player.transform;
            playerHealth = player.GetComponent<Health>();
            currentState = RebsamenState.Chasing;
        }
        else
        {
            GameObject pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj != null)
            {
                playerTransform = pObj.transform;
                playerHealth = pObj.GetComponent<Health>();
                currentState = RebsamenState.Chasing;
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
        if (currentState == RebsamenState.Dead || playerTransform == null) return;

        float distance = Vector2.Distance(transform.position, playerTransform.position);

        // Si esta persiguiendo y esta en rango de ataque
        if (currentState == RebsamenState.Chasing)
        {
            if (distance <= kickRange && Time.time >= nextAttackTime)
            {
                ChooseAndExecuteAttack(distance);
            }
            // Ocasionalmente, si el jugador se acerca agresivamente, activar guardia tactica
            else if (distance <= 2.0f && Time.time >= nextDefenseTime && Random.value < 0.2f * Time.deltaTime)
            {
                StartDefenseMode();
            }
        }
    }

    private void FixedUpdate()
    {
        if (currentState != RebsamenState.Chasing || playerTransform == null)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        float distance = Vector2.Distance(transform.position, playerTransform.position);

        if (distance > stopDistance)
        {
            Vector2 dir = (playerTransform.position - transform.position).normalized;
            rb.linearVelocity = dir * moveSpeed;

            // Orientar sprite hacia el jugador
            if (dir.x > 0.05f) transform.localScale = new Vector3(Mathf.Abs(baseScale.x), baseScale.y, 1);
            else if (dir.x < -0.05f) transform.localScale = new Vector3(-Mathf.Abs(baseScale.x), baseScale.y, 1);
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    private void ChooseAndExecuteAttack(float distance)
    {
        currentState = RebsamenState.Attacking;
        nextAttackTime = Time.time + attackCooldown;
        rb.linearVelocity = Vector2.zero;

        // Orientar hacia el jugador antes de golpear
        FacePlayer();

        // 70% patada de largo alcance, 30% bofetada rapida
        if (distance > punchRange || Random.value < 0.7f)
        {
            StartCoroutine(KickRoutine());
        }
        else
        {
            StartCoroutine(PunchRoutine());
        }
    }

    private IEnumerator KickRoutine()
    {
        if (animator != null)
        {
            animator.SetTrigger("AttackKick");
            animator.SetTrigger("Attack"); // Fallback
        }

        // Momento del impacto de la patada (0.25s)
        yield return new WaitForSeconds(0.25f);

        HitPlayer(kickRange, kickDamage, kickKnockback, "Patada Rebsamen");

        yield return new WaitForSeconds(0.35f);

        if (currentState != RebsamenState.Dead)
        {
            currentState = RebsamenState.Chasing;
        }
    }

    private IEnumerator PunchRoutine()
    {
        if (animator != null)
        {
            animator.SetTrigger("AttackPunch");
        }

        yield return new WaitForSeconds(0.18f);

        HitPlayer(punchRange, punchDamage, punchKnockback, "Bofetada Rebsamen");

        yield return new WaitForSeconds(0.25f);

        if (currentState != RebsamenState.Dead)
        {
            currentState = RebsamenState.Chasing;
        }
    }

    private void HitPlayer(float range, int damage, float knockback, string logMsg)
    {
        if (playerTransform == null) return;

        float dist = Vector2.Distance(transform.position, playerTransform.position);
        if (dist <= range + 0.3f)
        {
            if (playerHealth == null) playerHealth = playerTransform.GetComponent<Health>();
            if (playerHealth != null && !playerHealth.IsDead)
            {
                Vector2 knockDir = (playerTransform.position - transform.position).normalized;
                playerHealth.TakeDamage(damage, knockDir, knockback);
                Debug.Log($"[Rebsamen] {logMsg} Dano: {damage}");
            }
        }
    }

    public void StartDefenseMode()
    {
        if (currentState == RebsamenState.Dead || isDefending) return;

        if (defenseRoutine != null) StopCoroutine(defenseRoutine);
        defenseRoutine = StartCoroutine(DefenseRoutine());
    }

    private IEnumerator DefenseRoutine()
    {
        isDefending = true;
        currentState = RebsamenState.Defending;
        nextDefenseTime = Time.time + defenseCooldown;
        rb.linearVelocity = Vector2.zero;

        FacePlayer();

        if (animator != null) animator.SetBool("IsDefending", true);

        yield return new WaitForSeconds(defenseDuration);

        EndDefenseMode();
    }

    private void EndDefenseMode()
    {
        isDefending = false;
        if (animator != null) animator.SetBool("IsDefending", false);

        if (currentState != RebsamenState.Dead)
        {
            currentState = RebsamenState.Chasing;
        }
    }

    private void HandleDamageReceived(int current, int max)
    {
        if (currentState == RebsamenState.Dead) return;

        int damageTaken = lastHealthRecorded - current;
        lastHealthRecorded = current;

        // Si estaba en guardia/bloqueo:
        if (isDefending && damageTaken > 0)
        {
            // Mitigacion: recupera el 75% del dano recibido
            int absorbed = Mathf.RoundToInt(damageTaken * 0.75f);
            if (absorbed > 0 && health != null)
            {
                health.Heal(absorbed);
                lastHealthRecorded = health.CurrentHealth;
            }

            if (blockSound != null) AudioSource.PlayClipAtPoint(blockSound, transform.position, 1.0f);

            // Flash visual azul/dorado de bloqueo
            if (spriteRenderer != null) StartCoroutine(FlashColorRoutine(new Color(0.4f, 0.8f, 1f, 1f)));

            // Contraataque sorpresivo: rompe la defensa con una patada si Fercho esta cerca
            if (playerTransform != null && Vector2.Distance(transform.position, playerTransform.position) <= kickRange)
            {
                EndDefenseMode();
                StartCoroutine(KickRoutine());
            }
            return;
        }

        // Si NO estaba defendiendo:
        // Probabilidad de entrar en defensa para el siguiente intercambio
        if (Time.time >= nextDefenseTime && Random.value <= blockChanceOnHit)
        {
            StartDefenseMode();
            return;
        }

        // Aturdimiento normal
        currentState = RebsamenState.Stunned;
        if (animator != null) animator.SetTrigger("Hurt");

        CancelInvoke(nameof(ResumeChase));
        Invoke(nameof(ResumeChase), 0.35f);
    }

    private IEnumerator FlashColorRoutine(Color col)
    {
        if (spriteRenderer == null) yield break;
        Color orig = spriteRenderer.color;
        spriteRenderer.color = col;
        yield return new WaitForSeconds(0.15f);
        if (spriteRenderer != null) spriteRenderer.color = orig;
    }

    private void ResumeChase()
    {
        if (currentState != RebsamenState.Dead && !isDefending)
        {
            currentState = RebsamenState.Chasing;
        }
    }

    private void FacePlayer()
    {
        if (playerTransform == null) return;
        float diffX = playerTransform.position.x - transform.position.x;
        if (diffX > 0.05f) transform.localScale = new Vector3(Mathf.Abs(baseScale.x), baseScale.y, 1);
        else if (diffX < -0.05f) transform.localScale = new Vector3(-Mathf.Abs(baseScale.x), baseScale.y, 1);
    }

    private void HandleDeath()
    {
        currentState = RebsamenState.Dead;
        isDefending = false;
        rb.linearVelocity = Vector2.zero;

        if (animator != null)
        {
            animator.SetBool("IsDefending", false);
            animator.SetTrigger("Die");
        }

        if (TryGetComponent<Collider2D>(out var col)) col.enabled = false;

        if (ScoreManager.Instance != null) ScoreManager.Instance.RegisterEnemyDefeated();

        Debug.Log($"Estudiante de la Rebsamen {gameObject.name} derrotado!");
        Destroy(gameObject, 1.4f);
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
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, kickRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, punchRange);
    }
}
