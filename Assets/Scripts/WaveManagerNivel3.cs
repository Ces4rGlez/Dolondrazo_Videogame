using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// WaveManagerNivel3 - Gestor de oleadas para el Nivel 3 (CBTis 75).
/// Combina las 3 facciones: CECyTE, Rebsamen y SABES.
/// Administra el cruce del autobus Zafiro como peligro ambiental y transporte final,
/// y el despliegue del arma especial Regla T.
/// </summary>
public class WaveManagerNivel3 : MonoBehaviour
{
    [System.Serializable]
    public class SpawnEntry
    {
        public GameObject enemyPrefab;   // CECyTE, Rebsamen o SABES
        public Transform spawnPoint;     // Posicion de aparicion
        public int count = 1;            // Cantidad
    }

    [System.Serializable]
    public class Wave3
    {
        [Header("Identificacion")]
        public string waveTitle = "ACCESO AL CBTIS 75";
        public string waveSubtitle = "Guardias del SABES y CECyTE resguardando la avenida";

        [Header("Enemigos a Desplegar")]
        public List<SpawnEntry> spawns = new List<SpawnEntry>();

        [Header("Control de Camara")]
        public float cameraLockX = 7.0f;
        public float cameraMaxX = 7.0f;

        [Header("Peligro del Autobus")]
        public bool triggerBusHazard = false;
        public float busHazardDelay = 8.0f;

        [Header("Recompensa al Completar")]
        public GameObject rewardPickup;
        public Transform rewardSpawnPoint;
    }

    [Header("Oleadas del Nivel 3")]
    [SerializeField] private List<Wave3> waves = new List<Wave3>();

    [Header("Prefabs de Enemigos")]
    [SerializeField] private GameObject prefabSabes;
    [SerializeField] private GameObject prefabRebsamen;
    [SerializeField] private GameObject prefabCECyTE;

    [Header("Referencias de Nivel")]
    [SerializeField] private BusHazard busHazard;
    [SerializeField] private GameObject reglaTPrefab;
    [SerializeField] private Transform reglaTSpawnPoint;

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
        if (busHazard == null) busHazard = FindFirstObjectByType<BusHazard>();

        SetupWaveBannerUI();

        if (reglaTPrefab != null)
        {
            Vector3 spawnPos = reglaTSpawnPoint != null ? reglaTSpawnPoint.position : new Vector3(2.5f, -7.5f, 0f);
            Instantiate(reglaTPrefab, spawnPos, Quaternion.identity);
        }

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

        waveBannerObj = new GameObject("Wave3_Banner", typeof(RectTransform), typeof(Image));
        waveBannerObj.transform.SetParent(canvas.transform, false);
        RectTransform rt = waveBannerObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, 75f);
        rt.sizeDelta = new Vector2(650f, 90f);

        Image img = waveBannerObj.GetComponent<Image>();
        img.sprite = white;
        img.color = new Color(0.06f, 0.1f, 0.14f, 0.94f);

        GameObject topBar = new GameObject("CBTis_Bar", typeof(RectTransform), typeof(Image));
        topBar.transform.SetParent(waveBannerObj.transform, false);
        RectTransform rtb = topBar.GetComponent<RectTransform>();
        rtb.anchorMin = new Vector2(0f, 1f);
        rtb.anchorMax = new Vector2(1f, 1f);
        rtb.pivot = new Vector2(0.5f, 1f);
        rtb.sizeDelta = new Vector2(0f, 4f);
        topBar.GetComponent<Image>().sprite = white;
        topBar.GetComponent<Image>().color = new Color(0.2f, 0.85f, 0.6f, 1f);

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
        waveTitleText.color = new Color(0.3f, 1f, 0.7f, 1f);

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
        waveSubtitleText.color = new Color(0.9f, 0.95f, 1f, 0.92f);

        waveBannerObj.SetActive(false);
    }

    private IEnumerator StartWaveRoutine(int index)
    {
        if (index >= waves.Count) yield break;

        currentWaveIndex = index;
        waveActive = true;
        activeEnemies.Clear();
        Wave3 wave = waves[index];

        yield return StartCoroutine(ShowBannerRoutine($">> OLEADA {index + 1}: {wave.waveTitle.ToUpper()} <<", wave.waveSubtitle, 2.2f));

        foreach (var entry in wave.spawns)
        {
            GameObject prefabToUse = entry.enemyPrefab != null ? entry.enemyPrefab : prefabSabes;
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

        if (cameraFollow != null && wave.cameraLockX > 0f)
        {
            cameraFollow.SetLock(true);
        }

        // Despachar el autobus de forma periodica en esta oleada (solo si wave.triggerBusHazard = true)
        if (busHazard != null && wave.triggerBusHazard)
        {
            StartCoroutine(PeriodicBusHazardRoutine(wave.busHazardDelay));
        }

        yield return StartCoroutine(WaitForWaveClearRoutine());
    }

    private IEnumerator PeriodicBusHazardRoutine(float initialDelay)
    {
        yield return new WaitForSeconds(initialDelay);

        while (waveActive)
        {
            if (busHazard != null)
            {
                busHazard.TriggerPassingHazard();  // usa defaultLaneY configurado en el Inspector del BusHazard
            }
            // Repetir el cruce del autobus cada 13 segundos mientras la oleada continue activa
            yield return new WaitForSeconds(13.0f);
        }
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

    private IEnumerator OnWaveCompletedRoutine(Wave3 completedWave)
    {
        waveActive = false;
        Debug.Log($"[WaveManagerNivel3] !Oleada '{completedWave.waveTitle}' completada!");

        if (completedWave.rewardPickup != null && completedWave.rewardSpawnPoint != null)
        {
            Instantiate(completedWave.rewardPickup, completedWave.rewardSpawnPoint.position, Quaternion.identity);
        }

        yield return StartCoroutine(ShowBannerRoutine("!ZONA DESPEJADA!", "Avanza hacia la entrada del CBTis 75", 1.8f));

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
            if (cameraFollow != null)
            {
                cameraFollow.SetLock(false);
                cameraFollow.SetMaxX(28.0f);
            }

            yield return StartCoroutine(ShowBannerRoutine("!AREA LIBRE DE RIVALES!", "!Ahi viene el autobus Zafiro!", 2.5f));

            if (busHazard != null)
            {
                busHazard.TriggerArrivalSequence(new Vector2(20.0f, -7.6f));
            }
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
