using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BossJefaRebsamenAI - IA de Valentina "La Jefa Fresa" del Instituto Rébsamen.
/// Mecánicas:
///   - Hit-and-Run: corre hacia Fercho, ataca, y se retira rápido.
///   - Contusión: Patada Giratoria aplica ApplyConcussion() a Fercho por 2s.
///   - Invocación: tras retirarse asume Defensa_Arriba e invoca 2 secuaces Rébsamen.
///   - Al 40% de vida: entra en fase enfurecida (más velocidad, más daño).
/// No modifica BossAI.cs, BossCutscene.cs ni ningún script del Nivel 1.
/// </summary>
public enum JefaState { Dormant, Intro, Idle, Chasing, Attacking, Retreating, Defending, Summoning, Enraged, Dead }

[RequireComponent(typeof(Rigidbody2D), typeof(Health), typeof(CapsuleCollider2D))]
public class BossJefaRebsamenAI : MonoBehaviour
{
    [Header("Parámetros de Valentina")]
    [SerializeField] private string bossName = "VALENTINA - LÍDER RÉBSAMEN";
    [SerializeField] private float moveSpeed = 5.2f;
    [SerializeField] private float enragedSpeedMult = 1.4f;
    [SerializeField] private float activationDistance = 12f;
    [SerializeField] private float stopDistance = 1.4f;

    [Header("Combate")]
    [SerializeField] private float attackRange = 1.8f;
    [SerializeField] private float attackCooldown = 1.8f;
    [SerializeField] private int punchDamage = 18;
    [SerializeField] private int kickDamage = 28;
    [SerializeField] private float normalKnockback = 4.5f;
    [SerializeField] private float kickKnockback = 6.5f;
    [SerializeField] private float concussionDuration = 2.0f;
    [SerializeField] private AudioClip punchSfx;
    [SerializeField] private AudioClip kickSfx;
    [SerializeField] private LayerMask playerLayer;

    [Header("Retirada")]
    [SerializeField] private float retreatDistance = 5.5f;
    [SerializeField] private float retreatSpeed = 7.0f;
    [SerializeField] private float defendDuration = 2.5f;

    [Header("Invocación de Secuaces")]
    [SerializeField] private GameObject minionPrefab;   // Enemy_Rebsamen.prefab
    [SerializeField] private int minionsPerSummon = 2;
    [SerializeField] private float summonDelay = 0.6f;

    [Header("Música de Jefa")]
    [SerializeField] private AudioClip bossMusic;
    [SerializeField] private AudioClip victoryMusic;
    [SerializeField] private AudioClip laughVoice;

    [Header("Límites de Calle")]
    [SerializeField] private float minY = -3.75f;
    [SerializeField] private float maxY = -0.6f;

    [Header("Ajuste de Cápsula")]
    [SerializeField] private Vector2 colliderOffset = new Vector2(0f, -0.1f);
    [SerializeField] private Vector2 colliderSize   = new Vector2(1.1f, 2.0f);

    // Componentes
    private Rigidbody2D rb;
    private Health health;
    private Animator animator;
    private AudioSource bgmSource;
    private CameraFollow cameraFollow;

    // Estado
    private JefaState currentState = JefaState.Dormant;
    private Transform playerTransform;
    private PlayerController playerController;
    private Health playerHealth;

    private float nextAttackTime = 0f;
    private Vector3 retreatTarget;
    private float defendTimer = 0f;
    private bool hasEntered40Phase = false;
    private Vector3 baseScale;
    private int attackComboCount = 0;   // Alterna Punch / Patada
    private bool summonRoutineActive = false;

    // -----------------------------------------------
    #region Unity Lifecycle
    private void Awake()
    {
        rb        = GetComponent<Rigidbody2D>();
        health    = GetComponent<Health>();
        animator  = GetComponent<Animator>();
        baseScale = transform.localScale;

        rb.gravityScale = 0f;
        rb.constraints  = RigidbodyConstraints2D.FreezeRotation;

        var col = GetComponent<CapsuleCollider2D>();
        if (col != null)
        {
            col.direction = CapsuleDirection2D.Vertical;
            col.offset    = colliderOffset;
            col.size      = colliderSize;
        }
    }

    private void Start()
    {
        // Buscar a Fercho
        PlayerController pc = FindFirstObjectByType<PlayerController>();
        if (pc != null)
        {
            playerTransform  = pc.transform;
            playerController = pc;
            playerHealth     = pc.GetComponent<Health>();
        }
        else
        {
            GameObject go = GameObject.FindGameObjectWithTag("Player");
            if (go != null)
            {
                playerTransform  = go.transform;
                playerController = go.GetComponent<PlayerController>();
                playerHealth     = go.GetComponent<Health>();
            }
        }

        cameraFollow = FindFirstObjectByType<CameraFollow>();

        // Suscribir eventos de salud
        health.OnHealthChanged += HandleHealthChanged;
        health.OnDeath         += HandleDeath;

        // Audio
        GameObject bgmObj = GameObject.Find("Musica_Nivel");
        if (bgmObj != null) bgmSource = bgmObj.GetComponent<AudioSource>();

        // Postura inicial
        SetAnimBool("IsRunning",   false);
        SetAnimBool("IsDefending", false);
    }

    private void Update()
    {
        if (currentState == JefaState.Dead || playerTransform == null) return;

        // Despertar
        if (currentState == JefaState.Dormant)
        {
            float dist = Vector2.Distance(transform.position, playerTransform.position);
            if (dist <= activationDistance)
            {
                StartCoroutine(AwakenRoutine());
            }
            return;
        }

        // Trigger de ataque cuando esta cerca en X e Y
        if (currentState == JefaState.Chasing || currentState == JefaState.Enraged)
        {
            FacePlayer();
            float distX = Mathf.Abs(transform.position.x - playerTransform.position.x);
            float distY = Mathf.Abs(transform.position.y - playerTransform.position.y);
            if (distX <= attackRange && distY <= 2.0f && Time.time >= nextAttackTime)
            {
                ChooseAttack();
            }
        }
    }

    private void FixedUpdate()
    {
        if (rb != null && rb.gravityScale != 0f) rb.gravityScale = 0f;

        if (currentState == JefaState.Dead   ||
            currentState == JefaState.Dormant ||
            currentState == JefaState.Intro   ||
            currentState == JefaState.Attacking)
        {
            rb.linearVelocity = Vector2.zero;
            ClampToStreet();
            return;
        }

        if (currentState == JefaState.Retreating)
        {
            // ── FIX: Mueve SOLO en X al retirarse; Y permanece fija en el suelo ──
            float targetX   = retreatTarget.x;
            float distX     = Mathf.Abs(transform.position.x - targetX);
            float dirX      = Mathf.Sign(targetX - transform.position.x);

            if (distX > 0.3f)
            {
                rb.linearVelocity = new Vector2(dirX * retreatSpeed, 0f);
                FacePlayer();
            }
            else
            {
                rb.linearVelocity = Vector2.zero;
                if (currentState == JefaState.Retreating && !summonRoutineActive)
                    StartCoroutine(DefendAndSummonRoutine());
            }
            ClampToStreet();
            return;
        }

        if (currentState == JefaState.Defending || currentState == JefaState.Summoning)
        {
            rb.linearVelocity = Vector2.zero;
            ClampToStreet();
            return;
        }

        if (currentState == JefaState.Chasing || currentState == JefaState.Enraged)
        {
            float speed = (currentState == JefaState.Enraged) ? moveSpeed * enragedSpeedMult : moveSpeed;
            float distX = Mathf.Abs(transform.position.x - playerTransform.position.x);
            float distY = Mathf.Abs(transform.position.y - playerTransform.position.y);

            if (distX > stopDistance || distY > 1.5f)
            {
                // ── FIX: Movimiento separado en X e Y para evitar que Y se desboque ──
                // X: perseguir al jugador a velocidad completa
                float vx = Mathf.Sign(playerTransform.position.x - transform.position.x) * speed;

                // Y: corrección suave (máx. 1.5 u/s) para alinearse con Fercho sin overshooting
                float yError = playerTransform.position.y - transform.position.y;
                float vy     = Mathf.Clamp(yError * 3.5f, -1.5f, 1.5f);

                rb.linearVelocity = new Vector2(vx, vy);
                FacePlayer();
                SetAnimBool("IsRunning",   true);
                SetAnimBool("IsDefending", false);
            }
            else
            {
                rb.linearVelocity = Vector2.zero;
                SetAnimBool("IsRunning", false);
                FacePlayer();

                if (Time.time >= nextAttackTime)
                    ChooseAttack();
            }
            ClampToStreet();
        }
    }
    #endregion

    // -----------------------------------------------
    #region Combat & Attacks

    private void ChooseAttack()
    {
        if (currentState == JefaState.Attacking) return;

        attackComboCount++;
        // Cada 2 golpes hace Patada Giratoria (con contusión)
        if (attackComboCount % 2 == 0)
            StartCoroutine(PatadaGiratoriaRoutine());
        else
            StartCoroutine(PunchRoutine());
    }

    private IEnumerator PunchRoutine()
    {
        currentState = JefaState.Attacking;
        nextAttackTime = Time.time + attackCooldown;
        rb.linearVelocity = Vector2.zero;
        FacePlayer();

        SetAnimBool("IsRunning",   false);
        SetAnimBool("IsDefending", false);
        animator?.SetTrigger("Punch");

        yield return new WaitForSeconds(0.18f);

        if (punchSfx != null) AudioSource.PlayClipAtPoint(punchSfx, transform.position, 0.9f);
        TryDamagePlayer(punchDamage, normalKnockback, applyConcussion: false);

        yield return new WaitForSeconds(0.32f);

        if (currentState != JefaState.Dead)
            StartRetreat();
    }

    private IEnumerator PatadaGiratoriaRoutine()
    {
        currentState = JefaState.Attacking;
        nextAttackTime = Time.time + attackCooldown;
        rb.linearVelocity = Vector2.zero;
        FacePlayer();

        SetAnimBool("IsRunning",   false);
        SetAnimBool("IsDefending", false);
        animator?.SetTrigger("Patada");

        // 4 frames a 10fps = 0.4s total; impacto en el frame 2 (0.2s)
        yield return new WaitForSeconds(0.22f);

        if (kickSfx  != null) AudioSource.PlayClipAtPoint(kickSfx,  transform.position, 1f);
        else if (punchSfx != null) AudioSource.PlayClipAtPoint(punchSfx, transform.position, 1f);

        TryDamagePlayer(kickDamage, kickKnockback, applyConcussion: true);

        yield return new WaitForSeconds(0.25f);

        if (currentState != JefaState.Dead)
            StartRetreat();
    }

    private void TryDamagePlayer(int damage, float knockback, bool applyConcussion)
    {
        if (playerTransform == null) return;

        float distX = Mathf.Abs(transform.position.x - playerTransform.position.x);
        float distY = Mathf.Abs(transform.position.y - playerTransform.position.y);

        if (distX > attackRange + 0.6f || distY > 2.5f) return;
        if (playerHealth == null || playerHealth.IsDead) return;

        Vector2 knockDir = (playerTransform.position - transform.position).normalized;
        playerHealth.TakeDamage(damage, knockDir, knockback);

        if (applyConcussion && playerController != null)
        {
            playerController.ApplyConcussion(concussionDuration);
            Debug.Log($"[BossJefaRebsamenAI] ¡CONTUSION aplicada a Fercho por {concussionDuration}s!");
        }
    }

    private void StartRetreat()
    {
        if (currentState == JefaState.Dead) return;
        currentState = JefaState.Retreating;
        summonRoutineActive = false;

        // ── FIX: retreatTarget mantiene la Y actual clampeada, solo se mueve en X ──
        float clampedY = Mathf.Clamp(transform.position.y, minY, maxY);
        float dirX     = transform.position.x - playerTransform.position.x;
        float retX     = transform.position.x + Mathf.Sign(dirX) * retreatDistance;

        // Limitar retX a limites razonables de pantalla
        retX = Mathf.Clamp(retX, -2f, 35f);
        retreatTarget = new Vector3(retX, clampedY, 0f);

        SetAnimBool("IsRunning",   true);
        SetAnimBool("IsDefending", false);

        Debug.Log($"[BossJefaRebsamenAI] Retirada hacia x={retX:F1}");
    }

    private IEnumerator DefendAndSummonRoutine()
    {
        if (currentState == JefaState.Dead) yield break;
        summonRoutineActive = true;

        currentState = JefaState.Defending;
        rb.linearVelocity = Vector2.zero;
        FacePlayer();

        SetAnimBool("IsRunning",   false);
        SetAnimBool("IsDefending", true);

        Debug.Log("[BossJefaRebsamenAI] Valentina asume postura de Defensa. ¡Invocando secuaces!");

        yield return new WaitForSeconds(0.6f);

        // Invocar secuaces
        currentState = JefaState.Summoning;
        yield return StartCoroutine(SummonMinionsRoutine());

        SetAnimBool("IsDefending", false);
        yield return new WaitForSeconds(0.35f);

        summonRoutineActive = false;

        if (currentState != JefaState.Dead)
        {
            currentState = (hasEntered40Phase) ? JefaState.Enraged : JefaState.Chasing;
        }
    }

    private IEnumerator SummonMinionsRoutine()
    {
        if (minionPrefab == null)
        {
            Debug.LogWarning("[BossJefaRebsamenAI] minionPrefab no asignado. Secuaces no invocados.");
            yield break;
        }

        for (int i = 0; i < minionsPerSummon; i++)
        {
            float offsetX = (i % 2 == 0) ? -3.5f : 3.5f;
            Vector3 spawnPos = new Vector3(
                transform.position.x + offsetX,
                Mathf.Clamp(transform.position.y + Random.Range(-0.3f, 0.3f), minY, maxY),
                0f
            );

            GameObject minion = Instantiate(minionPrefab, spawnPos, Quaternion.identity);
            Debug.Log($"[BossJefaRebsamenAI] Secuaz Rebsamen #{i + 1} invocado en {spawnPos}");
            yield return new WaitForSeconds(summonDelay);
        }
    }
    #endregion

    // -----------------------------------------------
    #region Awaken & Phase Transitions

    private IEnumerator AwakenRoutine()
    {
        currentState = JefaState.Intro;
        rb.linearVelocity = Vector2.zero;
        FacePlayer();

        // Bloquear camara
        cameraFollow?.SetLock(true);

        // Musica de jefa
        if (bossMusic != null && bgmSource != null)
        {
            bgmSource.clip = bossMusic;
            bgmSource.loop = true;
            bgmSource.Play();
        }

        // Activar barra de jefa en HUD
        BossHUD hud = FindFirstObjectByType<BossHUD>();
        if (hud != null) hud.ActivateBossBar(health, bossName);

        // Risa
        if (laughVoice != null) AudioSource.PlayClipAtPoint(laughVoice, transform.position, 1f);

        yield return new WaitForSeconds(0.3f);

        // Cutscene (dialogo)
        var cutscene = GetComponent<BossJefaCutscene>();
        if (cutscene != null) cutscene.TriggerBossCutscene();
        if (cutscene != null)
        {
            while (DialogueSystem.Instance != null && DialogueSystem.Instance.IsActive)
                yield return null;
        }

        currentState = JefaState.Chasing;
    }

    private void EnterEnragedPhase()
    {
        if (hasEntered40Phase) return;
        hasEntered40Phase = true;
        currentState      = JefaState.Enraged;
        concussionDuration = 2.5f;

        StartCoroutine(EnragedFlashRoutine());
        Debug.Log("[BossJefaRebsamenAI] ¡VALENTINA ENTRO EN MODO FRESA ENOJADA! (Fase 40%)");
    }

    private IEnumerator EnragedFlashRoutine()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr == null) yield break;

        Color original = sr.color;
        for (int i = 0; i < 6; i++)
        {
            sr.color = new Color(1f, 0.3f, 0.8f, 1f);
            yield return new WaitForSeconds(0.09f);
            sr.color = original;
            yield return new WaitForSeconds(0.09f);
        }
    }
    #endregion

    // -----------------------------------------------
    #region Health Events

    private void HandleHealthChanged(int current, int max)
    {
        if (currentState == JefaState.Dead) return;

        // Despertar si Fercho la golpea primero
        if (currentState == JefaState.Dormant)
        {
            StartCoroutine(AwakenRoutine());
            return;
        }

        // Hiperarmadura: no interrumpir ataque activo
        if (currentState != JefaState.Attacking)
        {
            animator?.SetTrigger("Hurt");
        }

        // Fase de furia al 40%
        float ratio = (float)current / max;
        if (ratio <= 0.4f && !hasEntered40Phase)
        {
            EnterEnragedPhase();
        }
    }

    private void HandleDeath()
    {
        currentState = JefaState.Dead;
        rb.linearVelocity = Vector2.zero;

        SetAnimBool("IsRunning",   false);
        SetAnimBool("IsDefending", false);
        animator?.SetTrigger("Die");

        if (TryGetComponent<Collider2D>(out var col)) col.enabled = false;

        if (ScoreManager.Instance != null) ScoreManager.Instance.RegisterBossDefeated();

        Debug.Log("[BossJefaRebsamenAI] ¡VALENTINA HA SIDO DERROTADA! Ampliando camara...");

        cameraFollow?.SetMaxX(36f);
        cameraFollow?.SetLock(false);

        BossHUD hud = FindFirstObjectByType<BossHUD>();
        if (hud != null) hud.TriggerVictorySequence(victoryMusic, bgmSource);

        Destroy(gameObject, 3.5f);
    }
    #endregion

    // -----------------------------------------------
    #region Helpers

    private void FacePlayer()
    {
        if (playerTransform == null) return;
        if (playerTransform.position.x < transform.position.x - 0.05f)
            transform.localScale = new Vector3(-Mathf.Abs(baseScale.x), baseScale.y, 1f);
        else if (playerTransform.position.x > transform.position.x + 0.05f)
            transform.localScale = new Vector3(Mathf.Abs(baseScale.x), baseScale.y, 1f);
    }

    /// <summary>
    /// Fija la posicion Y dentro de los limites de calle Y anula la componente Y
    /// de la velocidad cuando toca los limites para evitar "flotado".
    /// </summary>
    private void ClampToStreet()
    {
        float cy = Mathf.Clamp(transform.position.y, minY, maxY);
        if (!Mathf.Approximately(transform.position.y, cy))
        {
            transform.position = new Vector3(transform.position.x, cy, transform.position.z);
        }

        // Cancelar velocidad Y si presiona contra el limite
        if (rb != null)
        {
            float vy = rb.linearVelocity.y;
            bool  atBottom = cy <= minY && vy < 0f;
            bool  atTop    = cy >= maxY && vy > 0f;
            if (atBottom || atTop)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            }
        }
    }

    private void SetAnimBool(string param, bool value)
    {
        if (animator != null) animator.SetBool(param, value);
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.OnHealthChanged -= HandleHealthChanged;
            health.OnDeath         -= HandleDeath;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, activationDistance);
    }
    #endregion
}
