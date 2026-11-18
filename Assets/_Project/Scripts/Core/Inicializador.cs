using UnityEngine;

/// <summary>
/// Roda sozinho quando o jogo inicia (não precisa estar em nenhuma cena).
/// Cria uma vez, e mantém entre as cenas:
///  • o menu de configurações (prefab em Resources/MenuConfiguracoes)
///  • o leitor de teclado 0–9 dos animais
/// </summary>
public static class Inicializador
{
    private const string CaminhoPrefabMenu = "MenuConfiguracoes";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Criar()
    {
        var prefab = Resources.Load<GameObject>(CaminhoPrefabMenu);
        if (prefab != null)
        {
            var menu = Object.Instantiate(prefab);
            menu.name = "MenuConfiguracoes";
            Object.DontDestroyOnLoad(menu);
        }
        else
        {
            Debug.LogWarning("Prefab Resources/MenuConfiguracoes não encontrado. " +
                             "Use o menu Ambientes > Gerar Menu Principal e Configurações.");
        }

        var teclado = new GameObject("TecladoAnimais");
        teclado.AddComponent<TecladoAnimais>();
        Object.DontDestroyOnLoad(teclado);
    }
}
