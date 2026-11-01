using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float walkSpeed = 4f;
    [SerializeField] private float runSpeed = 7f;        // Velocidad al correr (Shift)
    [SerializeField] private float runSpeedThreshold = 5f; // Speed value para animacion de correr

    [Header("Salto (Beat-em-up)")]
    [SerializeField] private float jumpHeight = 1.8f;    // Cuanto sube visualmente en Y
    [SerializeField] private float jumpDuration = 0.45f; // Duracion del salto completo

    [Header("Sonidos")]
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private AudioClip jumpSound;

    [Header("Combate")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private LayerMask enemyLayers;
    [SerializeField] private int punchDamage = 15;
    [SerializeField] private int kickDamage = 25;

    private Rigidbody2D rb;
    private Animator animator;
    private Health health;
    private Vector2 moveInput;
    private bool isFacingRight = true;
    private bool isDead = false;
    private bool isJumping = false;
    private float groundY;
    public bool IsCarryingItem { get; set; } = false;         // Posicion Y base del personaje
    private Coroutine jumpCoroutine;

    [Header("Estado de Contusion")]
    private bool isConcussed = false;
    private float concussionTimer = 0f;
    private GameObject activeStarsEffect;
    public bool IsConcussed => isConcussed;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        health = GetComponent<Health>();
    }

    private void Start()
    {
        groundY = transform.position.y; // Guardar Y inicial como "suelo"

        if (health != null)
        {
            health.OnHealthChanged += HandleDamageFeedback;
            health.OnDeath += HandlePlayerDeath;
        }
    }

    private void HandlePlayerDeath()
    {
        isDead = true;
        if (isConcussed) EndConcussion();
        rb.linearVelocity = Vector2.zero;

        // Cancelar salto si estaba en el aire
        if (jumpCoroutine != null) StopCoroutine(jumpCoroutine);
        isJumping = false;

        if (animator != null)
        {
            animator.SetBool("IsJumping", false);
            animator.SetBool("IsRunning", false);
            animator.SetFloat("Speed", 0f);
            animator.SetTrigger("Die");
        }
    }

    private void HandleDamageFeedback(int current, int max)
    {
        if (animator != null && current > 0 && !isJumping)
        {
            animator.SetTrigger("Hurt");
        }
    }

    private void Update()
    {
        if (isDead) return;

        if (isConcussed)
        {
            UpdateConcussion();
            return;
        }

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // --- 1. Lectura de movimiento ---
        float moveX = 0f;
        float moveY = 0f;

        if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)    moveY += 1f;
        if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)  moveY -= 1f;
        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)  moveX -= 1f;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) moveX += 1f;

        // Gamepad
        var gamepad = Gamepad.current;
        if (gamepad != null)
        {
            Vector2 stick = gamepad.leftStick.ReadValue();
            if (stick.sqrMagnitude > 0.1f) { moveX = stick.x; moveY = stick.y; }
        }

        // --- 2. Correr (mantener Shift) ---
        bool isRunHeld = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed
                      || (gamepad != null && gamepad.leftShoulder.isPressed);
        float currentSpeed = (isRunHeld && moveInput.sqrMagnitude > 0.1f) ? runSpeed : walkSpeed;

        moveInput = new Vector2(moveX, moveY).normalized;

        // --- 3. Animator: Speed, IsRunning ---
        if (animator != null)
        {
            float animSpeed = moveInput.sqrMagnitude * currentSpeed;
            animator.SetFloat("Speed", animSpeed);
            animator.SetBool("IsRunning", isRunHeld && moveInput.sqrMagnitude > 0.1f);
        }

        // --- 4. Voltear sprite ---
        if (moveX > 0.05f && !isFacingRight) Flip();
        else if (moveX < -0.05f && isFacingRight) Flip();

        // --- 5. Salto (Espacio) ---
        bool jumpPressed = keyboard.spaceKey.wasPressedThisFrame
                        || (gamepad != null && gamepad.buttonSouth.wasPressedThisFrame);

        if (jumpPressed && !isJumping)
        {
            jumpCoroutine = StartCoroutine(JumpRoutine());
        }

        // --- 6. Ataques (J = Punetazo, K = Patada) ---
        if (IsCarryingItem) return;
        bool punchPressed = keyboard.jKey.wasPressedThisFrame || (gamepad != null && gamepad.buttonWest.wasPressedThisFrame);
        bool kickPressed  = keyboard.kKey.wasPressedThisFrame || (gamepad != null && gamepad.buttonNorth.wasPressedThisFrame);

        if (punchPressed && !isJumping) ExecutePunch();
        else if (kickPressed && !isJumping) ExecuteKick();
    }

    private void FixedUpdate()
    {
        if (isDead || isConcussed)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        // Durante el salto, solo mover en X (Y lo maneja la corrutina)
        if (isJumping)
        {
            rb.linearVelocity = new Vector2(moveInput.x * walkSpeed, rb.linearVelocity.y);
        }
        else
        {
            float currentSpeed = (Keyboard.current != null &&
                (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed))
                ? runSpeed : walkSpeed;
            rb.linearVelocity = moveInput * currentSpeed;
        }
    }

    /// <summary>
    /// Salto estilo beat-em-up: el personaje sube y baja en Y visualmente
    /// sin usar fisicas (perfecto para perspectiva isometrica/callejera).
    /// </summary>
    private IEnumerator JumpRoutine()
    {
        isJumping = true;
        groundY = transform.position.y;

        if (animator != null) animator.SetBool("IsJumping", true);
        if (jumpSound != null) AudioSource.PlayClipAtPoint(jumpSound, transform.position, 1f);

        float halfDuration = jumpDuration / 2f;

        // Subida
        float elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            float newY = groundY + Mathf.Sin(t * Mathf.PI * 0.5f) * jumpHeight;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
            yield return null;
        }

        // Bajada
        elapsed = 0f;
        while (elapsed < halfDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / halfDuration;
            float newY = (groundY + jumpHeight) - Mathf.Sin(t * Mathf.PI * 0.5f) * jumpHeight;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
            yield return null;
        }

        // Aterrizar
        transform.position = new Vector3(transform.position.x, groundY, transform.position.z);
        isJumping = false;

        if (animator != null) animator.SetBool("IsJumping", false);

        jumpCoroutine = null;
    }

    private void Flip()
    {
        isFacingRight = !isFacingRight;
        Vector3 scaler = transform.localScale;
        scaler.x *= -1;
        transform.localScale = scaler;
    }

    private void ExecutePunch()
    {
        if (animator != null) animator.SetTrigger("Punch");
        DetectHit(punchDamage);
    }

    private void ExecuteKick()
    {
        if (animator != null) animator.SetTrigger("Kick");
        DetectHit(kickDamage);
    }

    private void DetectHit(int damage)
    {
        if (attackPoint == null) return;

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(attackPoint.position, attackRange, enemyLayers);
        foreach (Collider2D enemy in hitEnemies)
        {
            // Verificar si es una caja rompible
            if (enemy.TryGetComponent<BreakableCrate>(out var crate))
            {
                crate.ReceiveHit();
                if (hitSound != null) AudioSource.PlayClipAtPoint(hitSound, transform.position, 1.0f);
                continue;
            }

            // Enemigo normal
            if (enemy.TryGetComponent<Health>(out var enemyHealth))
            {
                Vector2 knockbackDir = isFacingRight
                    ? new Vector2(1f, 0.2f).normalized
                    : new Vector2(-1f, 0.2f).normalized;
                enemyHealth.TakeDamage(damage, knockbackDir, 4.5f);
                if (hitSound != null) AudioSource.PlayClipAtPoint(hitSound, transform.position, 1.0f);
            }
            Debug.Log($"Golpeaste a {enemy.name} haciendo {damage} de dano!");
        }
    }

    /// <summary>
    /// Aplica contusion (aturdimiento) a Fercho por una duracion determinada.
    /// Fercho no puede moverse ni atacar, pero machacar botones acelera la recuperacion.
    /// </summary>
    public void ApplyConcussion(float duration = 2.0f)
    {
        if (isDead) return;
        isConcussed = true;
        concussionTimer = Mathf.Max(concussionTimer, duration);
        rb.linearVelocity = Vector2.zero;
        if (animator != null)
        {
            animator.SetFloat("Speed", 0f);
            animator.SetBool("IsRunning", false);
            animator.SetTrigger("Hurt");
        }
        CreateOrShowConcussionStars();
        Debug.Log($"[PlayerController] Fercho aturdido por {duration}s! Machaca botones para recuperarte.");
    }

    private void UpdateConcussion()
    {
        concussionTimer -= Time.deltaTime;

        if (activeStarsEffect != null)
        {
            float wobble = Mathf.Sin(Time.time * 10f) * 12f;
            float pulse = 1f + Mathf.PingPong(Time.time * 2.5f, 0.2f);
            activeStarsEffect.transform.localRotation = Quaternion.Euler(0f, 0f, wobble);
            activeStarsEffect.transform.localScale = new Vector3(pulse, pulse, 1f);
        }

        var keyboard = Keyboard.current;
        var gamepad = Gamepad.current;
        bool mashed = false;

        if (keyboard != null)
        {
            if (keyboard.wKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame ||
                keyboard.sKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame ||
                keyboard.upArrowKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame ||
                keyboard.leftArrowKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame ||
                keyboard.spaceKey.wasPressedThisFrame || keyboard.jKey.wasPressedThisFrame ||
                keyboard.kKey.wasPressedThisFrame || keyboard.eKey.wasPressedThisFrame)
            {
                mashed = true;
            }
        }

        if (gamepad != null)
        {
            if (gamepad.buttonSouth.wasPressedThisFrame || gamepad.buttonWest.wasPressedThisFrame ||
                gamepad.buttonNorth.wasPressedThisFrame || gamepad.buttonEast.wasPressedThisFrame)
            {
                mashed = true;
            }
        }

        if (mashed)
        {
            concussionTimer -= 0.35f;
            if (activeStarsEffect != null)
            {
                activeStarsEffect.transform.localScale = Vector3.one * 1.35f;
            }
        }

        if (concussionTimer <= 0f)
        {
            EndConcussion();
        }
    }

    private void CreateOrShowConcussionStars()
    {
        if (activeStarsEffect == null)
        {
            activeStarsEffect = new GameObject("Concussion_Stars");
            activeStarsEffect.transform.SetParent(transform, false);
            activeStarsEffect.transform.localPosition = new Vector3(0f, 1.75f, 0f);

            TextMesh tm = activeStarsEffect.AddComponent<TextMesh>();
            tm.text = "★ ★ ★";
            tm.fontSize = 24;
            tm.characterSize = 0.12f;
            tm.alignment = TextAlignment.Center;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.color = new Color(1f, 0.85f, 0.1f, 1f);

            MeshRenderer mr = activeStarsEffect.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.sortingLayerName = "Default";
                mr.sortingOrder = 50;
            }
        }
        activeStarsEffect.SetActive(true);
    }

    private void EndConcussion()
    {
        isConcussed = false;
        concussionTimer = 0f;
        if (activeStarsEffect != null)
        {
            activeStarsEffect.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.OnHealthChanged -= HandleDamageFeedback;
            health.OnDeath -= HandlePlayerDeath;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }
}

