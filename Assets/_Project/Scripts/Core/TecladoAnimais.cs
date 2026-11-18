using UnityEngine;
using UnityEngine.SceneManagement;

public class TecladoAnimais : MonoBehaviour
{
    static readonly string[] ambientes = { "Aquario", "Jardin", "Cerrado" };

    void Update()
    {
        // Não mexe nos animais enquanto o menu de configurações estiver aberto
        if (MenuConfiguracoes.Aberto) return;

        int a = Teclas.AmbienteApertado();
        if (a >= 0 && (ConsentimentoCamera.Respondido || Application.isEditor)
            && SceneManager.GetActiveScene().name != ambientes[a])
        {
            SceneManager.LoadScene(ambientes[a]);
            return;
        }

        int d = Teclas.DigitoApertado();
        if (d < 0) return;

        if (d == 0)
            ContagemAnimais.Definir(ContagemAnimais.Valor == 0 ? 10 : 0);
        else
            ContagemAnimais.Definir(d);
    }
}