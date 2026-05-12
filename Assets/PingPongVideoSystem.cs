using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;

public class PingPongVideoSystem : MonoBehaviour
{
    [Header("UI")]
    public GameObject painelPedido;  // Painel com os botões "Aceitar" / "Recusar"
    public Button botaoAceitar;
    public Button botaoRecusar;

    [Header("Video Player")]
    public VideoPlayer videoPlayer;

    [Header("Clipes")]
    public VideoClip introDesafio;       // vídeo de pedido (opcional)
    public VideoClip recusaDesafio;      // vídeo caso recuse
    public VideoClip[] jogador1Clipes;   // vídeos do jogador 1
    public VideoClip[] jogador2Clipes;   // vídeos do jogador 2

    private bool jogoAtivo = false;
    private bool vezDoJogador1 = true;

    void Start()
    {
        // Mostrar o pedido inicial
        painelPedido.SetActive(true);

        botaoAceitar.onClick.AddListener(AceitarDesafio);
        botaoRecusar.onClick.AddListener(RecusarDesafio);

        if (introDesafio != null)
        {
            videoPlayer.clip = introDesafio;
            videoPlayer.Play();
        }

        videoPlayer.loopPointReached += ProximaAcao;
    }

    void AceitarDesafio()
    {
        painelPedido.SetActive(false);
        jogoAtivo = true;

        // Começa o ping pong
        JogadaJogador1();
    }

    void RecusarDesafio()
    {
        painelPedido.SetActive(false);
        jogoAtivo = false;

        if (recusaDesafio != null)
        {
            videoPlayer.clip = recusaDesafio;
            videoPlayer.Play();
        }
    }

    void JogadaJogador1()
    {
        vezDoJogador1 = true;
        ReproduzirAcao(jogador1Clipes[Random.Range(0, jogador1Clipes.Length)]);
    }

    void JogadaJogador2()
    {
        vezDoJogador1 = false;
        ReproduzirAcao(jogador2Clipes[Random.Range(0, jogador2Clipes.Length)]);
    }

    void ReproduzirAcao(VideoClip clip)
    {
        videoPlayer.Stop();
        videoPlayer.clip = clip;
        videoPlayer.Play();
    }

    void ProximaAcao(VideoPlayer vp)
    {
        if (!jogoAtivo) return;

        if (vezDoJogador1)
            JogadaJogador2();
        else
            JogadaJogador1();
    }
}

