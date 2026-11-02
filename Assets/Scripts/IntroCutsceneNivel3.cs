using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// IntroCutsceneNivel3 - Diálogo cinemático inicial para el Nivel 3 (Acceso al CBTis 75).
/// Se ejecuta automáticamente al arrancar el nivel. Pausa el juego y muestra el contexto
/// antes de iniciar las oleadas.
/// </summary>
public class IntroCutsceneNivel3 : MonoBehaviour
{
    [Header("Retratos")]
    [SerializeField] private Sprite ferChoPortrait;
    [SerializeField] private Sprite sabesPortrait;

    [Header("Colores")]
    [SerializeField] private Color ferChoColor = new Color(0.25f, 0.82f, 1f, 1f);   // Azul Fercho
    [SerializeField] private Color sabesColor  = new Color(1f, 0.84f, 0.1f, 1f);   // Amarillo SABES
    [SerializeField] private Color narratorColor = new Color(0.85f, 0.9f, 1f, 1f); // Celeste narrador

    private void Start()
    {
        StartCoroutine(RunIntroRoutine());
    }

    private IEnumerator RunIntroRoutine()
    {
        yield return new WaitForSeconds(0.35f);

        // Esperar a que el sistema de diálogo esté inicializado
        int retries = 0;
        while (DialogueSystem.Instance == null && retries < 30)
        {
            yield return new WaitForSeconds(0.1f);
            retries++;
        }

        if (DialogueSystem.Instance == null) yield break;

        // Auto-resolver retratos si no están asignados en el inspector
        if (ferChoPortrait == null)
        {
            PlayerController pc = FindFirstObjectByType<PlayerController>();
            if (pc != null) ferChoPortrait = pc.GetComponent<SpriteRenderer>()?.sprite;
        }

        var lines = new List<DialogueSystem.DialogueLine>
        {
            new DialogueSystem.DialogueLine
            {
                speakerName = "Narrador",
                portrait    = null,
                nameColor   = narratorColor,
                text        = "Dolores Hidalgo, Gto. 1:40 PM. Tras una carrera frenética por toda la ciudad, Fercho finalmente divisa las rejas rojas del CBTis 75.",
                typeSpeed   = 0.032f
            },
            new DialogueSystem.DialogueLine
            {
                speakerName = "Fercho",
                portrait    = ferChoPortrait,
                nameColor   = ferChoColor,
                text        = "¡Por fin llegué! Solo me quedan 20 minutos para las 2:00 PM... ¡Tengo que cruzar la avenida ya y entregar el proyecto!",
                typeSpeed   = 0.035f
            },
            new DialogueSystem.DialogueLine
            {
                speakerName = "Narrador",
                portrait    = null,
                nameColor   = narratorColor,
                text        = "Sin embargo, el acceso principal está fuertemente bloqueado por una guardia de estudiantes del SABES, CECyTE y Rébsamen.",
                typeSpeed   = 0.032f
            },
            new DialogueSystem.DialogueLine
            {
                speakerName = "Guardia del SABES",
                portrait    = sabesPortrait,
                nameColor   = sabesColor,
                text        = "¡Alto ahí, Fercho! De aquí no pasas. El Director Mondragón dio la orden: nadie entra sin pagar la 'cuota extraordinaria'.",
                typeSpeed   = 0.035f
            },
            new DialogueSystem.DialogueLine
            {
                speakerName = "Fercho",
                portrait    = ferChoPortrait,
                nameColor   = ferChoColor,
                text        = "¡¿Una cuota para entrar a entregar mi proyecto?! ¡Eso es una extorsión del Director! ¡No voy a pagar un solo centavo!",
                typeSpeed   = 0.035f
            },
            new DialogueSystem.DialogueLine
            {
                speakerName = "Guardia del SABES",
                portrait    = sabesPortrait,
                nameColor   = sabesColor,
                text        = "¡Pues vas a tener que aprender por las malas! ¡Y más te vale tener reflejos, porque por esta avenida los camiones Zafiro pasan volando!",
                typeSpeed   = 0.035f
            },
            new DialogueSystem.DialogueLine
            {
                speakerName = "Fercho",
                portrait    = ferChoPortrait,
                nameColor   = ferChoColor,
                text        = "¡Que vengan los camiones y que venga quien sea! ¡Este semestre lo apruebo cueste lo que cueste!",
                typeSpeed   = 0.038f
            }
        };

        DialogueSystem.Instance.PlayDialogue(lines, () =>
        {
            Debug.Log("[IntroCutsceneNivel3] Diálogo inicial finalizado. ¡Comienza el combate!");
            Destroy(gameObject);
        });
    }
}

