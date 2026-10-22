using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BossState { Sleeping, Intro, Chasing, Attacking, Stunned, Summoning, Dead }

[RequireComponent(typeof(Rigidbody2D), typeof(Health), typeof(CapsuleCollider2D))]
public class BossAI : MonoBehaviour
{
    [Header("Parámetros de Jefe")]
    [SerializeField] private string bossName = "ALUMNO DE TALLER";
    [SerializeField] private float moveSpeed = 2.6f;
    [SerializeField] private float stopDistance = 1.35f;
    [SerializeField] private float activationDistance = 10.0f;

    [Header("Combate Pesado")]
    [SerializeField] private float attackRange = 2.2f;
    [SerializeField] private float attackCooldown = 0.85f;
    [SerializeField] private int attackDamage = 25;
    [SerializeField] private float heavyKnockback = 6.0f;
    [SerializeField] private AudioClip punchSound;
    [SerializeField] private LayerMask playerLayer;

    [Header("Límites de Calle (Evitar Caídas)")]
    [SerializeField] private float minY = -3.75f;
    [SerializeField] private float maxY = -0.6f;

    [Header("Ajuste de Cápsula Automático")]
    [SerializeField] private Vector2 colliderOffset = new Vector2(0f, -0.15f);
    [SerializeField] private Vector2 colliderSize = new Vector2(1.25f, 2.1f);

    [Header("Refuerzos (Mecánica 50% HP)")]
    [SerializeField] private GameObject minionPrefab;
    [SerializeField] private Transform spawnLeft;
    [SerializeField] private Transform spawnRight;

    [Header("Música de Jefe")]
    [SerializeField] private AudioClip bossMusic;
    [SerializeField] private AudioClip victoryMusic;
    [SerializeField] private AudioClip laughVoice;

    private BossState currentState = BossState.Sleeping;
    private Transform playerTransform;
    private Health playerHealth;
    private Rigidbody2D rb;
    private Health health;
    private Animator animator;
    private AudioSource bgmSource;
    private CameraFollow cameraFollow;
    private float nextAttackTime = 0f;
    private bool hasSummoned = false;
    private Vector3 baseScale;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<Health>();
        animator = GetComponent<Animator>();
        baseScale = transform.localScale;

        // 1. Gravedad cero y rotación congelada
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        // 2. Calibración infalible del CapsuleCollider2D
        CapsuleCollider2D col = GetComponent<CapsuleCollider2D>();
        if (col != null)
        {
            col.direction = CapsuleDirection2D.Vertical;
            col.offset = colliderOffset;
            col.size = colliderSize;
        }
    }

    private void Start()
    {
        // Buscar a Fercho
        PlayerController player = FindFirstObjectByType<PlayerController>();
        if (player != null)
        {
            playerTransform = player.transform;
            playerHealth = player.GetComponent<Health>();
        }
        else
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
                playerHealth = playerObj.GetComponent<Health>();
            }
        }

        cameraFollow = FindFirstObjectByType<CameraFollow>();

        health.OnHealthChanged += HandleHealthChanged;
        health.OnDeath += HandleDeath;

        GameObject bgmObj = GameObject.Find("Musica_Nivel");
        if (bgmObj != null) bgmSource = bgmObj.GetComponent<AudioSource>();

        // Postura inicial: quieto esperando a Fercho
        if (animator != null)
        {
            animator.SetBool("IsWalking", false);
            animator.Play("Boss_Taunt");
        }
    }

    private void Update()
    {
        if (currentState == BossState.Dead || playerTransform == null) return;

        // Si esta esperando, despertar al instante al ver a Fercho
        if (currentState == BossState.Sleeping)
        {
            float dist = Vector2.Distance(transform.position, playerTransform.position);
            if (dist <= activationDistance)
            {
                StartCoroutine(AwakenBossRoutine());
            }
            return;
        }

        // Combate activo: ataque sin demoras
        if (currentState == BossState.Chasing)
        {
            FacePlayer();

            float distX = Mathf.Abs(transform.position.x - playerTransform.position.x);
            float distY = Mathf.Abs(transform.position.y - playerTransform.position.y);

            // DEBUG: Ver distancias en tiempo real
            // Debug removido - ataque funcionando correctamente

            // Umbral de Y mas generoso para beat-em-up (2.5 unidades)
            if (distX <= attackRange && distY <= 2.5f && Time.time >= nextAttackTime)
            {
                ExecuteHeavyAttack();
            }
        }
    }

    private void FacePlayer()
    {
        if (playerTransform == null) return;

        // Las animaciones originales miran naturalmente hacia el ESTE (derecha)
        // Por ende, para mirar a la izquierda (Oeste, hacia Fercho), la escala X debe ser NEGATIVA.
        // Para mirar a la derecha (Este), la escala X debe ser POSITIVA.
        if (playerTransform.position.x < transform.position.x - 0.05f)
        {
            transform.localScale = new Vector3(-Mathf.Abs(baseScale.x), baseScale.y, 1f); // Mirar a Fercho a la Izquierda
        }
        else if (playerTransform.position.x > transform.position.x + 0.05f)
        {
            transform.localScale = new Vector3(Mathf.Abs(baseScale.x), baseScale.y, 1f); // Mirar a Fercho a la Derecha
        }
    }

    private IEnumerator AwakenBossRoutine()
    {
        currentState = BossState.Intro;
        rb.linearVelocity = Vector2.zero;
        if (animator != null) animator.SetBool("IsWalking", false);

        FacePlayer();

        // Bloquear cámara en la arena
        if (cameraFollow != null) cameraFollow.SetLock(true);

        // Cambiar música a batalla
        if (bossMusic != null && bgmSource != null)
        {
            bgmSource.clip = bossMusic;
            bgmSource.loop = true;
            bgmSource.Play();
        }

        // Mostrar la barra de vida del jefe en pantalla
        BossHUD hud = FindFirstObjectByType<BossHUD>();
        if (hud != null) hud.ActivateBossBar(health, bossName);

        // Carcajada amenazante y reacción rápida (solo 0.35s)
        if (animator != null) animator.SetTrigger("Taunt");
        if (laughVoice != null) AudioSource.PlayClipAtPoint(laughVoice, transform.position, 1f);

        Debug.Log($"¡BATALLA DE JEFE INICIADA: {bossName}!!!");
        yield return new WaitForSeconds(0.35f);
        // Disparar cutscene del jefe
        BossCutscene bossCutscene = GetComponent<BossCutscene>();
        if (bossCutscene != null) bossCutscene.TriggerBossCutscene();
        // Esperar a que termine el dialogo antes de atacar
        if (bossCutscene != null) { while (DialogueSystem.Instance != null && DialogueSystem.Instance.IsActive) yield return null; }

        currentState = BossState.Chasing;
    }

    private void FixedUpdate()
    {
        if (rb != null && rb.gravityScale != 0f) rb.gravityScale = 0f;

        if (currentState != BossState.Chasing || playerTransform == null)
        {
            rb.linearVelocity = Vector2.zero;
            if (animator != null) animator.SetBool("IsWalking", false);
            ClampToStreet();
            return;
        }

        float distX = Mathf.Abs(transform.position.x - playerTransform.position.x);
        float distY = Mathf.Abs(transform.position.y - playerTransform.position.y);

        // Perseguir si está lejos en X o desalineado en Y
        if (distX > stopDistance || distY > 2.5f)
        {
            Vector2 dir = (playerTransform.position - transform.position).normalized;
            rb.linearVelocity = dir * moveSpeed;

            FacePlayer();
            if (animator != null) animator.SetBool("IsWalking", true);
        }
        else
        {
            rb.linearVelocity = Vector2.zero;
            if (animator != null) animator.SetBool("IsWalking", false);
            FacePlayer();

            if (Time.time >= nextAttackTime)
            {
                ExecuteHeavyAttack();
            }
        }

        ClampToStreet();
    }

    private void ClampToStreet()
    {
        float clampedY = Mathf.Clamp(transform.position.y, minY, maxY);
        if (transform.position.y != clampedY)
        {
            transform.position = new Vector3(transform.position.x, clampedY, transform.position.z);
        }
    }

    private void ExecuteHeavyAttack()
    {
        if (currentState == BossState.Attacking) return;
        StartCoroutine(HeavyPunchRoutine());
    }

    private IEnumerator HeavyPunchRoutine()
    {
        currentState = BossState.Attacking;
        rb.linearVelocity = Vector2.zero;
        nextAttackTime = Time.time + attackCooldown;

        FacePlayer();

        // Disparar animacion de golpe pesado
        if (animator != null)
        {
            animator.SetBool("IsWalking", false);
            animator.ResetTrigger("Hurt");
            int punchHash = Animator.StringToHash("Boss_Punch");
            if (animator.HasState(0, punchHash))
            {
                animator.Play(punchHash, 0, 0f);
                Debug.Log("[BossAI] Reproduciendo Boss_Punch por hash.");
            }
            else
            {
                animator.SetTrigger("Attack");
                Debug.LogWarning("[BossAI] Estado Boss_Punch NO encontrado. Usando trigger Attack.");
            }
        }

        // Esperar al fotograma del impacto (0.25s)
        yield return new WaitForSeconds(0.25f);

        // Sonido de golpe
        if (punchSound != null)
            AudioSource.PlayClipAtPoint(punchSound, transform.position, 1.0f);

        // Aplicar dano a Fercho si esta en rango
        if (playerTransform != null)
        {
            float distX = Mathf.Abs(transform.position.x - playerTransform.position.x);
            float distY = Mathf.Abs(transform.position.y - playerTransform.position.y);

            if (distX <= attackRange + 0.5f && distY <= 2.5f)
            {
                if (playerHealth == null) playerHealth = playerTransform.GetComponent<Health>();
                if (playerHealth != null && !playerHealth.IsDead)
                {
                    Vector2 knockback = (playerTransform.position - transform.position).normalized;
                    playerHealth.TakeDamage(attackDamage, knockback, heavyKnockback);
                    Debug.Log($"[BossAI] GOLPE PESADO a Fercho! Dano: {attackDamage}");
                }
            }
        }

        // Esperar a que termine la animacion (0.4s)
        yield return new WaitForSeconds(0.4f);

        if (currentState != BossState.Dead && currentState != BossState.Sleeping)
        {
            currentState = BossState.Chasing;
        }
    }

    private void HandleHealthChanged(int current, int max)
    {
        if (currentState == BossState.Dead) return;

        // Despertar de inmediato si Fercho lo golpea
        if (currentState == BossState.Sleeping)
        {
            StartCoroutine(AwakenBossRoutine());
        }

        // Refuerzos al 50%: Spawneo INSTANTÁNEO sin congelar al jefe
        if (!hasSummoned && current <= (max / 2))
        {
            hasSummoned = true;
            SpawnReinforcementsImmediately();
        }

        // Hiperarmadura de Jefe: Si está atacando, no interrumpir su puñetazo
        if (currentState != BossState.Attacking)
        {
            if (animator != null) animator.SetTrigger("Hurt");
        }
    }

    private void SpawnReinforcementsImmediately()
    {
        Debug.Log("¡EL ALUMNO DE TALLER LLAMA A SUS REFUERZOS!");
        if (animator != null) animator.SetTrigger("Taunt");

        if (minionPrefab != null)
        {
            Vector3 posL = spawnLeft != null ? spawnLeft.position : transform.position + new Vector3(-3.5f, 0f, 0f);
            Vector3 posR = spawnRight != null ? spawnRight.position : transform.position + new Vector3(3.5f, 0f, 0f);

            Instantiate(minionPrefab, posL, Quaternion.identity);
            Instantiate(minionPrefab, posR, Quaternion.identity);
        }
    }

    private void HandleDeath()
    {
        currentState = BossState.Dead;
        rb.linearVelocity = Vector2.zero;

        if (animator != null)
        {
            animator.SetBool("IsWalking", false);
            animator.SetTrigger("Die");
        }
        if (TryGetComponent<Collider2D>(out var col)) col.enabled = false;

        if (ScoreManager.Instance != null) ScoreManager.Instance.RegisterBossDefeated();

        Debug.Log("¡EL JEFE DE TALLER HA SIDO DERROTADO! Verificando que Fercho derrote a todos los secuaces...");

        // Delegar la verificación de victoria a BossHUD (que vive en HUD_Manager y nunca se destruye)
        BossHUD hud = FindFirstObjectByType<BossHUD>();
        if (hud != null)
        {
            hud.TriggerVictorySequence(victoryMusic, bgmSource);
        }

        // Destruir el cuerpo del jefe después de 3.5 segundos
        Destroy(gameObject, 3.5f);
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.OnHealthChanged -= HandleHealthChanged;
            health.OnDeath -= HandleDeath;
        }
    }

    public string BossName => bossName;
}


