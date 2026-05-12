using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Video;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class PersonagemVideos
{
    public string nome;                 // Nome do personagem
    public List<VideoClip> videos;      // Lista de vídeos do personagem
}

public class TesteVoz : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("UI")]
    public Image botaoImagem;
    public Color corNormal = Color.gray;
    public Color corPressionado = Color.white;

    [Header("IA")]
    public WhisperTranscriber whisper;

    [Header("Vídeo")]
    public VideoPlayer videoPlayer;
    public List<PersonagemVideos> personagens;   // Lista de personagens e seus vídeos
    private List<VideoClip> listaAtiva;          // Lista atualmente ativa
    private AudioClip clip;
    private bool gravando = false;
    private int personagemAtual = 0;             // índice do personagem ativo

    // Fila de áudios e controle de processamento
    private Queue<AudioClip> filaAudios = new Queue<AudioClip>();
    private bool processandoFila = false;
    private float delayEntreChamadas = 2f; // espera 2 segundos entre cada chamada

    void Start()
    {
        if (botaoImagem != null)
            botaoImagem.color = corNormal;

        // Inicializa lista ativa com o primeiro personagem
        if (personagens.Count > 0)
        {
            listaAtiva = personagens[0].videos;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (botaoImagem != null)
            botaoImagem.color = corPressionado;

        clip = Microphone.Start(null, false, 10, 44100);
        gravando = true;
        Debug.Log("Gravando...");
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (botaoImagem != null)
            botaoImagem.color = corNormal;

        if (gravando)
        {
            Microphone.End(null);
            gravando = false;

            if (clip != null)
            {
                filaAudios.Enqueue(clip); // adiciona à fila
                if (!processandoFila)
                    StartCoroutine(ProcessarFila());
            }
            clip = null;
        }
    }

    private IEnumerator ProcessarFila()
    {
        processandoFila = true;

        while (filaAudios.Count > 0)
        {
            AudioClip audioAtual = filaAudios.Dequeue();
            bool concluido = false;
            int tentativas = 3;

            while (!concluido && tentativas > 0)
            {
                yield return StartCoroutine(whisper.TranscreverAudio(audioAtual, (texto) =>
                {
                    if (!string.IsNullOrEmpty(texto))
                    {
                        Debug.Log("Transcrição: " + texto);
                        TocarVideo(texto.ToLower());
                        concluido = true;
                    }
                }));

                if (!concluido)
                {
                    tentativas--;
                    Debug.LogWarning("Falha na transcrição, tentando novamente em " + delayEntreChamadas + "s...");
                    yield return new WaitForSeconds(delayEntreChamadas);
                }
            }

            if (!concluido)
                Debug.LogError("Não foi possível transcrever o áudio após várias tentativas.");

            // Delay mínimo entre chamadas para evitar 429
            yield return new WaitForSeconds(delayEntreChamadas);
        }

        processandoFila = false;
    }

    void TocarVideo(string texto)
    {
        // Palavra-chave "mude" para trocar personagem
        if (texto.Contains("mude"))
        {
            personagemAtual++;
            if (personagemAtual >= personagens.Count)
                personagemAtual = 0; // volta para o primeiro

            listaAtiva = personagens[personagemAtual].videos;
            Debug.Log("Mudou para personagem: " + personagens[personagemAtual].nome);
            
            // Toca primeiro vídeo da nova lista
            if (listaAtiva.Count > 0)
            {
                videoPlayer.clip = listaAtiva[0];
                videoPlayer.Play();
                Debug.Log("Tocando vídeo do novo personagem: " + listaAtiva[0].name);
            }
            return;
        }

        // Tenta combinar com palavras-chave do personagem ativo
        for (int i = 0; i < listaAtiva.Count; i++)
        {
            if (texto.Contains(personagens[personagemAtual].nome.ToLower()))
            {
                videoPlayer.clip = listaAtiva[i];
                videoPlayer.Play();
                Debug.Log("Tocando vídeo: " + listaAtiva[i].name);
                return;
            }
        }

        // Vídeo padrão se não combinar nada
        if (listaAtiva.Count > 0)
        {
            videoPlayer.clip = listaAtiva[0];
            videoPlayer.Play();
            Debug.Log("Tocando vídeo padrão: " + listaAtiva[0].name);
        }
    }
}
