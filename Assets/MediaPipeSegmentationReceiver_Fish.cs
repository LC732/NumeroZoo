using UnityEngine;

/// <summary>
/// Conector da cena Aquario. Toda a parte de rede/câmera está em ReceptorMediaPipe.
/// Mantém o mesmo nome de classe e de campos do original, então as referências
/// que já estão na cena continuam valendo.
/// </summary>
public class MediaPipeSegmentationReceiver : ReceptorMediaPipe
{
    [SerializeField] public FishManager manager;
    [SerializeField] public SoundManager soundManager;

    protected override void AplicarQuantidade(int quantidade)
    {
        if (manager != null) manager.SetQuantidade(quantidade);
        if (soundManager != null) soundManager.SetQuantidadeSom(quantidade);
    }
}
