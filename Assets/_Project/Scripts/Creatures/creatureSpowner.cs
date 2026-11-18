using UnityEngine;
using UnityEngine.UI;
using System.Net.Sockets;
using System.Threading;
using System;
using System.Collections.Generic;

public class PlanetManager : MonoBehaviour
{
    [Header("Objetos")]

    [SerializeField] private List<GameObject> creatures = new List<GameObject>();  // ou prefabsToSpawn

    public void SetQuantidade(int quantidade)
    {
        quantidade = Mathf.Clamp(quantidade, 0, 200);

        // Ativar os primeiros 'quantidade' objetos
        for (int i = 0; i < creatures.Count; i++)
        {
            bool deveEstarVisivel = i <= quantidade-1;
            creatures[i].gameObject.SetActive(deveEstarVisivel);
        }
    }
}