using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// PauseManager - Menu de pausa con ESC.
/// Agrega este script al GameObject "HUD_Manager".
/// </summary>
public class PauseManager : MonoBehaviour
{
    private bool isPaused = false;
    private GameObject pausePanel;
    private bool gameOverActive = false;

    private void Update()
    {
        // No permitir pausar si hay Game Over
        if (Time.timeScale == 0f && !isPaused) return;

        if (UnityEngine.InputSystem.Keyboard.current != null &&
            UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (isPaused) ResumeGame();
            else          PauseGame();
        }
    }

    private void Start()
    {
        BuildPausePanel();
    }

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;
        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
            StartCoroutine(FadeIn());
        }
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        if (pausePanel != null) pausePanel.SetActive(false);
    }

    private IEnumerator FadeIn()
    {
        CanvasGroup cg = pausePanel.GetComponent<CanvasGroup>();
        if (cg == null) cg = pausePanel.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        float elapsed = 0f;
        while (elapsed < 0.25f)
        {
            elapsed += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Clamp01(elapsed / 0.25f);
            yield return null;
        }
        cg.alpha = 1f;
    }

    private void BuildPausePanel()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        Texture2D tex = new Texture2D(4, 4);
        Color[] px = new Color[16];
        for (int i = 0; i < px.Length; i++) px[i] = Color.white;
        tex.SetPixels(px); tex.Apply();
        Sprite white = Sprite.Create(tex, new Rect(0,0,4,4), new Vector2(0.5f,0.5f));

        // Fondo semi-transparente pantalla completa
        pausePanel = new GameObject("Pause_Panel", typeof(RectTransform), typeof(Image));
        pausePanel.transform.SetParent(canvas.transform, false);
        RectTransform rp = pausePanel.GetComponent<RectTransform>();
        rp.anchorMin = Vector2.zero; rp.anchorMax = Vector2.one;
        rp.offsetMin = Vector2.zero; rp.offsetMax = Vector2.zero;
        Image bg = pausePanel.GetComponent<Image>();
        bg.sprite = white; bg.color = new Color(0f, 0f, 0.06f, 0.82f);

        // Caja central del menu
        GameObject menuBox = new GameObject("Pause_Box", typeof(RectTransform), typeof(Image));
        menuBox.transform.SetParent(pausePanel.transform, false);
        RectTransform rmb = menuBox.GetComponent<RectTransform>();
        rmb.anchorMin = new Vector2(0.5f,0.5f); rmb.anchorMax = new Vector2(0.5f,0.5f);
        rmb.pivot = new Vector2(0.5f,0.5f);
        rmb.anchoredPosition = Vector2.zero; rmb.sizeDelta = new Vector2(420f, 380f);
        Image mbBg = menuBox.GetComponent<Image>();
        mbBg.sprite = white; mbBg.color = new Color(0.05f,0.06f,0.14f,0.97f);

        // Borde dorado superior
        GameObject border = new GameObject("Gold_Border", typeof(RectTransform), typeof(Image));
        border.transform.SetParent(menuBox.transform, false);
        RectTransform rb2 = border.GetComponent<RectTransform>();
        rb2.anchorMin = new Vector2(0f,1f); rb2.anchorMax = new Vector2(1f,1f);
        rb2.pivot = new Vector2(0.5f,1f); rb2.anchoredPosition = Vector2.zero;
        rb2.sizeDelta = new Vector2(0f, 5f);
        Image bImg = border.GetComponent<Image>();
        bImg.sprite = white; bImg.color = new Color(1f,0.84f,0f,1f);

        // Titulo PAUSADO
        AddText(menuBox.transform, "[ PAUSA ]", new Vector2(0f,140f), new Vector2(380f,65f),
                46, FontStyle.Bold, new Color(1f,0.88f,0.15f,1f), "Text_Pause");

        // Score actual
        int score = ScoreManager.Instance != null ? ScoreManager.Instance.GetTotalScore() : 0;
        AddText(menuBox.transform, $"Score actual: {score:N0} pts",
                new Vector2(0f, 70f), new Vector2(380f, 35f),
                20, FontStyle.Normal, new Color(0.75f,0.75f,0.85f,1f), "Text_Score");

        // Separador
        GameObject sep = new GameObject("Separator", typeof(RectTransform), typeof(Image));
        sep.transform.SetParent(menuBox.transform, false);
        RectTransform rs = sep.GetComponent<RectTransform>();
        rs.anchorMin = new Vector2(0.5f,0.5f); rs.anchorMax = new Vector2(0.5f,0.5f);
        rs.pivot = new Vector2(0.5f,0.5f);
        rs.anchoredPosition = new Vector2(0f,28f); rs.sizeDelta = new Vector2(340f,2f);
        sep.GetComponent<Image>().color = new Color(1f,1f,1f,0.12f);

        // Botones
        AddButton(menuBox.transform, white, new Vector2(0f,-15f), new Vector2(340f,62f),
                  "> REANUDAR",  new Color(0.1f,0.55f,0.2f,1f), new Color(0.15f,0.75f,0.28f,1f),
                  "Btn_Resume",  () => ResumeGame());

        AddButton(menuBox.transform, white, new Vector2(0f,-90f), new Vector2(340f,62f),
                  ">> REINICIAR NIVEL", new Color(0.65f,0.1f,0.08f,1f), new Color(0.85f,0.18f,0.12f,1f),
                  "Btn_Restart", () => { Time.timeScale = 1f; SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); });

        AddButton(menuBox.transform, white, new Vector2(0f,-165f), new Vector2(340f,62f),
                  "<< MENU PRINCIPAL", new Color(0.12f,0.12f,0.22f,1f), new Color(0.22f,0.22f,0.38f,1f),
                  "Btn_Menu",    () => {
                  Time.timeScale = 1f;
                  if (Application.CanStreamedLevelBeLoaded("Pantalla_Inicio"))
                      SceneManager.LoadScene("Pantalla_Inicio");
                  else
                      SceneManager.LoadScene(0);
              });

        // Hint ESC
        AddText(pausePanel.transform, "[ESC] para reanudar", new Vector2(0f,-220f),
                new Vector2(400f,30f), 16, FontStyle.Italic,
                new Color(0.45f,0.45f,0.55f,1f), "Text_Hint");

        pausePanel.SetActive(false);
    }

    private void AddText(Transform parent, string content, Vector2 aPos, Vector2 size,
                         int fontSize, FontStyle style, Color color, string name)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Text));
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f,0.5f); rt.anchorMax = new Vector2(0.5f,0.5f);
        rt.pivot = new Vector2(0.5f,0.5f);
        rt.anchoredPosition = aPos; rt.sizeDelta = size;
        Text t = obj.GetComponent<Text>();
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.text = content; t.fontSize = fontSize;
        t.fontStyle = style; t.alignment = TextAnchor.MiddleCenter; t.color = color;
    }

    private void AddButton(Transform parent, Sprite white, Vector2 aPos, Vector2 size,
                           string label, Color normalColor, Color hoverColor,
                           string name, UnityEngine.Events.UnityAction action)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);
        RectTransform rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f,0.5f); rt.anchorMax = new Vector2(0.5f,0.5f);
        rt.pivot = new Vector2(0.5f,0.5f);
        rt.anchoredPosition = aPos; rt.sizeDelta = size;
        Image bg = btnObj.GetComponent<Image>();
        bg.sprite = white; bg.color = normalColor;
        Button btn = btnObj.GetComponent<Button>();
        ColorBlock cb = btn.colors;
        cb.normalColor = normalColor; cb.highlightedColor = hoverColor;
        cb.pressedColor = new Color(normalColor.r*0.6f,normalColor.g*0.6f,normalColor.b*0.6f,1f);
        btn.colors = cb;
        btn.onClick.AddListener(action);
        AddText(btnObj.transform, label, Vector2.zero, size, 21, FontStyle.Bold, Color.white, "Label");
    }
}

