using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// DirectorMondragonAI - Boss Final del Nivel 3: "Lic. Mondragón - Director del CBTis 75".
/// Mecánicas solicitadas:
///   1. Al perder el 25% de vida (75% restante): Invoca un clon de sí mismo.
///      El original se queda en postura defensiva/idle sin atacar.
///      Cuando Fercho derrota al clon, el original vuelve a la acción.
///   2. Al perder el 90% de vida (10% restante): Invoca a varios secuaces del SABES.
///      Mientras Fercho combate a los secuaces, el Director NO ataca.
///      Vuelve a atacar únicamente cuando todos los secuaces hayan sido derrotados.
///   3. Al perder el 99% de vida (al borde de caer): Se restaura automáticamente al 40% de vida
///      para una fase final de desesperación, antes de caer definitivamente.
/// </summary>
public enum DirectorState { Dormant, Chasing, Attacking, WaitingForClone, WaitingForMinions, Restoring, Dead }

[RequireComponent(typeof(Rigidbody2D), typeof(Health), typeof(CapsuleCollider2D))]
public class DirectorMondragonAI : MonoBehaviour
{
    [Header("Identificación")]
    [SerializeField] private string bossName = "LIC. MONDRAGÓN - DIRECTOR DEL CBTIS 75";
    [SerializeField] private bool isClone = false;

    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 3.2f;
    [SerializeField] private float stopDistance = 1.4f;
    [SerializeField] private float activationDistance = 14f;

    [Header("Combate")]
    [SerializeField] private float attackRange = 1.8f;
    [SerializeField] private float attackCooldown = 1.6f;
    [SerializeField] private int punchDamage = 25;
    [SerializeField] private float knockbackForce = 5.5f;
    [SerializeField] private AudioClip punchSfx;

    [Header("Fase 1: Clonación (25% de daño / 75% HP)")]
    [SerializeField] private GameObject clonePrefab;
    [SerializeField] private float clonePhaseThreshold = 0.75f;

    [Header("Fase 2: Invocación de Secuaces (90% de daño / 10% HP)")]
    [SerializeField] private GameObject minionPrefab;
    [SerializeField] private int minionCount = 4;
    [SerializeField] private float minionPhaseThreshold = 0.10f;

    [Header("Fase 3: Restauración Final (99% de daño / 1% HP)")]
    [SerializeField] private float restoreToPercent = 0.40f;

    [Header("Límites de Calle")]
    [SerializeField] private float minY = -10.5f;
    [SerializeField] private float maxY = -5.0f;

    [Header("Collider")]
    [SerializeField] private Vector2 colliderOffset = new Vector2(0f, -0.1f);
    [SerializeField] private Vector2 colliderSize = new Vector2(1.1f, 2.0f);

    [Header("Audio y Música")]
    [SerializeField] private AudioClip bossMusic;
    [SerializeField] private AudioClip victoryMusic;

    // Componentes
    private Rigidbody2D rb;
    private Health health;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private AudioSource sfxSource;
    private AudioSource bgmSource;

    // Estado
    private DirectorState currentState = DirectorState.Dormant;
    private Transform playerTransform;
    private Health playerHealth;

    private float nextAttackTime = 0f;
    private Vector3 baseScale;

    // Flags de fases
    private bool hasTriggeredClone = false;
    private bool hasTriggeredMinions = false;
    private bool hasRestoredLife = false;

    // Control de entidades invocadas
    private GameObject activeClone = null;
    private List<GameObject> activeMinions = new List<GameObject>();

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
            col.direction = CapsuleDirection2D.Vertical;
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

        GameObject bgmObj = GameObject.Find("Musica_Nivel");
        if (bgmObj != null) bgmSource = bgmObj.GetComponent<AudioSource>();

        DetectStreetLimits();

        // Si es un clon, se inicia activo de inmediato
        if (isClone)
        {
            currentState = DirectorState.Chasing;
            if (spriteRenderer != null)
            {
                // Tinte espectral / azulado para diferenciar la copia
                spriteRenderer.color = new Color(0.7f, 0.85f, 1f, 0.85f);
            }
        }
    }

    private void Update()
    {
        if (currentState == DirectorState.Dead || currentState == DirectorState.Restoring || playerTransform == null)
            return;

        float dist = Vector2.Distance(transform.position, playerTransform.position);

        if (currentState == DirectorState.Dormant)
        {
            if (dist <= activationDistance)
            {
                AwakenDirector();
            }
            return;
        }

        ClampToStreet();

        // FASE 1: Esperando que el clon sea derrotado
        if (currentState == DirectorState.WaitingForClone)
        {
            if (rb != null) rb.linearVelocity = Vector2.zero;
            FacePlayer();

            bool cloneDead = (activeClone == null || !activeClone.activeInHierarchy);
            if (!cloneDead && activeClone.TryGetComponent<Health>(out var cloneH))
            {
                if (cloneH.IsDead) cloneDead = true;
            }

            if (cloneDead)
            {
                Debug.Log("[DirectorMondragonAI] ¡El clon ha sido destruido! El Director original retoma el combate.");
                if (animator != null) animator.SetBool("IsDefending", false);
                currentState = DirectorState.Chasing;
            }
            return;
        }

        // FASE 2: Esperando que los secuaces sean derrotados
        if (currentState == DirectorState.WaitingForMinions)
        {
            if (rb != null) rb.linearVelocity = Vector2.zero;
            FacePlayer();

            activeMinions.RemoveAll(m => m == null || !m.activeInHierarchy || (m.TryGetComponent<Health>(out var mh) && mh.IsDead));

            if (activeMinions.Count == 0)
            {
                Debug.Log("[DirectorMondragonAI] ¡Todos los secuaces eliminados! El Director vuelve al ataque.");
                if (animator != null) animator.SetBool("IsDefending", false);
                currentState = DirectorState.Chasing;
            }
            return;
        }

        // Combate activo (Chasing)
        if (currentState == DirectorState.Chasing)
        {
            FacePlayer();

            float distX = Mathf.Abs(transform.position.x - playerTransform.position.x);
            float distY = Mathf.Abs(transform.position.y - playerTransform.position.y);

            if (distX <= attackRange && distY <= 1.2f && Time.time >= nextAttackTime)
            {
                StartCoroutine(PerformPunchRoutine());
            }
            else
            {
                MoveTowardPlayer();
            }
        }
    }

    private void AwakenDirector()
    {
        currentState = DirectorState.Chasing;
        Debug.Log($"[DirectorMondragonAI] ¡{bossName} entra en batalla!");

        if (!isClone)
        {
            BossHUD hud = FindFirstObjectByType<BossHUD>();
            if (hud != null && health != null)
            {
                hud.ActivateBossBar(health, bossName);
            }

            if (bossMusic != null && bgmSource != null)
            {
                bgmSource.clip = bossMusic;
                bgmSource.Play();
            }
        }
    }

    private void MoveTowardPlayer()
    {
        if (playerTransform == null || rb == null) return;

        Vector2 dir = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
        rb.linearVelocity = dir * moveSpeed;

        if (animator != null)
        {
            animator.SetBool("IsWalking", true);
            animator.SetBool("IsDefending", false);
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

    private IEnumerator PerformPunchRoutine()
    {
        currentState = DirectorState.Attacking;
        if (rb != null) rb.linearVelocity = Vector2.zero;

        if (animator != null)
        {
            animator.SetBool("IsWalking", false);
            animator.SetTrigger("Punch");
        }

        if (punchSfx != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(punchSfx);
        }

        // Sincronizar fotograma de impacto
        yield return new WaitForSeconds(0.35f);

        if (playerTransform != null && playerHealth != null && !playerHealth.IsDead)
        {
            float distX = Mathf.Abs(transform.position.x - playerTransform.position.x);
            float distY = Mathf.Abs(transform.position.y - playerTransform.position.y);

            if (distX <= attackRange + 0.4f && distY <= 1.4f)
            {
                Vector2 kbDir = (playerTransform.position - transform.position).normalized;
                playerHealth.TakeDamage(punchDamage, kbDir, knockbackForce);
                Debug.Log($"[DirectorMondragonAI] El Director conecta golpe con el maletín: {punchDamage} daño.");
            }
        }

        yield return new WaitForSeconds(0.45f);

        nextAttackTime = Time.time + attackCooldown;

        if (currentState != DirectorState.Dead && currentState != DirectorState.WaitingForClone && currentState != DirectorState.WaitingForMinions)
        {
            currentState = DirectorState.Chasing;
        }
    }

    private void HandleHealthChanged(int current, int max)
    {
        if (currentState == DirectorState.Dead) return;

        if (currentState == DirectorState.Dormant)
        {
            AwakenDirector();
        }

        float ratio = (float)current / Mathf.Max(1, max);

        // SOLO EL JEFE ORIGINAL EJECUTA LAS FASES ESPECIALES
        if (!isClone)
        {
            // MECÁNICA 3: Restauración al 99% de daño (al llegar a 1% o menos)
            // Se ejecuta de inmediato para evitar que Die() sea llamado
            if (!hasRestoredLife && current <= Mathf.CeilToInt(max * 0.02f))
            {
                hasRestoredLife = true;
                int restoreHp = Mathf.RoundToInt(max * restoreToPercent);
                health.Heal(restoreHp);
                StartCoroutine(LifeRestoreRoutine());
                return;
            }

            // MECÁNICA 2: Invocación de Secuaces al 90% de daño (10% HP restante)
            if (!hasTriggeredMinions && ratio <= minionPhaseThreshold && current > 0)
            {
                hasTriggeredMinions = true;
                StartCoroutine(SummonMinionsPhaseRoutine());
                return;
            }

            // MECÁNICA 1: Clonación al 25% de daño (75% HP restante)
            if (!hasTriggeredClone && ratio <= clonePhaseThreshold && current > 0)
            {
                hasTriggeredClone = true;
                StartCoroutine(SpawnClonePhaseRoutine());
                return;
            }
        }

        // Hurt regular
        if (currentState != DirectorState.Attacking && currentState != DirectorState.WaitingForClone &&
            currentState != DirectorState.WaitingForMinions && currentState != DirectorState.Restoring)
        {
            if (animator != null)
            {
                animator.SetTrigger("Hurt");
            }
        }
    }

    private IEnumerator SpawnClonePhaseRoutine()
    {
        currentState = DirectorState.WaitingForClone;
        if (rb != null) rb.linearVelocity = Vector2.zero;

        if (animator != null)
        {
            animator.SetBool("IsWalking", false);
            animator.SetBool("IsDefending", true);
        }

        Debug.Log("[DirectorMondragonAI] ¡FASE CLON! El Director se duplica a sí mismo.");

        yield return new WaitForSeconds(0.4f);

        // Posición para el clon (del otro lado de Fercho o un desplazamiento)
        Vector3 clonePos = transform.position + new Vector3(transform.localScale.x > 0 ? 3.0f : -3.0f, 0f, 0f);

        GameObject prefabToSpawn = clonePrefab != null ? clonePrefab : gameObject;
        activeClone = Instantiate(prefabToSpawn, clonePos, Quaternion.identity);

        // Configurar el clon
        if (activeClone.TryGetComponent<DirectorMondragonAI>(out var cloneAI))
        {
            cloneAI.isClone = true;
        }

        // Si tiene componente Health, darle un porcentaje razonable de vida
        if (activeClone.TryGetComponent<Health>(out var cloneHealth))
        {
            // El clon tiene 80 HP
            cloneHealth.Heal(80);
        }

        Debug.Log("[DirectorMondragonAI] Clon listo en combate. El Director original permanece en defensa.");
    }

    private IEnumerator SummonMinionsPhaseRoutine()
    {
        currentState = DirectorState.WaitingForMinions;
        if (rb != null) rb.linearVelocity = Vector2.zero;

        if (animator != null)
        {
            animator.SetBool("IsWalking", false);
            animator.SetBool("IsDefending", true);
        }

        Debug.Log("[DirectorMondragonAI] ¡FASE SECUACES! El Director convoca a la guardia del SABES.");

        yield return new WaitForSeconds(0.5f);

        if (minionPrefab != null)
        {
            activeMinions.Clear();
            for (int i = 0; i < minionCount; i++)
            {
                float spawnX = transform.position.x + Random.Range(-4.5f, 4.5f);
                float spawnY = Mathf.Lerp(minY, maxY, Random.value);
                Vector3 spawnPos = new Vector3(spawnX, spawnY, 0f);

                GameObject minion = Instantiate(minionPrefab, spawnPos, Quaternion.identity);
                activeMinions.Add(minion);
                yield return new WaitForSeconds(0.35f);
            }
        }

        Debug.Log($"[DirectorMondragonAI] {activeMinions.Count} secuaces en el campo. El Director aguarda sin atacar.");
    }

    private IEnumerator LifeRestoreRoutine()
    {
        currentState = DirectorState.Restoring;
        if (rb != null) rb.linearVelocity = Vector2.zero;

        if (animator != null)
        {
            animator.SetBool("IsWalking", false);
            animator.SetBool("IsDefending", false);
            animator.SetTrigger("Hurt");
        }

        Debug.Log("[DirectorMondragonAI] ¡RESTAURACIÓN FINAL! El Director Mondragón se niega a caer y recupera el 40% de vida.");

        // Destello dorado de furia
        Color orig = spriteRenderer != null ? spriteRenderer.color : Color.white;
        for (int i = 0; i < 8; i++)
        {
            if (spriteRenderer != null) spriteRenderer.color = new Color(1f, 0.85f, 0.1f, 1f);
            yield return new WaitForSeconds(0.1f);
            if (spriteRenderer != null) spriteRenderer.color = orig;
            yield return new WaitForSeconds(0.1f);
        }

        yield return new WaitForSeconds(0.3f);

        currentState = DirectorState.Chasing;
        Debug.Log("[DirectorMondragonAI] ¡El Director Mondragón vuelve con furia total!");
    }

    private void HandleDeath()
    {
        currentState = DirectorState.Dead;
        if (rb != null) rb.linearVelocity = Vector2.zero;

        if (animator != null)
        {
            animator.SetBool("IsWalking", false);
            animator.SetBool("IsDefending", false);
            animator.SetTrigger("Die");
        }

        if (TryGetComponent<Collider2D>(out var col))
        {
            col.enabled = false;
        }

        if (!isClone)
        {
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.RegisterBossDefeated();
            }

            Debug.Log("[DirectorMondragonAI] ¡EL DIRECTOR MONDRAGÓN HA SIDO DERROTADO DEFINITIVAMENTE!");

            BossHUD hud = FindFirstObjectByType<BossHUD>();
            if (hud != null)
            {
                hud.TriggerVictorySequence(victoryMusic, bgmSource);
            }
        }
        else
        {
            Debug.Log("[DirectorMondragonAI] La copia del Director fue derrotada.");
        }

        Destroy(gameObject, 3.5f);
    }

    private void DetectStreetLimits()
    {
        GameObject limInf = GameObject.Find("Limite_Inferior");
        GameObject limSup = GameObject.Find("Limite_Superior");

        if (limInf != null && limInf.TryGetComponent<Collider2D>(out var colInf))
        {
            minY = colInf.bounds.max.y + 0.15f;
        }

        if (limSup != null && limSup.TryGetComponent<Collider2D>(out var colSup))
        {
            maxY = colSup.bounds.min.y - 0.15f;
        }
    }

    private void ClampToStreet()
    {
        float cy = Mathf.Clamp(transform.position.y, minY, maxY);
        if (!Mathf.Approximately(transform.position.y, cy))
        {
            transform.position = new Vector3(transform.position.x, cy, transform.position.z);
        }

        if (rb != null)
        {
            float vy = rb.linearVelocity.y;
            bool atBottom = cy <= minY && vy < 0f;
            bool atTop = cy >= maxY && vy > 0f;
            if (atBottom || atTop)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            }
        }
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.OnHealthChanged -= HandleHealthChanged;
            health.OnDeath -= HandleDeath;
        }
    }

    // Setters para inicialización dinámica si es necesario
    public void SetClonePrefab(GameObject prefab) => clonePrefab = prefab;
    public void SetMinionPrefab(GameObject prefab) => minionPrefab = prefab;
}
