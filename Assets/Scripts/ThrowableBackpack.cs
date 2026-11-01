using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// ThrowableBackpack - Objeto arrojable estilo beat-em-up clasico (Streets of Rage).
/// Fercho se para cerca, presiona E o J para recogerla, la transporta en sus manos,
/// y al presionar ataque la arroja en una parabola giratoria que causa dano masivo (35 HP).
/// </summary>
[RequireComponent(typeof(SpriteRenderer), typeof(CircleCollider2D))]
public class ThrowableBackpack : MonoBehaviour
{
    public enum BackpackState { Grounded, Carried, Flying }

    [Header("Combate y Vuelo")]
    [SerializeField] private float throwForceX = 9.0f;
    [SerializeField] private float throwForceY = 4.8f;
    [SerializeField] private float gravity = 16.0f;
    [SerializeField] private int throwDamage = 35;
    [SerializeField] private float knockbackForce = 6.5f;
    [SerializeField] private LayerMask enemyLayers;

    [Header("Offset al Cargar")]
    [SerializeField] private Vector3 carryOffset = new Vector3(0.35f, 0.35f, 0f);

    [Header("Sonidos")]
    [SerializeField] private AudioClip impactSound;
    [SerializeField] private AudioClip pickupSound;

    private BackpackState currentState = BackpackState.Grounded;
    private PlayerController player;
    private Transform playerTransform;
    private SpriteRenderer sr;
    private CircleCollider2D col;

    private Vector2 flightVelocity;
    private float groundY;
    private float throwCooldown = 0f;
    private bool playerInRange = false;

    // UI flotante
    private GameObject promptObj;
    private TextMesh promptText;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<CircleCollider2D>();
        col.isTrigger = true;

        // Capa por defecto de enemigos si no esta asignada (Layer 7 = Enemy)
        if (enemyLayers.value == 0)
        {
            enemyLayers = 1 << 7;
        }
    }

    private void Start()
    {
        groundY = transform.position.y;
        CreatePromptUI();
    }

    private void CreatePromptUI()
    {
        promptObj = new GameObject("Mochila_Prompt");
        promptObj.transform.SetParent(transform, false);
        promptObj.transform.localPosition = new Vector3(0f, 0.9f, 0f);

        promptText = promptObj.AddComponent<TextMesh>();
        promptText.text = "[E] MOCHILA";
        promptText.fontSize = 28;
        promptText.characterSize = 0.05f;
        promptText.anchor = TextAnchor.MiddleCenter;
        promptText.alignment = TextAlignment.Center;
        promptText.color = new Color(1f, 0.9f, 0.2f, 1f);

        promptObj.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (currentState != BackpackState.Grounded) return;

        if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null)
        {
            playerTransform = other.transform;
            player = other.GetComponent<PlayerController>();
            playerInRange = true;
            if (promptObj != null) promptObj.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (currentState != BackpackState.Grounded) return;

        if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null)
        {
            playerInRange = false;
            if (promptObj != null) promptObj.SetActive(false);
        }
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        var gamepad = Gamepad.current;

        // 1. Recoger mochila del suelo
        if (currentState == BackpackState.Grounded && playerInRange && player != null)
        {
            bool grabPressed = (keyboard != null && (keyboard.eKey.wasPressedThisFrame || keyboard.jKey.wasPressedThisFrame))
                            || (gamepad != null && gamepad.buttonWest.wasPressedThisFrame);

            if (grabPressed && !player.IsCarryingItem)
            {
                PickupBackpack();
            }
        }
        // 2. Transportando la mochila
        else if (currentState == BackpackState.Carried)
        {
            if (player == null || player.GetComponent<Health>().IsDead)
            {
                DropToGround();
                return;
            }

            // Alinear orientacion con Fercho
            float dirX = playerTransform.localScale.x > 0 ? 1f : -1f;
            transform.localPosition = new Vector3(carryOffset.x * dirX, carryOffset.y, 0f);
            transform.localRotation = Quaternion.identity;

            // Lanzar mochila (Cooldown breve para no arrojarla en el mismo instante de recogerla)
            if (Time.time >= throwCooldown)
            {
                bool throwPressed = (keyboard != null && (keyboard.eKey.wasPressedThisFrame || keyboard.jKey.wasPressedThisFrame || keyboard.kKey.wasPressedThisFrame))
                                 || (gamepad != null && (gamepad.buttonWest.wasPressedThisFrame || gamepad.buttonNorth.wasPressedThisFrame));

                if (throwPressed)
                {
                    ExecuteThrow(dirX);
                }
            }
        }
        // 3. Mochila en vuelo (Fisica parabolica)
        else if (currentState == BackpackState.Flying)
        {
            UpdateFlight();
        }
    }

    private void PickupBackpack()
    {
        currentState = BackpackState.Carried;
        player.IsCarryingItem = true;
        playerInRange = false;
        groundY = transform.position.y;

        if (promptObj != null) promptObj.SetActive(false);

        // Anclar a Fercho
        transform.SetParent(playerTransform, false);
        transform.localPosition = carryOffset;
        transform.localRotation = Quaternion.identity;

        throwCooldown = Time.time + 0.25f;

        if (pickupSound != null)
            AudioSource.PlayClipAtPoint(pickupSound, transform.position, 1.0f);

        Debug.Log("[ThrowableBackpack] Fercho recogio la mochila escolar.");
    }

    private void ExecuteThrow(float dirX)
    {
        currentState = BackpackState.Flying;
        if (player != null) player.IsCarryingItem = false;

        // Desanclar de Fercho
        transform.SetParent(null);
        flightVelocity = new Vector2(throwForceX * dirX, throwForceY);

        if (pickupSound != null)
            AudioSource.PlayClipAtPoint(pickupSound, transform.position, 1.0f);

        Debug.Log($"[ThrowableBackpack] ¡Fercho arrojo la mochila con fuerza! Dir: {dirX}");
    }

    private void UpdateFlight()
    {
        // Aplicar gravedad
        flightVelocity.y -= gravity * Time.deltaTime;
        transform.position += (Vector3)(flightVelocity * Time.deltaTime);

        // Giro en el aire
        float spinDir = flightVelocity.x > 0 ? -1f : 1f;
        transform.Rotate(0f, 0f, spinDir * 600f * Time.deltaTime);

        // Detectar colision con enemigos
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 0.65f, enemyLayers);
        foreach (var hit in hits)
        {
            // Ignorar al propio jugador
            if (hit.CompareTag("Player") || hit.GetComponent<PlayerController>() != null) continue;

            if (hit.TryGetComponent<Health>(out var enemyHealth))
            {
                if (!enemyHealth.IsDead)
                {
                    Vector2 knockbackDir = new Vector2(Mathf.Sign(flightVelocity.x), 0.35f).normalized;
                    enemyHealth.TakeDamage(throwDamage, knockbackDir, knockbackForce);

                    OnImpactHit(hit.transform.position);
                    return;
                }
            }
        }

        // Si aterriza en el suelo sin tocar a nadie
        if (transform.position.y <= groundY && flightVelocity.y < 0f)
        {
            SettleOnGround();
        }
    }

    private void OnImpactHit(Vector3 hitPos)
    {
        if (impactSound != null)
            AudioSource.PlayClipAtPoint(impactSound, transform.position, 1.0f);

        if (HitEffects.Instance != null)
        {
            HitEffects.Instance.SpawnHitParticles(hitPos, new Color(0.9f, 0.8f, 0.2f, 1f));
        }

        Debug.Log($"[ThrowableBackpack] ¡IMPACTO MASIVO DE MOCHILA! {throwDamage} de dano aplicado.");

        // Destruir con efecto de impacto
        Destroy(gameObject);
    }

    private void SettleOnGround()
    {
        currentState = BackpackState.Grounded;
        transform.position = new Vector3(transform.position.x, groundY, transform.position.z);
        transform.rotation = Quaternion.identity;
        flightVelocity = Vector2.zero;

        if (impactSound != null)
            AudioSource.PlayClipAtPoint(impactSound, transform.position, 0.6f);

        Debug.Log("[ThrowableBackpack] La mochila cayo al suelo.");
    }

    private void DropToGround()
    {
        currentState = BackpackState.Grounded;
        if (player != null) player.IsCarryingItem = false;
        transform.SetParent(null);
        transform.rotation = Quaternion.identity;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 1.2f);
    }
}
