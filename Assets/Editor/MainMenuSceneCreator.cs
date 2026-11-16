using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// MainMenuSceneCreator - Script de editor que asegura que Pantalla_Inicio.unity
/// este correctamente registrada como Escena 0 en Build Settings.
/// </summary>
[InitializeOnLoad]
public static class MainMenuSceneCreator
{
    static MainMenuSceneCreator()
    {
        EditorApplication.delayCall += EnsurePantallaInicioSetup;
    }

    [MenuItem("Dolondrazo/Verificar Escena Pantalla_Inicio")]
    public static void EnsurePantallaInicioSetup()
    {
        string scenePath = "Assets/Scenes/Pantalla_Inicio.unity";

        // Asegurar que las escenas esten registradas en EditorBuildSettings
        List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

        bool hasPantallaInicio = false;
        int pantallaInicioIdx = -1;

        for (int i = 0; i < buildScenes.Count; i++)
        {
            if (buildScenes[i].path == scenePath)
            {
                hasPantallaInicio = true;
                pantallaInicioIdx = i;
                break;
            }
        }

        if (!hasPantallaInicio)
        {
            buildScenes.Insert(0, new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = buildScenes.ToArray();
            Debug.Log("[MainMenu] Escena Pantalla_Inicio.unity registrada como Escena 0 en Build Settings!");
        }
        else if (pantallaInicioIdx != 0)
        {
            var item = buildScenes[pantallaInicioIdx];
            buildScenes.RemoveAt(pantallaInicioIdx);
            buildScenes.Insert(0, item);
            EditorBuildSettings.scenes = buildScenes.ToArray();
            Debug.Log("[MainMenu] Escena Pantalla_Inicio.unity reordenada como Escena 0 en Build Settings!");
        }
    }
}
