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
    [SerializeField] private AudioClip victoryMusic;

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
    private Coroutine promptPulseCoroutine;

    // UI de Alerta y Abordaje
    private GameObject warningBannerObj;
    private Text warningText;
    private GameObject boardPromptObj;
    private Text boardPromptText;
    private Image boardPromptBg;

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
        EnsureUI();
        transform.position = new Vector3(leftSpawnX, defaultLaneY, 0f);
        currentMode = BusMode.Inactive;
    }

    private void EnsureUI()
    {
        if (boardPromptObj != null && warningBannerObj != null) return;

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("HUD_Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObj.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;
            CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
        }

        Texture2D tex = new Texture2D(4, 4);
        Color[] px = new Color[16];
        for (int i = 0; i < px.Length; i++) px[i] = Color.white;
        tex.SetPixels(px); tex.Apply();
        Sprite white = Sprite.Create(tex, new Rect(0,0,4,4), new Vector2(0.5f,0.5f));

        // 1. Banner de Alerta de Atropello
        if (warningBannerObj == null)
        {
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
        }

        // 2. Prompt de abordaje (Presionar [E])
        if (boardPromptObj == null)
        {
            boardPromptObj = new GameObject("Bus_BoardPrompt", typeof(RectTransform), typeof(Image));
            boardPromptObj.transform.SetParent(canvas.transform, false);
            RectTransform rtb = boardPromptObj.GetComponent<RectTransform>();
            rtb.anchorMin = new Vector2(0.5f, 0.22f);
            rtb.anchorMax = new Vector2(0.5f, 0.22f);
            rtb.pivot = new Vector2(0.5f, 0.5f);
            rtb.anchoredPosition = Vector2.zero;
            rtb.sizeDelta = new Vector2(540f, 65f);

            boardPromptBg = boardPromptObj.GetComponent<Image>();
            boardPromptBg.sprite = white;
            boardPromptBg.color = new Color(0.08f, 0.65f, 0.22f, 0.95f);

            // Borde superior decorativo dorado
            GameObject borderTop = new GameObject("GoldBorder", typeof(RectTransform), typeof(Image));
            borderTop.transform.SetParent(boardPromptObj.transform, false);
            RectTransform rtbt = borderTop.GetComponent<RectTransform>();
            rtbt.anchorMin = new Vector2(0f, 1f);
            rtbt.anchorMax = new Vector2(1f, 1f);
            rtbt.pivot = new Vector2(0.5f, 1f);
            rtbt.anchoredPosition = Vector2.zero;
            rtbt.sizeDelta = new Vector2(0f, 4f);
            Image bImg = borderTop.GetComponent<Image>();
            bImg.sprite = white;
            bImg.color = new Color(1f, 0.88f, 0.2f, 1f);

            GameObject bt = new GameObject("BoardText", typeof(RectTransform), typeof(Text));
            bt.transform.SetParent(boardPromptObj.transform, false);
            RectTransform rtbText = bt.GetComponent<RectTransform>();
            rtbText.anchorMin = Vector2.zero;
            rtbText.anchorMax = Vector2.one;
            rtbText.sizeDelta = Vector2.zero;
            boardPromptText = bt.GetComponent<Text>();
            boardPromptText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            boardPromptText.fontSize = 22;
            boardPromptText.fontStyle = FontStyle.Bold;
            boardPromptText.alignment = TextAnchor.MiddleCenter;
            boardPromptText.color = Color.white;
            boardPromptText.text = ">> PRESIONA [E] PARA SUBIR AL AUTOBUS <<";

            boardPromptObj.SetActive(false);
        }
    }

    /// <summary>
    /// Dispara el cruce a toda velocidad del autobus atropellando enemigos.
    /// </summary>
    public void TriggerPassingHazard(float laneY = 0f)
    {
        if (currentMode != BusMode.Inactive) return;
        if (laneY == 0f) laneY = defaultLaneY;
        gameObject.SetActive(true);
        EnsureUI();
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
        EnsureUI();
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

        EnsureUI();
        if (boardPromptObj != null)
        {
            boardPromptObj.SetActive(true);
            boardPromptObj.transform.SetAsLastSibling();
            if (promptPulseCoroutine != null) StopCoroutine(promptPulseCoroutine);
            promptPulseCoroutine = StartCoroutine(PulsePromptRoutine());
        }

        Debug.Log("[BusHazard] Autobus estacionado frente al CBTis 75. Presiona [E] para subir.");
    }

    private IEnumerator PulsePromptRoutine()
    {
        Color colorA = new Color(0.08f, 0.65f, 0.22f, 0.95f);
        Color colorB = new Color(0.15f, 0.85f, 0.35f, 1f);

        while (playerCanBoard)
        {
            if (boardPromptBg != null)
            {
                float t = Mathf.PingPong(Time.time * 2.5f, 1f);
                boardPromptBg.color = Color.Lerp(colorA, colorB, t);
            }
            yield return null;
        }
    }

    private void Update()
    {
        if (currentMode == BusMode.Parked && playerCanBoard)
        {
            var keyboard = Keyboard.current;
            var gamepad = Gamepad.current;

            bool boardPressed = (keyboard != null && (keyboard.eKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame))
                             || (gamepad != null && (gamepad.buttonSouth.wasPressedThisFrame || gamepad.buttonWest.wasPressedThisFrame))
                             || Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return);

            if (boardPressed)
            {
                StartCoroutine(BoardAndDepartRoutine());
            }
        }
    }

    private IEnumerator BoardAndDepartRoutine()
    {
        playerCanBoard = false;
        if (promptPulseCoroutine != null) StopCoroutine(promptPulseCoroutine);
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
            hud.TriggerVictorySequence(victoryMusic, null);
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
