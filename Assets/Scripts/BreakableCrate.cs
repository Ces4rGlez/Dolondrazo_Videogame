using System.Collections;
using UnityEngine;

/// <summary>
/// BreakableCrate - Caja de madera rompible al estilo beat-em-up.
/// Fercho debe golpearla para romperla y obtener los alimentos/botiquin.
/// Requiere: SpriteRenderer, BoxCollider2D, Layer = "Enemy" o "Breakable".
/// </summary>
[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
public class BreakableCrate : MonoBehaviour
{
    [Header("Resistencia")]
    [SerializeField] private int hitsToBreak = 2;      // Golpes necesarios para romper

    [Header("Contenido al romper")]
    [SerializeField] private GameObject foodPickupPrefab; // Prefab del alimento (con HealthPickup.cs)
    [SerializeField] private int foodCount = 1;           // Cuantos alimentos salen

    [Header("Efectos visuales")]
    [SerializeField] private Color damagedTint = new Color(0.6f, 0.35f, 0.1f, 1f); // Color al danar
    [SerializeField] private AudioClip hitSound;    // Sonido al golpear
    [SerializeField] private AudioClip breakSound;  // Sonido al romper

    [Header("Score")]
    [SerializeField] private int breakPoints = 25;

    private int hitsReceived = 0;
    private bool isBroken = false;
    private SpriteRenderer sr;
    private Color originalColor;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) originalColor = sr.color;

        // Asegurar que el collider NO sea trigger (para que Fercho la golpee)
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col != null) col.isTrigger = false;
    }

    /// <summary>
    /// Llamado por DetectHit de PlayerController cuando golpea la caja.
    /// La caja debe estar en la LayerMask enemyLayers de Fercho.
    /// </summary>
    public void ReceiveHit()
    {
        if (isBroken) return;

        hitsReceived++;

        if (hitSound != null)
            AudioSource.PlayClipAtPoint(hitSound, transform.position, 1f);

        // Efecto de golpe: sacudida + tinte
        StartCoroutine(HitShakeRoutine());

        if (HitEffects.Instance != null)
            HitEffects.Instance.SpawnHitParticles(transform.position, new Color(0.8f, 0.5f, 0.1f, 1f));

        if (hitsReceived >= hitsToBreak)
        {
            StartCoroutine(BreakRoutine());
        }
        else
        {
            // Tinte de dano segun golpes recibidos
            if (sr != null)
            {
                float t = (float)hitsReceived / hitsToBreak;
                sr.color = Color.Lerp(originalColor, damagedTint, t);
            }
        }
    }

    private IEnumerator HitShakeRoutine()
    {
        Vector3 original = transform.localPosition;
        float elapsed = 0f;
        while (elapsed < 0.15f)
        {
            elapsed += Time.deltaTime;
            float x = Random.Range(-0.08f, 0.08f);
            transform.localPosition = original + new Vector3(x, 0f, 0f);
            yield return null;
        }
        transform.localPosition = original;
    }

    private IEnumerator BreakRoutine()
    {
        isBroken = true;

        // Desactivar collider
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col != null) col.enabled = false;

        if (breakSound != null)
            AudioSource.PlayClipAtPoint(breakSound, transform.position, 1f);

        // Score por romper la caja
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.RegisterCrateBroken(breakPoints);

        // Particulas de madera (amarillo-marron)
        if (HitEffects.Instance != null)
        {
            for (int i = 0; i < 3; i++)
            {
                HitEffects.Instance.SpawnHitParticles(
                    transform.position + new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(-0.3f, 0.3f), 0f),
                    new Color(0.72f, 0.45f, 0.1f, 1f)
                );
                yield return new WaitForSeconds(0.04f);
            }
        }

        // Animacion de destruccion: escalar hacia 0 y romper
        float elapsed = 0f;
        Vector3 startScale = transform.localScale;
        while (elapsed < 0.3f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / 0.3f;
            // Squish horizontal al romperse
            transform.localScale = new Vector3(
                Mathf.Lerp(startScale.x, 0f, t * t),
                Mathf.Lerp(startScale.y, startScale.y * 1.3f, t * 0.5f),
                1f
            );
            if (sr != null) sr.color = new Color(1f, 0.8f, 0.3f, 1f - t);
            yield return null;
        }

        // Spawnear alimento(s)
        SpawnFood();

        Destroy(gameObject);
    }

    private void SpawnFood()
    {
        if (foodPickupPrefab == null) return;

        for (int i = 0; i < foodCount; i++)
        {
            Vector3 spawnPos = transform.position + new Vector3(
                Random.Range(-0.4f, 0.4f),
                Random.Range(0.1f, 0.5f),
                0f
            );
            Instantiate(foodPickupPrefab, spawnPos, Quaternion.identity);
        }
    }

    // Dibujar gizmo en la escena para ver facilmente la caja
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.8f, 0.5f, 0.1f, 0.4f);
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.8f);
    }
}
