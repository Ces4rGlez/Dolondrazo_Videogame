using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// BusHazard - Autobus Zafiro DH-0087 del Nivel 3 (CBTis 75).
/// Cumple dos funciones clave en el gameplay:
/// 1. Peligro ambiental periodico: Cruza a toda velocidad la avenida,
///    atropellando a los rivales y despejando la calle.
/// 2. Transporte final: Al terminar la ultima oleada, se estaciona frente
///    a la entrada del CBTis 75 para recoger a Fercho y avanzar en la historia.
/// </summary>
public class BusHazard : MonoBehaviour
{
    public enum BusMode { Inactive, Warning, Speeding, Arriving, Parked, Departing }

    [Header("Configuracion de Velocidad")]
    [SerializeField] private float speedCross = 22.0f;
    [SerializeField] private float speedArrive = 6.5f;
    [SerializeField] private float speedDepart = 12.0f;

    [Header("Combate / Atropello")]
    [SerializeField] private int hitDamage = 50;
    [SerializeField] private float hitKnockback = 12.0f;
    [SerializeField] private LayerMask targetLayers;

    [Header("Sonidos")]
    [SerializeField] private AudioClip hornSound;
    [SerializeField] private AudioClip engineSound;
    [SerializeField] private AudioClip brakesSound;
    [SerializeField] private AudioClip doorSound;

    [Header("Limites de Recorrido")]
    [SerializeField] private float leftSpawnX = -32.0f;
    [SerializeField] private float rightDespawnX = 35.0f;
    [SerializeField] private float defaultLaneY = -7.6f;  // Configurable desde Inspector

    private BusMode currentMode = BusMode.Inactive;
    private Rigidbody2D rb;
    private Collider2D col;
    private SpriteRenderer sr;
    private AudioSource audioSource;

    private Vector2 targetParkPos;
    private bool playerCanBoard = false;

    // UI de Alerta y Abordaje
    private GameObject warningBannerObj;
    private Text warningText;
    private GameObject boardPromptObj;
    private Text boardPromptText;

    public BusMode CurrentMode => currentMode;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();

        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        if (col != null) col.isTrigger = true;

        if (targetLayers.value == 0)
        {
            targetLayers = (1 << 6) | (1 << 7); // Player (6) and Enemy (7)
        }
    }

    private void Start()
    {
        SetupUI();
        transform.position = new Vector3(leftSpawnX, defaultLaneY, 0f);
        currentMode = BusMode.Inactive;
    }

    private void SetupUI()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        Texture2D tex = new Texture2D(4, 4);
        Color[] px = new Color[16];
        for (int i = 0; i < px.Length; i++) px[i] = Color.white;
        tex.SetPixels(px); tex.Apply();
        Sprite white = Sprite.Create(tex, new Rect(0,0,4,4), new Vector2(0.5f,0.5f));

        // 1. Banner de Alerta de Atropello
        warningBannerObj = new GameObject("Bus_WarningBanner", typeof(RectTransform), typeof(Image));
        warningBannerObj.transform.SetParent(canvas.transform, false);
        RectTransform rt = warningBannerObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.78f);
        rt.anchorMax = new Vector2(0.5f, 0.78f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(580f, 60f);

        Image img = warningBannerObj.GetComponent<Image>();
        img.sprite = white;
        img.color = new Color(0.85f, 0.15f, 0.15f, 0.90f);

        GameObject wt = new GameObject("WarningText", typeof(RectTransform), typeof(Text));
        wt.transform.SetParent(warningBannerObj.transform, false);
        RectTransform rtt = wt.GetComponent<RectTransform>();
        rtt.anchorMin = Vector2.zero;
        rtt.anchorMax = Vector2.one;
        rtt.sizeDelta = Vector2.zero;
        warningText = wt.GetComponent<Text>();
        warningText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        warningText.fontSize = 22;
        warningText.fontStyle = FontStyle.Bold;
        warningText.alignment = TextAnchor.MiddleCenter;
        warningText.color = Color.white;
        warningText.text = "!CUIDADO! !AUTOBUS APROXIMANDOSE!";

        warningBannerObj.SetActive(false);

        // 2. Prompt de abordaje
        boardPromptObj = new GameObject("Bus_BoardPrompt", typeof(RectTransform), typeof(Image));
        boardPromptObj.transform.SetParent(canvas.transform, false);
        RectTransform rtb = boardPromptObj.GetComponent<RectTransform>();
        rtb.anchorMin = new Vector2(0.5f, 0.25f);
        rtb.anchorMax = new Vector2(0.5f, 0.25f);
        rtb.pivot = new Vector2(0.5f, 0.5f);
        rtb.anchoredPosition = Vector2.zero;
        rtb.sizeDelta = new Vector2(480f, 55f);

        Image imgb = boardPromptObj.GetComponent<Image>();
        imgb.sprite = white;
        imgb.color = new Color(0.1f, 0.65f, 0.25f, 0.92f);

        GameObject bt = new GameObject("BoardText", typeof(RectTransform), typeof(Text));
        bt.transform.SetParent(boardPromptObj.transform, false);
        RectTransform rtbText = bt.GetComponent<RectTransform>();
        rtbText.anchorMin = Vector2.zero;
        rtbText.anchorMax = Vector2.one;
        rtbText.sizeDelta = Vector2.zero;
        boardPromptText = bt.GetComponent<Text>();
        boardPromptText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        boardPromptText.fontSize = 20;
        boardPromptText.fontStyle = FontStyle.Bold;
        boardPromptText.alignment = TextAnchor.MiddleCenter;
        boardPromptText.color = Color.white;
        boardPromptText.text = ">> [E] SUBIR AL AUTOBUS (CONTINUAR) <<";

        boardPromptObj.SetActive(false);
    }

    /// <summary>
    /// Dispara el cruce a toda velocidad del autobus atropellando enemigos.
    /// </summary>
    public void TriggerPassingHazard(float laneY = 0f)
    {
        if (currentMode != BusMode.Inactive) return;
        if (laneY == 0f) laneY = defaultLaneY;  // usa el valor del Inspector si no se especifica
        gameObject.SetActive(true);
        StartCoroutine(PassingHazardRoutine(laneY));
    }

    private IEnumerator PassingHazardRoutine(float laneY)
    {
        currentMode = BusMode.Warning;
        transform.position = new Vector3(leftSpawnX, laneY, 0f);

        if (warningBannerObj != null) warningBannerObj.SetActive(true);
        if (hornSound != null && Camera.main != null) AudioSource.PlayClipAtPoint(hornSound, Camera.main.transform.position, 1.0f);

        yield return new WaitForSeconds(1.5f);
        if (warningBannerObj != null) warningBannerObj.SetActive(false);

        currentMode = BusMode.Speeding;
        while (transform.position.x < rightDespawnX)
        {
            transform.position += Vector3.right * (speedCross * Time.deltaTime);
            yield return null;
        }

        transform.position = new Vector3(leftSpawnX, laneY, 0f);
        currentMode = BusMode.Inactive;
    }

    /// <summary>
    /// Secuencia final de victoria: El autobus llega, se estaciona frente al CBTis 75
    /// y abre sus puertas para recoger a Fercho.
    /// </summary>
    public void TriggerArrivalSequence(Vector2 parkPosition)
    {
        gameObject.SetActive(true);
        targetParkPos = parkPosition;
        StartCoroutine(ArrivalRoutine());
    }

    private IEnumerator ArrivalRoutine()
    {
        currentMode = BusMode.Arriving;
        transform.position = new Vector3(targetParkPos.x - 18f, targetParkPos.y, 0f);

        if (hornSound != null) AudioSource.PlayClipAtPoint(hornSound, transform.position, 0.9f);

        while (transform.position.x < targetParkPos.x)
        {
            float dist = targetParkPos.x - transform.position.x;
            float currentSpeed = Mathf.Max(2.0f, Mathf.Min(speedArrive, dist * 1.5f));
            transform.position += Vector3.right * (currentSpeed * Time.deltaTime);
            yield return null;
        }

        transform.position = new Vector3(targetParkPos.x, targetParkPos.y, 0f);

        if (brakesSound != null) AudioSource.PlayClipAtPoint(brakesSound, transform.position, 1.0f);
        yield return new WaitForSeconds(0.4f);
        if (doorSound != null) AudioSource.PlayClipAtPoint(doorSound, transform.position, 1.0f);

        currentMode = BusMode.Parked;
        playerCanBoard = true;
        if (boardPromptObj != null) boardPromptObj.SetActive(true);

        Debug.Log("[BusHazard] !Autobus estacionado! Fercho puede subir con [E].");
    }

    private void Update()
    {
        if (currentMode == BusMode.Parked && playerCanBoard)
        {
            var keyboard = Keyboard.current;
            var gamepad = Gamepad.current;

            bool boardPressed = (keyboard != null && (keyboard.eKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame))
                             || (gamepad != null && gamepad.buttonSouth.wasPressedThisFrame);

            if (boardPressed)
            {
                StartCoroutine(BoardAndDepartRoutine());
            }
        }
    }

    private IEnumerator BoardAndDepartRoutine()
    {
        playerCanBoard = false;
        if (boardPromptObj != null) boardPromptObj.SetActive(false);

        PlayerController p = FindFirstObjectByType<PlayerController>();
        if (p != null)
        {
            p.gameObject.SetActive(false);
        }

        if (doorSound != null) AudioSource.PlayClipAtPoint(doorSound, transform.position, 1.0f);
        yield return new WaitForSeconds(0.6f);

        if (hornSound != null) AudioSource.PlayClipAtPoint(hornSound, transform.position, 1.0f);

        currentMode = BusMode.Departing;
        while (transform.position.x < rightDespawnX)
        {
            transform.position += Vector3.right * (speedDepart * Time.deltaTime);
            yield return null;
        }

        BossHUD hud = FindFirstObjectByType<BossHUD>();
        if (hud != null)
        {
            hud.TriggerVictorySequence(null, null);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (currentMode != BusMode.Speeding) return;

        if (other.TryGetComponent<Health>(out var h) && !h.IsDead)
        {
            bool isPlayer = other.CompareTag("Player") || other.GetComponent<PlayerController>() != null;

            if (isPlayer)
            {
                if (other.transform.position.y < -6.8f)
                {
                    Vector2 knockDir = new Vector2(1f, 0.4f).normalized;
                    h.TakeDamage(15, knockDir, 8f);
                    Debug.Log("[BusHazard] !Fercho fue rozado por el autobus!");
                }
            }
            else
            {
                Vector2 knockDir = new Vector2(1.5f, 0.5f).normalized;
                h.TakeDamage(hitDamage, knockDir, hitKnockback);
                Debug.Log($"[BusHazard] !EL AUTOBUS ATROPELLO A {other.name}! Dano masivo: {hitDamage}");
            }
        }
    }
}
