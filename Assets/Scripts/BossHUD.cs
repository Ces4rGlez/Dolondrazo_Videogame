using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class BossHUD : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Health bossHealth;
    [SerializeField] private BossAI bossAI;
    [SerializeField] private Sprite ferChoPortrait;

    [Header("Componentes de UI")]
    [SerializeField] private GameObject bossBarContainer;
    [SerializeField] private Image bossFillBar;
    [SerializeField] private Text bossNameText;
    [SerializeField] private Text bossHpText;

    [Header("Configuracion")]
    [SerializeField] private float smoothSpeed = 6f;

    private float targetFill = 1f;
    private float currentFill = 1f;
    private int lastCurrentHp = 180;
    private int lastMaxHp = 180;
    private Coroutine victoryWatchCoroutine;
    private GameObject resultsScreenObj;
    private Text nextLevelStatusText;

    private void Awake()
    {
        EnsureCanvasAndUI();
    }

    private void Start()
    {
        EnsureCanvasAndUI();

        if (bossAI == null) bossAI = FindFirstObjectByType<BossAI>();
        if (bossAI != null && bossHealth == null) bossHealth = bossAI.GetComponent<Health>();

        if (bossBarContainer != null) bossBarContainer.SetActive(false);
    }

    public void ActivateBossBar(Health healthRef, string name)
    {
        EnsureCanvasAndUI();

        bossHealth = healthRef;
        if (bossHealth != null)
        {
            bossHealth.OnHealthChanged -= HandleBossHealthChanged;
            bossHealth.OnHealthChanged += HandleBossHealthChanged;
            lastCurrentHp = bossHealth.CurrentHealth;
            lastMaxHp = bossHealth.MaxHealth;
        }

        if (bossNameText != null)
            bossNameText.text = "BOSS: " + name.ToUpper();

        targetFill = 1f;
        currentFill = 1f;

        if (bossFillBar != null) bossFillBar.fillAmount = 1f;

        UpdateHpDisplay(lastCurrentHp, lastMaxHp);

        if (bossBarContainer != null) bossBarContainer.SetActive(true);
    }

    private void HandleBossHealthChanged(int current, int max)
    {
        lastCurrentHp = current;
        lastMaxHp = max;
        targetFill = Mathf.Clamp01((float)current / Mathf.Max(1, max));
        UpdateHpDisplay(current, max);
    }

    private void UpdateHpDisplay(int current, int max)
    {
        if (bossHpText != null)
        {
            int pct = Mathf.RoundToInt(targetFill * 100f);
            bossHpText.text = $"{Mathf.Max(0, current)} / {max} HP  ({pct}%)";
        }
    }

    private void Update()
    {
        if (bossBarContainer != null && bossBarContainer.activeSelf && bossFillBar != null)
        {
            currentFill = Mathf.MoveTowards(currentFill, targetFill, Time.deltaTime * smoothSpeed);
            bossFillBar.fillAmount = currentFill;
        }
    }

    public void TriggerVictorySequence(AudioClip victoryClip, AudioSource bgmSource)
    {
        if (victoryWatchCoroutine != null) StopCoroutine(victoryWatchCoroutine);
        victoryWatchCoroutine = StartCoroutine(WaitForAllEnemiesDefeatedRoutine(victoryClip, bgmSource));
    }

    private IEnumerator WaitForAllEnemiesDefeatedRoutine(AudioClip victoryClip, AudioSource bgmSource)
    {
        yield return new WaitForSeconds(0.5f);

        while (true)
        {
            EnemyAI[] remainingEnemies = FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);
            int aliveCount = 0;
            foreach (var enemy in remainingEnemies)
            {
                if (enemy != null && enemy.gameObject.activeInHierarchy)
                {
                    Health h = enemy.GetComponent<Health>();
                    if (h != null && !h.IsDead) aliveCount++;
                }
            }

            if (aliveCount == 0)
            {
                // 1. Ocultar la barra del jefe
                if (bossBarContainer != null) bossBarContainer.SetActive(false);

                // 2. Ejecutar dialogo de cierre narrativo de Fercho
                yield return StartCoroutine(PlayOutroDialogueRoutine());

                // 3. Cambiar musica a tema de victoria
                if (bgmSource != null && victoryClip != null)
                {
                    bgmSource.Stop();
                    bgmSource.clip = victoryClip;
                    bgmSource.loop = false;
                    bgmSource.Play();
                }

                // 4. Mostrar pantalla interactiva de resultados
                ShowResultsScreen();
                yield break;
            }

            yield return new WaitForSeconds(0.5f);
        }
    }

    private IEnumerator PlayOutroDialogueRoutine()
    {
        if (DialogueSystem.Instance == null) yield break;

        // Intentar obtener el portrait de Fercho si aun no se ha configurado
        if (ferChoPortrait == null)
        {
            BossCutscene bc = FindFirstObjectByType<BossCutscene>();
            if (bc != null)
            {
                var field = typeof(BossCutscene).GetField("ferChoPortrait", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null) ferChoPortrait = field.GetValue(bc) as Sprite;
            }
        }
        if (ferChoPortrait == null)
        {
            PlayerController pc = FindFirstObjectByType<PlayerController>();
            if (pc != null)
            {
                SpriteRenderer sr = pc.GetComponent<SpriteRenderer>();
                if (sr != null) ferChoPortrait = sr.sprite;
            }
        }

        Color ferChoColor = new Color(0.3f, 0.75f, 1f, 1f);
        Color narratorColor = new Color(0.85f, 0.85f, 0.95f, 1f);

        var lines = new List<DialogueSystem.DialogueLine>
        {
            new DialogueSystem.DialogueLine
            {
                speakerName = "Fercho",
                portrait    = ferChoPortrait,
                nameColor   = ferChoColor,
                text        = "!Uff... por poco! Ese tipo de taller casi me hace tirar la maqueta.",
                typeSpeed   = 0.04f
            },
            new DialogueSystem.DialogueLine
            {
                speakerName = "Fercho",
                portrait    = ferChoPortrait,
                nameColor   = ferChoColor,
                text        = "*mira su reloj* !No puede ser, se me acaba el tiempo! Apenas voy saliendo de la Calzada de los Heroes...",
                typeSpeed   = 0.04f
            },
            new DialogueSystem.DialogueLine
            {
                speakerName = "Fercho",
                portrait    = ferChoPortrait,
                nameColor   = ferChoColor,
                text        = "El transporte publico sigue totalmente detenido. Mi unica opcion es cruzar corriendo por el Jardin Principal hacia el CBTis 75.",
                typeSpeed   = 0.04f
            },
            new DialogueSystem.DialogueLine
            {
                speakerName = "Narrador",
                portrait    = null,
                nameColor   = narratorColor,
                text        = "!ACTO I COMPLETADO! Fercho avanza a toda velocidad rumbo al Centro de Dolores Hidalgo...",
                typeSpeed   = 0.038f
            }
        };

        bool finished = false;
        DialogueSystem.Instance.PlayDialogue(lines, () => finished = true);

        while (!finished) yield return null;
    }

    public void ShowResultsScreen()
    {
        EnsureCanvasAndUI();

        // Asegurar que exista un EventSystem para que los botones respondan a clicks
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem"); es.AddComponent<UnityEngine.EventSystems.EventSystem>(); es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        if (resultsScreenObj != null) Destroy(resultsScreenObj);

        Texture2D tex = new Texture2D(4, 4);
        Color[] px = new Color[16];
        for (int i = 0; i < px.Length; i++) px[i] = Color.white;
        tex.SetPixels(px); tex.Apply();
        Sprite white = Sprite.Create(tex, new Rect(0,0,4,4), new Vector2(0.5f,0.5f));

        // Datos y estadisticas de la partida
        int finalScore = ScoreManager.Instance != null ? ScoreManager.Instance.GetTotalScore() : 0;
        int enemiesKilled = ScoreManager.Instance != null ? ScoreManager.Instance.EnemiesDefeated : 0;
        int cratesBroken = ScoreManager.Instance != null ? ScoreManager.Instance.CratesBroken : 0;
        float elapsed = ScoreManager.Instance != null ? ScoreManager.Instance.GetElapsedTime() : 0f;
        string timeStr = string.Format("{0:00}:{1:00}", (int)(elapsed / 60), (int)(elapsed % 60));

        Health playerH = GameObject.FindGameObjectWithTag("Player")?.GetComponent<Health>();
        int hpPct = playerH != null ? Mathf.RoundToInt(((float)playerH.CurrentHealth / Mathf.Max(1, playerH.MaxHealth)) * 100f) : 100;

        int highScore = PlayerPrefs.GetInt("HighScore", 0);
        if (finalScore > highScore)
        {
            highScore = finalScore;
            PlayerPrefs.SetInt("HighScore", highScore);
            PlayerPrefs.Save();
        }

        // --- BACKDROP COMPLETO ---
        resultsScreenObj = new GameObject("Results_Screen", typeof(RectTransform), typeof(Image));
        resultsScreenObj.transform.SetParent(canvas.transform, false);
        RectTransform rtFull = resultsScreenObj.GetComponent<RectTransform>();
        rtFull.anchorMin = Vector2.zero; rtFull.anchorMax = Vector2.one;
        rtFull.offsetMin = Vector2.zero; rtFull.offsetMax = Vector2.zero;
        Image fullBg = resultsScreenObj.GetComponent<Image>();
        fullBg.sprite = white;
        fullBg.color = new Color(0.02f, 0.03f, 0.07f, 0.94f);

        // --- TARJETA CENTRAL DE RESULTADOS (880 x 580) ---
        GameObject cardObj = new GameObject("Results_Card", typeof(RectTransform), typeof(Image));
        cardObj.transform.SetParent(resultsScreenObj.transform, false);
        RectTransform rc = cardObj.GetComponent<RectTransform>();
        rc.anchorMin = new Vector2(0.5f, 0.5f); rc.anchorMax = new Vector2(0.5f, 0.5f);
        rc.pivot = new Vector2(0.5f, 0.5f);
        rc.anchoredPosition = Vector2.zero; rc.sizeDelta = new Vector2(880f, 580f);
        Image cardBg = cardObj.GetComponent<Image>();
        cardBg.sprite = white; cardBg.color = new Color(0.06f, 0.08f, 0.14f, 0.98f);

        // Borde superior dorado
        MakeBar(cardObj.transform, white, new Vector2(0f,1f), new Vector2(1f,1f), new Vector2(0.5f,1f),
                Vector2.zero, new Vector2(0f, 6f), new Color(1f, 0.84f, 0f, 1f), "TopGold");

        // Titulo STAGE CLEAR
        MakeText(cardObj.transform, "*** STAGE 1 CLEAR! ***",
                 new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                 new Vector2(0f, -25f), new Vector2(0f, 50f),
                 38, FontStyle.Bold, TextAnchor.MiddleCenter,
                 new Color(1f, 0.88f, 0.2f, 1f), "Text_Title");

        // Subtitulo Nivel y Jefe derrotado
        MakeText(cardObj.transform, "CALZADA DE LOS HEROES - ALUMNO DE TALLER DERROTADO",
                 new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                 new Vector2(0f, -75f), new Vector2(0f, 32f),
                 20, FontStyle.Bold, TextAnchor.MiddleCenter,
                 new Color(0.35f, 0.85f, 1f, 1f), "Text_SubTitle");

        // --- PANEL DE ESTADISTICAS ---
        GameObject statsBox = new GameObject("Stats_Box", typeof(RectTransform), typeof(Image));
        statsBox.transform.SetParent(cardObj.transform, false);
        RectTransform rsb = statsBox.GetComponent<RectTransform>();
        rsb.anchorMin = new Vector2(0.5f, 0.5f); rsb.anchorMax = new Vector2(0.5f, 0.5f);
        rsb.pivot = new Vector2(0.5f, 0.5f);
        rsb.anchoredPosition = new Vector2(0f, 40f); rsb.sizeDelta = new Vector2(780f, 170f);
        Image sbBg = statsBox.GetComponent<Image>();
        sbBg.sprite = white; sbBg.color = new Color(0.1f, 0.13f, 0.2f, 0.9f);

        string statsString = $"ENEMIGOS DERROTADOS:       {enemiesKilled}\n" +
                             $"CAJAS DE SUMINISTROS:      {cratesBroken}\n" +
                             $"BATERIA / SALUD FERCHO:   {hpPct}%\n" +
                             $"TIEMPO DE RECORRIDO:       {timeStr}";

        MakeText(statsBox.transform, statsString,
                 Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                 new Vector2(35f, 0f), new Vector2(-70f, 0f),
                 22, FontStyle.Bold, TextAnchor.MiddleLeft,
                 new Color(0.92f, 0.95f, 1f, 1f), "Text_StatsDetails");

        // --- PANEL DE SCORE Y RECORD ---
        GameObject scoreBox = new GameObject("Score_Box", typeof(RectTransform), typeof(Image));
        scoreBox.transform.SetParent(cardObj.transform, false);
        RectTransform rsc = scoreBox.GetComponent<RectTransform>();
        rsc.anchorMin = new Vector2(0.5f, 0.5f); rsc.anchorMax = new Vector2(0.5f, 0.5f);
        rsc.pivot = new Vector2(0.5f, 0.5f);
        rsc.anchoredPosition = new Vector2(0f, -95f); rsc.sizeDelta = new Vector2(780f, 75f);
        Image scBg = scoreBox.GetComponent<Image>();
        scBg.sprite = white; scBg.color = new Color(0.04f, 0.06f, 0.1f, 0.95f);

        MakeText(scoreBox.transform, $"PUNTUACION TOTAL: {finalScore:N0} pts      RECORD: {highScore:N0} pts",
                 Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                 Vector2.zero, Vector2.zero,
                 26, FontStyle.Bold, TextAnchor.MiddleCenter,
                 new Color(1f, 0.88f, 0.2f, 1f), "Text_ScoreBanner");

        // Texto de estatus / aviso si no hay siguiente escena
        GameObject statusObj = new GameObject("Text_Status", typeof(RectTransform), typeof(Text));
        statusObj.transform.SetParent(cardObj.transform, false);
        RectTransform rst = statusObj.GetComponent<RectTransform>();
        rst.anchorMin = new Vector2(0.5f, 0.5f); rst.anchorMax = new Vector2(0.5f, 0.5f);
        rst.pivot = new Vector2(0.5f, 0.5f);
        rst.anchoredPosition = new Vector2(0f, -150f); rst.sizeDelta = new Vector2(780f, 32f);
        nextLevelStatusText = statusObj.GetComponent<Text>();
        nextLevelStatusText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        nextLevelStatusText.text = "";
        nextLevelStatusText.fontSize = 18;
        nextLevelStatusText.fontStyle = FontStyle.Bold;
        nextLevelStatusText.alignment = TextAnchor.MiddleCenter;
        nextLevelStatusText.color = new Color(0.4f, 1f, 0.4f, 1f);

        // --- BOTONES INTERACTIVOS ---
        // 1. Continuar al Nivel 2
        MakeButton(cardObj.transform, white, new Vector2(-260f, -220f), new Vector2(230f, 65f),
                   ">> NIVEL 2", new Color(0.15f, 0.65f, 0.35f, 1f), new Color(0.25f, 0.85f, 0.45f, 1f),
                   "Btn_NextLevel", () =>
                   {
                       int nextIdx = SceneManager.GetActiveScene().buildIndex + 1;
                       if (nextIdx < SceneManager.sceneCountInBuildSettings)
                       {
                           Time.timeScale = 1f;
                           SceneManager.LoadScene(nextIdx);
                       }
                       else
                       {
                           if (nextLevelStatusText != null)
                           {
                               nextLevelStatusText.text = "!NIVEL 2 (EL CENTRO) EN DESARROLLO! !Has completado con exito el Acto 1!";
                               nextLevelStatusText.color = new Color(1f, 0.85f, 0.2f, 1f);
                           }
                       }
                   });

        // 2. Reiniciar Nivel
        MakeButton(cardObj.transform, white, new Vector2(0f, -220f), new Vector2(230f, 65f),
                   "|| REINICIAR", new Color(0.2f, 0.45f, 0.8f, 1f), new Color(0.3f, 0.6f, 0.95f, 1f),
                   "Btn_Restart", () =>
                   {
                       Time.timeScale = 1f;
                       SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                   });

        // 3. Menu Principal
        MakeButton(cardObj.transform, white, new Vector2(260f, -220f), new Vector2(230f, 65f),
                   "<< MENU", new Color(0.25f, 0.2f, 0.35f, 1f), new Color(0.4f, 0.3f, 0.55f, 1f),
                   "Btn_Menu", () =>
                   {
                       Time.timeScale = 1f;
                       SceneManager.LoadScene(0);
                   });
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

        MakeBar(btnObj.transform, white, new Vector2(0f,1f), new Vector2(1f,1f),
                new Vector2(0.5f,1f), Vector2.zero, new Vector2(0f,3f),
                new Color(1f,1f,1f,0.25f), "TopShine");

        MakeText(btnObj.transform, label,
                 Vector2.zero, Vector2.one, new Vector2(0.5f,0.5f),
                 Vector2.zero, Vector2.zero,
                 22, FontStyle.Bold, TextAnchor.MiddleCenter,
                 Color.white, "Btn_Label");
    }

    private void EnsureCanvasAndUI()
    {
        if (bossBarContainer != null) return;

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("HUD_Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        Texture2D tex = new Texture2D(4, 4);
        Color[] pixels = new Color[16];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();
        Sprite whiteSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));

        bossBarContainer = new GameObject("Boss_Health_HUD", typeof(RectTransform), typeof(Image));
        bossBarContainer.transform.SetParent(canvas.transform, false);

        RectTransform rectBar = bossBarContainer.GetComponent<RectTransform>();
        rectBar.anchorMin = new Vector2(0.5f, 1f);
        rectBar.anchorMax = new Vector2(0.5f, 1f);
        rectBar.pivot = new Vector2(0.5f, 1f);
        rectBar.anchoredPosition = new Vector2(0f, -25f);
        rectBar.sizeDelta = new Vector2(800f, 80f);

        Image bg = bossBarContainer.GetComponent<Image>();
        bg.sprite = whiteSprite;
        bg.color = new Color(0.06f, 0.08f, 0.12f, 0.95f);

        GameObject borderTop = new GameObject("Gold_Border", typeof(RectTransform), typeof(Image));
        borderTop.transform.SetParent(bossBarContainer.transform, false);
        RectTransform rb2 = borderTop.GetComponent<RectTransform>();
        rb2.anchorMin = new Vector2(0f, 1f);
        rb2.anchorMax = new Vector2(1f, 1f);
        rb2.pivot = new Vector2(0.5f, 1f);
        rb2.anchoredPosition = Vector2.zero;
        rb2.sizeDelta = new Vector2(0f, 5f);
        Image imgBorder = borderTop.GetComponent<Image>();
        imgBorder.sprite = whiteSprite;
        imgBorder.color = new Color(1f, 0.84f, 0f, 1f);

        GameObject nameObj = new GameObject("Text_BossName", typeof(RectTransform), typeof(Text));
        nameObj.transform.SetParent(bossBarContainer.transform, false);
        RectTransform rn = nameObj.GetComponent<RectTransform>();
        rn.anchorMin = new Vector2(0f, 1f);
        rn.anchorMax = new Vector2(0.6f, 1f);
        rn.pivot = new Vector2(0f, 1f);
        rn.anchoredPosition = new Vector2(20f, -8f);
        rn.sizeDelta = new Vector2(0f, 28f);
        bossNameText = nameObj.GetComponent<Text>();
        bossNameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        bossNameText.text = "BOSS";
        bossNameText.fontSize = 22;
        bossNameText.fontStyle = FontStyle.Bold;
        bossNameText.alignment = TextAnchor.MiddleLeft;
        bossNameText.color = new Color(1f, 0.84f, 0f, 1f);

        GameObject hpTextObj = new GameObject("Text_BossHP", typeof(RectTransform), typeof(Text));
        hpTextObj.transform.SetParent(bossBarContainer.transform, false);
        RectTransform rh = hpTextObj.GetComponent<RectTransform>();
        rh.anchorMin = new Vector2(0.5f, 1f);
        rh.anchorMax = new Vector2(1f, 1f);
        rh.pivot = new Vector2(1f, 1f);
        rh.anchoredPosition = new Vector2(-20f, -8f);
        rh.sizeDelta = new Vector2(0f, 28f);
        bossHpText = hpTextObj.GetComponent<Text>();
        bossHpText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        bossHpText.text = "180 / 180 HP (100%)";
        bossHpText.fontSize = 18;
        bossHpText.fontStyle = FontStyle.Bold;
        bossHpText.alignment = TextAnchor.MiddleRight;
        bossHpText.color = new Color(0.95f, 0.95f, 0.95f, 1f);

        GameObject barBg = new GameObject("Boss_Bar_Bg", typeof(RectTransform), typeof(Image));
        barBg.transform.SetParent(bossBarContainer.transform, false);
        RectTransform rbb = barBg.GetComponent<RectTransform>();
        rbb.anchorMin = new Vector2(0f, 0f);
        rbb.anchorMax = new Vector2(1f, 0f);
        rbb.pivot = new Vector2(0.5f, 0f);
        rbb.anchoredPosition = new Vector2(0f, 10f);
        rbb.sizeDelta = new Vector2(-40f, 28f);
        Image imgBg = barBg.GetComponent<Image>();
        imgBg.sprite = whiteSprite;
        imgBg.color = new Color(0.25f, 0.05f, 0.05f, 1f);

        GameObject fillObj = new GameObject("Boss_Bar_Fill", typeof(RectTransform), typeof(Image));
        fillObj.transform.SetParent(barBg.transform, false);
        RectTransform rf = fillObj.GetComponent<RectTransform>();
        rf.anchorMin = Vector2.zero;
        rf.anchorMax = Vector2.one;
        rf.offsetMin = Vector2.zero;
        rf.offsetMax = Vector2.zero;

        bossFillBar = fillObj.GetComponent<Image>();
        bossFillBar.sprite = whiteSprite;
        bossFillBar.type = Image.Type.Filled;
        bossFillBar.fillMethod = Image.FillMethod.Horizontal;
        bossFillBar.fillOrigin = 0;
        bossFillBar.fillAmount = 1f;
        bossFillBar.color = new Color(0.9f, 0.15f, 0.1f, 1f);

        bossBarContainer.SetActive(false);
    }

    private void OnDestroy()
    {
        if (bossHealth != null)
            bossHealth.OnHealthChanged -= HandleBossHealthChanged;
    }
}
