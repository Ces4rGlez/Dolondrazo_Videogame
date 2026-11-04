using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// LevelTransitionLoader - Pantalla de carga arcade interactiva entre niveles.
/// Transiciona con elegancia mostrando titulo del nivel, consejos de combate y barra de carga.
/// </summary>
public class LevelTransitionLoader : MonoBehaviour
{
    private static LevelTransitionLoader instance;
    public static LevelTransitionLoader Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject obj = new GameObject("LevelTransitionLoader");
                instance = obj.AddComponent<LevelTransitionLoader>();
                DontDestroyOnLoad(obj);
            }
            return instance;
        }
    }

    private Canvas loadingCanvas;
    private CanvasGroup canvasGroup;
    private Text titleText;
    private Text subtitleText;
    private Text progressText;
    private Image progressBarFill;
    private bool isTransitioning = false;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        BuildLoadingUI();
    }

    private void BuildLoadingUI()
    {
        GameObject canvasObj = new GameObject("Loading_Canvas");
        canvasObj.transform.SetParent(transform, false);

        loadingCanvas = canvasObj.AddComponent<Canvas>();
        loadingCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        loadingCanvas.sortingOrder = 9999; // Siempre al frente de todo

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();
        canvasGroup = canvasObj.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;

        Texture2D tex = new Texture2D(2, 2);
        tex.SetPixels(new Color[] { Color.white, Color.white, Color.white, Color.white });
        tex.Apply();
        Sprite white = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));

        // Fondo oscuro
        GameObject bgObj = new GameObject("Bg", typeof(RectTransform), typeof(Image));
        bgObj.transform.SetParent(canvasObj.transform, false);
        RectTransform rtBg = bgObj.GetComponent<RectTransform>();
        rtBg.anchorMin = Vector2.zero; rtBg.anchorMax = Vector2.one;
        rtBg.offsetMin = Vector2.zero; rtBg.offsetMax = Vector2.zero;
        Image bgImg = bgObj.GetComponent<Image>();
        bgImg.sprite = white;
        bgImg.color = new Color(0.04f, 0.05f, 0.09f, 1f);

        // Contenedor central
        GameObject centerObj = new GameObject("CenterContent", typeof(RectTransform));
        centerObj.transform.SetParent(canvasObj.transform, false);
        RectTransform rtCenter = centerObj.GetComponent<RectTransform>();
        rtCenter.anchorMin = new Vector2(0.5f, 0.5f); rtCenter.anchorMax = new Vector2(0.5f, 0.5f);
        rtCenter.sizeDelta = new Vector2(1000f, 400f);

        // Titulo del nivel
        GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(Text));
        titleObj.transform.SetParent(centerObj.transform, false);
        RectTransform rtTitle = titleObj.GetComponent<RectTransform>();
        rtTitle.anchoredPosition = new Vector2(0f, 80f);
        rtTitle.sizeDelta = new Vector2(900f, 70f);
        titleText = titleObj.GetComponent<Text>();
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 44;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.fontStyle = FontStyle.Bold;
        titleText.color = new Color(1f, 0.85f, 0.2f, 1f);

        // Subtitulo / Consejo
        GameObject subObj = new GameObject("SubtitleText", typeof(RectTransform), typeof(Text));
        subObj.transform.SetParent(centerObj.transform, false);
        RectTransform rtSub = subObj.GetComponent<RectTransform>();
        rtSub.anchoredPosition = new Vector2(0f, 15f);
        rtSub.sizeDelta = new Vector2(900f, 50f);
        subtitleText = subObj.GetComponent<Text>();
        subtitleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        subtitleText.fontSize = 24;
        subtitleText.alignment = TextAnchor.MiddleCenter;
        subtitleText.fontStyle = FontStyle.Italic;
        subtitleText.color = new Color(0.7f, 0.85f, 1f, 0.9f);

        // Marco Barra de Progreso
        GameObject barFrame = new GameObject("BarFrame", typeof(RectTransform), typeof(Image));
        barFrame.transform.SetParent(centerObj.transform, false);
        RectTransform rtBar = barFrame.GetComponent<RectTransform>();
        rtBar.anchoredPosition = new Vector2(0f, -60f);
        rtBar.sizeDelta = new Vector2(600f, 24f);
        Image frameImg = barFrame.GetComponent<Image>();
        frameImg.sprite = white;
        frameImg.color = new Color(0.2f, 0.25f, 0.35f, 1f);

        // Relleno Barra de Progreso
        GameObject barFill = new GameObject("BarFill", typeof(RectTransform), typeof(Image));
        barFill.transform.SetParent(barFrame.transform, false);
        RectTransform rtFill = barFill.GetComponent<RectTransform>();
        rtFill.anchorMin = Vector2.zero; rtFill.anchorMax = Vector2.one;
        rtFill.offsetMin = new Vector2(2f, 2f); rtFill.offsetMax = new Vector2(-2f, -2f);
        progressBarFill = barFill.GetComponent<Image>();
        progressBarFill.sprite = white;
        progressBarFill.type = Image.Type.Filled;
        progressBarFill.fillMethod = Image.FillMethod.Horizontal;
        progressBarFill.fillAmount = 0f;
        progressBarFill.color = new Color(0.2f, 0.9f, 0.45f, 1f);

        // Texto Porcentaje
        GameObject progObj = new GameObject("ProgressText", typeof(RectTransform), typeof(Text));
        progObj.transform.SetParent(centerObj.transform, false);
        RectTransform rtProg = progObj.GetComponent<RectTransform>();
        rtProg.anchoredPosition = new Vector2(0f, -105f);
        rtProg.sizeDelta = new Vector2(400f, 35f);
        progressText = progObj.GetComponent<Text>();
        progressText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        progressText.fontSize = 20;
        progressText.alignment = TextAnchor.MiddleCenter;
        progressText.color = new Color(0.85f, 0.85f, 0.85f, 1f);
    }

    /// <summary>
    /// Inicia la transicion animada hacia una nueva escena con pantalla de carga.
    /// </summary>
    public static void LoadLevel(string sceneName, string levelTitle = "", string tipText = "")
    {
        Instance.StartCoroutine(Instance.TransitionRoutine(sceneName, levelTitle, tipText));
    }

    private IEnumerator TransitionRoutine(string sceneName, string levelTitle, string tipText)
    {
        if (isTransitioning) yield break;
        isTransitioning = true;
        Time.timeScale = 1f;

        // Titulo por defecto si viene vacio
        if (string.IsNullOrEmpty(levelTitle))
        {
            if (sceneName.Contains("1")) levelTitle = "NIVEL 1: CALZADA DE LOS HEROES";
            else if (sceneName.Contains("2")) levelTitle = "NIVEL 2: EL CENTRO Y CINEPOLIS";
            else if (sceneName.Contains("3")) levelTitle = "NIVEL 3: CBTIS 75 (BATALLA FINAL)";
            else if (sceneName.Contains("Credit") || sceneName.Contains("Final")) levelTitle = "FIN DEL JUEGO: CREDITOS";
            else levelTitle = "CARGANDO PARTIDA...";
        }

        if (string.IsNullOrEmpty(tipText))
        {
            if (sceneName.Contains("1")) tipText = "Consejo: Usa [J] para golpear y [K] para patadas devastadoras.";
            else if (sceneName.Contains("2")) tipText = "Consejo: Las alumnas del Rebsamen atacan en grupo. No te dejes acorralar.";
            else if (sceneName.Contains("3")) tipText = "Consejo: Cuidado con el autobus DH-0087 en la avenida.";
            else tipText = "Cargando recursos del juego...";
        }

        if (titleText != null) titleText.text = levelTitle;
        if (subtitleText != null) subtitleText.text = tipText;
        if (progressBarFill != null) progressBarFill.fillAmount = 0f;
        if (progressText != null) progressText.text = "CARGANDO... 0%";

        canvasGroup.blocksRaycasts = true;

        // Fade In de la pantalla de carga (0.35s)
        float elapsed = 0f;
        while (elapsed < 0.35f)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsed / 0.35f);
            yield return null;
        }
        canvasGroup.alpha = 1f;

        // Carga asincrona de la escena
        AsyncOperation async = SceneManager.LoadSceneAsync(sceneName);
        async.allowSceneActivation = false;

        float fakeProgress = 0f;
        while (async.progress < 0.9f || fakeProgress < 1f)
        {
            fakeProgress = Mathf.MoveTowards(fakeProgress, 1f, Time.unscaledDeltaTime * 1.5f);
            float displayPct = Mathf.Min(fakeProgress, async.progress / 0.9f);

            if (progressBarFill != null) progressBarFill.fillAmount = displayPct;
            if (progressText != null) progressText.text = $"CARGANDO... {Mathf.RoundToInt(displayPct * 100f)}%";

            if (displayPct >= 0.99f && async.progress >= 0.9f)
            {
                break;
            }
            yield return null;
        }

        if (progressBarFill != null) progressBarFill.fillAmount = 1f;
        if (progressText != null) progressText.text = "COMPLETADO 100%";
        yield return new WaitForSecondsRealtime(0.2f);

        // Activar escena
        async.allowSceneActivation = true;
        while (!async.isDone) yield return null;

        yield return new WaitForSecondsRealtime(0.15f);

        // Fade Out (0.35s)
        elapsed = 0f;
        while (elapsed < 0.35f)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / 0.35f);
            yield return null;
        }
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        isTransitioning = false;
    }
}

