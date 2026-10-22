using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// WaveManager - Sistema de oleadas para el Nivel 1.
/// Coloca este script en un GameObject vacio llamado "WaveManager" en la escena.
/// Configura cada oleada en el Inspector: enemigos a spawnear, posiciones, y camara.
/// </summary>
public class WaveManager : MonoBehaviour
{
    [System.Serializable]
    public class EnemySpawnPoint
    {
        public Transform spawnPoint;   // Transform del punto de aparicion
        public int count = 1;          // Cuantos enemigos spawnear en este punto
    }

    [System.Serializable]
    public class Wave
    {
        [Header("Info de Oleada")]
        public string waveName = "Oleada 1";

        [Header("Enemigos")]
        public GameObject enemyPrefab;             // Prefab del enemigo a spawnear
        public List<EnemySpawnPoint> spawnPoints;  // Puntos de aparicion con cantidad

        [Header("Camara")]
        public float cameraLockX = -1f;    // X hasta donde se bloquea la camara (-1 = no bloquear)
        public float cameraMaxX = 24.5f;   // Limite maximo de camara para esta oleada

        [Header("Avance del Jugador")]
        public float playerAdvanceX = -1f; // Si > 0, la camara avanza hasta aqui al completar la oleada
    }

    [Header("Oleadas del Nivel")]
    [SerializeField] private List<Wave> waves = new List<Wave>();

    [Header("Configuracion")]
    [SerializeField] private float timeBetweenSpawns = 0.3f;  // Segundos entre cada spawn en la oleada
    [SerializeField] private float waveCompleteDelay = 1.5f;  // Segundos antes de avanzar al completar

    private int currentWaveIndex = 0;
    private bool waveActive = false;
    private List<GameObject> currentEnemies = new List<GameObject>();
    private CameraFollow cameraFollow;

    // Banner de oleada (HUD)
    private UnityEngine.UI.Text waveAnnouncerText;
    private GameObject waveAnnouncerObj;

    private void Start()
    {
        cameraFollow = FindFirstObjectByType<CameraFollow>();
        SetupWaveAnnouncer();

        if (waves.Count > 0)
        {
            StartCoroutine(StartWave(0));
        }
    }

    private void SetupWaveAnnouncer()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        Texture2D tex = new Texture2D(4, 4);
        Color[] px = new Color[16];
        for (int i = 0; i < px.Length; i++) px[i] = Color.white;
        tex.SetPixels(px); tex.Apply();
        Sprite white = Sprite.Create(tex, new Rect(0,0,4,4), new Vector2(0.5f,0.5f));

        // Fondo del anuncio
        waveAnnouncerObj = new GameObject("Wave_Announcer", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        waveAnnouncerObj.transform.SetParent(canvas.transform, false);
        RectTransform rt = waveAnnouncerObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, 80f);
        rt.sizeDelta = new Vector2(600f, 70f);
        var img = waveAnnouncerObj.GetComponent<UnityEngine.UI.Image>();
        img.sprite = white;
        img.color = new Color(0.05f, 0.05f, 0.15f, 0.88f);

        // Texto del anuncio
        GameObject txtObj = new GameObject("Wave_Text", typeof(RectTransform), typeof(UnityEngine.UI.Text));
        txtObj.transform.SetParent(waveAnnouncerObj.transform, false);
        RectTransform rtTxt = txtObj.GetComponent<RectTransform>();
        rtTxt.anchorMin = Vector2.zero;
        rtTxt.anchorMax = Vector2.one;
        rtTxt.sizeDelta = Vector2.zero;
        waveAnnouncerText = txtObj.GetComponent<UnityEngine.UI.Text>();
        Font arcadeFont = Resources.Load<Font>("Fonts/ArcadeBold");
        waveAnnouncerText.font = arcadeFont != null ? arcadeFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (txtObj.GetComponent<Outline>() == null)
        {
            var outl = txtObj.AddComponent<Outline>();
            outl.effectColor = Color.black;
            outl.effectDistance = new Vector2(2f, -2f);
        }
        waveAnnouncerText.fontSize = 28;
        waveAnnouncerText.fontStyle = FontStyle.Bold;
        waveAnnouncerText.alignment = TextAnchor.MiddleCenter;
        waveAnnouncerText.color = new Color(1f, 0.85f, 0.2f, 1f);

        waveAnnouncerObj.SetActive(false);
    }

    private IEnumerator StartWave(int index)
    {
        if (index >= waves.Count) yield break;

        currentWaveIndex = index;
        waveActive = true;
        currentEnemies.Clear();

        Wave wave = waves[index];

        // Configurar camara para esta oleada
        if (cameraFollow != null)
        {
            // Bloquear camara si la oleada lo requiere
            if (wave.cameraLockX >= 0f)
            {
                cameraFollow.SetMaxX(wave.cameraLockX);
            }
            cameraFollow.SetLock(false); // Permitir que el jugador se mueva
        }

        // Mostrar anuncio de oleada
        yield return StartCoroutine(ShowWaveAnnouncement(wave.waveName, index + 1));

        // Spawnear enemigos con delay entre cada uno
        foreach (var spawnEntry in wave.spawnPoints)
        {
            if (spawnEntry.spawnPoint == null || wave.enemyPrefab == null) continue;

            for (int i = 0; i < spawnEntry.count; i++)
            {
                // Pequena variacion de posicion para que no se apilen
                Vector3 spawnPos = spawnEntry.spawnPoint.position + new Vector3(
                    Random.Range(-0.4f, 0.4f),
                    Random.Range(-0.3f, 0.3f),
                    0f
                );

                GameObject enemy = Instantiate(wave.enemyPrefab, spawnPos, Quaternion.identity);
                currentEnemies.Add(enemy);
                yield return new WaitForSeconds(timeBetweenSpawns);
            }
        }

        // Bloquear camara completamente durante la oleada
        if (cameraFollow != null && wave.cameraLockX >= 0f)
        {
            cameraFollow.SetLock(true);
        }

        // Esperar a que todos los enemigos mueran
        yield return StartCoroutine(WaitForWaveClear());
    }

    private IEnumerator WaitForWaveClear()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.5f);

            // Limpiar entradas null (enemigos ya destruidos)
            currentEnemies.RemoveAll(e => e == null);

            // Verificar si quedan enemigos vivos
            bool anyAlive = false;
            foreach (var enemy in currentEnemies)
            {
                if (enemy != null && enemy.activeInHierarchy)
                {
                    Health h = enemy.GetComponent<Health>();
                    if (h != null && !h.IsDead)
                    {
                        anyAlive = true;
                        break;
                    }
                }
            }

            if (!anyAlive)
            {
                // Oleada completada
                yield return StartCoroutine(OnWaveComplete(waves[currentWaveIndex]));
                yield break;
            }
        }
    }

    private IEnumerator OnWaveComplete(Wave completedWave)
    {
        waveActive = false;
        Debug.Log($"[WaveManager] Oleada '{completedWave.waveName}' completada!");

        // Mostrar mensaje de "Sigue avanzando!"
        yield return StartCoroutine(ShowMessage("SIGUE AVANZANDO!", 1.5f));

        // Desbloquear camara y dejar al jugador avanzar
        if (cameraFollow != null)
        {
            cameraFollow.SetLock(false);
            cameraFollow.SetMaxX(completedWave.cameraMaxX);
        }

        yield return new WaitForSeconds(waveCompleteDelay);

        // Iniciar la siguiente oleada si existe
        int next = currentWaveIndex + 1;
        if (next < waves.Count)
        {
            StartCoroutine(StartWave(next));
        }
        else
        {
            Debug.Log("[WaveManager] Todas las oleadas completadas. El jefe deberia estar activo.");
        }
    }

    private IEnumerator ShowWaveAnnouncement(string waveName, int waveNumber)
    {
        if (waveAnnouncerText == null || waveAnnouncerObj == null) yield break;

        waveAnnouncerText.text = $">> OLEADA {waveNumber}: {waveName.ToUpper()} ";
        waveAnnouncerObj.SetActive(true);

        // Fade in
        var img = waveAnnouncerObj.GetComponent<UnityEngine.UI.Image>();
        for (float t = 0; t < 1f; t += Time.deltaTime * 4f)
        {
            float alpha = Mathf.Lerp(0f, 0.88f, t);
            img.color = new Color(0.05f, 0.05f, 0.15f, alpha);
            waveAnnouncerText.color = new Color(1f, 0.85f, 0.2f, Mathf.Lerp(0f, 1f, t));
            yield return null;
        }

        yield return new WaitForSeconds(2f);

        // Fade out
        for (float t = 0; t < 1f; t += Time.deltaTime * 3f)
        {
            float alpha = Mathf.Lerp(0.88f, 0f, t);
            img.color = new Color(0.05f, 0.05f, 0.15f, alpha);
            waveAnnouncerText.color = new Color(1f, 0.85f, 0.2f, Mathf.Lerp(1f, 0f, t));
            yield return null;
        }

        waveAnnouncerObj.SetActive(false);
    }

    private IEnumerator ShowMessage(string msg, float duration)
    {
        if (waveAnnouncerText == null || waveAnnouncerObj == null) yield break;

        waveAnnouncerText.text = msg;
        waveAnnouncerObj.SetActive(true);

        var img = waveAnnouncerObj.GetComponent<UnityEngine.UI.Image>();
        img.color = new Color(0.05f, 0.15f, 0.05f, 0.88f);
        waveAnnouncerText.color = new Color(0.4f, 1f, 0.4f, 1f);

        yield return new WaitForSeconds(duration);

        waveAnnouncerObj.SetActive(false);
    }

    // Permite activar una oleada especifica desde otro script (ej: BossHUD)
    public void ForceStartWave(int index)
    {
        StopAllCoroutines();
        StartCoroutine(StartWave(index));
    }
}

