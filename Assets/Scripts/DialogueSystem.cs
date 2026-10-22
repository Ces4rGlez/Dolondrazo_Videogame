using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// DialogueSystem - Motor de dialogos estilo RPG con efecto typewriter.
/// Singleton. Se construye automaticamente al inicio.
/// Corrige de raiz el cuadro amarillo vacio: cuando no hay portrait, se oculta el borde
/// y el texto toma todo el ancho. Fuentes nitidas ArcadeBold/ArcadeRegular.
/// </summary>
public class DialogueSystem : MonoBehaviour
{
    public static DialogueSystem Instance { get; private set; }

    [System.Serializable]
    public class DialogueLine
    {
        public string speakerName;      // Nombre del personaje
        public Sprite portrait;         // Retrato (puede ser null)
        public Color nameColor = new Color(1f, 0.88f, 0.2f, 1f);
        [TextArea(2, 5)]
        public string text;             // Dialogo
        public float typeSpeed = 0.032f;// Segundos entre letras
    }

    // UI Components
    private GameObject dialoguePanel;
    private GameObject portraitBorderObj;
    private Image portraitImage;
    private RectTransform textZoneRect;
    private Text speakerNameText;
    private Text dialogueText;
    private GameObject continuePrompt;
    private Image nameBackground;

    private bool isTyping       = false;
    private bool skipRequested   = false;
    private bool isActive       = false;
    private System.Action onDialogueComplete;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        BuildDialogueUI();
    }

    private void Update()
    {
        if (!isActive) return;

        bool advancePressed = UnityEngine.InputSystem.Keyboard.current != null &&
            (UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame ||
             UnityEngine.InputSystem.Keyboard.current.enterKey.wasPressedThisFrame ||
             UnityEngine.InputSystem.Keyboard.current.zKey.wasPressedThisFrame);

        if (advancePressed)
        {
            if (isTyping) skipRequested = true;
        }
    }

    // -------------------------------------------------------
    // API publica
    // -------------------------------------------------------

    public void PlayDialogue(List<DialogueLine> lines, System.Action onComplete = null)
    {
        onDialogueComplete = onComplete;
        StartCoroutine(RunDialogueRoutine(lines));
    }

    public bool IsActive => isActive;

    // -------------------------------------------------------
    // Logica interna
    // -------------------------------------------------------

    private IEnumerator RunDialogueRoutine(List<DialogueLine> lines)
    {
        isActive = true;
        Time.timeScale = 0f; // Pausar el juego durante el dialogo

        if (dialoguePanel != null) dialoguePanel.SetActive(true);
        yield return StartCoroutine(FadePanel(0f, 1f, 0.25f));

        foreach (var line in lines)
        {
            // 1. Configurar hablante
            if (speakerNameText != null)
                speakerNameText.text = line.speakerName.ToUpper();
            if (nameBackground != null)
                nameBackground.color = new Color(line.nameColor.r * 0.25f, line.nameColor.g * 0.25f, line.nameColor.b * 0.25f, 0.95f);
            if (speakerNameText != null)
                speakerNameText.color = line.nameColor;

            // 2. Portrait: Si no hay portrait (ej. Narrador), ocultar borde y expandir texto
            if (line.portrait != null)
            {
                if (portraitBorderObj != null) portraitBorderObj.SetActive(true);
                if (portraitImage != null)
                {
                    portraitImage.gameObject.SetActive(true);
                    portraitImage.sprite = line.portrait;
                }
                if (textZoneRect != null)
                {
                    textZoneRect.offsetMin = new Vector2(185f, 10f);
                }
            }
            else
            {
                if (portraitBorderObj != null) portraitBorderObj.SetActive(false);
                if (portraitImage != null) portraitImage.gameObject.SetActive(false);
                if (textZoneRect != null)
                {
                    textZoneRect.offsetMin = new Vector2(30f, 10f);
                }
            }

            // 3. Efecto typewriter
            if (dialogueText != null) dialogueText.text = "";
            if (continuePrompt != null) continuePrompt.SetActive(false);

            yield return StartCoroutine(TypeTextRoutine(line.text, line.typeSpeed));

            // 4. Mostrar prompt "continuar"
            if (continuePrompt != null) continuePrompt.SetActive(true);

            // 5. Esperar input para avanzar al siguiente dialogo
            yield return StartCoroutine(WaitForAdvance());
        }

        // Fin del dialogo: fade out y desbloquear juego
        yield return StartCoroutine(FadePanel(1f, 0f, 0.25f));
        if (dialoguePanel != null) dialoguePanel.SetActive(false);

        isActive = false;
        Time.timeScale = 1f;

        onDialogueComplete?.Invoke();
    }

    private IEnumerator TypeTextRoutine(string fullText, float speed)
    {
        isTyping = true;
        skipRequested = false;
        string current = "";

        for (int i = 0; i < fullText.Length; i++)
        {
            if (skipRequested)
            {
                if (dialogueText != null) dialogueText.text = fullText;
                break;
            }
            current += fullText[i];
            if (dialogueText != null) dialogueText.text = current;
            yield return new WaitForSecondsRealtime(speed);
        }

        isTyping = false;
        skipRequested = false;
    }

    private IEnumerator WaitForAdvance()
    {
        // Espera de seguridad para evitar saltos accidentales de dialogo
        yield return new WaitForSecondsRealtime(0.18f);

        while (true)
        {
            bool pressed = UnityEngine.InputSystem.Keyboard.current != null &&
                (UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame ||
                 UnityEngine.InputSystem.Keyboard.current.enterKey.wasPressedThisFrame ||
                 UnityEngine.InputSystem.Keyboard.current.zKey.wasPressedThisFrame);
            if (pressed) yield break;
            yield return null;
        }
    }

    private IEnumerator FadePanel(float from, float to, float duration)
    {
        CanvasGroup cg = dialoguePanel?.GetComponent<CanvasGroup>();
        if (cg == null) yield break;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        cg.alpha = to;
    }

    // -------------------------------------------------------
    // Construccion de UI
    // -------------------------------------------------------

    private void BuildDialogueUI()
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

        Font regularFont = Resources.Load<Font>("Fonts/ArcadeRegular");
        if (regularFont == null) regularFont = boldFont;

        // === PANEL PRINCIPAL (parte inferior de la pantalla) ===
        dialoguePanel = new GameObject("Dialogue_Panel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
        dialoguePanel.transform.SetParent(canvas.transform, false);
        RectTransform rp = dialoguePanel.GetComponent<RectTransform>();
        rp.anchorMin = new Vector2(0f, 0f);
        rp.anchorMax = new Vector2(1f, 0f);
        rp.pivot = new Vector2(0.5f, 0f);
        rp.anchoredPosition = new Vector2(0f, 15f);
        rp.sizeDelta = new Vector2(-40f, 205f);
        Image panelBg = dialoguePanel.GetComponent<Image>();
        panelBg.sprite = white;
        panelBg.color = new Color(0.03f, 0.04f, 0.10f, 0.96f);
        dialoguePanel.GetComponent<CanvasGroup>().alpha = 0f;

        // Linea dorada superior brillante
        GameObject topLine = new GameObject("TopLine", typeof(RectTransform), typeof(Image));
        topLine.transform.SetParent(dialoguePanel.transform, false);
        RectTransform rtl = topLine.GetComponent<RectTransform>();
        rtl.anchorMin = new Vector2(0f,1f); rtl.anchorMax = new Vector2(1f,1f);
        rtl.pivot = new Vector2(0.5f,1f); rtl.anchoredPosition = Vector2.zero;
        rtl.sizeDelta = new Vector2(0f,4f);
        topLine.GetComponent<Image>().color = new Color(1f,0.85f,0.18f,1f);

        // === PORTRAIT (izquierda) ===
        portraitBorderObj = new GameObject("PortraitBorder", typeof(RectTransform), typeof(Image));
        portraitBorderObj.transform.SetParent(dialoguePanel.transform, false);
        RectTransform rpb = portraitBorderObj.GetComponent<RectTransform>();
        rpb.anchorMin = new Vector2(0f,0f); rpb.anchorMax = new Vector2(0f,1f);
        rpb.pivot = new Vector2(0f,0.5f);
        rpb.anchoredPosition = new Vector2(14f, 0f);
        rpb.sizeDelta = new Vector2(156f, -20f);
        Image pbImg = portraitBorderObj.GetComponent<Image>();
        pbImg.sprite = white;
        pbImg.color = new Color(1f,0.85f,0.2f,0.4f);

        GameObject portraitObj = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
        portraitObj.transform.SetParent(portraitBorderObj.transform, false);
        RectTransform rpo = portraitObj.GetComponent<RectTransform>();
        rpo.anchorMin = Vector2.zero; rpo.anchorMax = Vector2.one;
        rpo.offsetMin = new Vector2(4f, 4f); rpo.offsetMax = new Vector2(-4f, -4f);
        portraitImage = portraitObj.GetComponent<Image>();
        portraitImage.preserveAspect = true;

        portraitBorderObj.SetActive(false); // Oculto por defecto

        // === ZONA DE TEXTO (derecha del portrait o extendida) ===
        GameObject textZone = new GameObject("TextZone", typeof(RectTransform));
        textZone.transform.SetParent(dialoguePanel.transform, false);
        textZoneRect = textZone.GetComponent<RectTransform>();
        textZoneRect.anchorMin = new Vector2(0f,0f); textZoneRect.anchorMax = new Vector2(1f,1f);
        textZoneRect.offsetMin = new Vector2(30f, 10f);
        textZoneRect.offsetMax = new Vector2(-25f, -10f);

        // Fondo del nombre del hablante
        GameObject nameBg = new GameObject("Name_Background", typeof(RectTransform), typeof(Image));
        nameBg.transform.SetParent(textZone.transform, false);
        RectTransform rnb = nameBg.GetComponent<RectTransform>();
        rnb.anchorMin = new Vector2(0f,1f); rnb.anchorMax = new Vector2(0f,1f);
        rnb.pivot = new Vector2(0f,0f);
        rnb.anchoredPosition = new Vector2(0f, 6f);
        rnb.sizeDelta = new Vector2(250f, 38f);
        nameBackground = nameBg.GetComponent<Image>();
        nameBackground.sprite = white;
        nameBackground.color = new Color(0.18f,0.14f,0.06f,0.95f);

        // Texto nombre del hablante
        GameObject nameObj = new GameObject("Speaker_Name", typeof(RectTransform), typeof(Text));
        nameObj.transform.SetParent(nameBg.transform, false);
        RectTransform rno = nameObj.GetComponent<RectTransform>();
        rno.anchorMin = Vector2.zero; rno.anchorMax = Vector2.one;
        rno.offsetMin = new Vector2(12f,3f); rno.offsetMax = new Vector2(-10f,-3f);
        speakerNameText = nameObj.GetComponent<Text>();
        speakerNameText.font = boldFont;
        speakerNameText.fontSize = 22;
        speakerNameText.fontStyle = FontStyle.Bold;
        speakerNameText.alignment = TextAnchor.MiddleLeft;
        speakerNameText.color = new Color(1f,0.88f,0.2f,1f);
        Outline nameOut = nameObj.AddComponent<Outline>();
        nameOut.effectColor = Color.black;
        nameOut.effectDistance = new Vector2(1.5f, -1.5f);

        // Texto del dialogo
        GameObject textObj = new GameObject("Dialogue_Text", typeof(RectTransform), typeof(Text));
        textObj.transform.SetParent(textZone.transform, false);
        RectTransform rto = textObj.GetComponent<RectTransform>();
        rto.anchorMin = Vector2.zero; rto.anchorMax = Vector2.one;
        rto.offsetMin = new Vector2(0f, 0f);
        rto.offsetMax = new Vector2(0f, -44f);
        dialogueText = textObj.GetComponent<Text>();
        dialogueText.font = regularFont;
        dialogueText.fontSize = 24;
        dialogueText.fontStyle = FontStyle.Normal;
        dialogueText.lineSpacing = 1.18f;
        dialogueText.alignment = TextAnchor.UpperLeft;
        dialogueText.color = new Color(0.96f, 0.98f, 1f, 1f);
        dialogueText.horizontalOverflow = HorizontalWrapMode.Wrap;
        dialogueText.verticalOverflow = VerticalWrapMode.Overflow;
        Outline textOut = textObj.AddComponent<Outline>();
        textOut.effectColor = new Color(0.01f, 0.02f, 0.05f, 0.95f);
        textOut.effectDistance = new Vector2(1.5f, -1.5f);

        // Prompt "continuar"
        continuePrompt = new GameObject("Continue_Prompt", typeof(RectTransform), typeof(Text));
        continuePrompt.transform.SetParent(dialoguePanel.transform, false);
        RectTransform rcp = continuePrompt.GetComponent<RectTransform>();
        rcp.anchorMin = new Vector2(1f,0f); rcp.anchorMax = new Vector2(1f,0f);
        rcp.pivot = new Vector2(1f,0f);
        rcp.anchoredPosition = new Vector2(-25f, 12f);
        rcp.sizeDelta = new Vector2(250f, 30f);
        Text cpText = continuePrompt.GetComponent<Text>();
        cpText.font = boldFont;
        cpText.text = "[ESPACIO / ENTER] continuar >>";
        cpText.fontSize = 17;
        cpText.fontStyle = FontStyle.Bold;
        cpText.alignment = TextAnchor.LowerRight;
        cpText.color = new Color(1f, 0.85f, 0.25f, 1f);
        Outline cpOut = continuePrompt.AddComponent<Outline>();
        cpOut.effectColor = Color.black;
        cpOut.effectDistance = new Vector2(1.5f, -1.5f);
        continuePrompt.SetActive(false);
        StartCoroutine(BlinkPrompt(cpText));

        dialoguePanel.SetActive(false);
    }

    private IEnumerator BlinkPrompt(Text t)
    {
        while (true)
        {
            if (t != null && continuePrompt != null && continuePrompt.activeSelf)
            {
                float a = (Mathf.Sin(Time.unscaledTime * 4f) + 1f) * 0.5f;
                t.color = new Color(1f, 0.85f, 0.25f, 0.35f + a * 0.65f);
            }
            yield return null;
        }
    }
}
