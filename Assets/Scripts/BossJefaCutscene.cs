using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BossJefaCutscene - Secuencia de diálogo cinemático previa al enfrentamiento
/// con Valentina (Líder del Instituto Rébsamen) frente a Cinépolis Paseo Dolores.
/// </summary>
public class BossJefaCutscene : MonoBehaviour
{
    [Header("Retratos")]
    [SerializeField] private Sprite ferChoPortrait;   // Retrato de Fercho
    [SerializeField] private Sprite bossPortrait;     // Retrato de Valentina

    [Header("Colores de Nombre")]
    [SerializeField] private Color ferChoColor = new Color(0.3f, 0.75f, 1f, 1f);     // Azul CBTis
    [SerializeField] private Color bossColor   = new Color(1f, 0.35f, 0.75f, 1f);    // Rosa Fresa Rébsamen

    private bool triggered = false;

    private void Awake()
    {
        // Si no se asigno portrait manualmente, tomar el sprite del renderer
        if (bossPortrait == null)
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null) bossPortrait = sr.sprite;
        }
    }

    /// <summary>
    /// Inicia el diálogo de la Jefa Rébsamen.
    /// </summary>
    public void TriggerBossCutscene()
    {
        if (triggered) return;
        triggered = true;
        StartCoroutine(RunCutsceneRoutine());
    }

    private IEnumerator RunCutsceneRoutine()
    {
        while (DialogueSystem.Instance == null) yield return null;

        var lines = new List<DialogueSystem.DialogueLine>
        {
            new DialogueSystem.DialogueLine
            {
                speakerName = "???",
                portrait    = bossPortrait,
                nameColor   = bossColor,
                text        = "¡O sea, a ver! ¿Quién te dio permiso de acercarte a Cinépolis Paseo Dolores con esos tenis sucios?",
                typeSpeed   = 0.038f
            },
            new DialogueSystem.DialogueLine
            {
                speakerName = "Fercho",
                portrait    = ferChoPortrait,
                nameColor   = ferChoColor,
                text        = "¡No vengo a pasear, tengo que entregar mi proyecto hoy mismo! ¡Hazte a un lado!",
                typeSpeed   = 0.04f
            },
            new DialogueSystem.DialogueLine
            {
                speakerName = "Valentina",
                portrait    = bossPortrait,
                nameColor   = bossColor,
                text        = "¡Ay, qué oso! La gente del CBTis siempre tan ruidosa y ordinaria... En el Rébsamen tenemos categoría, querido.",
                typeSpeed   = 0.036f
            },
            new DialogueSystem.DialogueLine
            {
                speakerName = "Fercho",
                portrait    = ferChoPortrait,
                nameColor   = ferChoColor,
                text        = "Tus compañeros de chaleco ya no pudieron detenerme. ¡No me hagas perder más tiempo!",
                typeSpeed   = 0.04f
            },
            new DialogueSystem.DialogueLine
            {
                speakerName = "Valentina",
                portrait    = bossPortrait,
                nameColor   = bossColor,
                text        = "¡Jajaja! ¿Tú contra mí? Fui campeona de Tae Kwon Do en Intercolegiales. ¡Te voy a dejar viendo estrellas!",
                typeSpeed   = 0.036f
            },
            new DialogueSystem.DialogueLine
            {
                speakerName = "Fercho",
                portrait    = ferChoPortrait,
                nameColor   = ferChoColor,
                text        = "¡Vamos a ver si tus patadas son tan buenas como tus quejas!",
                typeSpeed   = 0.042f
            }
        };

        DialogueSystem.Instance.PlayDialogue(lines, () =>
        {
            Debug.Log("[BossJefaCutscene] ¡Diálogo completado! Empieza la batalla contra Valentina.");
        });
    }
}

