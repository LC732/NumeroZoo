using System;
using UnityEngine;

/// <summary>
/// Ponto único que guarda quantos animais devem aparecer.
/// Quem MUDA o valor: o conector (gesto vindo do Python) e o teclado.
/// Quem ESCUTA: os managers de cada cena (ParrotManager, peixes, abelhas...).
/// </summary>
public static class ContagemAnimais
{
    public const int Maximo = 10;

    public static int Valor { get; private set; }

    /// <summary>Disparado sempre que a quantidade muda.</summary>
    public static event Action<int> AoMudar;

    private static int ultimoGesto = -1;

    // Zera o estado ao dar Play (necessário se "Enter Play Mode Options" estiver ativo)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetarEstatico()
    {
        Valor = 0;
        ultimoGesto = -1;
        AoMudar = null;
    }

    /// <summary>Define a quantidade diretamente (usado pelo teclado).</summary>
    public static void Definir(int quantidade)
    {
        quantidade = Mathf.Clamp(quantidade, 0, Maximo);
        if (quantidade == Valor) return;
        Valor = quantidade;
        AoMudar?.Invoke(Valor);
    }

    /// <summary>
    /// Chame a cada pacote recebido do servidor, na THREAD PRINCIPAL (Update).
    /// Só repassa quando o gesto muda — assim o teclado não é sobrescrito
    /// a cada frame enquanto a mão estiver parada.
    /// </summary>
    public static void ReceberGesto(int gesto)
    {
        if (gesto == ultimoGesto) return;
        ultimoGesto = gesto;
        Definir(gesto);
    }

    /// <summary>Esquece o último gesto (ex.: ao reconectar com o servidor).</summary>
    public static void EsquecerGesto() => ultimoGesto = -1;
}
