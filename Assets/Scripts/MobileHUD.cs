using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// MobileHUD - HUD táctil moderno estilo Gamepad de consola para Dolondrazo Mobile.
/// Inspirado en controles modernos de gamepad:
/// - Izquierda: Joystick analógico con aro de neón y guías direccionales.
/// - Derecha: Diamante de 4 botones estilo consola (Y=Correr, X=Golpe, B=Patada, A=Saltar).
/// - Arriba: Botón cápsula estilizado de Pausa [ ❚❚ PAUSA ].
/// Se instancia automáticamente y garantiza visibilidad en todos los niveles del juego.
/// </summary>
public class MobileHUD : MonoBehaviour
{
    public static MobileHUD Instance { get; private set; }

    // ---- Joystick ----
    private RectTransform joystickBase;
    private RectTransform joystickThumb;
    private int joystickTouchId = -1;
    private bool mouseJoystickActive = false;
    private Vector2 joystickCenter;
    private const float JoystickRadius = 90f;
    private const float ThumbDeadZone = 0.12f;

    // ---- Doble tap para dash rápido ----
    private float lastJoystickTapTime = -1f;
    private Vector2 lastJoystickDir = Vector2.zero;
    private const float DoubleTapWindow = 0.28f;

    // ---- Paleta de colores Gamepad Moderno (Neón sobre Grafito) ----
    // X = Cian (Golpe / Puñetazo)
    private static readonly Color ColorX_Cyan    = new Color(0.0f,  0.88f, 1.0f,  1f);
    // B = Rojo / Coral (Patada)
    private static readonly Color ColorB_Red     = new Color(1.0f,  0.24f, 0.28f, 1f);
    // A = Verde Esmeralda (Saltar)
    private static readonly Color ColorA_Green   = new Color(0.0f,  0.92f, 0.46f, 1f);
    // Y = Amarillo Oro (Correr / Dash)
    private static readonly Color ColorY_Yellow  = new Color(1.0f,  0.84f, 0.05f, 1f);

    // Fondo y aros de botones
    private static readonly Color ColorDarkBase  = new Color(0.06f, 0.08f, 0.13f, 0.78f);
    private static readonly Color ColorJoyRing   = new Color(0.45f, 0.65f, 0.95f, 0.65f);
    private static readonly Color ColorJoyThumb  = new Color(0.95f, 0.98f, 1.0f,  0.95f);

    // ---- UI Canvas ----
    private Canvas hudCanvas;
    private RectTransform hudRoot;
    private PauseManager pauseManager;

    // ================================================================
    //  INICIALIZACIÓN GLOBAL PERMANENTE (Hook antes de cargar escenas)
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
            // Ocultar en menús
            if (hudCanvas != null) hudCanvas.gameObject.SetActive(false);
            return;
        }

        StartCoroutine(RebuildHUDForCurrentScene());
    }

    private IEnumerator RebuildHUDForCurrentScene()
    {
        // Esperar un frame a que la escena cargue sus elementos iniciales
        yield return null;

        if (hudCanvas != null)
        {
            Destroy(hudCanvas.gameObject);
        }

        pauseManager = FindFirstObjectByType<PauseManager>();
        BuildFullModernHUD();
    }

    // ================================================================
    //  CONSTRUCCIÓN DEL HUD
    // ================================================================
    private void BuildFullModernHUD()
    {
        // 1. Canvas propio (ScreenSpaceOverlay con sortingOrder alto)
        GameObject canvasGo = new GameObject("Modern_Mobile_HUD_Canvas");
        DontDestroyOnLoad(canvasGo);
        hudCanvas = canvasGo.AddComponent<Canvas>();
        hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        hudCanvas.sortingOrder = 400; // Por encima del juego y del HUD de barras

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

        Sprite circleFilled = MakeCircleSprite(128);
        Sprite circleRing   = MakeRingSprite(128, 0.16f);
        Sprite roundedCapsule = MakeRoundedCapsuleSprite(180, 60, 26);

        // Construir controles
        BuildModernJoystick(circleFilled, circleRing);
        BuildModernDiamondButtons(circleFilled, circleRing);
        BuildModernPauseButton(roundedCapsule);
    }

    // ================================================================
    //  JOYSTICK MODERNO (Izquierda)
    // ================================================================
    private void BuildModernJoystick(Sprite circleFilled, Sprite circleRing)
    {
        // Base anclada en la esquina inferior izquierda
        GameObject baseGo = new GameObject("Joy_Base", typeof(RectTransform), typeof(Image));
        baseGo.transform.SetParent(hudRoot, false);
        joystickBase = baseGo.GetComponent<RectTransform>();
        joystickBase.anchorMin = new Vector2(0f, 0f);
        joystickBase.anchorMax = new Vector2(0f, 0f);
        joystickBase.pivot     = new Vector2(0.5f, 0.5f);
        joystickBase.anchoredPosition = new Vector2(230f, 220f);
        joystickBase.sizeDelta = new Vector2(JoystickRadius * 2.2f, JoystickRadius * 2.2f);

        Image baseImg = baseGo.GetComponent<Image>();
        baseImg.sprite = circleFilled;
        baseImg.color  = ColorDarkBase;

        // Anillo de neón exterior
        GameObject ringGo = new GameObject("Joy_Ring", typeof(RectTransform), typeof(Image));
        ringGo.transform.SetParent(baseGo.transform, false);
        RectTransform ringRt = ringGo.GetComponent<RectTransform>();
        ringRt.anchorMin = ringRt.anchorMax = new Vector2(0.5f, 0.5f);
        ringRt.pivot     = new Vector2(0.5f, 0.5f);
        ringRt.anchoredPosition = Vector2.zero;
        ringRt.sizeDelta = new Vector2(JoystickRadius * 2.2f, JoystickRadius * 2.2f);
        Image ringImg = ringGo.GetComponent<Image>();
        ringImg.sprite = circleRing;
        ringImg.color  = ColorJoyRing;

        // 4 Flechas direccionales chevrons
        AddDirectionalMarkers(baseGo.transform);

        // Thumbstick (puck blanco pulido)
        GameObject thumbGo = new GameObject("Joy_Thumb", typeof(RectTransform), typeof(Image));
        thumbGo.transform.SetParent(baseGo.transform, false);
        joystickThumb = thumbGo.GetComponent<RectTransform>();
        joystickThumb.anchorMin = joystickThumb.anchorMax = new Vector2(0.5f, 0.5f);
        joystickThumb.pivot     = new Vector2(0.5f, 0.5f);
        joystickThumb.anchoredPosition = Vector2.zero;
        joystickThumb.sizeDelta = new Vector2(74f, 74f);

        Image thumbImg = thumbGo.GetComponent<Image>();
        thumbImg.sprite = circleFilled;
        thumbImg.color  = ColorJoyThumb;

        // Núcleo interno con sutil tinte cian
        GameObject thumbCore = new GameObject("Thumb_Core", typeof(RectTransform), typeof(Image));
        thumbCore.transform.SetParent(thumbGo.transform, false);
        RectTransform tcRt = thumbCore.GetComponent<RectTransform>();
        tcRt.anchorMin = tcRt.anchorMax = new Vector2(0.5f, 0.5f);
        tcRt.pivot     = new Vector2(0.5f, 0.5f);
        tcRt.anchoredPosition = Vector2.zero;
        tcRt.sizeDelta = new Vector2(36f, 36f);
        Image tcImg = thumbCore.GetComponent<Image>();
        tcImg.sprite = circleFilled;
        tcImg.color  = new Color(0.1f, 0.65f, 1.0f, 0.85f);
    }

    private void AddDirectionalMarkers(Transform parent)
    {
        string[] markers = { "▲", "▼", "◀", "▶" };
        Vector2[] pos = {
            new Vector2(0f,   62f),
            new Vector2(0f,  -62f),
            new Vector2(-62f,  0f),
            new Vector2( 62f,  0f)
        };

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
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.text = markers[i];
            t.fontSize = 20;
            t.color = new Color(0.7f, 0.85f, 1f, 0.75f);
            t.alignment = TextAnchor.MiddleCenter;
        }
    }

    // ================================================================
    //  DIAMANTE DE 4 BOTONES ESTILO GAMEPAD (Derecha)
    //  Y (Arriba)   = CORRER / DASH
    //  X (Izquierda)= GOLPE / PUÑETAZO
    //  B (Derecha)  = PATADA
    //  A (Abajo)    = SALTAR
    // ================================================================
    private void BuildModernDiamondButtons(Sprite circleFilled, Sprite circleRing)
    {
        // Contenedor del diamante anclado abajo a la derecha
        GameObject diamondCluster = new GameObject("Diamond_Cluster", typeof(RectTransform));
        diamondCluster.transform.SetParent(hudRoot, false);
        RectTransform clusterRt = diamondCluster.GetComponent<RectTransform>();
        clusterRt.anchorMin = new Vector2(1f, 0f);
        clusterRt.anchorMax = new Vector2(1f, 0f);
        clusterRt.pivot     = new Vector2(0.5f, 0.5f);
        clusterRt.anchoredPosition = new Vector2(-230f, 220f);
        clusterRt.sizeDelta = new Vector2(260f, 260f);

        float spread = 78f;  // Distancia del centro del diamante
        float btnSize = 82f; // Tamaño del botón

        // 1. Botón Y (Arriba) -> CORRER / DASH
        BuildModernGamepadButton(diamondCluster.transform, circleFilled, circleRing,
            new Vector2(0f, spread), btnSize,
            "Y", "CORRER", ColorY_Yellow,
            onDown: () => {
                MobileInputBridge.RunHeld = true;
                MobileInputBridge.DashPressedThisFrame = true;
                MobileInputBridge.AnyButtonTouchedThisFrame = true;
            },
            onUp: () => {
                MobileInputBridge.RunHeld = false;
            });

        // 2. Botón X (Izquierda) -> GOLPE (Puñetazo)
        BuildModernGamepadButton(diamondCluster.transform, circleFilled, circleRing,
            new Vector2(-spread, 0f), btnSize,
            "X", "GOLPE", ColorX_Cyan,
            onDown: () => {
                MobileInputBridge.PunchPressedThisFrame = true;
                MobileInputBridge.AnyButtonTouchedThisFrame = true;
            },
            onUp: null);

        // 3. Botón B (Derecha) -> PATADA
        BuildModernGamepadButton(diamondCluster.transform, circleFilled, circleRing,
            new Vector2(spread, 0f), btnSize,
            "B", "PATADA", ColorB_Red,
            onDown: () => {
                MobileInputBridge.KickPressedThisFrame = true;
                MobileInputBridge.AnyButtonTouchedThisFrame = true;
            },
            onUp: null);

        // 4. Botón A (Abajo) -> SALTAR
        BuildModernGamepadButton(diamondCluster.transform, circleFilled, circleRing,
            new Vector2(0f, -spread), btnSize,
            "A", "SALTAR", ColorA_Green,
            onDown: () => {
                MobileInputBridge.JumpPressedThisFrame = true;
                MobileInputBridge.AnyButtonTouchedThisFrame = true;
            },
            onUp: null);
    }

    private void BuildModernGamepadButton(Transform parent, Sprite circleFilled, Sprite circleRing,
        Vector2 pos, float size, string letter, string actionLabel, Color neonColor,
        System.Action onDown, System.Action onUp)
    {
        // Raíz del botón
        GameObject btnRoot = new GameObject($"Btn_{letter}_{actionLabel}", typeof(RectTransform));
        btnRoot.transform.SetParent(parent, false);
        RectTransform rt = btnRoot.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(size, size);

        // 1. Fondo oscuro circular
        GameObject bgObj = new GameObject("Bg", typeof(RectTransform), typeof(Image));
        bgObj.transform.SetParent(btnRoot.transform, false);
        RectTransform bgRt = bgObj.GetComponent<RectTransform>();
        bgRt.anchorMin = bgRt.anchorMax = new Vector2(0.5f, 0.5f);
        bgRt.pivot     = new Vector2(0.5f, 0.5f);
        bgRt.anchoredPosition = Vector2.zero;
        bgRt.sizeDelta = new Vector2(size, size);
        Image bgImg = bgObj.GetComponent<Image>();
        bgImg.sprite = circleFilled;
        bgImg.color  = ColorDarkBase;

        // 2. Anillo de neón exterior con el color del botón
        GameObject ringObj = new GameObject("Neon_Ring", typeof(RectTransform), typeof(Image));
        ringObj.transform.SetParent(btnRoot.transform, false);
        RectTransform ringRt = ringObj.GetComponent<RectTransform>();
        ringRt.anchorMin = ringRt.anchorMax = new Vector2(0.5f, 0.5f);
        ringRt.pivot     = new Vector2(0.5f, 0.5f);
        ringRt.anchoredPosition = Vector2.zero;
        ringRt.sizeDelta = new Vector2(size, size);
        Image ringImg = ringObj.GetComponent<Image>();
        ringImg.sprite = circleRing;
        ringImg.color  = neonColor;

        // 3. Letra principal en el centro (Y, X, B, A)
        GameObject letterObj = new GameObject("Letter", typeof(RectTransform), typeof(Text));
        letterObj.transform.SetParent(btnRoot.transform, false);
        RectTransform letterRt = letterObj.GetComponent<RectTransform>();
        letterRt.anchorMin = new Vector2(0.5f, 0.42f);
        letterRt.anchorMax = new Vector2(0.5f, 0.85f);
        letterRt.pivot     = new Vector2(0.5f, 0.5f);
        letterRt.anchoredPosition = Vector2.zero;
        letterRt.sizeDelta = new Vector2(size * 0.7f, size * 0.5f);
        Text letterTxt = letterObj.GetComponent<Text>();
        Font arcadeFont = Resources.Load<Font>("Fonts/ArcadeBold");
        if (arcadeFont == null) arcadeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        letterTxt.font      = arcadeFont;
        letterTxt.text      = letter;
        letterTxt.fontSize  = Mathf.RoundToInt(size * 0.44f);
        letterTxt.fontStyle = FontStyle.Bold;
        letterTxt.alignment = TextAnchor.MiddleCenter;
        letterTxt.color     = neonColor;
        Outline letterOut = letterObj.AddComponent<Outline>();
        letterOut.effectColor = new Color(0f, 0f, 0f, 0.85f);
        letterOut.effectDistance = new Vector2(1.5f, -1.5f);

        // 4. Etiqueta inferior con el nombre de la acción (GOLPE, PATADA, SALTAR, CORRER)
        GameObject labelObj = new GameObject("Label", typeof(RectTransform), typeof(Text));
        labelObj.transform.SetParent(btnRoot.transform, false);
        RectTransform labelRt = labelObj.GetComponent<RectTransform>();
        labelRt.anchorMin = new Vector2(0f, 0.08f);
        labelRt.anchorMax = new Vector2(1f, 0.35f);
        labelRt.offsetMin = labelRt.offsetMax = Vector2.zero;
        Text labelTxt = labelObj.GetComponent<Text>();
        labelTxt.font      = arcadeFont;
        labelTxt.text      = actionLabel;
        labelTxt.fontSize  = Mathf.RoundToInt(size * 0.16f);
        labelTxt.fontStyle = FontStyle.Bold;
        labelTxt.alignment = TextAnchor.MiddleCenter;
        labelTxt.color     = new Color(1f, 1f, 1f, 0.85f);
        Outline labelOut = labelObj.AddComponent<Outline>();
        labelOut.effectColor = new Color(0f, 0f, 0f, 0.9f);
        labelOut.effectDistance = new Vector2(1f, -1f);

        // 5. Trigger táctil con animaciones
        GameObject trigger = new GameObject("Touch_Trigger", typeof(RectTransform), typeof(Image), typeof(ModernButtonTrigger));
        trigger.transform.SetParent(btnRoot.transform, false);
        RectTransform triggerRt = trigger.GetComponent<RectTransform>();
        triggerRt.anchorMin = Vector2.zero;
        triggerRt.anchorMax = Vector2.one;
        triggerRt.offsetMin = triggerRt.offsetMax = Vector2.zero;
        Image triggerImg = trigger.GetComponent<Image>();
        triggerImg.color = new Color(0f, 0f, 0f, 0.001f); // Invisible pero intercepta toques

        ModernButtonTrigger mbt = trigger.GetComponent<ModernButtonTrigger>();
        mbt.Setup(onDown, onUp, bgImg, ColorDarkBase, new Color(neonColor.r, neonColor.g, neonColor.b, 0.45f));
    }

    // ================================================================
    //  BOTÓN DE PAUSA ESTILO CÁPSULA MODERNA (Arriba a la derecha)
    // ================================================================
    private void BuildModernPauseButton(Sprite capsuleSprite)
    {
        GameObject pauseObj = new GameObject("Btn_ModernPause", typeof(RectTransform), typeof(Image), typeof(Button));
        pauseObj.transform.SetParent(hudRoot, false);
        RectTransform rt = pauseObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot     = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-28f, -24f);
        rt.sizeDelta = new Vector2(140f, 48f);

        Image bg = pauseObj.GetComponent<Image>();
        bg.sprite = capsuleSprite;
        bg.color  = new Color(0.06f, 0.08f, 0.14f, 0.85f);

        GameObject txtObj = new GameObject("Label", typeof(RectTransform), typeof(Text));
        txtObj.transform.SetParent(pauseObj.transform, false);
        RectTransform txtRt = txtObj.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = txtRt.offsetMax = Vector2.zero;
        Text t = txtObj.GetComponent<Text>();
        t.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.text      = "❚❚  PAUSA";
        t.fontSize  = 16;
        t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter;
        t.color     = new Color(0.85f, 0.92f, 1f, 0.95f);

        Button btn = pauseObj.GetComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor      = new Color(0.06f, 0.08f, 0.14f, 0.85f);
        cb.highlightedColor = new Color(0.12f, 0.2f,  0.35f, 0.95f);
        cb.pressedColor     = new Color(0.2f,  0.35f, 0.6f,  0.95f);
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
        // 1. Detección táctil (Dispositivos móviles Android / iOS)
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

        if (dist > JoystickRadius) delta = dir * JoystickRadius;

        if (joystickThumb != null)
        {
            joystickThumb.anchoredPosition = delta;
        }

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

    private Vector2 ScreenToCanvasPos(Vector2 screenPos)
    {
        if (hudCanvas == null || hudRoot == null) return screenPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            hudRoot, screenPos, hudCanvas.worldCamera, out Vector2 localPoint);
        return localPoint;
    }

    // ================================================================
    //  GENERADORES PROCEDURALES DE SPRITES DE ALTA DEFINICIÓN
    // ================================================================
    private static Sprite MakeCircleSprite(int resolution)
    {
        Texture2D tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color[] px = new Color[resolution * resolution];
        Vector2 center = new Vector2(resolution / 2f, resolution / 2f);
        float r = resolution / 2f;
        float edge = r * 0.94f;
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
        return Sprite.Create(tex, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f), resolution);
    }

    private static Sprite MakeRingSprite(int resolution, float strokeThicknessRatio)
    {
        Texture2D tex = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color[] px = new Color[resolution * resolution];
        Vector2 center = new Vector2(resolution / 2f, resolution / 2f);
        float outerR = resolution / 2f;
        float strokePx = outerR * strokeThicknessRatio;
        float innerR = outerR - strokePx;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float outerAlpha = Mathf.Clamp01((outerR - dist) / 1.5f);
                float innerAlpha = Mathf.Clamp01((dist - innerR) / 1.5f);
                float alpha = Mathf.Min(outerAlpha, innerAlpha);
                px[y * resolution + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        tex.SetPixels(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, resolution, resolution), new Vector2(0.5f, 0.5f), resolution);
    }

    private static Sprite MakeRoundedCapsuleSprite(int width, int height, int radius)
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
//  Maneja pulsaciones instantáneas, sostener (para Correr) y micro-animación
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
        Vector3 targetScale = isDown ? new Vector3(0.88f, 0.88f, 1f) : Vector3.one;

        if (targetBg != null)
        {
            targetBg.color = isDown ? pressedColor : normalColor;
        }

        if (parentTransform != null)
        {
            float elapsed = 0f;
            Vector3 startScale = parentTransform.localScale;
            while (elapsed < 0.06f)
            {
                elapsed += Time.unscaledDeltaTime;
                parentTransform.localScale = Vector3.Lerp(startScale, targetScale, elapsed / 0.06f);
                yield return null;
            }
            parentTransform.localScale = targetScale;
        }

        animRoutine = null;
    }
}
