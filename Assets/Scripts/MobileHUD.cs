using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// MobileHUD - Interfaz táctil profesional moderna estilo Gamepad de consola para Dolondrazo Mobile.
/// Rediseñada con ergonomía ampliada (+50% tamaño), respuesta inmediata vía EventSystem nativo,
/// y acabado gráfico ultra-nítido inspirado en mandos para Cloud Gaming / Blacknut.
/// </summary>
public class MobileHUD : MonoBehaviour
{
    public static MobileHUD Instance { get; private set; }

    // ---- Dimensiones Ampliadas (Máximo confort ergonómico en pantallas móviles) ----
    private const float JoyBaseDiameter   = 310f; // Base circular espaciosa
    private const float JoyRadius         = 140f; // Rango fluido de recorrido
    private const float JoyThumbDiameter  = 120f; // Stick blanco generoso y visible
    private const float ThumbDeadZone     = 0.10f;

    private const float BtnDiameter       = 152f; // Botones extra grandes y nítidos
    private const float DiamondSpread     = 136f; // Separación ergonómica del diamante

    // ---- Joystick State ----
    private RectTransform joystickBase;
    private RectTransform joystickThumb;

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
    private static readonly Color ColorDarkGlass   = new Color(0.05f, 0.07f, 0.11f, 0.74f);
    private static readonly Color ColorJoyOuter    = new Color(0.85f, 0.90f, 0.98f, 0.60f);
    private static readonly Color ColorJoyGuide    = new Color(0.40f, 0.50f, 0.65f, 0.35f);
    private static readonly Color ColorWhiteThumb  = new Color(0.98f, 0.98f, 1.00f, 0.98f);

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

    private void LateUpdate()
    {
        MobileInputBridge.ClearFrameInputs();
    }

    public void HandleSceneChange(string sceneName)
    {
        bool isGameScene = sceneName == "Nivel_1_Calzada" || sceneName == "Nivel_2_Cinepolis" || sceneName == "Nivel_3_CBTIS75";

        if (!isGameScene)
        {
            if (hudCanvas != null)
            {
                Destroy(hudCanvas.gameObject);
                hudCanvas = null;
            }
            return;
        }

        StartCoroutine(RebuildHUDForCurrentScene());
    }

    private IEnumerator RebuildHUDForCurrentScene()
    {
        yield return null; // Esperar a que la escena se estabilice

        FixLevel2BackgroundGaps(SceneManager.GetActiveScene().name);

        if (hudCanvas != null)
        {
            Destroy(hudCanvas.gameObject);
        }

        pauseManager = FindFirstObjectByType<PauseManager>();
        BuildFullModernHUD();
    }

    private void FixLevel2BackgroundGaps(string sceneName)
    {
        if (sceneName == "Nivel_2_Cinepolis")
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.backgroundColor = new Color(0.05f, 0.06f, 0.1f, 1f);
                if (mainCam.transform.position.x < -5f)
                {
                    mainCam.transform.position = new Vector3(0f, 0f, -10f);
                }
            }

            GameObject bg = GameObject.Find("Fondo_Cinepolis");
            if (bg != null && GameObject.Find("Fondo_Cinepolis_Left") == null)
            {
                SpriteRenderer sr = bg.GetComponent<SpriteRenderer>();
                float width = 47.17f;
                if (sr != null && sr.sprite != null)
                {
                    width = (sr.sprite.rect.width / sr.sprite.pixelsPerUnit) * bg.transform.localScale.x;
                }

                GameObject leftExt = Instantiate(bg, bg.transform.position - new Vector3(width - 0.05f, 0f, 0f), bg.transform.rotation);
                leftExt.name = "Fondo_Cinepolis_Left";
            }
        }
    }

    // ================================================================
    //  CONSTRUCCIÓN DEL HUD PROFESIONAL
    // ================================================================
    private void BuildFullModernHUD()
    {
        EnsureSpritesCached();

        // 1. Canvas ScreenSpaceOverlay con sorting 90 (debajo de menus/dialogos que estan en sorting 100)
        GameObject canvasGo = new GameObject("Modern_Mobile_HUD_Canvas");
        DontDestroyOnLoad(canvasGo);
        hudCanvas = canvasGo.AddComponent<Canvas>();
        hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        hudCanvas.sortingOrder = 90; // Permite que menus, dialogos y avances en 100 reciban clicks con total prioridad

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

        // Construir controles
        BuildModernJoystick();
        BuildModernDiamondCluster();
        BuildModernPauseButton();
    }

    // ================================================================
    //  JOYSTICK MODERNO CON TOUCH ZONE ULTRA-RESPONSIVO
    // ================================================================
    private void BuildModernJoystick()
    {
        // 1. Zona táctil local confinada al cuadrante inferior izquierdo del joystick (hasta 42% ancho y 58% alto)
        GameObject touchZone = new GameObject("Joy_TouchZone", typeof(RectTransform), typeof(Image));
        touchZone.transform.SetParent(hudRoot, false);
        RectTransform tzRt = touchZone.GetComponent<RectTransform>();
        tzRt.anchorMin = new Vector2(0f, 0f);
        tzRt.anchorMax = new Vector2(0.42f, 0.58f);
        tzRt.pivot     = new Vector2(0f, 0f);
        tzRt.offsetMin = tzRt.offsetMax = Vector2.zero;
        Image tzImg = touchZone.GetComponent<Image>();
        tzImg.color = new Color(0f, 0f, 0f, 0.001f); // Invisible pero intercepta toques en su propia área
        tzImg.raycastTarget = true;

        // 2. Base del Joystick anclada en la esquina inferior izquierda
        GameObject baseGo = new GameObject("Joy_Base", typeof(RectTransform), typeof(Image));
        baseGo.transform.SetParent(hudRoot, false);
        joystickBase = baseGo.GetComponent<RectTransform>();
        joystickBase.anchorMin = new Vector2(0f, 0f);
        joystickBase.anchorMax = new Vector2(0f, 0f);
        joystickBase.pivot     = new Vector2(0.5f, 0.5f);
        joystickBase.anchoredPosition = new Vector2(260f, 260f);
        joystickBase.sizeDelta = new Vector2(JoyBaseDiameter, JoyBaseDiameter);

        // Fondo de cristal oscuro (sin bloquear raycasts)
        Image baseImg = baseGo.GetComponent<Image>();
        baseImg.sprite = cachedCircleFilled;
        baseImg.color  = ColorDarkGlass;
        baseImg.raycastTarget = false;

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
        igImg.raycastTarget = false;

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
        ringImg.raycastTarget = false;

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
        thumbImg.raycastTarget = false;

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
        trImg.raycastTarget = false;

        // Colocar la zona táctil al frente para que ningún gráfico la tape
        touchZone.transform.SetAsLastSibling();

        // Conectar el trigger táctil nativo para captura fluida y directa (caminar y sprint con joystick)
        ModernJoystickTrigger joyTrigger = touchZone.AddComponent<ModernJoystickTrigger>();
        joyTrigger.Setup(joystickBase, joystickThumb, JoyRadius, ThumbDeadZone,
            onMove: (norm) => {
                MobileInputBridge.MoveX = norm.x;
                MobileInputBridge.MoveY = norm.y;
                float mag = Mathf.Sqrt(norm.x * norm.x + norm.y * norm.y);
                MobileInputBridge.RunHeld = (mag >= 0.72f);
                MobileInputBridge.AnyButtonTouchedThisFrame = (mag > 0.1f);
            },
            onRelease: () => {
                MobileInputBridge.MoveX = 0f;
                MobileInputBridge.MoveY = 0f;
                MobileInputBridge.RunHeld = false;
            },
            onDoubleTap: null
        );
    }

    private void AddDirectionalMarkers(Transform parent)
    {
        string[] markers = { "▲", "▼", "◀", "▶" };
        float dist = (JoyBaseDiameter / 2f) - 22f;
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
            rt.sizeDelta = new Vector2(30f, 30f);

            Text t = m.GetComponent<Text>();
            t.font = sansFont;
            t.text = markers[i];
            t.fontSize = 18;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = new Color(0.75f, 0.85f, 1f, 0.35f);
            t.raycastTarget = false;
        }
    }

    // ================================================================
    //  BOTONES DE ACCIÓN ARCADE (3 BOTONES: GOLPE, PATADA, SALTAR)
    //  X (Izquierda) = GOLPE / AGARRAR (Cian Eléctrico)
    //  B (Derecha)   = PATADA / LANZAR (Rojo Carmesí)
    //  A (Abajo)     = SALTAR (Verde Esmeralda)
    // ================================================================
    private void BuildModernDiamondCluster()
    {
        GameObject diamondCluster = new GameObject("Action_Cluster", typeof(RectTransform));
        diamondCluster.transform.SetParent(hudRoot, false);
        RectTransform clusterRt = diamondCluster.GetComponent<RectTransform>();
        clusterRt.anchorMin = new Vector2(1f, 0f);
        clusterRt.anchorMax = new Vector2(1f, 0f);
        clusterRt.pivot     = new Vector2(0.5f, 0.5f);
        clusterRt.anchoredPosition = new Vector2(-270f, 260f);
        clusterRt.sizeDelta = new Vector2(400f, 360f);

        float actionBtnSize = 114f;

        // 1. Botón X (Izquierda) -> GOLPE / AGARRAR (Cian)
        BuildGamepadButton(diamondCluster.transform,
            new Vector2(-88f, 32f), actionBtnSize,
            "X", "GOLPE", ColorX_Cyan,
            onDown: () => {
                MobileInputBridge.PunchPressedThisFrame = true;
                MobileInputBridge.AnyButtonTouchedThisFrame = true;
            },
            onUp: null);

        // 2. Botón B (Derecha) -> PATADA (Rojo)
        BuildGamepadButton(diamondCluster.transform,
            new Vector2(88f, 32f), actionBtnSize,
            "B", "PATADA", ColorB_Red,
            onDown: () => {
                MobileInputBridge.KickPressedThisFrame = true;
                MobileInputBridge.AnyButtonTouchedThisFrame = true;
            },
            onUp: null);

        // 3. Botón A (Abajo) -> SALTAR (Verde)
        BuildGamepadButton(diamondCluster.transform,
            new Vector2(0f, -80f), actionBtnSize,
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
        sRt.anchoredPosition = new Vector2(0f, -5f);
        sRt.sizeDelta = new Vector2(size * 1.05f, size * 1.05f);
        Image sImg = shadowObj.GetComponent<Image>();
        sImg.sprite = cachedCircleFilled;
        sImg.color = new Color(0f, 0f, 0f, 0.42f);
        sImg.raycastTarget = false;

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
        bgImg.raycastTarget = false;

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
        ringImg.raycastTarget = false;

        // 5. Letra grande estilo consola moderna (Y, X, B, A)
        GameObject letterObj = new GameObject("Letter", typeof(RectTransform), typeof(Text));
        letterObj.transform.SetParent(btnRoot.transform, false);
        RectTransform letterRt = letterObj.GetComponent<RectTransform>();
        letterRt.anchorMin = new Vector2(0.5f, 0.5f);
        letterRt.anchorMax = new Vector2(0.5f, 0.5f);
        letterRt.pivot     = new Vector2(0.5f, 0.5f);
        letterRt.anchoredPosition = new Vector2(0f, 10f);
        letterRt.sizeDelta = new Vector2(size * 0.7f, size * 0.5f);
        Text letterTxt = letterObj.GetComponent<Text>();
        letterTxt.font      = sansFont;
        letterTxt.text      = letter;
        letterTxt.fontSize  = Mathf.RoundToInt(size * 0.42f);
        letterTxt.fontStyle = FontStyle.Bold;
        letterTxt.alignment = TextAnchor.MiddleCenter;
        letterTxt.color     = neonColor;
        letterTxt.raycastTarget = false;

        // Sombra de letra
        Shadow letterShadow = letterObj.AddComponent<Shadow>();
        letterShadow.effectColor = new Color(0f, 0f, 0f, 0.95f);
        letterShadow.effectDistance = new Vector2(2f, -2f);

        // 6. Etiqueta de acción limpia y elegante (GOLPE, PATADA, SALTAR, CORRER)
        GameObject labelObj = new GameObject("Label", typeof(RectTransform), typeof(Text));
        labelObj.transform.SetParent(btnRoot.transform, false);
        RectTransform labelRt = labelObj.GetComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(0.5f, 0.5f);
        labelRt.anchorMax = new Vector2(0.5f, 0.5f);
        labelRt.pivot     = new Vector2(0.5f, 0.5f);
        labelRt.anchoredPosition = new Vector2(0f, -32f);
        labelRt.sizeDelta = new Vector2(size * 0.9f, 28f);
        Text labelTxt = labelObj.GetComponent<Text>();
        labelTxt.font      = sansFont;
        labelTxt.text      = actionLabel;
        labelTxt.fontSize  = Mathf.RoundToInt(size * 0.14f);
        labelTxt.fontStyle = FontStyle.Bold;
        labelTxt.alignment = TextAnchor.MiddleCenter;
        labelTxt.color     = new Color(0.92f, 0.95f, 1.0f, 0.88f);
        labelTxt.raycastTarget = false;

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
        triggerImg.raycastTarget = true;

        ModernButtonTrigger mbt = trigger.GetComponent<ModernButtonTrigger>();
        Color glowColor = new Color(neonColor.r, neonColor.g, neonColor.b, 0.40f);
        mbt.Setup(onDown, onUp, bgImg, ColorDarkGlass, glowColor);
    }

    // ================================================================
    //  BOTÓN SUPERIOR DE PAUSA VISIBLE Y DESPEJADO
    // ================================================================
    private void BuildModernPauseButton()
    {
        Font sansFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Posicionado a la izquierda del panel de puntuación (-340f, -38f), libre de solapamientos
        GameObject pauseObj = new GameObject("Btn_TopPause", typeof(RectTransform), typeof(Image), typeof(Button));
        pauseObj.transform.SetParent(hudRoot, false);
        RectTransform rt = pauseObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot     = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-340f, -38f);
        rt.sizeDelta = new Vector2(195f, 68f);

        Image bg = pauseObj.GetComponent<Image>();
        bg.sprite = cachedCapsule;
        bg.color  = ColorDarkGlass;

        // Borde fino de cápsula neón cian
        GameObject borderObj = new GameObject("Capsule_Border", typeof(RectTransform), typeof(Image));
        borderObj.transform.SetParent(pauseObj.transform, false);
        RectTransform brt = borderObj.GetComponent<RectTransform>();
        brt.anchorMin = Vector2.zero;
        brt.anchorMax = Vector2.one;
        brt.offsetMin = brt.offsetMax = Vector2.zero;
        Image bImg = borderObj.GetComponent<Image>();
        bImg.sprite = cachedCapsule;
        bImg.color  = new Color(0.20f, 0.85f, 1.0f, 0.85f);
        bImg.raycastTarget = false;

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
        t.fontSize  = 22;
        t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter;
        t.color     = Color.white;
        t.raycastTarget = false;

        Shadow txtShadow = txtObj.AddComponent<Shadow>();
        txtShadow.effectColor = new Color(0f, 0f, 0f, 0.95f);
        txtShadow.effectDistance = new Vector2(2f, -2f);

        Button btn = pauseObj.GetComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor      = ColorDarkGlass;
        cb.highlightedColor = new Color(0.12f, 0.22f, 0.40f, 0.95f);
        cb.pressedColor     = new Color(0.22f, 0.42f, 0.70f, 1.0f);
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
//  COMPONENTE NATIVO: ModernJoystickTrigger
//  Maneja la entrada del joystick mediante el EventSystem nativo de Unity.
//  Garantiza respuesta 100% inmediata, sprint analógico y failsafe anti-bloqueo.
// ====================================================================
public class ModernJoystickTrigger : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler, IEndDragHandler
{
    private RectTransform joystickBase;
    private RectTransform joystickThumb;
    private float maxRadius;
    private float deadZone;
    private System.Action<Vector2> onMove;
    private System.Action onRelease;
    private System.Action onDoubleTap;

    private bool isPressed = false;
    private int activePointerId = -1;

    public void Setup(RectTransform baseRt, RectTransform thumbRt, float radius, float deadzone,
                      System.Action<Vector2> onMove, System.Action onRelease, System.Action onDoubleTap)
    {
        joystickBase  = baseRt;
        joystickThumb = thumbRt;
        maxRadius     = radius;
        deadZone      = deadzone;
        this.onMove   = onMove;
        this.onRelease = onRelease;
        this.onDoubleTap = onDoubleTap;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (Time.timeScale == 0f || (DialogueSystem.Instance != null && DialogueSystem.Instance.IsActive)) return;
        isPressed = true;
        activePointerId = eventData.pointerId;
        ProcessInput(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (Time.timeScale == 0f || (DialogueSystem.Instance != null && DialogueSystem.Instance.IsActive)) return;
        if (!isPressed) return;
        ProcessInput(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        ResetJoystick();
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        ResetJoystick();
    }

    public void ResetJoystick()
    {
        isPressed = false;
        activePointerId = -1;
        if (joystickThumb != null)
        {
            joystickThumb.anchoredPosition = Vector2.zero;
        }
        onRelease?.Invoke();
    }

    private void Update()
    {
        if (!isPressed) return;

        // Failsafe 1: Si Time.timeScale es 0 o hay diálogo activo, resetear
        if (Time.timeScale == 0f || (DialogueSystem.Instance != null && DialogueSystem.Instance.IsActive))
        {
            ResetJoystick();
            return;
        }

        // Failsafe 2: En dispositivo móvil con Touchscreen (hardware)
        if (UnityEngine.InputSystem.Touchscreen.current != null)
        {
            var touches = UnityEngine.InputSystem.Touchscreen.current.touches;
            bool anyTouchActive = false;
            for (int i = 0; i < touches.Count; i++)
            {
                if (touches[i].isInProgress)
                {
                    anyTouchActive = true;
                    break;
                }
            }

            // Si ningún dedo está tocando físicamente la pantalla, resetear
            if (!anyTouchActive)
            {
                ResetJoystick();
                return;
            }
        }
        // Failsafe 3: En editor o mouse
        else if (UnityEngine.InputSystem.Mouse.current != null)
        {
            if (!UnityEngine.InputSystem.Mouse.current.leftButton.isPressed)
            {
                ResetJoystick();
                return;
            }
        }
    }

    private void OnDisable()
    {
        ResetJoystick();
    }

    private void ProcessInput(PointerEventData eventData)
    {
        if (joystickBase == null || joystickThumb == null) return;

        // Convertir posición de pantalla directamente al espacio local de joystickBase (donde 0,0 es el centro)
        Camera eventCam = (eventData.pressEventCamera != null && eventData.pressEventCamera.isActiveAndEnabled) ? eventData.pressEventCamera : null;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(joystickBase, eventData.position, eventCam, out Vector2 localPoint))
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(joystickBase, eventData.position, null, out localPoint);
        }

        float dist = localPoint.magnitude;
        Vector2 dir = dist > 0.001f ? localPoint / dist : Vector2.zero;

        if (dist > maxRadius) localPoint = dir * maxRadius;

        joystickThumb.anchoredPosition = localPoint;

        float normX = localPoint.x / maxRadius;
        float normY = localPoint.y / maxRadius;

        if (Mathf.Abs(normX) < deadZone) normX = 0f;
        if (Mathf.Abs(normY) < deadZone) normY = 0f;

        onMove?.Invoke(new Vector2(normX, normY));
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
        if (Time.timeScale == 0f || (DialogueSystem.Instance != null && DialogueSystem.Instance.IsActive)) return;
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
