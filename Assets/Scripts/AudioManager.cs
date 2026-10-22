using UnityEngine;

/// <summary>
/// AudioManager - Control central de volumen (Master, Musica y Efectos de Sonido) y reproduccion de pistas.
/// Persiste entre escenas (DontDestroyOnLoad).
/// Guarda las preferencias en PlayerPrefs automaticamente.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Clips Generales")]
    [SerializeField] private AudioClip defaultClickSound;

    public float MasterVolume { get; private set; } = 1f;
    public float MusicVolume  { get; private set; } = 0.8f;
    public float SFXVolume    { get; private set; } = 0.8f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Crear fuentes de audio si no existen
        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
        }
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;
        }

        if (defaultClickSound == null)
        {
            defaultClickSound = Resources.Load<AudioClip>("Sounds/Menu Select");
        }

        LoadSettings();
    }

    private void LoadSettings()
    {
        MasterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        MusicVolume  = PlayerPrefs.GetFloat("MusicVolume",  0.8f);
        SFXVolume    = PlayerPrefs.GetFloat("SFXVolume",    0.8f);

        ApplyVolumes();
    }

    private void ApplyVolumes()
    {
        AudioListener.volume = MasterVolume;
        if (bgmSource != null) bgmSource.volume = MusicVolume;
        if (sfxSource != null) sfxSource.volume = SFXVolume;
    }

    public void SetMasterVolume(float value)
    {
        MasterVolume = Mathf.Clamp01(value);
        AudioListener.volume = MasterVolume;
        PlayerPrefs.SetFloat("MasterVolume", MasterVolume);
        PlayerPrefs.Save();
    }

    public void SetMusicVolume(float value)
    {
        MusicVolume = Mathf.Clamp01(value);
        if (bgmSource != null) bgmSource.volume = MusicVolume;
        PlayerPrefs.SetFloat("MusicVolume", MusicVolume);
        PlayerPrefs.Save();
    }

    public void SetSFXVolume(float value)
    {
        SFXVolume = Mathf.Clamp01(value);
        if (sfxSource != null) sfxSource.volume = SFXVolume;
        PlayerPrefs.SetFloat("SFXVolume", SFXVolume);
        PlayerPrefs.Save();
    }

    public void PlayBGM(AudioClip clip, bool loop = true)
    {
        if (clip == null) return;
        if (bgmSource.clip == clip && bgmSource.isPlaying) return;

        bgmSource.clip = clip;
        bgmSource.loop = loop;
        bgmSource.volume = MusicVolume;
        bgmSource.Play();
    }

    public void StopBGM()
    {
        if (bgmSource != null) bgmSource.Stop();
    }

    public void PlaySFX(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null || sfxSource == null) return;
        sfxSource.PlayOneShot(clip, volumeScale * SFXVolume);
    }

    public void PlayClickSound()
    {
        if (defaultClickSound != null)
        {
            PlaySFX(defaultClickSound, 1f);
        }
    }
}
