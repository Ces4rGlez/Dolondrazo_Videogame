using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// IntroCutscene - Cutscene de introduccion al Nivel 1.
/// Agrega este script a un GameObject vacio llamado "Intro_Cutscene" en la escena.
/// Auto-carga los retratos de Fercho y CECyTE si no se asignaron en el inspector.
/// </summary>
public class IntroCutscene : MonoBehaviour
{
    [Header("Retratos de Personajes")]
    [SerializeField] private Sprite ferChoPortrait;      // Sprite idle de Fercho
    [SerializeField] private Sprite enemyPortrait;       // Sprite idle del CECyTE

    [Header("Colores de Nombre")]
    [SerializeField] private Color ferChoColor   = new Color(0.25f, 0.82f, 1f, 1f);   // Azul CBTis brillante
    [SerializeField] private Color enemyColor    = new Color(1f, 0.35f, 0.2f, 1f);    // Rojo CECyTE
    [SerializeField] private Color narratorColor = new Color(1f, 0.85f, 0.25f, 1f);   // Dorado narrador

    private void Start()
    {
        PlayerController pc = FindFirstObjectByType<PlayerController>();
        StartCoroutine(DelayedStart(pc));
    }

    private System.Collections.IEnumerator DelayedStart(PlayerController pc)
    {
        yield return new WaitForSeconds(0.4f);

        // Esperar que DialogueSystem este listo
        int tries = 0;
        while (DialogueSystem.Instance == null && tries < 25)
        {
            yield return new WaitForSeconds(0.1f);
            tries++;
        }

        if (DialogueSystem.Instance == null) yield break;

        // Auto-cargar portraits si no estan asignados
        if (ferChoPortrait == null)
        {
            if (pc != null) ferChoPortrait = pc.GetComponent<SpriteRenderer>()?.sprite;
            if (ferChoPortrait == null) ferChoPortrait = Resources.Load<Sprite>("Sprites/Iddle");
        }
        if (enemyPortrait == null)
        {
            var enemy = FindFirstObjectByType<EnemyAI>();
            if (enemy != null) enemyPortrait = enemy.GetComponent<SpriteRenderer>()?.sprite;
            if (enemyPortrait == null) enemyPortrait = Resources.Load<Sprite>("Sprites/Enemy01_Walk");
        }

        var lines = new List<DialogueSystem.DialogueLine>
        {
            new DialogueSystem.DialogueLine
            {
                speakerName = "Narrador",
                portrait    = null,
                nameColor   = narratorColor,
                text        = "Dolores Hidalgo, Guanajuato. Ultimo dia habil del semestre en el CBTis 75.",
                typeSpeed   = 0.032f
            },
            new DialogueSystem.DialogueLine
            {
                speakerName = "Narrador",
                portrait    = null,
                nameColor   = narratorColor,
                text        = "El proyecto final debe entregarse antes de las 2:00 PM... o Fercho reprueba el ano.",
                typeSpeed   = 0.032f
            },
            new DialogueSystem.DialogueLine
            {
                speakerName = "Fercho",
                portrait    = ferChoPortrait,
                nameColor   = ferChoColor,
                text        = "!No puede ser! El transporte no pasa... ¡Ni un solo camion en toda la Avenida!",
                typeSpeed   = 0.035f
            },
            new DialogueSystem.DialogueLine
            {
                speakerName = "Fercho",
                portrait    = ferChoPortrait,
                nameColor   = ferChoColor,
                text        = "El celular con 0% de bateria y el tiempo encima. ¡Tengo que correr a pie!",
                typeSpeed   = 0.035f
            },
            new DialogueSystem.DialogueLine
            {
                speakerName = "Narrador",
                portrait    = null,
                nameColor   = narratorColor,
                text        = "Pero al llegar a la Calzada de los Heroes, el camino esta bloqueado...",
                typeSpeed   = 0.032f
            },
            new DialogueSystem.DialogueLine
            {
                speakerName = "Estudiante CECyTE",
                portrait    = enemyPortrait,
                nameColor   = enemyColor,
                text        = "!Ey, morro del CBTis! Esta calle es del CECyTE. De aqui no pasas.",
                typeSpeed   = 0.035f
            },
            new DialogueSystem.DialogueLine
            {
                speakerName = "Fercho",
                portrait    = ferChoPortrait,
                nameColor   = ferChoColor,
                text        = "!Tengo que entregar mi proyecto final! ¡Dejenme pasar!",
                typeSpeed   = 0.035f
            },
            new DialogueSystem.DialogueLine
            {
                speakerName = "Estudiante CECyTE",
                portrait    = enemyPortrait,
                nameColor   = enemyColor,
                text        = "!Si quieres pasar, vas a tener que ganartelo a golpes!",
                typeSpeed   = 0.035f
            },
            new DialogueSystem.DialogueLine
            {
                speakerName = "Fercho",
                portrait    = ferChoPortrait,
                nameColor   = ferChoColor,
                text        = "No tengo opcion... ¡Abran paso que se me hace tarde!",
                typeSpeed   = 0.04f
            }
        };

        DialogueSystem.Instance.PlayDialogue(lines, () =>
        {
            Debug.Log("[IntroCutscene] Intro completada. Fercho libre para luchar.");
            Destroy(gameObject);
        });
    }
}
