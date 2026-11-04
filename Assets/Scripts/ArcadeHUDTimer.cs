using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ArcadeHUDTimer - Reloj retro estilo arcade beat 'em up en la parte superior central.
/// Muestra TIME: 99 con estetica noventera y parpadeo de alerta en los ultimos segundos.
/// </summary>
public class ArcadeHUDTimer : MonoBehaviour
{
    public static ArcadeHUDTimer Instance { get; private set; }

    [Header("Parametros del Tiempo")]
    [SerializeField] private int initialTime = 99;
    [SerializeField] private bool autoStart = true;
    [SerializeField] private bool countUpInstead = false;

    private int currentTime;
    private float timerAccumulator = 0f;
    private bool isRunning = false;

    // Componentes de UI
    private GameObject timerContainer;
    private Text labelText;
    private Text valueText;
    private Image badgeBg;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        currentTime = initialTime;
        EnsureTimerUI();
        if (autoStart)
        {
            isRunning = true;
        }
    }

    private void Update()
    {
        if (!isRunning) return;

        timerAccumulator += Time.deltaTime;
        if (timerAccumulator >= 1.0f)
        {
            timerAccumulator -= 1.0f;
            if (countUpInstead)
            {
                currentTime++;
            }
            else
            {
                if (currentTime > 0)
                {
                    currentTime--;
                }
            }
            UpdateDisplay();
        }

        // Efecto visual de parpadeo de alerta cuando queda poco tiempo (menos de 15 segundos)
        if (!countUpInstead && currentTime <= 15 && currentTime > 0)
        {
            float pulse = Mathf.PingPong(Time.unscaledTime * 4f, 1f);
            if (valueText != null)
            {
                valueText.color = Color.Lerp(new Color(1f, 0.2f, 0.2f, 1f), new Color(1f, 0.85f, 0.1f, 1f), pulse);
            }
        }
    }

    private void UpdateDisplay()
    {
        if (valueText == null) return;

        valueText.text = currentTime.ToString("D2");

        if (countUpInstead || currentTime > 15)
        {
            valueText.color = new Color(1f, 0.9f, 0.15f, 1f); // Amarillo arcade
        }
    }

    public void SetTime(int seconds)
    {
        currentTime = seconds;
        UpdateDisplay();
    }

    public void AddTime(int seconds)
    {
        currentTime += seconds;
        UpdateDisplay();
    }

    public void PauseTimer() => isRunning = false;
    public void ResumeTimer() => isRunning = true;
    public void ResetTimer(int seconds = 99)
    {
        currentTime = seconds;
        timerAccumulator = 0f;
        UpdateDisplay();
    }

    public int GetCurrentTime() => currentTime;

    private void EnsureTimerUI()
    {
        Canvas canvas = HUDCanvasHelper.GetCanvas();
        if (canvas == null) return;

        if (timerContainer != null) return;

        Texture2D tex = new Texture2D(2, 2);
        tex.SetPixels(new Color[] { Color.white, Color.white, Color.white, Color.white });
        tex.Apply();
        Sprite white = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));

        // Contenedor principal arriba al centro
        timerContainer = new GameObject("Arcade_HUD_Timer", typeof(RectTransform), typeof(Image));
        timerContainer.transform.SetParent(canvas.transform, false);

        RectTransform rt = timerContainer.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -15f);
        rt.sizeDelta = new Vector2(170f, 65f);

        badgeBg = timerContainer.GetComponent<Image>();
        badgeBg.sprite = white;
        badgeBg.color = new Color(0.05f, 0.08f, 0.14f, 0.90f);

        // Borde superior dorado
        GameObject topBar = new GameObject("TopBorder", typeof(RectTransform), typeof(Image));
        topBar.transform.SetParent(timerContainer.transform, false);
        RectTransform rtTop = topBar.GetComponent<RectTransform>();
        rtTop.anchorMin = new Vector2(0f, 1f); rtTop.anchorMax = new Vector2(1f, 1f);
        rtTop.offsetMin = new Vector2(0f, -3f); rtTop.offsetMax = Vector2.zero;
        Image topImg = topBar.GetComponent<Image>();
        topImg.sprite = white;
        topImg.color = new Color(1f, 0.75f, 0.1f, 1f);

        // Etiqueta "TIME"
        GameObject lblObj = new GameObject("Label_TIME", typeof(RectTransform), typeof(Text));
        lblObj.transform.SetParent(timerContainer.transform, false);
        RectTransform rtLbl = lblObj.GetComponent<RectTransform>();
        rtLbl.anchoredPosition = new Vector2(0f, 14f);
        rtLbl.sizeDelta = new Vector2(150f, 22f);
        labelText = lblObj.GetComponent<Text>();
        labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        labelText.fontSize = 15;
        labelText.alignment = TextAnchor.MiddleCenter;
        labelText.fontStyle = FontStyle.Bold;
        labelText.color = new Color(0.3f, 0.85f, 1f, 1f); // Cyan arcade
        labelText.text = "TIME";

        // Numero digital grande
        GameObject valObj = new GameObject("Value_TIME", typeof(RectTransform), typeof(Text));
        valObj.transform.SetParent(timerContainer.transform, false);
        RectTransform rtVal = valObj.GetComponent<RectTransform>();
        rtVal.anchoredPosition = new Vector2(0f, -12f);
        rtVal.sizeDelta = new Vector2(150f, 42f);
        valueText = valObj.GetComponent<Text>();
        valueText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        valueText.fontSize = 34;
        valueText.alignment = TextAnchor.MiddleCenter;
        valueText.fontStyle = FontStyle.Bold;
        valueText.color = new Color(1f, 0.9f, 0.15f, 1f);
        valueText.text = currentTime.ToString("D2");
    }
}


