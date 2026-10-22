using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BossCutscene - Dialogo al encontrar al Jefe (Alumno de Taller).
/// Coloca este script en el Boss_Taller. Se activa automaticamente
/// cuando BossAI despierta al jefe (primera vez).
/// </summary>
public class BossCutscene : MonoBehaviour
{
    [Header("Retratos")]
    [SerializeField] private Sprite ferChoPortrait;   // Portrait de Fercho
    [SerializeField] private Sprite bossPortrait;     // Primer frame de Carcajada_Boss_1

    [Header("Colores")]
    [SerializeField] private Color ferChoColor = new Color(0.3f, 0.75f, 1f, 1f);
    [SerializeField] private Color bossColor   = new Color(1f, 0.55f, 0.05f, 1f); // Naranja amenazante

    private bool triggered = false;
    private BossAI bossAI;

    private void Start()
    {
        bossAI = GetComponent<BossAI>();
    }

    /// <summary>Llamado por BossAI al despertar al jefe.</summary>
    public void TriggerBossCutscene()
    {
        if (triggered) return;
        triggered = true;
        StartCoroutine(RunCutscene());
    }

    private IEnumerator RunCutscene()
    {
        while (DialogueSystem.Instance == null) yield return null;

        var lines = new List<DialogueSystem.DialogueLine>
        {
            new DialogueSystem.DialogueLine
            {
                speakerName = "???",
                portrait    = bossPortrait,
                nameColor   = bossColor,
                text        = "JA JA JA JA... !Mira quien llego hasta aqui!",
                typeSpeed   = 0.05f
            },
            new DialogueSystem.DialogueLine
            {
                speakerName = "Fercho",
                portrait    = ferChoPortrait,
                nameColor   = ferChoColor,
                text        = "!Quien eres tu? !Quitate del camino, necesito pasar!",
                typeSpeed   = 0.04f
            },
            new DialogueSystem.DialogueLine
            {
                speakerName = "Alumno de Taller",
                portrait    = bossPortrait,
                nameColor   = bossColor,
                text        = "Soy el que manda en esta calle. El Alumno de Taller del CECyTE. !Y no le abro paso a ningun CBTis!",
                typeSpeed   = 0.038f
            },
            new DialogueSystem.DialogueLine
            {
                speakerName = "Fercho",
                portrait    = ferChoPortrait,
                nameColor   = ferChoColor,
                text        = "Ya derrote a todos tus companeros. !Tu eres el ultimo obstaculo!",
                typeSpeed   = 0.04f
            },
            new DialogueSystem.DialogueLine
            {
                speakerName = "Alumno de Taller",
                portrait    = bossPortrait,
                nameColor   = bossColor,
                text        = "!Esos eran los principiantes! Conmigo es diferente. !Preparate para reprobar... y para el hospital!",
                typeSpeed   = 0.038f
            },
            new DialogueSystem.DialogueLine
            {
                speakerName = "Fercho",
                portrait    = ferChoPortrait,
                nameColor   = ferChoColor,
                text        = "Mi proyecto se entrega hoy. !Nada ni nadie me lo va a impedir!",
                typeSpeed   = 0.045f
            }
        };

        DialogueSystem.Instance.PlayDialogue(lines, () =>
        {
            Debug.Log("[BossCutscene] Dialogo del jefe completado. Iniciando batalla.");
        });
    }
}
