using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

/// <summary>
/// IdleControlsReminder - Panel rapido de controles en pantalla.
/// Aparece al inicio del nivel o cuando el jugador permanece inactivo por unos segundos.
/// Se desvanece suavemente en cuanto el jugador pulsa cualquier tecla de movimiento o ataque.
/// </summary>
public class IdleControlsReminder : MonoBehaviour
{
    public static IdleControlsReminder Instance { get; private set; }

    [Header("Configuracion de Inactividad")]
    [SerializeField] private float idleTriggerTime = 4.5f;
    [SerializeField] private bool showOnStart = true;

    private GameObject panelObj;
    private CanvasGroup canvasGroup;
    private float idleTimer = 0f;
    private bool isVisible = false;
    private Coroutine fadeCoroutine;

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
        EnsureUI();

        if (showOnStart)
        {
            ShowReminder();
        }
    }

    private void Update()
    {
        bool hasInput = CheckAnyPlayerInput();

        if (hasInput)
        {
            idleTimer = 0f;
            if (isVisible)
            {
                HideReminder();
            }
        }
        else
        {
            idleTimer += Time.deltaTime;
            if (!isVisible && idleTimer >= idleTriggerTime)
            {
                ShowReminder();
            }
        }
    }

    private bool CheckAnyPlayerInput()
    {
        if (Keyboard.current == null) return false;

        var kb = Keyboard.current;
        return kb.wKey.isPressed || kb.sKey.isPressed || kb.aKey.isPressed || kb.dKey.isPressed ||
               kb.upArrowKey.isPressed || kb.downArrowKey.isPressed || kb.leftArrowKey.isPressed || kb.rightArrowKey.isPressed ||
               kb.jKey.isPressed || kb.kKey.isPressed || kb.zKey.isPressed || kb.xKey.isPressed ||
               kb.eKey.isPressed || kb.spaceKey.isPressed;
    }

    public void ShowReminder()
    {
        if (isVisible) return;
        isVisible = true;

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeRoutine(1f));
    }

    public void HideReminder()
    {
        if (!isVisible) return;
        isVisible = false;

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeRoutine(0f));
    }

    private IEnumerator FadeRoutine(float targetAlpha)
    {
        if (canvasGroup == null) yield break;

        float startAlpha = canvasGroup.alpha;
        float duration = 0.25f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
        if (targetAlpha <= 0.01f && panelObj != null)
        {
            panelObj.SetActive(false);
        }
    }

    private void EnsureUI()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        if (panelObj != null) return;

        Texture2D tex = new Texture2D(2, 2);
        tex.SetPixels(new Color[] { Color.white, Color.white, Color.white, Color.white });
        tex.Apply();
        Sprite white = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));

        // Tarjeta flotante en la parte inferior central
        panelObj = new GameObject("Idle_Controls_Reminder", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        panelObj.transform.SetParent(canvas.transform, false);

        RectTransform rt = panelObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 30f);
        rt.sizeDelta = new Vector2(760f, 100f);

        Image bg = panelObj.GetComponent<Image>();
        bg.sprite = white;
        bg.color = new Color(0.06f, 0.08f, 0.14f, 0.94f);

        canvasGroup = panelObj.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;

        // Borde superior color cian
        GameObject borderTop = new GameObject("BorderTop", typeof(RectTransform), typeof(Image));
        borderTop.transform.SetParent(panelObj.transform, false);
        RectTransform rtB = borderTop.GetComponent<RectTransform>();
        rtB.anchorMin = new Vector2(0f, 1f); rtB.anchorMax = new Vector2(1f, 1f);
        rtB.offsetMin = new Vector2(0f, -3f); rtB.offsetMax = Vector2.zero;
        Image bImg = borderTop.GetComponent<Image>();
        bImg.sprite = white;
        bImg.color = new Color(0.25f, 0.85f, 1f, 1f);

        // Titulo
        GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(Text));
        titleObj.transform.SetParent(panelObj.transform, false);
        RectTransform rtT = titleObj.GetComponent<RectTransform>();
        rtT.anchoredPosition = new Vector2(0f, 28f);
        rtT.sizeDelta = new Vector2(720f, 26f);
        Text t = titleObj.GetComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = 17;
        t.alignment = TextAnchor.MiddleCenter;
        t.fontStyle = FontStyle.Bold;
        t.color = new Color(1f, 0.85f, 0.2f, 1f);
        t.text = "GUIA DE CONTROLES RAPIDA";

        // Contenido de teclas
        GameObject bodyObj = new GameObject("Body", typeof(RectTransform), typeof(Text));
        bodyObj.transform.SetParent(panelObj.transform, false);
        RectTransform rtBody = bodyObj.GetComponent<RectTransform>();
        rtBody.anchoredPosition = new Vector2(0f, -12f);
        rtBody.sizeDelta = new Vector2(740f, 48f);
        Text b = bodyObj.GetComponent<Text>();
        b.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        b.fontSize = 15;
        b.alignment = TextAnchor.MiddleCenter;
        b.color = new Color(0.9f, 0.95f, 1f, 1f);
        b.text = "[WASD / Flechas] Moverse   |   [J / Z] Golpear   |   [K / X] Patada   |   [E] Subir al Autobus / Interactuar";
    }
}

