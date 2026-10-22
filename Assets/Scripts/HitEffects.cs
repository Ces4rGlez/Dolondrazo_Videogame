using System.Collections;
using UnityEngine;

/// <summary>
/// HitEffects - Particulas de impacto y screen shake al recibir golpes.
/// Coloca este script en el GameObject "HUD_Manager" o en la camara principal.
/// </summary>
public class HitEffects : MonoBehaviour
{
    public static HitEffects Instance { get; private set; }

    [Header("Screen Shake")]
    [SerializeField] private float shakeIntensityNormal = 0.12f;
    [SerializeField] private float shakeIntensityHeavy  = 0.28f;
    [SerializeField] private float shakeDuration        = 0.2f;

    [Header("Particulas de Impacto")]
    [SerializeField] private int particleCount = 6;

    private Transform cameraTransform;
    private Vector3 cameraOriginalPos;
    private Coroutine shakeCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        if (Camera.main != null)
        {
            cameraTransform  = Camera.main.transform;
            cameraOriginalPos = cameraTransform.localPosition;
        }
    }

    // -------------------------------------------------------
    // API publica
    // -------------------------------------------------------

    /// <summary>Sacudida normal (golpe de minion).</summary>
    public void ShakeNormal() => TriggerShake(shakeIntensityNormal, shakeDuration);

    /// <summary>Sacudida fuerte (golpe del jefe).</summary>
    public void ShakeHeavy()  => TriggerShake(shakeIntensityHeavy, shakeDuration * 1.5f);

    /// <summary>Genera particulas de impacto en la posicion del mundo.</summary>
    public void SpawnHitParticles(Vector3 worldPos, Color color)
    {
        StartCoroutine(HitParticleRoutine(worldPos, color));
    }

    // -------------------------------------------------------
    // Implementacion
    // -------------------------------------------------------

    private void TriggerShake(float intensity, float duration)
    {
        if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
        shakeCoroutine = StartCoroutine(ShakeRoutine(intensity, duration));
    }

    private IEnumerator ShakeRoutine(float intensity, float duration)
    {
        if (cameraTransform == null) yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = 1f - (elapsed / duration);
            float x = Random.Range(-1f, 1f) * intensity * progress;
            float y = Random.Range(-1f, 1f) * intensity * progress;
            cameraTransform.localPosition = cameraOriginalPos + new Vector3(x, y, 0f);
            yield return null;
        }

        cameraTransform.localPosition = cameraOriginalPos;
        shakeCoroutine = null;
    }

    private IEnumerator HitParticleRoutine(Vector3 worldPos, Color color)
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null || Camera.main == null) yield break;

        Texture2D tex = new Texture2D(4, 4);
        Color[] px = new Color[16];
        for (int i = 0; i < px.Length; i++) px[i] = Color.white;
        tex.SetPixels(px); tex.Apply();
        Sprite white = Sprite.Create(tex, new Rect(0,0,4,4), new Vector2(0.5f,0.5f));

        Vector2 screenPos = Camera.main.WorldToScreenPoint(worldPos);

        for (int i = 0; i < particleCount; i++)
        {
            GameObject p = new GameObject("HitParticle", typeof(RectTransform), typeof(UnityEngine.UI.Image));
            p.transform.SetParent(canvas.transform, false);

            RectTransform rt = p.GetComponent<RectTransform>();
            rt.position  = screenPos;
            rt.sizeDelta = new Vector2(Random.Range(6f, 14f), Random.Range(6f, 14f));

            UnityEngine.UI.Image img = p.GetComponent<UnityEngine.UI.Image>();
            img.sprite = white;
            img.color  = color;

            Vector2 dir = new Vector2(Random.Range(-1f, 1f), Random.Range(0.2f, 1f)).normalized;
            float speed = Random.Range(80f, 180f);
            StartCoroutine(AnimateParticle(p, rt, img, dir, speed));
        }
        yield break;
    }

    private IEnumerator AnimateParticle(GameObject obj, RectTransform rt, UnityEngine.UI.Image img, Vector2 dir, float speed)
    {
        float elapsed = 0f;
        float lifetime = 0.5f;
        Vector2 startPos = rt.position;

        while (elapsed < lifetime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / lifetime;
            rt.position = startPos + dir * speed * elapsed;
            img.color   = new Color(img.color.r, img.color.g, img.color.b, 1f - t);
            rt.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, t * t);
            yield return null;
        }
        Destroy(obj);
    }
}
