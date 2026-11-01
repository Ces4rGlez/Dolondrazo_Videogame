using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// WaveManagerNivel2 - Gestor de oleadas para el Nivel 2 (Cinepolis / Paseo Dolores).
/// Combina enemigos del CECyTE y de la Rebsamen en oleadas progresivas.
/// </summary>
public class WaveManagerNivel2 : MonoBehaviour
{
    [System.Serializable]
    public class SpawnEntry
    {
        public GameObject enemyPrefab;   // CECyTE o Rebsamen
        public Transform spawnPoint;     // Posicion de aparicion
        public int count = 1;            // Cantidad
    }

    [System.Serializable]
    public class Wave2
    {
        [Header("Identificacion")]
        public string waveTitle = "PARADERO CINEPOLIS";
        public string waveSubtitle = "Estudiantes de la Rebsamen bloqueando el paso";

        [Header("Enemigos a Desplegar")]
        public List<SpawnEntry> spawns = new List<SpawnEntry>();

        [Header("Control de Camara")]
        public float cameraLockX = 0f;
        public float cameraMaxX = 24.5f;

        [Header("Recompensa al Completar")]
        public GameObject rewardPickup;
        public Transform rewardSpawnPoint;
    }

    [Header("Oleadas del Nivel 2")]
    [SerializeField] private List<Wave2> waves = new List<Wave2>();

    [Header("Prefabs de Enemigos (Fallback)")]
    [SerializeField] private GameObject prefabRebsamen;
    [SerializeField] private GameObject prefabCECyTE;

    [Header("Parametros")]
    [SerializeField] private float timeBetweenSpawns = 0.35f;
    [SerializeField] private float waveClearDelay = 1.5f;

    private int currentWaveIndex = 0;
    private bool waveActive = false;
    private List<GameObject> activeEnemies = new List<GameObject>();
    private CameraFollow cameraFollow;

    // UI de Anuncio
    private GameObject waveBannerObj;
    private Text waveTitleText;
    private Text waveSubtitleText;

    private void Start()
    {
        cameraFollow = FindFirstObjectByType<CameraFollow>();
        SetupWaveBannerUI();

        if (waves.Count > 0)
        {
            StartCoroutine(StartWaveRoutine(0));
        }
    }

    private void SetupWaveBannerUI()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        Texture2D tex = new Texture2D(4, 4);
        Color[] px = new Color[16];
        for (int i = 0; i < px.Length; i++) px[i] = Color.white;
        tex.SetPixels(px); tex.Apply();
        Sprite white = Sprite.Create(tex, new Rect(0,0,4,4), new Vector2(0.5f,0.5f));

        waveBannerObj = new GameObject("Wave2_Banner", typeof(RectTransform), typeof(Image));
        waveBannerObj.transform.SetParent(canvas.transform, false);
        RectTransform rt = waveBannerObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, 75f);
        rt.sizeDelta = new Vector2(650f, 90f);

        Image img = waveBannerObj.GetComponent<Image>();
        img.sprite = white;
        img.color = new Color(0.04f, 0.08f, 0.16f, 0.92f);

        // Borde superior dorado
        GameObject topBar = new GameObject("Gold_Bar", typeof(RectTransform), typeof(Image));
        topBar.transform.SetParent(waveBannerObj.transform, false);
        RectTransform rtb = topBar.GetComponent<RectTransform>();
        rtb.anchorMin = new Vector2(0f, 1f);
        rtb.anchorMax = new Vector2(1f, 1f);
        rtb.pivot = new Vector2(0.5f, 1f);
        rtb.sizeDelta = new Vector2(0f, 4f);
        topBar.GetComponent<Image>().sprite = white;
        topBar.GetComponent<Image>().color = new Color(1f, 0.85f, 0.15f, 1f);

        // Texto Titulo
        GameObject tObj = new GameObject("WaveTitle", typeof(RectTransform), typeof(Text));
        tObj.transform.SetParent(waveBannerObj.transform, false);
        RectTransform rtt = tObj.GetComponent<RectTransform>();
        rtt.anchorMin = new Vector2(0f, 0.45f);
        rtt.anchorMax = new Vector2(1f, 1f);
        rtt.offsetMin = new Vector2(15f, 0f);
        rtt.offsetMax = new Vector2(-15f, -8f);
        waveTitleText = tObj.GetComponent<Text>();
        waveTitleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        waveTitleText.fontSize = 24;
        waveTitleText.fontStyle = FontStyle.Bold;
        waveTitleText.alignment = TextAnchor.MiddleCenter;
        waveTitleText.color = new Color(1f, 0.85f, 0.2f, 1f);

        // Texto Subtitulo
        GameObject sObj = new GameObject("WaveSub", typeof(RectTransform), typeof(Text));
        sObj.transform.SetParent(waveBannerObj.transform, false);
        RectTransform rts = sObj.GetComponent<RectTransform>();
        rts.anchorMin = new Vector2(0f, 0f);
        rts.anchorMax = new Vector2(1f, 0.45f);
        rts.offsetMin = new Vector2(15f, 8f);
        rts.offsetMax = new Vector2(-15f, 0f);
        waveSubtitleText = sObj.GetComponent<Text>();
        waveSubtitleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        waveSubtitleText.fontSize = 17;
        waveSubtitleText.fontStyle = FontStyle.Normal;
        waveSubtitleText.alignment = TextAnchor.MiddleCenter;
        waveSubtitleText.color = new Color(0.85f, 0.95f, 1f, 0.9f);

        waveBannerObj.SetActive(false);
    }

    private IEnumerator StartWaveRoutine(int index)
    {
        if (index >= waves.Count) yield break;

        currentWaveIndex = index;
        waveActive = true;
        activeEnemies.Clear();
        Wave2 wave = waves[index];

        // Anuncio en pantalla
        yield return StartCoroutine(ShowBannerRoutine($">> OLEADA {index + 1}: {wave.waveTitle.ToUpper()} <<", wave.waveSubtitle, 2.2f));

        // Spawneo de enemigos
        foreach (var entry in wave.spawns)
        {
            GameObject prefabToUse = entry.enemyPrefab != null ? entry.enemyPrefab : prefabRebsamen;
            if (prefabToUse == null) continue;

            Transform sp = entry.spawnPoint;
            Vector3 basePos = sp != null ? sp.position : transform.position;

            for (int i = 0; i < entry.count; i++)
            {
                Vector3 spawnPos = basePos + new Vector3(
                    Random.Range(-0.45f, 0.45f),
                    Random.Range(-0.25f, 0.25f),
                    0f
                );

                GameObject enemy = Instantiate(prefabToUse, spawnPos, Quaternion.identity);
                activeEnemies.Add(enemy);
                yield return new WaitForSeconds(timeBetweenSpawns);
            }
        }

        // Bloquear camara si aplica
        if (cameraFollow != null && wave.cameraLockX > 0f)
        {
            cameraFollow.SetLock(true);
        }

        // Monitorear eliminacion de enemigos
        yield return StartCoroutine(WaitForWaveClearRoutine());
    }

    private IEnumerator WaitForWaveClearRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.4f);

            activeEnemies.RemoveAll(e => e == null);

            bool anyAlive = false;
            foreach (var enemy in activeEnemies)
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
                yield return StartCoroutine(OnWaveCompletedRoutine(waves[currentWaveIndex]));
                yield break;
            }
        }
    }

    private IEnumerator OnWaveCompletedRoutine(Wave2 completedWave)
    {
        waveActive = false;
        Debug.Log($"[WaveManagerNivel2] Oleada '{completedWave.waveTitle}' completada!");

        // Spawneo de recompensa (comida/vida)
        if (completedWave.rewardPickup != null && completedWave.rewardSpawnPoint != null)
        {
            Instantiate(completedWave.rewardPickup, completedWave.rewardSpawnPoint.position, Quaternion.identity);
        }

        yield return StartCoroutine(ShowBannerRoutine("¡ZONA DESPEJADA!", "Sigue avanzando hacia Paseo Dolores", 1.8f));

        // Desbloquear camara
        if (cameraFollow != null)
        {
            cameraFollow.SetLock(false);
            cameraFollow.SetMaxX(completedWave.cameraMaxX);
        }

        yield return new WaitForSeconds(waveClearDelay);

        int next = currentWaveIndex + 1;
        if (next < waves.Count)
        {
            StartCoroutine(StartWaveRoutine(next));
        }
        else
        {
            // Todas las oleadas vencidas: ampliar camara al sector de la Jefa y mostrar banner
            if (cameraFollow != null)
            {
                cameraFollow.SetLock(false);
                cameraFollow.SetMaxX(36f);
            }
            yield return StartCoroutine(ShowBannerRoutine("\u00a1LIDERESA DEL R\u00c9BSAMEN!", "\u00a1Valentina bloquea la entrada principal!", 2.8f));
            Debug.Log("[WaveManagerNivel2] \u00a1Oleadas completadas! Valentina aparece en escena.");
        }
    }

    private IEnumerator ShowBannerRoutine(string title, string sub, float duration)
    {
        if (waveBannerObj == null || waveTitleText == null) yield break;

        waveTitleText.text = title;
        if (waveSubtitleText != null) waveSubtitleText.text = sub;
        waveBannerObj.SetActive(true);

        yield return new WaitForSeconds(duration);

        waveBannerObj.SetActive(false);
    }
}
