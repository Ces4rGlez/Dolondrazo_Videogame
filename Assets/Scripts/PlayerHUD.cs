using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHUD : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Health playerHealth;

    [Header("Componentes de UI")]
    [SerializeField] private Image batteryFill;
    [SerializeField] private Text percentageText;
    [SerializeField] private Text playerNameText;
    [SerializeField] private Image batteryFrame;
    [SerializeField] private Image batteryBolt;

    [Header("Colores de Bateria")]
    [SerializeField] private Color fullColor   = new Color(0.18f, 0.95f, 0.44f, 1f);  // Verde Neon Brillante
    [SerializeField] private Color mediumColor = new Color(1f, 0.82f, 0.1f, 1f);     // Amarillo Alerta
    [SerializeField] private Color lowColor    = new Color(1f, 0.2f, 0.2f, 1f);      // Rojo Critico

    [Header("Animacion")]
    [SerializeField] private float smoothSpeed = 8f;

    private float targetFill = 1f;
    private float currentFill = 1f;
    private Coroutine flashCoroutine;

    private void Start()
    {
        if (playerHealth == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                playerHealth = playerObj.GetComponent<Health>();
            }
        }

        GameObject existingHud = GameObject.Find("Player_Battery_HUD");
        if (existingHud != null) Destroy(existingHud);

        BuildBigBatteryHUD();

        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += HandleHealthChanged;
            targetFill = Mathf.Clamp01((float)playerHealth.CurrentHealth / Mathf.Max(1, playerHealth.MaxHealth));
            currentFill = targetFill;
            if (batteryFill != null) batteryFill.fillAmount = currentFill;
            UpdateDisplay(playerHealth.CurrentHealth, playerHealth.MaxHealth);
        }
    }

    private void HandleHealthChanged(int current, int max)
    {
        UpdateDisplay(current, max);
    }

    private void UpdateDisplay(int current, int max)
    {
        targetFill = Mathf.Clamp01((float)current / Mathf.Max(1, max));
        int percent = Mathf.RoundToInt(targetFill * 100f);

        if (percentageText != null)
        {
            if (percent <= 10 && percent > 0)
            {
                percentageText.text = "1% !";
            }
            else
            {
                percentageText.text = $"{percent}%";
            }
        }

        if (targetFill <= 0.25f && targetFill > 0f)
        {
            if (flashCoroutine == null) flashCoroutine = StartCoroutine(LowBatteryFlashRoutine());
        }
        else
        {
            if (flashCoroutine != null)
            {
                StopCoroutine(flashCoroutine);
                flashCoroutine = null;
                if (batteryFill != null) batteryFill.enabled = true;
            }
        }
    }

    private void Update()
    {
        if (batteryFill == null) return;

        currentFill = Mathf.Lerp(currentFill, targetFill, Time.deltaTime * smoothSpeed);
        batteryFill.fillAmount = currentFill;

        if (currentFill > 0.5f)
        {
            float t = (currentFill - 0.5f) / 0.5f;
            batteryFill.color = Color.Lerp(mediumColor, fullColor, t);
        }
        else
        {
            float t = currentFill / 0.5f;
            batteryFill.color = Color.Lerp(lowColor, mediumColor, t);
        }
    }

    private IEnumerator LowBatteryFlashRoutine()
    {
        while (true)
        {
            if (batteryFill != null) batteryFill.enabled = !batteryFill.enabled;
            yield return new WaitForSeconds(0.25f);
        }
    }

    private void BuildBigBatteryHUD()
    {
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
            scaler.dynamicPixelsPerUnit = 2.5f; // Maxima nitidez en fuentes

            canvasObj.AddComponent<GraphicRaycaster>();
        }
        else
        {
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null) scaler.dynamicPixelsPerUnit = 2.5f;
        }

        Texture2D tex = new Texture2D(4, 4);
        Color[] px = new Color[16];
        for (int i = 0; i < px.Length; i++) px[i] = Color.white;
        tex.SetPixels(px);
        tex.Apply();
        Sprite whiteSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));

        Font boldFont = Resources.Load<Font>("Fonts/ArcadeBold");
        if (boldFont == null) boldFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Contenedor principal de la bateria (440 x 110)
        GameObject hudContainer = new GameObject("Player_Battery_HUD");
        hudContainer.transform.SetParent(canvas.transform, false);
        RectTransform rectContainer = hudContainer.AddComponent<RectTransform>();
        rectContainer.anchorMin = new Vector2(0f, 1f);
        rectContainer.anchorMax = new Vector2(0f, 1f);
        rectContainer.pivot = new Vector2(0f, 1f);
        rectContainer.anchoredPosition = new Vector2(35f, -30f);
        rectContainer.sizeDelta = new Vector2(440f, 110f);

        Image containerBg = hudContainer.AddComponent<Image>();
        containerBg.sprite = whiteSprite;
        containerBg.color = new Color(0.06f, 0.08f, 0.14f, 0.94f);

        // Borde superior dorado
        GameObject topBorder = new GameObject("Container_TopBorder", typeof(RectTransform), typeof(Image));
        topBorder.transform.SetParent(hudContainer.transform, false);
        RectTransform rtTop = topBorder.GetComponent<RectTransform>();
        rtTop.anchorMin = new Vector2(0f, 1f); rtTop.anchorMax = new Vector2(1f, 1f);
        rtTop.pivot = new Vector2(0.5f, 1f); rtTop.anchoredPosition = Vector2.zero;
        rtTop.sizeDelta = new Vector2(0f, 3f);
        Image imgTop = topBorder.GetComponent<Image>();
        imgTop.sprite = whiteSprite;
        imgTop.color = new Color(1f, 0.85f, 0.2f, 0.95f);

        // Nombre de Fercho
        GameObject nameObj = new GameObject("Text_PlayerName");
        nameObj.transform.SetParent(hudContainer.transform, false);
        RectTransform rectName = nameObj.AddComponent<RectTransform>();
        rectName.anchorMin = new Vector2(0f, 1f);
        rectName.anchorMax = new Vector2(0f, 1f);
        rectName.pivot = new Vector2(0f, 1f);
        rectName.anchoredPosition = new Vector2(20f, -10f);
        rectName.sizeDelta = new Vector2(240f, 32f);

        playerNameText = nameObj.AddComponent<Text>();
        playerNameText.font = boldFont;
        playerNameText.text = "FERCHO";
        playerNameText.fontSize = 24;
        playerNameText.fontStyle = FontStyle.Bold;
        playerNameText.color = new Color(1f, 0.88f, 0.25f, 1f);
        Outline nameOut = nameObj.AddComponent<Outline>();
        nameOut.effectColor = Color.black;
        nameOut.effectDistance = new Vector2(1.5f, -1.5f);

        // Porcentaje
        GameObject percentObj = new GameObject("Text_BatteryPercent");
        percentObj.transform.SetParent(hudContainer.transform, false);
        RectTransform rectPercent = percentObj.AddComponent<RectTransform>();
        rectPercent.anchorMin = new Vector2(1f, 1f);
        rectPercent.anchorMax = new Vector2(1f, 1f);
        rectPercent.pivot = new Vector2(1f, 1f);
        rectPercent.anchoredPosition = new Vector2(-20f, -10f);
        rectPercent.sizeDelta = new Vector2(140f, 32f);

        percentageText = percentObj.AddComponent<Text>();
        percentageText.font = boldFont;
        percentageText.text = "100%";
        percentageText.fontSize = 22;
        percentageText.fontStyle = FontStyle.Bold;
        percentageText.alignment = TextAnchor.MiddleRight;
        percentageText.color = Color.white;
        Outline pctOut = percentObj.AddComponent<Outline>();
        pctOut.effectColor = Color.black;
        pctOut.effectDistance = new Vector2(1.5f, -1.5f);

        // Marco de la bateria
        GameObject frameObj = new GameObject("Battery_Frame");
        frameObj.transform.SetParent(hudContainer.transform, false);
        RectTransform rectFrame = frameObj.AddComponent<RectTransform>();
        rectFrame.anchorMin = new Vector2(0f, 0f);
        rectFrame.anchorMax = new Vector2(1f, 0f);
        rectFrame.pivot = new Vector2(0.5f, 0f);
        rectFrame.anchoredPosition = new Vector2(0f, 18f);
        rectFrame.sizeDelta = new Vector2(-36f, 38f);

        batteryFrame = frameObj.AddComponent<Image>();
        batteryFrame.sprite = whiteSprite;
        batteryFrame.color = new Color(0.02f, 0.03f, 0.06f, 1f);

        // Barra de relleno
        GameObject fillObj = new GameObject("Battery_Fill");
        fillObj.transform.SetParent(frameObj.transform, false);
        RectTransform rectFill = fillObj.AddComponent<RectTransform>();
        rectFill.anchorMin = Vector2.zero;
        rectFill.anchorMax = Vector2.one;
        rectFill.offsetMin = new Vector2(4f, 4f);
        rectFill.offsetMax = new Vector2(-4f, -4f);

        batteryFill = fillObj.AddComponent<Image>();
        batteryFill.sprite = whiteSprite;
        batteryFill.type = Image.Type.Filled;
        batteryFill.fillMethod = Image.FillMethod.Horizontal;
        batteryFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        batteryFill.color = fullColor;
        batteryFill.fillAmount = 1f;
    }

    private void OnDestroy()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= HandleHealthChanged;
        }
    }
}
