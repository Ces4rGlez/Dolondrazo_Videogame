using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// MobileHUD - HUD tactil completo y de alta calidad para Dolondrazo Mobile.
/// Se instancia automaticamente al arrancar cualquier escena de juego.
/// Dibuja todo por codigo: joystick virtual, botones de combate con animaciones,
/// indicador de dash y boton de pausa. Escribe en MobileInputBridge.
/// 100% compatible con el nuevo Input System Package de Unity.
/// </summary>
public class MobileHUD : MonoBehaviour
{
    // ---- Singleton ----
    public static MobileHUD Instance { get; private set; }

    // ---- Joystick ----
    private RectTransform joystickBase;
    private RectTransform joystickThumb;
    private int joystickTouchId = -1;
    private bool mouseJoystickActive = false;
    private Vector2 joystickCenter;
    private const float JoystickRadius = 90f;
    private const float ThumbDeadZone = 0.12f;

    // ---- Botones de accion ----
    private GameObject btnPunch;
    private GameObject btnKick;
    private GameObject btnJump;

    // ---- Efectos de pulsacion ----
    private Image btnPunchImg;
    private Image btnKickImg;
    private Image btnJumpImg;

    // ---- Estado de doble tap (Dash) ----
    private float lastJoystickTapTime = -1f;
    private Vector2 lastJoystickDir = Vector2.zero;
    private const float DoubleTapWindow = 0.28f;

    // Colores del tema arcade
    private static readonly Color ColorPunch      = new Color(0.95f, 0.35f, 0.1f,  1f);
    private static readonly Color ColorPunchGlow  = new Color(1f,    0.6f,  0.2f,  1f);
    private static readonly Color ColorKick       = new Color(0.1f,  0.5f,  0.95f, 1f);
    private static readonly Color ColorKickGlow   = new Color(0.3f,  0.75f, 1f,    1f);
    private static readonly Color ColorJump       = new Color(0.15f, 0.78f, 0.35f, 1f);
    private static readonly Color ColorJumpGlow   = new Color(0.3f,  1f,    0.55f, 1f);
    private static readonly Color ColorJoyBase    = new Color(0.08f, 0.1f,  0.18f, 0.75f);
    private static readonly Color ColorJoyThumb   = new Color(0.3f,  0.6f,  1f,    0.92f);
    private static readonly Color ColorJoyRing    = new Color(0.25f, 0.55f, 0.9f,  0.5f);

    // ---- Canvas propio del HUD movil ----
    private Canvas hudCanvas;
    private RectTransform hudRoot;

    // ---- Pausa ----
    private PauseManager pauseManager;

    // ================================================================
    //  INICIALIZACION AUTOMATICA (RuntimeInitializeOnLoadMethod)
    // ================================================================
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInit()
    {
        // Solo inicializar en escenas de juego, no en el menu
        string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (scene == "Pantalla_Inicio" || scene == "MainMenu" || scene == "Menu_Principal") return;

        // Solo en dispositivos con pantalla tactil o editor para pruebas
        if (!IsTouchDevice()) return;

        if (Instance != null) return;

        GameObject go = new GameObject("MobileHUD_Controller");
        DontDestroyOnLoad(go);
        go.AddComponent<MobileHUD>();
    }

    private static bool IsTouchDevice()
    {
#if UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS
        return true;
#else
        return Touchscreen.current != null;
#endif
    }

    // ================================================================
    //  LIFECYCLE
    // ================================================================
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        // Destruir el canvas anterior y reconstruir para la nueva escena
        if (hudCanvas != null) Destroy(hudCanvas.gameObject);

        string name = scene.name;
        bool isGameScene = name == "Nivel_1_Calzada" || name == "Nivel_2_Cinepolis" || name == "Nivel_3_CBTIS75";
        if (!isGameScene) return;

        pauseManager = null;
        StartCoroutine(BuildHUDNextFrame());
    }

    private IEnumerator BuildHUDNextFrame()
    {
        yield return null; // Esperar un frame para que el HUD del nivel ya exista
        pauseManager = FindFirstObjectByType<PauseManager>();
        BuildFullHUD();
    }

    private void Start()
    {
        string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        bool isGameScene = scene == "Nivel_1_Calzada" || scene == "Nivel_2_Cinepolis" || scene == "Nivel_3_CBTIS75";
        if (isGameScene)
        {
            pauseManager = FindFirstObjectByType<PauseManager>();
            StartCoroutine(BuildHUDNextFrame());
        }
    }

    // ================================================================
    //  CONSTRUCCION DEL HUD
    // ================================================================
    private void BuildFullHUD()
    {
        // --- Canvas propio (overlay, encima de todo) ---
        GameObject canvasGo = new GameObject("Mobile_HUD_Canvas");
        DontDestroyOnLoad(canvasGo);
        hudCanvas = canvasGo.AddComponent<Canvas>();
        hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        hudCanvas.sortingOrder = 200; // Por encima del HUD del juego

        UnityEngine.UI.CanvasScaler scaler = canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 1f; // Priorizar altura (landscape)

        canvasGo.AddComponent<GraphicRaycaster>();

        // Asegurar EventSystem con InputSystemUIInputModule (compatible con New Input System)
        EventSystem es = FindFirstObjectByType<EventSystem>();
        if (es == null)
        {
            GameObject evSys = new GameObject("Mobile_EventSystem");
            es = evSys.AddComponent<EventSystem>();
            evSys.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            DontDestroyOnLoad(evSys);
        }
        else
        {
            var oldModule = es.GetComponent<StandaloneInputModule>();
            if (oldModule != null)
            {
                Destroy(oldModule);
                if (es.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() == null)
                {
                    es.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
                }
            }
        }

        // Raiz del HUD (pantalla completa)
        hudRoot = new GameObject("HUD_Root", typeof(RectTransform)).GetComponent<RectTransform>();
        hudRoot.SetParent(hudCanvas.transform, false);
        hudRoot.anchorMin = Vector2.zero;
        hudRoot.anchorMax = Vector2.one;
        hudRoot.offsetMin = hudRoot.offsetMax = Vector2.zero;

        Sprite white = MakeWhiteSprite();
        Sprite circle = MakeCircleSprite(128);

        BuildJoystick(white, circle);
        BuildActionButtons(white, circle);
        BuildPauseButton(white);
    }

    // ================================================================
    //  JOYSTICK VIRTUAL
    // ================================================================
    private void BuildJoystick(Sprite white, Sprite circle)
    {
        // --- Base exterior (anillo) ---
        GameObject baseGo = new GameObject("Joy_Base", typeof(RectTransform), typeof(Image));
        baseGo.transform.SetParent(hudRoot, false);
        joystickBase = baseGo.GetComponent<RectTransform>();
        joystickBase.anchorMin = new Vector2(0f, 0f);
        joystickBase.anchorMax = new Vector2(0f, 0f);
        joystickBase.pivot     = new Vector2(0.5f, 0.5f);
        joystickBase.anchoredPosition = new Vector2(220f, 180f);
        joystickBase.sizeDelta = new Vector2(JoystickRadius * 2.2f, JoystickRadius * 2.2f);

        Image baseImg = baseGo.GetComponent<Image>();
        baseImg.sprite = circle;
        baseImg.color  = ColorJoyBase;
        baseImg.type   = Image.Type.Simple;
        baseImg.preserveAspect = true;

        // Anillo luminoso exterior
        GameObject ring = new GameObject("Joy_Ring", typeof(RectTransform), typeof(Image));
        ring.transform.SetParent(baseGo.transform, false);
        RectTransform ringRt = ring.GetComponent<RectTransform>();
        ringRt.anchorMin = ringRt.anchorMax = new Vector2(0.5f, 0.5f);
        ringRt.pivot = new Vector2(0.5f, 0.5f);
        ringRt.anchoredPosition = Vector2.zero;
        ringRt.sizeDelta = new Vector2(JoystickRadius * 2.2f, JoystickRadius * 2.2f);
        Image ringImg = ring.GetComponent<Image>();
        ringImg.sprite = circle;
        ringImg.color  = ColorJoyRing;
        ringImg.type   = Image.Type.Simple;
        ringImg.preserveAspect = true;

        // Icono de flechas en el centro (texto simbolico)
        AddJoystickIcon(baseGo.transform);

        // --- Thumb (el circulo interior que se mueve) ---
        GameObject thumbGo = new GameObject("Joy_Thumb", typeof(RectTransform), typeof(Image));
        thumbGo.transform.SetParent(baseGo.transform, false);
        joystickThumb = thumbGo.GetComponent<RectTransform>();
        joystickThumb.anchorMin = joystickThumb.anchorMax = new Vector2(0.5f, 0.5f);
        joystickThumb.pivot     = new Vector2(0.5f, 0.5f);
        joystickThumb.anchoredPosition = Vector2.zero;
        joystickThumb.sizeDelta = new Vector2(70f, 70f);

        Image thumbImg = thumbGo.GetComponent<Image>();
        thumbImg.sprite = circle;
        thumbImg.color  = ColorJoyThumb;
        thumbImg.type   = Image.Type.Simple;
        thumbImg.preserveAspect = true;

        // Brillo interno del thumb
        GameObject thumbGlow = new GameObject("Joy_Thumb_Glow", typeof(RectTransform), typeof(Image));
        thumbGlow.transform.SetParent(thumbGo.transform, false);
        RectTransform tgRt = thumbGlow.GetComponent<RectTransform>();
        tgRt.anchorMin = tgRt.anchorMax = new Vector2(0.5f, 0.5f);
        tgRt.pivot = new Vector2(0.5f, 0.5f);
        tgRt.anchoredPosition = Vector2.zero;
        tgRt.sizeDelta = new Vector2(38f, 38f);
        Image tgImg = thumbGlow.GetComponent<Image>();
        tgImg.sprite = circle;
        tgImg.color  = new Color(0.65f, 0.88f, 1f, 0.9f);
        tgImg.preserveAspect = true;
    }

    private void AddJoystickIcon(Transform parent)
    {
        string[] arrows = { "▲", "▼", "◀", "▶" };
        Vector2[] positions = {
            new Vector2(0f,  52f),
            new Vector2(0f, -52f),
            new Vector2(-52f, 0f),
            new Vector2( 52f, 0f)
        };

        for (int i = 0; i < 4; i++)
        {
            GameObject arrow = new GameObject("Arrow_" + arrows[i], typeof(RectTransform), typeof(Text));
            arrow.transform.SetParent(parent, false);
            RectTransform rt = arrow.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = positions[i];
            rt.sizeDelta = new Vector2(32f, 32f);
            Text t = arrow.GetComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.text = arrows[i];
            t.fontSize = 18;
            t.color = new Color(0.4f, 0.65f, 1f, 0.55f);
            t.alignment = TextAnchor.MiddleCenter;
        }
    }

    // ================================================================
    //  BOTONES DE ACCION
    // ================================================================
    private void BuildActionButtons(Sprite white, Sprite circle)
    {
        // Layout ergonomico en el lado derecho de la pantalla
        float baseX = -220f;
        float baseY =  180f;

        btnPunch = BuildActionButton(circle, white,
            new Vector2(baseX - 95f, baseY - 10f), 95f,
            "J", "GOLPE", ColorPunch, ColorPunchGlow,
            () => { MobileInputBridge.PunchPressedThisFrame = true; MobileInputBridge.AnyButtonTouchedThisFrame = true; },
            ref btnPunchImg);

        btnKick = BuildActionButton(circle, white,
            new Vector2(baseX + 95f, baseY - 10f), 95f,
            "K", "PATADA", ColorKick, ColorKickGlow,
            () => { MobileInputBridge.KickPressedThisFrame = true; MobileInputBridge.AnyButtonTouchedThisFrame = true; },
            ref btnKickImg);

        btnJump = BuildActionButton(circle, white,
            new Vector2(baseX, baseY + 95f), 85f,
            "↑", "SALTAR", ColorJump, ColorJumpGlow,
            () => { MobileInputBridge.JumpPressedThisFrame = true; MobileInputBridge.AnyButtonTouchedThisFrame = true; },
            ref btnJumpImg);
    }

    private GameObject BuildActionButton(Sprite circle, Sprite white,
        Vector2 anchoredPos, float size,
        string icon, string label,
        Color normalColor, Color glowColor,
        System.Action onPress,
        ref Image imgOut)
    {
        GameObject root = new GameObject("Btn_" + label, typeof(RectTransform));
        root.transform.SetParent(hudRoot, false);
        RectTransform rootRt = root.GetComponent<RectTransform>();
        rootRt.anchorMin = new Vector2(1f, 0f);
        rootRt.anchorMax = new Vector2(1f, 0f);
        rootRt.pivot     = new Vector2(0.5f, 0.5f);
        rootRt.anchoredPosition = anchoredPos;
        rootRt.sizeDelta = new Vector2(size, size);

        // Sombra exterior
        GameObject shadow = new GameObject("Shadow", typeof(RectTransform), typeof(Image));
        shadow.transform.SetParent(root.transform, false);
        RectTransform shadowRt = shadow.GetComponent<RectTransform>();
        shadowRt.anchorMin = shadowRt.anchorMax = new Vector2(0.5f, 0.5f);
        shadowRt.pivot     = new Vector2(0.5f, 0.5f);
        shadowRt.anchoredPosition = new Vector2(3f, -3f);
        shadowRt.sizeDelta = new Vector2(size, size);
        Image shadowImg = shadow.GetComponent<Image>();
        shadowImg.sprite = circle;
        shadowImg.color  = new Color(0f, 0f, 0f, 0.4f);
        shadowImg.preserveAspect = true;

        // Circulo principal
        GameObject main = new GameObject("Main", typeof(RectTransform), typeof(Image));
        main.transform.SetParent(root.transform, false);
        RectTransform mainRt = main.GetComponent<RectTransform>();
        mainRt.anchorMin = mainRt.anchorMax = new Vector2(0.5f, 0.5f);
        mainRt.pivot     = new Vector2(0.5f, 0.5f);
        mainRt.anchoredPosition = Vector2.zero;
        mainRt.sizeDelta = new Vector2(size, size);
        Image mainImg = main.GetComponent<Image>();
        mainImg.sprite = circle;
        mainImg.color  = normalColor;
        mainImg.preserveAspect = true;
        imgOut = mainImg;

        // Brillo superior (efecto 3D)
        GameObject shine = new GameObject("Shine", typeof(RectTransform), typeof(Image));
        shine.transform.SetParent(main.transform, false);
        RectTransform shineRt = shine.GetComponent<RectTransform>();
        shineRt.anchorMin = new Vector2(0.1f, 0.55f);
        shineRt.anchorMax = new Vector2(0.9f, 0.95f);
        shineRt.offsetMin = shineRt.offsetMax = Vector2.zero;
        Image shineImg = shine.GetComponent<Image>();
        shineImg.sprite = circle;
        shineImg.color  = new Color(1f, 1f, 1f, 0.22f);
        shineImg.preserveAspect = true;

        // Icono (letra/simbolo)
        GameObject iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Text));
        iconGo.transform.SetParent(main.transform, false);
        RectTransform iconRt = iconGo.GetComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0.5f, 0.4f);
        iconRt.anchorMax = new Vector2(0.5f, 0.75f);
        iconRt.pivot     = new Vector2(0.5f, 0.5f);
        iconRt.anchoredPosition = Vector2.zero;
        iconRt.sizeDelta = new Vector2(size * 0.7f, size * 0.5f);
        Text iconText = iconGo.GetComponent<Text>();
        Font arcadeFont = Resources.Load<Font>("Fonts/ArcadeBold");
        if (arcadeFont == null) arcadeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        iconText.font      = arcadeFont;
        iconText.text      = icon;
        iconText.fontSize  = Mathf.RoundToInt(size * 0.42f);
        iconText.fontStyle = FontStyle.Bold;
        iconText.alignment = TextAnchor.MiddleCenter;
        iconText.color     = new Color(1f, 1f, 1f, 0.95f);
        Outline outline = iconGo.AddComponent<Outline>();
        outline.effectColor    = new Color(0f, 0f, 0f, 0.6f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        // Etiqueta inferior
        GameObject labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
        labelGo.transform.SetParent(main.transform, false);
        RectTransform labelRt = labelGo.GetComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(0f, 0.05f);
        labelRt.anchorMax = new Vector2(1f, 0.3f);
        labelRt.offsetMin = labelRt.offsetMax = Vector2.zero;
        Text labelText = labelGo.GetComponent<Text>();
        labelText.font      = arcadeFont;
        labelText.text      = label;
        labelText.fontSize  = Mathf.RoundToInt(size * 0.17f);
        labelText.fontStyle = FontStyle.Bold;
        labelText.alignment = TextAnchor.MiddleCenter;
        labelText.color     = new Color(1f, 1f, 1f, 0.7f);

        // Trigger tactil interactivo
        GameObject trigger = new GameObject("TouchTrigger", typeof(RectTransform), typeof(Image), typeof(MobileButtonTrigger));
        trigger.transform.SetParent(root.transform, false);
        RectTransform triggerRt = trigger.GetComponent<RectTransform>();
        triggerRt.anchorMin = Vector2.zero;
        triggerRt.anchorMax = Vector2.one;
        triggerRt.offsetMin = triggerRt.offsetMax = Vector2.zero;
        Image triggerImg = trigger.GetComponent<Image>();
        triggerImg.color = new Color(0f, 0f, 0f, 0.001f);
        MobileButtonTrigger mbt = trigger.GetComponent<MobileButtonTrigger>();
        mbt.Setup(onPress, mainImg, normalColor, glowColor);

        return root;
    }

    // ================================================================
    //  BOTON DE PAUSA
    // ================================================================
    private void BuildPauseButton(Sprite white)
    {
        GameObject btnGo = new GameObject("Btn_Pause", typeof(RectTransform), typeof(Image), typeof(Button));
        btnGo.transform.SetParent(hudRoot, false);
        RectTransform rt = btnGo.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot     = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-24f, -24f);
        rt.sizeDelta = new Vector2(80f, 80f);

        Image bg = btnGo.GetComponent<Image>();
        bg.sprite = MakeCircleSprite(64);
        bg.color  = new Color(0.08f, 0.1f, 0.2f, 0.85f);
        bg.preserveAspect = true;

        GameObject iconGo = new GameObject("PauseIcon", typeof(RectTransform), typeof(Text));
        iconGo.transform.SetParent(btnGo.transform, false);
        RectTransform iconRt = iconGo.GetComponent<RectTransform>();
        iconRt.anchorMin = iconRt.anchorMax = new Vector2(0.5f, 0.5f);
        iconRt.pivot     = new Vector2(0.5f, 0.5f);
        iconRt.anchoredPosition = Vector2.zero;
        iconRt.sizeDelta = new Vector2(80f, 80f);
        Text iconText = iconGo.GetComponent<Text>();
        iconText.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        iconText.text      = "❚❚";
        iconText.fontSize  = 28;
        iconText.fontStyle = FontStyle.Bold;
        iconText.alignment = TextAnchor.MiddleCenter;
        iconText.color     = new Color(1f, 0.88f, 0.2f, 0.95f);

        Button btn = btnGo.GetComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor      = new Color(0.08f, 0.1f, 0.2f, 0.85f);
        cb.highlightedColor = new Color(0.15f, 0.2f, 0.4f, 0.95f);
        cb.pressedColor     = new Color(0.3f,  0.4f, 0.7f, 0.95f);
        btn.colors = cb;
        btn.onClick.AddListener(() =>
        {
            if (pauseManager != null)
                pauseManager.PauseGame();
            else
            {
                pauseManager = FindFirstObjectByType<PauseManager>();
                if (pauseManager != null) pauseManager.PauseGame();
            }
        });
    }

    // ================================================================
    //  UPDATE - INPUT NATIVO CON NEW INPUT SYSTEM
    // ================================================================
    private void Update()
    {
        HandleJoystickInput();
    }

    private void HandleJoystickInput()
    {
        // 1. Deteccion tactil (dispositivos moviles / simulador tactil)
        var touchScreen = Touchscreen.current;
        if (touchScreen != null)
        {
            var touches = touchScreen.touches;
            for (int i = 0; i < touches.Count; i++)
            {
                var t = touches[i];
                if (!t.isInProgress) continue;

                Vector2 screenPos = t.position.ReadValue();
                bool isLeftSide   = screenPos.x < Screen.width * 0.45f;
                int touchId       = t.touchId.ReadValue();

                if (t.press.wasPressedThisFrame && isLeftSide && joystickTouchId == -1)
                {
                    joystickTouchId = touchId;
                    joystickCenter  = ScreenToCanvasPos(screenPos);
                    SetJoystickBasePosition(joystickCenter);
                    CheckDoubleTap(Vector2.zero);
                }
                else if (touchId == joystickTouchId)
                {
                    if (t.press.isPressed)
                    {
                        Vector2 canvasPos = ScreenToCanvasPos(screenPos);
                        UpdateJoystickThumb(canvasPos);
                    }
                    else
                    {
                        ResetJoystick();
                    }
                }
            }
        }
        else if (joystickTouchId != -1)
        {
            ResetJoystick();
        }

        // 2. Deteccion con Mouse (para pruebas directamente en el Editor de Unity)
        var mouse = Mouse.current;
        if (mouse != null)
        {
            Vector2 mouseScreen = mouse.position.ReadValue();
            bool isLeftSide = mouseScreen.x < Screen.width * 0.45f;

            if (mouse.leftButton.wasPressedThisFrame && isLeftSide && !mouseJoystickActive && joystickTouchId == -1)
            {
                mouseJoystickActive = true;
                joystickCenter = ScreenToCanvasPos(mouseScreen);
                SetJoystickBasePosition(joystickCenter);
                CheckDoubleTap(Vector2.zero);
            }
            else if (mouse.leftButton.isPressed && mouseJoystickActive)
            {
                Vector2 canvasPos = ScreenToCanvasPos(mouseScreen);
                UpdateJoystickThumb(canvasPos);
            }
            else if (mouse.leftButton.wasReleasedThisFrame && mouseJoystickActive)
            {
                mouseJoystickActive = false;
                ResetJoystick();
            }
        }
    }

    private void UpdateJoystickThumb(Vector2 canvasPos)
    {
        Vector2 delta = canvasPos - joystickCenter;
        float dist    = delta.magnitude;
        Vector2 dir   = delta / Mathf.Max(dist, 0.001f);

        if (dist > JoystickRadius) delta = dir * JoystickRadius;

        if (joystickThumb != null)
        {
            joystickThumb.anchoredPosition = delta;
        }

        // Input normalizado
        float normX = delta.x / JoystickRadius;
        float normY = delta.y / JoystickRadius;

        if (Mathf.Abs(normX) < ThumbDeadZone) normX = 0f;
        if (Mathf.Abs(normY) < ThumbDeadZone) normY = 0f;

        MobileInputBridge.MoveX = normX;
        MobileInputBridge.MoveY = normY;
        MobileInputBridge.AnyButtonTouchedThisFrame = (Mathf.Abs(normX) > 0.1f || Mathf.Abs(normY) > 0.1f);

        CheckDoubleTap(dir);
    }

    private void CheckDoubleTap(Vector2 dir)
    {
        if (dir == Vector2.zero) return;
        float now = Time.unscaledTime;
        float dot  = Vector2.Dot(dir.normalized, lastJoystickDir.normalized);
        bool sameDir = dot > 0.7f;

        if (sameDir && (now - lastJoystickTapTime) < DoubleTapWindow)
        {
            MobileInputBridge.DashPressedThisFrame = true;
            lastJoystickTapTime = -1f;
        }
        else
        {
            lastJoystickTapTime = now;
            lastJoystickDir     = dir;
        }
    }

    private void ResetJoystick()
    {
        joystickTouchId = -1;
        mouseJoystickActive = false;
        if (joystickThumb != null)
        {
            joystickThumb.anchoredPosition = Vector2.zero;
        }
        MobileInputBridge.MoveX = 0f;
        MobileInputBridge.MoveY = 0f;
    }

    private void SetJoystickBasePosition(Vector2 canvasPos)
    {
        if (joystickBase != null)
        {
            joystickBase.anchoredPosition = canvasPos;
        }
    }

    // ================================================================
    //  HELPERS DE CONVERSION DE COORDENADAS
    // ================================================================
    private Vector2 ScreenToCanvasPos(Vector2 screenPos)
    {
        if (hudCanvas == null || hudRoot == null) return screenPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            hudRoot, screenPos, hudCanvas.worldCamera, out Vector2 localPoint);
        return localPoint;
    }

    // ================================================================
    //  GENERADORES DE SPRITES PROCEDURALES
    // ================================================================
    private static Sprite MakeWhiteSprite()
    {
        Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        Color[] px = new Color[16];
        for (int i = 0; i < 16; i++) px[i] = Color.white;
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
    }

    private static Sprite MakeCircleSprite(int resolution)
    {
        Texture2D tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color[] px = new Color[resolution * resolution];
        Vector2 center = new Vector2(resolution / 2f, resolution / 2f);
        float r = resolution / 2f;
        float edge = r * 0.92f;
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float alpha = Mathf.Clamp01((r - dist) / (r - edge));
                px[y * resolution + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, resolution, resolution),
                             new Vector2(0.5f, 0.5f), resolution);
    }
}

// ====================================================================
//  COMPONENTE AUXILIAR: MobileButtonTrigger
//  Detecta toques con soporte multi-touch y anima el boton al presionar
// ====================================================================
public class MobileButtonTrigger : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
{
    private System.Action onPressAction;
    private Image targetImage;
    private Color normalColor;
    private Color pressedColor;
    private Coroutine flashRoutine;

    public void Setup(System.Action onPress, Image img, Color normal, Color pressed)
    {
        onPressAction = onPress;
        targetImage   = img;
        normalColor   = normal;
        pressedColor  = pressed;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        onPressAction?.Invoke();
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashButton());
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (targetImage != null)
            targetImage.color = normalColor;
    }

    public void OnPointerClick(PointerEventData eventData) { /* Manejado en OnPointerDown */ }

    private IEnumerator FlashButton()
    {
        if (targetImage == null) yield break;

        // Flash de presion (escala + color)
        targetImage.color = pressedColor;
        RectTransform rt  = targetImage.GetComponent<RectTransform>();
        Vector3 origScale = rt != null ? rt.localScale : Vector3.one;

        if (rt != null) rt.localScale = origScale * 0.88f;
        yield return new WaitForSecondsRealtime(0.08f);

        if (rt != null) rt.localScale = origScale;
        targetImage.color = normalColor;
        flashRoutine = null;
    }
}
