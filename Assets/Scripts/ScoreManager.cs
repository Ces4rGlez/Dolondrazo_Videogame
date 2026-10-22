using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ScoreManager - Sistema de puntuacion arcade, multiplicadores de combo y registro de records.
/// Fuentes de alta definicion (ArcadeBold) con contornos nitidos.
/// </summary>
public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("Puntuacion Base")]
    [SerializeField] private int minionPoints = 100;
    [SerializeField] private int bossPoints   = 1000;

    [Header("Sistema de Combo")]
    [SerializeField] private float comboWindowSeconds = 3f;

    public int EnemiesDefeated { get; private set; } = 0;
    public int CratesBroken    { get; private set; } = 0;
    public float LevelStartTime { get; private set; }

    private int totalScore = 0;
    private int comboCount = 0;
    private float comboTimer = 0f;
    private bool comboActive = false;

    // Componentes de UI
    private Text scoreValueText;
    private Text scoreTitleText;
    private GameObject comboObj;
    private Text comboText;
    private Coroutine comboPulseCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        LevelStartTime = Time.time;
    }

    private void Start()
    {
        BuildScoreHUD();
        UpdateScoreDisplay();
    }

    private void Update()
    {
        if (!comboActive) return;

        comboTimer -= Time.deltaTime;
        if (comboTimer <= 0f)
        {
            ResetCombo();
        }
    }

    public void RegisterEnemyDefeated(int points = -1)
    {
        EnemiesDefeated++;
        int pts = (points > 0) ? points : minionPoints;
        AddScore(pts, "ENEMIGO");
    }

    public void RegisterBossDefeated(int points = -1)
    {
        EnemiesDefeated++;
        int pts = (points > 0) ? points : bossPoints;
        AddScore(pts, "JEFE DERROTADO");
    }

    public void RegisterCrateBroken(int points = 25)
    {
        CratesBroken++;
        AddScore(points, "CAJA");
    }

    public void AddMinionScore() => RegisterEnemyDefeated(minionPoints);
    public void AddBossScore()   => RegisterBossDefeated(bossPoints);

    public void AddScore(int basePoints, string label = "")
    {
        comboCount++;
        comboTimer  = comboWindowSeconds;
        comboActive = true;

        float multiplier = Mathf.Min(1f + (comboCount - 1) * 0.5f, 5f);
        int earned       = Mathf.RoundToInt(basePoints * multiplier);
        totalScore      += earned;

        UpdateScoreDisplay();
        UpdateComboDisplay(comboCount, multiplier, earned);

        Debug.Log($"[Score] +{earned} pts (x{multiplier:F1} combo) | Total: {totalScore}");
    }

    public int GetTotalScore() => totalScore;
    public float GetElapsedTime() => Time.time - LevelStartTime;

    private void ResetCombo()
    {
        comboCount  = 0;
        comboActive = false;
        if (comboObj != null) comboObj.SetActive(false);
    }

    private void UpdateScoreDisplay()
    {
        if (scoreValueText != null)
            scoreValueText.text = $"{totalScore:N0}";
    }

    private void UpdateComboDisplay(int combo, float multiplier, int earned)
    {
        if (comboObj == null || comboText == null) return;

        comboObj.SetActive(true);

        if (combo >= 2)
            comboText.text = $"x{combo} COMBO!\n+{earned} pts (x{multiplier:F1})";
        else
            comboText.text = $"+{earned} pts";

        if      (combo >= 5) comboText.color = new Color(1f, 0.3f, 0.1f, 1f);
        else if (combo >= 3) comboText.color = new Color(1f, 0.8f, 0.1f, 1f);
        else                 comboText.color = Color.white;

        if (comboPulseCoroutine != null) StopCoroutine(comboPulseCoroutine);
        comboPulseCoroutine = StartCoroutine(PulseCombo());
    }

    private IEnumerator PulseCombo()
    {
        if (comboObj == null) yield break;
        RectTransform rt = comboObj.GetComponent<RectTransform>();

        float elapsed = 0f;
        while (elapsed < 0.25f)
        {
            elapsed += Time.deltaTime;
            float scale = Mathf.Lerp(1.35f, 1f, elapsed / 0.25f);
            rt.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }
        rt.localScale = Vector3.one;
    }

    private void BuildScoreHUD()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        Texture2D tex = new Texture2D(4, 4);
        Color[] px = new Color[16];
        for (int i = 0; i < px.Length; i++) px[i] = Color.white;
        tex.SetPixels(px); tex.Apply();
        Sprite white = Sprite.Create(tex, new Rect(0,0,4,4), new Vector2(0.5f,0.5f));

        Font boldFont = Resources.Load<Font>("Fonts/ArcadeBold");
        if (boldFont == null) boldFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // --- Panel Score Ampliado (esquina superior derecha, tamano 270x100) ---
        GameObject scorePanel = new GameObject("Score_Panel", typeof(RectTransform), typeof(Image));
        scorePanel.transform.SetParent(canvas.transform, false);
        RectTransform rp = scorePanel.GetComponent<RectTransform>();
        rp.anchorMin = new Vector2(1f, 1f);
        rp.anchorMax = new Vector2(1f, 1f);
        rp.pivot     = new Vector2(1f, 1f);
        rp.anchoredPosition = new Vector2(-35f, -30f);
        rp.sizeDelta = new Vector2(270f, 100f);
        Image spBg = scorePanel.GetComponent<Image>();
        spBg.sprite = white;
        spBg.color  = new Color(0.06f, 0.08f, 0.14f, 0.94f);

        // Borde superior dorado brillante
        GameObject border = new GameObject("Score_Border", typeof(RectTransform), typeof(Image));
        border.transform.SetParent(scorePanel.transform, false);
        RectTransform rb2 = border.GetComponent<RectTransform>();
        rb2.anchorMin = new Vector2(0f, 1f); rb2.anchorMax = new Vector2(1f, 1f);
        rb2.pivot = new Vector2(0.5f, 1f);   rb2.anchoredPosition = Vector2.zero;
        rb2.sizeDelta = new Vector2(0f, 4f);
        Image bImg = border.GetComponent<Image>();
        bImg.sprite = white; bImg.color = new Color(1f, 0.85f, 0.18f, 1f);

        // Titulo "PUNTUACION"
        GameObject titleObj = new GameObject("Text_ScoreTitle", typeof(RectTransform), typeof(Text));
        titleObj.transform.SetParent(scorePanel.transform, false);
        RectTransform rtTitle = titleObj.GetComponent<RectTransform>();
        rtTitle.anchorMin = new Vector2(0f, 1f);
        rtTitle.anchorMax = new Vector2(1f, 1f);
        rtTitle.pivot = new Vector2(0.5f, 1f);
        rtTitle.anchoredPosition = new Vector2(0f, -10f);
        rtTitle.sizeDelta = new Vector2(0f, 26f);
        scoreTitleText = titleObj.GetComponent<Text>();
        scoreTitleText.font = boldFont;
        scoreTitleText.text = "PUNTUACION";
        scoreTitleText.fontSize = 18;
        scoreTitleText.fontStyle = FontStyle.Bold;
        scoreTitleText.alignment = TextAnchor.MiddleCenter;
        scoreTitleText.color = new Color(0.3f, 0.88f, 1f, 1f);
        Outline titleOut = titleObj.AddComponent<Outline>();
        titleOut.effectColor = Color.black;
        titleOut.effectDistance = new Vector2(1.5f, -1.5f);

        // Valor Numerico del Score
        GameObject valObj = new GameObject("Text_ScoreValue", typeof(RectTransform), typeof(Text));
        valObj.transform.SetParent(scorePanel.transform, false);
        RectTransform rtVal = valObj.GetComponent<RectTransform>();
        rtVal.anchorMin = new Vector2(0f, 0f);
        rtVal.anchorMax = new Vector2(1f, 1f);
        rtVal.pivot = new Vector2(0.5f, 0.5f);
        rtVal.offsetMin = new Vector2(10f, 8f);
        rtVal.offsetMax = new Vector2(-10f, -32f);
        scoreValueText = valObj.GetComponent<Text>();
        scoreValueText.font = boldFont;
        scoreValueText.text = "0";
        scoreValueText.fontSize = 36;
        scoreValueText.fontStyle = FontStyle.Bold;
        scoreValueText.alignment = TextAnchor.MiddleCenter;
        scoreValueText.color = new Color(1f, 0.88f, 0.2f, 1f);
        Outline valOut = valObj.AddComponent<Outline>();
        valOut.effectColor = Color.black;
        valOut.effectDistance = new Vector2(1.5f, -1.5f);

        // --- Panel Combo ---
        comboObj = new GameObject("Combo_Panel", typeof(RectTransform), typeof(Image));
        comboObj.transform.SetParent(canvas.transform, false);
        RectTransform rc = comboObj.GetComponent<RectTransform>();
        rc.anchorMin = new Vector2(0.5f, 0.5f);
        rc.anchorMax = new Vector2(0.5f, 0.5f);
        rc.pivot     = new Vector2(0.5f, 0.5f);
        rc.anchoredPosition = new Vector2(240f, -60f);
        rc.sizeDelta = new Vector2(300f, 75f);
        Image cBg = comboObj.GetComponent<Image>();
        cBg.sprite = white;
        cBg.color  = new Color(0.04f, 0.04f, 0.1f, 0.85f);

        GameObject comboTextObj = new GameObject("Text_Combo", typeof(RectTransform), typeof(Text));
        comboTextObj.transform.SetParent(comboObj.transform, false);
        RectTransform rct = comboTextObj.GetComponent<RectTransform>();
        rct.anchorMin = Vector2.zero; rct.anchorMax = Vector2.one; rct.sizeDelta = Vector2.zero;
        comboText = comboTextObj.GetComponent<Text>();
        comboText.font = boldFont;
        comboText.text = "";
        comboText.fontSize = 22;
        comboText.fontStyle = FontStyle.Bold;
        comboText.alignment = TextAnchor.MiddleCenter;
        comboText.color = Color.white;
        Outline comboOut = comboTextObj.AddComponent<Outline>();
        comboOut.effectColor = Color.black;
        comboOut.effectDistance = new Vector2(1.5f, -1.5f);

        comboObj.SetActive(false);
    }
}
