using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BossCutsceneSabesLider - Cinemática de diálogo al enfrentarse al Minijefe "El Prefecto / Líder del SABES".
/// Se activa automáticamente en cuanto el Líder SABES entra en combate.
/// </summary>
public class BossCutsceneSabesLider : MonoBehaviour
{
    [Header("Retratos")]
    [SerializeField] private Sprite ferChoPortrait;
    [SerializeField] private Sprite bossPortrait;

    [Header("Colores")]
    [SerializeField] private Color ferChoColor = new Color(0.25f, 0.82f, 1f, 1f);  // Azul Fercho
    [SerializeField] private Color bossColor   = new Color(1f, 0.75f, 0.1f, 1f);  // Amarillo SABES amenazante

    private bool triggered = false;

    private void Awake()
    {
        if (bossPortrait == null)
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null) bossPortrait = sr.sprite;
        }
    }

    public void TriggerCutscene()
    {
        if (triggered) return;
        triggered = true;
        StartCoroutine(RunCutsceneRoutine());
    }

    private IEnumerator RunCutsceneRoutine()
    {
        while (DialogueSystem.Instance == null) yield return null;

        if (ferChoPortrait == null)
        {
            PlayerController pc = FindFirstObjectByType<PlayerController>();
            if (pc != null) ferChoPortrait = pc.GetComponent<SpriteRenderer>()?.sprite;
        }

        var lines = new List<DialogueSystem.DialogueLine>
        {
            new DialogueSystem.DialogueLine
            {
                speakerName = "El Prefecto",
                portrait    = bossPortrait,
                nameColor   = bossColor,
                text        = "¡Ya fue suficiente payasada! ¿Quién te crees para pasar por encima de mi guardia, chamaco?",
                typeSpeed   = 0.035f
            },
            new DialogueSystem.DialogueLine
            {
                speakerName = "Fercho",
                portrait    = ferChoPortrait,
                nameColor   = ferChoColor,
                text        = "¡Llevas 8 semestres en la prepa y todavía le haces los mandados sucios al Director! ¡Hazte a un lado!",
                typeSpeed   = 0.038f
            },
            new DialogueSystem.DialogueLine
            {
                speakerName = "El Prefecto",
                portrait    = bossPortrait,
                nameColor   = bossColor,
                text        = "¡A mí me respetas! Aquí mando yo, y nadie entra al CBTis sin mi permiso. ¡Te voy a romper esa maqueta en la cabeza!",
                typeSpeed   = 0.035f
            },
            new DialogueSystem.DialogueLine
            {
                speakerName = "Fercho",
                portrait    = ferChoPortrait,
                nameColor   = ferChoColor,
                text        = "¡Inténtalo! ¡A ver si aguantas el ritmo de un estudiante con entrega final a contrarreloj!",
                typeSpeed   = 0.038f
            }
        };

        DialogueSystem.Instance.PlayDialogue(lines, () =>
        {
            Debug.Log("[BossCutsceneSabesLider] ¡Diálogo del Líder SABES completado! ¡A luchar!");
        });
    }
}

