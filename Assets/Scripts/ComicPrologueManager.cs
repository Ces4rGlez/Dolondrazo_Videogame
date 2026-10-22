using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// ComicPrologueManager - Historieta interactiva de 3 vinetas + Pantalla de Controles.
/// Resuelve de raiz el texto amontonado (Wrap activado con espaciado amplio),
/// elimina las vinetas 4 y 5 innecesarias y anade pantalla de carga fluida para que
/// NUNCA se muestre el menu principal durante la carga del nivel 1.
/// </summary>
public class ComicPrologueManager : MonoBehaviour
{
    public static ComicPrologueManager Instance { get; private set; }

    [System.Serializable]
    public class ComicPanelData
    {
        public string chapterTag;
        public string title;
        public string subtitle;
        public string speakerName;
        public Color speakerColor;
        [TextArea(2, 5)]
        public string narrativeText;
        public string spriteResourcePath;
        public string fallbackIconText;
        public Color accentColor;
    }

    [Header("Musica y Efectos")]
    [SerializeField] private AudioClip prologueMusic;
    [SerializeField] private AudioClip pageTurnSound;

    // Componentes de la UI
    private GameObject comicOverlay;
    private Image panelImage;
    private Text panelPlaceholderIcon;
    private Text chapterBadgeText;
    private Text panelTitleText;
    private Text panelSubtitleText;
    private Text speakerBadgeText;
    private Image speakerBadgeBg;
    private Text narrativeBodyText;
    private GameObject controlsCardObj;
    private GameObject normalContentObj;
    private GameObject loadingScreenObj;

    // Navegacion
    private Button btnPrevious;
    private Button btnNext;
    private Text btnNextText;
    private Button btnSkip;
    private Text pageIndicatorText;

    private int currentPanelIndex = 0;
    private bool autoLoadOnFinish = false;
    private bool isLoadingScene = false;
    private System.Action onCompleteCallback;
    private List<ComicPanelData> panels = new List<ComicPanelData>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (prologueMusic == null)
            prologueMusic = Resources.Load<AudioClip>("Sounds/Intro");
        if (pageTurnSound == null)
            pageTurnSound = Resources.Load<AudioClip>("Sounds/Menu Select");

        SetupPanelsData();
    }

    private void Start()
    {
        BuildComicUI();
    }

    private void SetupPanelsData()
    {
        // UNICAMENTE 3 VINETAS + CONTROLES (Las vinetas 4 y 5 se narran en los dialogos de Nivel 1)
        panels = new List<ComicPanelData>
        {
            // Vineta 1: La Noche Anterior (3:00 AM)
            new ComicPanelData
            {
                chapterTag = "VINETA 1 DE 3 • PROLOGO",
                title = "LA NOCHE ANTERIOR (3:00 AM)",
                subtitle = "Habitacion de Fercho — Dolores Hidalgo, Gto.",
                speakerName = "FERCHO",
                speakerColor = new Color(0.25f, 0.88f, 1f, 1f),
                narrativeText = "«Uff... 3:00 de la manana. Tres dias sin dormir pero por fin termine la maqueta del proyecto final de mecatronica. Si no la entrego hoy en el CBTis 75 antes de las 2:00 PM, repruebo el semestre entero...»",
                spriteResourcePath = "Sprites/Comic/Vineta_1",
                fallbackIconText = "[ 3:00 AM • MAQUETA FINAL CBTis 75 ]",
                accentColor = new Color(1f, 0.85f, 0.2f, 1f)
            },

            // Vineta 2: La Manana Siguiente (1:15 PM)
            new ComicPanelData
            {
                chapterTag = "VINETA 2 DE 3 • PROLOGO",
                title = "LA MANANA SIGUIENTE (1:15 PM)",
                subtitle = "Parada de Autobuses — Calzada de los Heroes",
                speakerName = "FERCHO",
                speakerColor = new Color(0.25f, 0.88f, 1f, 1f),
                narrativeText = "«¡No puede ser! Llego corriendo a la parada de la Calzada y no hay ni un alma... ¡Las rutas de camiones se fueron a paro sorpresa! No hay un solo transporte publico en toda la avenida...»",
                spriteResourcePath = "Sprites/Comic/Vineta_2",
                fallbackIconText = "[ PARADA VACIA • SIN TRANSPORTE ]",
                accentColor = new Color(1f, 0.75f, 0.1f, 1f)
            },

            // Vineta 3: La Mala Suerte (1:20 PM)
            new ComicPanelData
            {
                chapterTag = "VINETA 3 DE 3 • PROLOGO",
                title = "LA MALA SUERTE (1:20 PM)",
                subtitle = "Pantalla del Celular — Bateria Agotada",
                speakerName = "FERCHO",
                speakerColor = new Color(0.25f, 0.88f, 1f, 1f),
                narrativeText = "«¡Maldicion! Saco mi telefono para pedir un aventon o avisarle al profe de taller y... ¡pantalla negra con el icono de 0% de bateria! Se me apago en el peor momento. Estoy completamente incomunicado.»",
                spriteResourcePath = "Sprites/Comic/Vineta_3",
                fallbackIconText = "[ BATERIA 0% • CELULAR APAGADO ]",
                accentColor = new Color(1f, 0.25f, 0.25f, 1f)
            },

            // Panel 4: Manual de Controles y Mision
            new ComicPanelData
            {
                chapterTag = "GUIA DE SUPERVIVENCIA • COMBATE",
                title = "MANUAL DE COMBATE Y CONTROLES",
                subtitle = "Todo lo necesario antes de entrar a la Calzada de los Heroes",
                speakerName = "SISTEMA",
                speakerColor = new Color(0.2f, 0.9f, 1f, 1f),
                narrativeText = "Memoriza los controles para salvar el semestre.",
                spriteResourcePath = "",
                fallbackIconText = "[ CONTROLES Y COMBATE ]",
                accentColor = new Color(0.2f, 0.9f, 1f, 1f)
            }
        };
    }

    private void Update()
    {
        if (comicOverlay == null || !comicOverlay.activeSelf || isLoadingScene) return;

        bool nextKey = false;
        bool prevKey = false;
        bool skipKey = false;

        if (UnityEngine.InputSystem.Keyboard.current != null)
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            nextKey = kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame;
            prevKey = kb.leftArrowKey.wasPressedThisFrame;
            skipKey = kb.escapeKey.wasPressedThisFrame;
        }

        if (skipKey)
        {
            SkipOrFinish();
        }
        else if (nextKey)
        {
            OnNextClicked();
        }
        else if (prevKey)
        {
            OnPreviousClicked();
        }
    }

    // -------------------------------------------------------
    // API Publica
    // -------------------------------------------------------

    public void ShowPrologue(bool autoLoadLevelOnFinish = false, System.Action onComplete = null)
    {
        autoLoadOnFinish = autoLoadLevelOnFinish;
        onCompleteCallback = onComplete;
        isLoadingScene = false;

        if (comicOverlay == null)
        {
            BuildComicUI();
        }

        if (comicOverlay != null)
        {
            if (loadingScreenObj != null) loadingScreenObj.SetActive(false);
            comicOverlay.SetActive(true);
            currentPanelIndex = 0;
            DisplayCurrentPanel();

            if (AudioManager.Instance != null && prologueMusic != null)
            {
                AudioManager.Instance.PlayBGM(prologueMusic, true);
            }
        }
    }

    public void HidePrologue()
    {
        if (comicOverlay != null)
        {
            comicOverlay.SetActive(false);
        }
    }

    // -------------------------------------------------------
    // Navegacion
    // -------------------------------------------------------

    public void OnNextClicked()
    {
        if (isLoadingScene) return;
        PlayPageTurn();

        if (currentPanelIndex < panels.Count - 1)
        {
            currentPanelIndex++;
            DisplayCurrentPanel();
        }
        else
        {
            SkipOrFinish();
        }
    }

    public void OnPreviousClicked()
    {
        if (isLoadingScene) return;
        PlayPageTurn();

        if (currentPanelIndex > 0)
        {
            currentPanelIndex--;
            DisplayCurrentPanel();
        }
    }

    public void SkipOrFinish()
    {
        if (isLoadingScene) return;
        PlayPageTurn();

        if (autoLoadOnFinish)
        {
            // Transicion fluida: Mantener pantalla oscura de carga para NUNCA mostrar el menu principal
            isLoadingScene = true;
            if (loadingScreenObj != null) loadingScreenObj.SetActive(true);

            StartCoroutine(LoadLevelTransitionRoutine());
        }
        else
        {
            HidePrologue();

            AudioClip menuMusic = Resources.Load<AudioClip>("Sounds/Character Menu");
            if (AudioManager.Instance != null && menuMusic != null)
            {
                AudioManager.Instance.PlayBGM(menuMusic, true);
            }

            onCompleteCallback?.Invoke();
        }
    }

    private IEnumerator LoadLevelTransitionRoutine()
    {
        // Breve espera para que la pantalla de carga se dibuje
        yield return null;

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
    }

    private void PlayPageTurn()
    {
        if (AudioManager.Instance != null)
        {
            if (pageTurnSound != null) AudioManager.Instance.PlaySFX(pageTurnSound, 0.8f);
            else AudioManager.Instance.PlayClickSound();
        }
    }

    // -------------------------------------------------------
    // Actualizacion de Pantalla
    // -------------------------------------------------------

    private void DisplayCurrentPanel()
    {
        if (currentPanelIndex < 0 || currentPanelIndex >= panels.Count) return;

        ComicPanelData data = panels[currentPanelIndex];

        // 1. Cabecera
        if (chapterBadgeText != null) chapterBadgeText.text = data.chapterTag;
        if (panelTitleText != null)
        {
            panelTitleText.text = data.title;
            panelTitleText.color = data.accentColor;
        }
        if (panelSubtitleText != null) panelSubtitleText.text = data.subtitle;

        // 2. Indicador de paginas: [ 1 ]  2  3  CONTROLES
        if (pageIndicatorText != null)
        {
            string ind = "";
            for (int i = 0; i < panels.Count; i++)
            {
                string tag = (i == panels.Count - 1) ? "CONTROLES" : $"{i + 1}";
                if (i == currentPanelIndex) ind += $"<color=#FFE000>[ {tag} ]</color>   ";
                else ind += $"<color=#667799>{tag}</color>   ";
            }
            pageIndicatorText.text = ind.TrimEnd();
        }

        // 3. Activar/Desactivar Panel Normal vs Panel de Controles
        bool isControlsPanel = (currentPanelIndex == panels.Count - 1);
        if (controlsCardObj != null) controlsCardObj.SetActive(isControlsPanel);
        if (normalContentObj != null) normalContentObj.SetActive(!isControlsPanel);

        if (!isControlsPanel)
        {
            if (speakerBadgeText != null)
            {
                speakerBadgeText.text = data.speakerName;
                speakerBadgeText.color = data.speakerColor;
            }
            if (speakerBadgeBg != null)
            {
                speakerBadgeBg.color = new Color(data.speakerColor.r * 0.2f, data.speakerColor.g * 0.2f, data.speakerColor.b * 0.2f, 0.95f);
            }
            if (narrativeBodyText != null)
            {
                narrativeBodyText.text = data.narrativeText;
            }

            // Imagen / Sprite de la Vineta
            Sprite imgSprite = LoadComicSprite(data.spriteResourcePath);

            if (panelImage != null)
            {
                if (imgSprite != null)
                {
                    panelImage.sprite = imgSprite;
                    panelImage.color = Color.white;
                    if (panelPlaceholderIcon != null) panelPlaceholderIcon.gameObject.SetActive(false);
                }
                else
                {
                    panelImage.sprite = null;
                    panelImage.color = new Color(0.05f, 0.07f, 0.14f, 1f);
                    if (panelPlaceholderIcon != null)
                    {
                        panelPlaceholderIcon.gameObject.SetActive(true);
                        panelPlaceholderIcon.text = data.fallbackIconText;
                        panelPlaceholderIcon.color = data.accentColor;
                    }
                }
            }
        }

        // 4. Botones
        if (btnPrevious != null)
        {
            btnPrevious.gameObject.SetActive(currentPanelIndex > 0);
        }
        if (btnNextText != null)
        {
            btnNextText.text = isControlsPanel ? "¡ENTRAR A LA CALZADA! >>" : "SIGUIENTE >>";
        }
    }

    private Sprite LoadComicSprite(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;

        // 1. Carga directa como Sprite individual
        Sprite s = Resources.Load<Sprite>(path);
        if (s != null) return s;

        // 2. Si se importo como Multiple Sprites (spriteMode: 2)
        Sprite[] sprites = Resources.LoadAll<Sprite>(path);
        if (sprites != null && sprites.Length > 0)
        {
            return sprites[0];
        }

        return null;
    }

    // -------------------------------------------------------
    // Construccion de la Interfaz Visual
    // -------------------------------------------------------

    private void BuildComicUI()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        Font boldFont = Resources.Load<Font>("Fonts/ArcadeBold");
        if (boldFont == null) boldFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        Font regularFont = Resources.Load<Font>("Fonts/ArcadeRegular");
        if (regularFont == null) regularFont = boldFont;

        Texture2D tex = new Texture2D(4, 4);
        Color[] px = new Color[16];
        for (int i = 0; i < px.Length; i++) px[i] = Color.white;
        tex.SetPixels(px); tex.Apply();
        Sprite white = Sprite.Create(tex, new Rect(0,0,4,4), new Vector2(0.5f,0.5f));

        // === 1. OVERLAY PRINCIPAL PANTALLA COMPLETA ===
        comicOverlay = new GameObject("Comic_Prologue_Overlay", typeof(RectTransform), typeof(Image));
        comicOverlay.transform.SetParent(canvas.transform, false);
        RectTransform rtOverlay = comicOverlay.GetComponent<RectTransform>();
        rtOverlay.anchorMin = Vector2.zero; rtOverlay.anchorMax = Vector2.one;
        rtOverlay.offsetMin = Vector2.zero; rtOverlay.offsetMax = Vector2.zero;
        Image bgImg = comicOverlay.GetComponent<Image>();
        bgImg.sprite = white;
        bgImg.color = new Color(0.02f, 0.03f, 0.06f, 0.99f);

        // Bordes superior e inferior estilo arcade
        MakeBar(comicOverlay.transform, white, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                Vector2.zero, new Vector2(0f, 5f), new Color(1f, 0.85f, 0.2f, 1f), "TopGold");
        MakeBar(comicOverlay.transform, white, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                Vector2.zero, new Vector2(0f, 5f), new Color(0.15f, 0.85f, 1f, 1f), "BottomCyan");

        // === 2. BARRA SUPERIOR (Cabecera y Boton Saltar) ===
        GameObject topBar = new GameObject("Top_Header_Bar", typeof(RectTransform));
        topBar.transform.SetParent(comicOverlay.transform, false);
        RectTransform rtTb = topBar.GetComponent<RectTransform>();
        rtTb.anchorMin = new Vector2(0f, 1f); rtTb.anchorMax = new Vector2(1f, 1f);
        rtTb.pivot = new Vector2(0.5f, 1f);
        rtTb.anchoredPosition = new Vector2(0f, -18f);
        rtTb.sizeDelta = new Vector2(-100f, 85f);

        chapterBadgeText = MakeText(topBar.transform, "VINETA 1 DE 3 • PROLOGO",
                 new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                 Vector2.zero, new Vector2(500f, 24f),
                 16, FontStyle.Bold, TextAnchor.MiddleLeft,
                 new Color(0.2f, 0.9f, 1f, 1f), "Text_ChapterBadge", boldFont, false);

        panelTitleText = MakeText(topBar.transform, "LA NOCHE ANTERIOR (3:00 AM)",
                 new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                 new Vector2(0f, -24f), new Vector2(800f, 36f),
                 28, FontStyle.Bold, TextAnchor.MiddleLeft,
                 new Color(1f, 0.88f, 0.2f, 1f), "Text_PanelTitle", boldFont, false);

        panelSubtitleText = MakeText(topBar.transform, "Habitacion de Fercho — Dolores Hidalgo, Gto.",
                 new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                 new Vector2(0f, -60f), new Vector2(800f, 24f),
                 16, FontStyle.Italic, TextAnchor.MiddleLeft,
                 new Color(0.8f, 0.86f, 0.96f, 0.85f), "Text_PanelSubtitle", regularFont, false);

        GameObject skipBtnObj = MakeButton(topBar.transform, white,
                                           new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                                           new Vector2(0f, 0f), new Vector2(250f, 52f),
                                           "SALTAR PROLOGO >>",
                                           new Color(0.28f, 0.08f, 0.12f, 0.95f),
                                           new Color(0.55f, 0.15f, 0.22f, 1f),
                                           "Btn_SkipPrologue", SkipOrFinish, boldFont);
        btnSkip = skipBtnObj.GetComponent<Button>();

        // === 3. CONTENEDOR CENTRAL: VINETA NORMAL ===
        normalContentObj = new GameObject("Normal_Content_Container", typeof(RectTransform));
        normalContentObj.transform.SetParent(comicOverlay.transform, false);
        RectTransform rtNc = normalContentObj.GetComponent<RectTransform>();
        rtNc.anchorMin = new Vector2(0.5f, 0.5f); rtNc.anchorMax = new Vector2(0.5f, 0.5f);
        rtNc.pivot = new Vector2(0.5f, 0.5f);
        rtNc.anchoredPosition = new Vector2(0f, 15f);
        rtNc.sizeDelta = new Vector2(1360f, 740f);

        // Marco exterior de la Ilustracion de la Viñeta (540 px de alto)
        GameObject frameObj = new GameObject("Comic_Panel_Frame", typeof(RectTransform), typeof(Image));
        frameObj.transform.SetParent(normalContentObj.transform, false);
        RectTransform rtFrame = frameObj.GetComponent<RectTransform>();
        rtFrame.anchorMin = new Vector2(0.5f, 1f); rtFrame.anchorMax = new Vector2(0.5f, 1f);
        rtFrame.pivot = new Vector2(0.5f, 1f);
        rtFrame.anchoredPosition = new Vector2(0f, 0f);
        rtFrame.sizeDelta = new Vector2(1280f, 520f);

        Image frameBg = frameObj.GetComponent<Image>();
        frameBg.sprite = white;
        frameBg.color = new Color(0.04f, 0.06f, 0.12f, 1f);

        MakeBar(frameObj.transform, white, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                Vector2.zero, new Vector2(0f, 4f), new Color(1f, 0.85f, 0.2f, 1f), "FrameTop");
        MakeBar(frameObj.transform, white, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                Vector2.zero, new Vector2(0f, 4f), new Color(0.15f, 0.85f, 1f, 0.8f), "FrameBottom");

        // Imagen de la Viñeta con preserveAspect = true para que nunca se distorsione
        GameObject panelImgObj = new GameObject("Panel_Artwork_Image", typeof(RectTransform), typeof(Image));
        panelImgObj.transform.SetParent(frameObj.transform, false);
        RectTransform rtPi = panelImgObj.GetComponent<RectTransform>();
        rtPi.anchorMin = Vector2.zero; rtPi.anchorMax = Vector2.one;
        rtPi.offsetMin = new Vector2(6f, 6f); rtPi.offsetMax = new Vector2(-6f, -6f);
        panelImage = panelImgObj.GetComponent<Image>();
        panelImage.preserveAspect = true;

        panelPlaceholderIcon = MakeText(frameObj.transform, "[ ILUSTRACION EN PROCESO ]",
                 Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                 Vector2.zero, Vector2.zero,
                 26, FontStyle.Bold, TextAnchor.MiddleCenter,
                 new Color(0.4f, 0.85f, 1f, 1f), "Text_Placeholder", boldFont, false);

        // Globo / Cuadro de Dialogo estilo Comic (Debajo de la Ilustracion con Wrap activado)
        GameObject dialogueCard = new GameObject("Dialogue_Bubble_Card", typeof(RectTransform), typeof(Image));
        dialogueCard.transform.SetParent(normalContentObj.transform, false);
        RectTransform rtDc = dialogueCard.GetComponent<RectTransform>();
        rtDc.anchorMin = new Vector2(0.5f, 0f); rtDc.anchorMax = new Vector2(0.5f, 0f);
        rtDc.pivot = new Vector2(0.5f, 0f);
        rtDc.anchoredPosition = new Vector2(0f, 10f);
        rtDc.sizeDelta = new Vector2(1280f, 185f);
        Image dcBg = dialogueCard.GetComponent<Image>();
        dcBg.sprite = white;
        dcBg.color = new Color(0.04f, 0.06f, 0.13f, 0.97f);

        MakeBar(dialogueCard.transform, white, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                Vector2.zero, new Vector2(0f, 3f), new Color(1f, 0.85f, 0.2f, 0.9f), "BubbleTop");
        MakeBar(dialogueCard.transform, white, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f),
                Vector2.zero, new Vector2(6f, 0f), new Color(0.2f, 0.85f, 1f, 0.95f), "BubbleLeft");

        // Placa del Hablante (Ubicada ARRIBA del marco del globo para NUNCA encimarse con el texto)
        GameObject speakerBadgeObj = new GameObject("Speaker_Badge", typeof(RectTransform), typeof(Image));
        speakerBadgeObj.transform.SetParent(dialogueCard.transform, false);
        RectTransform rtSb = speakerBadgeObj.GetComponent<RectTransform>();
        rtSb.anchorMin = new Vector2(0f, 1f); rtSb.anchorMax = new Vector2(0f, 1f);
        rtSb.pivot = new Vector2(0f, 0f);
        rtSb.anchoredPosition = new Vector2(25f, 0f); // Se asienta elegantemente sobre el borde superior
        rtSb.sizeDelta = new Vector2(220f, 38f);
        speakerBadgeBg = speakerBadgeObj.GetComponent<Image>();
        speakerBadgeBg.sprite = white;
        speakerBadgeBg.color = new Color(0.08f, 0.16f, 0.32f, 0.98f);

        MakeBar(speakerBadgeObj.transform, white, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                Vector2.zero, new Vector2(0f, 3f), new Color(0.25f, 0.9f, 1f, 1f), "BadgeTop");

        speakerBadgeText = MakeText(speakerBadgeObj.transform, "FERCHO",
                 Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                 Vector2.zero, Vector2.zero,
                 20, FontStyle.Bold, TextAnchor.MiddleCenter,
                 new Color(0.2f, 0.9f, 1f, 1f), "Text_Speaker", boldFont, false);

        // Texto Narrativo del Globo (Ocupa todo el interior del cuadro con amplio margen y Wrap)
        GameObject txtObj = new GameObject("Text_NarrativeBody", typeof(RectTransform), typeof(Text));
        txtObj.transform.SetParent(dialogueCard.transform, false);
        RectTransform rtTxt = txtObj.GetComponent<RectTransform>();
        rtTxt.anchorMin = new Vector2(0f, 1f); rtTxt.anchorMax = new Vector2(1f, 1f);
        rtTxt.pivot = new Vector2(0f, 1f);
        rtTxt.anchoredPosition = new Vector2(30f, -22f);
        rtTxt.sizeDelta = new Vector2(-60f, 140f);

        narrativeBodyText = txtObj.GetComponent<Text>();
        narrativeBodyText.font = regularFont;
        narrativeBodyText.text = "...";
        narrativeBodyText.fontSize = 24;
        narrativeBodyText.fontStyle = FontStyle.Normal;
        narrativeBodyText.alignment = TextAnchor.UpperLeft;
        narrativeBodyText.color = new Color(0.96f, 0.98f, 1f, 1f);
        narrativeBodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
        narrativeBodyText.verticalOverflow = VerticalWrapMode.Overflow;
        narrativeBodyText.lineSpacing = 1.3f;

        Outline bodyOut = txtObj.AddComponent<Outline>();
        bodyOut.effectColor = new Color(0.01f, 0.02f, 0.05f, 0.95f);
        bodyOut.effectDistance = new Vector2(1.5f, -1.5f);

        // === 4. PANEL DE CONTROLES (Panel 4) ===
        controlsCardObj = new GameObject("Controls_Special_Card", typeof(RectTransform), typeof(Image));
        controlsCardObj.transform.SetParent(comicOverlay.transform, false);
        RectTransform rtCc = controlsCardObj.GetComponent<RectTransform>();
        rtCc.anchorMin = new Vector2(0.5f, 0.5f); rtCc.anchorMax = new Vector2(0.5f, 0.5f);
        rtCc.pivot = new Vector2(0.5f, 0.5f);
        rtCc.anchoredPosition = new Vector2(0f, 15f);
        rtCc.sizeDelta = new Vector2(1320f, 680f);

        Image ccBg = controlsCardObj.GetComponent<Image>();
        ccBg.sprite = white;
        ccBg.color = new Color(0.03f, 0.05f, 0.11f, 0.98f);

        MakeBar(controlsCardObj.transform, white, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                Vector2.zero, new Vector2(0f, 5f), new Color(0.2f, 0.85f, 1f, 1f), "ControlsTop");

        // 4 Tarjetas de Instruccion Arcade espaciadas sin traslape
        BuildControlTile(controlsCardObj.transform, white, new Vector2(-320f, 150f), "SISTEMA DE COMBATE",
                         "• [Z] o [J] o [Click Izq] : Combo de Golpes\n" +
                         "• 1er Golpe : Jab rapido a la cara\n" +
                         "• 2do Golpe : Cross directo al cuerpo\n" +
                         "• 3er Golpe : Patada alta con noqueo",
                         new Color(0.95f, 0.35f, 0.15f, 1f), boldFont, regularFont);

        BuildControlTile(controlsCardObj.transform, white, new Vector2(320f, 150f), "MOVILIDAD URBANA",
                         "• [A] / [D] o [Flechas] : Mover a Fercho\n" +
                         "• [Espacio] : Salto acrobatico vertical\n" +
                         "• Esquiva ataques enemigos y salta sobre cajas de madera para ganar ventaja.",
                         new Color(0.2f, 0.85f, 0.45f, 1f), boldFont, regularFont);

        BuildControlTile(controlsCardObj.transform, white, new Vector2(-320f, -135f), "CAJAS Y SUMINISTROS",
                         "• Rompe las cajas de madera a golpes.\n" +
                         "• Contienen alimentos que recargan la bateria de salud de Fercho.\n" +
                         "• ¡Cuidado! Los enemigos tambien pueden derribarlas.",
                         new Color(1f, 0.85f, 0.2f, 1f), boldFont, regularFont);

        BuildControlTile(controlsCardObj.transform, white, new Vector2(320f, -135f), "OBJETIVO DE LA MISION",
                         "• Cruza la Calzada de los Heroes.\n" +
                         "• Derrota a las 3 oleadas de estudiantes rivales.\n" +
                         "• Vence al temible Alumno de Taller antes de las 2:00 PM para salvar el semestre.",
                         new Color(0.35f, 0.8f, 1f, 1f), boldFont, regularFont);

        controlsCardObj.SetActive(false);

        // === 5. BARRA DE NAVEGACION INFERIOR ===
        GameObject bottomBar = new GameObject("Bottom_Navigation_Bar", typeof(RectTransform));
        bottomBar.transform.SetParent(comicOverlay.transform, false);
        RectTransform rtBb = bottomBar.GetComponent<RectTransform>();
        rtBb.anchorMin = new Vector2(0f, 0f); rtBb.anchorMax = new Vector2(1f, 0f);
        rtBb.pivot = new Vector2(0.5f, 0f);
        rtBb.anchoredPosition = new Vector2(0f, 20f);
        rtBb.sizeDelta = new Vector2(-100f, 70f);

        GameObject prevBtnObj = MakeButton(bottomBar.transform, white,
                                           new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                                           new Vector2(0f, 0f), new Vector2(230f, 56f),
                                           "<< ANTERIOR",
                                           new Color(0.12f, 0.16f, 0.28f, 1f),
                                           new Color(0.2f, 0.28f, 0.48f, 1f),
                                           "Btn_Previous", OnPreviousClicked, boldFont);
        btnPrevious = prevBtnObj.GetComponent<Button>();

        pageIndicatorText = MakeText(bottomBar.transform, "[ 1 ]   2   3   CONTROLES",
                 new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                 Vector2.zero, new Vector2(600f, 40f),
                 20, FontStyle.Bold, TextAnchor.MiddleCenter,
                 Color.white, "Text_PageIndicator", boldFont, false);

        GameObject nextBtnObj = MakeButton(bottomBar.transform, white,
                                           new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                                           new Vector2(0f, 0f), new Vector2(320f, 62f),
                                           "SIGUIENTE >>",
                                           new Color(0.08f, 0.45f, 0.22f, 1f),
                                           new Color(0.14f, 0.75f, 0.36f, 1f),
                                           "Btn_Next", OnNextClicked, boldFont);
        btnNext = nextBtnObj.GetComponent<Button>();
        btnNextText = nextBtnObj.GetComponentInChildren<Text>();

        // === 6. PANTALLA DE CARGA (Loading Screen Overlay) ===
        loadingScreenObj = new GameObject("Loading_Screen_Overlay", typeof(RectTransform), typeof(Image));
        loadingScreenObj.transform.SetParent(comicOverlay.transform, false);
        RectTransform rtLs = loadingScreenObj.GetComponent<RectTransform>();
        rtLs.anchorMin = Vector2.zero; rtLs.anchorMax = Vector2.one;
        rtLs.offsetMin = Vector2.zero; rtLs.offsetMax = Vector2.zero;
        Image imgLs = loadingScreenObj.GetComponent<Image>();
        imgLs.sprite = white;
        imgLs.color = new Color(0.01f, 0.02f, 0.04f, 1f);

        MakeText(loadingScreenObj.transform, "» CARGANDO LA CALZADA DE LOS HEROES... «",
                 new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                 new Vector2(0f, 25f), new Vector2(900f, 48f),
                 28, FontStyle.Bold, TextAnchor.MiddleCenter,
                 new Color(1f, 0.85f, 0.2f, 1f), "Text_LoadingTitle", boldFont, false);

        MakeText(loadingScreenObj.transform, "[ PREPARANDO PROYECTO FINAL DEL CBTis 75 ]",
                 new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                 new Vector2(0f, -25f), new Vector2(900f, 32f),
                 18, FontStyle.Bold, TextAnchor.MiddleCenter,
                 new Color(0.2f, 0.85f, 1f, 0.9f), "Text_LoadingSubtitle", regularFont, false);

        loadingScreenObj.SetActive(false);
        comicOverlay.SetActive(false);
    }

    private void BuildControlTile(Transform parent, Sprite white, Vector2 aPos, string title,
                                  string body, Color accentColor, Font boldFont, Font regularFont)
    {
        GameObject tile = new GameObject("Tile_" + title, typeof(RectTransform), typeof(Image));
        tile.transform.SetParent(parent, false);
        RectTransform rt = tile.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f); rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = aPos;
        rt.sizeDelta = new Vector2(610f, 250f);
        Image bg = tile.GetComponent<Image>();
        bg.sprite = white;
        bg.color = new Color(0.06f, 0.08f, 0.16f, 0.97f);

        MakeBar(tile.transform, white, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f),
                Vector2.zero, new Vector2(6f, 0f), accentColor, "SideBar");

        // 1. Titulo del Tile (Posicionado de Y: -12 a Y: -46)
        GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(Text));
        titleObj.transform.SetParent(tile.transform, false);
        RectTransform rtT = titleObj.GetComponent<RectTransform>();
        rtT.anchorMin = new Vector2(0f, 1f); rtT.anchorMax = new Vector2(1f, 1f);
        rtT.pivot = new Vector2(0f, 1f);
        rtT.anchoredPosition = new Vector2(22f, -14f);
        rtT.sizeDelta = new Vector2(-44f, 34f);

        Text tComp = titleObj.GetComponent<Text>();
        tComp.font = boldFont;
        tComp.text = title;
        tComp.fontSize = 20;
        tComp.fontStyle = FontStyle.Bold;
        tComp.alignment = TextAnchor.MiddleLeft;
        tComp.color = accentColor;
        tComp.horizontalOverflow = HorizontalWrapMode.Overflow;
        tComp.verticalOverflow = VerticalWrapMode.Overflow;

        Outline tOut = titleObj.AddComponent<Outline>();
        tOut.effectColor = Color.black;
        tOut.effectDistance = new Vector2(1.5f, -1.5f);

        // 2. Linea divisoria elegante
        MakeBar(tile.transform, white, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -50f), new Vector2(-30f, 2f), new Color(1f, 1f, 1f, 0.15f), "Divider");

        // 3. Cuerpo del texto (Empieza de Y: -62 hacia abajo, con espacio garantizado y Wrap)
        GameObject bodyObj = new GameObject("Body", typeof(RectTransform), typeof(Text));
        bodyObj.transform.SetParent(tile.transform, false);
        RectTransform rtB = bodyObj.GetComponent<RectTransform>();
        rtB.anchorMin = new Vector2(0f, 1f); rtB.anchorMax = new Vector2(1f, 1f);
        rtB.pivot = new Vector2(0f, 1f);
        rtB.anchoredPosition = new Vector2(22f, -62f);
        rtB.sizeDelta = new Vector2(-44f, 175f);

        Text bComp = bodyObj.GetComponent<Text>();
        bComp.font = regularFont;
        bComp.text = body;
        bComp.fontSize = 17;
        bComp.fontStyle = FontStyle.Normal;
        bComp.alignment = TextAnchor.UpperLeft;
        bComp.color = new Color(0.92f, 0.95f, 1f, 1f);
        bComp.horizontalOverflow = HorizontalWrapMode.Wrap;
        bComp.verticalOverflow = VerticalWrapMode.Overflow;
        bComp.lineSpacing = 1.28f;

        Outline bOut = bodyObj.AddComponent<Outline>();
        bOut.effectColor = new Color(0.01f, 0.02f, 0.05f, 0.95f);
        bOut.effectDistance = new Vector2(1.5f, -1.5f);
    }

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
                          Color color, string name, Font font, bool enableWrap = false)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Text));
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = ancMin; rt.anchorMax = ancMax; rt.pivot = pivot;
        rt.anchoredPosition = aPos; rt.sizeDelta = sizeDelta;

        Text t = obj.GetComponent<Text>();
        t.font = font;
        t.text = content;
        t.fontSize = fontSize;
        t.fontStyle = style;
        t.alignment = align;
        t.color = color;

        if (enableWrap)
        {
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
        }
        else
        {
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
        }

        Outline outline = obj.AddComponent<Outline>();
        outline.effectColor = new Color(0.01f, 0.02f, 0.05f, 0.95f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);

        return t;
    }

    private GameObject MakeButton(Transform parent, Sprite white,
                                  Vector2 ancMin, Vector2 ancMax, Vector2 pivot,
                                  Vector2 aPos, Vector2 size,
                                  string label, Color normalColor, Color hoverColor,
                                  string name, UnityEngine.Events.UnityAction action, Font font)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = ancMin; rt.anchorMax = ancMax; rt.pivot = pivot;
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

        MakeBar(btnObj.transform, white, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 3f),
                new Color(1f, 1f, 1f, 0.3f), "TopShine");

        Text t = MakeText(btnObj.transform, label,
                 Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                 Vector2.zero, Vector2.zero,
                 20, FontStyle.Bold, TextAnchor.MiddleCenter,
                 Color.white, "Btn_Label", font, false);

        return btnObj;
    }
}
