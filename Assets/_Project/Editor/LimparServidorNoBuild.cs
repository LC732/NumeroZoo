using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// Depois de cada build do jogo, apaga da pasta gerada o servidor dos OUTROS sistemas.
/// Ex.: no build de Windows sobra só StreamingAssets/ServidorMaos/Windows.
/// Assim o jogo não carrega ~2x o tamanho à toa. Não mexe na pasta do projeto.
/// </summary>
public class LimparServidorNoBuild : IPostprocessBuildWithReport
{
    private const string PastaServidor = "ServidorMaos";

    public int callbackOrder => 0;

    public void OnPostprocessBuild(BuildReport report)
    {
        string manter;
        switch (report.summary.platform)
        {
            case BuildTarget.StandaloneWindows:
            case BuildTarget.StandaloneWindows64:
                manter = "Windows"; break;
            case BuildTarget.StandaloneLinux64:
                manter = "Linux"; break;
            case BuildTarget.StandaloneOSX:
                manter = "Mac"; break;
            default:
                return;
        }

        string saida = report.summary.outputPath;
        string pastaDados = report.summary.platform == BuildTarget.StandaloneOSX
            ? Path.Combine(saida, "Contents", "Resources", "Data")
            : Path.Combine(Path.GetDirectoryName(saida), Path.GetFileNameWithoutExtension(saida) + "_Data");

        string pasta = Path.Combine(pastaDados, "StreamingAssets", PastaServidor);
        if (!Directory.Exists(pasta))
        {
            Debug.LogWarning($"[Build] Pasta do servidor não encontrada no build: {pasta}");
            return;
        }

        foreach (var dir in Directory.GetDirectories(pasta))
        {
            if (Path.GetFileName(dir) != manter)
                Directory.Delete(dir, true);
        }

        if (!Directory.Exists(Path.Combine(pasta, manter)))
            Debug.LogWarning($"[Build] Não há servidor para {manter} em StreamingAssets/{PastaServidor}/{manter}. " +
                             "O jogo vai abrir, mas sem câmera.");
        else
            Debug.Log($"[Build] Servidor de mãos mantido só para {manter}.");
    }
}
