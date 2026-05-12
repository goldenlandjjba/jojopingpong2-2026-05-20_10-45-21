using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Text))]
public class EfeitoDigitacao : MonoBehaviour
{
    [Header("Configurações da Animação")]
    [Tooltip("Tempo em segundos entre cada letra. (Ex: 0.02 é rápido, 0.1 é lento)")]
    public float tempoEntreLetras = 0.02f;
    
    [Header("Som (Opcional)")]
    public AudioSource audioSource;
    public AudioClip somDeTecla;

    private Text textoUI;
    private string textoCompleto;
    private Coroutine corrotinaAtual;

    void Awake()
    {
        textoUI = GetComponent<Text>();
        textoCompleto = textoUI.text; 
        
        // Limpa o texto logo no início para evitar que ele "pisque" inteiro na tela antes de sumir
        textoUI.text = ""; 
    }

    void OnEnable()
    {
        if (corrotinaAtual != null) StopCoroutine(corrotinaAtual);
        corrotinaAtual = StartCoroutine(AnimarTexto());
    }

    IEnumerator AnimarTexto()
    {
        textoUI.text = "";

        // 🔥 A CORREÇÃO ESTÁ AQUI: 
        // Espera uma pequena fração de segundo antes de digitar a primeira letra.
        // Isso impede que o som toque no momento em que o jogo carrega a cena.
        yield return new WaitForSeconds(0.1f);

        foreach (char letra in textoCompleto.ToCharArray())
        {
            textoUI.text += letra; 

            if (audioSource != null && somDeTecla != null)
            {
                audioSource.PlayOneShot(somDeTecla);
            }

            yield return new WaitForSeconds(tempoEntreLetras);
        }
    }
}