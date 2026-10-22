using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// HealthPickup - Objeto coleccionable que restaura vida al jugador.
/// Coloca este script en un GameObject con SpriteRenderer y Collider2D (Is Trigger = true).
/// Sprites sugeridos: comida, botiquin, etc.
/// </summary>
public class HealthPickup : MonoBehaviour
{
    [Header("Configuracion")]
    [SerializeField] private int healAmount = 30;         // Vida que restaura
    [SerializeField] private float bobSpeed = 2f;         // Velocidad de flotacion
    [SerializeField] private float bobHeight = 0.18f;     // Altura de flotacion
    [SerializeField] private AudioClip pickupSound;       // Sonido al recogerlo

    [Header("Puntos Bonus")]
    [SerializeField] private int bonusPoints = 50;        // Score bonus al recogerlo

    private Vector3 startPos;
    private bool collected = false;

    private void Start()
    {
        startPos = transform.position;

        // Asegurar que el collider sea trigger
        Collider2D col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void Update()
    {
        if (collected) return;
        // Animacion de flotacion suave
        float newY = startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);

        // Rotacion lenta
        transform.Rotate(0f, 0f, 45f * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (collected) return;
        if (!other.CompareTag("Player")) return;

        Health playerHealth = other.GetComponent<Health>();
        if (playerHealth == null || playerHealth.IsDead) return;

        collected = true;
        StartCoroutine(CollectRoutine(other.transform, playerHealth));
    }

    private IEnumerator CollectRoutine(Transform player, Health playerHealth)
    {
        // Sonido
        if (pickupSound != null)
            AudioSource.PlayClipAtPoint(pickupSound, transform.position, 1f);

        // Curar al jugador
        playerHealth.Heal(healAmount);

        // Score bonus
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.AddScore(bonusPoints, "PICKUP");

        // Mostrar texto flotante "+30 HP"
        ShowFloatingText($"+{healAmount} HP", transform.position, new Color(0.3f, 1f, 0.4f, 1f));

        // Animacion: el objeto vuela hacia el jugador y desaparece
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        float elapsed = 0f;
        Vector3 startScale = transform.localScale;
        while (elapsed < 0.35f)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / 0.35f;
            transform.position = Vector3.Lerp(transform.position, player.position, t * 0.3f);
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            if (sr != null) sr.color = new Color(1f, 1f, 1f, 1f - t);
            yield return null;
        }

        Destroy(gameObject);
    }

    private void ShowFloatingText(string msg, Vector3 worldPos, Color color)
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        Texture2D tex = new Texture2D(4,4);
        Color[] px = new Color[16];
        for (int i = 0; i < px.Length; i++) px[i] = Color.white;
        tex.SetPixels(px); tex.Apply();
        Sprite white = Sprite.Create(tex, new Rect(0,0,4,4), new Vector2(0.5f,0.5f));

        GameObject txtObj = new GameObject("FloatingText", typeof(RectTransform), typeof(Text));
        txtObj.transform.SetParent(canvas.transform, false);
        RectTransform rt = txtObj.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(160f, 50f);

        // Convertir posicion del mundo a posicion de pantalla
        Vector2 screenPos = Camera.main.WorldToScreenPoint(worldPos);
        rt.position = screenPos;

        Text t2 = txtObj.GetComponent<Text>();
        t2.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t2.text = msg;
        t2.fontSize = 28;
        t2.fontStyle = FontStyle.Bold;
        t2.alignment = TextAnchor.MiddleCenter;
        t2.color = color;

        StartCoroutine(AnimateFloatingText(txtObj, rt));
    }

    private IEnumerator AnimateFloatingText(GameObject obj, RectTransform rt)
    {
        float elapsed = 0f;
        Vector2 startPos2 = rt.position;
        Text t = obj.GetComponent<Text>();
        while (elapsed < 1.2f)
        {
            elapsed += Time.deltaTime;
            rt.position = startPos2 + new Vector2(0f, elapsed * 60f);
            if (t != null) t.color = new Color(t.color.r, t.color.g, t.color.b, 1f - (elapsed / 1.2f));
            yield return null;
        }
        Destroy(obj);
    }
}
