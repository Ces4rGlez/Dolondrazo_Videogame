using UnityEngine;

public enum EnemyState { Idle, Chasing, Attacking, Stunned, Dead }

[RequireComponent(typeof(Rigidbody2D), typeof(Health))]
public class EnemyAI : MonoBehaviour
{
    [Header("Parámetros de Movimiento")]
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private float stopDistance = 1.0f;

    [Header("Combate")]
    [SerializeField] private float attackRange = 1.2f;
    [SerializeField] private float attackCooldown = 1.8f;
    [SerializeField] private int attackDamage = 10;
    [SerializeField] private float knockbackPower = 3f;
    [SerializeField] private LayerMask playerLayer;

    private EnemyState currentState = EnemyState.Idle;
    private Transform playerTransform;
    private Rigidbody2D rb;
    private Health health;
    private Animator animator;
    private float nextAttackTime = 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<Health>();
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        // Buscar automáticamente a Fercho por su Tag "Player"
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            currentState = EnemyState.Chasing;
        }

        health.OnHealthChanged += HandleDamageFeedback;
        health.OnDeath += HandleDeath;
    }

    private void HandleDamageFeedback(int current, int max)
    {
        if (currentState == EnemyState.Dead) return;

        currentState = EnemyState.Stunned;
        if (animator != null) animator.SetTrigger("Hurt");

        // Pausa breve de aturdimiento tras recibir el golpe
        CancelInvoke(nameof(ResumeChase));
        Invoke(nameof(ResumeChase), 0.4f);
    }

    private void Update()
    {
        if (currentState == EnemyState.Dead || playerTransform == null) return;

        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);

        if (currentState == EnemyState.Chasing)
        {
            if (distanceToPlayer <= attackRange && Time.time >= nextAttackTime)
            {
                ExecuteAttack();
            }
        }
    }

    private void FixedUpdate()
    {
        if (currentState != EnemyState.Chasing || playerTransform == null)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        float distance = Vector2.Distance(transform.position, playerTransform.position);

        if (distance > stopDistance)
        {
            // Perseguir a Fercho en la calle (eje X e Y)
            Vector2 direction = (playerTransform.position - transform.position).normalized;
            rb.linearVelocity = direction * moveSpeed;

            // Voltear sprite según hacia dónde camina
            if (direction.x > 0.05f) transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, 1);
            else if (direction.x < -0.05f) transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, 1);
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    private void ExecuteAttack()
    {
        currentState = EnemyState.Attacking;
        nextAttackTime = Time.time + attackCooldown;

        if (animator != null) animator.SetTrigger("Attack");

        // Comprobar si golpea a Fercho
        Collider2D hit = Physics2D.OverlapCircle(transform.position, attackRange, playerLayer);
        if (hit != null && hit.TryGetComponent<Health>(out var playerHealth))
        {
            Vector2 knockbackDir = (hit.transform.position - transform.position).normalized;
            playerHealth.TakeDamage(attackDamage, knockbackDir, knockbackPower);
            Debug.Log("¡El enemigo ha golpeado a Fercho!");
        }

        Invoke(nameof(ResumeChase), 0.6f);
    }

    private void ResumeChase()
    {
        if (currentState != EnemyState.Dead)
        {
            currentState = EnemyState.Chasing;
        }
    }

    private void HandleDeath()
    {
        currentState = EnemyState.Dead;
        rb.linearVelocity = Vector2.zero;

        if (animator != null) animator.SetTrigger("Die");
        
        // Desactivar colisiones para no tropezar con el cuerpo
        if (TryGetComponent<Collider2D>(out var col)) col.enabled = false;

        if (ScoreManager.Instance != null) ScoreManager.Instance.RegisterEnemyDefeated();

        Debug.Log($"¡{gameObject.name} ha sido derrotado!");
        Destroy(gameObject, 1.2f);
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.OnHealthChanged -= HandleDamageFeedback;
            health.OnDeath -= HandleDeath;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}