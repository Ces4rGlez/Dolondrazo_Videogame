using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BossCutsceneDirector - Diálogo cinemático con el Jefe Final: "Lic. Mondragón - Director del CBTis 75".
/// Se ejecuta automáticamente cuando el Director original entra en combate.
/// </summary>
public class BossCutsceneDirector : MonoBehaviour
{
    [Header("Retratos")]
    [SerializeField] private Sprite ferChoPortrait;
    [SerializeField] private Sprite bossPortrait;

    [Header("Colores")]
    [SerializeField] private Color ferChoColor = new Color(0.25f, 0.82f, 1f, 1f);  // Azul Fercho
    [SerializeField] private Color bossColor   = new Color(1f, 0.35f, 0.35f, 1f); // Rojo institucional autoritario

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
                speakerName = "Lic. Mondragón",
                portrait    = bossPortrait,
                nameColor   = bossColor,
                text        = "Vaya, vaya... Fernando. Tienes agallas para haber llegado hasta la explanada del plantel.",
                typeSpeed   = 0.035f
            },
            new DialogueSystem.DialogueLine
            {
                speakerName = "Fercho",
                portrait    = ferChoPortrait,
                nameColor   = ferChoColor,
                text        = "¡Director Mondragón! ¡Usted planeó todo esto! ¡Bloqueó el transporte y mandó a todas las escuelas para cobrarnos extraordinarios!",
                typeSpeed   = 0.038f
            },
            new DialogueSystem.DialogueLine
            {
                speakerName = "Lic. Mondragón",
                portrait    = bossPortrait,
                nameColor   = bossColor,
                text        = "Por favor, muchacho... esto es el mundo real. Las instituciones se administran con mano dura y con recursos. ¿De verdad creíste que tu ridículo proyecto cambiaría algo?",
                typeSpeed   = 0.034f
            },
            new DialogueSystem.DialogueLine
            {
                speakerName = "Fercho",
                portrait    = ferChoPortrait,
                nameColor   = ferChoColor,
                text        = "¡Mi proyecto representa noches sin dormir y el esfuerzo de mi equipo! ¡No voy a permitir que su corrupción nos robe el semestre!",
                typeSpeed   = 0.038f
            },
            new DialogueSystem.DialogueLine
            {
                speakerName = "Lic. Mondragón",
                portrait    = bossPortrait,
                nameColor   = bossColor,
                text        = "Qué conmovedor... pero en mi escuela se hace lo que yo digo. ¡Guárdate tu discurso para el hospital!",
                typeSpeed   = 0.035f
            },
            new DialogueSystem.DialogueLine
            {
                speakerName = "Fercho",
                portrait    = ferChoPortrait,
                nameColor   = ferChoColor,
                text        = "¡Se le acabó su negocio, Director! ¡Aquí se gradúa quien se esfuerza, no quien usted decida!",
                typeSpeed   = 0.038f
            }
        };

        DialogueSystem.Instance.PlayDialogue(lines, () =>
        {
            Debug.Log("[BossCutsceneDirector] ¡Diálogo final completado! ¡Inicia la batalla decisiva!");
        });
    }
}

