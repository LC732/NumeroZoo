using UnityEngine;

/// <summary>
/// Guarda a resposta do jogador sobre o uso da câmera durante a sessão.
/// O balão aparece uma vez por execução do jogo (voltar ao menu não pergunta de novo).
/// Os conectores das cenas devem checar <see cref="Permitido"/> antes de conectar.
/// </summary>
public static class ConsentimentoCamera
{
    public static bool Respondido { get; private set; }
    public static bool Permitido { get; private set; }

    /// <summary>
    /// Use nos conectores. No Editor, se você der Play direto numa cena de jogo
    /// (sem passar pelo menu), libera a câmera para facilitar os testes.
    /// </summary>
    public static bool PodeUsarCamera => Permitido || (Application.isEditor && !Respondido);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetarEstatico()
    {
        Respondido = false;
        Permitido = false;
    }

    public static void Responder(bool permitir)
    {
        Respondido = true;
        Permitido = permitir;

        if (permitir)
            ServidorPython.GarantirInstancia().Iniciar();
        else if (ServidorPython.Instancia != null)
            ServidorPython.Instancia.Parar();
    }
}
