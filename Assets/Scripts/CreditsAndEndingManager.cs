using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

/// <summary>
/// CreditsAndEndingManager - Gestiona la experiencia cinematica final de Dolondrazo:
/// 1. Epilogo narrativo en historieta (3 vinetas ilustradas).
/// 2. Creditos finales con el autobus Zafiro DH-0087 recorriendo la carretera al atardecer.
/// </summary>
public class CreditsAndEndingManager : MonoBehaviour
{
    public static CreditsAndEndingManager Instance { get; private set; }

    [System.Serializable]
    public class EpiloguePanel
    {
        public string tag;
        public string title;
        public string subtitle;
        [TextArea(2, 5)]
        public string narrative;
        public Color accentColor;
    }

    [Header("Musica y Sonido")]
    [SerializeField] private AudioClip creditsMusic;
    [SerializeField] private AudioSource audioSource;

    [Header("Sprite del Autobus")]
    [SerializeField] private Sprite busSprite;

    private Canvas canvas;
    private GameObject epilogueRoot;
    private GameObject creditsRoot;

    // Elementos del epilogo
    private Text epTagText;
    private Text epTitleText;
    private Text epSubText;
    private Text epBodyText;
    private Text epPageIndicator;
    private Image epIllustrationImage;
    private Text epPlaceholderText;
    private Button btnNextEp;
    private Button btnPrevEp;
    private Button btnSkipEp;
    private int currentEpIndex = 0;

    // Elementos de creditos
    private RectTransform creditsTextContainer;
    private RectTransform busTransform;
    private bool isRollingCredits = false;
    private float rollSpeed = 42f;

    private List<EpiloguePanel> epiloguePanels = new List<EpiloguePanel>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = true;
        }

        StopAllLevelAudio();
        InitEpilogueData();
    }

    private void Start()
    {
        if (creditsMusic == null)
            creditsMusic = Resources.Load<AudioClip>("Sounds/Junior H - Mente Positiva (Letra_Lyric Video) 2020");
        if (creditsMusic == null)
            creditsMusic = Resources.Load<AudioClip>("Sounds/Intro");
        PlayEndingMusic();
        EnsureCanvas();
        StartEpilogue();
    }






    private void Update()
    {
        // Durante los creditos rodantes
        if (isRollingCredits)
        {
            if (creditsTextContainer != null)
            {
                creditsTextContainer.anchoredPosition += Vector2.up * (rollSpeed * Time.deltaTime);

                // Balanceo suave del autobus
                if (busTransform != null)
                {
                    float wobble = Mathf.Sin(Time.time * 6f) * 3f;
                    busTransform.anchoredPosition = new Vector2(-600f, -260f + wobble);
                }

                // Acelerar creditos si se mantiene pulsado Espacio o flecha abajo
                if (Keyboard.current != null)
                {
                    if (Keyboard.current.spaceKey.isPressed || Keyboard.current.downArrowKey.isPressed)
                    {
                        rollSpeed = 120f;
                    }
                    else
                    {
                        rollSpeed = 42f;
                    }

                    if (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame)
                    {
                        ReturnToMenu();
                    }
                }
            }
        }
    }

    private void InitEpilogueData()
    {
        epiloguePanels = new List<EpiloguePanel>
        {
            new EpiloguePanel
            {
                tag = "EPILOGO â€¢ VINETA 1 DE 3",
                title = "ENTREGA A TIEMPO (1:55 PM)",
                subtitle = "Aula de Clases â€” CBTis 75, Dolores Hidalgo",
                narrative = "Â¡Por un pelo! Entro corriendo al salon justo cinco minutos antes de que suene la campana. Coloco la maqueta intacta sobre el escritorio del profesor. Me mira con asombro viendo mi ropa llena de polvo por las peleas en la calle... pero la maqueta esta impecable. Â¡Calificacion de 10 asegurada!",
                accentColor = new Color(0.25f, 0.85f, 1f, 1f)
            },
            new EpiloguePanel
            {
                tag = "EPILOGO â€¢ VINETA 2 DE 3",
                title = "EL ZAFIRO DEL TRIUNFO (2:30 PM)",
                subtitle = "Avenida Principal â€” Autobus Zafiro DH-0087",
                narrative = "Sentado comodamente junto a la ventanilla del autobus, veo pasar la Calzada de los Heroes y el Centro de Dolores Hidalgo. El rugido del motor diesel suena como musica de victoria. Todo el esfuerzo y cada golpe valieron la pena.",
                accentColor = new Color(1f, 0.85f, 0.2f, 1f)
            },
            new EpiloguePanel
            {
                tag = "EPILOGO â€¢ VINETA 3 DE 3",
                title = "UNA NIEVE BIEN MERECIDA (3:15 PM)",
                subtitle = "Jardin Principal â€” Parroquia de Dolores Hidalgo",
                narrative = "Termino el dia en una banca del jardin colonial saboreando una nieve tradicional de mantecado con queso. El sol se oculta sobre las torres de la iglesia. Hoy defendi mi proyecto y demostre de que estamos hechos los del CBTis 75.",
                accentColor = new Color(0.3f, 0.95f, 0.5f, 1f)
            }
        };
    }

    public void StartEpilogue()
    {
        currentEpIndex = 0;
        BuildEpilogueUI();
        ShowEpiloguePanel(0);
    }

    private void ShowEpiloguePanel(int index)
    {
        if (index < 0 || index >= epiloguePanels.Count) return;
        currentEpIndex = index;

        var panel = epiloguePanels[index];
        if (epTagText != null) { epTagText.text = panel.tag; epTagText.color = panel.accentColor; }
        if (epTitleText != null) epTitleText.text = panel.title;
        if (epSubText != null) epSubText.text = panel.subtitle;
        if (epBodyText != null) epBodyText.text = panel.narrative;
        if (epPageIndicator != null) epPageIndicator.text = $"{index + 1} / {epiloguePanels.Count}";

        // Cargar imagen de la vineta si existe en Resources/Sprites/Comic/
        Sprite epSprite = Resources.Load<Sprite>($"Sprites/Comic/Epilogo_{index + 1}");
        if (epSprite == null) epSprite = Resources.Load<Sprite>($"Sprites/Comic/Vineta_Final_{index + 1}");

        if (epIllustrationImage != null)
        {
            if (epSprite != null)
            {
                epIllustrationImage.sprite = epSprite;
                epIllustrationImage.color = Color.white;
                if (epPlaceholderText != null) epPlaceholderText.gameObject.SetActive(false);
            }
            else
            {
                epIllustrationImage.sprite = null;
                epIllustrationImage.color = new Color(0.06f, 0.08f, 0.14f, 1f);
                if (epPlaceholderText != null) epPlaceholderText.gameObject.SetActive(true);
            }
        }

        if (btnPrevEp != null) btnPrevEp.gameObject.SetActive(index > 0);

        if (btnNextEp != null)
        {
            Text nextTxt = btnNextEp.GetComponentInChildren<Text>();
            if (nextTxt != null)
            {
                nextTxt.text = (index == epiloguePanels.Count - 1) ? "VER CREDITOS >>" : "SIGUIENTE >>";
            }
        }
    }

    private void NextEpilogue()
    {
        if (currentEpIndex < epiloguePanels.Count - 1)
        {
            ShowEpiloguePanel(currentEpIndex + 1);
        }
        else
        {
            StartCredits();
        }
    }

    private void PrevEpilogue()
    {
        if (currentEpIndex > 0)
        {
            ShowEpiloguePanel(currentEpIndex - 1);
        }
    }

    public void StartCredits()
    {
        if (epilogueRoot != null) Destroy(epilogueRoot);
        BuildCreditsUI();
        isRollingCredits = true;
        PlayEndingMusic();
    }







    
    private void StopAllLevelAudio()
    {
        GameObject bgmObj = GameObject.Find("Musica_Nivel");
        if (bgmObj != null)
        {
            AudioSource src = bgmObj.GetComponent<AudioSource>();
            if (src != null)
            {
                src.Stop();
                src.clip = null;
            }
            bgmObj.SetActive(false);
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopBGM();
        }

        AudioSource[] allSources = FindObjectsByType<AudioSource>(FindObjectsSortMode.None);
        foreach (var s in allSources)
        {
            if (s != null && s != audioSource && (AudioManager.Instance == null || s != AudioManager.Instance.GetComponent<AudioSource>()))
            {
                s.Stop();
            }
        }
    }
    private void PlayEndingMusic()
    {
        StopAllLevelAudio();
        if (creditsMusic == null) return;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBGM(creditsMusic, true);
        }
        else
        {
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.loop = true;
                audioSource.playOnAwake = false;
            }
            audioSource.clip = creditsMusic;
            audioSource.volume = 1f;
            audioSource.Play();
        }
    }
    public void ReturnToMenu()
    {
        Time.timeScale = 1f;
        StopAllLevelAudio();
        if (audioSource != null) audioSource.Stop();
        if (canvas != null) Destroy(canvas.gameObject);
        Destroy(gameObject);
        LevelTransitionLoader.LoadLevel("Pantalla_Inicio", "MENU PRINCIPAL", "Regresando al inicio...");

    }

    // -------------------------------------------------------------
    // Construccion Dinamica de la Interfaz
    // -------------------------------------------------------------

    private void EnsureCanvas()
    {
        if (canvas != null) return;

        GameObject cObj = new GameObject("Ending_Canvas");
        canvas = cObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        CanvasScaler scaler = cObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        cObj.AddComponent<GraphicRaycaster>();
    }

    private void BuildEpilogueUI()
    {
        if (canvas == null) EnsureCanvas();

        Texture2D tex = new Texture2D(2, 2);
        tex.SetPixels(new Color[] { Color.white, Color.white, Color.white, Color.white });
        tex.Apply();
        Sprite white = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));

        epilogueRoot = new GameObject("Epilogue_Root", typeof(RectTransform));
        epilogueRoot.transform.SetParent(canvas.transform, false);
        RectTransform rtRoot = epilogueRoot.GetComponent<RectTransform>();
        rtRoot.anchorMin = Vector2.zero; rtRoot.anchorMax = Vector2.one;
        rtRoot.offsetMin = Vector2.zero; rtRoot.offsetMax = Vector2.zero;

        // Fondo oscuro con viÃ±eta
        GameObject bgObj = new GameObject("Bg", typeof(RectTransform), typeof(Image));
        bgObj.transform.SetParent(epilogueRoot.transform, false);
        RectTransform rtBg = bgObj.GetComponent<RectTransform>();
        rtBg.anchorMin = Vector2.zero; rtBg.anchorMax = Vector2.one;
        rtBg.offsetMin = Vector2.zero; rtBg.offsetMax = Vector2.zero;
        Image bgImg = bgObj.GetComponent<Image>();
        bgImg.sprite = white;
        bgImg.color = new Color(0.04f, 0.05f, 0.09f, 0.98f);

        // Tarjeta central de la viÃ±eta
        GameObject card = new GameObject("Card", typeof(RectTransform), typeof(Image));
        card.transform.SetParent(epilogueRoot.transform, false);
        RectTransform rtCard = card.GetComponent<RectTransform>();
        rtCard.sizeDelta = new Vector2(1240f, 650f);
        Image cardImg = card.GetComponent<Image>();
        cardImg.sprite = white;
        cardImg.color = new Color(0.08f, 0.11f, 0.18f, 0.96f);

        // Borde superior dorado
        GameObject topBar = new GameObject("TopBar", typeof(RectTransform), typeof(Image));
        topBar.transform.SetParent(card.transform, false);
        RectTransform rtTop = topBar.GetComponent<RectTransform>();
        rtTop.anchorMin = new Vector2(0f, 1f); rtTop.anchorMax = new Vector2(1f, 1f);
        rtTop.offsetMin = new Vector2(0f, -4f); rtTop.offsetMax = Vector2.zero;
        Image topImg = topBar.GetComponent<Image>();
        topImg.sprite = white;
        topImg.color = new Color(1f, 0.85f, 0.2f, 1f);

        // Etiqueta Capitulo / Vineta
        GameObject tagObj = new GameObject("Tag", typeof(RectTransform), typeof(Text));
        tagObj.transform.SetParent(card.transform, false);
        RectTransform rtTag = tagObj.GetComponent<RectTransform>();
        rtTag.anchoredPosition = new Vector2(0f, 255f);
        rtTag.sizeDelta = new Vector2(1180f, 32f);
        epTagText = tagObj.GetComponent<Text>();
        epTagText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        epTagText.fontSize = 20;
        epTagText.alignment = TextAnchor.MiddleCenter;
        epTagText.fontStyle = FontStyle.Bold;

        // Titulo
        GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(Text));
        titleObj.transform.SetParent(card.transform, false);
        RectTransform rtTitle = titleObj.GetComponent<RectTransform>();
        rtTitle.anchoredPosition = new Vector2(0f, 205f);
        rtTitle.sizeDelta = new Vector2(1180f, 50f);
        epTitleText = titleObj.GetComponent<Text>();
        epTitleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        epTitleText.fontSize = 36;
        epTitleText.alignment = TextAnchor.MiddleCenter;
        epTitleText.fontStyle = FontStyle.Bold;
        epTitleText.color = Color.white;

        // Subtitulo
        GameObject subObj = new GameObject("Subtitle", typeof(RectTransform), typeof(Text));
        subObj.transform.SetParent(card.transform, false);
        RectTransform rtSub = subObj.GetComponent<RectTransform>();
        rtSub.anchoredPosition = new Vector2(0f, 155f);
        rtSub.sizeDelta = new Vector2(1180f, 32f);
        epSubText = subObj.GetComponent<Text>();
        epSubText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        epSubText.fontSize = 21;
        epSubText.alignment = TextAnchor.MiddleCenter;
        epSubText.fontStyle = FontStyle.Italic;
        epSubText.color = new Color(0.7f, 0.85f, 1f, 0.85f);

        // Marco de Ilustracion de la Vineta (Lado Izquierdo)
        GameObject frameObj = new GameObject("IllustrationFrame", typeof(RectTransform), typeof(Image));
        frameObj.transform.SetParent(card.transform, false);
        RectTransform rtFr = frameObj.GetComponent<RectTransform>();
        rtFr.anchoredPosition = new Vector2(-280f, -20f);
        rtFr.sizeDelta = new Vector2(540f, 310f);
        Image frImg = frameObj.GetComponent<Image>();
        frImg.sprite = white;
        frImg.color = new Color(0.15f, 0.22f, 0.35f, 1f);

        GameObject imgObj = new GameObject("IllustrationImage", typeof(RectTransform), typeof(Image));
        imgObj.transform.SetParent(frameObj.transform, false);
        RectTransform rtIm = imgObj.GetComponent<RectTransform>();
        rtIm.anchorMin = Vector2.zero; rtIm.anchorMax = Vector2.one;
        rtIm.offsetMin = new Vector2(4f, 4f); rtIm.offsetMax = new Vector2(-4f, -4f);
        epIllustrationImage = imgObj.GetComponent<Image>();
        epIllustrationImage.preserveAspect = true;

        GameObject phObj = new GameObject("PlaceholderText", typeof(RectTransform), typeof(Text));
        phObj.transform.SetParent(frameObj.transform, false);
        RectTransform rtPh = phObj.GetComponent<RectTransform>();
        rtPh.anchorMin = Vector2.zero; rtPh.anchorMax = Vector2.one;
        rtPh.offsetMin = Vector2.zero; rtPh.offsetMax = Vector2.zero;
        epPlaceholderText = phObj.GetComponent<Text>();
        epPlaceholderText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        epPlaceholderText.fontSize = 20;
        epPlaceholderText.fontStyle = FontStyle.Bold;
        epPlaceholderText.alignment = TextAnchor.MiddleCenter;
        epPlaceholderText.color = new Color(0.4f, 0.65f, 0.85f, 0.8f);
        epPlaceholderText.text = "[ ILUSTRACION EN PROCESO ]\n\nGuarda la imagen en:\nAssets/Resources/Sprites/Comic/\ncon nombre Epilogo_1, Epilogo_2, etc.";

        // Cuadro de Narrativa / Dialogo (Lado Derecho)
        GameObject bodyBox = new GameObject("BodyBox", typeof(RectTransform), typeof(Image));
        bodyBox.transform.SetParent(card.transform, false);
        RectTransform rtBox = bodyBox.GetComponent<RectTransform>();
        rtBox.anchoredPosition = new Vector2(300f, -20f);
        rtBox.sizeDelta = new Vector2(580f, 310f);
        Image boxImg = bodyBox.GetComponent<Image>();
        boxImg.sprite = white;
        boxImg.color = new Color(0.04f, 0.06f, 0.11f, 0.9f);

        GameObject bodyObj = new GameObject("BodyText", typeof(RectTransform), typeof(Text));
        bodyObj.transform.SetParent(bodyBox.transform, false);
        RectTransform rtBody = bodyObj.GetComponent<RectTransform>();
        rtBody.anchorMin = Vector2.zero; rtBody.anchorMax = Vector2.one;
        rtBody.offsetMin = new Vector2(30f, 20f); rtBody.offsetMax = new Vector2(-30f, -20f);
        epBodyText = bodyObj.GetComponent<Text>();
        epBodyText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        epBodyText.fontSize = 24;
        epBodyText.lineSpacing = 1.35f;
        epBodyText.alignment = TextAnchor.MiddleCenter;
        epBodyText.color = new Color(0.95f, 0.95f, 0.98f, 1f);

        // Indicador de pagina
        GameObject indObj = new GameObject("Indicator", typeof(RectTransform), typeof(Text));
        indObj.transform.SetParent(card.transform, false);
        RectTransform rtInd = indObj.GetComponent<RectTransform>();
        rtInd.anchoredPosition = new Vector2(0f, -220f);
        rtInd.sizeDelta = new Vector2(200f, 30f);
        epPageIndicator = indObj.GetComponent<Text>();
        epPageIndicator.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        epPageIndicator.fontSize = 18;
        epPageIndicator.alignment = TextAnchor.MiddleCenter;
        epPageIndicator.color = new Color(0.6f, 0.7f, 0.8f, 1f);

        // Boton Anterior
        btnPrevEp = CreateButton(card.transform, new Vector2(-320f, -220f), new Vector2(180f, 50f), "<< ANTERIOR",
                                 new Color(0.2f, 0.25f, 0.35f, 1f), PrevEpilogue);

        // Boton Siguiente
        btnNextEp = CreateButton(card.transform, new Vector2(320f, -220f), new Vector2(200f, 50f), "SIGUIENTE >>",
                                 new Color(0.15f, 0.65f, 0.35f, 1f), NextEpilogue);

        // Boton Saltar
        btnSkipEp = CreateButton(card.transform, new Vector2(400f, 260f), new Vector2(140f, 38f), "SALTAR >>",
                                 new Color(0.4f, 0.15f, 0.15f, 0.9f), StartCredits);
    }

    private void BuildCreditsUI()
    {
        if (canvas == null) EnsureCanvas();

        Texture2D tex = new Texture2D(2, 2);
        tex.SetPixels(new Color[] { Color.white, Color.white, Color.white, Color.white });
        tex.Apply();
        Sprite white = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));

        creditsRoot = new GameObject("Credits_Root", typeof(RectTransform));
        creditsRoot.transform.SetParent(canvas.transform, false);
        RectTransform rtRoot = creditsRoot.GetComponent<RectTransform>();
        rtRoot.anchorMin = Vector2.zero; rtRoot.anchorMax = Vector2.one;
        rtRoot.offsetMin = Vector2.zero; rtRoot.offsetMax = Vector2.zero;

        // Fondo personalizado si el usuario coloca una imagen en Resources/Sprites/Fondo_Creditos
        Sprite customBg = Resources.Load<Sprite>("Sprites/Fondo_Creditos");
        if (customBg == null) customBg = Resources.Load<Sprite>("Sprites/Credits_Background");
        if (customBg == null) customBg = Resources.Load<Sprite>("Sprites/Fondo_Final");

        GameObject bgObj = new GameObject("CreditsBg", typeof(RectTransform), typeof(Image));
        bgObj.transform.SetParent(creditsRoot.transform, false);
        RectTransform rtBg = bgObj.GetComponent<RectTransform>();
        rtBg.anchorMin = Vector2.zero; rtBg.anchorMax = Vector2.one;
        rtBg.offsetMin = Vector2.zero; rtBg.offsetMax = Vector2.zero;
        Image bgImg = bgObj.GetComponent<Image>();

        if (customBg != null)
        {
            bgImg.sprite = customBg;
            bgImg.color = Color.white;
            bgImg.preserveAspect = false; // Ajustar a toda la pantalla
        }
        else
        {
            bgImg.sprite = white;
            bgImg.color = new Color(0.02f, 0.03f, 0.07f, 1f);
        }

        // Silueta de carretera en la parte inferior
        GameObject road = new GameObject("Road", typeof(RectTransform), typeof(Image));
        road.transform.SetParent(creditsRoot.transform, false);
        RectTransform rtRoad = road.GetComponent<RectTransform>();
        rtRoad.anchorMin = new Vector2(0f, 0f); rtRoad.anchorMax = new Vector2(1f, 0f);
        rtRoad.anchoredPosition = new Vector2(0f, 100f);
        rtRoad.sizeDelta = new Vector2(0f, 200f);
        Image roadImg = road.GetComponent<Image>();
        roadImg.sprite = white;
        roadImg.color = new Color(0.12f, 0.13f, 0.16f, 1f);

        // Linea central de la carretera
        GameObject line = new GameObject("RoadLine", typeof(RectTransform), typeof(Image));
        line.transform.SetParent(road.transform, false);
        RectTransform rtLine = line.GetComponent<RectTransform>();
        rtLine.anchorMin = new Vector2(0f, 0.5f); rtLine.anchorMax = new Vector2(1f, 0.5f);
        rtLine.sizeDelta = new Vector2(0f, 8f);
        Image lineImg = line.GetComponent<Image>();
        lineImg.sprite = white;
        lineImg.color = new Color(1f, 0.8f, 0.1f, 0.7f);

        // Autobus avanzando por la carretera
        GameObject busObj = new GameObject("BusVisual", typeof(RectTransform), typeof(Image));
        busObj.transform.SetParent(creditsRoot.transform, false);
        busTransform = busObj.GetComponent<RectTransform>();
        busTransform.anchoredPosition = new Vector2(-600f, -260f);
        busTransform.sizeDelta = new Vector2(300f, 150f);
        Image busImg = busObj.GetComponent<Image>();

        if (busSprite == null)
        {
            busSprite = Resources.Load<Sprite>("Sprites/Autobus");
        }
        if (busSprite != null)
        {
            busImg.sprite = busSprite;
            busImg.preserveAspect = true;
        }
        else
        {
            busObj.SetActive(false); // Ocultar si falta el sprite

        }

        // Contenedor de texto rodante
        GameObject textCont = new GameObject("CreditsTextContainer", typeof(RectTransform));
        textCont.transform.SetParent(creditsRoot.transform, false);
        creditsTextContainer = textCont.GetComponent<RectTransform>();
        creditsTextContainer.anchorMin = new Vector2(0.5f, 0f);
        creditsTextContainer.anchorMax = new Vector2(0.5f, 0f);
        creditsTextContainer.anchoredPosition = new Vector2(120f, -600f); // Desplazado a la derecha para no tapar el autobus
        creditsTextContainer.sizeDelta = new Vector2(960f, 2200f);

        GameObject creditsTextObj = new GameObject("CreditsText", typeof(RectTransform), typeof(Text));
        creditsTextObj.transform.SetParent(textCont.transform, false);
        RectTransform rtCredits = creditsTextObj.GetComponent<RectTransform>();
        rtCredits.anchorMin = Vector2.zero; rtCredits.anchorMax = Vector2.one;
        rtCredits.offsetMin = Vector2.zero; rtCredits.offsetMax = Vector2.zero;

        Text cText = creditsTextObj.GetComponent<Text>();
        cText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        cText.fontSize = 24;
        cText.lineSpacing = 1.4f;
        cText.alignment = TextAnchor.UpperCenter;
        cText.color = Color.white;

        cText.text =
            "<b><size=46><color=#FFD700>DOLONDRAZO</color></size></b>\n" +
            "<i><size=26><color=#4DEEEA>LA ODISEA DEL CBTIS 75</color></size></i>\n\n\n\n" +

            "<b><size=28><color=#FFB300>CREADOR, DIRECCION Y DISENO DE JUEGO</color></size></b>\n" +
            "<size=32><b>Cesar Fernando Gonzalez Avalos</b></size>\n\n\n" +

            "<b><size=28><color=#FFB300>PROGRAMACION Y SISTEMAS</color></size></b>\n" +
            "<size=30>Cesar Fernando Gonzalez Avalos</size>\n\n\n" +

            "<b><size=28><color=#FFB300>ARTE, SPRITES Y ANIMACIONES</color></size></b>\n" +
            "<size=26>Cesar Fernando Gonzalez Avalos\nHerramientas y Recursos Externos\nSprites Retro 16-Bit Arcade</size>\n\n\n" +

            "<b><size=28><color=#FFB300>MOTOR DE VIDEOJUEGO</color></size></b>\n" +
            "<size=26>Unity 6 (Universal Render Pipeline 2D)</size>\n\n\n" +

            "<b><size=28><color=#FFB300>HERRAMIENTAS DE DESARROLLO</color></size></b>\n" +
            "<size=24>Unity Engine\nVisual Studio\nAseprite & Photoshop</size>\n\n\n" +

            "<b><size=28><color=#FFB300>MUSICA Y EFECTOS DE SONIDO</color></size></b>\n" +
            "<size=24>Efectos de Sonido Arcade 90s\nLibrerias de Audio de Uso Libre</size>\n\n\n" +

            "<b><size=28><color=#FFB300>AGRADECIMIENTOS ESPECIALES</color></size></b>\n" +
            "<size=24>Al CBTis 75 de Dolores Hidalgo\nA la Cuna de la Independencia Nacional, Guanajuato\nA los profesores, companeros de clase y amigos\nY a ti por haber jugado esta aventura</size>\n\n\n\n" +

            "<b><size=38><color=#33FF77>Â¡MISION CUMPLIDA!</color></size></b>\n" +
            "<b><size=30><color=#FFD700>Â¡CALIFICACION DE 10 ASEGURADA!</color></size></b>\n\n\n\n" +

            "<i><size=22><color=#AAAAAA>[ Manten pulsado ESPACIO para acelerar | Pulsa ENTER para volver al Menu ]</color></size></i>\n";

        // Boton Salir al Menu Principal (fijo en la esquina superior derecha)
        CreateButton(creditsRoot.transform, new Vector2(760f, 480f), new Vector2(220f, 50f), "MENU PRINCIPAL",
                     new Color(0.2f, 0.45f, 0.75f, 0.95f), ReturnToMenu);
    }

    private Button CreateButton(Transform parent, Vector2 pos, Vector2 size, string text, Color bgColor, UnityEngine.Events.UnityAction action)
    {
        Texture2D tex = new Texture2D(2, 2);
        tex.SetPixels(new Color[] { Color.white, Color.white, Color.white, Color.white });
        tex.Apply();
        Sprite white = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));

        GameObject btnObj = new GameObject($"Btn_{text}", typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);

        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        Image img = btnObj.GetComponent<Image>();
        img.sprite = white;
        img.color = bgColor;

        Button btn = btnObj.GetComponent<Button>();
        btn.onClick.AddListener(action);

        GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(Text));
        txtObj.transform.SetParent(btnObj.transform, false);
        RectTransform rtT = txtObj.GetComponent<RectTransform>();
        rtT.anchorMin = Vector2.zero; rtT.anchorMax = Vector2.one;
        rtT.offsetMin = Vector2.zero; rtT.offsetMax = Vector2.zero;

        Text t = txtObj.GetComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = 18;
        t.alignment = TextAnchor.MiddleCenter;
        t.fontStyle = FontStyle.Bold;
        t.color = Color.white;
        t.text = text;

        return btn;
    }
}



