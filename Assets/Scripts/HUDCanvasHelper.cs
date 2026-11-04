using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// HUDCanvasHelper - Proveedor centralizado y seguro del Canvas para todo el juego (HUD, Barra de vida,
/// Reloj arcade, Puntuacion, Dialogos, Oleadas, Jefes, Pausa y Game Over).
/// Garantiza que ningun script del juego se emparenta accidentalmente a pantallas de carga DontDestroyOnLoad.
/// </summary>
public static class HUDCanvasHelper
{
    private static Canvas cachedCanvas;

    public static Canvas GetCanvas()
    {
        Scene activeScene = SceneManager.GetActiveScene();

        // 1. Si el canvas cacheado sigue vivo y activo en la escena actual, reutilizarlo
        if (cachedCanvas != null && cachedCanvas.gameObject != null)
        {
            if (cachedCanvas.gameObject.scene == activeScene && cachedCanvas.gameObject.activeInHierarchy)
            {
                return cachedCanvas;
            }
        }

        // 2. Buscar en la escena activa un canvas existente valido (excluyendo cargas y menus)
        Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        foreach (var c in canvases)
        {
            if (c.gameObject.scene == activeScene && 
                !c.name.Contains("Loading") && 
                !c.name.Contains("Ending") && 
                !c.name.Contains("Menu"))
            {
                cachedCanvas = c;
                return cachedCanvas;
            }
        }

        // 3. Si no existe, crear un Canvas nuevo y nitido en la escena activa
        GameObject canvasObj = new GameObject("HUD_Canvas");
        SceneManager.MoveGameObjectToScene(canvasObj, activeScene);

        cachedCanvas = canvasObj.AddComponent<Canvas>();
        cachedCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        cachedCanvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        scaler.dynamicPixelsPerUnit = 2.5f; // Maxima nitidez en fuentes pixel art y texto HD

        canvasObj.AddComponent<GraphicRaycaster>();
        return cachedCanvas;
    }
}
