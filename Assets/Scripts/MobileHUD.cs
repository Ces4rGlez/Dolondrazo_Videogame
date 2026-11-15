using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// MobileHUD - Interfaz táctil profesional moderna estilo Gamepad de consola.
/// Diseñada con estética de mando de última generación (inspirada en mandos de cloud gaming):
/// - Aros de neón ultra-nítidos con fondo obsidiana translúcido.
/// - Dimensiones y ergonomía generosas para pantallas táctiles de alta resolución.
/// - Diamante de 4 botones (X=Golpe, B=Patada, A=Saltar, Y=Correr).
/// - Joystick analógico con aro exterior, anillo concéntrico de guía y marcadores direccionales.
/// - Botón cápsula estilizado [ ❚❚ PAUSA ] estilo START de consola.
/// </summary>
public class MobileHUD : MonoBehaviour
{
    public static MobileHUD Instance { get; private set; }

    // ---- Dimensiones Profesionales (Optimizadas para pantallas móviles 1080p/FHD+) ----
    private const float JoyBaseDiameter   = 280f; // Diámetro amplio para maniobrar cómodamente
    private const float JoyRadius         = 130f; // Rango de desplazamiento del stick
    private const float JoyThumbDiameter  = 104f; // Tamaño del stick blanco táctil
    private const float ThumbDeadZone     = 0.12f;

    private const float BtnDiameter       = 122f; // Diámetro generoso de los botones (~50% más grandes)
    private const float DiamondSpread     = 112f; // Separación del centro del diamante

    // ---- Joystick State ----
    private RectTransform joystickBase;
    private RectTransform joystickThumb;
    private int joystickTouchId = -1;
    private bool mouseJoystickActive = false;
    private Vector2 joystickCenter;

    // ---- Doble tap para dash rápido ----
    private float lastJoystickTapTime = -1f;
    private Vector2 lastJoystickDir = Vector2.zero;
    private const float DoubleTapWindow = 0.28f;

    // ---- Paleta de Neón y Obsidiana Estilo Gamepad Moderno ----
    // X = Cian Eléctrico (Golpe / Puñetazo)
    private static readonly Color ColorX_Cyan    = new Color(0.00f, 0.85f, 1.00f, 1.0f);
    // B = Rojo Carmesí / Coral (Patada)
    private static readonly Color ColorB_Red     = new Color(1.00f, 0.22f, 0.26f, 1.0f);
    // A = Verde Esmeralda (Saltar)
    private static readonly Color ColorA_Green   = new Color(0.00f, 0.90f, 0.44f, 1.0f);
    // Y = Amarillo Eléctrico (Correr / Dash)
    private static readonly Color ColorY_Yellow  = new Color(1.00f, 0.86f, 0.05f, 1.0f);

    // Fondos y Aros
    private static readonly Color ColorDarkGlass   = new Color(0.05f, 0.07f, 0.11f, 0.72f);
    private static readonly Color ColorJoyOuter    = new Color(0.85f, 0.90f, 0.98f, 0.55f);
    private static readonly Color ColorJoyGuide    = new Color(0.40f, 0.50f, 0.65f, 0.30f);
    private static readonly Color ColorWhiteThumb  = new Color(0.98f, 0.98f, 1.00f, 0.96f);

    // ---- UI Canvas ----
    private Canvas hudCanvas;
    private RectTransform hudRoot;
    private PauseManager pauseManager;

    // ---- Caché de Sprites Procedurales de Alta Resolución ----
    private static Sprite cachedCircleFilled;
    private static Sprite cachedNeonRing;
    private static Sprite cachedOuterJoyRing;
    private static Sprite cachedThinGuideRing;
    private static Sprite cachedCapsule;

    // ================================================================
    //  INICIALIZACIÓN GLOBAL PERMANENTE
    // ================================================================
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterGlobalHook()
    {
        SceneManager.sceneLoaded += OnSceneLoadedStatic;
    }

    private static void OnSceneLoadedStatic(Scene scene, LoadSceneMode mode)
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("MobileHUD_Controller");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<MobileHUD>();
        }

        Instance.HandleSceneChange(scene.name);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        EnsureSpritesCached();
    }

    private void Start()
    {
        HandleSceneChange(SceneManager.GetActiveScene().name);
    }

    public void HandleSceneChange(string sceneName)
    {
        bool isGameScene = sceneName == "Nivel_1_Calzada" || sceneName == "Nivel_2_Cinepolis" || sceneName == "Nivel_3_CBTIS75";

        if (!isGameScene)
        {
            if (hudCanvas != null) hudCanvas.gameObject.SetActive(false);
            return;
        }

        StartCoroutine(RebuildHUDForCurrentScene());
    }

    private IEnumerator RebuildHUDForCurrentScene()
    {
        yield return null; // Esperar a que la escena se estabilice

        if (hudCanvas != null)
        {
            Destroy(hudCanvas.gameObject);
        }

        pauseManager = FindFirstObjectByType<PauseManager>();
        BuildFullModernHUD();
    }

    // ================================================================
    //  CONSTRUCCIÓN DEL HUD PROFESIONAL
    // ================================================================
    private void BuildFullModernHUD()
    {
        EnsureSpritesCached();

        // 1. Canvas ScreenSpaceOverlay con sorting alto
        GameObject canvasGo = new GameObject("Modern_Mobile_HUD_Canvas");
        DontDestroyOnLoad(canvasGo);
        hudCanvas = canvasGo.AddComponent<Canvas>();
        hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        hudCanvas.sortingOrder = 450; // Siempre por encima del juego

        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 1f;

        canvasGo.AddComponent<GraphicRaycaster>();

        // 2. EventSystem compatible con New Input System
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

        // 3. Raíz del HUD
        hudRoot = new GameObject("HUD_Root", typeof(RectTransform)).GetComponent<RectTransform>();
        hudRoot.SetParent(hudCanvas.transform, false);
        hudRoot.anchorMin = Vector2.zero;
        hudRoot.anchorMax = Vector2.one;
        hudRoot.offsetMin = hudRoot.offsetMax = Vector2.zero;

        // Construir controles estilo consola moderna
        BuildModernJoystick();
        BuildModernDiamondCluster();
        BuildModernTopMenu();
    }

    // ================================================================
    //  JOYSTICK MODERNO CON GUÍAS CONCÉNTRICAS (Izquierda)
    // ================================================================
    private void BuildModernJoystick()
    {
        // Base en la esquina inferior izquierda
        GameObject baseGo = new GameObject("Joy_Base", typeof(RectTransform), typeof(Image));
        baseGo.transform.SetParent(hudRoot, false);
        joystickBase = baseGo.GetComponent<RectTransform>();
        joystickBase.anchorMin = new Vector2(0f, 0f);
        joystickBase.anchorMax = new Vector2(0f, 0f);
        joystickBase.pivot     = new Vector2(0.5f, 0.5f);
        joystickBase.anchoredPosition = new Vector2(250f, 250f);
        joystickBase.sizeDelta = new Vector2(JoyBaseDiameter, JoyBaseDiameter);

        // Fondo de cristal oscuro
        Image baseImg = baseGo.GetComponent<Image>();
        baseImg.sprite = cachedCircleFilled;
        baseImg.color  = ColorDarkGlass;

        // Anillo de guía interior concéntrico (como en el mando de referencia)
        GameObject innerGuide = new GameObject("Inner_Guide_Ring", typeof(RectTransform), typeof(Image));
        innerGuide.transform.SetParent(baseGo.transform, false);
        RectTransform igRt = innerGuide.GetComponent<RectTransform>();
        igRt.anchorMin = igRt.anchorMax = new Vector2(0.5f, 0.5f);
        igRt.pivot = new Vector2(0.5f, 0.5f);
        igRt.anchoredPosition = Vector2.zero;
        igRt.sizeDelta = new Vector2(JoyBaseDiameter * 0.62f, JoyBaseDiameter * 0.62f);
        Image igImg = innerGuide.GetComponent<Image>();
        igImg.sprite = cachedThinGuideRing;
        igImg.color  = ColorJoyGuide;

        // Aro exterior nítido
        GameObject ringGo = new GameObject("Outer_Ring", typeof(RectTransform), typeof(Image));
        ringGo.transform.SetParent(baseGo.transform, false);
        RectTransform ringRt = ringGo.GetComponent<RectTransform>();
        ringRt.anchorMin = ringRt.anchorMax = new Vector2(0.5f, 0.5f);
        ringRt.pivot     = new Vector2(0.5f, 0.5f);
        ringRt.anchoredPosition = Vector2.zero;
        ringRt.sizeDelta = new Vector2(JoyBaseDiameter, JoyBaseDiameter);
        Image ringImg = ringGo.GetComponent<Image>();
        ringImg.sprite = cachedOuterJoyRing;
        ringImg.color  = ColorJoyOuter;

        // 4 Marcadores direccionales triangulares
        AddDirectionalMarkers(baseGo.transform);

        // Stick blanco táctil (thumb)
        GameObject thumbGo = new GameObject("Joy_Thumb", typeof(RectTransform), typeof(Image));
        thumbGo.transform.SetParent(baseGo.transform, false);
        joystickThumb = thumbGo.GetComponent<RectTransform>();
        joystickThumb.anchorMin = joystickThumb.anchorMax = new Vector2(0.5f, 0.5f);
        joystickThumb.pivot     = new Vector2(0.5f, 0.5f);
        joystickThumb.anchoredPosition = Vector2.zero;
        joystickThumb.sizeDelta = new Vector2(JoyThumbDiameter, JoyThumbDiameter);

        Image thumbImg = thumbGo.GetComponent<Image>();
        thumbImg.sprite = cachedCircleFilled;
        thumbImg.color  = ColorWhiteThumb;

        // Anillo interior sutil en el stick para darle profundidad
        GameObject thumbRing = new GameObject("Thumb_Ring", typeof(RectTransform), typeof(Image));
        thumbRing.transform.SetParent(thumbGo.transform, false);
        RectTransform trRt = thumbRing.GetComponent<RectTransform>();
        trRt.anchorMin = trRt.anchorMax = new Vector2(0.5f, 0.5f);
        trRt.pivot = new Vector2(0.5f, 0.5f);
        trRt.anchoredPosition = Vector2.zero;
        trRt.sizeDelta = new Vector2(JoyThumbDiameter * 0.75f, JoyThumbDiameter * 0.75f);
        Image trImg = thumbRing.GetComponent<Image>();
        trImg.sprite = cachedThinGuideRing;
        trImg.color  = new Color(0.1f, 0.15f, 0.25f, 0.22f);
    }

    private void AddDirectionalMarkers(Transform parent)
    {
        string[] markers = { "▲", "▼", "◀", "▶" };
        float dist = (JoyBaseDiameter / 2f) - 18f;
        Vector2[] pos = {
            new Vector2(0f,   dist),
            new Vector2(0f,  -dist),
            new Vector2(-dist,  0f),
            new Vector2( dist,  0f)
        };

        Font sansFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        for (int i = 0; i < 4; i++)
        {
            GameObject m = new GameObject("Marker_" + i, typeof(RectTransform), typeof(Text));
            m.transform.SetParent(parent, false);
            RectTransform rt = m.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos[i];
            rt.sizeDelta = new Vector2(36f, 36f);

            Text t = m.GetComponent<Text>();
            t.font = sansFont;
            t.text = markers[i];
            t.fontSize = 20;
            t.color = new Color(0.88f, 0.94f, 1f, 0.85f);
            t.alignment = TextAnchor.MiddleCenter;
        }
    }

    // ================================================================
    //  DIAMANTE PROFESIONAL DE 4 BOTONES (Derecha)
    //  Disposición exacta estilo Mando / Gamepad moderno:
    //  Y (Arriba)   = CORRER / DASH (Amarillo Neón)
    //  X (Izquierda)= GOLPE (Cian Eléctrico)
    //  B (Derecha)  = PATADA (Rojo Carmesí)
    //  A (Abajo)    = SALTAR (Verde Esmeralda)
    // ================================================================
    private void BuildModernDiamondCluster()
    {
        GameObject diamondCluster = new GameObject("Diamond_Cluster", typeof(RectTransform));
        diamondCluster.transform.SetParent(hudRoot, false);
        RectTransform clusterRt = diamondCluster.GetComponent<RectTransform>();
        clusterRt.anchorMin = new Vector2(1f, 0f);
        clusterRt.anchorMax = new Vector2(1f, 0f);
        clusterRt.pivot     = new Vector2(0.5f, 0.5f);
        clusterRt.anchoredPosition = new Vector2(-260f, 250f);
        clusterRt.sizeDelta = new Vector2(360f, 360f);

        // 1. Botón Y (Arriba) -> CORRER / DASH (Amarillo)
        BuildGamepadButton(diamondCluster.transform,
            new Vector2(0f, DiamondSpread), BtnDiameter,
            "Y", "CORRER", ColorY_Yellow,
            onDown: () => {
                MobileInputBridge.RunHeld = true;
                MobileInputBridge.DashPressedThisFrame = true;
                MobileInputBridge.AnyButtonTouchedThisFrame = true;
            },
            onUp: () => {
                MobileInputBridge.RunHeld = false;
            });

        // 2. Botón X (Izquierda) -> GOLPE (Cian)
        BuildGamepadButton(diamondCluster.transform,
            new Vector2(-DiamondSpread, 0f), BtnDiameter,
            "X", "GOLPE", ColorX_Cyan,
            onDown: () => {
                MobileInputBridge.PunchPressedThisFrame = true;
                MobileInputBridge.AnyButtonTouchedThisFrame = true;
            },
            onUp: null);

        // 3. Botón B (Derecha) -> PATADA (Rojo)
        BuildGamepadButton(diamondCluster.transform,
            new Vector2(DiamondSpread, 0f), BtnDiameter,
            "B", "PATADA", ColorB_Red,
            onDown: () => {
                MobileInputBridge.KickPressedThisFrame = true;
                MobileInputBridge.AnyButtonTouchedThisFrame = true;
            },
            onUp: null);

        // 4. Botón A (Abajo) -> SALTAR (Verde)
        BuildGamepadButton(diamondCluster.transform,
            new Vector2(0f, -DiamondSpread), BtnDiameter,
            "A", "SALTAR", ColorA_Green,
            onDown: () => {
                MobileInputBridge.JumpPressedThisFrame = true;
                MobileInputBridge.AnyButtonTouchedThisFrame = true;
            },
            onUp: null);
    }

    private void BuildGamepadButton(Transform parent, Vector2 pos, float size,
        string letter, string actionLabel, Color neonColor,
        System.Action onDown, System.Action onUp)
    {
        Font sansFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // 1. Raíz del botón
        GameObject btnRoot = new GameObject($"Btn_{letter}_{actionLabel}", typeof(RectTransform));
        btnRoot.transform.SetParent(parent, false);
        RectTransform rt = btnRoot.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(size, size);

        // 2. Sombra sutil exterior
        GameObject shadowObj = new GameObject("Shadow", typeof(RectTransform), typeof(Image));
        shadowObj.transform.SetParent(btnRoot.transform, false);
        RectTransform sRt = shadowObj.GetComponent<RectTransform>();
        sRt.anchorMin = sRt.anchorMax = new Vector2(0.5f, 0.5f);
        sRt.pivot = new Vector2(0.5f, 0.5f);
        sRt.anchoredPosition = new Vector2(0f, -4f);
        sRt.sizeDelta = new Vector2(size * 1.05f, size * 1.05f);
        Image sImg = shadowObj.GetComponent<Image>();
        sImg.sprite = cachedCircleFilled;
        sImg.color = new Color(0f, 0f, 0f, 0.40f);

        // 3. Fondo obsidiana translúcido
        GameObject bgObj = new GameObject("Bg_Glass", typeof(RectTransform), typeof(Image));
        bgObj.transform.SetParent(btnRoot.transform, false);
        RectTransform bgRt = bgObj.GetComponent<RectTransform>();
        bgRt.anchorMin = bgRt.anchorMax = new Vector2(0.5f, 0.5f);
        bgRt.pivot     = new Vector2(0.5f, 0.5f);
        bgRt.anchoredPosition = Vector2.zero;
        bgRt.sizeDelta = new Vector2(size, size);
        Image bgImg = bgObj.GetComponent<Image>();
        bgImg.sprite = cachedCircleFilled;
        bgImg.color  = ColorDarkGlass;

        // 4. Anillo de neón de alta definición
        GameObject ringObj = new GameObject("Neon_Ring", typeof(RectTransform), typeof(Image));
        ringObj.transform.SetParent(btnRoot.transform, false);
        RectTransform ringRt = ringObj.GetComponent<RectTransform>();
        ringRt.anchorMin = ringRt.anchorMax = new Vector2(0.5f, 0.5f);
        ringRt.pivot     = new Vector2(0.5f, 0.5f);
        ringRt.anchoredPosition = Vector2.zero;
        ringRt.sizeDelta = new Vector2(size, size);
        Image ringImg = ringObj.GetComponent<Image>();
        ringImg.sprite = cachedNeonRing;
        ringImg.color  = neonColor;

        // 5. Letra grande estilo consola moderna (Y, X, B, A)
        GameObject letterObj = new GameObject("Letter", typeof(RectTransform), typeof(Text));
        letterObj.transform.SetParent(btnRoot.transform, false);
        RectTransform letterRt = letterObj.GetComponent<RectTransform>();
        letterRt.anchorMin = new Vector2(0.5f, 0.5f);
        letterRt.anchorMax = new Vector2(0.5f, 0.5f);
        letterRt.pivot     = new Vector2(0.5f, 0.5f);
        letterRt.anchoredPosition = new Vector2(0f, 9f);
        letterRt.sizeDelta = new Vector2(size * 0.7f, size * 0.5f);
        Text letterTxt = letterObj.GetComponent<Text>();
        letterTxt.font      = sansFont;
        letterTxt.text      = letter;
        letterTxt.fontSize  = Mathf.RoundToInt(size * 0.42f);
        letterTxt.fontStyle = FontStyle.Bold;
        letterTxt.alignment = TextAnchor.MiddleCenter;
        letterTxt.color     = neonColor;

        // Sombra de letra
        Shadow letterShadow = letterObj.AddComponent<Shadow>();
        letterShadow.effectColor = new Color(0f, 0f, 0f, 0.95f);
        letterShadow.effectDistance = new Vector2(1.5f, -1.5f);

        // 6. Etiqueta de acción limpia y elegante (GOLPE, PATADA, SALTAR, CORRER)
        GameObject labelObj = new GameObject("Label", typeof(RectTransform), typeof(Text));
        labelObj.transform.SetParent(btnRoot.transform, false);
        RectTransform labelRt = labelObj.GetComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(0.5f, 0.5f);
        labelRt.anchorMax = new Vector2(0.5f, 0.5f);
        labelRt.pivot     = new Vector2(0.5f, 0.5f);
        labelRt.anchoredPosition = new Vector2(0f, -28f);
        labelRt.sizeDelta = new Vector2(size * 0.9f, 26f);
        Text labelTxt = labelObj.GetComponent<Text>();
        labelTxt.font      = sansFont;
        labelTxt.text      = actionLabel;
        labelTxt.fontSize  = Mathf.RoundToInt(size * 0.14f);
        labelTxt.fontStyle = FontStyle.Bold;
        labelTxt.alignment = TextAnchor.MiddleCenter;
        labelTxt.color     = new Color(0.92f, 0.95f, 1.0f, 0.88f);

        Shadow labelShadow = labelObj.AddComponent<Shadow>();
        labelShadow.effectColor = new Color(0f, 0f, 0f, 0.95f);
        labelShadow.effectDistance = new Vector2(1f, -1f);

        // 7. Trigger táctil con micro-animación suave
        GameObject trigger = new GameObject("Touch_Trigger", typeof(RectTransform), typeof(Image), typeof(ModernButtonTrigger));
        trigger.transform.SetParent(btnRoot.transform, false);
        RectTransform triggerRt = trigger.GetComponent<RectTransform>();
        triggerRt.anchorMin = Vector2.zero;
        triggerRt.anchorMax = Vector2.one;
        triggerRt.offsetMin = triggerRt.offsetMax = Vector2.zero;
        Image triggerImg = trigger.GetComponent<Image>();
        triggerImg.color = new Color(0f, 0f, 0f, 0.001f);

        ModernButtonTrigger mbt = trigger.GetComponent<ModernButtonTrigger>();
        Color glowColor = new Color(neonColor.r, neonColor.g, neonColor.b, 0.40f);
        mbt.Setup(onDown, onUp, bgImg, ColorDarkGlass, glowColor);
    }

    // ================================================================
    //  BOTÓN SUPERIOR DE PAUSA ESTILO START (Cápsula de consola)
    // ================================================================
    private void BuildModernTopMenu()
    {
        Font sansFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        GameObject pauseObj = new GameObject("Btn_TopPause", typeof(RectTransform), typeof(Image), typeof(Button));
        pauseObj.transform.SetParent(hudRoot, false);
        RectTransform rt = pauseObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot     = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-36f, -30f);
        rt.sizeDelta = new Vector2(150f, 50f);

        Image bg = pauseObj.GetComponent<Image>();
        bg.sprite = cachedCapsule;
        bg.color  = ColorDarkGlass;

        // Borde fino de cápsula
        GameObject borderObj = new GameObject("Capsule_Border", typeof(RectTransform), typeof(Image));
        borderObj.transform.SetParent(pauseObj.transform, false);
        RectTransform brt = borderObj.GetComponent<RectTransform>();
        brt.anchorMin = Vector2.zero;
        brt.anchorMax = Vector2.one;
        brt.offsetMin = brt.offsetMax = Vector2.zero;
        Image bImg = borderObj.GetComponent<Image>();
        bImg.sprite = cachedCapsule;
        bImg.color  = new Color(0.70f, 0.80f, 0.95f, 0.45f);

        // Texto START / PAUSA
        GameObject txtObj = new GameObject("Label", typeof(RectTransform), typeof(Text));
        txtObj.transform.SetParent(pauseObj.transform, false);
        RectTransform txtRt = txtObj.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = txtRt.offsetMax = Vector2.zero;
        Text t = txtObj.GetComponent<Text>();
        t.font      = sansFont;
        t.text      = "❚❚  PAUSA";
        t.fontSize  = 16;
        t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter;
        t.color     = new Color(0.92f, 0.96f, 1f, 0.95f);

        Button btn = pauseObj.GetComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor      = ColorDarkGlass;
        cb.highlightedColor = new Color(0.12f, 0.20f, 0.35f, 0.90f);
        cb.pressedColor     = new Color(0.20f, 0.35f, 0.60f, 0.95f);
        btn.colors = cb;
        btn.onClick.AddListener(() =>
        {
            if (pauseManager != null) pauseManager.PauseGame();
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
        // 1. Detección táctil (Celulares Android / iOS)
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

        // 2. Detección con Mouse (pruebas directas en el Editor de Unity)
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

        if (dist > JoyRadius) delta = dir * JoyRadius;

        if (joystickThumb != null)
        {
            joystickThumb.anchoredPosition = delta;
        }

        float normX = delta.x / JoyRadius;
        float normY = delta.y / JoyRadius;

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

    private Vector2 ScreenToCanvasPos(Vector2 screenPos)
    {
        if (hudCanvas == null || hudRoot == null) return screenPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            hudRoot, screenPos, hudCanvas.worldCamera, out Vector2 localPoint);
        return localPoint;
    }

    // ================================================================
    //  GENERACIÓN DE TEXTURAS DE ALTA DEFINICIÓN (256x256 Anti-Aliased)
    // ================================================================
    private static void EnsureSpritesCached()
    {
        if (cachedCircleFilled != null) return;

        cachedCircleFilled   = GenerateCircle(256);
        cachedNeonRing       = GenerateRing(256, 0.085f);  // Anillo fino y elegante
        cachedOuterJoyRing   = GenerateRing(256, 0.045f);  // Anillo exterior de joystick fino
        cachedThinGuideRing  = GenerateRing(256, 0.025f);  // Línea guía interior muy fina
        cachedCapsule        = GenerateRoundedCapsule(256, 96, 44);
    }

    private static Sprite GenerateCircle(int res)
    {
        Texture2D tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color[] px = new Color[res * res];
        Vector2 center = new Vector2(res / 2f, res / 2f);
        float r = (res / 2f) - 2f;

        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float alpha = Mathf.Clamp01((r - dist) / 1.5f);
                px[y * res + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), res);
    }

    private static Sprite GenerateRing(int res, float thicknessRatio)
    {
        Texture2D tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color[] px = new Color[res * res];
        Vector2 center = new Vector2(res / 2f, res / 2f);
        float outerR = (res / 2f) - 2f;
        float stroke = outerR * thicknessRatio;
        float innerR = outerR - stroke;

        for (int y = 0; y < res; y++)
        {
            for (int x = 0; x < res; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float outerAlpha = Mathf.Clamp01((outerR - dist) / 1.5f);
                float innerAlpha = Mathf.Clamp01((dist - innerR) / 1.5f);
                float alpha = Mathf.Min(outerAlpha, innerAlpha);
                px[y * res + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), res);
    }

    private static Sprite GenerateRoundedCapsule(int width, int height, int radius)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color[] px = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float dist = 0f;
                if (x < radius)
                {
                    dist = Vector2.Distance(new Vector2(x, y), new Vector2(radius, height / 2f));
                }
                else if (x > width - radius)
                {
                    dist = Vector2.Distance(new Vector2(x, y), new Vector2(width - radius, height / 2f));
                }
                else
                {
                    dist = Mathf.Abs(y - height / 2f);
                }

                float alpha = Mathf.Clamp01((radius - dist) / 1.5f);
                px[y * width + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
    }
}

// ====================================================================
//  COMPONENTE AUXILIAR: ModernButtonTrigger
//  Micro-animación ultra-suave de pulsación y respuesta de color
// ====================================================================
public class ModernButtonTrigger : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private System.Action onDownAction;
    private System.Action onUpAction;
    private Image targetBg;
    private Color normalColor;
    private Color pressedColor;
    private Coroutine animRoutine;

    public void Setup(System.Action onDown, System.Action onUp, Image bg, Color normal, Color pressed)
    {
        onDownAction = onDown;
        onUpAction   = onUp;
        targetBg     = bg;
        normalColor  = normal;
        pressedColor = pressed;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        onDownAction?.Invoke();

        if (animRoutine != null) StopCoroutine(animRoutine);
        animRoutine = StartCoroutine(AnimatePress(true));
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        onUpAction?.Invoke();

        if (animRoutine != null) StopCoroutine(animRoutine);
        animRoutine = StartCoroutine(AnimatePress(false));
    }

    private IEnumerator AnimatePress(bool isDown)
    {
        Transform parentTransform = transform.parent;
        Vector3 targetScale = isDown ? new Vector3(0.91f, 0.91f, 1f) : Vector3.one;

        if (targetBg != null)
        {
            targetBg.color = isDown ? pressedColor : normalColor;
        }

        if (parentTransform != null)
        {
            float elapsed = 0f;
            Vector3 startScale = parentTransform.localScale;
            while (elapsed < 0.05f)
            {
                elapsed += Time.unscaledDeltaTime;
                parentTransform.localScale = Vector3.Lerp(startScale, targetScale, elapsed / 0.05f);
                yield return null;
            }
            parentTransform.localScale = targetScale;
        }

        animRoutine = null;
    }
}
