using UnityEngine;

/// <summary>
/// GameSaveManager - Sistema de persistencia y progreso de Dolondrazo.
/// Gestiona niveles desbloqueados y puntuaciones maximas mediante PlayerPrefs.
/// </summary>
public static class GameSaveManager
{
    private const string KEY_UNLOCKED_LEVEL = "Dolondrazo_MaxLevelUnlocked";
    private const string KEY_HIGH_SCORE = "Dolondrazo_HighScore";
    private const string KEY_LAST_SELECTED_LEVEL = "Dolondrazo_LastSelectedLevel";

    /// <summary>
    /// Retorna el numero maximo de nivel desbloqueado (1, 2 o 3). Minimo 1 por defecto.
    /// </summary>
    public static int GetMaxUnlockedLevel()
    {
        return Mathf.Clamp(PlayerPrefs.GetInt(KEY_UNLOCKED_LEVEL, 1), 1, 3);
    }

    /// <summary>
    /// Desbloquea un nuevo nivel si es superior al actual guardado.
    /// </summary>
    public static void UnlockLevel(int levelNumber)
    {
        int current = GetMaxUnlockedLevel();
        if (levelNumber > current)
        {
            PlayerPrefs.SetInt(KEY_UNLOCKED_LEVEL, Mathf.Clamp(levelNumber, 1, 3));
            PlayerPrefs.Save();
            Debug.Log($"[GameSaveManager] !NIVEL {levelNumber} DESBLOQUEADO CON EXITO!");
        }
    }

    /// <summary>
    /// Comprueba si un nivel especifico esta disponible para jugar.
    /// </summary>
    public static bool IsLevelUnlocked(int levelNumber)
    {
        return levelNumber <= GetMaxUnlockedLevel();
    }

    /// <summary>
    /// Guarda el ultimo nivel seleccionado por el jugador.
    /// </summary>
    public static void SetLastSelectedLevel(int levelNumber)
    {
        PlayerPrefs.SetInt(KEY_LAST_SELECTED_LEVEL, Mathf.Clamp(levelNumber, 1, 3));
        PlayerPrefs.Save();
    }

    public static int GetLastSelectedLevel()
    {
        return Mathf.Clamp(PlayerPrefs.GetInt(KEY_LAST_SELECTED_LEVEL, 1), 1, GetMaxUnlockedLevel());
    }

    /// <summary>
    /// Guarda un nuevo record de puntuacion si supera al anterior.
    /// </summary>
    public static void SaveHighScore(int score)
    {
        int current = PlayerPrefs.GetInt(KEY_HIGH_SCORE, 0);
        if (score > current)
        {
            PlayerPrefs.SetInt(KEY_HIGH_SCORE, score);
            PlayerPrefs.Save();
        }
    }

    public static int GetHighScore()
    {
        return PlayerPrefs.GetInt(KEY_HIGH_SCORE, 0);
    }

    /// <summary>
    /// Obtiene el nombre exacto de la escena para el nivel especificado.
    /// </summary>
    public static string GetLevelSceneName(int levelNumber)
    {
        switch (levelNumber)
        {
            case 1: return "Nivel_1_Calzada";
            case 2: return "Nivel_2_Cinepolis";
            case 3: return "Nivel_3_CBTIS75";
            default: return "Nivel_1_Calzada";
        }
    }

    public static string GetLevelDisplayName(int levelNumber)
    {
        switch (levelNumber)
        {
            case 1: return "NIVEL 1: CALZADA DE LOS HEROES";
            case 2: return "NIVEL 2: EL CENTRO Y CINEPOLIS";
            case 3: return "NIVEL 3: CBTIS 75 (BATALLA FINAL)";
            default: return $"NIVEL {levelNumber}";
        }
    }

    /// <summary>
    /// Borra todo el progreso para iniciar partida desde cero.
    /// </summary>
    public static void ResetProgress()
    {
        PlayerPrefs.DeleteKey(KEY_UNLOCKED_LEVEL);
        PlayerPrefs.DeleteKey(KEY_LAST_SELECTED_LEVEL);
        PlayerPrefs.Save();
        Debug.Log("[GameSaveManager] Progreso de niveles reiniciado al Nivel 1.");
    }
}
