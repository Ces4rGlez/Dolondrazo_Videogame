using UnityEngine;

/// <summary>
/// MobileInputBridge - Puente de input tactil para Dolondrazo Mobile.
/// Expone propiedades estaticas que PlayerController lee ademas del teclado.
/// MobileHUD escribe en estas propiedades cuando el jugador toca los controles.
/// No modifica PlayerController directamente: solo complementa el input.
/// </summary>
public static class MobileInputBridge
{
    // ---- Movimiento (Joystick virtual) ----
    public static float MoveX { get; set; } = 0f;
    public static float MoveY { get; set; } = 0f;

    // ---- Acciones de un frame (limpiadas en LateUpdate de MobileHUD) ----
    public static bool JumpPressedThisFrame { get; set; } = false;
    public static bool PunchPressedThisFrame { get; set; } = false;
    public static bool KickPressedThisFrame { get; set; } = false;
    public static bool PausePressedThisFrame { get; set; } = false;
    public static bool DashPressedThisFrame { get; set; } = false;

    // ---- Sprint (activado al empujar el joystick hacia afuera) ----
    public static bool RunHeld { get; set; } = false;

    // ---- Estado continuo para mashing de contusion ----
    public static bool AnyButtonTouchedThisFrame { get; set; } = false;

    public static void ClearFrameInputs()
    {
        JumpPressedThisFrame = false;
        PunchPressedThisFrame = false;
        KickPressedThisFrame = false;
        PausePressedThisFrame = false;
        DashPressedThisFrame = false;
    }

    public static void ResetContinuous()
    {
        MoveX = 0f;
        MoveY = 0f;
        RunHeld = false;
        AnyButtonTouchedThisFrame = false;
        ClearFrameInputs();
    }
}
