using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// EquippableWeapon - Sistema de armas cuerpo a cuerpo equipables (Beat-em-up).
/// Diseñado para la Regla T del Nivel 3 (CBTis 75).
/// Fercho la recoge del suelo con [E] o [J], se equipa en sus manos,
/// aumenta el alcance de ataque, inflige dano extra contundente,
/// tiene durabilidad limitada (8 golpes) y se rompe al agotarse.
/// </summary>
[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
public class EquippableWeapon : MonoBehaviour
{
    public enum WeaponState { Grounded, Equipped, Broken }

    [Header("Configuracion del Arma")]
    [SerializeField] private string weaponName = "Regla T";
    [SerializeField] private int maxDurability = 8;
    [SerializeField] private int bonusDamage = 20;
    [SerializeField] private float weaponRange = 2.2f;
    [SerializeField] private float knockbackForce = 6.0f;
    [SerializeField] private LayerMask enemyLayers;

    [Header("Offset al Portar")]
    [SerializeField] private Vector3 equipOffset = new Vector3(0.45f, 0.15f, -0.05f);
    [SerializeField] private Vector3 restRotation = new Vector3(0f, 0f, -20f);

    [Header("Sonidos")]
    [SerializeField] private AudioClip pickupSound;
    [SerializeField] private AudioClip swingSound;
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private AudioClip breakSound;

    private WeaponState currentState = WeaponState.Grounded;
    private int currentDurability;
    private PlayerController player;
    private Transform playerTransform;
    private SpriteRenderer sr;
    private Collider2D col;

    private bool playerInRange = false;
    private bool isSwinging = false;
    private float nextAttackTime = 0f;
    private float attackCooldown = 0.35f;

    // UI flotante
    private GameObject promptObj;
    private TextMesh promptText;

    public WeaponState CurrentState => currentState;
    public int CurrentDurability => currentDurability;
    public int BonusDamage => bonusDamage;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        col.isTrigger = true;
        currentDurability = maxDurability;

        if (enemyLayers.value == 0)
        {
            enemyLayers = (1 << 7) | (1 << 8); // Enemy and Breakable layers
        }
    }

    private void Start()
    {
        CreatePromptUI();
    }

    private void CreatePromptUI()
    {
        promptObj = new GameObject("Weapon_Prompt");
        promptObj.transform.SetParent(transform, false);
        promptObj.transform.localPosition = new Vector3(0f, 0.85f, 0f);

        promptText = promptObj.AddComponent<TextMesh>();
        promptText.text = $"[E] {weaponName.ToUpper()}";
        promptText.fontSize = 28;
        promptText.characterSize = 0.05f;
        promptText.anchor = TextAnchor.MiddleCenter;
        promptText.alignment = TextAlignment.Center;
        promptText.color = new Color(1f, 0.88f, 0.2f, 1f);

        promptObj.SetActive(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (currentState != WeaponState.Grounded) return;

        if (other.CompareTag("Player") || other.GetComponent<PlayerController>() != null)
        {
            playerTransform = other.transform;
            player = other.GetComponent<PlayerController>();
            playerInRange = true;
            if (promptObj != null)
            {
                promptText.text = $"[E] EQUIPAR {weaponName.ToUpper()}";
                promptObj.SetActive(true);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (currentState != WeaponState.Grounded) return;

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

        // 1. Recoger arma del suelo
        if (currentState == WeaponState.Grounded && playerInRange && player != null)
        {
            bool equipPressed = (keyboard != null && (keyboard.eKey.wasPressedThisFrame || keyboard.jKey.wasPressedThisFrame))
                             || (gamepad != null && gamepad.buttonWest.wasPressedThisFrame);

            if (equipPressed && !player.IsCarryingItem)
            {
                EquipWeapon();
            }
        }
        // 2. Arma equipada en manos de Fercho
        else if (currentState == WeaponState.Equipped)
        {
            if (player == null || player.GetComponent<Health>().IsDead)
            {
                DropWeapon();
                return;
            }

            // Alinear posicion y escala con la orientacion de Fercho
            float dirX = playerTransform.localScale.x > 0 ? 1f : -1f;
            transform.localPosition = new Vector3(equipOffset.x * dirX, equipOffset.y, equipOffset.z);

            // Escala local
            Vector3 s = transform.localScale;
            s.x = Mathf.Abs(s.x) * dirX;
            transform.localScale = s;

            // Orden de dibujo frente a Fercho
            if (sr != null)
            {
                SpriteRenderer playerSr = playerTransform.GetComponent<SpriteRenderer>();
                if (playerSr != null)
                {
                    sr.sortingLayerID = playerSr.sortingLayerID;
                    sr.sortingOrder = playerSr.sortingOrder + 1;
                }
            }

            // Escuchar ataque de Fercho (J = golpe, K = patada)
            bool attackPressed = (keyboard != null && (keyboard.jKey.wasPressedThisFrame || keyboard.kKey.wasPressedThisFrame))
                              || (gamepad != null && (gamepad.buttonWest.wasPressedThisFrame || gamepad.buttonNorth.wasPressedThisFrame));

            if (attackPressed && !isSwinging && Time.time >= nextAttackTime)
            {
                ExecuteSwing(dirX);
            }
        }
    }

    private void EquipWeapon()
    {
        currentState = WeaponState.Equipped;
        playerInRange = false;
        if (col != null) col.enabled = false;

        transform.SetParent(playerTransform, false);
        transform.localPosition = equipOffset;
        transform.localRotation = Quaternion.Euler(restRotation);
        transform.localScale = new Vector3(1.15f, 1.15f, 1f);

        if (promptText != null)
        {
            promptText.text = $"REGLA T [{currentDurability}/{maxDurability}]";
            promptText.fontSize = 24;
            promptText.color = new Color(0.2f, 1f, 0.4f, 1f);
            promptObj.transform.localPosition = new Vector3(0f, 1.4f, 0f);
            promptObj.SetActive(true);
        }

        if (pickupSound != null)
        {
            AudioSource.PlayClipAtPoint(pickupSound, transform.position, 1.0f);
        }

        Debug.Log($"[EquippableWeapon] !Fercho equipo la {weaponName}! Durabilidad: {currentDurability}");
    }

    private void ExecuteSwing(float dirX)
    {
        nextAttackTime = Time.time + attackCooldown;
        StartCoroutine(SwingRoutine(dirX));
    }

    private IEnumerator SwingRoutine(float dirX)
    {
        isSwinging = true;

        if (swingSound != null)
        {
            AudioSource.PlayClipAtPoint(swingSound, transform.position, 1.0f);
        }

        // Animacion de barrido en arco (+50 a -60 grados)
        float startAngle = restRotation.z + 55f;
        float endAngle = restRotation.z - 65f;
        float elapsed = 0f;
        float duration = 0.22f;

        bool hitRegistered = false;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float currentAngle = Mathf.Lerp(startAngle, endAngle, t);
            transform.localRotation = Quaternion.Euler(0f, 0f, currentAngle * dirX);

            if (!hitRegistered && t >= 0.4f)
            {
                hitRegistered = true;
                DetectWeaponHits(dirX);
            }

            yield return null;
        }

        // Retornar a postura de descanso
        transform.localRotation = Quaternion.Euler(restRotation);
        isSwinging = false;
    }

    private void DetectWeaponHits(float dirX)
    {
        Vector2 attackOrigin = (Vector2)transform.position + new Vector2(dirX * 0.8f, 0f);
        Collider2D[] hits = Physics2D.OverlapCircleAll(attackOrigin, weaponRange, enemyLayers);

        bool landedAnyHit = false;

        foreach (var c in hits)
        {
            if (c.gameObject == gameObject || c.transform == playerTransform) continue;

            // Caja rompible
            if (c.TryGetComponent<BreakableCrate>(out var crate))
            {
                crate.ReceiveHit();
                landedAnyHit = true;
                continue;
            }

            // Enemigo con Health
            if (c.TryGetComponent<Health>(out var enemyHealth) && !enemyHealth.IsDead)
            {
                Vector2 knockDir = new Vector2(dirX, 0.25f).normalized;
                enemyHealth.TakeDamage(bonusDamage, knockDir, knockbackForce);
                landedAnyHit = true;
                Debug.Log($"[EquippableWeapon] !Golpe contundente con {weaponName} a {c.name}! Dano: {bonusDamage}");
            }
        }

        if (landedAnyHit)
        {
            if (hitSound != null) AudioSource.PlayClipAtPoint(hitSound, transform.position, 1.0f);

            currentDurability--;
            if (promptText != null)
            {
                promptText.text = $"REGLA T [{currentDurability}/{maxDurability}]";
            }

            if (currentDurability <= 0)
            {
                BreakWeapon();
            }
        }
    }

    private void BreakWeapon()
    {
        currentState = WeaponState.Broken;
        Debug.Log($"[EquippableWeapon] !La {weaponName} se partio en dos tras el impacto!");

        if (breakSound != null)
        {
            AudioSource.PlayClipAtPoint(breakSound, transform.position, 1.2f);
        }

        if (promptObj != null)
        {
            promptText.text = "!REGLA T ROTA!";
            promptText.color = Color.red;
            promptObj.transform.SetParent(null, true);
            Destroy(promptObj, 1.2f);
        }

        Destroy(gameObject);
    }

    private void DropWeapon()
    {
        currentState = WeaponState.Grounded;
        transform.SetParent(null, true);
        if (col != null) col.enabled = true;
        if (promptObj != null) promptObj.SetActive(false);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, weaponRange);
    }
}
