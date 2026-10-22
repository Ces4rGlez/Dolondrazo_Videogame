using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// MainMenuSceneCreator - Script de editor que crea y registra automaticamente
/// la escena MainMenu.unity al compilar, asegurando que sea la Escena 0 en Build Settings.
/// </summary>
[InitializeOnLoad]
public static class MainMenuSceneCreator
{
    static MainMenuSceneCreator()
    {
        EditorApplication.delayCall += EnsureMainMenuSetup;
    }

    [MenuItem("Dolondrazo/Regenerar Escena MainMenu")]
    public static void EnsureMainMenuSetup()
    {
        string scenePath = "Assets/Scenes/MainMenu.unity";

        // 1. Crear la escena si no existe
        if (!File.Exists(scenePath))
        {
            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Crear el controlador del Menu Principal
            GameObject manager = new GameObject("MainMenu_Manager");
            manager.AddComponent<AudioManager>();
            manager.AddComponent<MainMenuManager>();

            EditorSceneManager.SaveScene(newScene, scenePath);
            Debug.Log("[MainMenu] Escena MainMenu.unity creada exitosamente!");
        }

        // 2. Asegurar que las escenas esten registradas en EditorBuildSettings
        List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

        bool hasMainMenu = false;
        int mainMenuIdx = -1;

        for (int i = 0; i < buildScenes.Count; i++)
        {
            if (buildScenes[i].path == scenePath)
            {
                hasMainMenu = true;
                mainMenuIdx = i;
                break;
            }
        }

        if (!hasMainMenu)
        {
            // Insertar al inicio como Escena 0
            buildScenes.Insert(0, new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = buildScenes.ToArray();
            Debug.Log("[MainMenu] Escena MainMenu.unity registrada como Escena 0 en Build Settings!");
        }
        else if (mainMenuIdx != 0)
        {
            // Mover al indice 0
            var item = buildScenes[mainMenuIdx];
            buildScenes.RemoveAt(mainMenuIdx);
            buildScenes.Insert(0, item);
            EditorBuildSettings.scenes = buildScenes.ToArray();
            Debug.Log("[MainMenu] Escena MainMenu.unity reordenada como Escena 0 en Build Settings!");
        }

        // Asegurar que SampleScene este en la lista
        string samplePath = "Assets/Scenes/SampleScene.unity";
        bool hasSample = false;
        foreach (var s in EditorBuildSettings.scenes)
        {
            if (s.path == samplePath) { hasSample = true; break; }
        }
        if (!hasSample && File.Exists(samplePath))
        {
            buildScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            buildScenes.Add(new EditorBuildSettingsScene(samplePath, true));
            EditorBuildSettings.scenes = buildScenes.ToArray();
        }
    }
}
