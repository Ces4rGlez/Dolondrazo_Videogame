using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// GameOverManager - Pantalla de Game Over estilo arcade con estadisticas, animaciones y diseno tematico.
/// Se activa automaticamente cuando Fercho (Player) muere.
/// </summary>
public class GameOverManager : MonoBehaviour
{
    [Header("Configuracion")]
    [SerializeField] private float gameOverDelay = 1.2f;
    [SerializeField] private AudioClip gameOverMusic;

    private GameObject gameOverPanel;
    private Health playerHealth;
    private AudioSource bgmSource;
    private bool gameOverTriggered = false;
    private bool isVisible = false;

    // Componentes de texto dinamicos
    private Text titleText;
    private RectTransform titleTransform;
    private Text statsText;
    private Text scoreBannerText;
    private Text newRecordText;

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerHealth = playerObj.GetComponent<Health>();
            if (playerHealth != null) playerHealth.OnDeath += HandlePlayerDeath;
        }

        bgmSource = GameObject.Find("Musica_Nivel")?.GetComponent<AudioSource>();
        BuildGameOverPanel();
    }

    private void Update()
    {
        if (!isVisible) return;

        // Animacion de respiracion / pulso suave en el titulo "GAME OVER"
        if (titleTransform != null)
        {
            float scale = 1f + Mathf.Sin(Time.unscaledTime * 3.5f) * 0.035f;
            titleTransform.localScale = new Vector3(scale, scale, 1f);
        }

        // Atajos de teclado para reintentar rapido (Espacio, Enter o R)
        bool retryKey = false;
        bool menuKey = false;

        #if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Keyboard.current != null)
        {
            var kb = UnityEngine.InputSystem.Keyboard.current;
            retryKey = kb.spaceKey.wasPressedThisFrame || kb.enterKey.wasPressedThisFrame || kb.rKey.wasPressedThisFrame;
            menuKey  = kb.escapeKey.wasPressedThisFrame;
        }
        #endif

// Input managed via Keyboard.current

        if (retryKey) RestartLevel();
        else if (menuKey) GoToMainMenu();
    }

    private void HandlePlayerDeath()
    {
        if (gameOverTriggered) return;
        gameOverTriggered = true;
        StartCoroutine(ShowGameOverRoutine());
    }

    private IEnumerator ShowGameOverRoutine()
    {
        yield return new WaitForSeconds(gameOverDelay);

        // Asegurar que exista EventSystem para clicks de mouse
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject esObj = new GameObject("EventSystem"); esObj.AddComponent<UnityEngine.EventSystems.EventSystem>(); esObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        // 1. Obtener estadisticas reales de la partida
        int currentScore  = ScoreManager.Instance != null ? ScoreManager.Instance.GetTotalScore() : 0;
        int enemiesKilled = ScoreManager.Instance != null ? ScoreManager.Instance.EnemiesDefeated : 0;
        int cratesBroken  = ScoreManager.Instance != null ? ScoreManager.Instance.CratesBroken : 0;
        float elapsed     = ScoreManager.Instance != null ? ScoreManager.Instance.GetElapsedTime() : 0f;
        string timeStr    = string.Format("{0:00}:{1:00}", (int)(elapsed / 60), (int)(elapsed % 60));

        int savedHigh = PlayerPrefs.GetInt("HighScore", 0);
        bool isNewRecord = false;
        if (currentScore > savedHigh)
        {
            isNewRecord = true;
            savedHigh = currentScore;
            PlayerPrefs.SetInt("HighScore", savedHigh);
            PlayerPrefs.Save();
        }

        // 2. Rellenar los textos de estadisticas
        if (statsText != null)
        {
            statsText.text = $"ENEMIGOS DERROTADOS:       {enemiesKilled}\n" +
                             $"CAJAS DE SUMINISTROS:      {cratesBroken}\n" +
                             $"TIEMPO SOBREVIVIDO:        {timeStr}";
        }

        if (scoreBannerText != null)
        {
            scoreBannerText.text = $"PUNTUACION FINAL: {currentScore:N0} pts      RECORD: {savedHigh:N0} pts";
        }

        if (newRecordText != null)
        {
            newRecordText.gameObject.SetActive(isNewRecord);
        }

        // 3. Audio de Game Over
        if (bgmSource != null && bgmSource.isPlaying) bgmSource.Stop();
        if (gameOverMusic != null)
        {
            AudioSource.PlayClipAtPoint(gameOverMusic, Camera.main.transform.position, 1f);
        }

        // 4. Activar panel y animar aparicion suave
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            isVisible = true;
            yield return StartCoroutine(FadeInPanel());
        }

        yield return new WaitForSecondsRealtime(0.1f);
        Time.timeScale = 0f;
    }

    private IEnumerator FadeInPanel()
    {
        CanvasGroup cg = gameOverPanel.GetComponent<CanvasGroup>();
        if (cg == null) cg = gameOverPanel.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        float elapsed = 0f;
        while (elapsed < 0.6f)
        {
            elapsed += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Clamp01(elapsed / 0.6f);
            yield return null;
        }
        cg.alpha = 1f;
    }

    private void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void GoToMainMenu()
    {
        Time.timeScale = 1f;
        if (Application.CanStreamedLevelBeLoaded("Pantalla_Inicio"))
            SceneManager.LoadScene("Pantalla_Inicio");
        else
            SceneManager.LoadScene(0);
    }

    // -------------------------------------------------------
    // Construccion de la Interfaz Visual (Estilo Arcade)
    // -------------------------------------------------------

    private void BuildGameOverPanel()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        Texture2D tex = new Texture2D(4, 4);
        Color[] px = new Color[16];
        for (int i = 0; i < px.Length; i++) px[i] = Color.white;
        tex.SetPixels(px); tex.Apply();
        Sprite white = Sprite.Create(tex, new Rect(0,0,4,4), new Vector2(0.5f,0.5f));

        // === 1. FONDO DE PANTALLA COMPLETO (Vignette carmesi oscuro) ===
        gameOverPanel = new GameObject("GameOver_Panel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        gameOverPanel.transform.SetParent(canvas.transform, false);
        RectTransform rtPanel = gameOverPanel.GetComponent<RectTransform>();
        rtPanel.anchorMin = Vector2.zero; rtPanel.anchorMax = Vector2.one;
        rtPanel.offsetMin = Vector2.zero; rtPanel.offsetMax = Vector2.zero;
        Image panelBg = gameOverPanel.GetComponent<Image>();
        panelBg.sprite = white;
        panelBg.color  = new Color(0.04f, 0.01f, 0.02f, 0.95f);

        // Barras de acento carmesi superior e inferior de pantalla
        MakeBar(gameOverPanel.transform, white, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 6f),
                new Color(0.92f, 0.15f, 0.18f, 1f), "RedBar_Top");

        MakeBar(gameOverPanel.transform, white, new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 6f),
                new Color(0.92f, 0.15f, 0.18f, 1f), "RedBar_Bottom");

        // === 2. TARJETA CENTRAL ARCADE (880 x 580 px) ===
        GameObject cardObj = new GameObject("Card_GameOver", typeof(RectTransform), typeof(Image));
        cardObj.transform.SetParent(gameOverPanel.transform, false);
        RectTransform rc = cardObj.GetComponent<RectTransform>();
        rc.anchorMin = new Vector2(0.5f, 0.5f); rc.anchorMax = new Vector2(0.5f, 0.5f);
        rc.pivot     = new Vector2(0.5f, 0.5f);
        rc.anchoredPosition = Vector2.zero;
        rc.sizeDelta = new Vector2(880f, 580f);
        Image cardBg = cardObj.GetComponent<Image>();
        cardBg.sprite = white;
        cardBg.color  = new Color(0.08f, 0.05f, 0.08f, 0.98f);

        // Borde superior brillante carmesi
        MakeBar(cardObj.transform, white, new Vector2(0f,1f), new Vector2(1f,1f),
                new Vector2(0.5f,1f), Vector2.zero, new Vector2(0f, 6f),
                new Color(1f, 0.2f, 0.25f, 1f), "Card_TopShine");

        // === 3. TITULO "GAME OVER" (Intenso y con animacion de pulso) ===
        GameObject titleObj = new GameObject("Text_GameOver", typeof(RectTransform), typeof(Text));
        titleObj.transform.SetParent(cardObj.transform, false);
        titleTransform = titleObj.GetComponent<RectTransform>();
        titleTransform.anchorMin = new Vector2(0f, 1f); titleTransform.anchorMax = new Vector2(1f, 1f);
        titleTransform.pivot     = new Vector2(0.5f, 1f);
        titleTransform.anchoredPosition = new Vector2(0f, -22f);
        titleTransform.sizeDelta        = new Vector2(0f, 60f);

        titleText = titleObj.GetComponent<Text>();
        titleText.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.text      = "G A M E   O V E R";
        titleText.fontSize  = 54;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color     = new Color(1f, 0.22f, 0.2f, 1f);

        // Subtitulo tematico dramático
        MakeText(cardObj.transform, "!PROYECTO NO ENTREGADO! - FERCHO HA SIDO DERROTADO",
                 new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                 new Vector2(0f, -82f), new Vector2(0f, 30f),
                 20, FontStyle.Bold, TextAnchor.MiddleCenter,
                 new Color(1f, 0.78f, 0.2f, 1f), "Text_Tagline");

        MakeText(cardObj.transform, "El CBTis 75 aun espera tu proyecto final... !Levantate y pelea!",
                 new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                 new Vector2(0f, -112f), new Vector2(0f, 26f),
                 16, FontStyle.Italic, TextAnchor.MiddleCenter,
                 new Color(0.75f, 0.75f, 0.8f, 1f), "Text_Flavour");

        // === 4. PANEL DE ESTADISTICAS DE LA PARTIDA ===
        GameObject statsBox = new GameObject("Stats_Box", typeof(RectTransform), typeof(Image));
        statsBox.transform.SetParent(cardObj.transform, false);
        RectTransform rsb = statsBox.GetComponent<RectTransform>();
        rsb.anchorMin = new Vector2(0.5f, 0.5f); rsb.anchorMax = new Vector2(0.5f, 0.5f);
        rsb.pivot     = new Vector2(0.5f, 0.5f);
        rsb.anchoredPosition = new Vector2(0f, 30f);
        rsb.sizeDelta        = new Vector2(780f, 140f);
        Image sbBg = statsBox.GetComponent<Image>();
        sbBg.sprite = white;
        sbBg.color  = new Color(0.12f, 0.08f, 0.12f, 0.92f);

        // Linea izquierda roja en el panel de stats
        MakeBar(statsBox.transform, white, new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(0f, 0.5f), Vector2.zero, new Vector2(5f, 0f),
                new Color(0.95f, 0.2f, 0.25f, 1f), "LeftBar");

        statsText = MakeText(statsBox.transform,
                 "ENEMIGOS DERROTADOS:       0\nCAJAS DE SUMINISTROS:      0\nTIEMPO SOBREVIVIDO:        00:00",
                 Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                 new Vector2(35f, 0f), new Vector2(-70f, 0f),
                 22, FontStyle.Bold, TextAnchor.MiddleLeft,
                 new Color(0.92f, 0.95f, 1f, 1f), "Text_StatsDetails");

        // === 5. PANEL DE PUNTUACION Y RECORD ===
        GameObject scoreBox = new GameObject("Score_Box", typeof(RectTransform), typeof(Image));
        scoreBox.transform.SetParent(cardObj.transform, false);
        RectTransform rsc = scoreBox.GetComponent<RectTransform>();
        rsc.anchorMin = new Vector2(0.5f, 0.5f); rsc.anchorMax = new Vector2(0.5f, 0.5f);
        rsc.pivot     = new Vector2(0.5f, 0.5f);
        rsc.anchoredPosition = new Vector2(0f, -85f);
        rsc.sizeDelta        = new Vector2(780f, 65f);
        Image scBg = scoreBox.GetComponent<Image>();
        scBg.sprite = white;
        scBg.color  = new Color(0.04f, 0.03f, 0.06f, 0.96f);

        scoreBannerText = MakeText(scoreBox.transform, "PUNTUACION FINAL: 0 pts      RECORD: 0 pts",
                 Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                 Vector2.zero, Vector2.zero,
                 24, FontStyle.Bold, TextAnchor.MiddleCenter,
                 new Color(1f, 0.88f, 0.2f, 1f), "Text_ScoreBanner");

        // Etiqueta de Nuevo Record
        GameObject recordBadgeObj = new GameObject("Text_NewRecordBadge", typeof(RectTransform), typeof(Text));
        recordBadgeObj.transform.SetParent(cardObj.transform, false);
        RectTransform rrb = recordBadgeObj.GetComponent<RectTransform>();
        rrb.anchorMin = new Vector2(0.5f, 0.5f); rrb.anchorMax = new Vector2(0.5f, 0.5f);
        rrb.pivot     = new Vector2(0.5f, 0.5f);
        rrb.anchoredPosition = new Vector2(0f, -135f);
        rrb.sizeDelta        = new Vector2(780f, 28f);
        newRecordText = recordBadgeObj.GetComponent<Text>();
        newRecordText.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        newRecordText.text      = "*** !NUEVO RECORD HISTORICO REGISTRADO! ***";
        newRecordText.fontSize  = 18;
        newRecordText.fontStyle = FontStyle.Bold;
        newRecordText.alignment = TextAnchor.MiddleCenter;
        newRecordText.color     = new Color(1f, 0.85f, 0.2f, 1f);
        recordBadgeObj.SetActive(false);

        // === 6. BOTONES DE ACCION ARCADE ===
        // Boton 1: Reintentar Nivel (Espacio / Enter / R)
        MakeButton(cardObj.transform, white, new Vector2(-180f, -215f), new Vector2(340f, 68f),
                   "|| REINTENTAR (ESPACIO)",
                   new Color(0.85f, 0.12f, 0.15f, 1f),
                   new Color(1f, 0.25f, 0.28f, 1f),
                   "Btn_Retry",
                   RestartLevel);

        // Boton 2: Menu Principal (Esc)
        MakeButton(cardObj.transform, white, new Vector2(180f, -215f), new Vector2(340f, 68f),
                   "<< MENU PRINCIPAL (ESC)",
                   new Color(0.16f, 0.16f, 0.26f, 1f),
                   new Color(0.28f, 0.28f, 0.42f, 1f),
                   "Btn_Menu",
                   GoToMainMenu);

        gameOverPanel.SetActive(false);
    }

    // -------------------------------------------------------
    // Helpers de construccion de UI
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
                          Color color, string name)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Text));
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = ancMin; rt.anchorMax = ancMax; rt.pivot = pivot;
        rt.anchoredPosition = aPos; rt.sizeDelta = sizeDelta;
        Text t = obj.GetComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.text = content; t.fontSize = fontSize;
        t.fontStyle = style; t.alignment = align; t.color = color;
        return t;
    }

    private void MakeButton(Transform parent, Sprite white, Vector2 aPos, Vector2 size,
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
        cb.pressedColor     = new Color(normalColor.r * 0.6f, normalColor.g * 0.6f, normalColor.b * 0.6f, 1f);
        btn.colors = cb;
        btn.onClick.AddListener(action);

        // Borde superior brillante de estilo cristal/arcade
        MakeBar(btnObj.transform, white, new Vector2(0f,1f), new Vector2(1f,1f),
                new Vector2(0.5f,1f), Vector2.zero, new Vector2(0f,3f),
                new Color(1f,1f,1f,0.25f), "TopShine");

        MakeText(btnObj.transform, label,
                 Vector2.zero, Vector2.one, new Vector2(0.5f,0.5f),
                 Vector2.zero, Vector2.zero,
                 20, FontStyle.Bold, TextAnchor.MiddleCenter,
                 Color.white, "Btn_Label");
    }

    private void OnDestroy()
    {
        if (playerHealth != null) playerHealth.OnDeath -= HandlePlayerDeath;
    }
}
