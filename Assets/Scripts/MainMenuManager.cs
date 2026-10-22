using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

/// <summary>
/// MainMenuManager - Menu de Inicio Arcade Premium disenado para "Fondo_Inicio_JUego".
/// Resuelve de raiz el bug de reinicio de nivel: MainMenuManager NO es DontDestroyOnLoad,
/// se autodestruye de inmediato si la escena no es Pantalla_Inicio, y utiliza tipografia
/// de alta resolucion (ArcadeBold/ArcadeRegular) con dynamicPixelsPerUnit = 2.5f y outlines
/// para garantizar letras 100% nitidas y legibles.
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [Header("Musica y Sonidos")]
    [SerializeField] private AudioClip menuMusic;
    [SerializeField] private AudioClip clickSound;

    [Header("Sprite de Fondo")]
    [SerializeField] private Sprite backgroundSprite;

    private GameObject optionsModal;
    private GameObject storyModal;

    private Text masterValueText;
    private Text musicValueText;
    private Text sfxValueText;

    private Slider masterSlider;
    private Slider musicSlider;
    private Slider sfxSlider;

    private Text pressStartText;
    private RectTransform titleTransform;

    private void Awake()
    {
        // Seguridad critica: Si esta instancia llega a una escena de juego, destruirse de inmediato
        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene != "Pantalla_Inicio" && currentScene != "Menu_Principal")
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        // 1. Asegurar AudioManager persistente en su propio GameObject independiente
        if (AudioManager.Instance == null)
        {
            GameObject audioObj = new GameObject("Audio_Manager");
            audioObj.AddComponent<AudioManager>();
        }

        // 2. Cargar assets por defecto si no estan asignados
        if (menuMusic == null)
            menuMusic = Resources.Load<AudioClip>("Sounds/Character Menu");
        if (clickSound == null)
            clickSound = Resources.Load<AudioClip>("Sounds/Menu Select");

        if (backgroundSprite == null)
            backgroundSprite = Resources.Load<Sprite>("Sprites/Fondo_Inicio_JUego");
        if (backgroundSprite == null)
            backgroundSprite = Resources.Load<Sprite>("Sprites/Menu_Background");

        // 3. Reproducir musica del menu
        if (AudioManager.Instance != null && menuMusic != null)
        {
            AudioManager.Instance.PlayBGM(menuMusic, true);
        }

        // 4. Construir interfaz visual de alta definicion
        BuildMainMenuUI();
    }

    private void Update()
    {
        // Seguridad critica adicional: Si la escena activa cambio, cesar ejecucion
        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene != "Pantalla_Inicio" && currentScene != "Menu_Principal")
        {
            Destroy(gameObject);
            return;
        }

        float unscaledTime = Time.unscaledTime;

        // 1. Pulso suave en el logo principal
        if (titleTransform != null)
        {
            float scale = 1f + Mathf.Sin(unscaledTime * 2.2f) * 0.018f;
            titleTransform.localScale = new Vector3(scale, scale, 1f);
        }

        // 2. Parpadeo arcade de "PULSA ESPACIO PARA INICIAR"
        if (pressStartText != null && (optionsModal == null || !optionsModal.activeSelf) && (storyModal == null || !storyModal.activeSelf))
        {
            float alpha = Mathf.PingPong(unscaledTime * 1.5f, 0.7f) + 0.3f;
            Color c = pressStartText.color;
            pressStartText.color = new Color(c.r, c.g, c.b, alpha);
        }

        // 3. Atajo de teclado (solo en la pantalla de inicio y si no hay modal abierto)
        if (optionsModal != null && optionsModal.activeSelf) return;
        if (storyModal != null && storyModal.activeSelf) return;

        bool startPressed = false;
        if (UnityEngine.InputSystem.Keyboard.current != null)
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            startPressed = kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame;
        }

        if (startPressed)
        {
            OnPlayClicked();
        }
    }

    // -------------------------------------------------------
    // Navegacion y Acciones de Botones
    // -------------------------------------------------------

    public void OnPlayClicked()
    {
        PlayClick();
        Time.timeScale = 1f;

        // Si existe el ComicPrologueManager, mostrar la historieta narrativa antes de entrar a la accion
        if (ComicPrologueManager.Instance != null)
        {
            ComicPrologueManager.Instance.ShowPrologue(autoLoadLevelOnFinish: true);
            return;
        }

        // Cargar Nivel 1 (Calzada de los Heroes)
        if (Application.CanStreamedLevelBeLoaded("Nivel_1_Calzada"))
        {
            SceneManager.LoadScene("Nivel_1_Calzada");
        }
        else if (Application.CanStreamedLevelBeLoaded("MainMenu"))
        {
            SceneManager.LoadScene("MainMenu");
        }
        else if (SceneManager.sceneCountInBuildSettings > 1)
        {
            SceneManager.LoadScene(1);
        }
        else
        {
            SceneManager.LoadScene("SampleScene");
        }
    }

    public void OnOptionsClicked()
    {
        PlayClick();
        if (optionsModal != null)
        {
            UpdateOptionSliders();
            optionsModal.SetActive(true);
        }
    }

    public void OnCloseOptionsClicked()
    {
        PlayClick();
        if (optionsModal != null) optionsModal.SetActive(false);
    }

    public void OnStoryClicked()
    {
        PlayClick();
        if (ComicPrologueManager.Instance != null)
        {
            ComicPrologueManager.Instance.ShowPrologue(autoLoadLevelOnFinish: false);
        }
        else if (storyModal != null)
        {
            storyModal.SetActive(true);
        }
    }

    public void OnCloseStoryClicked()
    {
        PlayClick();
        if (storyModal != null) storyModal.SetActive(false);
    }

    public void OnQuitClicked()
    {
        PlayClick();
        Debug.Log("[MainMenu] Saliendo del juego...");
        Application.Quit();
    }

    private void PlayClick()
    {
        if (AudioManager.Instance != null)
        {
            if (clickSound != null) AudioManager.Instance.PlaySFX(clickSound, 1f);
            else AudioManager.Instance.PlayClickSound();
        }
    }

    // -------------------------------------------------------
    // Sliders de Audio
    // -------------------------------------------------------

    private void UpdateOptionSliders()
    {
        if (AudioManager.Instance == null) return;

        if (masterSlider != null)
        {
            masterSlider.value = AudioManager.Instance.MasterVolume;
            if (masterValueText != null) masterValueText.text = $"{Mathf.RoundToInt(masterSlider.value * 100f)}%";
        }
        if (musicSlider != null)
        {
            musicSlider.value = AudioManager.Instance.MusicVolume;
            if (musicValueText != null) musicValueText.text = $"{Mathf.RoundToInt(musicSlider.value * 100f)}%";
        }
        if (sfxSlider != null)
        {
            sfxSlider.value = AudioManager.Instance.SFXVolume;
            if (sfxValueText != null) sfxValueText.text = $"{Mathf.RoundToInt(sfxSlider.value * 100f)}%";
        }
    }

    private void OnMasterChanged(float val)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.SetMasterVolume(val);
        if (masterValueText != null) masterValueText.text = $"{Mathf.RoundToInt(val * 100f)}%";
    }

    private void OnMusicChanged(float val)
    {
        if (AudioManager.Instance != null) AudioManager.Instance.SetMusicVolume(val);
        if (musicValueText != null) musicValueText.text = $"{Mathf.RoundToInt(val * 100f)}%";
    }

    private void OnSFXChanged(float val)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSFXVolume(val);
            if (clickSound != null) AudioManager.Instance.PlaySFX(clickSound, 0.7f);
        }
        if (sfxValueText != null) sfxValueText.text = $"{Mathf.RoundToInt(val * 100f)}%";
    }

    // -------------------------------------------------------
    // Construccion de la Interfaz Visual (Alta Definicion y Nitidez)
    // -------------------------------------------------------

    private void BuildMainMenuUI()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Menu_Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            scaler.dynamicPixelsPerUnit = 2.5f; // Genera fuentes ultra nitidas y sin desenfoque

            canvasObj.AddComponent<GraphicRaycaster>();
        }
        else
        {
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.dynamicPixelsPerUnit = 2.5f;
            }
        }

        EventSystem es = FindFirstObjectByType<EventSystem>();
        if (es == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            es = esObj.AddComponent<EventSystem>();
            esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }
        else
        {
            var oldModule = es.GetComponent<StandaloneInputModule>();
            if (oldModule != null) DestroyImmediate(oldModule);
            if (es.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() == null)
            {
                es.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }
        }

        Texture2D tex = new Texture2D(4, 4);
        Color[] px = new Color[16];
        for (int i = 0; i < px.Length; i++) px[i] = Color.white;
        tex.SetPixels(px); tex.Apply();
        Sprite white = Sprite.Create(tex, new Rect(0,0,4,4), new Vector2(0.5f,0.5f));

        // === 1. IMAGEN DE FONDO NOCTURNO LIMPIA ===
        GameObject bgObj = new GameObject("Background_Image", typeof(RectTransform), typeof(Image));
        bgObj.transform.SetParent(canvas.transform, false);
        RectTransform rtBg = bgObj.GetComponent<RectTransform>();
        rtBg.anchorMin = Vector2.zero; rtBg.anchorMax = Vector2.one;
        rtBg.offsetMin = Vector2.zero; rtBg.offsetMax = Vector2.zero;
        Image imgBg = bgObj.GetComponent<Image>();
        if (backgroundSprite != null)
        {
            imgBg.sprite = backgroundSprite;
            imgBg.color = Color.white;
            imgBg.preserveAspect = false;
        }
        else
        {
            imgBg.sprite = white;
            imgBg.color = new Color(0.04f, 0.04f, 0.08f, 1f);
        }

        // Vignette oscura suave en el lateral izquierdo para dar contraste maximo a los textos
        GameObject vignetteLeft = new GameObject("Vignette_Left", typeof(RectTransform), typeof(Image));
        vignetteLeft.transform.SetParent(canvas.transform, false);
        RectTransform rtVl = vignetteLeft.GetComponent<RectTransform>();
        rtVl.anchorMin = new Vector2(0f, 0f); rtVl.anchorMax = new Vector2(0.55f, 1f);
        rtVl.offsetMin = Vector2.zero; rtVl.offsetMax = Vector2.zero;
        Image imgVl = vignetteLeft.GetComponent<Image>();
        imgVl.sprite = white;
        imgVl.color = new Color(0.01f, 0.02f, 0.06f, 0.65f);

        // Bordes superior e inferior estilo arcade
        MakeBar(canvas.transform, white, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                Vector2.zero, new Vector2(0f, 5f), new Color(1f, 0.82f, 0.18f, 0.95f), "Border_TopGold");
        MakeBar(canvas.transform, white, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                Vector2.zero, new Vector2(0f, 4f), new Color(0.15f, 0.85f, 1f, 0.8f), "Border_BottomCyan");

        // === 2. INSIGNIA DE RECORD (Esquina Superior Derecha) ===
        int highScore = PlayerPrefs.GetInt("HighScore", 0);
        GameObject recordObj = new GameObject("Panel_Record", typeof(RectTransform), typeof(Image));
        recordObj.transform.SetParent(canvas.transform, false);
        RectTransform rtRecord = recordObj.GetComponent<RectTransform>();
        rtRecord.anchorMin = new Vector2(1f, 1f); rtRecord.anchorMax = new Vector2(1f, 1f);
        rtRecord.pivot = new Vector2(1f, 1f);
        rtRecord.anchoredPosition = new Vector2(-40f, -25f);
        rtRecord.sizeDelta = new Vector2(360f, 52f);
        Image recordBg = recordObj.GetComponent<Image>();
        recordBg.sprite = white;
        recordBg.color = new Color(0.05f, 0.08f, 0.16f, 0.94f);

        MakeBar(recordObj.transform, white, new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(0f, 0.5f), Vector2.zero, new Vector2(5f, 0f),
                new Color(1f, 0.85f, 0.2f, 1f), "RecordLeftGold");

        MakeText(recordObj.transform, $"RECORD: {highScore:N0} PTS",
                 Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                 new Vector2(15f, 0f), new Vector2(-15f, 0f),
                 20, FontStyle.Bold, TextAnchor.MiddleCenter,
                 new Color(1f, 0.9f, 0.25f, 1f), "Text_RecordVal");

        // === 3. LOGOTIPO Y TITULO ARCADE NITIDO ===
        float colX = 120f;

        GameObject titleContainer = new GameObject("Title_Container", typeof(RectTransform));
        titleContainer.transform.SetParent(canvas.transform, false);
        titleTransform = titleContainer.GetComponent<RectTransform>();
        titleTransform.anchorMin = new Vector2(0f, 1f); titleTransform.anchorMax = new Vector2(0f, 1f);
        titleTransform.pivot = new Vector2(0f, 1f);
        titleTransform.anchoredPosition = new Vector2(colX, -45f);
        titleTransform.sizeDelta = new Vector2(740f, 210f);

        // Capa 1: Sombra negra profunda
        MakeText(titleContainer.transform, "DOLONDRAZO",
                 new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                 new Vector2(6f, -6f), new Vector2(760f, 120f),
                 88, FontStyle.Bold, TextAnchor.MiddleLeft,
                 new Color(0.01f, 0.01f, 0.02f, 1f), "Title_Shadow", false);

        // Capa 2: Extrusion naranja fuego
        MakeText(titleContainer.transform, "DOLONDRAZO",
                 new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                 new Vector2(3f, -3f), new Vector2(760f, 120f),
                 88, FontStyle.Bold, TextAnchor.MiddleLeft,
                 new Color(0.95f, 0.38f, 0.08f, 1f), "Title_Extrusion", false);

        // Capa 3: Oro brillante frontal
        MakeText(titleContainer.transform, "DOLONDRAZO",
                 new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                 Vector2.zero, new Vector2(760f, 120f),
                 88, FontStyle.Bold, TextAnchor.MiddleLeft,
                 new Color(1f, 0.88f, 0.22f, 1f), "Title_Main", true);

        // Placa del subtitulo (Nitida con borde cian brillante)
        GameObject badgeObj = new GameObject("Subtitle_Badge", typeof(RectTransform), typeof(Image));
        badgeObj.transform.SetParent(titleContainer.transform, false);
        RectTransform rtBadge = badgeObj.GetComponent<RectTransform>();
        rtBadge.anchorMin = new Vector2(0f, 1f); rtBadge.anchorMax = new Vector2(0f, 1f);
        rtBadge.pivot = new Vector2(0f, 1f);
        rtBadge.anchoredPosition = new Vector2(0f, -114f);
        rtBadge.sizeDelta = new Vector2(640f, 44f);
        Image badgeBg = badgeObj.GetComponent<Image>();
        badgeBg.sprite = white;
        badgeBg.color = new Color(0.04f, 0.08f, 0.18f, 0.95f);

        MakeBar(badgeObj.transform, white, new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(0f, 0.5f), Vector2.zero, new Vector2(5f, 0f),
                new Color(0.0f, 0.9f, 1f, 1f), "Badge_Border");

        MakeText(badgeObj.transform, "* LA ENTREGA FINAL - CBTis 75 DOLORES HIDALGO *",
                 Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                 new Vector2(18f, 0f), new Vector2(-15f, 0f),
                 20, FontStyle.Bold, TextAnchor.MiddleLeft,
                 new Color(0.2f, 0.92f, 1f, 1f), "Text_Badge");

        // Prompt arcade con brillo dorado
        pressStartText = MakeText(titleContainer.transform, ">> PULSA [ESPACIO] O HAZ CLICK PARA INICIAR <<",
                 new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                 new Vector2(6f, -168f), new Vector2(640f, 30f),
                 18, FontStyle.Bold, TextAnchor.MiddleLeft,
                 new Color(1f, 0.92f, 0.45f, 1f), "Text_PressStart");

        // === 4. BOTONES DEL MENU (Distribuidos, Nitidos y con Micro-interaccion) ===
        float startY = -285f;
        float spacingY = 86f;
        float btnWidth = 480f;
        float btnHeight = 70f;

        // 1. Iniciar Aventura
        MakeStyledMenuButton(canvas.transform, white, new Vector2(colX, startY), new Vector2(btnWidth, btnHeight),
                             "INICIAR AVENTURA", ">",
                             new Color(0.06f, 0.35f, 0.18f, 0.95f),
                             new Color(0.12f, 0.65f, 0.35f, 1f),
                             new Color(0.1f, 1f, 0.5f, 1f),
                             "Btn_Play", OnPlayClicked);

        // 2. Opciones de Audio
        MakeStyledMenuButton(canvas.transform, white, new Vector2(colX, startY - spacingY), new Vector2(btnWidth, btnHeight),
                             "AJUSTES DE AUDIO", "=",
                             new Color(0.06f, 0.24f, 0.48f, 0.95f),
                             new Color(0.14f, 0.45f, 0.85f, 1f),
                             new Color(0.25f, 0.82f, 1f, 1f),
                             "Btn_Options", OnOptionsClicked);

        // 3. Historia y Controles
        MakeStyledMenuButton(canvas.transform, white, new Vector2(colX, startY - spacingY * 2), new Vector2(btnWidth, btnHeight),
                             "MISION Y CONTROLES", "?",
                             new Color(0.28f, 0.12f, 0.48f, 0.95f),
                             new Color(0.50f, 0.22f, 0.80f, 1f),
                             new Color(0.85f, 0.45f, 1f, 1f),
                             "Btn_Story", OnStoryClicked);

        // 4. Salir
        MakeStyledMenuButton(canvas.transform, white, new Vector2(colX, startY - spacingY * 3), new Vector2(btnWidth, btnHeight),
                             "SALIR DEL JUEGO", "X",
                             new Color(0.38f, 0.08f, 0.12f, 0.95f),
                             new Color(0.65f, 0.16f, 0.22f, 1f),
                             new Color(1f, 0.25f, 0.32f, 1f),
                             "Btn_Quit", OnQuitClicked);

        // === 5. EXPEDIENTE DE MISION (Tipografia Clara, Alto Contraste y Nitidez) ===
        GameObject missionCard = new GameObject("Mission_Info_Card", typeof(RectTransform), typeof(Image));
        missionCard.transform.SetParent(canvas.transform, false);
        RectTransform rtMc = missionCard.GetComponent<RectTransform>();
        rtMc.anchorMin = new Vector2(0f, 1f); rtMc.anchorMax = new Vector2(0f, 1f);
        rtMc.pivot = new Vector2(0f, 1f);
        rtMc.anchoredPosition = new Vector2(colX, startY - spacingY * 4 - 8f);
        rtMc.sizeDelta = new Vector2(btnWidth + 40f, 145f);
        Image mcBg = missionCard.GetComponent<Image>();
        mcBg.sprite = white;
        mcBg.color = new Color(0.03f, 0.05f, 0.12f, 0.94f);

        MakeBar(missionCard.transform, white, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 3f),
                new Color(1f, 0.85f, 0.2f, 0.95f), "MissionTopGold");

        MakeBar(missionCard.transform, white, new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(0f, 0.5f), Vector2.zero, new Vector2(4f, 0f),
                new Color(0.2f, 0.85f, 1f, 0.95f), "MissionLeftCyan");

        // Cabecera del expediente
        MakeText(missionCard.transform, "EXPEDIENTE DE MISION:",
                 new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
                 new Vector2(18f, -12f), new Vector2(-36f, 26f),
                 18, FontStyle.Bold, TextAnchor.MiddleLeft,
                 new Color(1f, 0.88f, 0.2f, 1f), "Text_MissionTitle");

        string missionText = "• DESTINO: CBTis 75 (Entrega limite antes de las 2:00 PM)\n" +
                             "• ESTADO: Sin transporte escolar  |  Bateria de celular: 0%\n" +
                             "• ALERTA: Calles tomadas por estudiantes rivales del CECyTE";

        MakeText(missionCard.transform, missionText,
                 new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0.5f, 0.5f),
                 new Vector2(18f, 8f), new Vector2(-36f, -40f),
                 16, FontStyle.Bold, TextAnchor.MiddleLeft,
                 new Color(0.92f, 0.95f, 1f, 1f), "Text_MissionDetails");

        // Pie de pagina
        MakeText(canvas.transform, "Dolondrazo v1.0 - CBTis 75 Dolores Hidalgo, Gto.",
                 new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                 new Vector2(0f, 16f), new Vector2(0f, 24f),
                 14, FontStyle.Italic, TextAnchor.MiddleCenter,
                 new Color(0.7f, 0.75f, 0.88f, 0.7f), "Text_Footer");

        // === 6. MODALES REDISENADOS ===
        BuildOptionsModal(canvas.transform, white);
        BuildStoryModal(canvas.transform, white);
    }

    private void MakeStyledMenuButton(Transform parent, Sprite white, Vector2 aPos, Vector2 size,
                                      string label, string icon,
                                      Color normalColor, Color hoverColor, Color neonAccentColor,
                                      string name, UnityEngine.Events.UnityAction action)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(MenuButtonHover));
        btnObj.transform.SetParent(parent, false);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = aPos; rt.sizeDelta = size;

        Image bg = btnObj.GetComponent<Image>();
        bg.sprite = white; bg.color = normalColor;

        Button btn = btnObj.GetComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor      = normalColor;
        cb.highlightedColor = hoverColor;
        cb.pressedColor     = new Color(normalColor.r * 0.7f, normalColor.g * 0.7f, normalColor.b * 0.7f, 1f);
        btn.colors = cb;
        btn.onClick.AddListener(action);

        MakeBar(btnObj.transform, white, new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(0f, 0.5f), Vector2.zero, new Vector2(6f, 0f),
                neonAccentColor, "NeonSide");

        MakeBar(btnObj.transform, white, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 3f),
                new Color(1f, 1f, 1f, 0.3f), "TopShine");

        GameObject iconObj = new GameObject("Icon_Badge", typeof(RectTransform), typeof(Text));
        iconObj.transform.SetParent(btnObj.transform, false);
        RectTransform rtIcon = iconObj.GetComponent<RectTransform>();
        rtIcon.anchorMin = new Vector2(0f, 0.5f); rtIcon.anchorMax = new Vector2(0f, 0.5f);
        rtIcon.pivot = new Vector2(0f, 0.5f);
        rtIcon.anchoredPosition = new Vector2(20f, 0f);
        rtIcon.sizeDelta = new Vector2(36f, 36f);
        Text iconText = iconObj.GetComponent<Text>();
        iconText.font = Resources.Load<Font>("Fonts/ArcadeBold") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        iconText.text = icon;
        iconText.fontSize = 24;
        iconText.fontStyle = FontStyle.Bold;
        iconText.alignment = TextAnchor.MiddleCenter;
        iconText.color = neonAccentColor;
        Outline iconOut = iconObj.AddComponent<Outline>();
        iconOut.effectColor = Color.black;
        iconOut.effectDistance = new Vector2(1.5f, -1.5f);

        MakeText(btnObj.transform, label,
                 Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                 new Vector2(68f, 0f), new Vector2(-20f, 0f),
                 22, FontStyle.Bold, TextAnchor.MiddleLeft,
                 Color.white, "Btn_Label");

        MenuButtonHover hover = btnObj.GetComponent<MenuButtonHover>();
        if (hover != null) hover.Init(clickSound);
    }

    private void BuildOptionsModal(Transform parent, Sprite white)
    {
        optionsModal = new GameObject("Modal_Options", typeof(RectTransform), typeof(Image));
        optionsModal.transform.SetParent(parent, false);
        RectTransform rt = optionsModal.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero; rt.sizeDelta = new Vector2(820f, 560f);

        Image bg = optionsModal.GetComponent<Image>();
        bg.sprite = white; bg.color = new Color(0.04f, 0.05f, 0.12f, 0.98f);

        MakeBar(optionsModal.transform, white, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 6f),
                new Color(0.2f, 0.85f, 1f, 1f), "ModalTopCyan");

        MakeText(optionsModal.transform, "AJUSTES DE AUDIO Y PANTALLA",
                 new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                 new Vector2(0f, -32f), new Vector2(0f, 40f),
                 26, FontStyle.Bold, TextAnchor.MiddleCenter,
                 new Color(1f, 0.88f, 0.2f, 1f), "Modal_Title");

        float sY = -120f;
        float sGap = 88f;

        CreateOptionSlider(optionsModal.transform, white, "VOLUMEN GENERAL", sY,
                           AudioManager.Instance != null ? AudioManager.Instance.MasterVolume : 1f,
                           out masterSlider, out masterValueText, OnMasterChanged);

        CreateOptionSlider(optionsModal.transform, white, "MUSICA DE FONDO", sY - sGap,
                           AudioManager.Instance != null ? AudioManager.Instance.MusicVolume : 0.8f,
                           out musicSlider, out musicValueText, OnMusicChanged);

        CreateOptionSlider(optionsModal.transform, white, "EFECTOS DE SONIDO", sY - sGap * 2,
                           AudioManager.Instance != null ? AudioManager.Instance.SFXVolume : 0.8f,
                           out sfxSlider, out sfxValueText, OnSFXChanged);

        // Toggle Pantalla Completa
        GameObject fsObj = new GameObject("Fullscreen_Row", typeof(RectTransform));
        fsObj.transform.SetParent(optionsModal.transform, false);
        RectTransform rtFs = fsObj.GetComponent<RectTransform>();
        rtFs.anchorMin = new Vector2(0.5f, 1f); rtFs.anchorMax = new Vector2(0.5f, 1f);
        rtFs.pivot = new Vector2(0.5f, 1f);
        rtFs.anchoredPosition = new Vector2(0f, sY - sGap * 3);
        rtFs.sizeDelta = new Vector2(700f, 50f);

        MakeText(fsObj.transform, "PANTALLA COMPLETA",
                 new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                 new Vector2(0f, 0f), new Vector2(300f, 40f),
                 20, FontStyle.Bold, TextAnchor.MiddleLeft,
                 Color.white, "Label_FS");

        MakeArcadeSimpleButton(fsObj.transform, white, new Vector2(250f, 0f), new Vector2(180f, 44f),
                               Screen.fullScreen ? "[ ACTIVADO ]" : "[ DESACTIVADO ]",
                               new Color(0.15f, 0.45f, 0.7f, 1f), new Color(0.25f, 0.65f, 0.95f, 1f),
                               "Btn_ToggleFS", () =>
                               {
                                   Screen.fullScreen = !Screen.fullScreen;
                                   PlayClick();
                               });

        MakeArcadeSimpleButton(optionsModal.transform, white, new Vector2(0f, -490f), new Vector2(260f, 56f),
                               "<< GUARDAR Y SALIR",
                               new Color(0.12f, 0.5f, 0.25f, 1f),
                               new Color(0.2f, 0.75f, 0.38f, 1f),
                               "Btn_CloseOptions", OnCloseOptionsClicked);

        optionsModal.SetActive(false);
    }

    private void BuildStoryModal(Transform parent, Sprite white)
    {
        storyModal = new GameObject("Modal_Story", typeof(RectTransform), typeof(Image));
        storyModal.transform.SetParent(parent, false);
        RectTransform rt = storyModal.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero; rt.sizeDelta = new Vector2(900f, 620f);

        Image bg = storyModal.GetComponent<Image>();
        bg.sprite = white; bg.color = new Color(0.04f, 0.05f, 0.12f, 0.98f);

        MakeBar(storyModal.transform, white, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 6f),
                new Color(0.95f, 0.45f, 1f, 1f), "ModalTopPurple");

        MakeText(storyModal.transform, "M I S I O N   Y   C O N T R O L E S",
                 new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                 new Vector2(0f, -32f), new Vector2(0f, 40f),
                 26, FontStyle.Bold, TextAnchor.MiddleCenter,
                 new Color(1f, 0.88f, 0.2f, 1f), "Story_Title");

        string synopsis = "Eres Fercho, estudiante del CBTis 75 en Dolores Hidalgo, Gto. Hoy es el ultimo dia " +
                          "para entregar tu proyecto escolar final antes de las 2:00 PM. El transporte publico " +
                          "no llego, tu celular se quedo sin bateria y las calles han sido bloqueadas por bandas " +
                          "rivales del CECyTE. Tu unica opcion: ¡cruzar la ciudad a pie y abrirte paso a golpes!";

        MakeText(storyModal.transform, synopsis,
                 new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                 new Vector2(0f, -90f), new Vector2(-90f, 105f),
                 18, FontStyle.Normal, TextAnchor.UpperLeft,
                 new Color(0.92f, 0.95f, 1f, 1f), "Story_Synopsis");

        // Tarjeta de Controles
        GameObject controlsCard = new GameObject("Controls_Card", typeof(RectTransform), typeof(Image));
        controlsCard.transform.SetParent(storyModal.transform, false);
        RectTransform rtCc = controlsCard.GetComponent<RectTransform>();
        rtCc.anchorMin = new Vector2(0.5f, 0.5f); rtCc.anchorMax = new Vector2(0.5f, 0.5f);
        rtCc.pivot = new Vector2(0.5f, 0.5f);
        rtCc.anchoredPosition = new Vector2(0f, -70f);
        rtCc.sizeDelta = new Vector2(810f, 210f);
        Image ccBg = controlsCard.GetComponent<Image>();
        ccBg.sprite = white; ccBg.color = new Color(0.08f, 0.1f, 0.18f, 0.95f);

        MakeBar(controlsCard.transform, white, new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(0f, 0.5f), Vector2.zero, new Vector2(5f, 0f),
                new Color(0.35f, 0.88f, 1f, 1f), "ControlsLeftCyan");

        string controlsText = "MANDOS DEL JUGADOR (TECLADO):\n\n" +
                              "• [A] / [D] o [Flechas]     : Mover a Fercho por la calle\n" +
                              "• [Z] o [J] o [Click Izq]   : Combo de Golpes (Punch & Kick)\n" +
                              "• [Espacio]                 : Saltar / Esquivar obstaculos\n" +
                              "• [Esc] o [P]               : Pausar partida y Menu rapido\n" +
                              "• Rompe las CAJAS de madera con golpes para conseguir comida y recargar salud.";

        MakeText(controlsCard.transform, controlsText,
                 Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                 new Vector2(25f, 0f), new Vector2(-25f, 0f),
                 18, FontStyle.Bold, TextAnchor.MiddleLeft,
                 new Color(0.85f, 0.95f, 1f, 1f), "Controls_Details");

        MakeArcadeSimpleButton(storyModal.transform, white, new Vector2(0f, -540f), new Vector2(260f, 56f),
                               "<< ENTENDIDO",
                               new Color(0.35f, 0.15f, 0.55f, 1f),
                               new Color(0.55f, 0.25f, 0.85f, 1f),
                               "Btn_CloseStory", OnCloseStoryClicked);

        storyModal.SetActive(false);
    }

    private void CreateOptionSlider(Transform parent, Sprite white, string label, float yPos,
                                    float initialVal, out Slider slider, out Text valText,
                                    UnityEngine.Events.UnityAction<float> onChanged)
    {
        GameObject row = new GameObject("SliderRow_" + label, typeof(RectTransform));
        row.transform.SetParent(parent, false);
        RectTransform rtRow = row.GetComponent<RectTransform>();
        rtRow.anchorMin = new Vector2(0.5f, 1f); rtRow.anchorMax = new Vector2(0.5f, 1f);
        rtRow.pivot = new Vector2(0.5f, 1f);
        rtRow.anchoredPosition = new Vector2(0f, yPos);
        rtRow.sizeDelta = new Vector2(700f, 50f);

        MakeText(row.transform, label,
                 new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                 new Vector2(0f, 0f), new Vector2(240f, 40f),
                 20, FontStyle.Bold, TextAnchor.MiddleLeft,
                 Color.white, "Label");

        // Slider GameObject
        GameObject sliderObj = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
        sliderObj.transform.SetParent(row.transform, false);
        RectTransform rtSlider = sliderObj.GetComponent<RectTransform>();
        rtSlider.anchorMin = new Vector2(0f, 0.5f); rtSlider.anchorMax = new Vector2(0f, 0.5f);
        rtSlider.pivot = new Vector2(0f, 0.5f);
        rtSlider.anchoredPosition = new Vector2(250f, 0f);
        rtSlider.sizeDelta = new Vector2(340f, 24f);

        slider = sliderObj.GetComponent<Slider>();
        slider.minValue = 0f; slider.maxValue = 1f; slider.value = initialVal;

        // Background
        GameObject sBg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        sBg.transform.SetParent(sliderObj.transform, false);
        RectTransform rtSbg = sBg.GetComponent<RectTransform>();
        rtSbg.anchorMin = Vector2.zero; rtSbg.anchorMax = Vector2.one; rtSbg.sizeDelta = Vector2.zero;
        Image imgSbg = sBg.GetComponent<Image>();
        imgSbg.sprite = white; imgSbg.color = new Color(0.12f, 0.16f, 0.28f, 1f);

        // Fill Area
        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform rtFa = fillArea.GetComponent<RectTransform>();
        rtFa.anchorMin = Vector2.zero; rtFa.anchorMax = Vector2.one; rtFa.sizeDelta = Vector2.zero;

        GameObject fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform rtFill = fill.GetComponent<RectTransform>();
        rtFill.sizeDelta = Vector2.zero;
        Image imgFill = fill.GetComponent<Image>();
        imgFill.sprite = white; imgFill.color = new Color(0.15f, 0.85f, 1f, 1f);

        slider.fillRect = rtFill;

        slider.onValueChanged.AddListener(onChanged);

        valText = MakeText(row.transform, $"{Mathf.RoundToInt(initialVal * 100f)}%",
                           new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                           Vector2.zero, new Vector2(80f, 40f),
                           20, FontStyle.Bold, TextAnchor.MiddleRight,
                           new Color(1f, 0.88f, 0.2f, 1f), "Value");
    }

    // -------------------------------------------------------
    // Helpers de Creacion con Tipografia Nitida
    // -------------------------------------------------------

    private void MakeBar(Transform parent, Sprite white, Vector2 ancMin, Vector2 ancMax,
                         Vector2 pivot, Vector2 aPos, Vector2 sizeDelta, Color color, string name)
    {
        GameObject bar = new GameObject(name, typeof(RectTransform), typeof(Image));
        bar.transform.SetParent(parent, false);
        RectTransform rt = bar.GetComponent<RectTransform>();
        rt.anchorMin = ancMin; rt.anchorMax = ancMax; rt.pivot = pivot;
        rt.anchoredPosition = aPos; rt.sizeDelta = sizeDelta;
        Image img = bar.GetComponent<Image>();
        img.sprite = white; img.color = color;
    }

    private Text MakeText(Transform parent, string content,
                          Vector2 ancMin, Vector2 ancMax, Vector2 pivot,
                          Vector2 aPos, Vector2 sizeDelta,
                          int fontSize, FontStyle style, TextAnchor align,
                          Color color, string name, bool addOutline = true)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Text));
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = ancMin; rt.anchorMax = ancMax; rt.pivot = pivot;
        rt.anchoredPosition = aPos; rt.sizeDelta = sizeDelta;

        Text t = obj.GetComponent<Text>();
        // Intentar cargar la tipografia ArcadeBold de alta definicion
        Font arcadeFont = Resources.Load<Font>("Fonts/ArcadeBold");
        if (arcadeFont == null) arcadeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.font = arcadeFont;
        t.text = content;
        t.fontSize = fontSize;
        t.fontStyle = style;
        t.alignment = align;
        t.color = color;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;

        if (addOutline)
        {
            Outline outline = obj.AddComponent<Outline>();
            outline.effectColor = new Color(0.01f, 0.02f, 0.05f, 0.95f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
        }

        return t;
    }

    private void MakeArcadeSimpleButton(Transform parent, Sprite white, Vector2 aPos, Vector2 size,
                                        string label, Color normalColor, Color hoverColor,
                                        string name, UnityEngine.Events.UnityAction action)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = aPos; rt.sizeDelta = size;

        Image bg = btnObj.GetComponent<Image>();
        bg.sprite = white; bg.color = normalColor;

        Button btn = btnObj.GetComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor      = normalColor;
        cb.highlightedColor = hoverColor;
        cb.pressedColor     = new Color(normalColor.r * 0.7f, normalColor.g * 0.7f, normalColor.b * 0.7f, 1f);
        btn.colors = cb;
        btn.onClick.AddListener(action);

        MakeBar(btnObj.transform, white, new Vector2(0f,1f), new Vector2(1f,1f),
                new Vector2(0.5f,1f), Vector2.zero, new Vector2(0f,3f),
                new Color(1f,1f,1f,0.3f), "TopShine");

        MakeText(btnObj.transform, label,
                 Vector2.zero, Vector2.one, new Vector2(0.5f,0.5f),
                 Vector2.zero, Vector2.zero,
                 20, FontStyle.Bold, TextAnchor.MiddleCenter,
                 Color.white, "Btn_Label");
    }

    // Hook permanente: Se ejecuta CADA VEZ que se carga cualquier escena en Unity
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneLoadHook()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Pantalla_Inicio" || scene.name == "Menu_Principal")
        {
            Time.timeScale = 1f;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;

            // 1. Asegurar AudioManager persistente
            if (AudioManager.Instance == null)
            {
                GameObject audioGo = new GameObject("Audio_Manager");
                audioGo.AddComponent<AudioManager>();
            }

            // 2. Crear MainMenu_Controller y ComicPrologueManager en la escena
            if (FindFirstObjectByType<MainMenuManager>() == null)
            {
                GameObject menuGo = new GameObject("MainMenu_Controller");
                menuGo.AddComponent<MainMenuManager>();
                menuGo.AddComponent<ComicPrologueManager>();
            }
        }
    }
}

/// <summary>
/// Helper de micro-animacion de escala y sonido hover para los botones del menu.
/// </summary>
public class MenuButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Vector3 originalScale;
    private RectTransform rt;
    private AudioClip hoverSound;

    public void Init(AudioClip sound)
    {
        hoverSound = sound;
        rt = GetComponent<RectTransform>();
        if (rt != null) originalScale = rt.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (rt != null) rt.localScale = originalScale * 1.04f;
        if (AudioManager.Instance != null && hoverSound != null)
        {
            AudioManager.Instance.PlaySFX(hoverSound, 0.5f);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (rt != null) rt.localScale = originalScale;
    }

    private void OnDisable()
    {
        if (rt != null) rt.localScale = originalScale;
    }
}
