using UnityEngine;

public class SoundManager : MonoBehaviour
{
    [Header("Sons — índice 0 = quantidade 0, índice 1 = quantidade 1...")]
    public AudioClip[] sons; // arrasta os 11 sons no Inspector na ordem

    private AudioSource audioSource;
    private int ultimaQuantidade = -1;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void SetQuantidadeSom(int quantidade)
    {
        if (quantidade == ultimaQuantidade) return;
        ultimaQuantidade = quantidade;

        if (quantidade >= 0 && quantidade < sons.Length && sons[quantidade] != null)
            audioSource.PlayOneShot(sons[quantidade]);
    }
}