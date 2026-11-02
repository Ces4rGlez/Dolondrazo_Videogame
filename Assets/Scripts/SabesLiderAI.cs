using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SabesLiderAI - Minijefe del Nivel 3: "El Prefecto / Líder del SABES".
/// Mecánicas:
///   - Tanque ágil: Alta resistencia, ataques devastadores y libre desplazamiento por toda la calle.
///   - Collider horizontal a los pies: Permite subir y bajar libremente por la avenida como Fercho.
///   - Combo pesado: Alterna Puñetazo Uppercut (daño medio-alto) y Patada Pesada (daño muy alto).
///   - Auto-curación al 60% de daño recibido (40% de vida restante): se recupera 70 HP una sola vez.
///   - Hiperarmadura durante ataques.
///   - Integración con BossHUD y diálogo cinemático previo al combate.
/// </summary>
public enum SabesLiderState { Dormant, Idle, Chasing, Attacking, Healing, Dead }

[RequireComponent(typeof(Rigidbody2D), typeof(Health), typeof(CapsuleCollider2D))]
public class SabesLiderAI : MonoBehaviour
{
    [Header("Parámetros del Líder")]
    [SerializeField] private string bossName = "EL PREFECTO - LÍDER SABES";
    [SerializeField] private float moveSpeed = 3.8f;
    [SerializeField] private float stopDistance = 1.3f;
    [SerializeField] private float activationDistance = 14f;

    [Header("Combate Pesado")]
    [SerializeField] private float attackRange = 1.8f;
    [SerializeField] private float attackCooldown = 1.3f;
    [SerializeField] private int punchDamage = 28;
    [SerializeField] private int kickDamage = 42;
    [SerializeField] private float punchKnockback = 5.5f;
    [SerializeField] private float kickKnockback = 7.5f;

    [Header("Audio y Música")]
    [SerializeField] private AudioClip punchSfx;
    [SerializeField] private AudioClip kickSfx;
    [SerializeField] private AudioClip healSfx;
    [SerializeField] private AudioClip bossMusic;
    [SerializeField] private AudioClip victoryMusic;

    [Header("Habilidad Especial: Auto-Curación")]
    [Tooltip("Umbral de vida restante para activar la curación (0.4 = tras perder el 60% de vida)")]
    [SerializeField] private float healHealthThreshold = 0.40f;
    [SerializeField] private int healAmount = 70;
    [SerializeField] private float healDuration = 1.2f;

    [Header("Collider a los Pies")]
    [SerializeField] private Vector2 colliderOffset = new Vector2(0f, -0.95f);
    [SerializeField] private Vector2 colliderSize = new Vector2(1.1f, 0.45f);

    // Componentes
    private Rigidbody2D rb;
    private Health health;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private AudioSource sfxSource;

    // Estado
    private SabesLiderState currentState = SabesLiderState.Dormant;
    private Transform playerTransform;
    private Health playerHealth;

    private float nextAttackTime = 0f;
    private bool hasHealed = false;
    private int comboStep = 0;
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

        CapsuleCollider2D col = GetComponent<CapsuleCollider2D>();
        if (col != null)
        {
            col.direction = CapsuleDirection2D.Horizontal;
            col.offset = colliderOffset;
            col.size = colliderSize;
        }

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
    }

    private void Start()
    {
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

        if (health != null)
        {
            health.OnHealthChanged += HandleHealthChanged;
            health.OnDeath += HandleDeath;
        }
    }

    private void Update()
    {
        if (currentState == SabesLiderState.Dead || currentState == SabesLiderState.Healing || playerTransform == null)
            return;

        float dist = Vector2.Distance(transform.position, playerTransform.position);

        if (currentState == SabesLiderState.Dormant)
        {
            if (dist <= activationDistance)
            {
                AwakenBoss();
            }
            return;
        }

        // Seguridad para no salir del mapa
        if (transform.position.y < -11.5f)
        {
            transform.position = new Vector3(transform.position.x, -10.0f, transform.position.z);
        }

        if (currentState == SabesLiderState.Chasing)
        {
            FacePlayer();

            float distX = Mathf.Abs(transform.position.x - playerTransform.position.x);
            float distY = Mathf.Abs(transform.position.y - playerTransform.position.y);

            if (distX <= attackRange && distY <= 1.8f && Time.time >= nextAttackTime)
            {
                StartCoroutine(PerformAttackRoutine());
            }
        }
    }

    private void FixedUpdate()
    {
        if (currentState == SabesLiderState.Chasing && playerTransform != null)
        {
            float dx = playerTransform.position.x - transform.position.x;
            float dy = playerTransform.position.y - transform.position.y;
            float absDx = Mathf.Abs(dx);
            float absDy = Mathf.Abs(dy);

            float vx = 0f;
            float vy = 0f;

            // Alineación en carril vertical (Y)
            if (absDy > 0.35f)
            {
                vy = Mathf.Sign(dy) * moveSpeed;
            }

            // Acercamiento horizontal (X)
            if (absDx > stopDistance)
            {
                vx = Mathf.Sign(dx) * moveSpeed;
            }

            Vector2 moveDir = new Vector2(vx, vy);
            if (moveDir.sqrMagnitude > 0.01f)
            {
                rb.linearVelocity = moveDir.normalized * moveSpeed;
                if (animator != null) animator.SetBool("IsRunning", true);
            }
            else
            {
                rb.linearVelocity = Vector2.zero;
                if (animator != null) animator.SetBool("IsRunning", false);
            }
        }
        else if (currentState != SabesLiderState.Attacking)
        {
            if (rb != null) rb.linearVelocity = Vector2.zero;
            if (animator != null) animator.SetBool("IsRunning", false);
        }
    }

    private void AwakenBoss()
    {
        currentState = SabesLiderState.Chasing;
        Debug.Log($"[SabesLiderAI] ¡{bossName} despierta y entra en combate!");

        BossHUD hud = FindFirstObjectByType<BossHUD>();
        if (hud != null && health != null)
        {
            hud.ActivateBossBar(health, bossName);
        }

        if (bossMusic != null)
        {
            GameObject bgmObj = GameObject.Find("Musica_Nivel");
            if (bgmObj != null && bgmObj.TryGetComponent<AudioSource>(out var bgm))
            {
                bgm.clip = bossMusic;
                bgm.Play();
            }
        }

        if (TryGetComponent<BossCutsceneSabesLider>(out var cutscene))
        {
            cutscene.TriggerCutscene();
        }
    }

    private void FacePlayer()
    {
        if (playerTransform == null) return;

        float diffX = playerTransform.position.x - transform.position.x;
        if (Mathf.Abs(diffX) > 0.05f)
        {
            float sign = diffX > 0 ? 1f : -1f;
            transform.localScale = new Vector3(Mathf.Abs(baseScale.x) * sign, baseScale.y, baseScale.z);
        }
    }

    private IEnumerator PerformAttackRoutine()
    {
        currentState = SabesLiderState.Attacking;
        if (rb != null) rb.linearVelocity = Vector2.zero;

        if (animator != null)
        {
            animator.SetBool("IsRunning", false);
        }

        FacePlayer();

        bool isKick = (comboStep % 2 == 1);
        comboStep++;

        string animTrigger = isKick ? "Kick" : "Punch";
        int currentDmg = isKick ? kickDamage : punchDamage;
        float currentKb = isKick ? kickKnockback : punchKnockback;
        AudioClip clipToPlay = isKick ? kickSfx : punchSfx;

        if (animator != null)
        {
            animator.SetTrigger(animTrigger);
        }

        if (clipToPlay != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(clipToPlay);
        }

        // Momento de impacto
        yield return new WaitForSeconds(0.32f);

        if (playerTransform != null && playerHealth != null && !playerHealth.IsDead)
        {
            float distX = Mathf.Abs(transform.position.x - playerTransform.position.x);
            float distY = Mathf.Abs(transform.position.y - playerTransform.position.y);

            if (distX <= attackRange + 0.4f && distY <= 1.8f)
            {
                Vector2 kbDir = (playerTransform.position - transform.position).normalized;
                playerHealth.TakeDamage(currentDmg, kbDir, currentKb);
                Debug.Log($"[SabesLiderAI] {bossName} golpea a Fercho con {animTrigger}! Daño: {currentDmg}");
            }
        }

        yield return new WaitForSeconds(0.45f);

        nextAttackTime = Time.time + attackCooldown;

        if (currentState != SabesLiderState.Dead)
        {
            currentState = SabesLiderState.Chasing;
        }
    }

    private void HandleHealthChanged(int current, int max)
    {
        if (currentState == SabesLiderState.Dead) return;

        if (currentState == SabesLiderState.Dormant)
        {
            AwakenBoss();
        }

        // Habilidad de Auto-Curación al llegar al umbral de 40% (perdió el 60%)
        float ratio = (float)current / Mathf.Max(1, max);
        if (!hasHealed && ratio <= healHealthThreshold && current > 0)
        {
            hasHealed = true;
            StartCoroutine(HealRoutine());
            return;
        }

        // Hiperarmadura durante ataques
        if (currentState != SabesLiderState.Attacking && currentState != SabesLiderState.Healing)
        {
            if (animator != null)
            {
                animator.SetTrigger("Hurt");
            }
        }
    }

    private IEnumerator HealRoutine()
    {
        currentState = SabesLiderState.Healing;
        if (rb != null) rb.linearVelocity = Vector2.zero;

        if (animator != null)
        {
            animator.SetBool("IsRunning", false);
        }

        Debug.Log($"[SabesLiderAI] ¡{bossName} USA SU HABILIDAD DE RECUPERACIÓN!");

        if (healSfx != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(healSfx);
        }

        // Parpadeo verde de curación
        Color originalColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
        int flashes = 6;
        float interval = healDuration / (flashes * 2);

        for (int i = 0; i < flashes; i++)
        {
            if (spriteRenderer != null) spriteRenderer.color = new Color(0.2f, 1f, 0.4f, 1f);
            yield return new WaitForSeconds(interval);
            if (spriteRenderer != null) spriteRenderer.color = originalColor;
            yield return new WaitForSeconds(interval);
        }

        if (health != null && !health.IsDead)
        {
            health.Heal(healAmount);
            Debug.Log($"[SabesLiderAI] {bossName} se ha recuperado {healAmount} HP! Vida actual: {health.CurrentHealth}/{health.MaxHealth}");
        }

        yield return new WaitForSeconds(0.2f);

        if (currentState != SabesLiderState.Dead)
        {
            currentState = SabesLiderState.Chasing;
        }
    }

    private void HandleDeath()
    {
        currentState = SabesLiderState.Dead;
        if (rb != null) rb.linearVelocity = Vector2.zero;

        if (animator != null)
        {
            animator.SetBool("IsRunning", false);
            animator.SetTrigger("Die");
        }

        if (TryGetComponent<Collider2D>(out var col))
        {
            col.enabled = false;
        }

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.RegisterEnemyDefeated();
        }

        Debug.Log($"[SabesLiderAI] ¡{bossName} HA SIDO DERROTADO!");

        if (victoryMusic != null)
        {
            GameObject bgmObj = GameObject.Find("Musica_Nivel");
            if (bgmObj != null && bgmObj.TryGetComponent<AudioSource>(out var bgm))
            {
                bgm.clip = victoryMusic;
                bgm.Play();
            }
        }

        // Ocultar barra de jefe en HUD
        GameObject bossBar = GameObject.Find("Boss_Health_HUD");
        if (bossBar != null)
        {
            bossBar.SetActive(false);
        }

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
}
