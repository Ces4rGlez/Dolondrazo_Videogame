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
    // Valor entre -1 y 1 en cada eje
    public static float MoveX { get; set; } = 0f;
    public static float MoveY { get; set; } = 0f;

    // ---- Acciones (un frame) ----
    // Estas se activan un frame y se consumen al leerlas
    private static bool _jumpPressed;
    private static bool _punchPressed;
    private static bool _kickPressed;
    private static bool _pausePressed;

    // Dash y Correr continuo (boton Y en movil)
    private static bool _dashPressed;
    public static bool RunHeld { get; set; } = false;

    public static bool JumpPressedThisFrame
    {
        get { bool v = _jumpPressed; _jumpPressed = false; return v; }
        set { if (value) _jumpPressed = true; }
    }

    public static bool PunchPressedThisFrame
    {
        get { bool v = _punchPressed; _punchPressed = false; return v; }
        set { if (value) _punchPressed = true; }
    }

    public static bool KickPressedThisFrame
    {
        get { bool v = _kickPressed; _kickPressed = false; return v; }
        set { if (value) _kickPressed = true; }
    }

    public static bool PausePressedThisFrame
    {
        get { bool v = _pausePressed; _pausePressed = false; return v; }
        set { if (value) _pausePressed = true; }
    }

    public static bool DashPressedThisFrame
    {
        get { bool v = _dashPressed; _dashPressed = false; return v; }
        set { if (value) _dashPressed = true; }
    }

    // ---- Estado continuo para mashing de contusion ----
    public static bool AnyButtonTouchedThisFrame { get; set; } = false;

    public static void ResetContinuous()
    {
        MoveX = 0f;
        MoveY = 0f;
        AnyButtonTouchedThisFrame = false;
    }
}
