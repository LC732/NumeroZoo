using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Leitura de teclado que funciona tanto com o Input System novo
/// quanto com o Input Manager antigo (o que estiver ativo no projeto).
/// </summary>
public static class Teclas
{
#if ENABLE_INPUT_SYSTEM
    private static readonly Key[] digitos =
        { Key.Digit0, Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4,
          Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9 };

    private static readonly Key[] numpad =
        { Key.Numpad0, Key.Numpad1, Key.Numpad2, Key.Numpad3, Key.Numpad4,
          Key.Numpad5, Key.Numpad6, Key.Numpad7, Key.Numpad8, Key.Numpad9 };
#endif

    /// <summary>Retorna o dígito (0–9) apertado neste frame, ou -1.</summary>
    public static int DigitoApertado()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        if (kb == null) return -1;
        for (int i = 0; i < 10; i++)
            if (kb[digitos[i]].wasPressedThisFrame || kb[numpad[i]].wasPressedThisFrame)
                return i;
        return -1;
#else
        for (int i = 0; i < 10; i++)
            if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha0 + i)) ||
                Input.GetKeyDown((KeyCode)((int)KeyCode.Keypad0 + i)))
                return i;
        return -1;
#endif
    }

    public static bool EscApertado()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = Keyboard.current;
        return kb != null && kb.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }


public static int AmbienteApertado()
{
#if ENABLE_INPUT_SYSTEM
    var kb = Keyboard.current;
    if (kb == null) return -1;
    if (kb.zKey.wasPressedThisFrame) return 0;
    if (kb.xKey.wasPressedThisFrame) return 1;
    if (kb.cKey.wasPressedThisFrame) return 2;
    return -1;
#else
    if (Input.GetKeyDown(KeyCode.Z)) return 0;
    if (Input.GetKeyDown(KeyCode.X)) return 1;
    if (Input.GetKeyDown(KeyCode.C)) return 2;
    return -1;
#endif
}

}