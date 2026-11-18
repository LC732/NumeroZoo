using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Menu inicial: carrega uma das cenas de ambiente.
/// Os botões chamam CarregarCena("NomeDaCena") pelo OnClick.
/// As cenas precisam estar em File > Build Profiles / Build Settings.
/// </summary>
public class MenuPrincipal : MonoBehaviour
{
    public const string NomeCenaMenu = "MenuPrincipal";

    public void CarregarCena(string nomeCena)
    {
        if (!Application.CanStreamedLevelBeLoaded(nomeCena))
        {
            Debug.LogError($"A cena '{nomeCena}' não está no Build Settings.");
            return;
        }
        SceneManager.LoadScene(nomeCena);
    }

    public void Sair()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
